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
/// BlazorRoutedApplication - contains one or more app blocks.
/// </summary>
public class BlazorRoutedApplication : IBlazorRoutedApplication {
	private readonly IBlazorRoutedApplicationBlock[] _appBlocks;

	/// <summary>
	/// Initialize a new instance of the <see cref="BlazorRoutedApplication"/> class.
	/// </summary>
	/// <param name="route"></param>
	/// <param name="name"></param>
	/// <param name="icon"></param>
	/// <param name="appBlocks"></param>
	public BlazorRoutedApplication(string route, string name, string icon, IEnumerable<IBlazorRoutedApplicationBlock> appBlocks) {
		Route = route ?? throw new ArgumentNullException(nameof(route));
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Guard.ArgumentNotNull(appBlocks, nameof(appBlocks));
		_appBlocks = appBlocks.ToArray();
		Icon = icon ?? throw new ArgumentNullException(nameof(icon));
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
	/// Gets the app blocks that are part of this 
	/// </summary>
	public IBlazorRoutedApplicationBlock[] AppBlocks => Tools.Array.Clone(_appBlocks);

	/// <summary>
	/// Gets the icon font-awesome ccs classes for this app block.
	/// </summary>
	public string Icon { get; }
}


