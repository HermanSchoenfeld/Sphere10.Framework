// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.
using System;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationCommandMergeTests {
	[Test]
	public void NarrowerCommandsOverrideInPlaceAndHelpRemainsLastWithoutMutatingSources() {
		var original = new ApplicationMenuItem { Id = "save", Title = "Application save" };
		var screenSave = new ApplicationMenuItem { Id = "save", Title = "Screen save" };
		var blockAction = new ApplicationMenuItem { Id = "block", Title = "Block action" };
		var extra = new ApplicationMenuItem { Id = "extra", Title = "Screen action" };
		var app = new[] { Menu("work", original), Menu("help", new ApplicationMenuItem { Id = "help", Title = "Help" }) };
		var block = new[] { Menu("work", blockAction) };
		var screen = new[] { Menu("work", screenSave, extra), Menu("screen", extra) };

		var merged = Tools.UI.MergeMenus<ApplicationMenu<ApplicationMenuItem>, ApplicationMenuItem>(
			(menu, items) => Menu(menu.Id, items), app, block, screen);
		Assert.That(merged.Select(menu => menu.Id), Is.EqualTo(new[] { "work", "screen", "help" }));
		Assert.That(merged[0].Items, Is.EqualTo(new[] { screenSave, blockAction, extra }));
		Assert.That(app[0].Items.Single(), Is.SameAs(original));
		merged[0].RemoveItem(screenSave);
		Assert.That(screen[0].Items, Has.Length.EqualTo(2));
		Assert.That(Tools.UI.MergeMenus<ApplicationMenu<ApplicationMenuItem>, ApplicationMenuItem>(
			(menu, items) => Menu(menu.Id, items), app, block)[0].Items, Is.EqualTo(new[] { original, blockAction }));
	}

	[Test]
	public void SeparatorsAreNormalizedAndOverridingDoesNotDuplicateCommands() {
		var first = new ApplicationMenuItem { Id = "first", Title = "First" };
		var replacement = new ApplicationMenuItem { Id = "first", Title = "Replacement" };
		var last = new ApplicationMenuItem { Id = "last", Title = "Last" };
		var separator = new Separator();
		var merged = Tools.UI.MergeMenuItems<IApplicationMenuItem>(
			new IApplicationMenuItem[] { separator, first, separator, separator },
			new IApplicationMenuItem[] { replacement, separator, last, separator });
		Assert.That(merged, Is.EqualTo(new IApplicationMenuItem[] { replacement, separator, last }));
		merged[0] = last;
		Assert.That(first.Title, Is.EqualTo("First"));
		Assert.That(Tools.UI.MergeMenuItems<IApplicationMenuItem>(new IApplicationMenuItem[] { separator }), Is.Empty);
	}

	[Test]
	public void InvalidCommandLayersFailBeforeRendering() {
		Assert.That(() => Tools.UI.MergeMenuItems<IApplicationMenuItem>(null), Throws.ArgumentNullException);
		Assert.That(() => Tools.UI.MergeMenuItems(new IApplicationMenuItem[] { null }), Throws.ArgumentNullException);
		Assert.That(() => Tools.UI.MergeMenuItems(new[] { new ApplicationMenuItem { Id = "", Title = "Invalid" } }), Throws.ArgumentException);
	}

	private static ApplicationMenu<ApplicationMenuItem> Menu(string id, params ApplicationMenuItem[] items) {
		var menu = new ApplicationMenu<ApplicationMenuItem> { Id = id, Text = id };
		foreach (var item in items)
			menu.AddItem(item);
		return menu;
	}

	private sealed class Separator : IApplicationMenuSeparator {
		public string Id => "separator";

		public string Title => "Separator";
	}
}