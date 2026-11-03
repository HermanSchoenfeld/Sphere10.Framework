// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Handles a selected action on a composite selection button.</summary>
public delegate void SubButtonItemEventHandler(object sender, SubButtonItemEventArgs args);

/// <summary>Describes a caption and callback offered by a composite selection button.</summary>
public class SubButtonItem {
	private event EventHandler _click;

	public event EventHandler Click {
		add {
			_click += value;
		}
		remove {
			_click -= value;
		}
	}

	internal ToolStripMenuItem MenuItem;

	public SubButtonItem()
		: this("NewItem") {
	}

	public SubButtonItem(string text)
		: this(text, (EventHandler)null) {
	}

	public SubButtonItem(string text, EventHandler handler)
		: this(text, handler, null) {
	}

	public SubButtonItem(string text, EventHandler handler, Image image) {
		if (image == null)
			MenuItem = new ToolStripMenuItem();
		else {
			MenuItem = new ToolStripMenuItem();
			//TODO add the image
		}

		InitializeMenuItem(text, handler);
	}

	public SubButtonItem(string text, EventHandler handler, ImageList imageList, int imageIndex) {
		if (imageList == null)
			MenuItem = new ToolStripMenuItem();
		else {
			MenuItem = new ToolStripMenuItem();
			//TODO add the image
		}

		InitializeMenuItem(text, handler);
	}

	public object Tag { get; set; }

	public string Text {
		get => MenuItem.Text;
		set => MenuItem.Text = value;
	}

	public MultiSelectionButton Owner { get; set; }

	public void InvokeItemClick(EventArgs args) {
		if (_click != null)
			_click(this, args);
	}

	// Initializes the backing menu item and optional click callback.
	private void InitializeMenuItem(string text, EventHandler handler) {
		MenuItem.Click += new EventHandler(MenuClick);

		Text = text;
		if (handler != null)
			Click += handler;
	}

	// Forwards the backing menu action through its owning multi-selection button.
	private void MenuClick(object sender, EventArgs args) {
		if (Owner != null)
			Owner.InvokeSubItemClick(new SubButtonItemEventArgs(this));
	}
}
