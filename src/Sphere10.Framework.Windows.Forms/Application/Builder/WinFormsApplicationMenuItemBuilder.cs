// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsApplicationMenuItemBuilder {
	private object _specificBuilder;

	public WinFormsScreenMenuItemBuilder AsScreenItem() {
		_specificBuilder = new WinFormsScreenMenuItemBuilder();
		return (WinFormsScreenMenuItemBuilder)_specificBuilder;
	}

	public WinFormsActionMenuItemBuilder AsActionItem() {
		_specificBuilder = new WinFormsActionMenuItemBuilder();
		return (WinFormsActionMenuItemBuilder)_specificBuilder;
	}

	public IWinFormsApplicationMenuItem Build() {
		Guard.Ensure(_specificBuilder != null, "Menu item type is not configured");
		return _specificBuilder is WinFormsScreenMenuItemBuilder screenBuilder
			? screenBuilder.Build()
			: ((WinFormsActionMenuItemBuilder)_specificBuilder).Build();
	}

	public class WinFormsScreenMenuItemBuilder : ApplicationMenuItemBuilderBase {
		private Image _image16x16;
		private bool _showOnExplorerBar = true;
		private bool _showOnToolBar = true;
		private bool _isStartScreen = false;

		public WinFormsScreenMenuItemBuilder WithText(string text) {
			SetText(text);
			return this;
		}

		public WinFormsScreenMenuItemBuilder WithScreen(Type screenType) {
			SetScreenType(screenType);
			return this;
		}

		public WinFormsScreenMenuItemBuilder WithScreen<TScreen>() where TScreen : WinFormsApplicationScreen {
			return WithScreen(typeof(TScreen));
		}

		/// <summary>Declares one instance for this screen type across all of the host's blocks and menus.</summary>
		public WinFormsScreenMenuItemBuilder AsSingleInstance() {
			SetActivationMode(ScreenActivationMode.SingleInstance);
			return this;
		}

		/// <summary>Declares that every activation of this screen type creates a new instance.</summary>
		public WinFormsScreenMenuItemBuilder AsMultiInstance() {
			SetActivationMode(ScreenActivationMode.MultiInstance);
			return this;
		}

		/// <summary>Opens one anchored instance during application startup.</summary>
		public WinFormsScreenMenuItemBuilder AsPermanentSingleton() {
			SetActivationMode(ScreenActivationMode.PermanentSingleton);
			return this;
		}

		public WinFormsScreenMenuItemBuilder WithScreenKind(ScreenKind screenKind) {
			SetScreenKind(screenKind);
			return this;
		}

		public WinFormsScreenMenuItemBuilder AsDefault(bool isDefault = true) {
			SetIsDefault(isDefault);
			return this;
		}

		public WinFormsScreenMenuItemBuilder WithTitle(string Title) {
			Guard.ArgumentNotNullOrEmpty(Title, nameof(Title));
			SetScreenTitle(Title);
			return this;
		}

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

		protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(WinFormsApplicationScreen));

		internal WinFormsScreenMenuItem Build() {
			ValidateItem();
			return new WinFormsScreenMenuItem(Text, ScreenType, _image16x16, _showOnExplorerBar, _showOnToolBar, _isStartScreen) {
				Id = Id,
				ActivationMode = ActivationMode,
				ScreenTitle = ScreenTitle,
				ScreenKind = ScreenKind,
				IsDefault = IsDefault
			};
		}
	}

	public class WinFormsActionMenuItemBuilder : ApplicationMenuItemBuilderBase {
		private Image _image16x16;
		private bool _showOnExplorerBar = true;
		private bool _showOnToolBar = true;
		private bool _executeOnLoad = false;

		public WinFormsActionMenuItemBuilder WithText(string text) {
			SetText(text);
			return this;
		}

		public WinFormsActionMenuItemBuilder WithAction(Action action) {
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

		internal WinFormsActionMenuItem Build() {
			ValidateItem();
			return new WinFormsActionMenuItem(Text, Action) {
				Id = Id,
				Image16x16 = _image16x16,
				ShowOnExplorerBar = _showOnExplorerBar,
				ShowOnToolStrip = _showOnToolBar,
				ExecuteOnLoad = _executeOnLoad
			};
		}
	}
}
