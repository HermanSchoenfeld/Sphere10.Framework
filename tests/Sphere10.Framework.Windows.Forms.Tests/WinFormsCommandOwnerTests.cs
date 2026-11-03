// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Verifies owner activation, fixed command membership and native-to-portable provider dispatch.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WinFormsCommandOwnerTests {
	[Test]
	public void BlockActivatesItsTemplatesOnceAndRejectsLaterMembershipChanges() {
		using var block = new CountingBlock();
		var menu = new WinFormsApplicationMenu("Commands");
		using var otherMenu = new WinFormsApplicationMenu("Other");
		block.AddMenu(menu);
		var menus = block.Menus;
		var toolbar = block.ToolBarItems;
		Assert.That(block.Menus[0], Is.SameAs(menu));
		Assert.That(block.ToolBarItems[0], Is.SameAs(toolbar[0]));
		Assert.That(block.MenuActivations, Is.EqualTo(1));
		Assert.That(block.ToolBarActivations, Is.EqualTo(1));

		// Returned array membership cannot mutate the owner's fixed command collection.
		menus[0] = null;
		toolbar[0] = null;
		Assert.That(block.Menus[0], Is.SameAs(menu));
		Assert.That(block.ToolBarItems[0], Is.SameAs(block.RefreshCommand));
		Assert.That(() => block.AddMenu(otherMenu), Throws.InvalidOperationException);
		Assert.That(() => block.RemoveMenu(menu), Throws.InvalidOperationException);
	}

	[Test]
	public void PluginActivatesItsOwnCommandsThroughTheNativeAndPortableInterfaces() {
		var plugin = new CountingPlugin();
		IWinFormsApplicationPlugin native = plugin;
		IApplicationPlugin portable = plugin;
		var menu = native.Menus[0];
		var action = native.ToolBarItems[0];
		Assert.That(portable.Menus[0], Is.SameAs(menu));
		Assert.That(portable.ToolBarItems[0], Is.SameAs(action));
		Assert.That(plugin.MenuActivations, Is.EqualTo(1));
		Assert.That(plugin.ToolBarActivations, Is.EqualTo(1));

		var returnedMenus = native.Menus;
		returnedMenus[0] = null;
		Assert.That(native.Menus[0], Is.SameAs(menu));
		menu.Dispose();
		action.Dispose();
	}

	[Test]
	public void BlockDisposesConfiguredAndFactoryDefinitionsOnceWithoutDuplicatingMenuChildren() {
		var block = new CountingBlock();
		var configured = new CountingMenu("Configured");
		block.AddMenu(configured);
		_ = block.Menus;
		_ = block.ToolBarItems;

		block.Dispose();

		Assert.That(configured.Disposals, Is.EqualTo(1));
		Assert.That(block.FactoryMenu.Disposals, Is.EqualTo(1));
		Assert.That(block.MenuCommand.Disposals, Is.EqualTo(1), "A toolbar projection must not dispose the same menu child twice.");
		Assert.That(block.RefreshCommand.Disposals, Is.EqualTo(1));
	}

	[Test]
	public void DisposingUnusedBlockDoesNotActivateItsCommandFactories() {
		var block = new CountingBlock();

		block.Dispose();

		Assert.That(block.MenuActivations, Is.Zero);
		Assert.That(block.ToolBarActivations, Is.Zero);
		Assert.That(block.FactoryMenu, Is.Null);
		Assert.That(block.RefreshCommand, Is.Null);
	}

	private sealed class CountingBlock : WinFormsApplicationBlock {
		internal int MenuActivations { get; private set; }

		internal int ToolBarActivations { get; private set; }

		internal CountingMenu FactoryMenu { get; private set; }

		internal CountingAction MenuCommand { get; private set; }

		internal CountingAction RefreshCommand { get; private set; }

		protected override IWinFormsApplicationMenu[] GetMenus() {
			MenuActivations++;
			FactoryMenu = new CountingMenu("Factory");
			MenuCommand = new CountingAction("Menu child");
			FactoryMenu.AddItem(MenuCommand);
			return base.GetMenus().Append(FactoryMenu).ToArray();
		}

		protected override IWinFormsApplicationMenuItem[] GetToolBarItems() {
			ToolBarActivations++;
			_ = Menus;
			RefreshCommand = new CountingAction("Refresh");
			return new IWinFormsApplicationMenuItem[] { RefreshCommand, MenuCommand, RefreshCommand };
		}
	}

	private sealed class CountingMenu(string title) : WinFormsApplicationMenu(title) {
		internal int Disposals { get; private set; }

		public override void Dispose() {
			Disposals++;
			base.Dispose();
		}
	}

	private sealed class CountingAction(string title) : WinFormsActionMenuItem(title, () => {
	}) {
		internal int Disposals { get; private set; }

		public override void Dispose() {
			Disposals++;
			base.Dispose();
		}
	}

	private sealed class CountingPlugin : WinFormsApplicationPlugin {
		internal CountingPlugin()
			: base("Commands", Array.Empty<IWinFormsApplicationBlock>()) {
		}

		internal int MenuActivations { get; private set; }

		internal int ToolBarActivations { get; private set; }

		protected override IWinFormsApplicationMenu[] GetMenus() {
			MenuActivations++;
			return new IWinFormsApplicationMenu[] { new WinFormsApplicationMenu("Plugin Commands") };
		}

		protected override IWinFormsApplicationMenuItem[] GetToolBarItems() {
			ToolBarActivations++;
			return new IWinFormsApplicationMenuItem[] {
				new WinFormsActionMenuItem("Plugin Action", () => {
				})
			};
		}
	}
}
