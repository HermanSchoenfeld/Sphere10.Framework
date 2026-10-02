// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Utils.BlazorTester;
using Sphere10.Framework.Utils.BlazorTester.Layouts;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DemoNavigationTests {
	[Test]
	public void ReturnedAliasArraysCannotAlterLaterResolution() {
		var aliases = DemoNavigation.Aliases;
		aliases[0] = null;
		var root = DemoNavigation.ResolveAlias("/");
		Assert.That(root.BlockId, Is.EqualTo("workspace"));
		Assert.That(root.ScreenId, Is.EqualTo("overview"));
		Assert.That(DemoNavigation.Aliases[0], Is.Not.Null);
	}

	[TestCase("/components/grid", "components", "gallery")]
	[TestCase("/MODERN/", "components", "gallery")]
	[TestCase("/widget-gallery/wizards", "legacy", "wizards")]
	[TestCase("/servers", "legacy", "servers")]
	public void CompatibilityAliasesSelectStableRegisteredScreenIds(string path, string blockId, string screenId) {
		var alias = DemoNavigation.ResolveAlias(path);
		Assert.That(alias, Is.Not.Null);
		Assert.That(alias.BlockId, Is.EqualTo(blockId));
		Assert.That(alias.ScreenId, Is.EqualTo(screenId));
	}

	[TestCase("/application")]
	[TestCase("/not-found")]
	[TestCase("/unknown")]
	public void CanonicalAndUnknownPathsDoNotOverrideQuerySelections(string path) => Assert.That(DemoNavigation.ResolveAlias(path), Is.Null);

	[TestCase(" GRID ", "application?block=components&screen=gallery")]
	[TestCase("endpoint", "application?block=legacy&screen=servers")]
	[TestCase("dashboard", "application?block=legacy&screen=dashboard")]
	public async Task SearchFindsRegisteredScreensWithCanonicalApplicationUrls(string term, string expectedRoute) {
		using var provider = CreateProvider();
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var results = (await DemoNavigation.SearchAsync(host, term)).ToArray();
		Assert.That(results, Has.Length.EqualTo(1));
		Assert.That(results[0].Route.OriginalString, Is.EqualTo(expectedRoute));
	}

	[Test]
	public async Task SearchTracksTheHostsRegisteredBlocksInsteadOfASeparateNavigationList() {
		using var provider = CreateProvider();
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		Assert.That(await DemoNavigation.SearchAsync(host, "grid"), Is.Not.Empty);
		await host.UnregisterBlockAsync("components");
		Assert.That(await DemoNavigation.SearchAsync(host, "grid"), Is.Empty);
		await host.RegisterBlockAsync("components");
		Assert.That((await DemoNavigation.SearchAsync(host, "grid")).Single().Route.OriginalString, Is.EqualTo("application?block=components&screen=gallery"));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("  ")]
	[TestCase("no-such-demo")]
	public async Task SearchOmitsEmptyAndUnmatchedQueries(string term) {
		using var provider = CreateProvider();
		Assert.That(await DemoNavigation.SearchAsync(provider.GetRequiredService<IBlazorApplicationScreenHost>(), term), Is.Empty);
	}

	private static ServiceProvider CreateProvider() {
		var services = new ServiceCollection().AddLogging();
		Program.ConfigureServices(services);
		services.AddSingleton<NavigationManager>(new TestNavigationManager());
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		return services.BuildServiceProvider();
	}
}
