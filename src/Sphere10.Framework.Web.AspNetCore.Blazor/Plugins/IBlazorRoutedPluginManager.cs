// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;

/// <summary>
/// Manages plugins.
/// </summary>
public interface IBlazorRoutedPluginManager {
	/// <summary>
	/// Gets the currently available plugins
	/// </summary>
	IBlazorRoutedPlugin[] Plugins { get; }

	/// <summary>
	/// Configures the service collection with services from plugins.
	/// </summary>
	IServiceCollection ConfigureServices(IServiceCollection serviceCollection);
}


