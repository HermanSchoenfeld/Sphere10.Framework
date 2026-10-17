// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridSnapshotControllerTests {
	[TestCase(false)]
	[TestCase(true)]
	public void ReadCapabilitiesRetainNativeHeadersAndRowsThroughSelectionModalEditingAndRefresh(bool save) => RunWithMessageLoop(async owner => {
		using var crud = CreateGrid(owner);
		var rows = Enumerable.Range(1, 90).Select(index => new Row { Id = index, Name = $"Original {index}" }).ToArray();
		var source = new EditableSnapshotSource();
		var controller = new CrudGridSnapshotController<Row>(crud, source, row => row.Id, _ => Task.FromResult(rows));
		var readCapabilities = source.Capabilities & (DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage);
		crud.EntitySelected += (_, _) => crud.Capabilities = readCapabilities | DataSourceCapabilities.CanCreate | DataSourceCapabilities.CanUpdate | DataSourceCapabilities.CanDelete;
		crud.EntityDeselected += (_, _) => crud.Capabilities = readCapabilities | DataSourceCapabilities.CanCreate;
		await controller.RefreshAsync();
		var native = (SourceGrid.Grid)crud.Controls.Find("_grid", true).Single();
		var bounds = native.PositionToRectangle(new SourceGrid.Position(1, 0));
		var point = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
		Sphere10.Framework.Windows.WinAPI.USER32.SendMessage(native.Handle, Sphere10.Framework.Windows.WinAPI.WindowMessageFlags.WM_RBUTTONDOWN, (IntPtr)2,
			(IntPtr)((point.Y << 16) | (point.X & ushort.MaxValue)));
		Assert.That(crud.SelectedEntity, Is.SameAs(rows[0]), "Native right-click selection must update the editing target before its permissions are read.");
		var editing = crud.EditSelectedEntity();
		CrudEntityEditorDialog dialog = null;
		for (var attempt = 0; attempt < 100 && dialog == null; attempt++) {
			dialog = System.Windows.Forms.Application.OpenForms.OfType<CrudEntityEditorDialog>().SingleOrDefault();
			if (dialog == null) await Task.Delay(10);
		}
		Assert.That(dialog, Is.Not.Null, "The real grid operation must show the native CRUD editor.");
		{
			using var closeEditorDialog = Tools.Scope.ExecuteOnDispose(() => {
				if (!dialog.IsDisposed) {
					dialog.CancelChanges();
					dialog.Close();
				}
			});
			Assert.That(Sphere10.Framework.Windows.WinAPI.USER32.IsWindowEnabled(owner.Handle), Is.False);
			var editor = dialog.Controls.Find("_propertyGrid", true).OfType<PropertyGrid>().Single();
			TypeDescriptor.GetProperties(editor.SelectedObject)[nameof(Row.Name)].SetValue(editor.SelectedObject, "Edited Name");
			Assert.That(dialog.HasChanges, Is.True);
			// Suppress outside management commands while retaining the independent inventory permissions.
			crud.Capabilities = readCapabilities;
			await controller.RefreshAsync();
			Assert.That(crud.Capabilities.HasFlag(DataSourceCapabilities.CanRead), Is.True);
			Assert.That(native.ColumnsCount, Is.EqualTo(2));
			Assert.That(native.RowsCount, Is.EqualTo(91));
			Assert.That(native[0, 0].Value, Is.EqualTo("ID"));
			Assert.That(native[0, 1].Value, Is.EqualTo("Name"));
			Assert.That(native.Visible, Is.True);
			Assert.That(native.PositionToRectangle(new SourceGrid.Position(0, 0)).Height, Is.GreaterThan(0));
			Assert.That(native.PositionToRectangle(new SourceGrid.Position(1, 1)).Width, Is.GreaterThan(0));
			Assert.That(controller.Count, Is.EqualTo(90));
			Assert.That(Sphere10.Framework.Windows.WinAPI.USER32.IsWindowEnabled(owner.Handle), Is.False, "A refresh must not release native editor modality.");
			crud.Capabilities = source.Capabilities;
			dialog.Controls.Find(save ? "_saveButton" : "_cancelButton", true).OfType<Button>().Single().PerformClick();
			await editing.WaitAsync(TimeSpan.FromSeconds(5));
			await controller.RefreshAsync();
			Assert.That(source.Updates, Is.EqualTo(save ? 1 : 0));
			Assert.That(rows[0].Name, Is.EqualTo(save ? "Edited Name" : "Original 1"));
			Assert.That(crud.SelectedEntity, Is.SameAs(rows[0]));
			Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(90));
			Assert.That(native.RowsCount, Is.EqualTo(91));
			Assert.That(native[0, 1].Value, Is.EqualTo("Name"));
			Assert.That(native[1, 1].Value, Is.EqualTo(rows[0].Name));
			Assert.That(Sphere10.Framework.Windows.WinAPI.USER32.IsWindowEnabled(owner.Handle), Is.True);
		}
	});

	[Test]
	public void TypedSnapshotsPreserveSelectionAcrossReplacedRowInstances() => RunWithMessageLoop(async owner => {
		using var grid = CreateGrid(owner);
		var rows = new[] { new Row { Id = 1, Name = "First" }, new Row { Id = 2, Name = "Second" } };
		var controller = new CrudGridSnapshotController<Row>(grid, row => row.Id, _ => Task.FromResult(rows));
		await controller.RefreshAsync();
		grid.SelectedEntity = rows[1];
		rows = new[] { new Row { Id = 1, Name = "Updated first" }, new Row { Id = 2, Name = "Updated second" } };
		await controller.RefreshAsync();
		Assert.That(grid.SelectedEntity, Is.SameAs(rows[1]));
		Assert.That(grid.VisibleEntities.Cast<Row>().Select(row => row.Name), Is.EqualTo(new[] { "Updated first", "Updated second" }));
		Assert.That(controller.Count, Is.EqualTo(2));
		var snapshot = controller.Rows;
		snapshot[0] = null;
		Assert.That(controller.Rows[0], Is.SameAs(rows[0]));
		Assert.That(((Row[])((ICrudGridSnapshotController)controller).CaptureSnapshot())[0], Is.SameAs(rows[0]));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void UserSelectionDuringBackgroundPreparationSurvivesTheAcceptedSnapshot(bool deselect) => RunWithMessageLoop(async owner => {
		using var grid = CreateGrid(owner);
		grid.PrepareReadOnlyCellsInBackground = true;
		grid.SuppressLoadingIndicator = true;
		grid.UseEntityReferenceForLookup = true;
		var rows = new[] { new Row { Id = 1, Name = "First" }, new Row { Id = 2, Name = "Second" } };
		var original = rows;
		using var release = new ManualResetEventSlim();
		using var releaseOnFailure = Tools.Scope.ExecuteOnDispose(release.Set);
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var block = 0;
		grid.GridBindings = new[] {
			new CrudGridColumn<Row> { ColumnName = "Name", DataType = typeof(string), CanEditCell = false, PropertyValue = row => {
				if (Interlocked.Exchange(ref block, 0) != 0) {
					started.TrySetResult();
					release.Wait();
				}
				return row.Name;
			} }
		};
		var controller = new CrudGridSnapshotController<Row>(grid, row => row.Id, _ => Task.FromResult(rows));
		await controller.RefreshAsync();
		grid.SelectedEntity = original[0];
		rows = new[] { new Row { Id = 1, Name = "Updated first" }, new Row { Id = 2, Name = "Updated second" } };
		Interlocked.Exchange(ref block, 1);
		var refresh = controller.RefreshAsync();
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		grid.SelectedEntity = deselect ? null : original[1];
		release.Set();
		await refresh;
		Assert.That(grid.VisibleEntities.Cast<Row>().Select(row => row.Name), Is.EqualTo(new[] { "Updated first", "Updated second" }));
		Assert.That(grid.SelectedEntity, deselect ? Is.Null : Is.SameAs(rows[1]), "A background update must not restore a selection captured before the user's input.");
	});

	[Test]
	public void AnExistingSourceRetainsItsConfiguredDefaultSort() => RunWithMessageLoop(async owner => {
		using var grid = CreateGrid(owner);
		var source = new SnapshotDataSource<Row>();
		var controller = new CrudGridSnapshotController<Row>(grid, source, row => row.Id,
			_ => Task.FromResult(new[] { new Row { Id = 2 }, new Row { Id = 1 } }));
		controller.SetDefaultSort(nameof(Row.Id), SortDirection.Ascending);
		await controller.RefreshAsync();
		Assert.That(grid.VisibleEntities.Cast<Row>().Select(row => row.Id), Is.EqualTo(new[] { 1, 2 }));
		Assert.That(source.Snapshot.Select(row => row.Id), Is.EqualTo(new[] { 2, 1 }), "Sorting must not mutate the collected membership.");
	});

	[Test]
	public void DisposedGridIgnoresLateCollectedResultsAndCallbacks() => RunWithMessageLoop(async owner => {
		using var grid = CreateGrid(owner);
		var collected = new TaskCompletionSource<Row[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		var changes = 0;
		var controller = new CrudGridSnapshotController<Row>(grid, row => row.Id, _ => collected.Task, _ => changes++);
		var refresh = controller.RefreshAsync();
		grid.Dispose();
		collected.SetResult(new[] { new Row { Id = 1 } });
		await refresh;
		Assert.That(controller.Rows, Is.Empty);
		Assert.That(changes, Is.Zero);
	});

	[Test]
	public void CancellationBeforeCollectedResultsArePublishedPreservesThePreviousSnapshot() => RunWithMessageLoop(async owner => {
		using var grid = CreateGrid(owner);
		using var cancellation = new CancellationTokenSource();
		var collected = new TaskCompletionSource<Row[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		var controller = new CrudGridSnapshotController<Row>(grid, row => row.Id, _ => collected.Task);
		var refresh = controller.RefreshAsync(cancellation.Token);
		cancellation.Cancel();
		collected.SetResult(new[] { new Row { Id = 1 } });
		await Assert.ThatAsync(async () => await refresh, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(controller.Rows, Is.Empty);
		Assert.That(grid.VisibleEntities, Is.Empty);
	});

	[Test]
	public void OverlappingRefreshRequestsSerializeCollectionAndCompleteUiCallbacks() => RunWithMessageLoop(async owner => {
		using var grid = CreateGrid(owner);
		var active = 0;
		var maximumActive = 0;
		var captures = 0;
		var callbacks = new List<int>();
		var controller = new CrudGridSnapshotController<Row>(grid, row => row.Id, async token => {
			active++;
			maximumActive = Math.Max(maximumActive, active);
			var id = ++captures;
			await Task.Delay(25, token);
			active--;
			return new[] { new Row { Id = id } };
		}, rows => callbacks.Add(rows.Single().Id));
		await Task.WhenAll(controller.RefreshAsync(), controller.RefreshAsync());
		Assert.That(maximumActive, Is.EqualTo(1));
		Assert.That(callbacks, Is.EqualTo(new[] { 1, 2 }));
		Assert.That(grid.VisibleEntities.Cast<Row>().Single().Id, Is.EqualTo(2));
	});

	private static CrudGrid CreateGrid(Form owner) {
		var grid = new CrudGrid { Name = "snapshotGrid", Dock = DockStyle.Fill, PersistSettings = false };
		grid.GridBindings = new[] {
			new CrudGridColumn<Row> { ColumnName = "ID", DataType = typeof(int), SortName = nameof(Row.Id), PropertyValue = row => row.Id },
			new CrudGridColumn<Row> { ColumnName = "Name", DataType = typeof(string), SortName = nameof(Row.Name), PropertyValue = row => row.Name }
		};
		owner.Controls.Add(grid);
		return grid;
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previousContext = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(800, 500) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The snapshot grid test did not complete within fifteen seconds.");
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try {
				await test(owner);
			} catch (Exception error) {
				failure = error;
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	public sealed class Row {
		public int Id { get; set; }
		public string Name { get; set; }
	}

	private sealed class EditableSnapshotSource : SnapshotDataSource<Row> {
		private int _updates;

		public int Updates => Volatile.Read(ref _updates);

		public override DataSourceCapabilities Capabilities => DataSourceCapabilities.Default;

		public override void UpdateRange(IEnumerable<Row> entities) => Interlocked.Add(ref _updates, entities.Count());

		public override Result ValidateRange(IEnumerable<(Row entity, CrudAction action)> actions) => Result.Success;
	}
}
