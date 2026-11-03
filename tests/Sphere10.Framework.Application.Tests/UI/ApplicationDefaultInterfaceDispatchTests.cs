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

/// <summary>Exercises shared application default interface dispatch tests.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationDefaultInterfaceDispatchTests {
	/// <summary>Verifies typed plugin overrides remain live through both interfaces and the existing base class.</summary>
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

	/// <summary>Verifies neutral host uses the typed decorator overrides for properties and async return values.</summary>
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
	}

	/// <summary>Verifies detached presentation keeps the typed override and the original pending task through neutral decoration.</summary>
	[Test]
	public async Task DetachedPresentationKeepsTheTypedOverrideAndTheOriginalPendingTaskThroughNeutralDecoration() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Workspace" };
		using var host = new ProbeHost(block, provider);
		using var typedDecorator = new ProjectedHostDecorator(host, block);
		using var neutral = new NeutralHostDecorator(typedDecorator);
		using var cancellation = new CancellationTokenSource();
		var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		host.UndockedPresentation = completion.Task;
		var screen = new ProbeScreen();
		var pending = neutral.ShowScreenUndockedAsync(screen, cancellation.Token);
		Assert.That(pending, Is.SameAs(completion.Task));
		Assert.That(pending.IsCompleted, Is.False);
		Assert.That(typedDecorator.UndockedCalls, Is.EqualTo(1));
		Assert.That(host.UndockedCalls, Is.EqualTo(1));
		Assert.That(host.LastUndockedScreen, Is.SameAs(screen));
		Assert.That(host.LastUndockedToken, Is.EqualTo(cancellation.Token));
		completion.SetResult(false);
		Assert.That(await pending, Is.False);
	}

	/// <summary>Verifies neutral reusable hosts keep class dispatch when subclasses remap typed interfaces.</summary>
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
		Assert.That(neutralMulti.Screens.Single(), Is.SameAs(screen));
		Assert.That(neutralMulti.OpenScreens.Single(), Is.SameAs(screen));
		Assert.That(await neutralMulti.ShowScreenAsync(screen), Is.True);
		Assert.That(await neutral.ShowScreenUndockedAsync(screen), Is.True);
		Assert.That(typed.Blocks.Single(), Is.SameAs(remappedBlock));
		Assert.That(typed.ActiveScreen, Is.SameAs(remappedScreen));
		Assert.That(await typed.OpenScreenAsync(block, block.DefaultScreenDefinition), Is.SameAs(remappedScreen));
		Assert.That(typedMulti.Screens.Single(), Is.SameAs(remappedScreen));
		Assert.That(await typedMulti.ShowScreenAsync((ProbeScreen)screen), Is.False);
		Assert.That(await typed.ShowScreenUndockedAsync((ProbeScreen)screen), Is.False);
		Assert.That(host.UndockedCalls, Is.EqualTo(1), "The neutral detached operation must use the virtual class member, not an explicit typed remapping.");
		Assert.That(host.OpenCalls, Is.EqualTo(1), "An explicit typed remapping must not replace the neutral class mapping.");
	}

	/// <summary>Verifies neutral host rejects mismatched block and screen types before calling typed overrides.</summary>
	[Test]
	public void NeutralHostRejectsMismatchedBlockAndScreenTypesBeforeCallingTypedOverrides() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock { Name = "Workspace" };
		using var host = new ProbeHost(block, provider);
		using var decorator = new ProjectedHostDecorator(host, block);
		IApplicationScreenHost neutral = decorator;

		Assert.That(() => neutral.SelectBlockAsync(new OtherBlock()), Throws.ArgumentException);
		Assert.That(() => neutral.ShowScreenUndockedAsync(new OtherScreen()), Throws.ArgumentException);
		Assert.That(decorator.SelectionCalls, Is.Zero);
		Assert.That(decorator.UndockedCalls, Is.Zero);
	}

	#region Inner Types

	/// <summary>Supplies dynamic plugin for the fixture's existing contract checks.</summary>
	private sealed class DynamicPlugin : ApplicationPluginBase<TestApplicationBlock> {
		/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
		public DynamicPlugin()
			: base("Dynamic", Array.Empty<TestApplicationBlock>()) {
		}

		/// <inheritdoc />
		public TestApplicationBlock CurrentBlock { get; set; }

		/// <inheritdoc />
		public override TestApplicationBlock[] Blocks {
			get => new[] { CurrentBlock };
			init => base.Blocks = value;
		}
	}

	/// <summary>Supplies probe host for the fixture's existing contract checks.</summary>
	private class ProbeHost : ApplicationScreenHost<TestApplicationBlock, ProbeScreen> {
		/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
		public ProbeHost(TestApplicationBlock block, IServiceProvider services)
			: base(new[] { block }, services) {
		}

		/// <inheritdoc />
		public int OpenCalls { get; private set; }

		/// <inheritdoc />
		public int UndockedCalls { get; private set; }

		/// <inheritdoc />
		public Task<bool> UndockedPresentation { get; set; } = Task.FromResult(true);

		/// <inheritdoc />
		public ProbeScreen LastUndockedScreen { get; private set; }

		/// <inheritdoc />
		public CancellationToken LastUndockedToken { get; private set; }

		/// <inheritdoc />
		public override Task<bool> ShowScreenUndockedAsync(ProbeScreen screen, CancellationToken cancellationToken = default) {
			UndockedCalls++;
			LastUndockedScreen = screen;
			LastUndockedToken = cancellationToken;
			return UndockedPresentation;
		}

		/// <inheritdoc />
		public override Task<ProbeScreen> OpenScreenAsync(TestApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) {
			OpenCalls++;
			return base.OpenScreenAsync(block, definition, cancellationToken);
		}

		/// <inheritdoc />
		protected override ProbeScreen CreateScreen(TestApplicationBlock block, ApplicationScreenDefinition definition) {
			return new();
		}
	}

	/// <summary>Supplies remapped host for the fixture's existing contract checks.</summary>
	private sealed class RemappedHost : ProbeHost, IApplicationScreenHost<TestApplicationBlock, ProbeScreen>, IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen> {
		private readonly TestApplicationBlock _block;
		private readonly ProbeScreen _screen;

		/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
		public RemappedHost(TestApplicationBlock block, IServiceProvider services, TestApplicationBlock remappedBlock, ProbeScreen remappedScreen)
			: base(block, services) {
			_block = remappedBlock;
			_screen = remappedScreen;
		}

		/// <inheritdoc />
		TestApplicationBlock[] IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.AvailableBlocks => new[] { _block };

		/// <inheritdoc />
		TestApplicationBlock[] IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.Blocks => new[] { _block };

		/// <inheritdoc />
		TestApplicationBlock IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ActiveBlock => _block;

		/// <inheritdoc />
		ProbeScreen IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ActiveScreen => _screen;

		/// <inheritdoc />
		Task<ProbeScreen> IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.OpenScreenAsync(TestApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken) {
			return Task.FromResult(_screen);
		}

		/// <inheritdoc />
		Task<bool> IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ShowScreenUndockedAsync(ProbeScreen screen, CancellationToken cancellationToken) {
			return Task.FromResult(false);
		}

		/// <inheritdoc />
		ProbeScreen[] IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.Screens => new[] { _screen };

		/// <inheritdoc />
		ProbeScreen[] IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.OpenScreens => new[] { _screen };

		/// <inheritdoc />
		Task<bool> IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ShowScreenAsync(ProbeScreen screen, CancellationToken cancellationToken) {
			return Task.FromResult(false);
		}
	}

	/// <summary>Supplies remapped host decorator for the fixture's existing contract checks.</summary>
	private sealed class RemappedHostDecorator : MultiApplicationScreenHostDecorator<TestApplicationBlock, ProbeScreen>, IApplicationScreenHost<TestApplicationBlock, ProbeScreen>, IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen> {
		private readonly TestApplicationBlock _block;
		private readonly ProbeScreen _screen;

		/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
		public RemappedHostDecorator(IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen> host, TestApplicationBlock remappedBlock, ProbeScreen remappedScreen)
			: base(host) {
			_block = remappedBlock;
			_screen = remappedScreen;
		}

		/// <inheritdoc />
		TestApplicationBlock[] IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.AvailableBlocks => new[] { _block };

		/// <inheritdoc />
		TestApplicationBlock[] IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.Blocks => new[] { _block };

		/// <inheritdoc />
		TestApplicationBlock IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ActiveBlock => _block;

		/// <inheritdoc />
		ProbeScreen IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ActiveScreen => _screen;

		/// <inheritdoc />
		Task<ProbeScreen> IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.OpenScreenAsync(TestApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken) {
			return Task.FromResult(_screen);
		}

		/// <inheritdoc />
		Task<bool> IApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ShowScreenUndockedAsync(ProbeScreen screen, CancellationToken cancellationToken) {
			return Task.FromResult(false);
		}

		/// <inheritdoc />
		ProbeScreen[] IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.Screens => new[] { _screen };

		/// <inheritdoc />
		ProbeScreen[] IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.OpenScreens => new[] { _screen };

		/// <inheritdoc />
		Task<bool> IMultiApplicationScreenHost<TestApplicationBlock, ProbeScreen>.ShowScreenAsync(ProbeScreen screen, CancellationToken cancellationToken) {
			return Task.FromResult(false);
		}
	}

	/// <summary>Supplies projected host decorator for the fixture's existing contract checks.</summary>
	private sealed class ProjectedHostDecorator : ApplicationScreenHostDecorator<TestApplicationBlock, ProbeScreen> {
		private readonly TestApplicationBlock _projectedBlock;

		/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
		public ProjectedHostDecorator(IApplicationScreenHost<TestApplicationBlock, ProbeScreen> host, TestApplicationBlock projectedBlock)
			: base(host) {
			_projectedBlock = projectedBlock;
		}

		/// <inheritdoc />
		public override TestApplicationBlock[] Blocks => new[] { _projectedBlock };

		/// <inheritdoc />
		public override TestApplicationBlock ActiveBlock => _projectedBlock;

		/// <inheritdoc />
		public int OpenCalls { get; private set; }

		/// <inheritdoc />
		public int SelectionCalls { get; private set; }

		/// <inheritdoc />
		public int UndockedCalls { get; private set; }

		/// <inheritdoc />
		public override Task<bool> ShowScreenUndockedAsync(ProbeScreen screen, CancellationToken cancellationToken = default) {
			UndockedCalls++;
			return base.ShowScreenUndockedAsync(screen, cancellationToken);
		}

		/// <inheritdoc />
		public override Task SelectBlockAsync(TestApplicationBlock block, CancellationToken cancellationToken = default) {
			SelectionCalls++;
			return base.SelectBlockAsync(block, cancellationToken);
		}

		/// <inheritdoc />
		public override Task<ProbeScreen> OpenScreenAsync(TestApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) {
			OpenCalls++;
			return base.OpenScreenAsync(block, definition, cancellationToken);
		}
	}

	/// <summary>Supplies probe screen for the fixture's existing contract checks.</summary>
	private sealed class ProbeScreen : IApplicationScreen {
		/// <inheritdoc />
		public IApplicationScreenHost Host { get; set; }

		/// <inheritdoc />
		public IApplicationBlock Block { get; set; }
	}

	/// <summary>Supplies neutral host decorator for the fixture's existing contract checks.</summary>
	private sealed class NeutralHostDecorator(IApplicationScreenHost host) : ApplicationScreenHostDecorator(host);

	/// <summary>Supplies other screen for the fixture's existing contract checks.</summary>
	private sealed class OtherScreen : IApplicationScreen {
		/// <inheritdoc />
		public IApplicationScreenHost Host { get; set; }

		/// <inheritdoc />
		public IApplicationBlock Block { get; set; }
	}

	/// <summary>Supplies other block for the fixture's existing contract checks.</summary>
	private sealed class OtherBlock : IApplicationBlock {
		// Retain the actual optional settings value owned by this fixture.
		public SettingsObject BlockSettings { get; set; }

		/// <inheritdoc />
		public IApplication Parent { get; set; }

		/// <inheritdoc />
		public IApplicationPlugin Source { get; set; }

		/// <inheritdoc />
		public string Name => "Other";

		/// <inheritdoc />
		public int Position => 0;

		/// <inheritdoc />
		public IApplicationMenu[] Menus => Array.Empty<IApplicationMenu>();

		/// <inheritdoc />
		public ApplicationScreenDefinition[] ScreenDefinitions => Array.Empty<ApplicationScreenDefinition>();

		/// <inheritdoc />
		public ApplicationScreenDefinition DefaultScreenDefinition => null;
	}

	#endregion
}
