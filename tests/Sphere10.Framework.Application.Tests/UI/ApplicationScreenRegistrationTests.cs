// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Protects block-local registration resolution, persistent identity and independent host ownership.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenRegistrationTests {
	[Test]
	public async Task OneSourceMonikerResolvesIndependentDefinitionsAndSingletonsInTwoGraphs() {
		using var services = new ServiceCollection().BuildServiceProvider();
		var source = new ApplicationScreenBuilder<Screen>().WithTitle("Inspector").AsSingleInstance().Build();
		var firstBlock = new TestApplicationBlock("Work", source.Clone());
		var secondBlock = new TestApplicationBlock("Work", source.Clone());
		using var firstHost = new ApplicationScreenHost(new[] { firstBlock }, services);
		using var secondHost = new ApplicationScreenHost(new[] { secondBlock }, services);

		var first = await firstHost.OpenScreenAsync(firstBlock, source.Registration);
		var second = await secondHost.OpenScreenAsync(secondBlock, source.Registration);

		Assert.That(first, Is.Not.SameAs(second));
		Assert.That(first.Block, Is.SameAs(firstBlock));
		Assert.That(second.Block, Is.SameAs(secondBlock));
		Assert.That(firstBlock.DefaultScreenDefinition, Is.Not.SameAs(secondBlock.DefaultScreenDefinition));
		Assert.That(await firstHost.OpenScreenAsync(firstBlock, source.Registration), Is.SameAs(first));
		Assert.That(first.Host, Is.SameAs(firstHost), "Opening a second graph must not retarget a retained source moniker.");
	}

	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.PermanentSingleton)]
	public async Task OnePrototypeRegisteredInTwoBlocksHasIndependentSingletonOwnership(ScreenActivationMode mode) {
		using var services = new ServiceCollection().BuildServiceProvider();
		var prototype = new ApplicationScreenDefinition(typeof(Screen), activationMode: mode);
		var firstBlock = new TestApplicationBlock("First", prototype);
		var secondBlock = new TestApplicationBlock("Second", prototype);
		using var host = new ApplicationScreenHost(new[] { firstBlock, secondBlock }, services);
		await host.InitializeAsync();

		var first = await host.OpenScreenAsync(firstBlock, prototype.Registration);
		var second = await host.OpenScreenAsync(secondBlock, prototype.Registration);

		Assert.That(second, Is.Not.SameAs(first));
		Assert.That(first.Block, Is.SameAs(firstBlock));
		Assert.That(second.Block, Is.SameAs(secondBlock));
		Assert.That(await host.OpenScreenAsync(firstBlock, prototype.Registration), Is.SameAs(first));
		Assert.That(await host.UnregisterBlockAsync(firstBlock), Is.True);
		Assert.That(host.Screens.Single(), Is.SameAs(second));
	}

	[Test]
	public void MenuTypeShortcutSelectsFirstRegistrationAndExactMonikerSelectsItsTarget() {
		var first = new ApplicationScreenDefinition(typeof(Screen), "First", ScreenActivationMode.SingleInstance);
		var second = new ApplicationScreenDefinition(typeof(Screen), "Second", ScreenActivationMode.MultiInstance);
		var block = new TestApplicationBlock("Work", first);
		block.AddScreenDefinition(second);
		var typeCommand = new ShowScreenItem(ApplicationScreenRegistration.ForType(typeof(Screen)));
		var exactCommand = new ShowScreenItem(second.Registration);
		var menu = new TestApplicationMenu<ShowScreenItem> { Text = "Inspect" };
		menu.AddItem(typeCommand);
		menu.AddItem(exactCommand);
		block.AddMenu(menu);

		Tools.UI.ResolveScreenReferences(block);

		Assert.That(typeCommand.ScreenDefinition, Is.SameAs(first));
		Assert.That(exactCommand.ScreenDefinition, Is.SameAs(second));
		Assert.That(typeCommand.Title, Is.EqualTo("First"));
		Assert.That(exactCommand.Title, Is.EqualTo("Second"));
		Assert.That(first.Activators.Single(), Is.SameAs(typeCommand));
		Assert.That(second.Activators.Single(), Is.SameAs(exactCommand));
	}

	[Test]
	public async Task ProgrammaticTypeLookupStillRejectsAmbiguityBeforeRunningScreenGuards() {
		using var services = new ServiceCollection().BuildServiceProvider();
		var first = new ApplicationScreenDefinition(typeof(Screen), "First");
		var block = new TestApplicationBlock("Work", first);
		block.AddScreenDefinition(new ApplicationScreenDefinition(typeof(Screen), "Second"));
		using var host = new ApplicationScreenHost(new[] { block }, services);

		await Assert.ThatAsync(async () => await host.ActivateScreenAsync(block, typeof(Screen)), Throws.ArgumentException);

		Assert.That(host.Screens, Is.Empty);
		Assert.That(host.ActiveScreen, Is.Null);
	}

	[Test]
	public void AShowScreenCommandNeverRegistersItsTargetAsASideEffect() {
		var block = new TestApplicationBlock { Name = "Work" };
		var command = new ShowScreenItem(new ApplicationScreenRegistration<Screen>());
		var menu = new TestApplicationMenu<ShowScreenItem> { Text = "Inspect" };
		menu.AddItem(command);
		block.AddMenu(menu);

		Assert.That(() => Tools.UI.ResolveScreenReferences(block), Throws.ArgumentException);

		Assert.That(block.ScreenDefinitions, Is.Empty);
		Assert.That(command.ScreenDefinition, Is.Null);
		Assert.That(block.DefaultScreenDefinition, Is.Null);
	}

	[Test]
	public void PersistentKeysIgnoreCaptionsAndTransientConstructionTokensAcrossSessions() {
		var first = new ApplicationScreenDefinition(typeof(Screen), "Old title");
		var nextSession = new ApplicationScreenDefinition(typeof(Screen), "Renamed title");
		var firstBlock = new TestApplicationBlock("Work", first);
		var nextBlock = new TestApplicationBlock("Work", nextSession);

		Assert.That(first.Registration.Token, Is.Not.EqualTo(nextSession.Registration.Token));
		Assert.That(Tools.UI.GetScreenRegistrationKey(firstBlock, first), Is.EqualTo(Tools.UI.GetScreenRegistrationKey(nextBlock, nextSession)));
	}

	[Test]
	public void DuplicateTypesNeedExplicitKeysOnlyWhenPersistentIdentityIsRequested() {
		var first = new ApplicationScreenDefinition(typeof(Screen), "First");
		var second = new ApplicationScreenDefinition(typeof(Screen), "Second", stableKey: "secondary");
		var block = new TestApplicationBlock("Work", first);
		block.AddScreenDefinition(second);

		Assert.That(() => Tools.UI.GetScreenDefinitions(new[] { block }), Throws.Nothing);
		Assert.That(() => Tools.UI.GetScreenRegistrationKey(block, first), Throws.ArgumentException.With.Message.Contains("WithStableKey"));
		Assert.That(Tools.UI.GetScreenRegistrationKey(block, second), Is.EqualTo("secondary"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ExplicitPersistentKeysCannotCollideWithOtherExplicitOrGeneratedKeys(bool generatedKey) {
		var first = new ApplicationScreenDefinition(typeof(Screen), stableKey: generatedKey ? null : "shared");
		var block = new TestApplicationBlock("Work", first);
		var duplicate = new ApplicationScreenDefinition(typeof(OtherScreen), stableKey: generatedKey ? Tools.UI.GetScreenRegistrationKey(block, first) : "shared");
		block.AddScreenDefinition(duplicate);

		Assert.That(() => Tools.UI.GetScreenDefinitions(new[] { block }), Throws.ArgumentException.With.Message.Contains("stable keys"));
	}

	[Test]
	public void CopyingDisplayMetadataPreservesDerivedRegistrationStateAndClearsCommandMembership() {
		var definition = new ExtendedDefinition("activation configuration");
		var command = new ShowScreenItem(definition.Registration);
		command.ResolveScreenDefinition(definition);
		var menu = new TestApplicationMenu<ShowScreenItem> { Text = "Inspect" };
		menu.AddItem(command);

		var copy = (ExtendedDefinition)definition.WithTitle("Different display title").WithActivationMode(ScreenActivationMode.SingleInstance);

		Assert.That(copy.Configuration, Is.EqualTo("activation configuration"));
		Assert.That(copy.Registration, Is.SameAs(definition.Registration));
		Assert.That(copy.StableKey, Is.EqualTo("extended"));
		Assert.That(copy.Title, Is.EqualTo("Different display title"));
		Assert.That(copy.Activators, Is.Empty);
		Assert.That(definition.Activators.Single(), Is.SameAs(command));
		Assert.That(definition.ActivationMode, Is.Null);
	}

	private sealed class ShowScreenItem(ApplicationScreenRegistration registration) : ApplicationMenuItemBase, IScreenMenuItem {
		public ApplicationScreenRegistration Registration { get; } = registration;

		public ApplicationScreenDefinition ScreenDefinition { get; private set; }

		public void ResolveScreenDefinition(ApplicationScreenDefinition definition) {
			ScreenDefinition?.RemoveActivator(this);
			ScreenDefinition = definition;
			if (Parent != null)
				definition.AddActivator(this);
			if (string.IsNullOrWhiteSpace(Title))
				Title = definition.Title;
		}
	}

	private sealed class ExtendedDefinition : ApplicationScreenDefinition {
		public ExtendedDefinition(string configuration, ApplicationScreenRegistration registration = null, string title = null, ScreenActivationMode? activationMode = null)
			: base(typeof(Screen), title, activationMode, registration: registration, stableKey: "extended") {
			Configuration = configuration;
		}

		public string Configuration { get; }

		public override ApplicationScreenDefinition Clone() {
			return new ExtendedDefinition(Configuration, Registration, Title, ActivationMode);
		}
	}

	public class Screen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	public class OtherScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}
}
