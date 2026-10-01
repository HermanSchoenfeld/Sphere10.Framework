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

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;

/// <summary>
/// Application block
/// </summary>
public class BlazorRoutedApplicationBlock : IBlazorRoutedApplicationBlock {
	private readonly IBlazorRoutedApplicationPage[] _appBlockPages;

	/// <summary>
	/// Initializes a new instance of the <see cref="BlazorRoutedApplicationBlock"/> class.
	/// </summary>
	/// <param name="name"> name</param>
	/// <param name="appBlockPages"> pages</param>
	/// <param name="icon"> icon</param>
	public BlazorRoutedApplicationBlock(string name, string icon, IEnumerable<IBlazorRoutedApplicationPage> appBlockPages) {
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Guard.ArgumentNotNull(appBlockPages, nameof(appBlockPages));
		_appBlockPages = appBlockPages.ToArray();
		Icon = icon ?? throw new ArgumentNullException(nameof(icon));
	}

	/// <summary>
	/// Gets the name of the item, useful for displaying in menus or headings.
	/// </summary>
	public string Name { get; }

	/// <inheritdoc />
	public IBlazorRoutedApplicationPage[] AppBlockPages => Tools.Array.Clone(_appBlockPages);

	/// <summary>
	/// Gets the icon font-awesome ccs classes for this app block.
	/// </summary>
	public string Icon { get; }
}


