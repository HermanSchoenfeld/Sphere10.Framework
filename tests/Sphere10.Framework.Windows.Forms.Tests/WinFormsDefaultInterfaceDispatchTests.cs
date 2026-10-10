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
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WinFormsDefaultInterfaceDispatchTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void NativePluginOverrideProjectsThroughTheSharedInterfaceAndExistingPluginClass() {
		using var first = new WinFormsApplicationBlock { Name = "First" };
		using var second = new WinFormsApplicationBlock { Name = "Second" };
		var plugin = new ProjectedPlugin { CurrentBlock = first };
		IApplicationPlugin neutral = plugin;
		ApplicationPlugin compatibility = plugin;

		Assert.That(neutral.Blocks.Single(), Is.SameAs(first));
		plugin.CurrentBlock = second;
		Assert.That(neutral.Blocks.Single(), Is.SameAs(second));
		Assert.That(compatibility.Blocks.Single(), Is.SameAs(second));
	}

	[Test]
	public void NativeDecoratorScreenModeOverrideProjectsThroughInheritedClassAndInterfaceGetters() {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.SingleView };
		using var decorator = new ProjectedModeDecorator(host);
		IWinFormsApplicationScreenHost native = decorator;
		IApplicationScreenHost neutral = decorator;
		ApplicationScreenHostDecorator<IWinFormsApplicationBlock, WinFormsApplicationScreen, IWinFormsApplicationScreenHost> compatibility = decorator;
		native.ScreenMode = ScreenMode.MultiView;

		Assert.That(host.ScreenMode, Is.EqualTo(ScreenMode.SingleView));
		Assert.That(native.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(neutral.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(compatibility.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		neutral.ScreenMode = ScreenMode.SingleView;
		Assert.That(native.ScreenMode, Is.EqualTo(ScreenMode.SingleView));
		compatibility.ScreenMode = ScreenMode.MultiView;
		Assert.That(neutral.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(host.ScreenMode, Is.EqualTo(ScreenMode.SingleView));
	}

	private sealed class ProjectedPlugin : WinFormsApplicationPlugin {
		public ProjectedPlugin()
			: base("Projected", Array.Empty<IWinFormsApplicationBlock>()) {
		}

		public IWinFormsApplicationBlock CurrentBlock { get; set; }

		public override IWinFormsApplicationBlock[] Blocks {
			get => new[] { CurrentBlock };
			init => base.Blocks = value;
		}
	}

	private sealed class ProjectedModeDecorator : WinFormsApplicationScreenHostDecorator {
		public ProjectedModeDecorator(IWinFormsApplicationScreenHost host)
			: base(host) {
		}

		public override ScreenMode ScreenMode { get; set; }
	}
}
