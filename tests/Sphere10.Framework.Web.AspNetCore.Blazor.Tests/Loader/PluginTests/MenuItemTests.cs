// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;
using Assert = NUnit.Framework.Assert;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader.PluginTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class MenuItemTests {
	[Test]
	public void MergeMenuItemsDuplicateRetainsOrig() {
		var menu1 = new List<MenuItem> {
			new("A",
				"/",
				new List<MenuItem> {
					new("B", "/", new List<MenuItem>())
				})
		};

		var menu2 = new List<MenuItem> {
			new("A",
				"/a",
				new List<MenuItem> {
					new("B", "/", new List<MenuItem>()),
					new("C", "/", new List<MenuItem>())
				}),
			new("AA",
				"/",
				new List<MenuItem> {
					new("B", "/", new List<MenuItem>())
				}),
		};

		List<MenuItem> merged = menu1.Merge(menu2).ToList();

		Assert.That(merged.Count, Is.EqualTo(2));
		Assert.That(merged[0].Heading, Is.EqualTo(menu1[0].Heading));
		Assert.That(merged[0].Route, Is.EqualTo(menu1[0].Route));
		Assert.That(merged[0].Children.Count, Is.EqualTo(2));
		Assert.That(merged[1].Children.Count, Is.EqualTo(1));
	}

	[Test]
	public void CopyMenuItemsSameButDifRef() {
		var menu1 = new List<MenuItem> {
			new("A",
				"/",
				new List<MenuItem> {
					new("B", "/", new List<MenuItem>())
				})
		};

		var copy = menu1.Copy().ToList();

		Assert.That(menu1, Is.Not.SameAs(copy));
		Assert.That(menu1[0].Heading == copy[0].Heading, Is.True);
		Assert.That(menu1[0].Children[0].Heading == copy[0].Children[0].Heading, Is.True);
	}
}


