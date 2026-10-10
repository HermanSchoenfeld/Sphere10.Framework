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

public class WinFormsApplicationMenu : ApplicationMenuBase<IWinFormsApplicationMenuItem>, IWinFormsApplicationMenu {
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

	public new virtual IWinFormsApplicationBlock Parent {
		get => (IWinFormsApplicationBlock)base.Parent;
		set => base.Parent = value;
	}

	public override IWinFormsApplicationMenuItem[] Items => base.Items;

	public Image Image32x32 { get; set; }

	public override void AddItem(IWinFormsApplicationMenuItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		base.AddItem(item);
		if (Parent is WinFormsApplicationBlock block && item is IScreenMenuItem screen) {
			block.AddScreenDefinition(screen.ScreenDefinition);
			if (item is WinFormsScreenMenuItem { IsDefault: true })
				block.DefaultScreenDefinition ??= screen.ScreenDefinition;
		}
	}

	public virtual void Dispose() {
		Parent = null;
		foreach (var item in ItemCollection)
			item.Dispose();
		Image32x32?.Dispose();
	}
}
