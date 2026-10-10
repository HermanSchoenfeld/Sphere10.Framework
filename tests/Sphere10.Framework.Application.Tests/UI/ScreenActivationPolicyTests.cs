// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ScreenActivationPolicyTests {
	[Test]
	public async Task CompatibleExplicitAliasesResolveAnUnspecifiedDefinitionWithoutChangingItsMetadata() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var inferred = new ApplicationScreenDefinition(typeof(FirstScreen), "Inferred");
		var explicitAlias = new ApplicationScreenDefinition(typeof(FirstScreen), "Alias", ScreenActivationMode.SingleInstance);
		var block = new TestApplicationBlock("Work", inferred);
		block.AddScreenDefinition(explicitAlias);
		block.AddScreenDefinition(explicitAlias.WithTitle("Another alias"));
		Assert.That(() => ApplicationScreenDefinition.ValidateDefinitions(block.ScreenDefinitions), Throws.Nothing);
		using var host = new ProbeHost(block, provider);
		var first = await host.ActivateBlockAsync(block);
		Assert.That(await host.OpenScreenAsync(block, explicitAlias), Is.SameAs(first));
		Assert.That(host.Mode(first), Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(inferred.ActivationMode, Is.Null);
		Assert.That(inferred.Title, Is.EqualTo("Inferred"));
		Assert.That(first.Host, Is.SameAs(host));
		Assert.That(first.Block, Is.SameAs(block));
	}

	[Test]
	public async Task ConflictingDefinitionBatchDoesNotPartiallyChangeTheHostsTypeIndex() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var initial = new ApplicationScreenDefinition(typeof(FirstScreen), activationMode: ScreenActivationMode.SingleInstance);
		var block = new TestApplicationBlock("Work", initial);
		using var host = new ProbeHost(block, provider);
		var screen = await host.ActivateBlockAsync(block);
		var conflict = new ApplicationScreenDefinition(typeof(FirstScreen), activationMode: ScreenActivationMode.MultiInstance);
		Assert.That(() => host.RegisterDefinitions(new[] {
			new ApplicationScreenDefinition(typeof(SecondScreen), activationMode: ScreenActivationMode.SingleInstance), conflict
		}), Throws.ArgumentException);
		Assert.That(host.ActivationDefinition(typeof(SecondScreen)), Is.Null);
		Assert.That(host.ActivationDefinition(typeof(FirstScreen)), Is.SameAs(initial));
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
		Assert.That(host.Screens.Single(), Is.SameAs(screen));
		Assert.That(((FirstScreen)screen).GuardCalls, Is.Zero);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DefinitionValidationRejectsConflictingModesOrKindsWithoutChangingTheGraph(bool conflictingKinds) {
		var first = new ApplicationScreenDefinition(typeof(FirstScreen), "First", ScreenActivationMode.SingleInstance);
		var second = new ApplicationScreenDefinition(typeof(FirstScreen), "Second",
			conflictingKinds ? ScreenActivationMode.SingleInstance : ScreenActivationMode.MultiInstance,
			conflictingKinds ? ScreenKind.Empty : ScreenKind.Normal);
		var command = new ScreenItem { ScreenDefinition = first };
		var menu = new TestApplicationMenu<TestApplicationMenuItem> { Text = "Actions" };
		menu.AddItem(command);
		Assert.That(() => ApplicationScreenDefinition.ValidateDefinitions(new[] { first, second }), Throws.ArgumentException);
		Assert.That(first.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(second.Title, Is.EqualTo("Second"));
		Assert.That(first.Activators.Single(), Is.SameAs(command));
		Assert.That(command.Parent, Is.SameAs(menu));
		Assert.That(second.Activators, Is.Empty);
		Assert.That(() => ApplicationScreenDefinition.ValidateDefinitions(new[] { first }), Throws.Nothing);
	}

	[Test]
	public void AdapterValidationFailureDoesNotChangeDefinitionsOrMakeTheHostUsableWithIncompatibleTypes() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var adapted = new ApplicationScreenDefinition(typeof(AdaptedScreen), activationMode: ScreenActivationMode.SingleInstance);
		var incompatible = new ApplicationScreenDefinition(typeof(FirstScreen), activationMode: ScreenActivationMode.SingleInstance);
		var validatedTypes = new List<Type>();
		Assert.That(() => ApplicationScreenDefinition.ValidateDefinitions(new[] { adapted, incompatible }, screenType => {
			validatedTypes.Add(screenType);
			Tools.UI.ValidateScreenType(screenType, typeof(IAdapterScreen));
		}), Throws.ArgumentException);
		Assert.That(validatedTypes, Is.EqualTo(new[] { typeof(AdaptedScreen), typeof(FirstScreen) }));
		Assert.That(adapted.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(adapted.Activators, Is.Empty);
		var block = new TestApplicationBlock("Work", adapted);
		block.AddScreenDefinition(incompatible);
		Assert.That(() => new ApplicationScreenHost<TestApplicationBlock, IAdapterScreen>(new[] { block }, provider), Throws.ArgumentException);
		block.RemoveScreenDefinition(incompatible);
		using var host = new ApplicationScreenHost<TestApplicationBlock, IAdapterScreen>(new[] { block }, provider);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(host.Blocks.Single(), Is.SameAs(block));
	}

	[TestCase(ScreenActivationMode.SingleInstance, ScreenActivationMode.MultiInstance)]
	[TestCase(ScreenActivationMode.MultiInstance, ScreenActivationMode.SingleInstance)]
	public async Task RetainedInstanceModeCannotChangeBeforeNavigationGuardsRun(ScreenActivationMode initialMode, ScreenActivationMode otherMode) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var initial = new ApplicationScreenDefinition(typeof(FirstScreen), activationMode: initialMode);
		var block = new TestApplicationBlock("Work", initial);
		using var host = new ProbeHost(block, provider);
		var first = (FirstScreen)await host.ActivateBlockAsync(block);
		var conflict = new ApplicationScreenDefinition(typeof(FirstScreen), activationMode: otherMode);
		block.AddScreenDefinition(conflict);
		Assert.That(async () => await host.OpenScreenAsync(block, conflict), Throws.ArgumentException);
		Assert.That(host.ActiveScreen, Is.SameAs(first));
		Assert.That(host.Mode(first), Is.EqualTo(initialMode));
		Assert.That(first.GuardCalls, Is.Zero);
		Assert.That(first.DeactivationCalls, Is.Zero);
		Assert.That(host.Screens.Single(), Is.SameAs(first));
		block.RemoveScreenDefinition(conflict);
		var next = await host.ActivateBlockAsync(block);
		if (initialMode == ScreenActivationMode.SingleInstance)
			Assert.That(next, Is.SameAs(first));
		else
			Assert.That(next, Is.Not.SameAs(first));
	}

	[Test]
	public async Task PureValidationDoesNotCreateOrRetainScreenInstances() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var definition = new ApplicationScreenDefinition(typeof(FirstScreen), activationMode: ScreenActivationMode.SingleInstance);
		var block = new TestApplicationBlock("Work", definition);
		ApplicationScreenDefinition.ValidateDefinitions(new[] { definition });
		using var host = new ProbeHost(block, provider);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(host.ActiveScreen, Is.Null);
		Assert.That(host.CreatedScreens, Is.Zero);
		await host.ActivateBlockAsync(block);
		Assert.That(host.CreatedScreens, Is.EqualTo(1));
		Assert.That(host.Screens, Has.Length.EqualTo(1));
	}

	[Test]
	public async Task AHostingKindCanChangeAfterItsPreviousInstanceAndDefinitionAreRemoved() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var normal = new ApplicationScreenDefinition(typeof(FirstScreen), "Editor", ScreenActivationMode.SingleInstance);
		var block = new TestApplicationBlock { Name = "Work" };
		block.AddScreenDefinition(normal);
		using var host = new ProbeHost(block, provider);
		var previous = await host.OpenScreenAsync(block, normal);
		Assert.That(await host.CloseScreenAsync(previous), Is.True);
		Assert.That(host.Screens, Is.Empty);
		block.RemoveScreenDefinition(normal);
		var empty = new ApplicationScreenDefinition(typeof(FirstScreen), "Placeholder", ScreenActivationMode.SingleInstance, ScreenKind.Empty);
		block.AddScreenDefinition(empty);
		var current = await host.OpenScreenAsync(block, empty);
		Assert.That(current, Is.Not.Null.And.Not.SameAs(previous));
		Assert.That(host.ActiveScreen, Is.SameAs(current));
		Assert.That(host.Mode(current), Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(host.Screens.Single(), Is.SameAs(current));
		Assert.That(current.Host, Is.SameAs(host));
		Assert.That(current.Block, Is.SameAs(block));
		Assert.That(previous.Host, Is.Null);
		Assert.That(previous.Block, Is.Null);
	}

	[Test]
	public void DefinitionValidationRejectsNullCollectionsAndMembers() {
		Assert.That(() => ApplicationScreenDefinition.ValidateDefinitions(null), Throws.ArgumentNullException);
		Assert.That(() => ApplicationScreenDefinition.ValidateDefinitions(new ApplicationScreenDefinition[] { null }), Throws.ArgumentNullException);
		Assert.That(() => ApplicationScreenDefinition.ValidateDefinitions(Array.Empty<ApplicationScreenDefinition>()), Throws.Nothing);
	}

	[TestCase(typeof(string))]
	[TestCase(typeof(IApplicationScreen))]
	[TestCase(typeof(AbstractScreen))]
	[TestCase(typeof(GenericScreen<>))]
	[TestCase(null)]
	public void InvalidScreenTypesCannotBecomeDefinitions(Type screenType) =>
		Assert.That(() => new ApplicationScreenDefinition(screenType), Throws.InstanceOf<ArgumentException>());

	[TestCase(-1)]
	[TestCase(3)]
	[TestCase(int.MaxValue)]
	public void InvalidActivationModesCannotBecomeDefinitions(int mode) =>
		Assert.That(() => new ApplicationScreenDefinition(typeof(FirstScreen), activationMode: (ScreenActivationMode)mode), Throws.ArgumentException);

	private sealed class ScreenItem : TestApplicationMenuItem, IScreenMenuItem {
		public ApplicationScreenDefinition ScreenDefinition { get; set; }
	}

	private sealed class ProbeHost(TestApplicationBlock block, IServiceProvider provider) : ApplicationScreenHost<TestApplicationBlock, IApplicationScreen>(new[] { block }, provider) {
		public int CreatedScreens { get; private set; }
		public ScreenActivationMode Mode(IApplicationScreen screen) => GetScreenActivationMode(screen);
		public ApplicationScreenDefinition ActivationDefinition(Type screenType) => GetScreenActivationDefinition(screenType);
		public void RegisterDefinitions(IEnumerable<ApplicationScreenDefinition> definitions) => RegisterTrackedDefinitions(definitions);

		protected override IApplicationScreen CreateScreen(TestApplicationBlock block, ApplicationScreenDefinition definition) {
			CreatedScreens++;
			return base.CreateScreen(block, definition);
		}
	}

	public class FirstScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
		public int GuardCalls { get; private set; }
		public int DeactivationCalls { get; private set; }

		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) {
			GuardCalls++;
			return Task.FromResult(true);
		}

		public Task OnDeactivatedAsync(CancellationToken cancellationToken = default) {
			DeactivationCalls++;
			return Task.CompletedTask;
		}
	}

	public class SecondScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
	}

	private interface IAdapterScreen : IApplicationScreen {
	}

	private class AdaptedScreen : IAdapterScreen {
		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
	}

	private abstract class AbstractScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
	}

	private class GenericScreen<T> : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
	}
}
