// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsLinkMenuItem : WinFormsApplicationMenuItem, IWinFormsLinkMenuItem {

	public WinFormsLinkMenuItem()
		: this(string.Empty) {
	}

	public WinFormsLinkMenuItem(string text)
		: this(text, null) {
	}

	public WinFormsLinkMenuItem(string text, Image image16x16)
		: this(text, image16x16, true, true, false) {
	}

	public WinFormsLinkMenuItem(string text, Image image16x16, bool showOnExplorerBar, bool showOnToolBar, bool executeOnLoad)
		: base(image16x16, showOnExplorerBar, showOnToolBar, executeOnLoad) {

		Title = text;
	}


	public virtual string Text {
		get => Title;
		set => Title = value;
	}


	public virtual void OnSelect() => NotifySelect();

	public override void Dispose() {
		base.Dispose();
	}

}

