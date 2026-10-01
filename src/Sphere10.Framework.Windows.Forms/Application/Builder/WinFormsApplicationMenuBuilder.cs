// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Collections.Generic;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsApplicationMenuBuilder : ApplicationMenuBuilderBase<IWinFormsApplicationMenuItem, WinFormsApplicationMenu> {
	private WinFormsApplicationMenu _menu;
	private Image _image32x32;
	private bool _showInMenuStrip;

	public WinFormsApplicationMenuBuilder WithText(string text) {
		SetText(text);
		if (_menu != null) {
			_menu.Text = text;
			_menu.Id = Id;
		}
		return this;
	}

	public WinFormsApplicationMenuBuilder WithId(string id) {
		SetId(id);
		if (_menu != null)
			_menu.Id = id;
		return this;
	}

	public WinFormsApplicationMenuBuilder WithImage32x32(Image image) {
		_image32x32 = image;
		if (_menu != null)
			_menu.Image32x32 = image;
		return this;
	}

	public WinFormsApplicationMenuBuilder ShowInMenuStrip(bool show = true) {
		_showInMenuStrip = show;
		if (_menu != null)
			_menu.ShowInMenuStrip = show;
		return this;
	}

	public WinFormsApplicationMenuBuilder AddItem(IWinFormsApplicationMenuItem item) {
		AddItemDefinition(item);
		_menu?.AddItem(item);
		return this;
	}

	public WinFormsApplicationMenuBuilder AddScreenItem(string text, Type screenType, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false,
		string title = null) {
		Guard.ArgumentNotNull(text, nameof(text));
		Tools.UI.ValidateScreenType(screenType, typeof(WinFormsApplicationScreen));
		var item = new WinFormsScreenMenuItem(text, screenType, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen) { ScreenTitle = title };
		return AddItem(item);
	}

	public WinFormsApplicationMenuBuilder AddScreenItem<TScreen>(string text, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false,
		string title = null) where TScreen : WinFormsApplicationScreen {
		return AddScreenItem(text, typeof(TScreen), image16x16, showOnExplorerBar, showOnToolBar, isStartScreen, title);
	}

	public WinFormsApplicationMenuBuilder AddActionItem(string text, Action action, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool executeOnLoad = false) {
		Guard.ArgumentNotNull(text, nameof(text));
		Guard.ArgumentNotNull(action, nameof(action));
		var item = new WinFormsActionMenuItem(text, action) {
			Image16x16 = image16x16,
			ShowOnExplorerBar = showOnExplorerBar,
			ShowOnToolStrip = showOnToolBar,
			ExecuteOnLoad = executeOnLoad
		};
		return AddItem(item);
	}

	public WinFormsApplicationMenuBuilder ConfigureItem(Action<WinFormsApplicationMenuItemBuilder> itemBuild) {
		Guard.ArgumentNotNull(itemBuild, nameof(itemBuild));
		var itemBuilder = new WinFormsApplicationMenuItemBuilder();
		itemBuild(itemBuilder);
		var item = itemBuilder.Build();
		return AddItem(item);
	}

	public override WinFormsApplicationMenu Build() {
		if (_menu == null)
			return base.Build();
		Guard.Ensure(!string.IsNullOrEmpty(_menu.Text), "Menu text is required");
		return _menu;
	}

	protected override WinFormsApplicationMenu CreateMenu(IReadOnlyList<IWinFormsApplicationMenuItem> items) {
		if (_menu != null)
			return _menu;
		_menu = new WinFormsApplicationMenu(Text) { Id = Id, Image32x32 = _image32x32, ShowInMenuStrip = _showInMenuStrip };
		foreach (var item in items)
			_menu.AddItem(item);
		return _menu;
	}
}
