// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridRefreshTests {
	[Test]
	public void GridTitleCanBeConfiguredBeforeAHandleOrApplicationFormExists() {
		var forms = FormsApplication.OpenForms.Count;
		using var crud = new CrudGrid { PersistSettings = false, GridTitle = "Devices" };
		Assert.That(crud.IsHandleCreated, Is.False);
		Assert.That(Find<Label>(crud, "_titleLabel").Text, Is.EqualTo("Devices"));
		Assert.That(FormsApplication.OpenForms.Count, Is.EqualTo(forms), "An appearance property must not require or create an application window.");
		crud.GridTitle = null;
		Assert.That(crud.GridTitle, Is.Empty);
		Assert.That(Find<Label>(crud, "_titleLabel").Text, Is.Empty);
	}

	[Test]
	public void GridTitleUpdatesSynchronouslyAndRemainsVisibleForReadOnlySnapshots() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		var source = new SnapshotDataSource<string>();
		source.ReplaceSnapshot(new[] { "Device" });
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Device", PropertyValue = row => row, DataType = typeof(string) } };
		await crud.SetDataSource(source, source.Capabilities);
		await crud.RefreshGrid();
		crud.GridTitle = "Connected Devices";
		var caption = Find<Label>(crud, "_titleLabel");
		Assert.That(caption.Text, Is.EqualTo("Connected Devices"), "Owner-thread appearance changes should not wait for another message-loop turn.");
		Assert.That(caption.Visible, Is.True, "Read-only searchable tables must retain their title bar.");
		Assert.That(caption.Width, Is.GreaterThan(0));
		Assert.That(crud.VisibleEntities, Is.EqualTo(new[] { "Device" }));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void TitleOnlyHeaderRemainsVisibleWithoutSearchOrActionsEvenWhenTheGridIsDisabled(bool disabled) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		crud.Enabled = !disabled;
		crud.GridTitleAlignment = ContentAlignment.MiddleLeft;
		crud.GridTitle = "Connected Devices";
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Device", PropertyValue = row => row, DataType = typeof(string) } };
		var source = new SnapshotDataSource<string>();
		source.ReplaceSnapshot(new[] { "Device" });
		await crud.SetDataSource(source, DataSourceCapabilities.CanRead);
		await crud.RefreshGrid();
		var caption = Find<Label>(crud, "_titleLabel");
		Assert.That(caption.Visible, Is.True, "A title identifies a table even when no editing or search controls are available.");
		Assert.That(WinAPI.USER32.IsWindowVisible(caption.Handle), Is.True, "The native header must be displayed rather than hidden behind a collapsed layout row.");
		Assert.That(caption.Width, Is.GreaterThan(0));
		Assert.That(caption.Parent.Height, Is.GreaterThanOrEqualTo(caption.Bottom));
		Assert.That(crud.Capabilities, Is.EqualTo(DataSourceCapabilities.CanRead));
		Assert.That(Find<Control>(crud, "_searchTextBox").Enabled, Is.False);
		Assert.That(crud.VisibleEntities, Is.EqualTo(new[] { "Device" }));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void AddingAndClearingATitleShowsAndCollapsesAnOtherwiseUnusedHeaderWithoutRebinding(bool disabled) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		crud.Enabled = !disabled;
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Row", PropertyValue = row => row, DataType = typeof(string) } };
		var source = new ControlledSource();
		await crud.SetDataSource(source, DataSourceCapabilities.CanRead);
		await crud.RefreshGrid();
		var caption = Find<Label>(crud, "_titleLabel");
		var selected = crud.VisibleEntities.First();
		crud.SelectedEntity = selected;
		var reads = source.Requests.Count;
		Assert.That(caption.Visible, Is.False, "An untitled table without header tools retains the compact legacy layout.");
		crud.GridTitle = "All Devices";
		Assert.That(caption.Text, Is.EqualTo("All Devices"));
		Assert.That(caption.Visible && WinAPI.USER32.IsWindowVisible(caption.Handle), Is.True, "Setting a title must reveal it synchronously on the owning UI thread.");
		crud.GridTitle = null;
		Assert.That(caption.Visible || WinAPI.USER32.IsWindowVisible(caption.Handle), Is.False, "Clearing the last header content must collapse the unused header again.");
		Assert.That(crud.SelectedEntity, Is.SameAs(selected));
		Assert.That(source.Requests.Count, Is.EqualTo(reads), "Title-only changes must not schedule source reads or replace the committed rows.");
		Assert.That(crud.VisibleEntities, Does.Contain(selected));
	});

	[Test]
	public void UntitledSearchHeaderRetainsItsLayoutWhileTheGridIsDisabled() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		crud.Enabled = false;
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Device", PropertyValue = row => row, DataType = typeof(string) } };
		var source = new SnapshotDataSource<string>();
		await crud.SetDataSource(source, DataSourceCapabilities.CanSearch);
		await crud.RefreshGrid();
		var search = Find<Control>(crud, "_searchTextBox");
		Assert.That(search.Visible && WinAPI.USER32.IsWindowVisible(search.Handle), Is.True, "Temporarily disabling a grid must not remove its source's supported search layout.");
		Assert.That(search.Enabled, Is.False);
		crud.Enabled = true;
		Assert.That(search.Visible && search.Enabled, Is.True, "Re-enabling restores interaction without another capability assignment or source read.");
	});

	[Test]
	public void GridTitleMarshalsAWorkerUpdateToItsExistingOwnerHandle() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Device", PropertyValue = row => row, DataType = typeof(string) } };
		await crud.SetDataSource(new SnapshotDataSource<string>(), DataSourceCapabilities.CanRead);
		await crud.RefreshGrid();
		var caption = Find<Label>(crud, "_titleLabel");
		Assert.That(caption.Visible, Is.False);
		var ownerThread = Environment.CurrentManagedThreadId;
		var updates = new List<int>();
		caption.TextChanged += (_, _) => updates.Add(Environment.CurrentManagedThreadId);
		await Task.Run(() => crud.GridTitle = "Worker Update");
		await owner.InvokeAsync(() => { });
		Assert.That(caption.Text, Is.EqualTo("Worker Update"));
		Assert.That(caption.Visible && WinAPI.USER32.IsWindowVisible(caption.Handle), Is.True, "A marshaled title update must reveal an otherwise unused header on its native owner thread.");
		Assert.That(updates, Is.Not.Empty.And.All.EqualTo(ownerThread));
	});

	[Test]
	public void GridTitleIgnoresUpdatesAfterItsNativeSurfaceIsDisposed() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		crud.Dispose();
		await Task.Run(() => crud.GridTitle = "Closed Table");
		await owner.InvokeAsync(() => { });
		Assert.That(crud.IsDisposed, Is.True);
	});

	[Test]
	public void ConstructorPageAndCapabilitySettingsDoNotScheduleAnUnconfiguredBinding() {
		using var crud = new CrudGrid { PersistSettings = false };
		crud.PageMode = CrudGridPageMode.Auto;
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Result", PropertyValue = row => row, DataType = typeof(string) } };
		crud.Capabilities = CrudReferenceBinding.ReadOnlyCapabilities;
		var loading = Find<LoadingCircle>(crud, "_initialLoadingCircle");
		Assert.That(typeof(CrudGrid).GetField("_refreshTask", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(crud), Is.Null,
			"Configuring a never-hosted grid must not start an implicit asynchronous read before any data source exists.");
		Assert.That(crud.IsHandleCreated, Is.False);
		Assert.That(crud.VisibleEntities, Is.Empty);
		Assert.That(Find<TableLayoutPanel>(crud, "_layoutPanel").Visible, Is.False);
		Assert.That(loading.Active, Is.True, "The first-display loading state remains ready for actual source initialization.");
		crud.Dispose();
		Assert.That(loading.Active, Is.False, "Disposing an uninitialized grid must stop its initial animation.");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void FirstBindingHidesDesignerChromeUntilItsReadCompletesEvenWhenRoutineIndicatorsAreSuppressed(bool suppress) => RunWithMessageLoop(async owner => {
		var ownerThread = Environment.CurrentManagedThreadId;
		using var crud = NewGrid(owner, suppress);
		var handleThreads = new ConcurrentQueue<int>();
		foreach (var control in Descendants(crud))
			control.HandleCreated += (_, _) => handleThreads.Enqueue(Environment.CurrentManagedThreadId);
		var loading = Find<LoadingCircle>(crud, "_initialLoadingCircle");
		Assert.That(loading.Visible && loading.Active, Is.True);
		Assert.That(Find<TableLayoutPanel>(crud, "_layoutPanel").Visible, Is.False);
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Row", PropertyValue = row => row, DataType = typeof(string) } };
		var source = new ControlledSource();
		source.DelayNextRead();
		await crud.SetDataSource(source);
		var pending = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(loading.Visible && loading.Active, Is.True);
		Assert.That(Find<TableLayoutPanel>(crud, "_layoutPanel").Visible, Is.False, "The search, creation and paging placeholders must remain hidden through a slow first read.");
		source.ContinueRead.SetResult();
		await pending;
		Assert.That(loading.Visible || loading.Active, Is.False);
		Assert.That(Find<TableLayoutPanel>(crud, "_layoutPanel").Visible, Is.True);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(100));
		Assert.That(handleThreads, Is.Not.Empty, "Revealing the first completed layout must create its previously hidden native controls.");
		Assert.That(handleThreads, Is.All.EqualTo(ownerThread), "Awaiting a source must retain the constructing UI thread while revealing native controls.");
	});

	[TestCase(850, 400)]
	[TestCase(1600, 900)]
	public void FirstBindingAndReloadUseTheSameExpandedIndicatorAndFollowHostResizing(int width, int height) => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(width, height);
		using var crud = NewGrid(owner, false);
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Row", PropertyValue = row => row, DataType = typeof(string) } };
		var source = new ControlledSource();
		source.DelayNextRead();
		await crud.SetDataSource(source);
		var initialRead = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var loading = Descendants(crud).OfType<LoadingCircle>().Single();
		Assert.That(loading.Visible && loading.Active, Is.True);
		Assert.That(loading.Bounds, Is.EqualTo(crud.ClientRectangle), "The first loading surface must cover the complete grid viewport.");
		Assert.That(loading.OuterCircleRadius, Is.EqualTo((Math.Min(crud.ClientSize.Width, crud.ClientSize.Height) - 1) / 3),
			"Filling the control bounds must also expand the painted spokes, rather than retaining their tiny designer radius.");
		SaveLoadingScreenshot(crud, $"loading-{width}x{height}-initial.png");

		// Resize while the first read is still pending, as happens when a tab first fills its host.
		owner.ClientSize = new Size(width + 200, height + 150);
		owner.PerformLayout();
		crud.PerformLayout();
		Assert.That(loading.Bounds, Is.EqualTo(crud.ClientRectangle));
		var appearance = (loading.Bounds, loading.InnerCircleRadius, loading.OuterCircleRadius, loading.SpokeThickness, loading.StylePreset);
		Assert.That(appearance.OuterCircleRadius, Is.EqualTo((Math.Min(crud.ClientSize.Width, crud.ClientSize.Height) - 1) / 3));
		SaveLoadingScreenshot(crud, $"loading-{width}x{height}-initial-resized.png");
		source.ContinueRead.SetResult();
		await initialRead;
		Assert.That(loading.Visible || loading.Active, Is.False);

		// A later read reuses the exact same control, bounds and relative painted geometry.
		source.DelayNextRead();
		var reload = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(Descendants(crud).OfType<LoadingCircle>().Single(), Is.SameAs(loading));
		Assert.That(loading.Visible && loading.Active, Is.True);
		Assert.That(crud.GetChildAtPoint(new Point(crud.Width / 2, crud.Height / 2)), Is.SameAs(loading), "The active indicator must cover the committed table in native child order.");
		Assert.That((loading.Bounds, loading.InnerCircleRadius, loading.OuterCircleRadius, loading.SpokeThickness, loading.StylePreset), Is.EqualTo(appearance));
		SaveLoadingScreenshot(crud, $"loading-{width}x{height}-reload.png");
		source.ContinueRead.SetResult();
		await reload;
		Assert.That(loading.Visible || loading.Active, Is.False);
	});

	[Test]
	public void AFailedFirstReadRetainsTheUnifiedIndicatorUntilASuccessfulRetry() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Row", PropertyValue = row => row, DataType = typeof(string) } };
		var source = new ControlledSource { FailNextRead = true };
		await crud.SetDataSource(source);
		var loading = Descendants(crud).OfType<LoadingCircle>().Single();
		await Assert.ThatAsync(crud.RefreshGrid, Throws.TypeOf<IOException>().With.Message.EqualTo("Source read failed."));
		Assert.That(loading.Visible && loading.Active, Is.True);
		Assert.That(Find<TableLayoutPanel>(crud, "_layoutPanel").Visible, Is.False);
		await crud.RefreshGrid();
		Assert.That(loading.Visible || loading.Active, Is.False);
		Assert.That(Find<TableLayoutPanel>(crud, "_layoutPanel").Visible, Is.True);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(100));
	});

	[Test]
	public void NestedLoadingScopesKeepTheOuterIndicatorAndDisposalStopsIt() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		await BindAsync(crud);
		var loading = Descendants(crud).OfType<LoadingCircle>().Single();
		var enterLoading = typeof(CrudGrid).GetMethod("EnterLoadingIndicator", BindingFlags.Instance | BindingFlags.NonPublic);
		using (var outer = (IDisposable)enterLoading.Invoke(crud, null)) {
			using (var inner = (IDisposable)enterLoading.Invoke(crud, null))
				Assert.That(loading.Visible && loading.Active, Is.True);
			Assert.That(loading.Visible && loading.Active, Is.True, "Completing an inner operation must not stop its enclosing operation's animation.");
		}
		Assert.That(loading.Visible || loading.Active, Is.False);
		using (var pending = (IDisposable)enterLoading.Invoke(crud, null)) {
			crud.Dispose();
			Assert.That(loading.Active, Is.False, "Destroying the grid stops its animation even while a scope is awaiting completion.");
		}
	});

	[Test]
	public void InitialLoadingAlsoCoversAnIncompleteSourceConfiguration() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Row", PropertyValue = row => row, DataType = typeof(string) } };
		var source = new ControlledSource { ContinueCapabilities = new TaskCompletionSource<DataSourceCapabilities>(TaskCreationOptions.RunContinuationsAsynchronously) };
		var setup = crud.SetDataSource(source);
		await crud.RefreshGrid();
		Assert.That(source.Requests, Is.Empty, "The pending source must not be queried before its capability configuration is complete.");
		Assert.That(Find<LoadingCircle>(crud, "_initialLoadingCircle").Active, Is.True);
		Assert.That(Find<TableLayoutPanel>(crud, "_layoutPanel").Visible, Is.False);
		source.ContinueCapabilities.SetResult(source.Capabilities);
		await setup;
		await crud.RefreshGrid();
		Assert.That(Find<TableLayoutPanel>(crud, "_layoutPanel").Visible, Is.True);
	});

	[Test]
	public void RowRefreshUsesReplacedSourceInstanceAndRetainsOtherRowsWidthsSelectionAndPage() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		var source = await BindAsync(crud);
		Find<IntBox>(crud, "_pageNumberBox").Value = 2;
		await crud.RefreshGrid();
		var grid = Find<Grid>(crud, "_grid");
		grid.Columns[0].Width = 317;
		var before = crud.VisibleEntities.ToArray();
		crud.SelectedEntity = before[4];
		source.RefreshedEntity = "Updated process";
		var reads = source.Requests.Count;
		Assert.That(await crud.RefreshSelectedEntityAsync(), Is.True);
		Assert.That(source.RefreshedOriginal, Is.SameAs(before[4]));
		Assert.That(crud.SelectedEntity, Is.SameAs(source.RefreshedEntity));
		Assert.That(crud.VisibleEntities.ElementAt(4), Is.SameAs(source.RefreshedEntity));
		Assert.That(crud.VisibleEntities.ElementAt(3), Is.SameAs(before[3]));
		Assert.That(grid.Columns[0].Width, Is.EqualTo(317));
		Assert.That(Find<IntBox>(crud, "_pageNumberBox").Value, Is.EqualTo(2));
		Assert.That(source.Requests.Count, Is.EqualTo(reads), "A row refresh must not reread or rebuild the whole page.");
		Assert.That(Find<LoadingCircle>(crud, "_initialLoadingCircle").Active, Is.False);
	});

	[Test]
	public void RowRefreshDiscardsAReplacementAfterTheUserChangesPage() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		var source = await BindAsync(crud);
		var entity = crud.VisibleEntities.First();
		source.DelayNextRefresh();
		source.RefreshedEntity = "Superseded row";
		var pending = crud.RefreshEntityAsync(entity);
		await source.RefreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Find<IntBox>(crud, "_pageNumberBox").Value = 2;
		source.ContinueRefresh.SetResult();
		Assert.That(await pending, Is.False);
		await crud.RefreshGrid();
		Assert.That(crud.VisibleEntities.First(), Is.EqualTo("Row 0100"));
		Assert.That(crud.VisibleEntities, Does.Not.Contain("Superseded row"));
	});

	[Test]
	public void ADeletedRowReplacementRemovesItsSelectionWithoutAWholePageRead() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		var source = await BindAsync(crud);
		crud.SelectedEntity = crud.VisibleEntities.First();
		source.RefreshedEntity = null;
		var reads = source.Requests.Count;
		Assert.That(await crud.RefreshSelectedEntityAsync(), Is.True);
		Assert.That(crud.SelectedEntity, Is.Null);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(99));
		Assert.That(Find<Label>(crud, "_totalRecordsLabel").Text, Is.EqualTo("199"));
		Assert.That(source.Requests.Count, Is.EqualTo(reads));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void SuppressedRefreshKeepsFocusAndControlsEnabledWhileDefaultRefreshShowsItsIndicator(bool suppress) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, suppress);
		var source = await BindAsync(crud);
		var grid = Find<Grid>(crud, "_grid");
		grid.Focus();
		Assert.That(grid.ContainsFocus, Is.True);
		var previousRows = crud.VisibleEntities.ToArray();
		source.DelayNextRead();
		var pending = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(grid.Enabled, Is.EqualTo(suppress));
		Assert.That(Find<LoadingCircle>(crud, "_initialLoadingCircle").Active, Is.EqualTo(!suppress));
		Assert.That(crud.VisibleEntities, Is.EqualTo(previousRows));
		if (suppress)
			Assert.That(grid.ContainsFocus, Is.True, "A routine live update must not take the user's focus away.");
		source.ContinueRead.SetResult();
		await pending;
		Assert.That(grid.Enabled, Is.True);
		Assert.That(Find<LoadingCircle>(crud, "_initialLoadingCircle").Active, Is.False);
	});

	[Test]
	public void SuppressedRefreshReadsImmediatelyAndCoalescesAnInFlightQueryChange() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		var source = await BindAsync(crud);
		source.DelayNextRead();
		var pending = crud.RefreshGrid();
		Assert.That(source.ReadStarted.Task.IsCompleted, Is.True, "Suppressed refresh must begin its source query without the stampede delay.");
		Assert.That(crud.RefreshGrid(), Is.SameAs(pending), "Repeated requests must join the active refresh task.");
		Find<IntBox>(crud, "_pageNumberBox").Value = 2;
		source.ContinueRead.SetResult();
		await pending;
		Assert.That(source.Requests.Last().Page, Is.EqualTo(1));
		Assert.That(crud.VisibleEntities.Cast<string>().First(), Is.EqualTo("Row 0100"));
		Assert.That(Find<IntBox>(crud, "_pageNumberBox").Value, Is.EqualTo(2));
		Assert.That(source.MaximumConcurrentReads, Is.EqualTo(1));
	});

	[Test]
	public void RapidCompletedSuppressedRefreshesAreNotDelayedOrDropped() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true);
		var source = await BindAsync(crud);
		var reads = source.Requests.Count;
		for (var index = 0; index < 5; index++) {
			var refresh = crud.RefreshGrid();
			Assert.That(refresh.IsCompleted, Is.True, "An immediately completed source must not wait for the visual loading throttle.");
			await refresh;
		}
		Assert.That(source.Requests.Count, Is.EqualTo(reads + 5));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void AFailedReadRestoresThePreviousEnabledStateAndAllowsAnotherRefresh(bool suppress) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, suppress);
		var source = await BindAsync(crud);
		var grid = Find<Grid>(crud, "_grid");
		source.FailNextRead = true;
		await Assert.ThatAsync(crud.RefreshGrid, Throws.TypeOf<IOException>().With.Message.EqualTo("Source read failed."));
		Assert.That(grid.Enabled, Is.True);
		Assert.That(Find<LoadingCircle>(crud, "_initialLoadingCircle").Active, Is.False);
		await crud.RefreshGrid();
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(100));
		grid.Enabled = false;
		await crud.RefreshGrid();
		Assert.That(grid.Enabled, Is.False, "Refresh must preserve a caller-disabled grid.");
	});

	[TestCase(false)]
	[TestCase(true)]
	public void DisposalDuringPendingRefreshDoesNotTouchDestroyedControls(bool suppress) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, suppress);
		var source = await BindAsync(crud);
		source.DelayNextRead();
		var pending = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		crud.Dispose();
		source.ContinueRead.SetResult();
		await pending;
		Assert.That(crud.IsDisposed, Is.True);
	});

	private static CrudGrid NewGrid(Form owner, bool suppress) {
		var crud = new CrudGrid { Dock = DockStyle.Fill, PersistSettings = false, SuppressLoadingIndicator = suppress };
		owner.Controls.Add(crud);
		return crud;
	}

	private static async Task<ControlledSource> BindAsync(CrudGrid crud) {
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Row", PropertyValue = row => row, DataType = typeof(string), ExpandsToFit = true } };
		var source = new ControlledSource();
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		return source;
	}

	private static T Find<T>(Control control, string name)
		where T : Control => (T)control.Controls.Find(name, true).Single();

	private static void SaveLoadingScreenshot(CrudGrid crud, string name) {
		var directory = Environment.GetEnvironmentVariable("SPHERE10_CRUD_LOADING_SCREENSHOT_DIRECTORY");
		if (string.IsNullOrWhiteSpace(directory))
			return;
		Directory.CreateDirectory(directory);
		using var bitmap = new Bitmap(crud.Width, crud.Height);
		// DrawToBitmap on a parent can print hidden/overlapped sibling controls over its foreground.
		// The test verifies native child order separately; render the actual full-viewport loading control.
		var loading = Descendants(crud).OfType<LoadingCircle>().Single();
		loading.DrawToBitmap(bitmap, loading.ClientRectangle);
		bitmap.Save(Path.Combine(directory, name));
	}

	private static IEnumerable<Control> Descendants(Control owner) => owner.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previousContext = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(850, 400) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The CRUD refresh test timed out.");
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			using var closeOwner = Tools.Scope.ExecuteOnDispose(owner.Close);
			try {
				await test(owner);
				completed = true;
			} catch (Exception error) {
				failure = error;
			}
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	private sealed class ControlledSource : ListDataSource<string> {
		private readonly string[] _rows = Enumerable.Range(0, 200).Select(index => $"Row {index:D4}").ToArray();
		private bool _delayNext;
		private int _concurrentReads;

		public ControlledSource()
			: base(new ExtendedList<string>()) {
		}

		public List<(int Length, int Page)> Requests { get; } = new();

		public TaskCompletionSource ReadStarted { get; private set; }

		public TaskCompletionSource ContinueRead { get; private set; }

		public bool FailNextRead { get; set; }

		public int MaximumConcurrentReads { get; private set; }

		public string RefreshedEntity { get; set; }

		public string RefreshedOriginal { get; private set; }

		public TaskCompletionSource RefreshStarted { get; private set; }

		public TaskCompletionSource ContinueRefresh { get; private set; }

		public TaskCompletionSource<DataSourceCapabilities> ContinueCapabilities { get; set; }

		public override Task<DataSourceCapabilities> CapabilitiesAsync => ContinueCapabilities?.Task ?? base.CapabilitiesAsync;

		public void DelayNextRefresh() {
			RefreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			ContinueRefresh = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		public override async Task RefreshRangeAsync(string[] entities) {
			RefreshedOriginal = entities[0];
			if (RefreshStarted != null) {
				RefreshStarted.SetResult();
				await ContinueRefresh.Task;
			}
			entities[0] = RefreshedEntity;
		}

		public void DelayNextRead() {
			_delayNext = true;
			ReadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			ContinueRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		public override async Task<DataSourceItems<string>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			Requests.Add((pageLength, page));
			MaximumConcurrentReads = Math.Max(MaximumConcurrentReads, ++_concurrentReads);
			using var closeRead = Tools.Scope.ExecuteOnDispose(() => _concurrentReads--);
			if (_delayNext) {
				_delayNext = false;
				ReadStarted.SetResult();
				await ContinueRead.Task;
			}
			if (FailNextRead) {
				FailNextRead = false;
				throw new IOException("Source read failed.");
			}
			page = Math.Min(page, (_rows.Length - 1) / pageLength);
			return new DataSourceItems<string> { Items = _rows.Skip(page * pageLength).Take(pageLength).ToArray(), Page = page, TotalCount = _rows.Length };
		}
	}
}
