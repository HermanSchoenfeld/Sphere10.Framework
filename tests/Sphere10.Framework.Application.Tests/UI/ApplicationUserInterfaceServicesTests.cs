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
public class ApplicationUserInterfaceServicesTests {
	[Test]
	public async Task AnActionCanNavigateThroughTheBorrowedHostWithoutReenteringItsLifecycleGate() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock("First", new(typeof(ProbeScreen)));
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		using var userInterface = new ApplicationUserInterfaceServices(host, provider);
		var calls = 0;
		var action = new ActionItem {
			Title = "Open editor", AsyncAction = async (_, token) => {
				calls++;
				await userInterface.ScreenHost.ActivateBlockAsync(block, token);
			}
		};

		Assert.That(await userInterface.ExecuteMenuItemAsync(action).WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(host.ActiveScreen, Is.InstanceOf<ProbeScreen>());
		Assert.That(host.ActiveBlock, Is.SameAs(block));
	}

	[Test]
	public async Task OwnerAwareActionsReceiveTheExecutingUiServicesAndCanBrowseItsHost() {
		IUserInterfaceServices received = null;
		IUserInterfaceServices userInterface = null;
		var registrations = new ServiceCollection();
		registrations.AddSingleton<IUserInterfaceServices>(_ => userInterface);
		using var provider = registrations.BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Workspace" };
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		using var owner = new ApplicationUserInterfaceServices(host, provider);
		userInterface = owner;
		var action = new ActionItem { Title = "Inspect", UIAction = services => received = services };

		Assert.That(await userInterface.ExecuteMenuItemAsync(action), Is.True);
		Assert.That(received, Is.SameAs(userInterface));
		Assert.That(received.ScreenHost, Is.SameAs(host));
		await received.ScreenHost.SelectBlockAsync(block);
		Assert.That(host.ActiveBlock, Is.SameAs(block));
		Assert.That(host.Screens, Is.Empty, "Browsing a block does not create a screen.");
	}

