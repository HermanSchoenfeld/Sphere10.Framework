// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
public class VisualGridLiveDemoTests {
	[Test]
	public void LiveCaptureKeepsCommittedRowsSelectableAndTimerPublishesReplacementInstances() => RunWithMessageLoop(async owner => {
		using var screen = CreateScreen(owner);
		await screen.Initialization;
		var grid = Find<VisualGrid>(screen, "_liveGrid");
		await grid.RefreshGrid();
		using (var image = new Bitmap(screen.Width, screen.Height)) {
			screen.DrawToBitmap(image, screen.ClientRectangle);
			image.Save(Path.Combine(TestContext.CurrentContext.TestDirectory, "visual-grid-source-notifications.png"), ImageFormat.Png);
		}
		var rows = grid.VisibleEntities.Cast<VisualGridTestScreen.LiveSample>().ToArray();
		Assert.That(rows.Length, Is.GreaterThan(1).And.LessThan(500), "Auto Paged must query a fitted page from the 500-row source.");
		grid.SelectedEntity = rows[0];
		var pending = screen.RefreshLiveSnapshotAsync();
		Assert.That(pending.IsCompleted, Is.False);
		Assert.That(grid.Enabled, Is.True, "Suppressed capture must keep its committed rows interactive.");
		grid.SelectedEntity = rows[1];
		await pending;
		var selected = (VisualGridTestScreen.LiveSample)grid.SelectedEntity;
		Assert.That(selected.Id, Is.EqualTo(rows[1].Id));
		Assert.That(selected, Is.Not.SameAs(rows[1]));
		Assert.That(selected.Generation, Is.GreaterThan(rows[1].Generation));
		var threads = Regex.Match(Find<Label>(screen, "_liveStatus").Text, @"Capture Thread (\d+) • UI Thread (\d+)");
		Assert.That(threads.Success, Is.True);
		Assert.That(threads.Groups[1].Value, Is.Not.EqualTo(threads.Groups[2].Value), "The demonstration must capture on a worker even when helper awaits complete synchronously.");

		Find<CheckBox>(screen, "_liveUpdatesCheckBox").Checked = true;
		await WaitForAsync(() => grid.SelectedEntity is VisualGridTestScreen.LiveSample current && current.Generation > selected.Generation);
		Find<CheckBox>(screen, "_liveUpdatesCheckBox").Checked = false;
		Assert.That(((VisualGridTestScreen.LiveSample)grid.SelectedEntity).Id, Is.EqualTo(selected.Id));
		Assert.That(grid.Enabled, Is.True);
		Find<CheckBox>(screen, "_liveRowStylesCheckBox").Checked = true;
		await grid.RefreshGrid();
		Assert.That(grid.RowStyleProvider, Is.Not.Null);
		Assert.That(grid.RowStyleProvider(selected with { Load = 95 }).Background, Is.EqualTo(Color.MistyRose));
		Assert.That(grid.RowStyleProvider(selected with { Load = 10 }).Background.IsEmpty, Is.True);
		Assert.That(((VisualGridTestScreen.LiveSample)grid.SelectedEntity).Id, Is.EqualTo(selected.Id));
		Find<CheckBox>(screen, "_liveRowStylesCheckBox").Checked = false;
		await grid.RefreshGrid();
		Assert.That(grid.RowStyleProvider, Is.Null);
	});

	[Test]
	public void DisposalJoinsAutomaticCellPreparationBeforeReleasingBorrowedImages() => RunWithMessageLoop(async owner => {
		using var screen = CreateScreen(owner);
		await screen.Initialization;
		var grid = Find<VisualGrid>(screen, "_liveGrid");
		await grid.RefreshGrid();
		var image = ((VisualGridTestScreen.LiveSample)grid.VisibleEntities.First()).Icon;
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var release = new ManualResetEventSlim();
		using var releaseOnFailure = Tools.Scope.ExecuteOnDispose(release.Set);
		var column = (VisualGridColumn<VisualGridTestScreen.LiveSample>)grid.GridBindings.First();
		var held = 0;
		column.PropertyValue = row => {
			if (Interlocked.Exchange(ref held, 1) == 0) {
				entered.TrySetResult();
				release.Wait();
				Assert.That(row.Icon.Width, Is.GreaterThan(0), "The source owner's images must remain alive until borrowed cell preparation ends.");
			}
			return row.Icon;
		};
		Find<CheckBox>(screen, "_liveUpdatesCheckBox").Checked = true;
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var pending = grid.RefreshCompletion;
		Assert.That(pending.IsCompleted, Is.False);
		screen.Dispose();
		Assert.That(screen.DisposalCompletion.IsCompleted, Is.False);
		release.Set();
		await screen.DisposalCompletion.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(pending.IsCompleted, Is.True);
		Assert.That(() => _ = image.Width, Throws.TypeOf<ArgumentException>());
	});

	[Test]
	public void TargetedDemoRefreshReplacesOnlyItsRowAndDisappearanceRemovesOnlyThatIdentity() => RunWithMessageLoop(async owner => {
		using var screen = CreateScreen(owner);
		await screen.Initialization;
		var grid = Find<VisualGrid>(screen, "_liveGrid");
		await grid.RefreshGrid();
		var before = grid.VisibleEntities.Cast<VisualGridTestScreen.LiveSample>().ToArray();
		grid.SelectedEntity = before[0];
		Assert.That(await screen.RefreshSelectedLiveRowAsync(), Is.True);
		var replacement = (VisualGridTestScreen.LiveSample)grid.SelectedEntity;
		Assert.That(replacement.Id, Is.EqualTo(before[0].Id));
		Assert.That(replacement.Generation, Is.GreaterThan(before[0].Generation));
		Assert.That(grid.VisibleEntities.Skip(1).Zip(before.Skip(1), ReferenceEquals).All(same => same), Is.True,
			"A targeted refresh must retain every other row instance.");
		Assert.That(await screen.RefreshSelectedLiveRowAsync(disappear: true), Is.True);
		Assert.That(grid.SelectedEntity, Is.Null);
		Assert.That(grid.VisibleEntities.Cast<VisualGridTestScreen.LiveSample>().Select(row => row.Id), Does.Not.Contain(before[0].Id));
		Assert.That(grid.VisibleEntities.Count(), Is.EqualTo(before.Length - 1));
		await screen.RefreshLiveSnapshotAsync();
		Assert.That(grid.VisibleEntities.Cast<VisualGridTestScreen.LiveSample>().Select(row => row.Id), Does.Contain(before[0].Id));
	});

	private static VisualGridTestScreen CreateScreen(Form owner) {
		var screen = new VisualGridTestScreen { Dock = DockStyle.Fill };
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
