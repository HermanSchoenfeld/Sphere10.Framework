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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationOwnerSettingsTests {
	/// <summary>Verifies block and plugin changes refresh ascreen without own settings and remain circuit local.</summary>
	[Test]
	public async Task BlockAndPluginChangesRefreshAScreenWithoutOwnSettingsAndRemainCircuitLocal() {
		var plugin = new BlazorPluginBuilder()
			.WithName("Owner settings")
			.UseSettings(new PluginPreferences { Number = 5 })
			.AddBlock(block => block
				.WithId("workspace")
				.WithName("Workspace")
				.UseSettings(new BlockPreferences { Number = 7 })
				.AddScreen<OwnerScreen>(screen => screen.WithStableKey("owner-screen"))
			)
			.Build();
		var startupLoads = 0;
		plugin.Loaded += () => startupLoads++;
		var services = new ServiceCollection().AddLogging();
		services.AddSingleton<ISettingsProvider, MemorySettingsProvider>();
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/application"));
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		services.AddSphere10BlazorPlugin(plugin);
		await using var provider = services.BuildServiceProvider();
		await using var firstScope = provider.CreateAsyncScope();
		await using var secondScope = provider.CreateAsyncScope();
		var first = firstScope.ServiceProvider.GetRequiredService<IBlazorApplication>();
		var second = secondScope.ServiceProvider.GetRequiredService<IBlazorApplication>();
		await first.InitializeAsync();
		await second.InitializeAsync();
		await using var renderer = new HtmlRenderer(firstScope.ServiceProvider, firstScope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var session = await first.ScreenHost.ActivateBlockAsync("workspace");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			Assert.That(session.Definition.Settings, Is.Null);
			Assert.That(rendered.ToHtmlString(), Does.Contain("block=7;plugin=5"));

			((BlockPreferences)session.Block.BlockSettings).Number = 23;
			await session.Block.BlockSettings.SaveAsync();
			Assert.That(rendered.ToHtmlString(), Does.Contain("block=23;plugin=5"));
			((PluginPreferences)session.Block.Source.PluginSettings).Number = 31;
			await session.Block.Source.PluginSettings.SaveAsync();
			Assert.That(rendered.ToHtmlString(), Does.Contain("block=23;plugin=31"));
			Assert.That(session.Screen, Is.TypeOf<OwnerScreen>(), "Applying owner settings must retain the existing screen.");
		});

		Assert.That(second.LoadedPlugins.Single(), Is.Not.SameAs(first.LoadedPlugins.Single()));
		Assert.That(((PluginPreferences)second.LoadedPlugins.Single().PluginSettings).Number, Is.EqualTo(5));
		Assert.That(((BlockPreferences)second.Blocks.Single().BlockSettings).Number, Is.EqualTo(7));
		Assert.That(startupLoads, Is.EqualTo(1), "Runtime circuit owners do not reload the startup plugin.");
		Assert.That(((PluginPreferences)plugin.PluginSettings).Number, Is.EqualTo(5), "Startup declarations must not become one user's live values.");
	}

	#region Inner Types

	/// <summary>Reads actual block and plugin settings without declaring screen settings of its own.</summary>
	public class OwnerScreen : BlazorApplicationScreen {
		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.AddContent(0, $"block={GetBlockSettings<BlockPreferences>().Number};plugin={GetPluginSettings<PluginPreferences>().Number}");
		}
	}

	/// <summary>Declares the block-owned refresh value used by the renderer.</summary>
	[UserEditable]
	public class BlockPreferences : SettingsObject {
		/// <summary>The refresh count used by the fixture.</summary>
		public int Number { get; set; }
	}

	/// <summary>Declares the plugin-owned refresh value used by the renderer.</summary>
	[UserEditable]
	public class PluginPreferences : SettingsObject {
		/// <summary>The refresh count used by the fixture.</summary>
		public int Number { get; set; }
	}

	// A successful store boundary isolates runtime ownership from browser/authentication tests covered elsewhere.
	private sealed class MemorySettingsProvider : SyncSettingsProviderBase {
		// Keeps this fixture storage readable for its assertions.
		public override bool EncryptSettings {
			get {
				return false;
			}
		}

		// Check whether this fixture has a persisted record.
		public override bool ContainsSetting(Type settingsType, object id = null) {
			return false;
		}

		// Read fixture storage without bypassing the base provider lifecycle.
		protected override SettingsObject LoadInternal(Type settingsType, object id = null) {
			throw new InvalidOperationException("No test preferences have been saved.");
		}

		// Persist fixture values after the base provider prepares them.
		protected override void SaveInternal(SettingsObject settings) {
		}

		// Remove this fixture record.
		public override void DeleteSetting(SettingsObject settings) {
		}

		// Clear this fixture storage.
		public override void ClearSettings() {
		}
	}

	#endregion
}
