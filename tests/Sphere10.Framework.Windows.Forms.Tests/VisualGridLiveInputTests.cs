// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridLiveInputTests {

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void NativeLeftClicksChangeSelectionDuringSuppressedPreparationAndCompletionRetainsTheLatestChoice(bool leftClickToDeselect, bool selectOnMouseUp) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true, leftClickToDeselect, selectOnMouseUp);
		var source = await BindAsync(crud);
		var originalRows = source.Rows;
		var grid = FindGrid(crud);
		crud.SelectedEntity = originalRows[0];
		var selected = new ConcurrentQueue<object>();
		crud.EntitySelected += (_, entity) => selected.Enqueue(entity);
		Task pending = null;
		await using var finishRead = Tools.Scope.ExecuteOnDisposeAsync(async () => { source.ReleaseRead(); if (pending != null) await pending; });

		// Hold the replacement on its worker while native input targets the previous complete page.
		source.Rows = new[] { originalRows[2], originalRows[1], originalRows[0] };
		source.HoldNextRead();
		pending = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(source.ReadThread, Is.Not.EqualTo(Environment.CurrentManagedThreadId));
		Assert.That(pending.IsCompleted, Is.False);
		Assert.That(grid.Enabled, Is.True);
		Assert.That(crud.VisibleEntities, Is.EqualTo(originalRows));
		ClickCell(grid, 2, MouseButtons.Left);
		Assert.That(crud.SelectedEntity, Is.SameAs(originalRows[1]), "An enabled live grid must accept selection before the held read completes.");
		Assert.That(grid.Selection.IsSelectedRow(2), Is.True);
		ClickCell(grid, 3, MouseButtons.Left);
		Assert.That(crud.SelectedEntity, Is.SameAs(originalRows[2]));
		Assert.That(selected, Is.EqualTo(new[] { originalRows[1], originalRows[2] }));
		Assert.That(pending.IsCompleted, Is.False, "The clicks must be handled during the read, rather than after it.");

		// The user's last choice follows its entity when the accepted snapshot reorders the rows.
		source.ReleaseRead();
		await pending;
		Assert.That(crud.SelectedEntity, Is.SameAs(originalRows[2]));
		Assert.That(grid.Selection.IsSelectedRow(1), Is.True);
		Assert.That(grid.Selection.IsSelectedRow(3), Is.False);
		Assert.That(crud.VisibleEntities, Is.EqualTo(source.Rows));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void NativeRightPressRetargetsTheContextMenuBeforeSuppressedPreparationCompletes(bool leftClickToDeselect) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true, leftClickToDeselect);
		var source = await BindAsync(crud);
		var rows = source.Rows;
		var grid = FindGrid(crud);
		var menu = FindMenu(crud);
		using var closeMenu = Tools.Scope.ExecuteOnDispose(menu.Close);
		crud.SelectedEntity = rows[0];
		object contextTarget = null;
		menu.Opening += (_, _) => contextTarget = crud.SelectedEntity;
		Task pending = null;
		await using var finishRead = Tools.Scope.ExecuteOnDisposeAsync(async () => { source.ReleaseRead(); if (pending != null) await pending; });

		// Mouse-down must establish the right-click target even though the refresh is logically busy.
		source.HoldNextRead();
		pending = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var point = CellCenter(grid, 2);
		SendMouseDown(grid, point, MouseButtons.Right);
		using var releaseCapture = Tools.Scope.ExecuteOnDispose(() => grid.Capture = false);
		Assert.That(crud.SelectedEntity, Is.SameAs(rows[1]), "A right press must replace the old context target before a menu opens.");
		Assert.That(grid.Selection.IsSelectedRow(2), Is.True);
		CompleteClick(grid, point, 2, MouseButtons.Right);
		Assert.That(menu.Visible, Is.True);
		Assert.That(contextTarget, Is.SameAs(rows[1]));
		Assert.That(pending.IsCompleted, Is.False);
		Assert.That(new CellContext(grid, new Position(2, 0)).IsEditing(), Is.False);
		menu.Close();

		source.ReleaseRead();
		await pending;
		Assert.That(crud.SelectedEntity, Is.SameAs(rows[1]));
		Assert.That(grid.Selection.IsSelectedRow(2), Is.True);
	});

	[TestCase(false)]
	[TestCase(true)]
	public void SuppressedPreparationAllowsDeselectingAnAlreadySelectedRow(bool selectOnMouseUp) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, true, true, selectOnMouseUp);
		var source = await BindAsync(crud);
		var grid = FindGrid(crud);
		crud.SelectedEntity = source.Rows[1];
		var deselected = new ConcurrentQueue<object>();
		crud.EntityDeselected += (_, entity) => deselected.Enqueue(entity);
		Task pending = null;
		await using var finishRead = Tools.Scope.ExecuteOnDisposeAsync(async () => { source.ReleaseRead(); if (pending != null) await pending; });
		source.HoldNextRead();
		pending = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		ClickCell(grid, 2, MouseButtons.Left);
		Assert.That(crud.SelectedEntity, Is.Null);
		Assert.That(grid.Selection.IsSelectedRow(2), Is.False);
		Assert.That(deselected, Is.EqualTo(new[] { source.Rows[1] }));
		Assert.That(pending.IsCompleted, Is.False);
		source.ReleaseRead();
		await pending;
		Assert.That(crud.SelectedEntity, Is.Null, "Completing the refresh must not restore a selection the user cleared while waiting.");
		Assert.That(grid.Selection.IsSelectedRow(2), Is.False);
	});

	[Test]
	public void OrdinaryLoadingKeepsNativeInputAndItsContextMenuGated() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, false, true);
		var source = await BindAsync(crud);
		var grid = FindGrid(crud);
		var menu = FindMenu(crud);
		using var closeMenu = Tools.Scope.ExecuteOnDispose(menu.Close);
		crud.SelectedEntity = source.Rows[0];
		Task pending = null;
		await using var finishRead = Tools.Scope.ExecuteOnDisposeAsync(async () => { source.ReleaseRead(); if (pending != null) await pending; });
		source.HoldNextRead();
		pending = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(grid.Enabled, Is.False, "Normal loading must retain its blocking surface.");
		ClickCell(grid, 2, MouseButtons.Left);
		ClickCell(grid, 3, MouseButtons.Right);
		Assert.That(crud.SelectedEntity, Is.SameAs(source.Rows[0]));
		Assert.That(menu.Visible, Is.False);
		Assert.That(pending.IsCompleted, Is.False);
		source.ReleaseRead();
		await pending;
		Assert.That(grid.Enabled, Is.True);
		Assert.That(crud.SelectedEntity, Is.SameAs(source.Rows[0]));
		Assert.That(grid.Selection.IsSelectedRow(1), Is.True);
	});

	[TestCase(false)]
	[TestCase(true)]
	public void FirstBindingHasNoInteractiveRowsOrContextTargetBeforeItsReadCompletes(bool suppressLoadingIndicator) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, suppressLoadingIndicator, true);
		var source = new ControlledSource();
		source.HoldNextRead();
		await crud.SetDataSource(source, DataSourceCapabilities.CanRead);
		var grid = FindGrid(crud);
		var menu = FindMenu(crud);
		using var closeMenu = Tools.Scope.ExecuteOnDispose(menu.Close);
		Task pending = null;
		await using var finishRead = Tools.Scope.ExecuteOnDisposeAsync(async () => { source.ReleaseRead(); if (pending != null) await pending; });
		pending = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(grid.Visible, Is.False, "Initial loading must not expose designer rows as interactive content.");
		Assert.That(crud.VisibleEntities, Is.Empty);
		SendMouseDown(grid, new Point(10, 10), MouseButtons.Right);
		using var releaseCapture = Tools.Scope.ExecuteOnDispose(() => grid.Capture = false);
		CompleteClick(grid, new Point(10, 10), -1, MouseButtons.Right);
		Assert.That(crud.SelectedEntity, Is.Null);
		Assert.That(menu.Visible, Is.False);
		Assert.That(pending.IsCompleted, Is.False);

		// The same row input becomes usable after the first complete page is actually published.
		source.ReleaseRead();
		await pending;
		Assert.That(grid.Visible, Is.True);
		ClickCell(grid, 2, MouseButtons.Left);
		Assert.That(crud.SelectedEntity, Is.SameAs(source.Rows[1]));
	});

	private static CrudGrid NewGrid(Form owner, bool suppress, bool leftClickToDeselect, bool selectOnMouseUp = false) {
		var crud = new CrudGrid {
			Dock = DockStyle.Fill, PersistSettings = false, SuppressLoadingIndicator = suppress, AllowCellEditing = false,
			LeftClickToDeselect = leftClickToDeselect, SelectOnMouseUp = selectOnMouseUp, PageMode = CrudGridPageMode.Single,
			AutoSizeRowsOnRefresh = false, PrepareReadOnlyCellsInBackground = true
		};
		owner.Controls.Add(crud);
		crud.GridBindings = new[] { new CrudGridColumn<Row> { ColumnName = "Name", DataType = typeof(string), PropertyValue = row => row.Name, ExpandsToFit = true } };
		return crud;
	}

	private static async Task<ControlledSource> BindAsync(CrudGrid crud) {
		var source = new ControlledSource();
		await crud.SetDataSource(source, DataSourceCapabilities.CanRead);
		await crud.RefreshGrid();
		return source;
	}

	private static Grid FindGrid(CrudGrid crud) => (Grid)crud.Controls.Find("_grid", true).Single();

	private static ContextMenuStrip FindMenu(CrudGrid crud)
		=> (ContextMenuStrip)typeof(CrudGrid).GetField("_selectionContextMenuStrip", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(crud);

	private static Point CellCenter(Grid grid, int row) {
		var bounds = grid.PositionToRectangle(new Position(row, 0));
		Assert.That(bounds.IsEmpty, Is.False, "The test must target an actual displayed row coordinate.");
		return new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
	}

	private static void ClickCell(Grid grid, int row, MouseButtons button) {
		var point = CellCenter(grid, row);
		using var releaseCapture = Tools.Scope.ExecuteOnDispose(() => grid.Capture = false);
		SendMouseDown(grid, point, button);
		CompleteClick(grid, point, row, button);
	}

	private static void SendMouseDown(Grid grid, Point point, MouseButtons button) {
		var coordinates = (IntPtr)((point.Y << 16) | (point.X & ushort.MaxValue));
		var message = button == MouseButtons.Right ? WinAPI.WindowMessageFlags.WM_RBUTTONDOWN : WinAPI.WindowMessageFlags.WM_LBUTTONDOWN;
		WinAPI.USER32.SendMessage(grid.Handle, message, button == MouseButtons.Right ? (IntPtr)2 : (IntPtr)1, coordinates);
	}

	private static void CompleteClick(Grid grid, Point point, int row, MouseButtons button) {
		// The owned form is off-screen: deliver the coordinate click/cell event without moving the user's desktop pointer.
		var args = new MouseEventArgs(button, 1, point.X, point.Y, 0);
		typeof(GridVirtual).GetMethod("OnMouseClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grid, new object[] { args });
		if (button == MouseButtons.Left && row > 0)
			grid.Controller.OnClick(new CellContext(grid, new Position(row, 0)), args);
		typeof(GridVirtual).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grid, new object[] { args });
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(850, 450) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The live CRUD input fixture timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
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

	private sealed class Row {
		public string Name { get; init; }
	}

	private sealed class ControlledSource : ListDataSource<Row> {
		private int _holdNext;

		public ControlledSource()
			: base(new ExtendedList<Row>()) {
		}

		public Row[] Rows { get; set; } = new[] { new Row { Name = "First" }, new Row { Name = "Second" }, new Row { Name = "Third" } };

		public TaskCompletionSource ReadStarted { get; private set; }

		public TaskCompletionSource ContinueRead { get; private set; }

		public int ReadThread { get; private set; }

		public void HoldNextRead() {
			ReadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			ContinueRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			Volatile.Write(ref _holdNext, 1);
		}

		public void ReleaseRead() => ContinueRead?.TrySetResult();

		public override async Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			ReadThread = Environment.CurrentManagedThreadId;
			var rows = Rows;
			if (Interlocked.Exchange(ref _holdNext, 0) != 0) {
				ReadStarted.TrySetResult();
				await ContinueRead.Task.ConfigureAwait(false);
			}
			return new DataSourceItems<Row> { Items = rows, Page = 0, TotalCount = rows.Length };
		}
	}
}
