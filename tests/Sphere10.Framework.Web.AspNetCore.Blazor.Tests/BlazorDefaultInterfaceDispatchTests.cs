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

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorDefaultInterfaceDispatchTests {
	[Test]
	public async Task ApplicationBlocksProjectLiveMembershipThroughSharedTypedAndClassViews() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = new BlazorApplicationBlockBuilder()
			.WithId("first")
			.WithName("First")
			.Build();
		var second = new BlazorApplicationBlockBuilder()
			.WithId("second")
			.WithName("Second")
			.Build();
		using var host = new BlazorApplicationScreenHost(new[] { first, second }, provider);
		Assert.That(await host.UnregisterBlockAsync(host.Blocks.Single(block => block.Id == "second")), Is.True);
		using var application = new BlazorApplication(host, Array.Empty<IBlazorPlugin>());
		IApplication neutral = application;
		IApplication<IBlazorPlugin, IBlazorApplicationScreenHost> typed = application;
		IBlazorApplication platform = application;
		ApplicationBase compatibility = application;

		Assert.That(neutral.Blocks.Single(), Is.SameAs(host.Blocks.Single()));
		Assert.That(typed.LoadedPlugins, Is.Empty);
		Assert.That(platform.Blocks.Single(), Is.SameAs(neutral.Blocks.Single()));
		await host.RegisterBlockAsync(host.AvailableBlocks.Single(block => block.Id == "second"));
		Assert.That(neutral.Blocks.Length, Is.EqualTo(2));
		Assert.That(compatibility.Blocks.Length, Is.EqualTo(2));
		var registeredFirst = host.Blocks.Single(block => block.Id == "first");
		Assert.That(await host.UnregisterBlockAsync(registeredFirst), Is.True);
		Assert.That(((IBlazorApplicationBlock)neutral.Blocks.Single()).Id, Is.EqualTo("second"));
		Assert.That(compatibility.Blocks.Single(), Is.SameAs(host.Blocks.Single()));
		Assert.That(await host.UnregisterBlockAsync(host.Blocks.Single()), Is.True);
		Assert.That(neutral.Blocks, Is.Empty);
		Assert.That(application.LoadedPlugins, Is.Empty);
	}

	[Test]
	public void DirectPlatformApplicationUsesInlineSharedProjections() {
		var plugin = new BlazorPluginBuilder()
			.WithName("Direct")
			.ConfigureBlock(block => block
				.WithId("direct")
				.WithName("Direct block")
			)
			.Build();
		using var provider = new ServiceCollection().AddSphere10BlazorPlugin(plugin).BuildServiceProvider();
		var application = new DirectApplication(provider.GetRequiredService<IBlazorApplicationScreenHost>(), plugin);
		IApplication neutral = application;
		IApplication<IBlazorPlugin, IBlazorApplicationScreenHost> typed = application;

		Assert.That(neutral.LoadedPlugins.Single(), Is.SameAs(application.LoadedPlugins.Single()));
		Assert.That(typed.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(neutral.Blocks.Single(), Is.SameAs(application.Blocks.Single()));
		Assert.That(neutral.ScreenHost, Is.SameAs(typed.ScreenHost));
	}

	[Test]
	public void BlazorPluginOverrideProjectsThroughTheSharedInterfaceAndExistingPluginClass() {
		var first = new BlazorApplicationBlockBuilder()
			.WithName("First")
			.Build();
		var second = new BlazorApplicationBlockBuilder()
			.WithName("Second")
			.Build();
		var plugin = new ProjectedPlugin { CurrentBlock = first };
		IApplicationPlugin neutral = plugin;
		ApplicationPlugin compatibility = plugin;

		Assert.That(neutral.Blocks.Single(), Is.SameAs(first));
		plugin.CurrentBlock = second;
		Assert.That(neutral.Blocks.Single(), Is.SameAs(second));
		Assert.That(compatibility.Blocks.Single(), Is.SameAs(second));
	}

	private sealed class DirectApplication : IBlazorApplication {
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

		public DirectApplication(IBlazorApplicationScreenHost screenHost, IBlazorPlugin plugin) {
			ScreenHost = screenHost;
			_plugin = plugin;
		}

		public ILogger Logger { get; } = new NoOpLogger();

		public Disposables Disposables { get; } = new();

		public bool IsInitialized => false;

		public bool IsExiting => false;

		public bool HasExited => false;

		public IBlazorPlugin[] LoadedPlugins => new[] { _plugin };

		public IBlazorApplicationBlock[] Blocks => ScreenHost.Blocks;

		public IBlazorApplicationScreenHost ScreenHost { get; }

		public bool HasUnsavedChanges => ScreenHost.HasUnsavedChanges;

		public IBlazorApplicationMenu[] Menus => Array.Empty<IBlazorApplicationMenu>();

		public IBlazorApplicationMenuItem[] ToolBarItems => Array.Empty<IBlazorApplicationMenuItem>();

		public void Initialize() {
		}

		public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

		public bool Exit(bool force = false, CancellationToken cancellationToken = default) => true;

		public Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default) => Task.FromResult(true);

		void IApplication.Finalize() {
		}

		public Task FinalizeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

		public void Dispose() {
		}

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	private sealed class ProjectedPlugin : BlazorPlugin {
		public ProjectedPlugin()
			: base("Projected", Array.Empty<IBlazorApplicationBlock>()) {
		}

		public IBlazorApplicationBlock CurrentBlock { get; set; }

		public override IBlazorApplicationBlock[] Blocks {
			get => new[] { CurrentBlock };
			init => base.Blocks = value;
		}
	}
}
