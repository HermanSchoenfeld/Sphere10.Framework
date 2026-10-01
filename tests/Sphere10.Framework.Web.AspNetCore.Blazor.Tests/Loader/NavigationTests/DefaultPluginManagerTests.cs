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
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader.PluginManagerTests;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader.NavigationTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class DefaultPluginManagerTests {
	[Test]
	public void PluginManagerLoadCorrectPlugins() {
		IBlazorRoutedPluginLocator locator = new TestPluginLocator();
		IBlazorRoutedPluginManager manager = new DefaultBlazorRoutedPluginManager(locator, new NullLogger<DefaultBlazorRoutedPluginManager>());

		Assert.That(manager.Plugins.Length, Is.EqualTo(1));
	}

	[Test]
	public void PluginManagerAddsPluginServices() {
		IBlazorRoutedPluginLocator locator = new TestPluginLocator();
		IBlazorRoutedPluginManager manager = new DefaultBlazorRoutedPluginManager(locator, new NullLogger<DefaultBlazorRoutedPluginManager>());

		var collection = new ServiceCollection();
		manager.ConfigureServices(collection);

		using var provider = collection.BuildServiceProvider();
		Assert.That(provider.GetRequiredService<TestViewModel>(), Is.Not.Null);
	}
}


