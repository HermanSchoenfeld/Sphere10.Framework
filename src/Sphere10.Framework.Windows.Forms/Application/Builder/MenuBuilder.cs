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

public class MenuBuilder {
	private readonly Menu _menu;

	public MenuBuilder() {
		_menu = new Menu();
	}

	public MenuBuilder WithText(string text) {
		Guard.ArgumentNotNull(text, nameof(text));
		_menu.Text = text;
		return this;
	}

	public MenuBuilder WithImage32x32(Image image) {
		_menu.Image32x32 = image;
		return this;
	}

	public MenuBuilder ShowInMenuStrip(bool show = true) {
		_menu.ShowInMenuStrip = show;
		return this;
	}

	public MenuBuilder AddItem(IMenuItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		_menu.AddItem(item);
		return this;
	}

	public MenuBuilder AddScreenItem(string text, Type screenType, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false) {
		Guard.ArgumentNotNull(text, nameof(text));
		Guard.ArgumentNotNull(screenType, nameof(screenType));
		var item = new ScreenMenuItem(text, screenType, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
		_menu.AddItem(item);
		return this;
	}

	public MenuBuilder AddScreenItem<TScreen>(string text, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false) {
		return AddScreenItem(text, typeof(TScreen), image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
	}

	public MenuBuilder AddActionItem(string text, Action action, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool executeOnLoad = false) {
		Guard.ArgumentNotNull(text, nameof(text));
		Guard.ArgumentNotNull(action, nameof(action));
		var item = new ActionMenuItem(text, action) {
			Image16x16 = image16x16,
			ShowOnExplorerBar = showOnExplorerBar,
			ShowOnToolStrip = showOnToolBar,
			ExecuteOnLoad = executeOnLoad
		};
		_menu.AddItem(item);
		return this;
	}

	public MenuBuilder ConfigureItem(Action<MenuItemBuilder> itemBuild) {
		var itemBuilder = new MenuItemBuilder();
		itemBuild(itemBuilder);
		var item = itemBuilder.Build();
		_menu.AddItem(item);
		return this;
	}

	public Menu Build() {
		Guard.Ensure(!string.IsNullOrEmpty(_menu.Text), "Menu text is required");
		return _menu;
	}
}
