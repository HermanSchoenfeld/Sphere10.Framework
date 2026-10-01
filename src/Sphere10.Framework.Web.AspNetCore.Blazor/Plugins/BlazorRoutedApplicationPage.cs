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
/// BlazorRoutedApplication block page.
/// </summary>
public class BlazorRoutedApplicationPage : IBlazorRoutedApplicationPage {
	private readonly BlazorRoutedMenuItem[] _menuItems;

	/// <summary>
	/// Initializes a new instance of the <see cref="BlazorRoutedApplicationPage"/> class.
	/// </summary>
	/// <param name="route"> route - the relative path from app to navigate to.</param>
	/// <param name="name"> page name</param>
	/// <param name="icon"></param>
	/// <param name="menuItems"></param>
	public BlazorRoutedApplicationPage(string route, string name, string icon, IEnumerable<BlazorRoutedMenuItem> menuItems) {
		Route = route ?? throw new ArgumentNullException(nameof(route));
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Icon = icon ?? throw new ArgumentNullException(nameof(icon));
		Guard.ArgumentNotNull(menuItems, nameof(menuItems));
		_menuItems = menuItems.ToArray();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BlazorRoutedApplicationPage"/> class.
	/// </summary>
	/// <param name="route"> route - the relative path from app to navigate to.</param>
	/// <param name="name"> page name</param>
	/// <param name="icon"></param>
	public BlazorRoutedApplicationPage(string route, string name, string icon) : this(route, name, icon, new List<BlazorRoutedMenuItem>()) {
	}

	/// <summary>
	/// Gets the routable page url for this app
	/// </summary>
	public string Route { get; }

	/// <summary>
	/// Gets the name of the item, useful for displaying in menus or headings.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the icon font-awesome ccs classes for this app block.
	/// </summary>
	public string Icon { get; }

	/// <summary>
	/// Gets the menu items
	/// </summary>
	public BlazorRoutedMenuItem[] MenuItems => Tools.Array.Clone(_menuItems);
}


