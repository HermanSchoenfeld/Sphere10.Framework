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
/// Block menu view model
/// </summary>
public class BlockMenuViewModel : ComponentViewModelBase, IDisposable {
	/// <summary>
	/// Gets the app manager
	/// </summary>
	private IBlazorRoutedApplicationManager AppManager { get; }

	/// <summary>
	/// Gets or sets the selected app.
	/// </summary>
	public IBlazorRoutedApplication SelectedApp { get; set; }

	/// <summary>
	/// Gets or sets the selected app block.
	/// </summary>
	public IBlazorRoutedApplicationBlock SelectedAppBlock { get; set; }

	/// <summary>
	/// Gets the app blocks for the selected app
	/// </summary>
	public IBlazorRoutedApplicationBlock[] AppBlocks => SelectedApp?.AppBlocks ?? Array.Empty<IBlazorRoutedApplicationBlock>();

	/// <summary>
	/// Initializes a new instance of the <see cref="BlockMenuViewModel"/> class.
	/// </summary>
	/// <param name="appManager"></param>
	public BlockMenuViewModel(IBlazorRoutedApplicationManager appManager) {
		Guard.ArgumentNotNull(appManager, nameof(appManager));
		AppManager = appManager;
		AppManager.AppSelected += AppManagerOnAppSelected;
		AppManager.AppBlockPageSelected += AppManagerOnAppBlockPageSelected;

		SelectedApp = appManager.SelectedApp;
		SelectedAppBlock = appManager.SelectedApp?.AppBlocks.FirstOrDefault(x =>
			x.AppBlockPages.Any(y => y.Route == appManager.SelectedPage?.Route));

		StateHasChangedDelegate?.Invoke();
	}

	/// <summary>
	/// Handles page selected event.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void AppManagerOnAppBlockPageSelected(object sender, BlazorRoutedApplicationPageSelectedEventArgs e) {
		SelectedAppBlock = AppManager.SelectedApp.AppBlocks.First(x =>
			x.AppBlockPages.Any(y => y.Route == e.AppBlockPage.Route));
		StateHasChangedDelegate?.Invoke();
	}

	/// <summary>
	/// Handles app selected event
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void AppManagerOnAppSelected(object sender, BlazorRoutedApplicationSelectedEventArgs e) {
		SelectedApp = e.SelectedApp;
		StateHasChangedDelegate?.Invoke();
	}
	public void Dispose() {
		AppManager.AppSelected -= AppManagerOnAppSelected;
		AppManager.AppBlockPageSelected -= AppManagerOnAppBlockPageSelected;
	}

}


