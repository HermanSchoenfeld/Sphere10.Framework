// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Utils.BlazorTester.Loader.ViewModels;

/// <summary>
/// View model for topbar menu
/// </summary>
public class MainMenuViewModel : ComponentViewModelBase, IDisposable {
	private BlazorRoutedMenuItem[] _menuItems;

	/// <summary>
	/// Gets the app manager
	/// </summary>
	private IBlazorRoutedApplicationManager AppManager { get; }

	/// <summary>
	/// Gets the default menu items
	/// </summary>
	private BlazorRoutedMenuItem[] DefaultMenuItems { get; }

	/// <summary>
	/// Gets the list of menu items.
	/// </summary>
	public BlazorRoutedMenuItem[] MenuItems => Tools.Array.Clone(_menuItems);

	/// <summary>
	/// Initializes a new instance of the <see cref="MainMenuViewModel"/> class.
	/// </summary>
	/// <param name="appManager"> app manager</param>
	public MainMenuViewModel(IBlazorRoutedApplicationManager appManager) {
		Guard.ArgumentNotNull(appManager, nameof(appManager));
		AppManager = appManager;
		DefaultMenuItems = new BlazorRoutedMenuItem[] {
			new BlazorRoutedMenuItem("File", "/", iconPath: "fa-list"),
			new("Help", "/", new List<BlazorRoutedMenuItem>(), "fa-info")
		};

		_menuItems = Tools.Array.Clone(DefaultMenuItems);

		AppManager.AppBlockPageSelected += AppManagerOnAppBlockPageSelected;

		if (AppManager.SelectedPage is not null) {
			_menuItems = DefaultMenuItems.Merge(AppManager.SelectedPage.MenuItems).ToArray();
		}
	}

	/// <summary>
	/// Handles selection of app, updates menu with new app's menu items.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void AppManagerOnAppBlockPageSelected(object sender, BlazorRoutedApplicationPageSelectedEventArgs e) {
		_menuItems = DefaultMenuItems.Merge(e.AppBlockPage.MenuItems).ToArray();
		StateHasChangedDelegate?.Invoke();
	}
	public void Dispose() {
		AppManager.AppBlockPageSelected -= AppManagerOnAppBlockPageSelected;
	}

}


