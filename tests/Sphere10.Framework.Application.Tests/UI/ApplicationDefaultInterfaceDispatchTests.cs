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
public class ApplicationDefaultInterfaceDispatchTests {
	[Test]
	public void TypedPluginOverridesRemainLiveThroughBothInterfacesAndTheExistingBaseClass() {
		var first = new TestApplicationBlock { Name = "First" };
		var second = new TestApplicationBlock { Name = "Second" };
		var plugin = new DynamicPlugin { CurrentBlock = first };
		IApplicationPlugin<TestApplicationBlock> typed = plugin;
		IApplicationPlugin neutral = plugin;
		ApplicationPlugin compatibility = plugin;

		Assert.That(typed.Blocks.Single(), Is.SameAs(first));
		Assert.That(neutral.Blocks.Single(), Is.SameAs(first));
		Assert.That(compatibility.Blocks.Single(), Is.SameAs(first));
		neutral.Blocks[0] = null;
		plugin.CurrentBlock = second;

		Assert.That(typed.Blocks.Single(), Is.SameAs(second));
		Assert.That(neutral.Blocks.Single(), Is.SameAs(second));
		Assert.That(compatibility.Blocks.Single(), Is.SameAs(second));
	}

	[Test]
	public async Task NeutralHostUsesTheTypedDecoratorOverridesForPropertiesAndAsyncReturnValues() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock("Workspace", new(typeof(ProbeScreen)));
		var projectedBlock = new TestApplicationBlock { Name = "Projected" };
		using var host = new ProbeHost(block, provider);
		using var decorator = new ProjectedHostDecorator(host, projectedBlock);
		IApplicationScreenHost neutral = decorator;
		IApplicationScreenHost<TestApplicationBlock, ProbeScreen> typed = decorator;

		var screen = await neutral.OpenScreenAsync(block, block.DefaultScreenDefinition);

		Assert.That(decorator.OpenCalls, Is.EqualTo(1));
		Assert.That(host.OpenCalls, Is.EqualTo(1));
		Assert.That(screen, Is.SameAs(typed.ActiveScreen));
		Assert.That(neutral.ActiveBlock, Is.SameAs(projectedBlock));
		Assert.That(neutral.Blocks.Single(), Is.SameAs(projectedBlock));
		Assert.That(neutral.ActiveScreen, Is.SameAs(screen));
		Assert.That(neutral.GetScreenBlock(screen), Is.SameAs(block));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task NeutralReusableHostsKeepClassDispatchWhenSubclassesRemapTypedInterfaces(bool decorate) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock("Workspace", new(typeof(ProbeScreen)));
		var remappedBlock = new TestApplicationBlock { Name = "Remapped" };
		var remappedScreen = new ProbeScreen();
		using var host = decorate
			? new ProbeHost(block, provider)
			: new RemappedHost(block, provider, remappedBlock, remappedScreen);
		using IApplicationScreenHost neutral = decorate
			? new RemappedHostDecorator(host, remappedBlock, remappedScreen)
			: host;
		IApplicationScreenHost<TestApplicationBlock, ProbeScreen> typed = (IApplicationScreenHost<TestApplicationBlock, ProbeScreen>)neutral;
		IMultiApplicationScreenHost neutralMulti = (IMultiApplicationScreenHost)neutral;
		IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen> typedMulti = (IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>)neutral;

		var screen = await neutral.OpenScreenAsync(block, block.DefaultScreenDefinition);

