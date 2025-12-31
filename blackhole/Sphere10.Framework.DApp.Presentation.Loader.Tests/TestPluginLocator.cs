// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Sphere10.Framework.DApp.Presentation.Loader.Plugins;

// ReSharper disable once CheckNamespace
namespace Sphere10.Framework.DApp.Presentation.Loader.Tests.PluginManagerTests;

internal class TestPluginLocator : IPluginLocator {
	public IEnumerable<Type> LocatePlugins() {
		return new[] { typeof(TestPlugin) };
	}
}


