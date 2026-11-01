// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsApplicationMenuBuilder : ApplicationMenuBuilderBase<WinFormsApplicationMenuBuilder, IWinFormsApplicationMenuItem, WinFormsApplicationMenuItemBuilder, WinFormsApplicationMenu> {
	private WinFormsApplicationMenu _menu;
	private Image _image32x32;
	private bool _showInMenuStrip;

	/// <summary>Adds an action using optional inline native presentation.</summary>
	public WinFormsApplicationMenuBuilder AddItem(string text, Action action, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool executeOnLoad = false) {
		return AddItem(text, action, out _, image16x16, showOnExplorerBar, showOnToolBar, executeOnLoad);
	}

	/// <summary>Adds an action and captures its configured command definition.</summary>
	public WinFormsApplicationMenuBuilder AddItem(string text, Action action, out IWinFormsApplicationMenuItem registration,
		Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool executeOnLoad = false) {
		Guard.ArgumentNotNull(text, nameof(text));
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Ensure(!string.IsNullOrWhiteSpace(text), "Menu item text is required.");
		return AddItem(new WinFormsActionMenuItem(text, image16x16, action) {
			ShowOnExplorerBar = showOnExplorerBar,
			ShowOnToolStrip = showOnToolBar,
			ExecuteOnLoad = executeOnLoad
		}, out registration);
	}

	/// <summary>Adds an action using optional inline native presentation.</summary>
	public WinFormsApplicationMenuBuilder AddItem(string text, Action<IUserInterfaceServices> action, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool executeOnLoad = false) {
		return AddItem(text, action, out _, image16x16, showOnExplorerBar, showOnToolBar, executeOnLoad);
	}

	/// <summary>Adds an action and captures its configured command definition.</summary>
	public WinFormsApplicationMenuBuilder AddItem(string text, Action<IUserInterfaceServices> action, out IWinFormsApplicationMenuItem registration,
		Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool executeOnLoad = false) {
		Guard.ArgumentNotNull(text, nameof(text));
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Ensure(!string.IsNullOrWhiteSpace(text), "Menu item text is required.");
		return AddItem(new WinFormsActionMenuItem(text, image16x16, action) {
			ShowOnExplorerBar = showOnExplorerBar,
			ShowOnToolStrip = showOnToolBar,
			ExecuteOnLoad = executeOnLoad
		}, out registration);
	}

	/// <summary>Adds navigation for the owning block's first matching screen registration.</summary>
	public WinFormsApplicationMenuBuilder AddItem<TScreen>(string text = null, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false)
		where TScreen : WinFormsApplicationScreen {
		return AddItem(ApplicationScreenRegistration.ForType(typeof(TScreen)), text, out _, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
	}

	/// <summary>Adds typed navigation and captures its configured command definition.</summary>
	public WinFormsApplicationMenuBuilder AddItem<TScreen>(string text, out IWinFormsApplicationMenuItem registration,
		Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false)
		where TScreen : WinFormsApplicationScreen {
		return AddItem(ApplicationScreenRegistration.ForType(typeof(TScreen)), text, out registration, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
	}

	/// <summary>Adds navigation without creating or changing a screen registration.</summary>
	public WinFormsApplicationMenuBuilder AddItem(Type screenType, string text = null, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false) {
		return AddItem(screenType, text, out _, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
	}

	/// <summary>Adds navigation and captures its configured command definition.</summary>
	public WinFormsApplicationMenuBuilder AddItem(Type screenType, string text, out IWinFormsApplicationMenuItem registration,
		Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false) {
		return AddItem(ApplicationScreenRegistration.ForType(screenType), text, out registration, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
	}

	/// <summary>Adds navigation without creating or changing a screen registration.</summary>
	public WinFormsApplicationMenuBuilder AddItem(ApplicationScreenRegistration screen, string text = null, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false) {
		return AddItem(screen, text, out _, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
	}

	/// <summary>Adds navigation and captures its configured command definition.</summary>
	public WinFormsApplicationMenuBuilder AddItem(ApplicationScreenRegistration screen, string text, out IWinFormsApplicationMenuItem registration,
		Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false) {
		return AddItem(new WinFormsScreenMenuItem(screen, text, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen), out registration);
	}

	/// <summary>Adds navigation without creating or changing a screen registration.</summary>
	public WinFormsApplicationMenuBuilder AddItem(ApplicationScreenDefinition definition, string text = null, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false) {
		return AddItem(definition, text, out _, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
	}

	/// <summary>Adds navigation and captures its configured command definition.</summary>
	public WinFormsApplicationMenuBuilder AddItem(ApplicationScreenDefinition definition, string text, out IWinFormsApplicationMenuItem registration,
		Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		return AddItem(definition.Registration, text, out registration, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
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

	/// <summary>Adds a command for the first matching screen registration in the owning block.</summary>
	public WinFormsApplicationMenuBuilder AddShowScreenItem<TScreen>(string text = null, Image image16x16 = null, bool showOnExplorerBar = true,
		bool showOnToolBar = true, bool isStartScreen = false)
		where TScreen : WinFormsApplicationScreen {
		return AddShowScreenItem(ApplicationScreenRegistration.ForType(typeof(TScreen)), text, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
	}

	/// <summary>Adds a runtime-type command for the first matching registration in the owning block.</summary>
	public WinFormsApplicationMenuBuilder AddShowScreenItem(Type screenType, string text = null, Image image16x16 = null, bool showOnExplorerBar = true,
		bool showOnToolBar = true, bool isStartScreen = false) {
		return AddShowScreenItem(ApplicationScreenRegistration.ForType(screenType), text, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
	}

	/// <summary>References one exact registration without changing its activation metadata.</summary>
	public WinFormsApplicationMenuBuilder AddShowScreenItem(ApplicationScreenRegistration registration, string text = null, Image image16x16 = null,
		bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false) {
		return AddItem(registration, text, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
	}

	/// <summary>References an existing block-owned definition without adding or changing its metadata.</summary>
	public WinFormsApplicationMenuBuilder AddShowScreenItem(ApplicationScreenDefinition definition, string text = null, Image image16x16 = null,
		bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		return AddShowScreenItem(definition.Registration, text, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen);
	}

	public WinFormsApplicationMenuBuilder AddActionItem(string text, Action action, Image image16x16 = null, bool showOnExplorerBar = true, bool showOnToolBar = true, bool executeOnLoad = false) {
		return AddItem(text, action, image16x16, showOnExplorerBar, showOnToolBar, executeOnLoad);
	}

	/// <summary>Passes the executing UI services; PrimaryUIController supplies the native modal owner.</summary>
	public WinFormsApplicationMenuBuilder AddActionItem(string text, Action<IUserInterfaceServices> action, Image image16x16 = null,
		bool showOnExplorerBar = true, bool showOnToolBar = true, bool executeOnLoad = false
	) {
		return AddItem(text, action, image16x16, showOnExplorerBar, showOnToolBar, executeOnLoad);
	}

	public override WinFormsApplicationMenu Build() {
		if (_menu == null)
			return base.Build();
		Guard.Ensure(!string.IsNullOrEmpty(_menu.Text), "Menu text is required");
		return _menu;
	}

	protected override void OnConfigurationChanged(string propertyName) {
		if (_menu == null)
			return;
		switch (propertyName) {
			case nameof(Text):
				_menu.Text = Text;
				break;
		}
	}

	protected override void OnItemAdded(IWinFormsApplicationMenuItem item) => _menu?.AddItem(item);

	protected override WinFormsApplicationMenu CreateMenu(IReadOnlyList<IWinFormsApplicationMenuItem> items) {
		if (_menu != null)
			return _menu;
		_menu = new WinFormsApplicationMenu(Text) { Image32x32 = _image32x32, ShowInMenuStrip = _showInMenuStrip };
		foreach (var item in items)
			_menu.AddItem(item);
		return _menu;
	}
}
