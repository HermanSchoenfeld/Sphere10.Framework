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
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorDefaultInterfaceDispatchTests {
	/// <summary>Verifies application blocks project live membership through shared typed and class views.</summary>
	[Test]
	public async Task ApplicationBlocksProjectLiveMembershipThroughSharedTypedAndClassViews() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = new ApplicationBlockBuilder()
			.WithId("first")
			.WithName("First")
			.Build();
		var second = new ApplicationBlockBuilder()
			.WithId("second")
			.WithName("Second")
			.Build();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { first, second }, provider);
		Assert.That(await host.UnregisterBlockAsync(host.Blocks.Single(block => block.Id == "second")), Is.True);
		using var application = new BlazorUI.Application(host, Array.Empty<IBlazorPlugin>());
		SharedUI.IApplication neutral = application;
		SharedUI.IApplication<IBlazorPlugin, BlazorUI.IApplicationScreenHost> typed = application;
		BlazorUI.IApplication platform = application;
		ApplicationBase compatibility = application;

		Assert.That(neutral.Blocks.Single(), Is.SameAs(host.Blocks.Single()));
		Assert.That(typed.LoadedPlugins, Is.Empty);
		Assert.That(platform.Blocks.Single(), Is.SameAs(neutral.Blocks.Single()));
		await host.RegisterBlockAsync(host.AvailableBlocks.Single(block => block.Id == "second"));
		Assert.That(neutral.Blocks.Length, Is.EqualTo(2));
		Assert.That(compatibility.Blocks.Length, Is.EqualTo(2));
		var registeredFirst = host.Blocks.Single(block => block.Id == "first");
		Assert.That(await host.UnregisterBlockAsync(registeredFirst), Is.True);
		Assert.That(((BlazorUI.IApplicationBlock)neutral.Blocks.Single()).Id, Is.EqualTo("second"));
		Assert.That(compatibility.Blocks.Single(), Is.SameAs(host.Blocks.Single()));
		Assert.That(await host.UnregisterBlockAsync(host.Blocks.Single()), Is.True);
		Assert.That(neutral.Blocks, Is.Empty);
		Assert.That(application.LoadedPlugins, Is.Empty);
	}

	/// <summary>Verifies direct platform application uses inline shared projections.</summary>
	[Test]
	public void DirectPlatformApplicationUsesInlineSharedProjections() {
		var plugin = new BlazorPluginBuilder()
			.WithName("Direct")
			.AddBlock(block => block
				.WithId("direct")
				.WithName("Direct block")
			)
			.Build();
		using var provider = new ServiceCollection()
			.AddSphere10BlazorPlugin(plugin)
			.BuildServiceProvider();
		var application = new DirectApplication(provider.GetRequiredService<BlazorUI.IApplicationScreenHost>(), plugin);
		SharedUI.IApplication neutral = application;
		SharedUI.IApplication<IBlazorPlugin, BlazorUI.IApplicationScreenHost> typed = application;

		Assert.That(neutral.LoadedPlugins.Single(), Is.SameAs(application.LoadedPlugins.Single()));
		Assert.That(typed.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(neutral.Blocks.Single(), Is.SameAs(application.Blocks.Single()));
		Assert.That(neutral.ScreenHost, Is.SameAs(typed.ScreenHost));
	}

	/// <summary>Verifies blazor plugin override projects through the shared interface and existing plugin class.</summary>
	[Test]
	public void BlazorPluginOverrideProjectsThroughTheSharedInterfaceAndExistingPluginClass() {
		var first = new ApplicationBlockBuilder()
			.WithName("First")
			.Build();
		var second = new ApplicationBlockBuilder()
			.WithName("Second")
			.Build();
		var plugin = new ProjectedPlugin { CurrentBlock = first };
		SharedUI.IApplicationPlugin neutral = plugin;
		SharedUI.ApplicationPlugin compatibility = plugin;

		Assert.That(neutral.Blocks.Single(), Is.SameAs(first));
		plugin.CurrentBlock = second;
		Assert.That(neutral.Blocks.Single(), Is.SameAs(second));
		Assert.That(compatibility.Blocks.Single(), Is.SameAs(second));
	}

	#region Inner Types

	private sealed class DirectApplication : BlazorUI.IApplication {
		/// <summary>The editor callback marking a completed input change.</summary>
		public event EventHandlerEx Changed {
			add { }
			remove { }
		}
		public event EventHandlerEx Initializing {
			add { }
			remove { }
		}
		public event EventHandlerEx Initialized {
			add { }
			remove { }
		}
		public event EventHandlerEx Finalizing {
			add { }
			remove { }
		}
		public event EventHandlerEx Finalized {
			add { }
			remove { }
		}
		public event ProgressHandler Progress {
			add { }
			remove { }
		}

		private readonly IBlazorPlugin _plugin;

		public DirectApplication(BlazorUI.IApplicationScreenHost screenHost, IBlazorPlugin plugin) {
			ScreenHost = screenHost;
			_plugin = plugin;
			Settings = null;
		}

		public ILogger Logger { get; } = new NoOpLogger();

		public Disposables Disposables { get; } = new();

		public bool IsInitialized => false;

		public bool IsExiting => false;

		public bool HasExited => false;

		public IBlazorPlugin[] LoadedPlugins => new[] { _plugin };

		public BlazorUI.IApplicationBlock[] Blocks => ScreenHost.Blocks;

		public BlazorUI.IApplicationScreenHost ScreenHost { get; }

		/// <summary>The actual registration-owned settings shared by both screen instances.</summary>
		public ISettingsProvider Settings { get; }

		public bool HasUnsavedChanges => ScreenHost.HasUnsavedChanges;

		public BlazorUI.IApplicationMenu[] Menus => Array.Empty<BlazorUI.IApplicationMenu>();

		public BlazorUI.IApplicationMenuItem[] ToolBarItems => Array.Empty<BlazorUI.IApplicationMenuItem>();

		public void Initialize() {
		}

		public Task InitializeAsync(CancellationToken cancellationToken = default) {
			return Task.CompletedTask;
		}

		public bool Exit(bool force = false, CancellationToken cancellationToken = default) {
			return true;
		}

		public Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default) {
			return Task.FromResult(true);
		}

		void SharedUI.IApplication.Finalize() {
		}

		public Task FinalizeAsync(CancellationToken cancellationToken = default) {
			return Task.CompletedTask;
		}

		public void Dispose() {
		}

		/// <summary>Releases the fixture-owned resources after pending asynchronous work completes.</summary>
		public ValueTask DisposeAsync() {
			return ValueTask.CompletedTask;
		}
	}

	private sealed class ProjectedPlugin : BlazorPlugin {
		public ProjectedPlugin()
			: base("Projected", Array.Empty<BlazorUI.IApplicationBlock>()) {
		}

		public BlazorUI.IApplicationBlock CurrentBlock { get; set; }

		/// <inheritdoc />
		public override BlazorUI.IApplicationBlock[] Blocks {
			get => new[] { CurrentBlock };
			init => base.Blocks = value;
		}
	}

	#endregion
}
