// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

public class MenuItemBuilder {
	private object _specificBuilder;

	public ScreenMenuItemBuilder AsScreenItem() {
		_specificBuilder = new ScreenMenuItemBuilder();
		return (ScreenMenuItemBuilder)_specificBuilder;
	}

	public ActionMenuItemBuilder AsActionItem() {
		_specificBuilder = new ActionMenuItemBuilder();
		return (ActionMenuItemBuilder)_specificBuilder;
	}

	public IMenuItem Build() {
		return _specificBuilder switch {
			ScreenMenuItemBuilder screenBuilder => screenBuilder.Build(),
			ActionMenuItemBuilder actionBuilder => actionBuilder.Build(),
			_ => throw new InvalidOperationException("MenuItem type not configured")
		};
	}

	public class ScreenMenuItemBuilder {
		private string _text;
		private Type _screenType;
		private Image _image16x16;
		private bool _showOnExplorerBar = true;
		private bool _showOnToolBar = true;
		private bool _isStartScreen = false;

		public ScreenMenuItemBuilder WithText(string text) {
			_text = text;
			return this;
		}

		public ScreenMenuItemBuilder WithScreen(Type screenType) {
			_screenType = screenType;
			return this;
		}

		public ScreenMenuItemBuilder WithScreen<TScreen>() {
			_screenType = typeof(TScreen);
			return this;
		}

		public ScreenMenuItemBuilder WithImage(Image image16x16) {
			_image16x16 = image16x16;
			return this;
		}

		public ScreenMenuItemBuilder ShowOnExplorerBar(bool show = true) {
			_showOnExplorerBar = show;
			return this;
		}

		public ScreenMenuItemBuilder ShowOnToolBar(bool show = true) {
			_showOnToolBar = show;
			return this;
		}

		public ScreenMenuItemBuilder IsStartScreen(bool isStart = true) {
			_isStartScreen = isStart;
			return this;
		}

		internal ScreenMenuItem Build() {
			Guard.Ensure(!string.IsNullOrEmpty(_text), "Screen menu item text is required");
			Guard.Ensure(_screenType != null, "Screen type is required");
			return new ScreenMenuItem(_text, _screenType, _image16x16, _showOnExplorerBar, _showOnToolBar, _isStartScreen);
		}
	}

	public class ActionMenuItemBuilder {
		private string _text;
		private Action _action;
		private Image _image16x16;
		private bool _showOnExplorerBar = true;
		private bool _showOnToolBar = true;
		private bool _executeOnLoad = false;

		public ActionMenuItemBuilder WithText(string text) {
			_text = text;
			return this;
		}

		public ActionMenuItemBuilder WithAction(Action action) {
			_action = action;
			return this;
		}

		public ActionMenuItemBuilder WithImage(Image image16x16) {
			_image16x16 = image16x16;
			return this;
		}

		public ActionMenuItemBuilder ShowOnExplorerBar(bool show = true) {
			_showOnExplorerBar = show;
			return this;
		}

		public ActionMenuItemBuilder ShowOnToolBar(bool show = true) {
			_showOnToolBar = show;
			return this;
		}

		public ActionMenuItemBuilder ExecuteOnLoad(bool execute = true) {
			_executeOnLoad = execute;
			return this;
		}

		internal ActionMenuItem Build() {
			Guard.Ensure(!string.IsNullOrEmpty(_text), "Action menu item text is required");
			Guard.Ensure(_action != null, "Action is required");
			return new ActionMenuItem(_text, _action) {
				Image16x16 = _image16x16,
				ShowOnExplorerBar = _showOnExplorerBar,
				ShowOnToolStrip = _showOnToolBar,
				ExecuteOnLoad = _executeOnLoad
			};
		}
	}
}
