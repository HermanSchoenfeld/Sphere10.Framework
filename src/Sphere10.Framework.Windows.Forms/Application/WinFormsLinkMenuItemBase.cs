// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

public abstract class WinFormsLinkMenuItemBase : WinFormsApplicationMenuItemBase, IWinFormsLinkMenuItem {

	protected WinFormsLinkMenuItemBase()
		: this(string.Empty) {
	}

	protected WinFormsLinkMenuItemBase(string text)
		: this(text, null) {
	}

	protected WinFormsLinkMenuItemBase(string text, Image image16x16)
		: this(text, image16x16, true, true, false) {
	}

	protected WinFormsLinkMenuItemBase(string text, Image image16x16, bool showOnExplorerBar, bool showOnToolBar, bool executeOnLoad)
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

