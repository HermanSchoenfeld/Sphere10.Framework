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

public class ApplicationMenu : ApplicationMenuBase<IApplicationMenuItem>, IApplicationMenu {
	public ApplicationMenu()
		: this(string.Empty) {
	}

	public ApplicationMenu(string title)
		: this(title, null) {
	}

	public ApplicationMenu(string title, IApplicationMenuItem[] items)
		: this(title, null, items) {
	}

	public ApplicationMenu(string title, Image image32x32, IApplicationMenuItem[] items)
		: this(title, false, image32x32, items) {
	}

	public ApplicationMenu(string title, bool showInMenuStrip, Image image32x32, IApplicationMenuItem[] items) {
		Text = title;
		Image32x32 = image32x32;
		ShowInMenuStrip = showInMenuStrip;
		if (items != null)
			foreach (var item in items)
				AddItem(item);
	}

	public bool ShowInMenuStrip { get; set; }

	public new virtual IApplicationBlock Parent {
		get => (IApplicationBlock)base.Parent;
		set => base.Parent = value;
	}

	public override IApplicationMenuItem[] Items => base.Items;

	public Image Image32x32 { get; set; }

	public virtual void Dispose() {
		Parent = null;
		foreach (var item in ItemCollection)
			item.Dispose();
		Image32x32?.Dispose();
	}
}
