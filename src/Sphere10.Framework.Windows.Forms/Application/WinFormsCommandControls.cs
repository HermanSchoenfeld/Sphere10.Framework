// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Creates window-owned native views for command definitions supplied by builders or application code.</summary>
internal static class WinFormsCommandControls {
	internal const string CommandMenuSourceName = "_commandMenuSource";

	internal const string CommandToolBarSourceName = "_commandToolBarSource";

	/// <summary>Executes a definition through the displaying window's host while preserving its owner's native action.</summary>
	internal static async Task<bool> ExecuteMenuItemAsync(IUserInterfaceServices userInterface, IApplicationMenuItem item,
		CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(userInterface, nameof(userInterface));
		cancellationToken.ThrowIfCancellationRequested();
		var nativeItem = Guard.ArgumentCast<IWinFormsApplicationMenuItem>(item, nameof(item));
		if (nativeItem is IWinFormsScreenMenuItem screenItem) {
			var block = screenItem.Parent?.Parent;
			Guard.Argument(block != null, nameof(item), "The screen command must belong to a block's menu.");
			if (await userInterface.ScreenHost.ActivateScreenAsync(block, screenItem, cancellationToken) == null)
				return false;
		}
		if (nativeItem is IWinFormsLinkMenuItem link)
			link.OnSelect(userInterface);
		return true;
	}

	/// <summary>Creates and merges fresh native views from the current command owners into one window.</summary>
	internal static void MergeSources(Form window, MenuStrip targetMenu, ToolStrip targetToolBar,
		IEnumerable<IApplicationCommandProvider> providers, IUserInterfaceServices userInterface) {
		foreach (var provider in providers) {
			var menus = provider.Menus.Cast<IWinFormsApplicationMenu>().Where(menu => menu.ShowInMenuStrip).ToArray();
			var toolBarItems = provider.ToolBarItems.Cast<IWinFormsApplicationMenuItem>();
			if (provider is IWinFormsApplicationBlock block) {
				if (!block.ShowInMenuStrip)
					menus = Array.Empty<IWinFormsApplicationMenu>();
				if (block.ShowInToolStrip)
					toolBarItems = toolBarItems.Concat(block.Menus.SelectMany(menu => menu.Items).OfType<IWinFormsLinkMenuItem>()
						.Where(item => item.ShowOnToolStrip));
			}
			MergeSources(window, targetMenu, targetToolBar, menus,
				toolBarItems.Distinct<IWinFormsApplicationMenuItem>(ReferenceEqualityComparer.Instance), provider, userInterface);
		}
	}

	/// <summary>Creates sources owned by this window while the supplied owner retains command definitions and images.</summary>
	internal static void MergeSources(Form window, MenuStrip targetMenu, ToolStrip targetToolBar,
		IEnumerable<IWinFormsApplicationMenu> menus, IEnumerable<IWinFormsApplicationMenuItem> toolBarItems,
		IApplicationCommandProvider owner, IUserInterfaceServices userInterface) {
		var menuDefinitions = menus.ToArray();
		if (menuDefinitions.Length > 0) {
			var source = CreateMenuStrip(menuDefinitions, userInterface);
			source.Tag = owner;
			window.Controls.Add(source);
			source.MergeInto(targetMenu, matchMenuText: true);
		}

		var itemDefinitions = toolBarItems.ToArray();
		if (itemDefinitions.Length > 0) {
			var source = CreateToolBar(itemDefinitions, userInterface);
			source.Tag = owner;
			window.Controls.Add(source);
			source.MergeInto(targetToolBar);
		}
	}

	/// <summary>Releases generated sources in reverse merge order through their normal native-control ownership.</summary>
	internal static void ClearSources(Form window) {
		foreach (var source in window.Controls.Cast<Control>().Where(IsCommandSource).Reverse().ToArray()) {
			if (source is MergeableMenuStrip menu)
				menu.Unmerge();
			else if (source is MergeableToolStrip toolBar)
				toolBar.Unmerge();
			source.Dispose();
		}
	}

