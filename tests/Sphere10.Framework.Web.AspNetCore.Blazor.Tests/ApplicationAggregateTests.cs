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

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationAggregateTests {
	[Test]
	public async Task SharedAndPlatformRegistrationsProjectOneLiveApplicationPerCircuit() {
		var loaded = 0;
		var unloaded = 0;
		var plugin = new BlazorPluginBuilder().WithName("Workspace")
			.AddBlock(block => block.WithId("work").WithName("Work").WithDefaultScreen<ProbeScreen>()).Build();
		plugin.Loaded += () => loaded++;
		plugin.Unloaded += () => unloaded++;
		var configured = 0;
		var services = new ServiceCollection().AddSphere10BlazorPlugin(plugin)
			.AddSphere10BlazorApplication(application => {
				configured++;
				application.SetToolBarItems(new[] { new BlazorActionMenuItem { Id = "run", Title = "Run", Action = () => { } } });
			});
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var firstScope = provider.CreateAsyncScope();
		await using var secondScope = provider.CreateAsyncScope();
		var application = firstScope.ServiceProvider.GetRequiredService<IBlazorApplication>();
		var shared = firstScope.ServiceProvider.GetRequiredService<IApplication>();
		Assert.That(shared, Is.SameAs(application));
		Assert.That(secondScope.ServiceProvider.GetRequiredService<IApplication>(), Is.Not.SameAs(shared));
		Assert.That(configured, Is.EqualTo(2), "Application configuration must run once per circuit, including when resolved through the shared alias");
		Assert.That(application.ScreenHost, Is.SameAs(firstScope.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>()));
		Assert.That(shared.ActiveScreen, Is.Null);
		var notifications = 0;
		shared.Changed += () => notifications++;
		var session = await application.ScreenHost.ActivateBlockAsync("work");
		var screen = new ProbeScreen { HasUnsavedChanges = true };
		await application.ScreenHost.AttachScreenAsync(session.Id, screen);
		Assert.That(shared.ActiveBlock.Id, Is.EqualTo("work"));
		Assert.That(shared.ActiveScreen, Is.SameAs(screen));
		Assert.That(shared.HasUnsavedChanges, Is.True);
		Assert.That(application.ActivePlugin, Is.SameAs(plugin));
		Assert.That(shared.ActivePlugin, Is.SameAs(plugin));
		Assert.That(application.Plugins.Single(), Is.SameAs(provider.GetServices<IApplicationPlugin>().Single()));
		Assert.That(shared.Plugins.Single(), Is.SameAs(application.Plugins.Single()));
		application.Plugins[0] = null;
		shared.Plugins[0] = null;
		application.LoadedPlugins[0] = null;
		shared.Blocks[0] = null;
		Assert.That(application.LoadedPlugins, Is.EqualTo(new[] { plugin }));
		Assert.That(shared.Blocks.Single().Id, Is.EqualTo("work"));
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
	public async Task StandaloneBlocksHaveALiveImplicitPluginAndBrowsingUpdatesTheSharedActiveOwner() {
		var explicitPlugin = new BlazorPluginBuilder().WithName("Application")
			.AddBlock(block => block.WithId("registered").WithName("Registered").WithDefaultScreen<ProbeScreen>()).Build();
		var services = new ServiceCollection().AddSphere10BlazorPlugin(explicitPlugin)
			.AddApplicationBlock(block => block.WithId("standalone").WithName("Standalone").WithDefaultScreen<ProbeScreen>());
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		await using var scope = provider.CreateAsyncScope();
		await using var otherScope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<IBlazorApplication>();
		var shared = (IApplication)application;
		var implicitPlugin = application.Plugins.Single(plugin => plugin != explicitPlugin);
		Assert.That(implicitPlugin.Name, Is.EqualTo("Application 2"));
		Assert.That(implicitPlugin.Blocks.Single().Id, Is.EqualTo("standalone"));
		Assert.That(((IApplicationPlugin)implicitPlugin).Blocks.Single(), Is.SameAs(implicitPlugin.Blocks.Single()));
		Assert.That(application.Plugins.SelectMany(plugin => plugin.Blocks).Select(block => block.Id), Is.EquivalentTo(application.Blocks.Select(block => block.Id)));
		Assert.That(application.LoadedPlugins, Is.EqualTo(application.Plugins));
		implicitPlugin.Blocks[0] = null;
		Assert.That(implicitPlugin.Blocks.Single(), Is.Not.Null);
		Assert.That(otherScope.ServiceProvider.GetRequiredService<IBlazorApplication>().Plugins.Single(plugin => plugin != explicitPlugin), Is.Not.SameAs(implicitPlugin));

		var session = await application.ScreenHost.ActivateBlockAsync("registered");
		Assert.That(shared.ActivePlugin, Is.SameAs(explicitPlugin));
		await application.ScreenHost.SelectBlockAsync("standalone");
		Assert.That(shared.ActivePlugin, Is.SameAs(implicitPlugin));
		Assert.That(application.ScreenHost.ActiveScreen, Is.SameAs(session), "Browsing ownership follows the selected block without changing its retained screen.");
		Assert.That(await application.ScreenHost.UnregisterBlockAsync("standalone"), Is.True);
		Assert.That(implicitPlugin.Blocks, Is.Empty);
		Assert.That(application.Plugins, Is.EqualTo(new[] { explicitPlugin }));
		await application.ScreenHost.RegisterBlockAsync("standalone");
		Assert.That(application.Plugins.Single(plugin => plugin != explicitPlugin), Is.SameAs(implicitPlugin));
		Assert.That(implicitPlugin.Blocks.Single().Id, Is.EqualTo("standalone"));
	}

	[Test]
	public async Task AnApplicationWithOnlyStandaloneBlocksStillHasACompletePluginHierarchy() {
		var services = new ServiceCollection().AddApplicationBlock(block => block.WithId("direct").WithName("Direct"));
		await using var provider = services.BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		var application = scope.ServiceProvider.GetRequiredService<IApplication>();
		Assert.That(application.Plugins.Single().Name, Is.EqualTo("Application"));
		Assert.That(application.Plugins.Single().Blocks.Single(), Is.SameAs(application.Blocks.Single()));
		await ((IBlazorApplication)application).ScreenHost.SelectBlockAsync("direct");
		Assert.That(application.ActivePlugin, Is.SameAs(application.Plugins.Single()));
	}

	[Test]
	public async Task ApplicationCommandsAreFrozenTypedSnapshotsAndExecuteWithTheExistingHost() {
		var executed = 0;
		var services = new ServiceCollection().AddSphere10Blazor();
		await using var provider = services.BuildServiceProvider();
		await using var scope = provider.CreateAsyncScope();
		var application = (BlazorApplication)scope.ServiceProvider.GetRequiredService<IBlazorApplication>();
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
		Assert.That(await application.ScreenHost.ExecuteMenuItemAsync(application.ToolBarItems.Single()), Is.True);
		Assert.That(executed, Is.EqualTo(1));
		Assert.That(() => application.SetMenus(new[] { new ApplicationMenu<IApplicationMenuItem>() }), Throws.ArgumentException);
		Assert.That(application.Menus.Single().Text, Is.EqualTo("File"));
	}

	private sealed class MutableMenu : IBlazorApplicationMenu {
		public string Id { get; set; }
		public string Text { get; set; }
		public string Icon => null;
		public IBlazorApplicationMenuItem[] Items { get; set; }
	}

	public sealed class ProbeScreen : ComponentBase, IBlazorApplicationScreen {
		public bool HasUnsavedChanges { get; set; }
	}
}
