// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationAggregateTests {
	[Test]
	public void CommandMembershipIsCopiedAndInvalidUpdatesLeaveExistingCommandsIntact() {
		using var application = new ProbeApplication();
		Assert.That(application.Blocks, Is.Empty);
		var menu = new TestApplicationMenu<IApplicationMenuItem> { Text = "File" };
		var item = new TestApplicationMenuItem { Title = "Save" };
		var menus = new IApplicationMenu[] { menu };
		var items = new IApplicationMenuItem[] { item };
		var notifications = 0;
		application.Changed += () => notifications++;
		application.SetMenus(menus);
		application.SetToolBarItems(items);
		menus[0] = null;
		items[0] = null;
		application.Menus[0] = null;
		application.ToolBarItems[0] = null;
		Assert.That(application.Menus, Is.EqualTo(new[] { menu }));
		Assert.That(application.ToolBarItems, Is.EqualTo(new[] { item }));
		Assert.That(() => application.SetMenus(menus), Throws.ArgumentException);
		Assert.That(() => application.SetToolBarItems(items), Throws.ArgumentException);
		Assert.That(application.Menus, Is.EqualTo(new[] { menu }));
		Assert.That(application.ToolBarItems, Is.EqualTo(new[] { item }));
		Assert.That(notifications, Is.EqualTo(2));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task DecoratorForwardsLiveStateNotificationsAndDisposal(bool asynchronous) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new TestApplicationBlock("Work", new(typeof(ProbeScreen)));
		using var host = new ProbeHost(block, new ProbeScreen(), provider);
		var application = new ProbeApplication(host);
		var decorator = new ProbeDecorator(application);
		var notifications = 0;
		EventHandlerEx handler = () => notifications++;
		decorator.Changed += handler;
		var menu = new TestApplicationMenu<IApplicationMenuItem> { Text = "File" };
		var item = new TestApplicationMenuItem { Title = "Save" };
		application.SetMenus(new[] { menu });
		application.SetToolBarItems(new[] { item });
		Assert.That(decorator.LoadedPlugins, Is.EqualTo(application.LoadedPlugins));
		Assert.That(decorator.Blocks, Is.EqualTo(application.Blocks));
		Assert.That(decorator.ScreenHost, Is.SameAs(application.ScreenHost));
		Assert.That(decorator.ScreenHost.ActiveBlock, Is.SameAs(block));
		Assert.That(decorator.ScreenHost.ActiveScreen, Is.SameAs(host.ActiveScreen));
		Assert.That(decorator.HasUnsavedChanges, Is.True);
		Assert.That(decorator.Menus, Is.EqualTo(new[] { menu }));
		Assert.That(decorator.ToolBarItems, Is.EqualTo(new[] { item }));
		Assert.That(notifications, Is.EqualTo(2));
		decorator.Changed -= handler;
		application.SetToolBarItems(Array.Empty<IApplicationMenuItem>());
		Assert.That(notifications, Is.EqualTo(2));
		if (asynchronous)
			await decorator.DisposeAsync();
		else
			decorator.Dispose();
		Assert.That(application.WasDisposed, Is.True);
		Assert.That(() => application.SetMenus(new[] { menu }), Throws.InvalidOperationException);
	}

	private sealed class ProbeApplication : SyncApplicationBase {
		private readonly ApplicationPlugin _plugin;

		public ProbeApplication(IApplicationScreenHost host = null) {
			_plugin = new("Work", host?.Blocks ?? Array.Empty<IApplicationBlock>());
			ScreenHost = host;
			if (host != null)
				ObserveScreenHost();
		}

		public override IApplicationPlugin[] LoadedPlugins => new[] { _plugin };
		public override IApplicationScreenHost ScreenHost { get; }
		public bool WasDisposed => IsDisposed;
	}

	private sealed class ProbeHost : ApplicationScreenHost {
		public ProbeHost(TestApplicationBlock block, ProbeScreen screen, IServiceProvider provider)
			: base(new[] { block }, provider) {
			TrackScreen(screen, block, block.DefaultScreenDefinition);
			SetTrackedSelection(screen);
		}
	}

	private sealed class ProbeScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
		public bool HasUnsavedChanges => true;
	}

	private sealed class ProbeDecorator : ApplicationDecorator<ProbeApplication> {
		public ProbeDecorator(ProbeApplication application) : base(application) { }
	}
}
