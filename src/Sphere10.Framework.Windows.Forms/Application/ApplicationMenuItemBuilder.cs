// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class ApplicationMenuItemBuilder : IApplicationUIBuilder<IApplicationMenuItem> {
	private IApplicationUIBuilder<IApplicationMenuItem> _specificBuilder;

	public WinFormsScreenMenuItemBuilder AsScreenItem() {
		_specificBuilder = new WinFormsScreenMenuItemBuilder();
		return (WinFormsScreenMenuItemBuilder)_specificBuilder;
	}

	public WinFormsActionMenuItemBuilder AsActionItem() {
		_specificBuilder = new WinFormsActionMenuItemBuilder();
		return (WinFormsActionMenuItemBuilder)_specificBuilder;
	}

	public IApplicationMenuItem Build() {
		Guard.Ensure(_specificBuilder != null, "Menu item type is not configured");
		return _specificBuilder.Build();
	}

	public class WinFormsScreenMenuItemBuilder : ApplicationMenuItemBuilderBase<WinFormsScreenMenuItemBuilder>, IApplicationUIBuilder<WinFormsScreenMenuItem> {
		private Image _image16x16;
		private bool _showOnExplorerBar = true;
		private bool _showOnToolBar = true;
		private bool _isStartScreen = false;

		public WinFormsScreenMenuItemBuilder WithImage(Image image16x16) {
			_image16x16 = image16x16;
			return this;
		}

		public WinFormsScreenMenuItemBuilder ShowOnExplorerBar(bool show = true) {
			_showOnExplorerBar = show;
			return this;
		}

		public WinFormsScreenMenuItemBuilder ShowOnToolBar(bool show = true) {
			_showOnToolBar = show;
			return this;
		}

		public WinFormsScreenMenuItemBuilder IsStartScreen(bool isStart = true) {
			_isStartScreen = isStart;
			return this;
		}

		public WinFormsScreenMenuItem Build() {
			ValidateItem();
			Guard.Ensure(ScreenType != null, "A native screen item requires a screen.");
			return new WinFormsScreenMenuItem(ScreenRegistration, Text, _image16x16, _showOnExplorerBar, _showOnToolBar, _isStartScreen);
		}

		protected override void ValidateScreenType(Type screenType) => ApplicationHelper.ValidateScreenType(screenType, typeof(ApplicationScreen));
	}

	public class WinFormsActionMenuItemBuilder : ApplicationMenuItemBuilderBase<WinFormsActionMenuItemBuilder>, IApplicationUIBuilder<WinFormsActionMenuItem> {
		private Image _image16x16;
		private bool _showOnExplorerBar = true;
		private bool _showOnToolBar = true;
		private bool _executeOnLoad = false;

		public WinFormsActionMenuItemBuilder WithAction(Action<IUserInterfaceServices> action) {
			SetAction(action);
			return this;
		}

		public WinFormsActionMenuItemBuilder WithImage(Image image16x16) {
			_image16x16 = image16x16;
			return this;
		}

		public WinFormsActionMenuItemBuilder ShowOnExplorerBar(bool show = true) {
			_showOnExplorerBar = show;
			return this;
		}

		public WinFormsActionMenuItemBuilder ShowOnToolBar(bool show = true) {
			_showOnToolBar = show;
			return this;
		}

		public WinFormsActionMenuItemBuilder ExecuteOnLoad(bool execute = true) {
			_executeOnLoad = execute;
			return this;
		}

		public WinFormsActionMenuItem Build() {
			ValidateItem();
			Guard.Ensure(ScreenType == null && (Action != null || UIAction != null) && AsyncAction == null, "A native action item requires a synchronous action.");
			var item = UIAction != null ? new WinFormsActionMenuItem(Text, UIAction) : new WinFormsActionMenuItem(Text, Action);
			item.Image16x16 = _image16x16;
			item.ShowOnExplorerBar = _showOnExplorerBar;
			item.ShowOnToolStrip = _showOnToolBar;
			item.ExecuteOnLoad = _executeOnLoad;
			return item;
		}
	}
}
