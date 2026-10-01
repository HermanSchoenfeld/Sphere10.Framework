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
/// Page selected event args
/// </summary>
public class BlazorRoutedApplicationPageSelectedEventArgs : EventArgs {
	/// <summary>
	/// Gets the selected page
	/// </summary>
	public IBlazorRoutedApplicationPage AppBlockPage { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="BlazorRoutedApplicationPageSelectedEventArgs"/> class.
	/// </summary>
	/// <param name="appBlockPage"> selected page</param>
	public BlazorRoutedApplicationPageSelectedEventArgs(IBlazorRoutedApplicationPage appBlockPage) {
		AppBlockPage = appBlockPage ?? throw new ArgumentNullException(nameof(appBlockPage));
	}
}


