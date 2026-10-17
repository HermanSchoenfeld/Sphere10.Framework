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
public class ApplicationScreenHostCapabilityTests {
	[Test]
	public void ContractsSeparateApplicationCompositionAndScreenHostingCapabilities() {
		Assert.That(typeof(IApplication).GetProperty(nameof(IApplication.Blocks)).PropertyType, Is.EqualTo(typeof(IApplicationBlock[])));
		Assert.That(typeof(IApplication).GetProperty(nameof(IApplication.LoadedPlugins)).PropertyType, Is.EqualTo(typeof(IApplicationPlugin[])));
		Assert.That(typeof(IApplication).GetProperties().Select(property => property.Name),
			Has.None.EqualTo("Plugins").And.None.EqualTo("ActivePlugin").And.None.EqualTo("ActiveBlock").And.None.EqualTo("ActiveScreen").And.None.EqualTo("UserInterfaceServices"));
		Assert.That(typeof(IApplicationScreenHost).GetProperties().Select(property => property.Name),
			Has.None.EqualTo(nameof(IMultiApplicationScreenHost.Screens)).And.None.EqualTo(nameof(IMultiApplicationScreenHost.OpenScreens)));
		Assert.That(typeof(IApplicationScreenHost).GetMethods().Select(method => method.Name),
			Has.None.EqualTo(nameof(IMultiApplicationScreenHost.MoveScreenAsync)).And.None.EqualTo(nameof(IMultiApplicationScreenHost.CloseScreensAsync)));
		Assert.That(typeof(ISingleApplicationScreenHost).GetProperties().Select(property => property.Name), Has.None.EqualTo(nameof(IMultiApplicationScreenHost.Screens)));
		Assert.That(typeof(IApplicationScreenHost).GetProperty(nameof(IApplicationScreenHost.ScreenMode)).CanWrite, Is.True);
		Assert.That(typeof(IApplicationScreenHost).GetProperty(nameof(IApplicationScreenHost.UserInterfaceServices)).PropertyType, Is.EqualTo(typeof(IUserInterfaceServices)));
		Assert.That(typeof(IApplicationScreenHost).GetMethod(nameof(IApplicationScreenHost.ShowScreenUndockedAsync)), Is.Not.Null);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ALogicalHostRejectsDetachedPresentationWithoutChangingOwnershipOrSelection(bool decorate) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = Block();
		using var owner = new ApplicationScreenHost(new[] { block }, provider);
		using IApplicationScreenHost host = decorate ? new SingleDecorator(owner) : owner;
		var active = await host.ActivateBlockAsync(block);
		var supplied = new ProbeScreen();
		await Assert.ThatAsync(async () => await host.ShowScreenUndockedAsync(supplied), Throws.TypeOf<NotSupportedException>());
		Assert.That(host.ActiveScreen, Is.SameAs(active));
		Assert.That(owner.OpenScreens, Is.EqualTo(new[] { active }));
		Assert.That(supplied.Host, Is.Null);
		Assert.That(supplied.Block, Is.Null);
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		await Assert.ThatAsync(async () => await host.ShowScreenUndockedAsync(supplied, canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(supplied.Host, Is.Null);
	}

	[Test]
	public async Task SingleScreenClosureRunsTheCurrentScreensGuardBeforeChangingSelection() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = Block();
		using var owner = new ApplicationScreenHost(new[] { block }, provider);
		ISingleApplicationScreenHost single = owner;
		var screen = (ProbeScreen)await single.ActivateBlockAsync(block);
		screen.AllowDeactivation = false;
		Assert.That(await single.CanCloseActiveScreenAsync(), Is.False);
		Assert.That(await single.CloseActiveScreenAsync(), Is.False);
		Assert.That(single.ActiveScreen, Is.SameAs(screen));
		screen.AllowDeactivation = true;
		Assert.That(await single.CanCloseActiveScreenAsync(), Is.True);
		Assert.That(await single.CloseActiveScreenAsync(), Is.True);
		Assert.That(single.ActiveScreen, Is.Null);
		Assert.That(owner.Screens, Is.Empty);
	}

	[Test]
	public async Task SingleScreenClosureCannotRemoveTheCurrentPermanentScreen() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = Block(ScreenActivationMode.PermanentSingleton);
		using var owner = new ApplicationScreenHost(new[] { block }, provider);
		ISingleApplicationScreenHost single = owner;
		var screen = await single.ActivateBlockAsync(block);
		Assert.That(await single.CanCloseActiveScreenAsync(), Is.False);
		Assert.That(await single.CloseActiveScreenAsync(), Is.False);
		Assert.That(single.ActiveScreen, Is.SameAs(screen));
		Assert.That(owner.OpenScreens.Single(), Is.SameAs(screen));
	}