	// Only factory-created source controls carry these names and a command owner; designer controls retain their own identity.
	internal static bool IsCommandSource(Control control) {
		return control.Tag is IApplicationCommandProvider
			&& (control is MergeableMenuStrip && control.Name == CommandMenuSourceName
				|| control is MergeableToolStrip && control.Name == CommandToolBarSourceName);
	}

	/// <summary>Creates the menu source with direct native callbacks to each original command.</summary>
	internal static MergeableMenuStrip CreateMenuStrip(IEnumerable<IWinFormsApplicationMenu> menus, IUserInterfaceServices userInterface = null) {
		Guard.ArgumentNotNull(menus, nameof(menus));
		var source = new MergeableMenuStrip { Name = CommandMenuSourceName };
		foreach (var menu in menus) {
			if (menu is NativeToolStripMenu nativeMenu) {
				// A second window owns views of application commands, never the root form's native items.
				var nativeHeader = CreateNativeView(nativeMenu.Header, () => source.InheritedToolStrip, userInterface, () => {
					if (userInterface?.PrimaryUIController is MainForm owner && owner.IsScreenNavigationMenu(nativeMenu.Item)) {
						// Navigation precedes the complete owner snapshot, independent of another screen's native merge edits.
						return nativeMenu.Item.DropDownItems.Cast<ToolStripItem>().Where(item => item.Tag is WinFormsApplicationScreen)
							.Select(item => (IWinFormsApplicationMenuItem)NativeToolStripCommand.Create(item))
							.Concat(nativeMenu.Items).ToArray();
					}
					return nativeMenu.Items;
				});
				source.Items.Add(nativeHeader);
				continue;
			}
			var header = new ToolStripMenuItem(menu.Text) { Image = menu.Image32x32 };
			foreach (var command in menu.Items)
				header.DropDownItems.Add(CreateMenuItem(command, () => source.InheritedToolStrip, userInterface));
			source.Items.Add(header);
		}
		return source;
	}

	/// <summary>Creates toolbar views without retaining a separate host-to-command binding table.</summary>
	internal static MergeableToolStrip CreateToolBar(IEnumerable<IWinFormsApplicationMenuItem> commands, IUserInterfaceServices userInterface = null) {
		Guard.ArgumentNotNull(commands, nameof(commands));
		var source = new MergeableToolStrip { Name = CommandToolBarSourceName };
		foreach (var command in commands) {
			if (command is NativeToolStripCommand native) {
				source.Items.Add(CreateNativeView(native, () => source.InheritedToolStrip, userInterface));
				continue;
			}
			if (command is IApplicationMenuSeparator) {
				source.Items.Add(new ToolStripSeparator());
				continue;
			}

			var button = new ToolStripButton {
				Image = command.Image16x16 ?? Resources.DefaultToolStripImage,
				ToolTipText = command.Title
			};
			button.Click += async (_, _) => {
				await ExecuteCommandAsync(command, source.InheritedToolStrip, userInterface);
			};
			source.Items.Add(button);
		}
		return source;
	}

	// Separators and actions retain their original command definitions; only their native view is created here.
	private static ToolStripItem CreateMenuItem(IWinFormsApplicationMenuItem command, Func<ToolStrip> target, IUserInterfaceServices userInterface) {
		if (command is NativeToolStripCommand native)
			return CreateNativeView(native, target, userInterface);
		if (command is IApplicationMenuSeparator)
			return new ToolStripSeparator();
		var item = new ToolStripMenuItem(command.Title, command.Image16x16);
		item.Click += async (_, _) => {
			await ExecuteCommandAsync(command, target(), userInterface);
		};
		return item;
	}

