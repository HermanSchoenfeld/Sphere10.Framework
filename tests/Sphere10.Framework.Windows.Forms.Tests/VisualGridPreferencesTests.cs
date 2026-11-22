// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using FormsApplication = System.Windows.Forms.Application;
using EditorControlBase = Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors.EditorControlBase;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Verifies screen-supplied grid values and deliberate changes without control-owned persistence.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class VisualGridPreferencesTests {
	private readonly record struct QueryRequest(int Length, int Page, string Sort, SortDirection Direction);

	private ISettingsProvider _previousProvider;
	private ProbeProvider _provider;

	[SetUp]
	public void SetUp() {
		_previousProvider = (ISettingsProvider)typeof(UserSettings).GetField("_provider", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		var directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "grid-preferences-" + Guid.NewGuid().ToString("N"));
		_provider = new ProbeProvider(directory);
		UserSettings.Provider = new CachedSettingsProvider(_provider);
	}

	[TearDown]
	public void TearDown() => UserSettings.Provider = _previousProvider;

	[Test]
	public void ExistingGridSettingsValidatePagingSortingAndColumnValues() => Assert.That(Preferences(20, width: 145).Validate().IsSuccess, Is.True);

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	[TestCase(4)]
	[TestCase(5)]
	[TestCase(6)]
	[TestCase(7)]
	[TestCase(8)]
	[TestCase(9)]
	[TestCase(10)]
	[TestCase(11)]
	public void ExistingGridSettingsRejectInvalidOptionsValues(int invalidValue) {
		var settings = Preferences(20, width: 145);
		switch (invalidValue) {
			case 0:
				settings.PageSize = 0;
				break;
			case 1:
				settings.PageNumber = -1;
				break;
			case 2:
				settings.PageMode = (VisualGridPageMode)99;
				break;
			case 3:
				settings.SortDirection = (SortDirection)99;
				break;
			case 4:
				settings.Columns = null;
				break;
			case 5:
				settings.Columns = new ApplicationGridColumnPreferences[] { null };
				break;
			case 6:
				settings.Columns[0].Key = string.Empty;
				break;
			case 7:
				settings.Columns = new[] { settings.Columns[0], settings.Columns[0] };
				break;
			case 8:
				settings.Columns[0].Width = 0;
				break;
			case 9:
				settings.Columns[0].Width = double.NaN;
				break;
			case 10:
				settings.Columns[0].Width = double.PositiveInfinity;
				break;
			case 11:
				settings.Columns[0].Width = 100001;
				break;
		}
		Assert.That(settings.Validate().IsSuccess, Is.False);
	}

	[Test]
	public void OwnedValuesRestoreBeforeTheFirstQueryAndOnlyCompletedActionsNotify() {
		RunWithMessageLoop(async owner => {
			var order = new List<string>();
			using var crud = CreateGrid(owner, "Owned", grid: new HookGrid(order));
			crud.Preferences = Preferences(pageSize: 2, page: 1, width: 180);
			var changes = new List<VisualGridSettings>();
			crud.PreferencesChanged += values => {
				order.Add("event");
				changes.Add(values);
			};
			var source = new ProbeSource();
			await crud.SetDataSource(source);
			await crud.RefreshGrid();
			Assert.That(source.Requests.First(), Is.EqualTo(new QueryRequest(2, 1, "Name", SortDirection.Descending)));
			Assert.That(changes, Is.Empty, "Restore and binding are not accepted user edits.");
			Assert.That(_provider.ReadCount, Is.Zero);
			Assert.That(_provider.SaveCount, Is.Zero);

			var grid = Find<Grid>(crud, "_grid");
			grid.Columns[0].Width = 193;
			Assert.That(changes, Is.Empty, "Pixel geometry changes must not publish or serialize preferences.");
			CompleteResize(grid);
			Assert.That(changes, Has.Count.EqualTo(1));
			Assert.That(order, Is.EqualTo(new[] { "hook", "event" }));
			Assert.That(changes[0].Columns.Single(column => column.Key == "name").Width, Is.EqualTo(193 * 96D / crud.DeviceDpi));
			changes[0].Columns[0].Width = 1;
			Assert.That(crud.CaptureGridPreferences().Columns.Single(column => column.Key == "name").Width, Is.EqualTo(193 * 96D / crud.DeviceDpi));
			Assert.That(_provider.SaveCount, Is.Zero, "The screen owner is the only persistence authority.");
		});
	}

	[Test]
	public void ManualWidthsRoundTripByStableKeyWhileOtherColumnsKeepStretching() {
		RunWithMessageLoop(async owner => {
			VisualGridSettings captured;
			double logical;
			using (var first = CreateGrid(owner, "Widths")) {
				first.Preferences = Preferences(20);
				await first.SetDataSource(new ProbeSource());
				await first.RefreshGrid();
				var grid = Find<Grid>(first, "_grid");
				grid.Columns[0].Width = 193;
				CompleteResize(grid);
				captured = first.CaptureGridPreferences();
				logical = captured.Columns.Single(column => column.Key == "name").Width.Value;

				// Automatic viewport stretching does not become a deliberate width or a stored record.
				var other = grid.Columns[1].Width;
				owner.Width += 300;
				await first.RefreshGrid();
				Assert.That(grid.Columns[0].Width, Is.EqualTo(193));
				Assert.That(grid.Columns[1].Width, Is.GreaterThan(other));
				Assert.That(first.CaptureGridPreferences().Columns.Single(column => column.Key == "group").Width, Is.Null);
				Assert.That(_provider.SaveCount, Is.Zero);
			}

			// The screen owns the captured value and supplies it to the next grid instance.
			using var restored = CreateGrid(owner, "Restored widths", "Renamed caption");
			restored.Preferences = captured;
			await restored.SetDataSource(new ProbeSource());
			await restored.RefreshGrid();
			Assert.That(Find<Grid>(restored, "_grid").Columns[0].Width, Is.EqualTo((int)Math.Round(logical * restored.DeviceDpi / 96D)));
			Assert.That(restored.CaptureGridPreferences().Columns.Single(column => column.Key == "name").Title, Is.EqualTo("Renamed caption"));

			var reset = restored.CaptureGridPreferences();
			foreach (var column in reset.Columns)
				column.Width = null;
			restored.ApplyGridPreferences(reset);
			await restored.RefreshGrid();
			Assert.That(restored.CaptureGridPreferences().Columns.All(column => column.Width == null), Is.True);
			Assert.That(_provider.ReadCount, Is.Zero);
			Assert.That(_provider.SaveCount, Is.Zero);
		});
	}

	[Test]
	public void StretchOnlyValuesApplyAndRoundTripByStableColumnKey() {
		RunWithMessageLoop(async owner => {
			VisualGridSettings captured;
			using (var first = CreateGrid(owner, "Stretch")) {
				first.Preferences = Preferences(20);
				await first.SetDataSource(new ProbeSource());
				await first.RefreshGrid();
				var values = first.CaptureGridPreferences();
				values.Columns.Single(column => column.Key == "name").ExpandsToFit = false;
				first.ApplyGridPreferences(values);
				await first.RefreshGrid();
				captured = first.CaptureGridPreferences();
			}

			using var restored = CreateGrid(owner, "Restored stretch", "Renamed caption");
			restored.Preferences = captured;
			await restored.SetDataSource(new ProbeSource());
			await restored.RefreshGrid();
			var columns = restored.CaptureGridPreferences().Columns;
			Assert.That(columns.Single(column => column.Key == "name").ExpandsToFit, Is.False);
			Assert.That(columns.Single(column => column.Key == "group").ExpandsToFit, Is.True);
			Assert.That(columns.All(column => column.Width == null), Is.True);
			var native = Find<Grid>(restored, "_grid");
			Assert.That(native.Columns[0].AutoSizeMode.HasFlag(SourceGrid.AutoSizeMode.EnableStretch), Is.False);
			Assert.That(native.Columns[1].AutoSizeMode.HasFlag(SourceGrid.AutoSizeMode.EnableStretch), Is.True);
			Assert.That(native.Columns[1].Width, Is.GreaterThan(native.Columns[0].Width));

			var reset = restored.CaptureGridPreferences();
			reset.Columns.Single(column => column.Key == "name").ExpandsToFit = null;
			restored.ApplyGridPreferences(reset);
			await restored.RefreshGrid();
			Assert.That(native.Columns[0].AutoSizeMode.HasFlag(SourceGrid.AutoSizeMode.EnableStretch), Is.True, "Clearing the override restores the binding default.");
			Assert.That(_provider.ReadCount, Is.Zero);
			Assert.That(_provider.SaveCount, Is.Zero);
		});
	}

	[Test]
	public void HiddenColumnsLeaveTheNativeGridAndRoundTripByStableColumnKey() {
		RunWithMessageLoop(async owner => {
			VisualGridSettings captured;
			using (var first = CreateGrid(owner, "Hidden")) {
				((VisualGridColumn<Row>)first.GridBindings[1]).Hidden = true;
				first.GridBindings = first.GridBindings;
				await first.SetDataSource(new ProbeSource());
				await first.RefreshGrid();
				var native = Find<Grid>(first, "_grid");
				Assert.That(native.ColumnsCount, Is.EqualTo(1), "A column hidden by its schema is not created.");
				Assert.That(first.VisibleGridBindings.Select(column => column.PreferenceKey), Is.EqualTo(new[] { "name" }));
				Assert.That(first.GridBindings.Select(column => column.PreferenceKey), Is.EqualTo(new[] { "name", "group" }), "The declared schema keeps hidden columns.");

				// An accepted preference overrides the schema in both directions.
				var values = first.CaptureGridPreferences();
				Assert.That(values.Columns.Select(column => column.Key), Is.EqualTo(new[] { "name", "group" }));
				Assert.That(values.Columns.Select(column => column.Hidden), Is.EqualTo(new bool?[] { false, true }));
				values.Columns.Single(column => column.Key == "name").Hidden = true;
				values.Columns.Single(column => column.Key == "group").Hidden = false;
				first.ApplyGridPreferences(values);
				await first.RefreshGrid();
				Assert.That(native.ColumnsCount, Is.EqualTo(1));
				Assert.That(native[0, 0].Value, Is.EqualTo("Group"));
				Assert.That(native[1, 0].Value, Is.EqualTo("Group"));
				captured = first.CaptureGridPreferences();
			}

			// The screen-owned value carries visibility to the next grid instance.
			using var restored = CreateGrid(owner, "Restored hidden");
			restored.Preferences = captured;
			await restored.SetDataSource(new ProbeSource());
			await restored.RefreshGrid();
			var grid = Find<Grid>(restored, "_grid");
			Assert.That(restored.VisibleGridBindings.Select(column => column.PreferenceKey), Is.EqualTo(new[] { "group" }));
			Assert.That(grid.ColumnsCount, Is.EqualTo(1));

			// A grid that mirrors another grid's displayed columns shows every binding it declares.
			restored.ShowHiddenColumns = true;
			await restored.RefreshGrid();
			Assert.That(grid.ColumnsCount, Is.EqualTo(2));
			restored.ShowHiddenColumns = false;
			await restored.RefreshGrid();
			Assert.That(grid.ColumnsCount, Is.EqualTo(1));

			// Restoring defaults releases the visibility choice; this grid's schema declares both columns visible.
			var reset = restored.CaptureGridPreferences();
			reset.RestoreDefaultValues();
			Assert.That(reset.Columns.All(column => column.Hidden == null), Is.True);
			restored.ApplyGridPreferences(reset);
			await restored.RefreshGrid();
			Assert.That(grid.ColumnsCount, Is.EqualTo(2));
			Assert.That(_provider.ReadCount, Is.Zero);
			Assert.That(_provider.SaveCount, Is.Zero);
		});
	}

	[Test]
	public void ScreenColumnOrderAppliesToHeadersRowsAndCapturedValues() {
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "Ordered columns");
			var declared = crud.GridBindings;
			var values = Preferences(2, width: 171);
			values.Columns = values.Columns.Reverse().ToArray();
			crud.Preferences = values;
			await crud.SetDataSource(new ProbeSource());
			await crud.RefreshGrid();

			// Moving a column must move its value cells and retain the width associated with its stable key.
			var native = Find<Grid>(crud, "_grid");
			Assert.That(crud.GridBindings.Select(column => column.PreferenceKey), Is.EqualTo(new[] { "group", "name" }));
			Assert.That(native[0, 0].Value, Is.EqualTo("Group"));
			Assert.That(native[0, 1].Value, Is.EqualTo("Name"));
			Assert.That(native[1, 0].Value, Is.EqualTo("Group"));
			Assert.That(native[1, 1].Value, Is.EqualTo("Name 19"));
			Assert.That(native.Columns[1].Width, Is.EqualTo((int)Math.Round(171 * crud.DeviceDpi / 96D)));
			Assert.That(crud.CaptureGridPreferences().Columns.Select(column => column.Key), Is.EqualTo(new[] { "group", "name" }));
			Assert.That(declared.Select(column => column.PreferenceKey), Is.EqualTo(new[] { "name", "group" }), "Applying display order must not reorder the caller's binding array.");
		});
	}

	[Test]
	public void ScreenColumnOrderAndWidthsSurviveRebinding() {
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "Rebound columns");
			var declared = crud.GridBindings.ToArray();
			var values = Preferences(2, width: 171);
			values.Columns = values.Columns.Reverse().ToArray();
			crud.Preferences = values;
			await crud.SetDataSource(new ProbeSource());
			await crud.RefreshGrid();

			// A refreshed declaration must not erase an accepted screen column order.
			crud.GridBindings = declared;
			await crud.RefreshGrid();
			var native = Find<Grid>(crud, "_grid");
			Assert.That(crud.GridBindings.Select(column => column.PreferenceKey), Is.EqualTo(new[] { "group", "name" }));
			Assert.That(native[0, 0].Value, Is.EqualTo("Group"));
			Assert.That(native[1, 1].Value, Is.EqualTo("Name 19"));
			Assert.That(native.Columns[1].Width, Is.EqualTo((int)Math.Round(171 * crud.DeviceDpi / 96D)));
			Assert.That(crud.CaptureGridPreferences().Columns.Single(column => column.Key == "name").Width, Is.EqualTo(171));
		});
	}

	[Test]
	public void PageModeAndManualSizeChangesNotifyTheScreenOwner() {
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "Paging choices");
			crud.Preferences = Preferences(2);
			await crud.SetDataSource(new ProbeSource());
			await crud.RefreshGrid();
			var changes = new List<VisualGridSettings>();
			crud.PreferencesChanged += changes.Add;

			crud.PageMode = VisualGridPageMode.Single;
			await crud.RefreshGrid();
			Assert.That(changes.Last().PageMode, Is.EqualTo(VisualGridPageMode.Single));
			Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(20));
			crud.PageMode = VisualGridPageMode.Paged;
			await crud.RefreshGrid();
			Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 3;
			await crud.RefreshGrid();
			Assert.That(changes.Last().PageMode, Is.EqualTo(VisualGridPageMode.Paged));
			Assert.That(changes.Last().PageSize, Is.EqualTo(3));
			Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(3));
			Assert.That(_provider.ReadCount, Is.Zero);
			Assert.That(_provider.SaveCount, Is.Zero, "Operational controls report values; only the screen owner saves them.");
		});
	}

	[Test]
	public void ScreenOwnedComputedColumnsRequireStableKeys() {
		RunWithMessageLoop(owner => {
			using var crud = CreateGrid(owner, "Computed");
			var column = new VisualGridColumn<Row> { ColumnName = "Projection", PropertyValue = row => row.Name };
			crud.GridBindings = new[] { column };
			Assert.That(() => crud.Preferences = Preferences(2), Throws.ArgumentException);
			column.PreferenceKey = "projection";
			crud.Preferences = Preferences(2);
			Assert.That(crud.CaptureGridPreferences().Columns.Any(value => value.Key == "projection"), Is.True);
			Assert.That(_provider.ReadCount, Is.Zero);
			Assert.That(_provider.SaveCount, Is.Zero);
			return Task.CompletedTask;
		});
	}

	[Test]
	public void ApplyingPreferencesPreservesTheLiveCellDraftAndDefersQueryAndColumnOrder() {
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "Draft");
			crud.Preferences = Preferences(pageSize: 2);
			crud.AllowCellEditing = true;
			var source = new ProbeSource();
			await crud.SetDataSource(source);
			await crud.RefreshGrid();
			var grid = Find<Grid>(crud, "_grid");
			var context = new CellContext(grid, new Position(1, 0));
			crud.SelectedEntity = crud.VisibleEntities.First();
			typeof(GridVirtual).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(grid, new object[] { new KeyEventArgs(Keys.F2) });
			Assert.That(context.IsEditing(), Is.True);
			var editor = ((EditorControlBase)context.Cell.Editor).Control;
			editor.Text = "Unapplied draft";

			// Options changes cannot replace the active column or trigger a read before the draft ends.
			var reads = source.Requests.Length;
			var values = Preferences(pageSize: 4, width: 185);
			values.Columns = values.Columns.Reverse().ToArray();
			crud.ApplyGridPreferences(values);
			await crud.RefreshGrid();
			Assert.That(context.IsEditing(), Is.True);
			Assert.That(editor.Text, Is.EqualTo("Unapplied draft"));
			Assert.That(source.Requests, Has.Length.EqualTo(reads));
			Assert.That(crud.GridBindings.Select(column => column.PreferenceKey), Is.EqualTo(new[] { "name", "group" }));
			Assert.That(grid[0, 0].Value, Is.EqualTo("Name"));

			context.EndEdit(true);
			await crud.RefreshGrid();
			Assert.That(source.Requests.Last().Length, Is.EqualTo(4));
			Assert.That(crud.GridBindings.Select(column => column.PreferenceKey), Is.EqualTo(new[] { "group", "name" }));
			Assert.That(grid[0, 0].Value, Is.EqualTo("Group"));
			Assert.That(grid[1, 1].Value, Is.EqualTo("Name 19"));
			Assert.That(source.Rows.All(row => row.Name != "Unapplied draft"), Is.True);
		});
	}

	[Test]
	public void DraggingAHeaderBoundaryResizesTheColumn([Values] bool stretchingColumns, [Values] bool rowsOverflow) {
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "Drag", stretching: stretchingColumns);
			// Rows taller than the grid add a vertical scrollbar: the 21 November 2026 ARP table of 25 entries could not be resized.
			if (rowsOverflow)
				crud.PageMode = VisualGridPageMode.Single;
			await crud.SetDataSource(new ProbeSource(rowsOverflow ? 200 : 20));
			await crud.RefreshGrid();
			var grid = Find<Grid>(crud, "_grid");
			var before = grid.Columns[0].Width;
			var header = grid.PositionToRectangle(new Position(0, 0));
			var start = new Point(header.Right - 1, header.Top + header.Height / 2);
			DragMouse(grid, start, new Point(start.X + 40, start.Y));
			await Task.Yield();
			Assert.That(grid.Columns[0].Width, Is.EqualTo(before + 40).Within(2), "The first column follows the dragged boundary.");
			await crud.RefreshGrid();
			Assert.That(grid.Columns[0].Width, Is.EqualTo(before + 40).Within(2), "A refresh keeps the width the user dragged.");

			// The line is grabbed from its right side too, inside the next header, where people usually aim; the release does not sort.
			var sorted = crud.CaptureGridPreferences().SortDirection;
			before = grid.Columns[0].Width;
			header = grid.PositionToRectangle(new Position(0, 1));
			start = new Point(header.Left + 2, header.Top + header.Height / 2);
			DragMouse(grid, start, new Point(start.X + 25, start.Y));
			await Task.Yield();
			Assert.That(grid.Columns[0].Width, Is.EqualTo(before + 25).Within(2), "Grabbing just right of the line resizes the column to its left.");
			Assert.That(crud.CaptureGridPreferences().SortDirection, Is.EqualTo(sorted), "Finishing a resize is not a click that sorts.");
		});
	}

	[Test]
	public void DraggingAStretchedColumnWiderWhenTheRowsJustFitKeepsTheNewWidth() {
		// 21 November 2026: an ARP table whose rows just filled the grid could not be widened. The wider column brought a horizontal scrollbar,
		// that one a vertical scrollbar, and refitting the narrower viewport put the dragged column back before the width was the user's.
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "JustFits");
			crud.PageMode = VisualGridPageMode.Single;
			await crud.SetDataSource(new ProbeSource(20));
			await crud.RefreshGrid();
			var grid = Find<Grid>(crud, "_grid");
			for (var attempt = 0; attempt < 5; attempt++) {
				var spare = grid.DisplayRectangle.Height - grid.Rows.GetBottom(grid.RowsCount - 1);
				if (spare == 3)
					break;
				owner.ClientSize = new Size(owner.ClientSize.Width, owner.ClientSize.Height - spare + 3);
				owner.PerformLayout();
				await Task.Delay(50);
			}
			Assert.That(grid.DisplayRectangle.Height - grid.Rows.GetBottom(grid.RowsCount - 1), Is.InRange(1, SystemInformation.HorizontalScrollBarHeight - 1),
				"The rows must just fit, so a horizontal scrollbar brings a vertical one.");
			var before = grid.Columns[0].Width;
			var header = grid.PositionToRectangle(new Position(0, 0));
			var start = new Point(header.Right - 1, header.Top + header.Height / 2);
			DragMouse(grid, start, new Point(start.X + 30, start.Y));
			await Task.Yield();
			Assert.That(grid.Columns[0].Width, Is.GreaterThanOrEqualTo(before + 28), "The dragged column keeps the width it was dragged to.");
		});
	}

	[Test]
	public void NaturalSizingGivesAColumnAtLeastItsMinimumWidth() {
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "Minimum", stretching: false);
			((VisualGridColumn<Row>)crud.GridBindings[1]).MinimumWidth = 260;
			crud.GridBindings = crud.GridBindings.ToArray();
			await crud.SetDataSource(new ProbeSource());
			await crud.RefreshGrid();
			var grid = Find<Grid>(crud, "_grid");
			Assert.That(grid.Columns[1].Width, Is.GreaterThanOrEqualTo(crud.LogicalToDeviceUnits(260)), "The short Group values would size the column far narrower.");
			Assert.That(grid.Columns[0].Width, Is.LessThan(crud.LogicalToDeviceUnits(260)), "A column without a minimum keeps its natural width.");
		});
	}

	[Test]
	public void NaturalSizingGivesAColumnAtMostItsMaximumWidth() {
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "Maximum", stretching: false);
			((VisualGridColumn<Row>)crud.GridBindings[0]).MaximumWidth = 30;
			crud.GridBindings = crud.GridBindings.ToArray();
			await crud.SetDataSource(new ProbeSource());
			await crud.RefreshGrid();
			var grid = Find<Grid>(crud, "_grid");
			Assert.That(grid.Columns[0].Width, Is.EqualTo(crud.LogicalToDeviceUnits(30)), "The Name values would size the column wider.");
			Assert.That(grid.Columns[1].Width, Is.GreaterThan(crud.LogicalToDeviceUnits(30)), "A column without a maximum keeps its natural width.");
		});
	}

	// Supply the ordinary value a screen owns, independently of any control or provider.
	private static VisualGridSettings Preferences(int pageSize, int page = 0, double? width = null) {
		return new VisualGridSettings {
			PageMode = VisualGridPageMode.Paged,
			PageSize = pageSize,
			PageNumber = page,
			SortColumnName = nameof(Row.Name),
			SortDirection = SortDirection.Descending,
			Columns = new[] {
				new ApplicationGridColumnPreferences { Key = "name", Title = "Name", Width = width },
				new ApplicationGridColumnPreferences { Key = "group", Title = "Group" }
			}
		};
	}

	// Create a named native surface with stable declaration keys and no persistence responsibility.
	private static VisualGrid CreateGrid(Form owner, string name, string caption = "Name", VisualGrid grid = null, bool stretching = true) {
		var crud = grid ?? new VisualGrid();
		crud.Name = name;
		crud.Dock = DockStyle.Fill;
		owner.Controls.Add(crud);
		crud.GridBindings = new[] {
			new VisualGridColumn<Row> {
				PreferenceKey = "name",
				ColumnName = caption,
				PropertyName = nameof(Row.Name),
				SortName = nameof(Row.Name),
				DataType = typeof(string),
				PropertyValue = row => row.Name,
				SetPropertyValue = (row, value) => row.Name = (string)value,
				CanEditCell = true,
				ExpandsToFit = stretching
			},
			new VisualGridColumn<Row> {
				PreferenceKey = "group",
				ColumnName = "Group",
				PropertyName = nameof(Row.Group),
				SortName = nameof(Row.Group),
				DataType = typeof(string),
				PropertyValue = row => row.Group,
				CanEditCell = false,
				ExpandsToFit = stretching
			}
		};
		return crud;
	}

	// Delivers a left-button drag to the native window under the start point, as the mouse would, without moving the cursor.
	private static void DragMouse(Grid grid, Point start, Point end) {
		var target = (Control)grid;
		for (var child = target.GetChildAtPoint(target.PointToClient(grid.PointToScreen(start))); child != null; child = target.GetChildAtPoint(target.PointToClient(grid.PointToScreen(start))))
			target = child;
		var wndProc = typeof(Control).GetMethod("WndProc", BindingFlags.Instance | BindingFlags.NonPublic);
		void Send(int message, int keys, Point point) {
			var local = target.PointToClient(grid.PointToScreen(point));
			var m = Message.Create(target.Handle, message, keys, (local.Y << 16) | (local.X & 0xFFFF));
			wndProc.Invoke(target, new object[] { m });
		}
		Send(0x0200, 0, start);
		Send(0x0201, 1, start);
		for (var step = 1; step <= 6; step++)
			Send(0x0200, 1, new Point(start.X + (end.X - start.X) * step / 6, start.Y + (end.Y - start.Y) * step / 6));
		Send(0x0202, 0, end);
	}

	// Resolve a designer-owned control through its public control tree.
	private static T Find<T>(Control control, string name)
		where T : Control {
		return (T)control.Controls.Find(name, true).Single();
	}

	// Finish the native header interaction that distinguishes a user resize from layout.
	private static void CompleteResize(Grid grid) {
		typeof(Control).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic)
			.Invoke(grid, new object[] { new MouseEventArgs(MouseButtons.Left, 1, -1, -1, 0) });
	}

	// Keep asynchronous native assertions on the same live Forms dispatcher with bounded completion.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(850, 450) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The grid preference test timed out.");
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	#region Inner Types

	// Return predictable rows while recording the public paging contract requested by the grid.
	private sealed class ProbeSource : ListDataSource<Row> {
		private readonly List<QueryRequest> _requests = new();

		public ProbeSource(int count = 20)
			: base(new ExtendedList<Row>()) {
			Rows = Enumerable.Range(0, count).Select(index => new Row { Name = "Name " + index.ToString("D2"), Group = "Group" }).ToArray();
		}

		public Row[] Rows { get; }

		public QueryRequest[] Requests => _requests.ToArray();

		public override Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			_requests.Add(new QueryRequest(pageLength, page, sortProperty, sortDirection));
			var rows = sortDirection == SortDirection.Descending ? Rows.Reverse().ToArray() : Rows;
			page = Math.Min(page, (rows.Length - 1) / pageLength);
			return Task.FromResult(new DataSourceItems<Row> { Items = rows.Skip(page * pageLength).Take(pageLength).ToArray(), Page = page, TotalCount = rows.Length });
		}
	}

	// Supply editable text and a second value with an independent stable column identity.
	private sealed class Row {
		public string Name { get; set; }

		public string Group { get; set; }
	}

	// Observe the public template hook before subscribers receive the same accepted capture.
	private sealed class HookGrid(List<string> order) : VisualGrid {
		protected override void OnPreferencesChanged(VisualGridSettings preferences) {
			order.Add("hook");
			base.OnPreferencesChanged(preferences);
		}
	}

	// Detect accidental control-owned provider reads or writes; the screen owns those operations.
	private sealed class ProbeProvider : DirectoryFileSettingsProvider {
		public ProbeProvider(string directory)
			: base(directory) {
		}

		public int ReadCount { get; private set; }

		public int SaveCount { get; private set; }

		public override bool ContainsSetting(Type type, object id = null) {
			ReadCount++;
			return base.ContainsSetting(type, id);
		}

		public override SettingsObject LoadSetting(Type type, object id = null) {
			ReadCount++;
			return base.LoadSetting(type, id);
		}

		public override void SaveSetting(SettingsObject settings) {
			SaveCount++;
			base.SaveSetting(settings);
		}
	}

	#endregion
}
