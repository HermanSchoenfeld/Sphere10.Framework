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
	/// <summary>Verifies application blocks project live membership through shared typed and class views.</summary>
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
		var application = new DirectApplication(provider.GetRequiredService<IBlazorApplicationScreenHost>(), plugin);
		IApplication neutral = application;
		IApplication<IBlazorPlugin, IBlazorApplicationScreenHost> typed = application;

		Assert.That(neutral.LoadedPlugins.Single(), Is.SameAs(application.LoadedPlugins.Single()));
		Assert.That(typed.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(neutral.Blocks.Single(), Is.SameAs(application.Blocks.Single()));
		Assert.That(neutral.ScreenHost, Is.SameAs(typed.ScreenHost));
	}

	/// <summary>Verifies blazor plugin override projects through the shared interface and existing plugin class.</summary>
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

	#region Inner Types

	private sealed class DirectApplication : IBlazorApplication {
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

		public DirectApplication(IBlazorApplicationScreenHost screenHost, IBlazorPlugin plugin) {
			ScreenHost = screenHost;
			_plugin = plugin;
			Settings = null;
		}

		public ILogger Logger { get; } = new NoOpLogger();

		public Disposables Disposables { get; } = new();

		public bool IsInitialized {
			get {
				return false;
			}
		}

		public bool IsExiting {
			get {
				return false;
			}
		}

		public bool HasExited {
			get {
				return false;
			}
		}

		public IBlazorPlugin[] LoadedPlugins {
			get {
				return new[] { _plugin };
			}
		}

		public IBlazorApplicationBlock[] Blocks {
			get {
				return ScreenHost.Blocks;
			}
		}

		public IBlazorApplicationScreenHost ScreenHost { get; }

		/// <summary>The actual registration-owned settings shared by both screen instances.</summary>
		public ISettingsProvider Settings { get; }

		public bool HasUnsavedChanges {
			get {
				return ScreenHost.HasUnsavedChanges;
			}
		}

		public IBlazorApplicationMenu[] Menus {
			get {
				return Array.Empty<IBlazorApplicationMenu>();
			}
		}

		public IBlazorApplicationMenuItem[] ToolBarItems {
			get {
				return Array.Empty<IBlazorApplicationMenuItem>();
			}
		}

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

		void IApplication.Finalize() {
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
			: base("Projected", Array.Empty<IBlazorApplicationBlock>()) {
		}

		public IBlazorApplicationBlock CurrentBlock { get; set; }

		/// <inheritdoc />
		public override IBlazorApplicationBlock[] Blocks {
			get {
				return new[] { CurrentBlock };
			}
			init {
				base.Blocks = value;
			}
		}
	}

	#endregion
}
