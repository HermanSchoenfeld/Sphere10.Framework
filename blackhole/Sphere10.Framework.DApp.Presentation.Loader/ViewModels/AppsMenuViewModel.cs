// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Sphere10.Framework.DApp.Presentation.Loader.Plugins;
using Sphere10.Framework.DApp.Presentation.Plugins;
using Sphere10.Framework.DApp.Presentation.ViewModels;

namespace Sphere10.Framework.DApp.Presentation.Loader.ViewModels;

/// <summary>
/// Apps menu view model.
/// </summary>
public class AppsMenuViewModel : ComponentViewModelBase {
	/// <summary>
	/// Gets the available apps.zs
	/// </summary>
	public IEnumerable<IApp> Apps => AppManager.Apps;

	/// <summary>
	/// Gets the selected app
	/// </summary>
	public IApp? SelectedApp { get; private set; }

	/// <summary>
	/// Gets the navigation manager
	/// </summary>
	private IAppManager AppManager { get; }

	/// <summary>
	/// Initialize an instance of the <see cref="AppsMenuViewModel"/> class.
	/// </summary>
	/// <param name="appManager"></param>
	public AppsMenuViewModel(
		IAppManager appManager) {
		AppManager = appManager ?? throw new ArgumentNullException(nameof(appManager));

		AppManager.AppSelected += AppManagerOnAppSelected;
		SelectedApp = appManager.SelectedApp;

		StateHasChangedDelegate?.Invoke();
	}

	/// <summary>
	/// Handles the app selected event, updates the list to reflected the new selected app.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void AppManagerOnAppSelected(object? sender, AppSelectedEventArgs e) {
		SelectedApp = e.SelectedApp;
		StateHasChangedDelegate?.Invoke();
	}
}


