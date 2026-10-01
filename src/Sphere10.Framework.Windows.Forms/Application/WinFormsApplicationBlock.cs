// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Linq;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsApplicationBlock : ApplicationBlock, IWinFormsApplicationBlock {
	public WinFormsApplicationBlock()
		: this(string.Empty, null, null, null) {
	}

	public WinFormsApplicationBlock(string title, Image image32x32, Image image8x8, string helpFile)
		: this(title, image32x32, image8x8, helpFile, null) {
	}

	public WinFormsApplicationBlock(string title, Image image32x32, Image image8x8, string helpFile, WinFormsApplicationMenu[] menus)
		: this(title, false, false, image32x32, image8x8, helpFile, menus) {
	}

	public WinFormsApplicationBlock(string title, bool showInToolStrip, bool showInMenuStrip, Image image32x32, Image image8x8, string helpFile, WinFormsApplicationMenu[] menus) {
		Name = title;
		ShowInMenuStrip = showInMenuStrip;
		ShowInToolStrip = showInToolStrip;
		HelpFileCHM = helpFile;
		Image32x32 = image32x32;
		Image8x8 = image8x8;
		if (menus != null)
			foreach (var menu in menus)
				AddMenu(menu);
	}

	public bool ShowInToolStrip { get; set; }

	public virtual bool ShowInMenuStrip { get; set; }

	public virtual Image Image32x32 { get; set; }

	public Image Image8x8 { get; set; }

	public override IWinFormsApplicationMenu[] Menus => MenuCollection.Cast<IWinFormsApplicationMenu>().ToArray();

	public string HelpFileCHM { get; set; }

	public override void AddMenu(IApplicationMenu menu) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		AddMenu(Guard.ArgumentCast<IWinFormsApplicationMenu>(menu, nameof(menu)));
	}

	public virtual void AddMenu(IWinFormsApplicationMenu menu) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		menu.Parent = this;
		base.AddMenu(menu);
	}

	public override bool ContainsMenu(IApplicationMenu menu) => menu is IWinFormsApplicationMenu nativeMenu && ContainsMenu(nativeMenu);

	public virtual bool ContainsMenu(IWinFormsApplicationMenu menu) => base.ContainsMenu(menu);

	public override void RemoveMenu(IApplicationMenu menu) {
		if (menu is IWinFormsApplicationMenu nativeMenu)
			RemoveMenu(nativeMenu);
	}

	public virtual void RemoveMenu(IWinFormsApplicationMenu menu) => base.RemoveMenu(menu);

	public virtual void Dispose() {
		foreach (IWinFormsApplicationMenu menu in MenuCollection)
			menu.Dispose();
		Image8x8?.Dispose();
		Image32x32?.Dispose();
	}
}
