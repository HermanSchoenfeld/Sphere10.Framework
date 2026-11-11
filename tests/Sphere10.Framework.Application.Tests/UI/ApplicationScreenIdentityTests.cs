// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Exercises shared application screen identity tests.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenIdentityTests {
	/// <summary>Verifies distinct screen references remain independent even when their value equality matches.</summary>
	[Test]
	public async Task DistinctScreenReferencesRemainIndependentEvenWhenTheirValueEqualityMatches() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Records" };
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		var definition = Definition(block);
		var first = await host.OpenScreenAsync(block, definition);
		var second = await host.OpenScreenAsync(block, definition);

		Assert.That(first.Equals(second), Is.True);
		Assert.That(second, Is.Not.SameAs(first));
		Assert.That(host.Screens, Has.Length.EqualTo(2));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(2));
		Assert.That(host.ActiveScreen, Is.SameAs(second));
		Assert.That(await host.CloseScreenAsync(first), Is.True);
		Assert.That(host.Screens.Single(), Is.SameAs(second));
		Assert.That(host.OpenScreens.Single(), Is.SameAs(second));
		Assert.That(host.ActiveScreen, Is.SameAs(second));
	}

	/// <summary>Verifies an unhosted equal screen cannot select or close aretained screen.</summary>
	[Test]
	public async Task AnUnhostedEqualScreenCannotSelectOrCloseARetainedScreen() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Records" };
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		var retained = await host.OpenScreenAsync(block, Definition(block));
		var foreign = new EqualScreen { Block = block };
		var notifications = 0;
		host.Changed += () => notifications++;

		Assert.That(async () => await host.ShowScreenAsync(foreign), Throws.ArgumentException);
		Assert.That(async () => await host.CloseScreenAsync(foreign), Throws.ArgumentException);
		Assert.That(async () => await host.CanCloseScreenAsync(foreign), Throws.ArgumentException);
		Assert.That(() => host.NotifyScreenChanged(foreign), Throws.ArgumentException);

		Assert.That(host.ActiveScreen, Is.SameAs(retained));
		Assert.That(foreign.Block, Is.SameAs(block), "Reading a screen's owner does not imply membership in this host.");
		Assert.That(notifications, Is.Zero);
	}

	/// <summary>Verifies returned array snapshots keep the same actual screen without owning its lifetime.</summary>
	[Test]
	public async Task ReturnedArraySnapshotsKeepTheSameActualScreenWithoutOwningItsLifetime() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Records" };
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		var screen = await host.OpenScreenAsync(block, Definition(block));
		var snapshot = host.Screens;

		Assert.That(await host.CloseScreenAsync(screen), Is.True);
		Assert.That(host.Screens, Is.Empty);
		Assert.That(snapshot.Single(), Is.SameAs(screen));
		Assert.That(((EqualScreen)screen).Disposals, Is.Zero);
	}

	/// <summary>Verifies dynamic screen metadata and dirty state are read from the hosted instance.</summary>
	[Test]
	public async Task DynamicScreenMetadataAndDirtyStateAreReadFromTheHostedInstance() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Records" };
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		var screen = (EqualScreen)await host.OpenScreenAsync(block, Definition(block));
		var notifications = 0;
		host.Changed += () => notifications++;
		screen.Title = "Edited record";
		screen.HasUnsavedChanges = true;
		host.NotifyScreenChanged(screen);

		Assert.That(host.ActiveScreen.Title, Is.EqualTo("Edited record"));
		Assert.That(host.HasUnsavedChanges, Is.True);
		Assert.That(notifications, Is.EqualTo(1));
	}

	/// <summary>Verifies late notifications from aclosed instance are rejected without affecting its replacement.</summary>
	[Test]
	public async Task LateNotificationsFromAClosedInstanceAreRejectedWithoutAffectingItsReplacement() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Records" };
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		var definition = Definition(block);
		var original = await host.OpenScreenAsync(block, definition);
		await host.CloseScreenAsync(original);
		var replacement = await host.OpenScreenAsync(block, definition);
		var notifications = 0;
		host.Changed += () => notifications++;

		Assert.That(() => host.NotifyScreenChanged(original), Throws.ArgumentException);

		Assert.That(notifications, Is.Zero);
		Assert.That(host.ActiveScreen, Is.SameAs(replacement));
		Assert.That(replacement, Is.Not.SameAs(original));
	}

	/// <summary>Verifies neutral hosting contract uses screen references without blazor session or guid dependencies.</summary>
	[Test]
	public void NeutralHostingContractUsesScreenReferencesWithoutBlazorSessionOrGuidDependencies() {
		var methods = typeof(IApplicationScreenHost).GetMethods();
		Assert.That(methods.SelectMany(method => method.GetParameters()).Select(parameter => parameter.ParameterType), Has.None.EqualTo(typeof(Guid)));
		Assert.That(typeof(IApplicationScreenHost).GetProperty(nameof(IApplicationScreenHost.ActiveScreen)).PropertyType, Is.EqualTo(typeof(IApplicationScreen)));
		Assert.That(typeof(IApplicationScreenHost).Assembly.GetType("Sphere10.Framework.Application.UI.ApplicationScreenSession"), Is.Null);
		Assert.That(typeof(IApplicationScreenHost).Assembly.GetType("Sphere10.Framework.Application.UI.ApplicationScreenActivation"), Is.Null);
	}

	/// <summary>Verifies shared metadata contracts do not require named identifiers.</summary>
	[TestCase(typeof(IApplicationBlock))]
	[TestCase(typeof(IApplicationMenu))]
	[TestCase(typeof(IApplicationMenuItem))]
	public void SharedMetadataContractsDoNotRequireNamedIdentifiers(Type contract)
		=> Assert.That(contract.GetProperties().Select(property => property.Name), Has.None.EqualTo("Id"));

	/// <summary>Verifies screen commands reference definitions and shared navigation does not use identifier lookup.</summary>
	[Test]
	public void ScreenCommandsReferenceDefinitionsAndSharedNavigationDoesNotUseIdentifierLookup() {
		Assert.That(typeof(IScreenMenuItem).GetProperties().Select(property => property.Name), Is.EquivalentTo(new[] { nameof(IScreenMenuItem.ScreenDefinition), nameof(IScreenMenuItem.Registration) }));
		Assert.That(typeof(IApplicationBlock).GetProperty(nameof(IApplicationBlock.DefaultScreenDefinition)).PropertyType, Is.EqualTo(typeof(ApplicationScreenDefinition)));
		Assert.That(typeof(IApplicationBlock).GetProperty(nameof(IApplicationBlock.ScreenDefinitions)).PropertyType, Is.EqualTo(typeof(ApplicationScreenDefinition[])));
		Assert.That(typeof(ApplicationScreenDefinition).GetProperties().Select(property => property.Name),
			Is.EquivalentTo(new[] { "ScreenType", "Title", "ActivationMode", "ScreenKind", "Activators", "Registration", "StableKey", "Settings" }));
		Assert.That(typeof(IApplicationScreenHost).GetMethods().SelectMany(method => method.GetParameters()).Select(parameter => parameter.Name),
			Has.None.EqualTo("blockId").And.None.EqualTo("menuItemId"));
		Assert.That(typeof(IUserInterfaceServices).GetMethods().Where(method => method.Name == nameof(IUserInterfaceServices.ExecuteMenuItemAsync))
			.SelectMany(method => method.GetParameters()).Select(parameter => parameter.ParameterType), Has.None.EqualTo(typeof(string)));
	}

	/// <summary>Verifies shared storage foundations are abstract and do not expose unused concrete shims.</summary>
	[Test]
	public void SharedStorageFoundationsAreAbstractAndDoNotExposeUnusedConcreteShims() {
		Assert.That(typeof(ApplicationBlockBase).IsAbstract, Is.True);
		Assert.That(typeof(ApplicationMenuBase<>).IsAbstract, Is.True);
		Assert.That(typeof(ApplicationMenuItemBase).IsAbstract, Is.True);
		var assembly = typeof(IApplicationBlock).Assembly;
		Assert.That(assembly.GetType("Sphere10.Framework.Application.UI.ApplicationBlock"), Is.Null);
		Assert.That(assembly.GetType("Sphere10.Framework.Application.UI.ApplicationMenu`1"), Is.Null);
		Assert.That(assembly.GetType("Sphere10.Framework.Application.UI.ApplicationMenuItem"), Is.Null);
	}

	/// <summary>Registers the canonical definition used by the reference-identity scenario.</summary>
	private static ApplicationScreenDefinition Definition(TestApplicationBlock block) {
		var definition = new ApplicationScreenDefinition(typeof(EqualScreen), "Record", ScreenActivationMode.MultiInstance);
		block.AddScreenDefinition(definition);
		return definition;
	}

	#region Inner Types

	/// <summary>Supplies equal screen for the fixture's existing contract checks.</summary>
	public sealed class EqualScreen : IApplicationScreen, IDisposable {
		/// <inheritdoc />
		public IApplicationScreenHost Host { get; set; }

		/// <inheritdoc />
		public IApplicationBlock Block { get; set; }

		/// <inheritdoc />
		public string Title { get; set; }

		/// <inheritdoc />
		public bool HasUnsavedChanges { get; set; }

		/// <inheritdoc />
		public int Disposals { get; private set; }

		/// <inheritdoc />
		public override bool Equals(object other) => other is EqualScreen;

		/// <inheritdoc />
		public override int GetHashCode() => 1;

		/// <summary>Disposes the wrapped host through its existing ownership contract.</summary>
		public void Dispose() => Disposals++;
	}

	#endregion
}
