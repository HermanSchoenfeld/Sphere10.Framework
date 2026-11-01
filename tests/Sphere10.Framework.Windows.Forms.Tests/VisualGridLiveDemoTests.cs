// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridLiveDemoTests {
	[Test]
	public void LiveCaptureKeepsCommittedRowsSelectableAndTimerPublishesReplacementInstances() => RunWithMessageLoop(async owner => {
		using var screen = CreateScreen(owner);
		await screen.Initialization;
		var grid = Find<CrudGrid>(screen, "_liveGrid");
		await grid.RefreshGrid();
		var rows = grid.VisibleEntities.Cast<CrudTestScreen.LiveSample>().ToArray();
		Assert.That(rows.Length, Is.GreaterThan(1).And.LessThan(500), "Auto Paged must query a fitted page from the 500-row source.");
		grid.SelectedEntity = rows[0];
		var pending = screen.RefreshLiveSnapshotAsync();
		Assert.That(pending.IsCompleted, Is.False);
		Assert.That(grid.Enabled, Is.True, "Suppressed capture must keep its committed rows interactive.");
		grid.SelectedEntity = rows[1];
		await pending;
		var selected = (CrudTestScreen.LiveSample)grid.SelectedEntity;
		Assert.That(selected.Id, Is.EqualTo(rows[1].Id));
		Assert.That(selected, Is.Not.SameAs(rows[1]));
		Assert.That(selected.Generation, Is.GreaterThan(rows[1].Generation));
		var threads = Regex.Match(Find<Label>(screen, "_liveStatus").Text, @"Capture Thread (\d+) • UI Thread (\d+)");
		Assert.That(threads.Success, Is.True);
		Assert.That(threads.Groups[1].Value, Is.Not.EqualTo(threads.Groups[2].Value), "The demonstration must capture on a worker even when helper awaits complete synchronously.");

		Find<CheckBox>(screen, "_liveUpdatesCheckBox").Checked = true;
		await WaitForAsync(() => grid.SelectedEntity is CrudTestScreen.LiveSample current && current.Generation > selected.Generation);
		Find<CheckBox>(screen, "_liveUpdatesCheckBox").Checked = false;
		Assert.That(((CrudTestScreen.LiveSample)grid.SelectedEntity).Id, Is.EqualTo(selected.Id));
		Assert.That(grid.Enabled, Is.True);
	});

	[Test]
	public void TargetedDemoRefreshReplacesOnlyItsRowAndDisappearanceRemovesOnlyThatIdentity() => RunWithMessageLoop(async owner => {
		using var screen = CreateScreen(owner);
		await screen.Initialization;
		var grid = Find<CrudGrid>(screen, "_liveGrid");
		await grid.RefreshGrid();
		var before = grid.VisibleEntities.Cast<CrudTestScreen.LiveSample>().ToArray();
		grid.SelectedEntity = before[0];
		Assert.That(await screen.RefreshSelectedLiveRowAsync(), Is.True);
		var replacement = (CrudTestScreen.LiveSample)grid.SelectedEntity;
		Assert.That(replacement.Id, Is.EqualTo(before[0].Id));
		Assert.That(replacement.Generation, Is.GreaterThan(before[0].Generation));
		Assert.That(grid.VisibleEntities.Skip(1).Zip(before.Skip(1), ReferenceEquals).All(same => same), Is.True,
			"A targeted refresh must retain every other row instance.");
		Assert.That(await screen.RefreshSelectedLiveRowAsync(disappear: true), Is.True);
		Assert.That(grid.SelectedEntity, Is.Null);
		Assert.That(grid.VisibleEntities.Cast<CrudTestScreen.LiveSample>().Select(row => row.Id), Does.Not.Contain(before[0].Id));
		Assert.That(grid.VisibleEntities.Count(), Is.EqualTo(before.Length - 1));
		await screen.RefreshLiveSnapshotAsync();
		Assert.That(grid.VisibleEntities.Cast<CrudTestScreen.LiveSample>().Select(row => row.Id), Does.Contain(before[0].Id));
	});

	private static CrudTestScreen CreateScreen(Form owner) {
		var screen = new CrudTestScreen { Dock = DockStyle.Fill };
		foreach (var grid in screen.Controls.Find("_crudGrid", true).OfType<CrudGrid>())
			grid.PersistSettings = false;
		owner.Controls.Add(screen);
		var tabs = Find<TabControl>(screen, "_contentTabs");
		tabs.SelectedTab = Find<TabPage>(screen, "_liveTab");
		return screen;
	}

	private static T Find<T>(Control parent, string name)
		where T : Control => parent.Controls.Find(name, true).OfType<T>().Single();

	private static async Task WaitForAsync(Func<bool> condition) {
		var deadline = DateTime.UtcNow.AddSeconds(8);
		while (!condition()) {
			Assert.That(DateTime.UtcNow, Is.LessThan(deadline), "The live demonstration did not publish another snapshot.");
			await Task.Delay(20);
		}
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1200, 850) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		var completed = false;
		watchdog.Tick += (_, _) => { failure = new AssertionException("CRUD live demonstration timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			try { await test(owner); completed = true; }
			catch (Exception error) { failure = error; }
			finally { watchdog.Stop(); owner.Close(); }
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}
}
