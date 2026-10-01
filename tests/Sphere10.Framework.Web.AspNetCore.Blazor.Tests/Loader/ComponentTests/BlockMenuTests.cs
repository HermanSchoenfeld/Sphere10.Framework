// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;
using Sphere10.Framework.Utils.BlazorTester.Loader.Components;
using Sphere10.Framework.Utils.BlazorTester.Loader.ViewModels;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader.PluginManagerTests;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader.ComponentTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlockMenuTests {
	[Test]
	public async Task BlockMenuRendersSelectedApplicationLinks() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/app1/page1"));
		services.AddTransient<BlockMenuViewModel>();
		services.AddScoped<IAppManager, DefaultAppManager>();
		services.AddScoped<IPluginManager, DefaultPluginManager>();
		services.AddScoped<IPluginLocator, TestPluginLocator>();
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var html = await renderer.Dispatcher.InvokeAsync(async () => {
			var component = await renderer.RenderComponentAsync<BlockMenu>();
			return component.ToHtmlString();
		});
		Assert.That(html, Does.Contain("app1 page"));
		Assert.That(html, Does.Contain("/app1/page1"));
	}
}
