// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;

/// <summary>
/// BlazorRoutedApplication manager
/// </summary>
public interface IBlazorRoutedApplicationManager {
	/// <summary>
	/// Raised when an app is selected
	/// </summary>
	event EventHandler<BlazorRoutedApplicationSelectedEventArgs> AppSelected;

	/// <summary>
	/// Raised when an app page is selected.
	/// </summary>
	event EventHandler<BlazorRoutedApplicationPageSelectedEventArgs> AppBlockPageSelected;

	/// <summary>
	/// Gets the available apps.
	/// </summary>
	IBlazorRoutedApplication[] Apps { get; }

	/// <summary>
	/// Gets or sets the selected app.
	/// </summary>
	IBlazorRoutedApplication? SelectedApp { get; }

	/// <summary>
	/// Selected page
	/// </summary>
	IBlazorRoutedApplicationPage? SelectedPage { get; }
}


