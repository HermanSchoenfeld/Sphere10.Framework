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

public class WinFormsScreenMenuItem : WinFormsLinkMenuItemBase, IWinFormsScreenMenuItem {
	private ApplicationScreenDefinition _screenDefinition;

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
		ScreenDefinition = screenType == null ? null : new ApplicationScreenDefinition(screenType, text);
	}

	public virtual ApplicationScreenDefinition ScreenDefinition {
		get => _screenDefinition;
		set {
			if (ReferenceEquals(_screenDefinition, value))
				return;
			if (Parent != null)
				_screenDefinition?.RemoveActivator(this);
			_screenDefinition = value;
			if (Parent != null)
				_screenDefinition?.AddActivator(this);
		}
	}

	public virtual bool IsDefault { get; set; }

	public override void Dispose() {
		base.Dispose();
	}
}

