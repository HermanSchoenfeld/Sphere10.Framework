// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader.PluginManagerTests;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader.NavigationTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class DefaultAppManagerTests {
	[Test]
	public void AppManagerLoadsPluginApps() {
		TestPlugin expected = new TestPlugin();

		IPluginLocator locator = new TestPluginLocator();
		IPluginManager pluginManager = new DefaultPluginManager(locator, new NullLogger<DefaultPluginManager>());
		IAppManager appManager = new DefaultAppManager(pluginManager, new TestNavigationManager());

		Assert.That(appManager.Apps.Count(), Is.EqualTo(expected.Apps.Count()));
	}

	[Test]
	public void AppManagerSelectsDefaultAppOrNone() {
		TestPlugin expected = new TestPlugin();

		IPluginLocator locator = new TestPluginLocator();
		IPluginManager pluginManager = new DefaultPluginManager(locator, new NullLogger<DefaultPluginManager>());
		IAppManager appManager = new DefaultAppManager(pluginManager, new TestNavigationManager());

		Assert.That(appManager.SelectedApp, Is.Not.Null);

		Assert.That(appManager.SelectedApp.Name, Is.EqualTo(expected.Apps.First().Name));
	}

	[Test]
	public void AppManagerNoSelectedAppOnBadNav() {
		var nav = new TestNavigationManager();

		IPluginLocator locator = new TestPluginLocator();
		IPluginManager pluginManager = new DefaultPluginManager(locator, new NullLogger<DefaultPluginManager>());
		IAppManager appManager = new DefaultAppManager(pluginManager, nav);

		nav.NavigateTo(nav.Uri + "unknown");

		Assert.That(appManager.SelectedApp, Is.Null);
	}

	[Test]
	public void NavToApp() {
		var nav = new TestNavigationManager();

		IPluginLocator locator = new TestPluginLocator();
		IPluginManager pluginManager = new DefaultPluginManager(locator, new NullLogger<DefaultPluginManager>());
		IAppManager appManager = new DefaultAppManager(pluginManager, nav);

		var app = appManager.Apps.First(x => x.Name != appManager.SelectedApp?.Name);

		nav.NavigateTo(app.Route);

		Assert.That(appManager.SelectedApp, Is.Not.Null);
		Assert.That(appManager.SelectedApp.Name, Is.EqualTo(app.Name));
	}

	[Test]
	public void NavToAppPage() {
		var nav = new TestNavigationManager();

		IPluginLocator locator = new TestPluginLocator();
		IPluginManager pluginManager = new DefaultPluginManager(locator, new NullLogger<DefaultPluginManager>());
		IAppManager appManager = new DefaultAppManager(pluginManager, nav);

		IApp app = appManager.Apps.First(x => x.Name != appManager.SelectedApp?.Name);
		IAppBlockPage page = app.AppBlocks.First().AppBlockPages.First();

		nav.NavigateTo(page.Route);

		Assert.That(appManager.SelectedApp, Is.Not.Null);
		Assert.That(appManager.SelectedPage, Is.Not.Null);
		Assert.That(appManager.SelectedApp.Name, Is.EqualTo(app.Name));
		Assert.That(appManager.SelectedPage.Name, Is.EqualTo(page.Name));
	}
}


