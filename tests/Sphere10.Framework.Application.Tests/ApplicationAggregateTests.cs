// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationAggregateTests {
	[Test]
	public void CommandMembershipIsCopiedAndInvalidUpdatesLeaveExistingCommandsIntact() {
		using var application = new ProbeApplication();
		var menu = new ApplicationMenu<IApplicationMenuItem> { Text = "File" };
		var item = new ApplicationMenuItem { Title = "Save" };
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
		var application = new ProbeApplication();
		var decorator = new ProbeDecorator(application);
		var notifications = 0;
		EventHandlerEx handler = () => notifications++;
		decorator.Changed += handler;
		var menu = new ApplicationMenu<IApplicationMenuItem> { Text = "File" };
		var item = new ApplicationMenuItem { Title = "Save" };
		application.SetMenus(new[] { menu });
		application.SetToolBarItems(new[] { item });
		Assert.That(decorator.Plugins, Is.EqualTo(application.Plugins));
		Assert.That(decorator.ActivePlugin, Is.SameAs(application.ActivePlugin));
		Assert.That(decorator.Blocks, Is.EqualTo(application.Blocks));
		Assert.That(decorator.ActiveBlock, Is.SameAs(application.ActiveBlock));
		Assert.That(decorator.ActiveScreen, Is.SameAs(application.ActiveScreen));
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

	private sealed class ProbeApplication : ApplicationBase {
		private readonly ApplicationBlock _block = new() { Name = "Work" };
		private readonly ProbeScreen _screen = new();
		private readonly ApplicationPlugin _plugin;

		public ProbeApplication() => _plugin = new ApplicationPlugin("Work", new[] { _block });

		public override IApplicationPlugin[] Plugins => new[] { _plugin };
		public override IApplicationBlock ActiveBlock => _block;
		public override IApplicationScreen ActiveScreen => _screen;
		public override bool HasUnsavedChanges => _screen.HasUnsavedChanges;
		public bool WasDisposed => IsDisposed;
	}

	private sealed class ProbeScreen : IApplicationScreen {
		public bool HasUnsavedChanges => true;
	}

	private sealed class ProbeDecorator : ApplicationDecorator<ProbeApplication> {
		public ProbeDecorator(ProbeApplication application) : base(application) { }
	}
}
