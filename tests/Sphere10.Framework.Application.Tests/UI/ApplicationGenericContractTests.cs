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
public class ApplicationGenericContractTests {
	[Test]
	public async Task ExplicitTypedImplementationsProjectTheSameMetadataAndHostThroughNeutralContracts() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var item = new TestApplicationMenuItem { Title = "Work" };
		var menu = new ExplicitMenu(item);
		var definition = new ApplicationScreenDefinition(typeof(ProbeScreen));
		var block = new ExplicitBlock(menu, item, definition);
		var plugin = new ExplicitPlugin(block);
		using var host = new ApplicationScreenHost<ExplicitBlock, ProbeScreen>(new[] { block }, provider);
		using IApplication application = new ExplicitApplication(plugin, host);
		var screen = await host.OpenScreenAsync(block, definition);

		Assert.That(application.LoadedPlugins, Is.EqualTo(new[] { plugin }));
		Assert.That(application.Blocks, Is.EqualTo(new[] { block }));
		Assert.That(application.ScreenHost, Is.SameAs(host));
		Assert.That(application.ScreenHost.Blocks, Is.EqualTo(new[] { block }));
		Assert.That(application.ScreenHost.ActiveBlock, Is.SameAs(block));
		Assert.That(application.ScreenHost.ActiveScreen, Is.SameAs(screen));
		Assert.That(screen.Host, Is.SameAs(host));
		Assert.That(screen.Block, Is.SameAs(block));
		Assert.That(((IApplicationPlugin)plugin).Blocks, Is.EqualTo(new[] { block }));
		Assert.That(((IApplicationBlock)block).Menus, Is.EqualTo(new[] { menu }));
		Assert.That(((IApplicationCommandProvider)block).Menus, Is.EqualTo(new[] { menu }));
		Assert.That(((IApplicationCommandProvider)block).ToolBarItems, Is.EqualTo(new[] { item }));
		Assert.That(((IApplicationMenu)menu).Items, Is.EqualTo(new[] { item }));
		Assert.That(((IApplicationMenu)menu).Parent, Is.SameAs(block));
		Assert.That(((IApplicationMenuItem)item).Parent, Is.SameAs(menu));
		IApplication<ExplicitPlugin, ApplicationScreenHost<ExplicitBlock, ProbeScreen>> typed = (ExplicitApplication)application;
		typed.LoadedPlugins[0] = null;
		application.Blocks[0] = null;
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(typed.ScreenHost, Is.SameAs(application.ScreenHost));
		Assert.That(application.Blocks.Single(), Is.SameAs(block));
		await host.UnregisterBlockAsync(block);
		Assert.That(application.Blocks, Is.Empty);
		Assert.That(application.LoadedPlugins.Single(), Is.SameAs(plugin));
		Assert.That(screen.Host, Is.Null);
		Assert.That(screen.Block, Is.Null);
	}

	[Test]
	public async Task TypedHostDecoratorDispatchesOverridesAndForwardsRemovableNotificationsThroughEitherInterface() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var first = new TestApplicationBlock("First", new(typeof(ProbeScreen)));
		var second = new TestApplicationBlock { Name = "Second" };
		using var implementation = new TrackingHost(new[] { first, second }, provider);
		using var decorator = new TrackingHostDecorator(implementation);
		IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen> typed = decorator;
		IMultiApplicationScreenHost neutral = decorator;
		var definition = Tools.UI.GetScreenDefinitions(new[] { first }).Single();
		var notifications = 0;
		EventHandlerEx handler = () => notifications++;
		neutral.Changed += handler;

		var selected = await typed.OpenScreenAsync(first, definition);
		Assert.That(selected, Is.SameAs(implementation.ActiveScreen));
		Assert.That(decorator.Activations, Is.EqualTo(1));
		Assert.That(implementation.Activations, Is.EqualTo(1));
		Assert.That(notifications, Is.GreaterThan(0));
		Assert.That(neutral.Blocks, Is.EqualTo(typed.Blocks));
		Assert.That(neutral.AvailableBlocks, Is.EqualTo(typed.AvailableBlocks));
		Assert.That(neutral.ActiveBlock, Is.SameAs(typed.ActiveBlock));
		Assert.That(neutral.Screens.Single(), Is.SameAs(typed.Screens.Single()));
		await neutral.SelectBlockAsync(second);
		var beforeRemoval = notifications;
		typed.Changed -= handler;

		await neutral.OpenScreenAsync(first, definition);

		Assert.That(decorator.Activations, Is.EqualTo(2));
		Assert.That(implementation.Activations, Is.EqualTo(2));
		Assert.That(notifications, Is.EqualTo(beforeRemoval));
		Assert.That(neutral.ActiveBlock, Is.SameAs(first));
	}

	private sealed class ExplicitMenu : IApplicationMenu<TestApplicationMenuItem> {
		private readonly TestApplicationMenuItem _item;

		public ExplicitMenu(TestApplicationMenuItem item) {
			_item = item;
			item.Parent = this;
		}

		public IApplicationBlock Parent { get; set; }

		public string Text => "Menu";
		TestApplicationMenuItem[] IApplicationMenu<TestApplicationMenuItem>.Items => new[] { _item };
	}

	private sealed class ExplicitBlock : IApplicationBlock<ExplicitMenu, TestApplicationMenuItem> {
		private readonly ExplicitMenu _menu;
		private readonly TestApplicationMenuItem _item;
		private readonly ApplicationScreenDefinition _definition;

		public IApplication Parent { get; set; }

		public IApplicationPlugin Source { get; set; }

		public ExplicitBlock(ExplicitMenu menu, TestApplicationMenuItem item, ApplicationScreenDefinition definition = null) {
			_menu = menu;
			_item = item;
			_definition = definition;
			menu.Parent = this;
		}

		public string Name => "Block";
		public int Position => 0;
		public ApplicationScreenDefinition[] ScreenDefinitions => _definition == null ? Array.Empty<ApplicationScreenDefinition>() : new[] { _definition };

		public ApplicationScreenDefinition DefaultScreenDefinition => _definition;
		ExplicitMenu[] IApplicationBlock<ExplicitMenu, TestApplicationMenuItem>.Menus => new[] { _menu };
		ExplicitMenu[] IApplicationCommandProvider<ExplicitMenu, TestApplicationMenuItem>.Menus => new[] { _menu };
		TestApplicationMenuItem[] IApplicationCommandProvider<ExplicitMenu, TestApplicationMenuItem>.ToolBarItems => new[] { _item };
	}

	private sealed class ExplicitPlugin(ExplicitBlock block) : IApplicationPlugin<ExplicitBlock> {
		public event EventHandlerEx Loaded { add { } remove { } }
		public event EventHandlerEx Unloaded { add { } remove { } }
		public string Name => "Plugin";
		ExplicitBlock[] IApplicationPlugin<ExplicitBlock>.Blocks => new[] { block };
		public void Load(IServiceCollection services) { }
		public void Unload() { }
	}

	private sealed class ExplicitApplication(ExplicitPlugin plugin, ApplicationScreenHost<ExplicitBlock, ProbeScreen> host)
		: IApplication<ExplicitPlugin, ApplicationScreenHost<ExplicitBlock, ProbeScreen>> {
		public event EventHandlerEx Changed { add { } remove { } }
		public event EventHandlerEx Initializing { add { } remove { } }
		public event EventHandlerEx Initialized { add { } remove { } }
		public event EventHandlerEx Finalizing { add { } remove { } }
		public event EventHandlerEx Finalized { add { } remove { } }
		public event ProgressHandler Progress { add { } remove { } }
		public ILogger Logger => SystemLog.Logger;
		public Disposables Disposables { get; } = new();
		public bool IsInitialized => false;
		public bool IsExiting => false;
		public bool HasExited { get; private set; }
		ExplicitPlugin[] IApplication<ExplicitPlugin, ApplicationScreenHost<ExplicitBlock, ProbeScreen>>.LoadedPlugins => new[] { plugin };
		ApplicationScreenHost<ExplicitBlock, ProbeScreen> IApplication<ExplicitPlugin, ApplicationScreenHost<ExplicitBlock, ProbeScreen>>.ScreenHost => host;
		public IApplicationBlock[] Blocks => host.Blocks;
		public bool HasUnsavedChanges => host.HasUnsavedChanges;
		public void Initialize() { }
		public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
		public bool Exit(bool force = false, CancellationToken cancellationToken = default) => ExitAsync(force, cancellationToken).GetAwaiter().GetResult();
		public Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			HasExited = true;
			return Task.FromResult(true);
		}
#pragma warning disable CS0465
		public void Finalize() { }
#pragma warning restore CS0465
		public Task FinalizeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
		public void Dispose() { }
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	private sealed class TrackingHost(TestApplicationBlock[] blocks, IServiceProvider provider) : ApplicationScreenHost<TestApplicationBlock, ProbeScreen>(blocks, provider) {
		public int Activations { get; private set; }

		public override Task<ProbeScreen> OpenScreenAsync(TestApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) {
			Activations++;
			return base.OpenScreenAsync(block, definition, cancellationToken);
		}
	}

	private sealed class TrackingHostDecorator(IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen> host) : MultiApplicationScreenHostDecorator<TestApplicationBlock, ProbeScreen>(host) {
		public int Activations { get; private set; }

		public override Task<ProbeScreen> OpenScreenAsync(TestApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) {
			Activations++;
			return base.OpenScreenAsync(block, definition, cancellationToken);
		}
	}

	private sealed class ProbeScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}
}
