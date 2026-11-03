// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Projects a borrowed native item without copying its callback, state, image or ownership.</summary>
internal class NativeToolStripCommand : IWinFormsLinkMenuItem {
	// Activate this owner's complete native hierarchy before any contextual merge changes physical ownership.
	public NativeToolStripCommand(ToolStripItem item, IApplicationMenu parent = null, Func<ToolStripItem, bool> isSourceItem = null) {
		Guard.ArgumentNotNull(item, nameof(item));
		Item = item;
		Parent = parent;

		// Capture each level before contextual merging can temporarily borrow another owner's children.
		Children = item is ToolStripDropDownItem dropDown
			? dropDown.DropDownItems.Cast<ToolStripItem>().Where(child => isSourceItem?.Invoke(child) ?? true)
				.Select(child => Create(child, parent, isSourceItem)).ToArray()
			: Array.Empty<NativeToolStripCommand>();
	}

	public ToolStripItem Item { get; }

	public IApplicationMenu Parent { get; set; }

	internal IReadOnlyList<NativeToolStripCommand> Children { get; }

	public string Title {
		get {
			return string.IsNullOrEmpty(Item.Text) ? Item.ToolTipText : Item.Text;
		}
	}

	public string Text {
		get {
			return Title;
		}
	}

	public Image Image16x16 {
		get {
			return Item.Image;
		}
	}

	public bool ShowOnExplorerBar {
		get {
			return false;
		}
	}

	public bool ShowOnToolStrip {
		get {
			return Item.Owner is ToolStrip and not MenuStrip and not ToolStripDropDown;
		}
	}

	public bool ExecuteOnLoad {
		get {
			return false;
		}
	}

	// Programmatic command execution follows the same native handler and checked/disabled behavior as a click.
	public void OnSelect() {
		if (Item is ToolStripSplitButton split)
			split.PerformButtonClick();
		else
			Item.PerformClick();
	}

	// The owning form invokes its original handlers with the displaying window's explicit context.
	public void OnSelect(IUserInterfaceServices userInterface) {
		if (userInterface?.PrimaryUIController is MainForm form)
			form.ExecuteNativeCommand(OnSelect, userInterface);
		else
			OnSelect();
	}

	// Run the original owner's opening handlers without showing or moving its native popup.
	internal void OnDropDownOpening(IUserInterfaceServices userInterface) {
		if (Item is not ToolStripDropDownItem dropDown)
			return;
		if (userInterface?.PrimaryUIController is MainForm form)
			form.ExecuteNativeCommand(() => ToolStripVisualInheritanceFixer.NotifyDropDownOpening(dropDown), userInterface);
		else
			ToolStripVisualInheritanceFixer.NotifyDropDownOpening(dropDown);
	}

	// The form, rather than a descriptor consumer, owns the native item and its image lifetime.
	public void Dispose() {
	}

	// Preserve native separator semantics in the shared command projection.
	internal static NativeToolStripCommand Create(ToolStripItem item, IApplicationMenu parent = null, Func<ToolStripItem, bool> isSourceItem = null) {
		return item is ToolStripSeparator ? new NativeToolStripSeparator(item, parent) : new NativeToolStripCommand(item, parent, isSourceItem);
	}
}
