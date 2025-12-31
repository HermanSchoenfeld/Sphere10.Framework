// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Sphere10.Framework.DApp.Presentation.Loader.Plugins;
using Sphere10.Framework.DApp.Presentation.Loader.Tests.PluginManagerTests;
using NUnit.Framework.Legacy;

namespace Sphere10.Framework.DApp.Presentation.Loader.Tests.NavigationTests;

public class DefaultPluginManagerTests {
	[Test]
	public void PluginManagerLoadCorrectPlugins() {
		IPluginLocator locator = new TestPluginLocator();
		IPluginManager manager = new DefaultPluginManager(locator, new NullLogger<DefaultPluginManager>());

		ClassicAssert.AreEqual(1, manager.Plugins.Count());
	}

	[Test]
	public void PluginManagerAddsPluginServices() {
		IPluginLocator locator = new TestPluginLocator();
		IPluginManager manager = new DefaultPluginManager(locator, new NullLogger<DefaultPluginManager>());

		var collection = new ServiceCollection();
		manager.ConfigureServices(collection);

		ClassicAssert.AreEqual(1, collection.Count);
	}
}


