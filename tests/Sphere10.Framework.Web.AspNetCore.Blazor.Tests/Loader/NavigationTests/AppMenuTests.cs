// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader.PluginManagerTests;
using Sphere10.Framework.Utils.BlazorTester.Loader.ViewModels;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader.NavigationTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class AppMenuTests {
	[Test]
	public void AppMenuInitializedWithApps() {
		IPluginLocator locator = new TestPluginLocator();
		IPluginManager pluginManager = new DefaultPluginManager(locator, new NullLogger<DefaultPluginManager>());
		var navigationManager = new TestNavigationManager();
		IAppManager appManager = new DefaultAppManager(pluginManager, navigationManager);
		AppsMenuViewModel appsMenuViewModel = new AppsMenuViewModel(appManager);
		BlockMenuViewModel blockMenuViewModel = new BlockMenuViewModel(appManager);

		navigationManager.NavigateTo("/");

		Assert.That(appsMenuViewModel.Apps, Is.SameAs(appManager.Apps));
		Assert.That(appsMenuViewModel.SelectedApp, Is.SameAs(appManager.SelectedApp));
		Assert.That(blockMenuViewModel.AppBlocks, Is.SameAs(appManager.SelectedApp?.AppBlocks));
	}
}


