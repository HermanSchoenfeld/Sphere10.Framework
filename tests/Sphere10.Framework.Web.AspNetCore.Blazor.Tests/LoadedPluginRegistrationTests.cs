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
public class LoadedPluginRegistrationTests {
	[TestCase(false)]
	[TestCase(true)]
	public void RegisteredButUnloadedPluginsAreExcludedFromBothApplicationFactories(bool configureApplication) {
		var plugin = new BlazorPlugin("Unloaded", Array.Empty<IBlazorApplicationBlock>());
		var loaded = 0;
		plugin.Loaded += () => loaded++;
		var services = new ServiceCollection().AddSphere10Blazor();
		services.AddSingleton<IBlazorPlugin>(plugin);
		if (configureApplication)
			services.AddSphere10BlazorApplication(_ => { });
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var scope = provider.CreateScope();
		var application = scope.ServiceProvider.GetRequiredService<IBlazorApplication>();

		Assert.That(provider.GetServices<IBlazorPlugin>().Single(), Is.SameAs(plugin));
		Assert.That(application.LoadedPlugins, Is.Empty);
		Assert.That(((IApplication)application).LoadedPlugins, Is.Empty);
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>(), Is.Empty);
		Assert.That(loaded, Is.Zero, "Circuit resolution must not turn ordinary DI metadata into a startup load.");
	}

	[Test]
	public void FailedLoadDoesNotPublishAReceiptOrDisplaceSuccessfulPlugins() {
		var successful = new BlazorPlugin("Successful", Array.Empty<IBlazorApplicationBlock>());
		var failing = new BlazorPluginBuilder()
			.WithName("Failing")
			.ConfigureServices(services => {
				services.AddSingleton<StartupMarker>();
				Guard.Ensure(false, "Startup failed.");
			})
			.Build();
		var services = new ServiceCollection().AddSphere10BlazorPlugin(successful);
		Assert.That(() => services.AddSphere10BlazorPlugin(failing), Throws.InvalidOperationException);
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IBlazorApplication>();

		Assert.That(application.LoadedPlugins, Is.EqualTo(new[] { successful }));
		Assert.That(provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>().Single().Plugin, Is.SameAs(successful));
		Assert.That(provider.GetServices<IApplicationPlugin>(), Is.EqualTo(new[] { successful }));
		Assert.That(provider.GetRequiredService<StartupMarker>(), Is.Not.Null, "The receipt does not imply rollback of service callback side effects.");
	}

	[Test]
	public void DuplicateRegistrationDoesNotLoadAgainOrPublishAnotherReceipt() {
		var plugin = new BlazorPlugin("Once", Array.Empty<IBlazorApplicationBlock>());
		var loaded = 0;
		plugin.Loaded += () => loaded++;
		var services = new ServiceCollection().AddSphere10BlazorPlugin(plugin);

		Assert.That(() => services.AddSphere10BlazorPlugin(plugin), Throws.ArgumentException);
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(ApplicationPluginRegistration<IBlazorPlugin>)), Is.EqualTo(1));
	}

	[Test]
	public async Task CapturedContributionsKeepStartupOrderWhenSourceMembershipChanges() {
		var first = new BlazorApplicationBlockBuilder()
			.WithId("first")
			.WithName("First")
			.WithPosition(50)
			.WithDefaultScreen<FirstScreen>()
			.Build();
		var second = new BlazorApplicationBlockBuilder()
			.WithId("second")
			.WithName("Second")
			.WithPosition(-10)
			.WithDefaultScreen<SecondScreen>()
			.Build();
		var plugin = new MutablePlugin(new[] { first, second });
		var services = new ServiceCollection().AddSphere10BlazorPlugin(plugin);
		plugin.CurrentBlocks = Array.Empty<IBlazorApplicationBlock>();
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var scope = provider.CreateScope();
		var receipt = provider.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>().Single();
		receipt.Blocks[0] = null;
		var application = scope.ServiceProvider.GetRequiredService<IBlazorApplication>();
		await application.ScreenHost.InitializeAsync();

		Assert.That(receipt.Plugin, Is.SameAs(plugin));
		Assert.That(receipt.Blocks.Cast<IBlazorApplicationBlock>().Select(block => block.Id), Is.EqualTo(new[] { "first", "second" }));
		Assert.That(receipt.Blocks.First(), Is.Not.SameAs(first), "The receipt captures the validated post-load contribution snapshots.");
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(application.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "second", "first" }));
		Assert.That(application.ScreenHost.ActiveScreen.Block.Id, Is.EqualTo("first"));
		Assert.That(application.ScreenHost.ActiveScreen.ScreenType, Is.EqualTo(typeof(FirstScreen)));
	}

	public sealed class StartupMarker {
	}

	private sealed class MutablePlugin : BlazorPlugin {
		public MutablePlugin(IBlazorApplicationBlock[] blocks)
			: base("Mutable", blocks) {
			CurrentBlocks = blocks;
		}

		public IBlazorApplicationBlock[] CurrentBlocks { get; set; }

		public override IBlazorApplicationBlock[] Blocks {
			get => Tools.Array.Clone(CurrentBlocks);
			init => base.Blocks = value;
		}
	}

	public sealed class FirstScreen : ComponentBase, IBlazorApplicationScreen {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

	}

	public sealed class SecondScreen : ComponentBase, IBlazorApplicationScreen {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

	}
}