	[Test]
	public async Task SynchronousModeChangesPreserveScreensWhenNoClosureIsRequired() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = Block();
		using var owner = new ApplicationScreenHost(new[] { block }, provider);
		IApplicationScreenHost host = owner;
		var screen = (ProbeScreen)await host.ActivateBlockAsync(block);
		screen.AllowDeactivation = false;
		host.ScreenMode = ScreenMode.SingleView;
		Assert.That(host.ScreenMode, Is.EqualTo(ScreenMode.SingleView));
		host.ScreenMode = ScreenMode.MultiView;
		Assert.That(host.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
		Assert.That(owner.OpenScreens.Single(), Is.SameAs(screen));
		Assert.That(screen.GuardCalls, Is.Zero, "Neither assignment changes which screen is hosted or closes a view.");
	}

	[Test]
	public async Task SynchronousSingleViewAssignmentRejectsBeforeCallingARequiredAsyncGuard() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = Block();
		using var owner = new ApplicationScreenHost(new[] { block }, provider);
		var first = (ProbeScreen)await owner.ActivateBlockAsync(block);
		var second = await owner.ActivateBlockAsync(block);
		first.GuardCalls = 0;
		Assert.That(() => owner.ScreenMode = ScreenMode.SingleView, Throws.InvalidOperationException);
		Assert.That(owner.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(owner.ActiveScreen, Is.SameAs(second));
		Assert.That(owner.OpenScreens, Is.EqualTo(new[] { first, second }));
		Assert.That(first.GuardCalls, Is.Zero);
	}

	[Test]
	public async Task RuntimeModeChangeWaitsForDeactivationPermissionBeforeCommitting() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = Block();
		using var owner = new ApplicationScreenHost(new[] { block }, provider);
		var first = (ProbeScreen)await owner.ActivateBlockAsync(block);
		var second = await owner.ActivateBlockAsync(block);
		first.Guard = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var change = owner.TrySetScreenModeAsync(ScreenMode.SingleView);
		Assert.That(change.IsCompleted, Is.False);
		Assert.That(owner.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(owner.OpenScreens, Has.Length.EqualTo(2));
		first.Guard.SetResult(true);
		Assert.That(await change, Is.True);
		Assert.That(owner.ScreenMode, Is.EqualTo(ScreenMode.SingleView));
		Assert.That(owner.OpenScreens.Single(), Is.SameAs(second));
		Assert.That(owner.Screens.Single(), Is.SameAs(second));
	}

	[Test]
	public void InvalidModeAssignmentLeavesConfigurationIntact() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var owner = new ApplicationScreenHost(provider);
		Assert.That(() => owner.ScreenMode = (ScreenMode)999, Throws.ArgumentException);
		Assert.That(owner.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
	}

	[Test]
	public async Task CapabilityDecoratorsExposeOnlyTheirDeclaredOperationsAndForwardTheSameState() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = Block();
		using var owner = new ApplicationScreenHost(new[] { block }, provider);
		using var single = new SingleDecorator(owner);
		using var multi = new MultiDecorator(owner);
		Assert.That(single, Is.Not.InstanceOf<IMultiApplicationScreenHost>());
		Assert.That(multi, Is.Not.InstanceOf<ISingleApplicationScreenHost>());
		var screen = await single.ActivateBlockAsync(block);
		Assert.That(multi.ActiveScreen, Is.SameAs(screen));
		Assert.That(multi.Screens.Single(), Is.SameAs(screen));
		single.ScreenMode = ScreenMode.SingleView;
		Assert.That(multi.ScreenMode, Is.EqualTo(ScreenMode.SingleView));
		Assert.That(await single.CloseActiveScreenAsync(), Is.True);
		Assert.That(multi.OpenScreens, Is.Empty);
	}

	private static TestApplicationBlock Block(ScreenActivationMode mode = ScreenActivationMode.MultiInstance) =>
		new("Work", new ApplicationScreenDefinition(typeof(ProbeScreen), activationMode: mode));

	private sealed class ProbeScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
		public bool AllowDeactivation { get; set; } = true;
		public int GuardCalls { get; set; }
		public TaskCompletionSource<bool> Guard { get; set; }
		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) {
			GuardCalls++;
			return Guard?.Task ?? Task.FromResult(AllowDeactivation);
		}
	}

	private sealed class SingleDecorator(ISingleApplicationScreenHost host) : SingleApplicationScreenHostDecorator(host) {
	}

	private sealed class MultiDecorator(IMultiApplicationScreenHost host) : MultiApplicationScreenHostDecorator(host) {
	}
}
