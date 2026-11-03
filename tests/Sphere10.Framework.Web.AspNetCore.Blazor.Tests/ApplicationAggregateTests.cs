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
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationAggregateTests {
	[Test]
	public async Task WebMenuSettersRejectChangesAfterDisposal() {
		await using var provider = new ServiceCollection().AddSphere10Blazor().BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		var application = (BlazorUI.Application)scope.ServiceProvider.GetRequiredService<BlazorUI.IApplication>();
		application.SetMenus(new[] { new ApplicationMenu { Id = "file", Text = "File" } });
		application.SetToolBarItems(new[] { new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => { } } });

		await application.DisposeAsync();

		Assert.That(() => application.SetMenus(Array.Empty<SharedUI.IApplicationMenu>()), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => application.SetToolBarItems(Array.Empty<SharedUI.IApplicationMenuItem>()), Throws.TypeOf<ObjectDisposedException>());
	}

	[Test]
	public async Task SharedAndPlatformRegistrationsProjectOneLiveApplicationPerCircuit() {
		var loaded = 0;
		var unloaded = 0;
		var plugin = new BlazorPluginBuilder()
			.WithName("Workspace")
			.AddBlock(block => block
				.WithId("work")
				.WithName("Work")
				.AddScreen<ProbeScreen>(screen => screen
					.WithTitle(nameof(ProbeScreen))
				)
				.WithDefaultScreen<ProbeScreen>()
			)
			.Build();
		plugin.Loaded += () => loaded++;
		plugin.Unloaded += () => unloaded++;
		var configured = 0;
		var services = new ServiceCollection()
			.AddSphere10BlazorPlugin(plugin)
			.AddSphere10BlazorApplication(application => {
				configured++;
				application.SetMenus(new[] { new ApplicationMenu { Id = "file", Text = "File" } });
				application.SetToolBarItems(new[] { new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => { } } });
			});
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var firstScope = provider.CreateAsyncScope();
		await using var secondScope = provider.CreateAsyncScope();
		var application = firstScope.ServiceProvider.GetRequiredService<BlazorUI.IApplication>();
		var shared = firstScope.ServiceProvider.GetRequiredService<SharedUI.IApplication>();
		Assert.That(shared, Is.SameAs(application));
		Assert.That(shared.Menus.Single(), Is.SameAs(application.Menus.Single()));
		Assert.That(shared.ToolBarItems.Single(), Is.SameAs(application.ToolBarItems.Single()));
		shared.Menus[0] = null;
		shared.ToolBarItems[0] = null;
		Assert.That(shared.Menus.Single().Text, Is.EqualTo("File"));
		Assert.That(shared.ToolBarItems.Single().Title, Is.EqualTo("Run"));
		Assert.That(secondScope.ServiceProvider.GetRequiredService<SharedUI.IApplication>(), Is.Not.SameAs(shared));
		Assert.That(configured, Is.EqualTo(2), "Application configuration must run once per circuit, including when resolved through the shared alias");
		Assert.That(application.ScreenHost, Is.SameAs(firstScope.ServiceProvider.GetRequiredService<BlazorUI.IApplicationScreenHost>()));
		Assert.That(application.ScreenHost.ActiveScreen, Is.Null);
		var notifications = 0;
		shared.Changed += () => notifications++;
		var session = await application.ScreenHost.ActivateBlockAsync("work");
		var screen = new ProbeScreen { HasUnsavedChanges = true };
		await application.ScreenHost.AttachScreenAsync(session.Id, screen);
		Assert.That(shared.ScreenHost.SelectedBlock, Is.SameAs(application.ScreenHost.SelectedBlock));
		Assert.That(application.ScreenHost.ActiveScreen, Is.SameAs(session));
		Assert.That(((ApplicationScreenSession)application.ScreenHost.ActiveScreen).Screen, Is.SameAs(screen));
		Assert.That(shared.HasUnsavedChanges, Is.True);
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(provider.GetServices<SharedUI.IApplicationPlugin>().Single()));
		Assert.That(shared.LoadedPlugins.Single(), Is.SameAs(application.LoadedPlugins.Single()));
		Assert.That(shared.Blocks.Single(), Is.SameAs(application.Blocks.Single()));
		Assert.That(application.Blocks.Single(), Is.SameAs(application.ScreenHost.Blocks.Single()));
		shared.LoadedPlugins[0] = null;
		application.LoadedPlugins[0] = null;
		shared.Blocks[0] = null;
		application.Blocks[0] = null;
		Assert.That(application.LoadedPlugins, Is.EqualTo(new[] { plugin }));
		Assert.That(shared.Blocks.Single(), Is.SameAs(application.ScreenHost.Blocks.Single()));
		Assert.That(notifications, Is.GreaterThan(0));
		Assert.That(loaded, Is.EqualTo(1), "Resolving another circuit must not rerun plugin startup");
		await application.DisposeAsync();
		var notificationsAfterDisposal = notifications;
		application.ScreenHost.NotifyScreenChanged(session.Id);
		Assert.That(notifications, Is.EqualTo(notificationsAfterDisposal));
		Assert.That(await application.ScreenHost.CloseScreenAsync(session.Id), Is.True, "The aggregate borrows the scoped host rather than disposing it");
		Assert.That(unloaded, Is.Zero, "Circuit cleanup must not unload shared plugin definitions");
	}

	[Test]
	public async Task ApplicationBlocksFollowLiveHostMembershipIndependentlyOfLoadedPlugins() {
		var explicitPlugin = new BlazorPluginBuilder()
			.WithName("Application")
			.AddBlock(block => block
				.WithId("registered")
				.WithName("Registered")
				.AddScreen<ProbeScreen>(screen => screen
					.WithTitle(nameof(ProbeScreen))
				)
				.WithDefaultScreen<ProbeScreen>()
			)
			.Build();
		var services = new ServiceCollection()
			.AddSphere10BlazorPlugin(explicitPlugin)
			.AddApplicationBlock(block => block
				.WithId("standalone")
				.WithName("Standalone")
				.AddScreen<ProbeScreen>(screen => screen
					.WithTitle(nameof(ProbeScreen))
				)
				.WithDefaultScreen<ProbeScreen>()
			);
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		await using var otherScope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<BlazorUI.IApplication>();
		var shared = (SharedUI.IApplication)application;
		Assert.That(application.LoadedPlugins, Is.EqualTo(new[] { explicitPlugin }));
		Assert.That(application.Blocks.Select(block => block.Id), Is.EquivalentTo(new[] { "registered", "standalone" }));
		Assert.That(shared.Blocks, Is.EqualTo(application.ScreenHost.Blocks));
		Assert.That(application.Blocks.Single(block => block.Id == "registered"), Is.Not.SameAs(explicitPlugin.Blocks.Single()),
			"The application exposes canonical host snapshots rather than plugin source definitions.");
		application.Blocks[0] = null;
		Assert.That(application.Blocks, Has.None.Null);
		Assert.That(otherScope.ServiceProvider.GetRequiredService<BlazorUI.IApplication>().LoadedPlugins.Single(), Is.SameAs(explicitPlugin));

		var session = await application.ScreenHost.ActivateBlockAsync("registered");
		await application.ScreenHost.SelectBlockAsync("standalone");
		Assert.That(shared.ScreenHost.SelectedBlock, Is.SameAs(application.Blocks.Single(block => block.Id == "standalone")));
		Assert.That(application.ScreenHost.ActiveScreen, Is.SameAs(session), "Browsing blocks preserves the retained screen.");
		Assert.That(await application.ScreenHost.UnregisterBlockAsync("standalone"), Is.True);
		Assert.That(shared.Blocks.Single(), Is.SameAs(application.ScreenHost.Blocks.Single()));
		await application.ScreenHost.RegisterBlockAsync("standalone");
		Assert.That(application.Blocks.Select(block => block.Id), Is.EquivalentTo(new[] { "registered", "standalone" }));
		Assert.That(await application.ScreenHost.CloseScreenAsync(session.Id), Is.True);
		Assert.That(await application.ScreenHost.UnregisterBlockAsync("registered"), Is.True);
		Assert.That(application.Blocks.Single().Id, Is.EqualTo("standalone"));
		Assert.That(shared.LoadedPlugins, Is.EqualTo(new[] { explicitPlugin }), "Runtime registration does not change successful startup metadata.");
		await application.ScreenHost.RegisterBlockAsync("registered");
		Assert.That(application.Blocks.Single(block => block.Id == "registered"), Is.SameAs(application.ScreenHost.Blocks.Single(block => block.Id == "registered")));
	}

	[Test]
	public async Task StandaloneBlocksRemainNavigableWithoutSyntheticLoadedPlugins() {
		var services = new ServiceCollection()
			.AddApplicationBlock(block => block
				.WithId("direct")
				.WithName("Direct")
			);
		await using var provider = services.BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<SharedUI.IApplication>();
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks.Single(), Is.SameAs(application.ScreenHost.Blocks.Single()));
		await ((BlazorUI.IApplication)application).ScreenHost.SelectBlockAsync("direct");
		Assert.That(application.ScreenHost.SelectedBlock, Is.SameAs(application.Blocks.Single()));
	}

	[Test]
	public async Task ApplicationCommandsAreFrozenTypedSnapshotsAndExecuteWithTheExistingHost() {
		var executed = 0;
		var services = new ServiceCollection().AddSphere10Blazor();
		await using var provider = services.BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		var application = (BlazorUI.Application)scope.ServiceProvider.GetRequiredService<BlazorUI.IApplication>();
		var item = new BlazorActionMenuItem { Id = "save", Title = "Save", Action = () => executed++ };
		var menu = new MutableMenu { Id = "file", Text = "File", Items = new[] { item } };
		application.SetMenus(new[] { menu });
		application.SetToolBarItems(new[] { item });
		menu.Text = "Changed";
		menu.Items[0] = null;
		item.Select += () => executed += 100;
		application.Menus[0] = null;
		application.ToolBarItems[0] = null;
		Assert.That(application.Menus.Single().Text, Is.EqualTo("File"));
		Assert.That(application.ToolBarItems.Single().Title, Is.EqualTo("Save"));
		Assert.That(await application.ScreenHost.UserInterfaceServices.ExecuteMenuItemAsync(application.ToolBarItems.Single()), Is.True);
		Assert.That(executed, Is.EqualTo(1));
		Assert.That(() => application.SetMenus(new[] { new NeutralMenu() }), Throws.ArgumentException);
		Assert.That(application.Menus.Single().Text, Is.EqualTo("File"));
	}

	private sealed class NeutralMenu : SharedUI.IApplicationMenu {
		public SharedUI.IApplicationBlock Parent { get; set; }

		public string Text => "File";
		public SharedUI.IApplicationMenuItem[] Items => Array.Empty<SharedUI.IApplicationMenuItem>();
	}

	private sealed class MutableMenu : BlazorUI.IApplicationMenu {
		public BlazorUI.IApplicationBlock Parent { get; set; }

		public string Id { get; set; }
		public string Text { get; set; }
		public string Icon => null;
		public BlazorUI.IApplicationMenuItem[] Items { get; set; }
	}

	public sealed class ProbeScreen : ComponentBase, BlazorUI.IApplicationScreen {
		public BlazorUI.IApplicationScreenHost Host { get; set; }

		public BlazorUI.IApplicationBlock Block { get; set; }

		public bool HasUnsavedChanges { get; set; }
	}
}
