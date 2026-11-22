// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public static class ToolStripExtensions {
	/// <summary>
	/// Scales the sizes a designer fixed on the tool strip's items, including those in its drop-downs, from 96-DPI pixels to the tool strip's DPI.
	/// Windows Forms scales a tool strip's fonts and images with the display but not the sizes of items with <see cref="ToolStripItem.AutoSize"/>
	/// off, nor the widths of text and combo boxes, so on a scaled display their text is cut. Call it once, after the items are created, as a
	/// screen does when it takes its tool strip; an item sized to its contents needs nothing.
	/// </summary>
	public static void ScaleFixedItemSizes(this ToolStrip toolStrip) {
		Guard.ArgumentNotNull(toolStrip, nameof(toolStrip));
		if (toolStrip.DeviceDpi == 96)
			return;
		ScaleFixedItemSizes(toolStrip, toolStrip.Items.Cast<ToolStripItem>());
	}

	private static void ScaleFixedItemSizes(ToolStrip toolStrip, IEnumerable<ToolStripItem> items) {
		foreach (var item in items) {
			if (!item.AutoSize)
				item.Size = toolStrip.LogicalToDeviceUnits(item.Size);
			else if (item is ToolStripTextBox or ToolStripComboBox)
				item.Width = toolStrip.LogicalToDeviceUnits(item.Width);
			if (item is ToolStripDropDownItem { HasDropDownItems: true } dropDown)
				ScaleFixedItemSizes(toolStrip, dropDown.DropDownItems.Cast<ToolStripItem>());
		}
	}
}
