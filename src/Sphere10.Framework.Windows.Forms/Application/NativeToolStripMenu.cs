// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Exposes an owner's activated menu membership while retaining the original native controls and their live state.</summary>
internal sealed class NativeToolStripMenu : IWinFormsApplicationMenu {
	private readonly NativeToolStripCommand _header;

	public NativeToolStripMenu(ToolStripMenuItem item, Func<ToolStripItem, bool> isSourceItem) {
		Guard.ArgumentNotNull(item, nameof(item));
		Guard.ArgumentNotNull(isSourceItem, nameof(isSourceItem));
		Item = item;
		// Membership is activated once by the owner; the original native items retain their live state.
		_header = new NativeToolStripCommand(item, this, isSourceItem);
	}

	public ToolStripMenuItem Item { get; }

	internal NativeToolStripCommand Header {
		get {
			return _header;
		}
	}

	public IWinFormsApplicationBlock Parent { get; set; }

	public string Text {
		get {
			return Item.Text;
		}
	}

	public bool ShowInMenuStrip {
		get {
			return Item.Available;
		}
	}

	public Image Image32x32 {
		get {
			return Item.Image;
		}
	}

	public IWinFormsApplicationMenuItem[] Items {
		get {
			return _header.Children.Cast<IWinFormsApplicationMenuItem>().ToArray();
		}
	}

	// Native headers and their children retain the form's existing disposal owner.
	public void Dispose() {
	}
}
