// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ScreenCommandTests {

	[Test]
	public void PlacedCommandsBecomeTheFileMenuTheRecordMenuAndTheToolbar() {
		var screen = new CommandScreen();
		var menus = screen.Menus;
		Assert.That(menus.Select(menu => menu.Id), Is.EqualTo(new[] { BlazorUI.Application.FileMenuId, "document" }));
		Assert.That(menus[0].Text, Is.EqualTo("File"));
		Assert.That(Describe(menus[0].Items), Is.EqualTo(new[] { "document-new", "document-save" }));
		Assert.That(menus[1].Text, Is.EqualTo("Document"));
		Assert.That(Describe(menus[1].Items), Is.EqualTo(new[] { "document-lock", "|", "document-delete" }));
		Assert.That(Describe(screen.ToolBarItems), Is.EqualTo(new[] { "document-new", "|", "document-save", "|", "document-lock", "|", "document-delete" }));
		var save = menus[0].Items.Single(item => item.Id == "document-save");
		Assert.That(save.Title, Is.EqualTo("Save Document"));
		Assert.That(save.Icon, Is.EqualTo("fas fa-save"));
	}

	[Test]
	public void ACommandInAMenuAndOnTheToolbarShowsOneState() {
		var screen = new CommandScreen { Locked = true };
		var inMenu = screen.Menus[1].Items.Single(item => item.Id == "document-lock");
		var onToolBar = screen.ToolBarItems.Single(item => item.Id == "document-lock");
		Assert.That(onToolBar.Title, Is.EqualTo(inMenu.Title));
		Assert.That(onToolBar.IsEnabled, Is.False);
		Assert.That(inMenu.IsEnabled, Is.False);
	}

	[Test]
	public void ACommandIsEnabledFromItsPredicateAndDisabledWhileTheScreenIsBusy() {
		var screen = new CommandScreen();
		Assert.That(Item(screen, "document-save").IsEnabled, Is.False);
		screen.Dirty = true;
		Assert.That(Item(screen, "document-save").IsEnabled, Is.True);
		screen.Busy = true;
		Assert.That(Item(screen, "document-save").IsEnabled, Is.False);
		Assert.That(Item(screen, "document-new").IsEnabled, Is.False);
	}

	[Test]
	public void ABlockedCommandIsDisabledAndItsCaptionNamesTheBlocker() {
		var screen = new CommandScreen { Locked = true };
		var item = Item(screen, "document-lock");
		Assert.That(item.Title, Is.EqualTo("Lock Document (Document: Locked)"));
		Assert.That(item.IsEnabled, Is.False);
		screen.Locked = false;
		item = Item(screen, "document-lock");
		Assert.That(item.Title, Is.EqualTo("Lock Document"));
		Assert.That(item.IsEnabled, Is.True);
	}

	[Test]
	public void AHiddenCommandLeavesNoItemAndNoDanglingSeparator() {
		var screen = new CommandScreen { CanDelete = false };
		Assert.That(Describe(screen.Menus[1].Items), Is.EqualTo(new[] { "document-lock" }));
		Assert.That(Describe(screen.ToolBarItems), Is.EqualTo(new[] { "document-new", "|", "document-save", "|", "document-lock" }));
	}

	[Test]
	public void TheRecordMenuCaptionFollowsTheScreen() {
		var screen = new CommandScreen();
		Assert.That(screen.Menus[1].Text, Is.EqualTo("Document"));
		screen.RecordType = "Template";
		Assert.That(screen.Menus[1].Text, Is.EqualTo("Template"));
	}

	[Test]
	public async Task InvokingAnItemRunsTheActionThroughTheScreensHook() {
		var screen = new CommandScreen();
		await using var services = new ServiceCollection().BuildServiceProvider();
		await ((BlazorActionMenuItem)Item(screen, "document-new")).ExecuteAsync(services);
		Assert.That(screen.Log, Is.EqualTo(new[] { "hook:begin", "new", "hook:end" }));
	}

	[Test]
	public void AScreenWithoutCommandsPublishesNoMenusOrToolbar() {
		var screen = new PlainScreen();
		Assert.That(screen.Menus, Is.Empty);
		Assert.That(screen.ToolBarItems, Is.Empty);
	}

	[Test]
	public void ACommandNeedsAnIdTextAndAction() {
		var screen = new PlainScreen();
		Assert.That(() => screen.Declare(" ", "Text", () => Task.CompletedTask), Throws.ArgumentException);
		Assert.That(() => screen.Declare("id", "", () => Task.CompletedTask), Throws.ArgumentException);
		Assert.That(() => screen.Declare("id", "Text", null), Throws.ArgumentNullException);
	}

	private static BlazorUI.IApplicationMenuItem Item(ApplicationScreen screen, string id)
		=> screen.Menus.SelectMany(menu => menu.Items).Concat(screen.ToolBarItems).First(item => item.Id == id);

	private static string[] Describe(IEnumerable<BlazorUI.IApplicationMenuItem> items)
		=> items.Select(item => item is BlazorMenuSeparator ? "|" : item.Id).ToArray();

	#region Inner Types

	private sealed class CommandScreen : ApplicationScreen {
		public CommandScreen() {
			var newDocument = Command("document-new", "New Document", "fas fa-plus", () => Record("new"));
			var save = Command("document-save", "Save Document", "fas fa-save", () => Record("save")).EnabledWhen(() => Dirty);
			var lockDocument = Command("document-lock", "Lock Document", "fas fa-lock", () => Record("lock")).BlockedBy(() => Locked ? "Document: Locked" : string.Empty);
			var delete = Command("document-delete", "Delete Document", "fas fa-trash-alt", () => Record("delete")).VisibleWhen(() => CanDelete);
			UseCommands(new ScreenCommandLayout {
				File = [newDocument, save],
				MenuId = "document",
				MenuText = () => RecordType,
				Menu = [[lockDocument], [delete]],
				ToolBar = [[newDocument], [save], [lockDocument], [delete]]
			});
		}

		public List<string> Log { get; } = new();

		public bool Busy { get; set; }

		public bool Dirty { get; set; }

		public bool Locked { get; set; }

		public bool CanDelete { get; set; } = true;

		public string RecordType { get; set; } = "Document";

		protected override bool IsBusy => Busy;

		protected override async Task RunCommandAsync(Func<Task> action) {
			Log.Add("hook:begin");
			await action();
			Log.Add("hook:end");
		}

		private Task Record(string entry) {
			Log.Add(entry);
			return Task.CompletedTask;
		}
	}

	private sealed class PlainScreen : ApplicationScreen {
		public ScreenCommand Declare(string id, string text, Func<Task> action) => Command(id, text, "fas fa-plus", action);
	}

	#endregion
}
