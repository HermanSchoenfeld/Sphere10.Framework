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

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenIdentityTests {
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

	[Test]
	public async Task AnUnhostedEqualScreenCannotSelectOrCloseARetainedScreen() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Records" };
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		var retained = await host.OpenScreenAsync(block, Definition(block));
		var foreign = new EqualScreen();
		var notifications = 0;
		host.Changed += () => notifications++;

		Assert.That(async () => await host.ShowScreenAsync(foreign), Throws.ArgumentException);
		Assert.That(async () => await host.CloseScreenAsync(foreign), Throws.ArgumentException);
		Assert.That(async () => await host.CanCloseScreenAsync(foreign), Throws.ArgumentException);
		Assert.That(() => host.NotifyScreenChanged(foreign), Throws.ArgumentException);

		Assert.That(host.ActiveScreen, Is.SameAs(retained));
		Assert.That(host.GetScreenBlock(foreign), Is.Null);
		Assert.That(notifications, Is.Zero);
	}

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

	[Test]
	public void NeutralHostingContractUsesScreenReferencesWithoutBlazorSessionOrGuidDependencies() {
		var methods = typeof(IApplicationScreenHost).GetMethods();
		Assert.That(methods.SelectMany(method => method.GetParameters()).Select(parameter => parameter.ParameterType), Has.None.EqualTo(typeof(Guid)));
		Assert.That(typeof(IApplicationScreenHost).GetProperty(nameof(IApplicationScreenHost.ActiveScreen)).PropertyType, Is.EqualTo(typeof(IApplicationScreen)));
		Assert.That(typeof(IApplicationScreenHost).Assembly.GetType("Sphere10.Framework.Application.UI.ApplicationScreenSession"), Is.Null);
		Assert.That(typeof(IApplicationScreenHost).Assembly.GetType("Sphere10.Framework.Application.UI.ApplicationScreenActivation"), Is.Null);
	}

	[TestCase(typeof(IApplicationBlock))]
	[TestCase(typeof(IApplicationMenu))]
	[TestCase(typeof(IApplicationMenuItem))]
	public void SharedMetadataContractsDoNotRequireNamedIdentifiers(Type contract) =>
		Assert.That(contract.GetProperties().Select(property => property.Name), Has.None.EqualTo("Id"));

	[Test]
	public void ScreenCommandsReferenceDefinitionsAndSharedNavigationDoesNotUseIdentifierLookup() {
		Assert.That(typeof(IScreenMenuItem).GetProperties().Select(property => property.Name), Is.EqualTo(new[] { nameof(IScreenMenuItem.ScreenDefinition) }));
		Assert.That(typeof(IApplicationBlock).GetProperty(nameof(IApplicationBlock.DefaultScreenDefinition)).PropertyType, Is.EqualTo(typeof(ApplicationScreenDefinition)));
		Assert.That(typeof(IApplicationBlock).GetProperty(nameof(IApplicationBlock.ScreenDefinitions)).PropertyType, Is.EqualTo(typeof(ApplicationScreenDefinition[])));
		Assert.That(typeof(ApplicationScreenDefinition).GetProperties().Select(property => property.Name),
			Is.EquivalentTo(new[] { "ScreenType", "Title", "ActivationMode", "ScreenKind", "Activators" }));
		Assert.That(typeof(IApplicationScreenHost).GetMethods().SelectMany(method => method.GetParameters()).Select(parameter => parameter.Name),
			Has.None.EqualTo("blockId").And.None.EqualTo("menuItemId"));
		Assert.That(typeof(IUserInterfaceServices).GetMethods().Where(method => method.Name == nameof(IUserInterfaceServices.ExecuteMenuItemAsync))
			.SelectMany(method => method.GetParameters()).Select(parameter => parameter.ParameterType), Has.None.EqualTo(typeof(string)));
	}

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

	private static ApplicationScreenDefinition Definition(TestApplicationBlock block) {
		var definition = new ApplicationScreenDefinition(typeof(EqualScreen), "Record", ScreenActivationMode.MultiInstance);
		block.AddScreenDefinition(definition);
		return definition;
	}

	public sealed class EqualScreen : IApplicationScreen, IDisposable {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
		public string Title { get; set; }
		public bool HasUnsavedChanges { get; set; }
		public int Disposals { get; private set; }
		public override bool Equals(object other) => other is EqualScreen;
		public override int GetHashCode() => 1;
		public void Dispose() => Disposals++;
	}
}
