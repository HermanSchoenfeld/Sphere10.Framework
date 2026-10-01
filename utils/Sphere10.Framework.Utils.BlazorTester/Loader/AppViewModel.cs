// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Reflection;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;

namespace Sphere10.Framework.Utils.BlazorTester.Loader;

public class AppViewModel {
	private readonly Assembly[] _routingAssemblies;

	public Assembly[] RoutingAssemblies => Tools.Array.Clone(_routingAssemblies);

	public AppViewModel(IBlazorRoutedPluginLocator pluginLocator) {
		Guard.ArgumentNotNull(pluginLocator, nameof(pluginLocator));

		_routingAssemblies = pluginLocator.LocatePlugins().Select(x => x.Assembly)
			.Where(x => x.FullName != typeof(Program).Assembly.FullName)
			.Distinct().ToArray();
	}
}


