// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsScreenMenuItem : WinFormsLinkMenuItem, IWinFormsScreenMenuItem {
	private Type _screen;

	public WinFormsScreenMenuItem()
		: this(string.Empty, null) {
	}

	public WinFormsScreenMenuItem(string text, Type viewType)
		: this(text, viewType, null) {
	}


	public WinFormsScreenMenuItem(string text, Type screenType, Image image16x16)
		: this(text, screenType, image16x16, true, true, false) {
	}

	public WinFormsScreenMenuItem(string text, Type screenType, Image image16x16, bool showOnExplorerBar, bool showOnToolBar, bool isStartScreen)
		: base(text, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen) {
		_screen = screenType;
	}

	public virtual Type Screen {
		get { return _screen; }
		set { _screen = value; }
	}

	public virtual ScreenActivationMode? ActivationMode { get; set; }

	public virtual string ScreenTitle { get; set; }

	public virtual ScreenKind ScreenKind { get; set; }

	public virtual bool IsDefault { get; set; }

	public override void Dispose() {
		base.Dispose();
	}
}

