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
	public void ChildrenCopyTheInputListAndReturnedArray() {
		var child = new BlazorRoutedMenuItem("Child", "/child");
		var children = new List<BlazorRoutedMenuItem> { child };
		var parent = new BlazorRoutedMenuItem("Parent", "/", children);
		children.Clear();
		var exposed = parent.Children;
		exposed[0] = null;

		Assert.That(parent.Children, Has.Length.EqualTo(1));
		Assert.That(parent.Children[0], Is.SameAs(child));
	}

	[Test]
	public void PluginMetadataArraysDoNotExposeNavigationMembership() {
		var item = new BlazorRoutedMenuItem("Item", "/item");
		var items = new[] { item };
		var page = new BlazorRoutedApplicationPage("/item", "Page", "icon", items);
		var pages = new IBlazorRoutedApplicationPage[] { page };
		var block = new BlazorRoutedApplicationBlock("Block", "icon", pages);
		var blocks = new IBlazorRoutedApplicationBlock[] { block };
		var app = new BlazorRoutedApplication("/", "App", "icon", blocks);
		items[0] = null;
		pages[0] = null;
		blocks[0] = null;
		page.MenuItems[0] = null;
		block.AppBlockPages[0] = null;
		app.AppBlocks[0] = null;

		Assert.That(app.AppBlocks[0], Is.SameAs(block));
		Assert.That(block.AppBlockPages[0], Is.SameAs(page));
		Assert.That(page.MenuItems[0], Is.SameAs(item));
	}

	[Test]
	public void MergeMenuItemsDuplicateRetainsOrig() {
		var menu1 = new List<BlazorRoutedMenuItem> {
			new("A",
				"/",
				new List<BlazorRoutedMenuItem> {
					new("B", "/", new List<BlazorRoutedMenuItem>())
				})
		};

		var menu2 = new List<BlazorRoutedMenuItem> {
			new("A",
				"/a",
				new List<BlazorRoutedMenuItem> {
					new("B", "/", new List<BlazorRoutedMenuItem>()),
					new("C", "/", new List<BlazorRoutedMenuItem>())
				}),
			new("AA",
				"/",
				new List<BlazorRoutedMenuItem> {
					new("B", "/", new List<BlazorRoutedMenuItem>())
				}),
		};

		List<BlazorRoutedMenuItem> merged = menu1.Merge(menu2).ToList();

		Assert.That(merged.Count, Is.EqualTo(2));
		Assert.That(merged[0].Heading, Is.EqualTo(menu1[0].Heading));
		Assert.That(merged[0].Route, Is.EqualTo(menu1[0].Route));
		Assert.That(merged[0].Children.Length, Is.EqualTo(2));
		Assert.That(merged[1].Children.Length, Is.EqualTo(1));
		Assert.That(menu1[0].Children, Has.Length.EqualTo(1));
		Assert.That(menu2[0].Children, Has.Length.EqualTo(2));
		merged[0].Children[0] = null;
		Assert.That(merged[0].Children[0].Heading, Is.EqualTo("B"));
	}

	[Test]
	public void CopyMenuItemsSameButDifRef() {
		var menu1 = new List<BlazorRoutedMenuItem> {
			new("A",
				"/",
				new List<BlazorRoutedMenuItem> {
					new("B", "/", new List<BlazorRoutedMenuItem>())
				})
		};

		var copy = menu1.Copy().ToList();

		Assert.That(menu1, Is.Not.SameAs(copy));
		Assert.That(menu1[0].Heading == copy[0].Heading, Is.True);
		Assert.That(menu1[0].Children[0].Heading == copy[0].Children[0].Heading, Is.True);
	}
}