		Assert.That(host.OpenCalls, Is.EqualTo(1));
		Assert.That(neutral.AvailableBlocks.Single(), Is.SameAs(block));
		Assert.That(neutral.Blocks.Single(), Is.SameAs(block));
		Assert.That(neutral.ActiveBlock, Is.SameAs(block));
		Assert.That(neutral.ActiveScreen, Is.SameAs(screen));
		Assert.That(neutral.GetScreenBlock(screen), Is.SameAs(block));
		Assert.That(neutralMulti.Screens.Single(), Is.SameAs(screen));
		Assert.That(neutralMulti.OpenScreens.Single(), Is.SameAs(screen));
		Assert.That(await neutralMulti.ShowScreenAsync(screen), Is.True);
		Assert.That(typed.Blocks.Single(), Is.SameAs(remappedBlock));
		Assert.That(typed.ActiveScreen, Is.SameAs(remappedScreen));
		Assert.That(typed.GetScreenBlock((ProbeScreen)screen), Is.SameAs(remappedBlock));
		Assert.That(await typed.OpenScreenAsync(block, block.DefaultScreenDefinition), Is.SameAs(remappedScreen));
		Assert.That(typedMulti.Screens.Single(), Is.SameAs(remappedScreen));
		Assert.That(await typedMulti.ShowScreenAsync((ProbeScreen)screen), Is.False);
		Assert.That(host.OpenCalls, Is.EqualTo(1), "An explicit typed remapping must not replace the neutral class mapping.");
	}

	[Test]
	public void NeutralHostRejectsMismatchedBlockAndScreenTypesBeforeCallingTypedOverrides() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Workspace" };
		using var host = new ProbeHost(block, provider);
		using var decorator = new ProjectedHostDecorator(host, block);
		IApplicationScreenHost neutral = decorator;

		Assert.That(() => neutral.SelectBlockAsync(new OtherBlock()), Throws.ArgumentException);
		Assert.That(() => neutral.GetScreenBlock(new OtherScreen()), Throws.ArgumentException);
		Assert.That(decorator.SelectionCalls, Is.Zero);
		Assert.That(decorator.ScreenBlockCalls, Is.Zero);
		Assert.That(neutral.GetScreenBlock(null), Is.Null);
	}

	private sealed class DynamicPlugin : ApplicationPluginBase<TestApplicationBlock> {
		public DynamicPlugin()
			: base("Dynamic", Array.Empty<TestApplicationBlock>()) {
		}

		public TestApplicationBlock CurrentBlock { get; set; }

		public override TestApplicationBlock[] Blocks {
			get => new[] { CurrentBlock };
			init => base.Blocks = value;
		}
	}

	private class ProbeHost : ApplicationScreenHost<TestApplicationBlock, ProbeScreen> {
		public ProbeHost(TestApplicationBlock block, IServiceProvider services)
			: base(new[] { block }, services) {
		}

		public int OpenCalls { get; private set; }

		public override Task<ProbeScreen> OpenScreenAsync(TestApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) {
			OpenCalls++;
			return base.OpenScreenAsync(block, definition, cancellationToken);
		}

		protected override ProbeScreen CreateScreen(TestApplicationBlock block, ApplicationScreenDefinition definition) => new();
	}

	private sealed class RemappedHost : ProbeHost, IApplicationScreenHost<TestApplicationBlock, ProbeScreen>, IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen> {
		private readonly TestApplicationBlock _block;
		private readonly ProbeScreen _screen;

		public RemappedHost(TestApplicationBlock block, IServiceProvider services, TestApplicationBlock remappedBlock, ProbeScreen remappedScreen)
			: base(block, services) {
			_block = remappedBlock;
			_screen = remappedScreen;
		}

		TestApplicationBlock[] IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.AvailableBlocks => new[] { _block };

		TestApplicationBlock[] IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.Blocks => new[] { _block };

		TestApplicationBlock IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ActiveBlock => _block;

		ProbeScreen IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ActiveScreen => _screen;

		TestApplicationBlock IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.GetScreenBlock(ProbeScreen screen) => _block;

		Task<ProbeScreen> IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.OpenScreenAsync(TestApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken) => Task.FromResult(_screen);

		ProbeScreen[] IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.Screens => new[] { _screen };

		ProbeScreen[] IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.OpenScreens => new[] { _screen };

		Task<bool> IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ShowScreenAsync(ProbeScreen screen, CancellationToken cancellationToken) => Task.FromResult(false);
	}

	private sealed class RemappedHostDecorator : MultiApplicationScreenHostDecorator<TestApplicationBlock, ProbeScreen>, IApplicationScreenHost<TestApplicationBlock, ProbeScreen>, IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen> {
		private readonly TestApplicationBlock _block;
		private readonly ProbeScreen _screen;

		public RemappedHostDecorator(IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen> host, TestApplicationBlock remappedBlock, ProbeScreen remappedScreen)
			: base(host) {
			_block = remappedBlock;
			_screen = remappedScreen;
		}

		TestApplicationBlock[] IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.AvailableBlocks => new[] { _block };

		TestApplicationBlock[] IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.Blocks => new[] { _block };

		TestApplicationBlock IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ActiveBlock => _block;

		ProbeScreen IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ActiveScreen => _screen;

		TestApplicationBlock IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.GetScreenBlock(ProbeScreen screen) => _block;

		Task<ProbeScreen> IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.OpenScreenAsync(TestApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken) => Task.FromResult(_screen);

		ProbeScreen[] IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.Screens => new[] { _screen };

		ProbeScreen[] IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.OpenScreens => new[] { _screen };

		Task<bool> IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ShowScreenAsync(ProbeScreen screen, CancellationToken cancellationToken) => Task.FromResult(false);
	}

	private sealed class ProjectedHostDecorator : ApplicationScreenHostDecorator<TestApplicationBlock, ProbeScreen> {
		private readonly TestApplicationBlock _projectedBlock;

		public ProjectedHostDecorator(IApplicationScreenHost<TestApplicationBlock, ProbeScreen> host, TestApplicationBlock projectedBlock)
			: base(host) {
			_projectedBlock = projectedBlock;
		}

		public override TestApplicationBlock[] Blocks => new[] { _projectedBlock };

		public override TestApplicationBlock ActiveBlock => _projectedBlock;

		public int OpenCalls { get; private set; }

		public int SelectionCalls { get; private set; }

		public int ScreenBlockCalls { get; private set; }

		public override Task SelectBlockAsync(TestApplicationBlock block, CancellationToken cancellationToken = default) {
			SelectionCalls++;
			return base.SelectBlockAsync(block, cancellationToken);
		}

		public override TestApplicationBlock GetScreenBlock(ProbeScreen screen) {
			ScreenBlockCalls++;
			return base.GetScreenBlock(screen);
		}

		public override Task<ProbeScreen> OpenScreenAsync(TestApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) {
			OpenCalls++;
			return base.OpenScreenAsync(block, definition, cancellationToken);
		}
	}

	private sealed class ProbeScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	private sealed class OtherScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	private sealed class OtherBlock : IApplicationBlock {
		public IApplication Parent { get; set; }
		public IApplicationPlugin Source { get; set; }
		public string Name => "Other";
		public int Position => 0;
		public IApplicationMenu[] Menus => Array.Empty<IApplicationMenu>();
		public ApplicationScreenDefinition[] ScreenDefinitions => Array.Empty<ApplicationScreenDefinition>();
		public ApplicationScreenDefinition DefaultScreenDefinition => null;
	}
}
