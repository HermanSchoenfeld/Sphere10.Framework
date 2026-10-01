// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsApplicationMenu : ApplicationMenu<IWinFormsApplicationMenuItem>, IWinFormsApplicationMenu {
	public WinFormsApplicationMenu()
		: this(string.Empty) {
	}

	public WinFormsApplicationMenu(string title)
		: this(title, null) {
	}

	public WinFormsApplicationMenu(string title, IWinFormsApplicationMenuItem[] items)
		: this(title, null, items) {
	}

	public WinFormsApplicationMenu(string title, Image image32x32, IWinFormsApplicationMenuItem[] items)
		: this(title, false, image32x32, items) {
	}

	public WinFormsApplicationMenu(string title, bool showInMenuStrip, Image image32x32, IWinFormsApplicationMenuItem[] items) {
		Text = title;
		Image32x32 = image32x32;
		ShowInMenuStrip = showInMenuStrip;
		if (items != null)
			foreach (var item in items)
				AddItem(item);
	}

	public bool ShowInMenuStrip { get; set; }

	public virtual IWinFormsApplicationBlock Parent { get; set; }

	public override IWinFormsApplicationMenuItem[] Items => base.Items;

	public Image Image32x32 { get; set; }

	public override void AddItem(IWinFormsApplicationMenuItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		if (item is WinFormsScreenMenuItem && item.Parent == null)
			item.Parent = this;
		base.AddItem(item);
	}

	public virtual void Dispose() {
		Parent = null;
		foreach (var item in ItemCollection)
			item.Dispose();
		Image32x32?.Dispose();
	}
}
