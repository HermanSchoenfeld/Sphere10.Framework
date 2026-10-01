// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Utils.BlazorTester.Layouts;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DemoNavigationTests {
	[Test]
	public async Task ReturnedNavigationArraysCannotAlterLaterLinksOrSearchResults() {
		var groups = DemoNavigation.Groups;
		groups[0].Links[0] = null;
		groups[0] = null;
		var freshGroups = DemoNavigation.Groups;

		Assert.That(freshGroups[0].Title, Is.EqualTo("Overview"));
		Assert.That(freshGroups[0].Links[0].Href, Is.EqualTo("/"));
		Assert.That((await DemoNavigation.SearchAsync("Overview")).Single().Route.OriginalString, Is.EqualTo("/"));
	}

	[TestCase(" GRID ", "/components/grid")]
	[TestCase("endpoint", "/servers")]
	[TestCase("dashboard", "/legacy/dashboard")]
	public async Task SearchFindsCurrentAndOriginalDemoRoutes(string term, string expectedRoute) {
		var results = (await DemoNavigation.SearchAsync(term)).ToArray();
		Assert.That(results, Has.Length.EqualTo(1));
		Assert.That(results[0].Route.OriginalString, Is.EqualTo(expectedRoute));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("  ")]
	[TestCase("no-such-demo")]
	public async Task SearchOmitsEmptyAndUnmatchedQueries(string term) {
		Assert.That(await DemoNavigation.SearchAsync(term), Is.Empty);
	}
}