	// Borrow state and callbacks while keeping every native view and subscription owned by its displaying window.
	private static ToolStripItem CreateNativeView(NativeToolStripCommand command, Func<ToolStrip> target, IUserInterfaceServices userInterface,
		Func<IWinFormsApplicationMenuItem[]> getChildren = null) {
		var original = command.Item;
		ToolStripItem view = original switch {
			ToolStripSeparator => new ToolStripSeparator(),
			ToolStripMenuItem => new ToolStripMenuItem(),
			ToolStripSplitButton => new ToolStripSplitButton(),
			ToolStripDropDownButton => new ToolStripDropDownButton(),
			ToolStripLabel => new ToolStripLabel(),
			_ => new ToolStripButton()
		};
		view.MergeAction = original.MergeAction;
		view.MergeIndex = original.MergeIndex;

		void Synchronize(object sender, EventArgs args) {
			if (view.IsDisposed)
				return;
			view.Name = original.Name;
			view.Text = original.Text;
			view.ToolTipText = original.ToolTipText;
			view.Image = original.Image;
			view.ImageScaling = original.ImageScaling;
			view.DisplayStyle = original.DisplayStyle;
			view.Enabled = original.Enabled;
			view.Available = original.Available;
			view.Font = original.Font;
			view.ForeColor = original.ForeColor;
			view.BackColor = original.BackColor;
			view.Tag = original.Tag;
			if (view is ToolStripMenuItem menu && original is ToolStripMenuItem originalMenu) {
				menu.CheckState = originalMenu.CheckState;
				menu.ShortcutKeys = originalMenu.ShortcutKeys;
				menu.ShowShortcutKeys = originalMenu.ShowShortcutKeys;
			} else if (view is ToolStripButton button && original is ToolStripButton originalButton)
				button.CheckState = originalButton.CheckState;
		}

		// These state changes remain live without a host-side binding list or a copied command model.
		Synchronize(original, EventArgs.Empty);
		original.EnabledChanged += Synchronize;
		original.AvailableChanged += Synchronize;
		original.TextChanged += Synchronize;
		if (original is ToolStripMenuItem checkedMenu)
			checkedMenu.CheckedChanged += Synchronize;
		if (original is ToolStripButton checkedButton)
			checkedButton.CheckedChanged += Synchronize;
		view.Disposed += (_, _) => {
			original.EnabledChanged -= Synchronize;
			original.AvailableChanged -= Synchronize;
			original.TextChanged -= Synchronize;
			if (original is ToolStripMenuItem menu)
				menu.CheckedChanged -= Synchronize;
			if (original is ToolStripButton button)
				button.CheckedChanged -= Synchronize;
		};

		if (view is ToolStripDropDownItem dropDown) {
			getChildren ??= () => command.Children.Cast<IWinFormsApplicationMenuItem>().ToArray();
			void RefreshChildren() {
				foreach (ToolStripItem child in dropDown.DropDownItems.Cast<ToolStripItem>().ToArray())
					child.Dispose();
				foreach (var child in getChildren())
					dropDown.DropDownItems.Add(CreateMenuItem(child, target, userInterface));
			}
			RefreshChildren();
			dropDown.DropDownOpening += (_, _) => {
				command.OnDropDownOpening(userInterface ?? target()?.FindForm() as IUserInterfaceServices);
				Synchronize(original, EventArgs.Empty);
				RefreshChildren();
			};
		}

		// CheckOnClick belongs to the original item; forwarding its click toggles state exactly once.
		if (view is ToolStripSplitButton split)
			split.ButtonClick += async (_, _) => {
				await ExecuteCommandAsync(command, target(), userInterface);
			};
		else if (view is not ToolStripSeparator)
			view.Click += async (_, _) => {
				await ExecuteCommandAsync(command, target(), userInterface);
			};
		return view;
	}

	// Resolve the currently displaying form at the click boundary, without caching a UI/application on the command owner.
	private static async Task ExecuteCommandAsync(IWinFormsApplicationMenuItem command, ToolStrip target, IUserInterfaceServices userInterface) {
		userInterface ??= target?.FindForm() as IUserInterfaceServices;
		if (userInterface == null)
			return;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => userInterface.ExecuteMenuItemAsync(command));
		if (error != null)
			userInterface.ReportError(error);
	}
}