	[Test]
	public async Task RunningApplicationExposesNavigationAndInteractionsThroughItsHost() {
		IUserInterfaceServices registeredUserInterface = null;
		using var provider = new ServiceCollection().AddSingleton<IUserInterfaceServices>(_ => registeredUserInterface).BuildServiceProvider();
		var first = new TestApplicationBlock("First", new(typeof(ProbeScreen)));
		var second = new TestApplicationBlock { Name = "Second" };
		using var host = new ApplicationScreenHost(new[] { first, second }, provider);
		using var userInterface = new ApplicationUserInterfaceServices(host, provider);
		registeredUserInterface = userInterface;
		using IApplication application = new NeutralApplication(host);
		var screen = (ProbeScreen)await application.ScreenHost.ActivateBlockAsync(first);
		screen.HasUnsavedChanges = true;
		host.NotifyScreenChanged(screen);

		Assert.That(application.ScreenHost.UserInterfaceServices, Is.SameAs(userInterface));
		Assert.That(application.ScreenHost, Is.SameAs(host));
		Assert.That(application.ScreenHost.Blocks, Is.EqualTo(host.Blocks));
		Assert.That(application.Blocks, Is.EqualTo(host.Blocks));
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(host.ActiveBlock));
		Assert.That(application.ScreenHost.ActiveScreen, Is.SameAs(screen));
		Assert.That(application.HasUnsavedChanges, Is.True);
		await host.SelectBlockAsync(second);
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(second));
		Assert.That(application.ScreenHost.ActiveScreen, Is.SameAs(screen));
		Assert.That(await host.UnregisterBlockAsync(second), Is.True);
		Assert.That(application.ScreenHost.Blocks, Is.EqualTo(new[] { first }));
		Assert.That(application.Blocks, Is.EqualTo(new[] { first }));
	}

	[Test]
	public async Task DisposingUiServicesReleasesSubscriptionsWithoutDisposingTheBorrowedHostOrScreens() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock("Work", new(typeof(ProbeScreen)));
		using var host = new ApplicationScreenHost(new[] { block }, provider);
		var userInterface = new ApplicationUserInterfaceServices(host, provider);
		var screen = (ProbeScreen)await host.ActivateBlockAsync(block);
		var notifications = 0;
		userInterface.Changed += () => notifications++;

		userInterface.Dispose();
		host.NotifyScreenChanged(screen);

		Assert.That(notifications, Is.Zero);
		Assert.That(host.ActiveScreen, Is.SameAs(screen));
		Assert.That(screen.Disposals, Is.Zero);
		Assert.That(await host.CloseScreenAsync(screen), Is.True);
		await Assert.ThatAsync(async () => await userInterface.ExecuteMenuItemAsync(new ActionItem()), Throws.TypeOf<ObjectDisposedException>());
	}

	[Test]
	public async Task UiDecoratorDispatchesInteractionOverridesAndForwardsRemovableInteractionNotifications() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new ApplicationScreenHost(provider);
		using var implementation = new ApplicationUserInterfaceServices(host, provider);
		var decorator = new InteractionDecorator(implementation);
		IUserInterfaceServices userInterface = decorator;
		var notifications = 0;
		EventHandlerEx handler = () => notifications++;
		userInterface.Changed += handler;

		Assert.That(await userInterface.AskYNAsync("Continue?"), Is.True);
		Assert.That(decorator.Questions, Is.EqualTo(1));
		userInterface.Status = "Ready";
		Assert.That(notifications, Is.EqualTo(1));
		userInterface.Changed -= handler;
		userInterface.Status = "Updated";
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That(userInterface.ScreenHost, Is.SameAs(host));
	}

	[Test]
	public void UiServicesExposeTheHostWithoutDuplicatingItsMembershipOrSelectionContract() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new ApplicationScreenHost(provider);
		using var userInterface = new ApplicationUserInterfaceServices(host, provider);

		Assert.That(userInterface.ScreenHost, Is.SameAs(host));
		Assert.That(typeof(IUserInterfaceServices).GetProperties().Select(property => property.Name),
			Has.None.EqualTo(nameof(IApplicationScreenHost.ActiveBlock)).And.None.EqualTo(nameof(IApplicationScreenHost.Blocks)));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AnUnconfiguredHostOwnsOneExecutableUiServiceAndDisposesOnlyItsFallback(bool asynchronous) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new ApplicationScreenHost(provider);
		IApplicationScreenHost decorated = new HostDecorator(host);
		var userInterface = host.UserInterfaceServices;
		IUserInterfaceServices received = null;
		var action = new ActionItem { Title = "Inspect", UIAction = services => received = services };
		Assert.That(userInterface, Is.Not.Null);
		Assert.That(host.UserInterfaceServices, Is.SameAs(userInterface));
		Assert.That(decorated.UserInterfaceServices, Is.SameAs(userInterface));
		Assert.That(userInterface.ScreenHost, Is.SameAs(host));
		Assert.That(await userInterface.ExecuteMenuItemAsync(action), Is.True);
		Assert.That(received, Is.SameAs(userInterface), "An owned fallback is the execution owner even without a DI registration.");
		if (asynchronous)
			await host.DisposeAsync();
		else
			host.Dispose();
		await Assert.ThatAsync(async () => await userInterface.ExecuteMenuItemAsync(action), Throws.TypeOf<ObjectDisposedException>());
	}

	[Test]
	public async Task AHostBorrowsConfiguredUiServicesWithoutDisposingTheirIndependentLifetime() {
		IUserInterfaceServices configured = null;
		using var provider = new ServiceCollection().AddSingleton<IUserInterfaceServices>(_ => configured).BuildServiceProvider();
		using var host = new ApplicationScreenHost(provider);
		using var implementation = new TrackingUiServices(host, provider);
		configured = implementation;
		Assert.That(host.UserInterfaceServices, Is.SameAs(implementation));
		host.Dispose();
		Assert.That(implementation.Disposals, Is.Zero);
		var calls = 0;
		Assert.That(await implementation.ExecuteMenuItemAsync(new ActionItem { Title = "Inspect", Action = () => calls++ }), Is.True);
		Assert.That(calls, Is.EqualTo(1));
	}

	private sealed class ActionItem : ApplicationAction, IApplicationMenuItem {
		public IApplicationMenu Parent { get; set; }

		public string Title { get; set; }
	}

	private sealed class NeutralApplication : SyncApplicationBase {
		public NeutralApplication(IApplicationScreenHost host) {
			ScreenHost = host;
			ObserveScreenHost();
		}

		public override IApplicationPlugin[] LoadedPlugins => Array.Empty<IApplicationPlugin>();
		public override IApplicationScreenHost ScreenHost { get; }
	}

	private sealed class InteractionDecorator(IUserInterfaceServices userInterface) : UserInterfaceServicesDecorator(userInterface) {
		public int Questions { get; private set; }
		public override Task<bool> AskYNAsync(string question, CancellationToken cancellationToken = default) {
			Questions++;
			return Task.FromResult(true);
		}
	}

	private sealed class HostDecorator(IApplicationScreenHost host) : ApplicationScreenHostDecorator(host) {
	}

	private sealed class TrackingUiServices(IApplicationScreenHost host, IServiceProvider provider) : ApplicationUserInterfaceServices(host, provider) {
		public int Disposals { get; private set; }
		protected override void FreeManagedResources() {
			Disposals++;
			base.FreeManagedResources();
		}
	}

	public sealed class ProbeScreen : IApplicationScreen, IDisposable {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
		public bool HasUnsavedChanges { get; set; }
		public int Disposals { get; private set; }
		public void Dispose() => Disposals++;
	}
}
