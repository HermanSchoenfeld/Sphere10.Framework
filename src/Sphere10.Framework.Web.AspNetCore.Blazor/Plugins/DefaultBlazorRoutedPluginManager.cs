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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;

/// <summary>
/// Default plugin managers
/// </summary>
public class DefaultBlazorRoutedPluginManager : IBlazorRoutedPluginManager {
	private readonly IBlazorRoutedPlugin[] _plugins;

	/// <summary>
	/// Gets the plugin locator
	/// </summary>
	private IBlazorRoutedPluginLocator PluginLocator { get; }

	private ILogger<DefaultBlazorRoutedPluginManager> Logger { get; }

	/// <summary>
	/// Gets the available loaded plugins.
	/// </summary>
	public IBlazorRoutedPlugin[] Plugins => Tools.Array.Clone(_plugins);

	/// <summary>
	/// Initializes a new instance of the <see cref="DefaultBlazorRoutedPluginManager"/> class.
	/// </summary>
	/// <param name="pluginLocator"> plugin locator</param>
	/// <param name="logger"> logger</param>
	public DefaultBlazorRoutedPluginManager(IBlazorRoutedPluginLocator pluginLocator, ILogger<DefaultBlazorRoutedPluginManager> logger) {
		PluginLocator = pluginLocator;
		Logger = logger;

		IEnumerable<Type> types = PluginLocator.LocatePlugins();

		_plugins = types.Select(type => type.ActivateWithCompatibleArgs())
			.Cast<IBlazorRoutedPlugin>().ToArray();
	}

	/// <summary>
	/// Configures the service collection with services from plugins.
	/// </summary>
	public IServiceCollection ConfigureServices(IServiceCollection serviceCollection) {
		foreach (var plugin in Plugins) {
			serviceCollection = plugin.ConfigureServices(serviceCollection);
		}

		return serviceCollection;
	}
}


