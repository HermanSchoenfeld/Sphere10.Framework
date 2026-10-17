// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;
using FormsApplication = System.Windows.Forms.Application;
using GridTextBox = Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors.TextBox;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridBackgroundPreparationTests {
	[Test]
	public void PreparationRunsReadsEnumerationGettersAndConvertersOffOwnerThreadWhileMessagesContinue() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var ownerThread = Environment.CurrentManagedThreadId;
		var modelThreads = new ConcurrentQueue<int>();
		DisplayValueConverter.Threads.Clear();
		DisplayValueConverter.ConfigurationThreads.Clear();
		using var release = new ManualResetEventSlim();
		using var releaseOnFailure = Tools.Scope.ExecuteOnDispose(release.Set);
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var block = 0;
		var source = new RecordingSource { Rows = new[] { new Row { Value = new DisplayValue { Number = 1 } } } };
		crud.GridBindings = new[] { new CrudGridColumn<Row> {
			ColumnName = "Value", DataType = typeof(DisplayValue),
			PropertyHasValue = row => { modelThreads.Enqueue(Environment.CurrentManagedThreadId); return row.HasValue; },
			PropertyValue = row => {
				modelThreads.Enqueue(Environment.CurrentManagedThreadId);
				if (Interlocked.Exchange(ref block, 0) != 0) {
					started.TrySetResult();
					if (!release.Wait(TimeSpan.FromSeconds(5)))
						throw new TimeoutException("The test's model evaluation was not released.");
				}
				return row.Value;
			}
		} };
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var originalCell = native[1, 0];
		var originalModel = originalCell.Model;
		var originalValueModel = originalCell.Model.ValueModel;
		var originalEditor = originalCell.Editor;
		var originalControllers = originalCell.Controller;
		var replacement = new Row { Value = new DisplayValue { Number = 2 } };
		source.Rows = new[] { replacement };
		Volatile.Write(ref block, 1);
		var pending = crud.RefreshGrid();
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var ticks = 0;
		using var heartbeat = new System.Windows.Forms.Timer { Interval = 10 };
		heartbeat.Tick += (_, _) => ticks++;
		heartbeat.Start();
		await Task.Delay(100);
		Assert.That(ticks, Is.GreaterThanOrEqualTo(2), "Blocked model work must leave the actual native message loop responsive.");
		Assert.That(crud.RefreshGrid(), Is.SameAs(pending));
		Assert.That(new CellContext(native, new Position(1, 0)).DisplayText, Is.EqualTo("Formatted 1"), "Keep the previous complete page visible while its replacement is prepared.");
		release.Set();
		await pending;
		Assert.That(native[1, 0], Is.SameAs(originalCell), "Compatible read-only row slots should retain their cell and selection controllers.");
		Assert.That(native[1, 0].Model, Is.SameAs(originalModel));
		Assert.That(native[1, 0].Model.ValueModel, Is.SameAs(originalValueModel));
		Assert.That(native[1, 0].Editor, Is.SameAs(originalEditor), "A warm publication must update the prepared text without replacing its editor graph.");
		Assert.That(native[1, 0].Controller, Is.SameAs(originalControllers));
		Assert.That(native[1, 0].Value, Is.SameAs(replacement.Value), "Formatting must retain the original typed value.");
		Assert.That(crud.VisibleEntities.Single(), Is.SameAs(replacement));
		Assert.That(native[1, 0].Editor, Is.Not.InstanceOf<EditorControlBase>());
		var conversions = DisplayValueConverter.Threads.Count;
		native.Rows.AutoSize(false);
		native.Refresh();
		Assert.That(new CellContext(native, new Position(1, 0)).DisplayText, Is.EqualTo("Formatted 2"));
		Assert.That(DisplayValueConverter.Threads.Count, Is.EqualTo(conversions), "Sizing and painting must use the prepared string without repeating conversion.");
		Assert.That(modelThreads, Is.Not.Empty.And.All.Not.EqualTo(ownerThread));
		Assert.That(source.ReadThreads, Is.Not.Empty.And.All.Not.EqualTo(ownerThread));
		Assert.That(source.EnumerationThreads, Is.Not.Empty.And.All.Not.EqualTo(ownerThread));
		Assert.That(DisplayValueConverter.Threads, Is.Not.Empty.And.All.Not.EqualTo(ownerThread));
		Assert.That(DisplayValueConverter.ConfigurationThreads, Is.Not.Empty.And.All.Not.EqualTo(ownerThread),
			"Creating a native display slot must reuse the initialized worker converter, without repeating arbitrary type-converter setup on the UI thread.");
	});

	[Test]
	public async Task PreparingTenThousandTextValuesAllocatesBoundedDataWithoutPerCellNativeGraphs() {
		// Warm reflection/conversion first and measure only a representative worker publication with pre-existing strings.
		var rows = Enumerable.Range(0, 500).Select(index => (object)new Row { Value = "Process " + index }).ToArray();
		var columns = Enumerable.Range(0, 20).Select(_ => (ICrudGridColumn)TextColumn()).ToArray();
		var formats = columns.Select(column => (column.DisplayType, column.DataType)).ToArray();
		var prepare = typeof(CrudGrid).GetMethod("PrepareReadOnlyRows", BindingFlags.NonPublic | BindingFlags.Static);
		var arguments = new object[] { rows, columns, formats };
		var result = await Task.Run(() => {
			prepare.Invoke(null, arguments);
			var before = GC.GetAllocatedBytesForCurrentThread();
			var prepared = (Array)prepare.Invoke(null, arguments);
			return (Prepared: prepared, Allocated: GC.GetAllocatedBytesForCurrentThread() - before);
		});
		Assert.That(result.Prepared.Length, Is.EqualTo(500));
		var firstRow = (Array)result.Prepared.GetValue(0);
		Assert.That(firstRow.Length, Is.EqualTo(20));
		var firstValue = firstRow.GetValue(0);
		Assert.That(firstValue, Is.Not.InstanceOf<ICell>(), "A worker publication contains prepared values, rather than complete SourceGrid cells.");
		Assert.That(firstValue.GetType().GetProperty("Value").GetValue(firstValue), Is.SameAs(((Row)rows[0]).Value));
		Assert.That(firstValue.GetType().GetProperty("DisplayText").GetValue(firstValue), Is.EqualTo("Process 0"));
		Assert.That(result.Allocated, Is.LessThan(2_500_000), "A 500-row, 20-column refresh must not rebuild the former multi-megabyte cell/model/editor graph.");
		TestContext.Out.WriteLine($"Preparing 500 x 20 existing string values allocated {result.Allocated:N0} bytes.");
	}

	[Test]
	public void PreparedUpdatesRetainExactNullEmptyAndMissingStatesWithoutNativeValueChangeCallbacks() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var source = new RecordingSource { Rows = new[] { new Row { Value = "Original" } } };
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Value", DataType = typeof(string),
			PropertyHasValue = row => row.HasValue, PropertyValue = row => row.Value } };
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var cell = native[1, 0];
		var model = cell.Model.ValueModel;
		var editor = cell.Editor;
		var changes = new ValueChanges();
		native.Controller.AddController(changes);
		foreach (var replacement in new[] {
			new Row { Value = string.Empty }, new Row { Value = null }, new Row { Value = "Hidden", HasValue = false },
			new Row { Value = null }, new Row { Value = string.Empty }, new Row { Value = "Published" }
		}) {
			source.Rows = new[] { replacement };
			await crud.RefreshGrid();
			Assert.That(native[1, 0], Is.SameAs(cell));
			Assert.That(cell.Model.ValueModel, Is.SameAs(model));
			Assert.That(cell.Value, Is.SameAs(replacement.HasValue ? replacement.Value : string.Empty));
			Assert.That(new CellContext(native, new Position(1, 0)).DisplayText, Is.EqualTo(replacement.HasValue ? replacement.Value ?? string.Empty : string.Empty));
			Assert.That(cell.Editor, replacement.HasValue ? Is.SameAs(editor) : Is.Null);
		}
		Assert.That(changes.Changing, Is.Zero);
		Assert.That(changes.Changed, Is.Zero, "Binding a prepared snapshot is not an external edit.");

		// Direct SourceGrid writes retain their normal notifications and conversion fallback.
		cell.Value = "External value";
		Assert.That(cell.Value, Is.EqualTo("External value"));
		Assert.That(new CellContext(native, new Position(1, 0)).DisplayText, Is.EqualTo("External value"));
		Assert.That(changes.Changing, Is.EqualTo(1));
		Assert.That(changes.Changed, Is.EqualTo(1));
	});

	[Test]
	public void ACustomValueModelIsReplacedOnRefreshWithoutWritingToTheOrphanedModel() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var source = new RecordingSource { Rows = new[] { new Row { Value = "Original" } } };
		crud.GridBindings = new[] { TextColumn() };
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var original = native[1, 0];
		var custom = new SourceGrid.Cells.Models.ValueModel("External model");
		original.Model.ValueModel = custom;
		source.Rows = new[] { new Row { Value = "Current" } };
		await crud.RefreshGrid();
		Assert.That(native[1, 0], Is.Not.SameAs(original));
		Assert.That(native[1, 0].Value, Is.EqualTo("Current"));
		Assert.That(custom.GetValue(CellContext.Empty), Is.EqualTo("External model"));
	});

	[Test]
	public void PreparationRejectsAResultWhenItsColumnsAreReplacedWhileAGetterIsPending() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		using var release = new ManualResetEventSlim();
		using var releaseOnFailure = Tools.Scope.ExecuteOnDispose(release.Set);
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var source = new RecordingSource { Rows = new[] { new Row { Value = "Original" } } };
		crud.GridBindings = new[] { TextColumn() };
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Superseded", DataType = typeof(string), PropertyValue = row => {
			started.TrySetResult();
			if (!release.Wait(TimeSpan.FromSeconds(5)))
				throw new TimeoutException("Superseded model work was not released.");
			return "Superseded";
		} } };
		var pending = crud.RefreshGrid();
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Current", DataType = typeof(string), PropertyValue = row => "Current value" } };
		release.Set();
		await pending;
		Assert.That(FindGrid(crud)[0, 0].Value, Is.EqualTo("Current"));
		Assert.That(FindGrid(crud)[1, 0].Value, Is.EqualTo("Current value"));
	});

	[Test]
	public void DisposalDuringPreparationDiscardsItsPreparedValues() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		using var release = new ManualResetEventSlim();
		using var releaseOnFailure = Tools.Scope.ExecuteOnDispose(release.Set);
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Pending", DataType = typeof(string), PropertyValue = row => {
			started.TrySetResult();
			if (!release.Wait(TimeSpan.FromSeconds(5)))
				throw new TimeoutException("Disposed model work was not released.");
			return row.Value;
		} } };
		await crud.SetDataSource(new RecordingSource { Rows = new[] { new Row { Value = "Never bind" } } });
		var pending = crud.RefreshGrid();
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		crud.Dispose();
		release.Set();
		await pending;
		Assert.That(crud.IsDisposed, Is.True);
	});

	[TestCase(true, CrudCellDisplayType.Text)]
	[TestCase(false, CrudCellDisplayType.Numeric)]
	public void EditableAndOtherCellTypesRetainOwnerThreadDelegateAndControlConstruction(bool allowEditing, CrudCellDisplayType displayType) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.AllowCellEditing = allowEditing;
		var ownerThread = Environment.CurrentManagedThreadId;
		var getterThreads = new ConcurrentQueue<int>();
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Value", DataType = typeof(int), DisplayType = displayType,
			CanEditCell = true, PropertyValue = row => { getterThreads.Enqueue(Environment.CurrentManagedThreadId); return row.Value; }
		} };
		var source = new RecordingSource { Rows = new[] { new Row { Value = 12 } } };
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		Assert.That(getterThreads, Is.Not.Empty.And.All.EqualTo(ownerThread));
		Assert.That(source.ReadThreads, Is.Not.Empty.And.All.EqualTo(ownerThread));
		Assert.That(FindGrid(crud)[1, 0].Editor, Is.InstanceOf<EditorControlBase>());
	});

	[Test]
	public void PreparedCellsStillPromoteToRealEditorsAndPersistAcceptedEdits() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var row = new Row { Value = "Original" };
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Value", DataType = typeof(string), CanEditCell = true,
			PropertyValue = entity => entity.Value, SetPropertyValue = (entity, value) => entity.Value = value } };
		await crud.SetDataSource(new ListDataSource<Row>(new ExtendedList<Row>(new[] { row })));
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var cell = native[1, 0];
		Assert.That(cell.Editor, Is.Not.InstanceOf<EditorControlBase>());
		crud.AllowCellEditing = true;
		Assert.That(native[1, 0], Is.SameAs(cell));
		var context = new CellContext(native, new Position(1, 0));
		context.StartEdit();
		Assert.That(context.IsEditing(), Is.True);
		((GridTextBox)cell.Editor).Control.Text = "Edited";
		var updated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		crud.EntityUpdated += (_, _) => updated.TrySetResult();
		Assert.That(context.EndEdit(false), Is.True);
		await updated.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(row.Value, Is.EqualTo("Edited"));
	});

	[Test]
	public void PreparedImagesReuseSlotsAcrossMembershipChangesWithoutOwningBorrowedImages() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		using var small = new Bitmap(16, 16);
		using var large = new Bitmap(16, 64);
		var source = new RecordingSource { Rows = new[] { new Row { Value = small }, new Row { HasValue = false } } };
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Icon", DataType = typeof(System.Drawing.Image), DisplayType = CrudCellDisplayType.Image,
			PropertyHasValue = row => row.HasValue, PropertyValue = row => row.Value } };
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var original = native[1, 0];
		source.Rows = new[] { new Row { Value = large }, new Row { Value = small }, new Row { HasValue = false } };
		await crud.RefreshGrid();
		Assert.That(native[1, 0], Is.SameAs(original));
		Assert.That(((Cell)native[1, 0]).Image, Is.SameAs(large));
		Assert.That(((Cell)native[2, 0]).Image, Is.SameAs(small));
		Assert.That(native.Rows[1].Height, Is.GreaterThanOrEqualTo(large.Height));
		source.Rows = new[] { new Row { HasValue = false } };
		await crud.RefreshGrid();
		Assert.That(native.RowsCount, Is.EqualTo(2));
		Assert.That(((Cell)native[1, 0]).Image, Is.Null);
		crud.Dispose();
		Assert.That(small.Height, Is.EqualTo(16));
		Assert.That(large.Height, Is.EqualTo(64));
	});

	[Test]
	public void TargetedPreparationRefreshesAllDuplicateOccurrencesWithDistinctCells() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.PrepareReadOnlyCellsInBackground = false;
		var original = new Row { Value = "Original" };
		var replacement = new Row { Value = "Refreshed" };
		var source = new RecordingSource { Rows = new[] { original, original }, Refreshed = replacement };
		crud.GridBindings = new[] { TextColumn() };
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		crud.SelectedEntity = original;
		crud.PrepareReadOnlyCellsInBackground = true;
		Assert.That(await crud.RefreshEntityAsync(original), Is.True);
		var native = FindGrid(crud);
		Assert.That(native[1, 0], Is.Not.SameAs(native[2, 0]));
		Assert.That(native[1, 0].Row.Index, Is.EqualTo(1));
		Assert.That(native[2, 0].Row.Index, Is.EqualTo(2));
		Assert.That(native[1, 0].Value, Is.EqualTo("Refreshed"));
		Assert.That(native[2, 0].Value, Is.EqualTo("Refreshed"));
		Assert.That(crud.SelectedEntity, Is.SameAs(replacement));
		Assert.That(crud.VisibleEntities, Is.All.SameAs(replacement));
		Assert.That(source.RefreshThreads, Is.Not.Empty.And.All.Not.EqualTo(Environment.CurrentManagedThreadId));
	});

	[TestCase("initial")]
	[TestCase("font")]
	[TestCase("width")]
	[TestCase("multiline")]
	public void NativeSizingKeepsTimerDispatchResponsiveAndMatchesSourceGridGeometry(string change) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var rows = Enumerable.Range(0, 180).Select(index => new Row { Value = "Original row " + index }).ToArray();
		var source = new RecordingSource { Rows = rows };
		crud.GridBindings = SizingColumns();
		await crud.SetDataSource(source);
		var native = FindGrid(crud);
		if (change != "initial")
			await crud.RefreshGrid();
		using var largeFont = new Font(native.Font.FontFamily, 19);
		if (change == "font")
			native.Font = largeFont;
		if (change == "width")
			native.Columns[0].Width += 31;
		foreach (var row in rows)
			row.Value = change == "multiline" ? "Updated first line\r\nSecond line\r\nThird line" : "Updated " + row.Value;
		Task pending = null;
		var dispatchedDuringSizing = 0;
		using var heartbeat = new System.Windows.Forms.Timer { Interval = 1 };
		heartbeat.Tick += (_, _) => {
			if (pending is { IsCompleted: false } && native.RowsCount == rows.Length + 1
				&& Convert.ToString(native[1, 0].Value).StartsWith("Updated", StringComparison.Ordinal))
				dispatchedDuringSizing++;
		};
		heartbeat.Start();
		pending = crud.RefreshGrid();
		await pending;
		heartbeat.Stop();
		Assert.That(dispatchedDuringSizing, Is.GreaterThan(0), "Native measurement must yield to real WM_TIMER dispatch after the prepared cells have been applied.");
		Assert.That(native.RowsCount, Is.EqualTo(rows.Length + 1));
		for (var row = 0; row < native.RowsCount; row++)
			Assert.That(native.Rows[row].Height, Is.EqualTo(native.Rows.MeasureRowHeight(row, false, 0, native.ColumnsCount - 1)));
		if (change == "initial") {
			using var legacy = NewGrid(owner);
			legacy.PrepareReadOnlyCellsInBackground = false;
			legacy.GridBindings = SizingColumns();
			await legacy.SetDataSource(new RecordingSource { Rows = rows });
			await legacy.RefreshGrid();
			var expected = FindGrid(legacy);
			Assert.That(Enumerable.Range(0, native.ColumnsCount).Select(column => native.Columns[column].Width),
				Is.EqualTo(Enumerable.Range(0, expected.ColumnsCount).Select(column => expected.Columns[column].Width)));
			Assert.That(Enumerable.Range(0, native.RowsCount).Select(row => native.Rows[row].Height),
				Is.EqualTo(Enumerable.Range(0, expected.RowsCount).Select(row => expected.Rows[row].Height)));
		}
	});

	[Test]
	public void ResizingDuringWarmNativeSizingDoesNotReadOrPrepareTheSameSnapshotAgain() => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(2600, 650);
		using var crud = NewGrid(owner);
		var ownerThread = Environment.CurrentManagedThreadId;
		var evaluations = 0;
		var columns = Enumerable.Range(0, 20).Select(column => new CrudGridColumn<Row> {
			ColumnName = "C" + column, DataType = typeof(string), ExpandsToFit = column == 19,
			PropertyValue = row => { Interlocked.Increment(ref evaluations); return row.Value; }
		}).ToArray();
		crud.GridBindings = columns;
		var source = new RecordingSource { Rows = Enumerable.Range(0, 500).Select(index => new Row { Value = index.ToString("D3") }).ToArray() };
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var native = FindGrid(crud);
		var reads = source.ReadThreads.Count;
		var beforeEvaluations = Volatile.Read(ref evaluations);
		foreach (var row in source.Rows)
			row.Value = "New";
		Task pending = null;
		var resizes = 0;
		var ticks = 0;
		var maximumGap = TimeSpan.Zero;
		var previousTick = Stopwatch.GetTimestamp();
		using var resizing = new System.Windows.Forms.Timer { Interval = 1 };
		resizing.Tick += (_, _) => {
			var now = Stopwatch.GetTimestamp();
			maximumGap = maximumGap > Stopwatch.GetElapsedTime(previousTick, now) ? maximumGap : Stopwatch.GetElapsedTime(previousTick, now);
			previousTick = now;
			ticks++;
			if (pending is { IsCompleted: false } && resizes < 8 && native.RowsCount == 501 && Equals(native[1, 0].Value, "New")) {
				owner.Width += resizes++ % 2 == 0 ? 23 : -17;
				owner.PerformLayout();
			}
		};
		resizing.Start();
		var started = Stopwatch.StartNew();
		pending = crud.RefreshGrid();
		await pending;
		resizing.Stop();
		TestContext.Out.WriteLine($"Warm 500x20 refresh with {resizes} resizes: reads={source.ReadThreads.Count - reads}, preparations={(Volatile.Read(ref evaluations) - beforeEvaluations) / 10000.0}, ticks={ticks}, elapsed={started.Elapsed.TotalMilliseconds:F1}ms, max timer gap={maximumGap.TotalMilliseconds:F1}ms.");
		Assert.That(resizes, Is.GreaterThan(0), "Exercise resizing after the new cells are accepted, during yielding native measurement.");
		Assert.That(source.ReadThreads.Count - reads, Is.EqualTo(1), "A geometry change must resize committed cells without collecting or querying the same data again.");
		Assert.That(Volatile.Read(ref evaluations) - beforeEvaluations, Is.EqualTo(10000), "Resizing must not repeat the worker's column evaluation/conversion pass.");
		Assert.That(source.ReadThreads, Is.All.Not.EqualTo(ownerThread));
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(500));
		Assert.That(native[500, 19].Value, Is.EqualTo("New"));
		Assert.That(native.Rows[1].Height, Is.EqualTo(native.Rows.MeasureRowHeight(1, false, 0, native.ColumnsCount - 1)));
	});

	[TestCase("bindings")]
	[TestCase("dispose")]
	[TestCase("font")]
	public void NativeSizingRejectsChangedBindingsDisposalOrFontGeometryBetweenMessageLoopSlices(string change) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.GridBindings = SizingColumns();
		await crud.SetDataSource(new RecordingSource { Rows = Enumerable.Range(0, 180).Select(index => new Row { Value = "Original row " + index }).ToArray() });
		var native = FindGrid(crud);
		var sizing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		native.Columns.ColumnWidthChanged += (_, _) => sizing.TrySetResult();
		var pending = crud.RefreshGrid();
		await sizing.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(pending.IsCompleted, Is.False);
		using var largeFont = new Font(native.Font.FontFamily, 19);
		if (change == "dispose")
			crud.Dispose();
		else if (change == "bindings")
			crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Current column", DataType = typeof(string), PropertyValue = row => "Current data" } };
		else
			native.Font = largeFont;
		await pending;
		if (change == "dispose") {
			Assert.That(crud.IsDisposed, Is.True);
		} else if (change == "bindings") {
			Assert.That(native.ColumnsCount, Is.EqualTo(1));
			Assert.That(native[0, 0].Value, Is.EqualTo("Current column"));
			Assert.That(native[1, 0].Value, Is.EqualTo("Current data"));
		} else {
			Assert.That(native.Rows[1].Height, Is.EqualTo(native.Rows.MeasureRowHeight(1, false, 0, native.ColumnsCount - 1)));
		}
	});

	[TestCase(0)]
	[TestCase(11)]
	public void ExplicitColumnWidthChangesDuringNativeSizingSurviveCompletionAndLaterRefresh(int column) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.GridBindings = SizingColumns();
		await crud.SetDataSource(new RecordingSource { Rows = Enumerable.Range(0, 180).Select(index => new Row { Value = "Row " + index }).ToArray() });
		var native = FindGrid(crud);
		var sizing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		native.Columns.ColumnWidthChanged += (_, _) => sizing.TrySetResult();
		var pending = crud.RefreshGrid();
		await sizing.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(pending.IsCompleted, Is.False);
		native.Columns[column].Width = 666;
		await pending;
		Assert.That(native.Columns[column].Width, Is.EqualTo(666));
		await crud.RefreshGrid();
		Assert.That(native.Columns[column].Width, Is.EqualTo(666));
	});

	[Test]
	public void NativeSizingHonorsColumnMinimumAndMaximumWidthsWithoutRetryingItsOwnClampedValues() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		using var legacy = NewGrid(owner);
		legacy.PrepareReadOnlyCellsInBackground = false;
		var rows = Enumerable.Range(0, 180).Select(index => new Row { Value = "Representative row " + index }).ToArray();
		var native = FindGrid(crud);
		var expected = FindGrid(legacy);
		foreach (var grid in new[] { native, expected })
			grid.Columns.ColumnsAdded += (_, _) => {
				grid.Columns[0].MinimalWidth = 400;
				grid.Columns[1].MaximalWidth = 45;
			};
		crud.GridBindings = SizingColumns();
		legacy.GridBindings = SizingColumns();
		await crud.SetDataSource(new RecordingSource { Rows = rows });
		await legacy.SetDataSource(new RecordingSource { Rows = rows });
		await crud.RefreshGrid().WaitAsync(TimeSpan.FromSeconds(10));
		await legacy.RefreshGrid();
		Assert.That(native.Columns[0].Width, Is.EqualTo(400));
		Assert.That(native.Columns[1].Width, Is.EqualTo(45));
		Assert.That(Enumerable.Range(0, native.ColumnsCount).Select(column => native.Columns[column].Width),
			Is.EqualTo(Enumerable.Range(0, expected.ColumnsCount).Select(column => expected.Columns[column].Width)));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void TargetedRefreshDoesNotPublishAfterReadPermissionIsRevoked(bool prepare) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.PrepareReadOnlyCellsInBackground = prepare;
		var original = new Row { Value = "Original" };
		var replacement = new Row { Value = "Not authorized for display" };
		var source = new RecordingSource { Rows = new[] { original }, Refreshed = replacement,
			RefreshEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
			RefreshRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
		crud.GridBindings = new[] { TextColumn() };
		await crud.SetDataSource(source);
		await crud.RefreshGrid();
		var pending = crud.RefreshEntityAsync(original);
		await source.RefreshEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		crud.Capabilities = (DataSourceCapabilities)0;
		source.RefreshRelease.TrySetResult();
		Assert.That(await pending, Is.False);
		Assert.That(crud.VisibleEntities, Does.Not.Contain(replacement));
		Assert.That(FindGrid(crud)[1, 0].Value, Is.EqualTo("Original"));
	});

	[Test]
	public void GridTitleAlignmentDefaultsToExistingCenteredPresentationAndSupportsLeftAlignment() {
		using var crud = new CrudGrid { PersistSettings = false };
		Assert.That(crud.GridTitleAlignment, Is.EqualTo(ContentAlignment.MiddleCenter));
		crud.GridTitleAlignment = ContentAlignment.MiddleLeft;
		crud.GridTitle = "Processes";
		var caption = (Label)crud.Controls.Find("_titleLabel", true).Single();
		Assert.That(caption.TextAlign, Is.EqualTo(ContentAlignment.MiddleLeft));
		Assert.That(caption.Text, Is.EqualTo("Processes"));
	}

	[TestCase(false, false, ContentAlignment.MiddleLeft)]
	[TestCase(true, false, ContentAlignment.MiddleLeft)]
	[TestCase(false, true, ContentAlignment.MiddleLeft)]
	[TestCase(true, true, ContentAlignment.MiddleLeft)]
	[TestCase(false, false, ContentAlignment.MiddleCenter)]
	[TestCase(true, true, ContentAlignment.MiddleCenter)]
	[TestCase(false, false, ContentAlignment.MiddleRight)]
	[TestCase(true, true, ContentAlignment.MiddleRight)]
	public void TitleUsesOnlyVisibleActionSpaceAndKeepsSearchAtTheRight(bool canCreate, bool canDelete, ContentAlignment alignment) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.GridTitleAlignment = alignment;
		crud.GridTitle = "Processes";
		crud.GridBindings = new[] { TextColumn() };
		var row = new Row { Value = "Selected row" };
		await crud.SetDataSource(new RecordingSource { Rows = new[] { row } });
		await crud.RefreshGrid();
		crud.SelectedEntity = row;
		var capabilities = DataSourceCapabilities.CanSearch;
		if (canCreate)
			capabilities |= DataSourceCapabilities.CanCreate;
		if (canDelete)
			capabilities |= DataSourceCapabilities.CanDelete;
		crud.Capabilities = capabilities;
		var caption = (Label)crud.Controls.Find("_titleLabel", true).Single();
		var search = crud.Controls.Find("_searchTextBox", true).Single();
		var create = (System.Windows.Forms.Button)crud.Controls.Find("_createButton", true).Single();
		var delete = (System.Windows.Forms.Button)crud.Controls.Find("_deleteButton", true).Single();
		var header = caption.Parent;
		foreach (var width in new[] { 1000, 440 }) {
			owner.ClientSize = new Size(width, 600);
			header.PerformLayout();
			Assert.That(create.Visible, Is.EqualTo(canCreate));
			Assert.That(delete.Visible, Is.EqualTo(canDelete));
			var actions = new[] { create, delete }.Where(button => button.Visible).ToArray();
			var expectedLeft = actions.Length == 0 ? header.Padding.Left : actions.Max(button => button.Right + button.Margin.Right);
			Assert.That(caption.Left, Is.EqualTo(expectedLeft), "Unavailable CRUD actions must not leave empty space before a left-aligned title.");
			Assert.That(caption.Width, Is.GreaterThan(0));
			Assert.That(caption.TextAlign, Is.EqualTo(alignment));
			Assert.That(caption.Right, Is.LessThanOrEqualTo(search.Left));
			Assert.That(search.Right + search.Margin.Right, Is.EqualTo(header.ClientSize.Width - header.Padding.Right));
			TestContext.Out.WriteLine($"Header: {header.ClientSize}; DPI: {crud.DeviceDpi}; Create: {create.Visible}; Delete: {delete.Visible}; Title: {caption.Bounds}; Search: {search.Bounds}; Alignment: {alignment}");
		}
		var screenshotDirectory = Environment.GetEnvironmentVariable("SPHERE10_VALIDATION_DIRECTORY");
		if (!string.IsNullOrWhiteSpace(screenshotDirectory)) {
			Directory.CreateDirectory(screenshotDirectory);
			using var image = new Bitmap(crud.Width, crud.Height);
			crud.DrawToBitmap(image, crud.ClientRectangle);
			var path = Path.Combine(screenshotDirectory, $"CrudGrid-Title-{canCreate}-{canDelete}-{alignment}.png");
			image.Save(path, System.Drawing.Imaging.ImageFormat.Png);
			TestContext.AddTestAttachment(path);
		}
		// Selection-dependent Delete also releases its slot without another binding.
		crud.SelectedEntity = null;
		header.PerformLayout();
		Assert.That(delete.Visible, Is.False);
		Assert.That(caption.Left, Is.EqualTo(canCreate ? create.Right + create.Margin.Right : header.Padding.Left));
	});

	private static CrudGrid NewGrid(Form owner) {
		var crud = new CrudGrid { Dock = DockStyle.Fill, PersistSettings = false, SuppressLoadingIndicator = true,
			PageMode = CrudGridPageMode.Single, AutoSizeRowsOnRefresh = false, PrepareReadOnlyCellsInBackground = true };
		owner.Controls.Add(crud);
		return crud;
	}

	private static CrudGridColumn<Row> TextColumn() => new() { ColumnName = "Value", DataType = typeof(string), PropertyValue = row => row.Value };

	private static CrudGridColumn<Row>[] SizingColumns() => Enumerable.Range(0, 12).Select(column => new CrudGridColumn<Row> {
		ColumnName = "Column " + column, DataType = typeof(string), PropertyValue = row => row.Value + " / column " + column
	}).ToArray();

	private static Grid FindGrid(CrudGrid crud) => (Grid)crud.Controls.Find("_grid", true).Single();

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 600) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The background CRUD preparation fixture timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try { await test(owner); completed = true; }
			catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	private sealed class Row {
		public object Value { get; set; }
		public bool HasValue { get; init; } = true;
	}

	private sealed class RecordingSource : ListDataSource<Row> {
		public RecordingSource() : base(new ExtendedList<Row>()) { }

		public Row[] Rows { get; set; } = Array.Empty<Row>();
		public Row Refreshed { get; set; }
		public ConcurrentQueue<int> ReadThreads { get; } = new();
		public ConcurrentQueue<int> EnumerationThreads { get; } = new();
		public ConcurrentQueue<int> RefreshThreads { get; } = new();
		public TaskCompletionSource RefreshEntered { get; init; }
		public TaskCompletionSource RefreshRelease { get; init; }

		public override Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			ReadThreads.Enqueue(Environment.CurrentManagedThreadId);
			var rows = Rows;
			return Task.FromResult(new DataSourceItems<Row> {
				Items = rows.Select(row => { EnumerationThreads.Enqueue(Environment.CurrentManagedThreadId); return row; }), Page = 0, TotalCount = rows.Length
			});
		}

		public override async Task RefreshRangeAsync(Row[] entities) {
			RefreshThreads.Enqueue(Environment.CurrentManagedThreadId);
			RefreshEntered?.TrySetResult();
			if (RefreshRelease != null)
				await RefreshRelease.Task;
			for (var index = 0; index < entities.Length; index++)
				entities[index] = Refreshed;
		}
	}

	private sealed class ValueChanges : SourceGrid.Cells.Controllers.ControllerBase {
		public int Changing { get; private set; }

		public int Changed { get; private set; }

		public override void OnValueChanging(CellContext sender, ValueChangeEventArgs args) => Changing++;

		public override void OnValueChanged(CellContext sender, EventArgs args) => Changed++;
	}

	[TypeConverter(typeof(DisplayValueConverter))]
	public sealed class DisplayValue {
		public int Number { get; init; }
	}

	public sealed class DisplayValueConverter : TypeConverter {
		public static ConcurrentQueue<int> Threads { get; } = new();
		public static ConcurrentQueue<int> ConfigurationThreads { get; } = new();

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
			ConfigurationThreads.Enqueue(Environment.CurrentManagedThreadId);
			return base.GetStandardValues(context);
		}
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
			Threads.Enqueue(Environment.CurrentManagedThreadId);
			return destinationType == typeof(string) && value is DisplayValue display ? "Formatted " + display.Number : base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
