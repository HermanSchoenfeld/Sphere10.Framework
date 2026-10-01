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
/// Menu item view model
/// </summary>
public class BlazorRoutedMenuItem {
	private readonly BlazorRoutedMenuItem[] _children;

	/// <summary>
	/// Gets the menu heading
	/// </summary>
	public string Heading { get; }

	/// <summary>
	/// Gets the child menu items.
	/// </summary>
	public BlazorRoutedMenuItem[] Children => Tools.Array.Clone(_children);

	/// <summary>
	/// Gets the route / path that this menu item should navigate to.
	/// </summary>
	public string Route { get; }

	/// <summary>
	/// Gets the icon image path for this menu item.
	/// </summary>
	public string? IconPath { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="BlazorRoutedMenuItem"/> class.
	/// </summary>
	/// <param name="heading"></param>
	/// <param name="route"></param>
	/// <param name="children"></param>
	/// <param name="iconPath"></param>
	public BlazorRoutedMenuItem(string heading, string route, List<BlazorRoutedMenuItem> children, string? iconPath = null) {
		Heading = heading ?? throw new ArgumentNullException(nameof(heading));
		Guard.ArgumentNotNull(children, nameof(children));
		_children = children.ToArray();
		Route = route ?? throw new ArgumentNullException(nameof(route));

		IconPath = iconPath;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BlazorRoutedMenuItem"/> class.
	/// </summary>
	/// <param name="heading"></param>
	/// <param name="route"></param>
	/// <param name="iconPath"></param>
	public BlazorRoutedMenuItem(string heading, string route, string? iconPath = null) : this(heading, route, new List<BlazorRoutedMenuItem>(), iconPath) {
	}
}


