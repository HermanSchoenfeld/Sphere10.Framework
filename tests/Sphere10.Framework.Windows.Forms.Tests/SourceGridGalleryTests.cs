// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;
using SourceGrid = Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises actual raw grid surfaces, binding transactions, rendering and gallery commands.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class SourceGridGalleryTests {

	[Test]
	public void GalleryUsesRawEngineSurfacesWithRealBindingsAndControllerFeedback() {
		RunWithMessageLoop(async owner => {
			using var screen = new SourceGridGalleryScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var tabs = Find<TabControl>(screen, "sourceGridExamples");
			Assert.That(tabs.TabPages.Count, Is.EqualTo(7));
			Assert.That(screen.Controls.Find("sourceGridObjects", true).Single(), Is.TypeOf<SourceGrid.DataGrid>());
			Assert.That(screen.Controls.Find("sourceGridDataView", true).Single(), Is.TypeOf<SourceGrid.DataGrid>());
			var array = Find<SourceGrid.ArrayGrid>(screen, "sourceGridArray");
			Assert.That(array.DataSource.GetLength(0), Is.EqualTo(10000));
			Assert.That(new SourceGrid.CellContext(array, new SourceGrid.Position(10000, 5)).Value, Is.EqualTo(1000004));
			var planning = Find<SourceGrid.Planning.PlanningGrid>(screen, "sourceGridPlanning");
			Assert.That(planning.Appointments.Count, Is.EqualTo(2));
			Assert.That(planning.Grid.Rows.Count, Is.GreaterThan(20));
			var ping = Find<SourceGrid.Extensions.PingGrids.PingGrid>(screen, "sourceGridPing");
			Assert.That(ping.DataSource.GetItemValue(0, nameof(SourceGridGalleryRow.Name)), Is.EqualTo("Lumen pump"));
			ping.SortRangeRows(ping.CompleteRange, 0, true, null);
			Assert.That(new SourceGrid.CellContext(ping, new SourceGrid.Position(1, 0)).Value, Is.EqualTo("Atlas valve"));
			ping.SortRangeRows(ping.CompleteRange, 0, false, null);
			Assert.That(new SourceGrid.CellContext(ping, new SourceGrid.Position(1, 0)).Value, Is.EqualTo("Nova motor"));

			var grid = Find<SourceGrid.Grid>(screen, "sourceGridCells");
			Assert.That(grid[5, 1].ColumnSpan, Is.EqualTo(3));
			Assert.That(grid[5, 4].RowSpan, Is.EqualTo(2));
			Assert.That(grid.Selection.Focus(new SourceGrid.Position(2, 1), true), Is.True);
			var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(2, 1));
			context.Value = "Edited through the cell model";
			Assert.That(Find<TextBox>(screen, "sourceGridEvents").Text, Does.Contain("value changed"));
			Assert.That(grid[2, 1].Value, Is.EqualTo("Edited through the cell model"));

			var commands = Find<ToolStrip>(screen, "sourceGridCommands");
			commands.Items["selectAllSourceGrid"].PerformClick();
			Assert.That(grid.Selection.GetSelectionRegion().Contains(grid.CompleteRange), Is.True);
			commands.Items["clearSelectionSourceGrid"].PerformClick();
			Assert.That(grid.Selection.IsEmpty(), Is.True);
			commands.Items["resetSourceGrid"].PerformClick();
			Assert.That(grid[2, 1].Value, Is.EqualTo("Filter cartridge"));
		});
	}

	[Test]
	public void ResetAndDisposalReleaseOwnedEditorsAfterARealCellEdit() {
		RunWithMessageLoop(async owner => {
			using var screen = new SourceGridGalleryScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var grid = Find<SourceGrid.Grid>(screen, "sourceGridCells");
			var position = new SourceGrid.Position(2, 1);
			Assert.That(grid.Selection.Focus(position, true), Is.True);
			var context = new SourceGrid.CellContext(grid, position);
			var oldEditor = (SourceGrid.Cells.Editors.EditorControlBase)context.Cell.Editor;
			var unattachedEditor = (SourceGrid.Cells.Editors.EditorControlBase)grid[2, 2].Editor;
			context.StartEdit();
			await Task.Yield();
			Assert.That(context.IsEditing(), Is.True);
			Assert.That(grid.LinkedControls.GetByControl(oldEditor.Control), Is.Not.Null);

			screen.ResetExamples();
			Assert.That(oldEditor.Control.IsDisposed, Is.True);
			Assert.That(unattachedEditor.Control.IsDisposed, Is.True);
			Assert.That(grid.LinkedControls.GetByControl(oldEditor.Control), Is.Null);
			var replacement = new SourceGrid.CellContext(grid, position);
			var newEditor = (SourceGrid.Cells.Editors.EditorControlBase)replacement.Cell.Editor;
			Assert.That(newEditor, Is.Not.SameAs(oldEditor));
			Assert.That(grid.Selection.Focus(position, true), Is.True);
			replacement.StartEdit();
			await Task.Yield();
			Assert.That(replacement.IsEditing(), Is.True);
			screen.Dispose();
			Assert.That(newEditor.Control.IsDisposed, Is.True);
			Assert.That(newEditor.Grid, Is.Null);
		});
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	[TestCase(4)]
	[TestCase(5)]
	[TestCase(6)]
	public void EveryGalleryTabExportsRealCsvHtmlAndABoundedRenderedImage(int index) {
		RunWithMessageLoop(async owner => {
			using var screen = new SourceGridGalleryScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<TabControl>(screen, "sourceGridExamples").SelectedIndex = index;
			await Task.Yield();

			var csv = screen.ExportCsv();
			Assert.That(csv, Is.Not.Empty);
			var html = screen.ExportHtml();
			Assert.That(html, Does.StartWith("<html>").And.EndWith("</html>"));
			Assert.That(html, Does.Contain("<table").And.Contain("</table>"));
			Assert.That(Regex.Matches(html, "<tr>", RegexOptions.IgnoreCase).Count, Is.GreaterThan(0));
			using var preview = screen.CreatePreviewImage();
			Assert.That(preview.Width, Is.GreaterThan(30));
			Assert.That(preview.Height, Is.InRange(20, 4000));
			(SourceGrid.GridVirtual grid, SourceGrid.Position position) = index switch {
				0 => ((SourceGrid.GridVirtual)Find<SourceGrid.Grid>(screen, "sourceGridCells"), new SourceGrid.Position(2, 1)),
				1 => (Find<SourceGrid.DataGrid>(screen, "sourceGridObjects"), new SourceGrid.Position(1, 1)),
				2 => (Find<SourceGrid.DataGrid>(screen, "sourceGridDataView"), new SourceGrid.Position(1, 1)),
				3 => (Find<SourceGrid.ArrayGrid>(screen, "sourceGridArray"), new SourceGrid.Position(1, 1)),
				4 => (Find<SourceGrid.Planning.PlanningGrid>(screen, "sourceGridPlanning").Grid, new SourceGrid.Position(4, 2)),
				5 => (Find<SourceGrid.Extensions.PingGrids.PingGrid>(screen, "sourceGridPing"), new SourceGrid.Position(1, 0)),
				_ => (Find<SourceGrid.ListEditor>(screen, "sourceGridListEditor").Grid, new SourceGrid.Position(1, 0))
			};
			Assert.That(new SourceGrid.CellContext(grid, position).DisplayText, Is.Not.Empty);
			var content = MeasureCellContentContrast(preview, grid, position);
			Assert.That(content.Area.Width, Is.GreaterThan(4));
			Assert.That(content.Area.Height, Is.GreaterThan(4));
			Assert.That(content.Maximum - content.Minimum, Is.GreaterThan(64), "A text-bearing cell interior must contain visible glyphs; borders and themed header backgrounds are excluded.");
			if (index == 3) {
				Assert.That(csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries), Has.Length.EqualTo(10001));
				Assert.That(Regex.Matches(html, "<tr>", RegexOptions.IgnoreCase).Count, Is.EqualTo(10001));
				Assert.That(Find<Label>(screen, "sourceGridStatus").Text, Does.Contain(10001.ToString("N0")));
			}
		});
	}

	[TestCase("sourceGridObjects")]
	[TestCase("sourceGridDataView")]
	public void BoundExamplesCommitAndCancelThroughTheGridsActualEditingContract(string name) {
		RunWithMessageLoop(async owner => {
			using var screen = new SourceGridGalleryScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var grid = Find<SourceGrid.DataGrid>(screen, name);
			var quantity = grid.DataSource.GetItemProperties()[nameof(SourceGridGalleryRow.Quantity)];
			Assert.That(grid.BeginEditRow(1), Is.True);
			grid.DataSource.SetEditValue(quantity, 31);
			grid.EndEditingRow(false);
			Assert.That(grid.DataSource.GetItemValue(0, quantity), Is.EqualTo(31));

			Assert.That(grid.BeginEditRow(1), Is.True);
			grid.DataSource.SetEditValue(quantity, 999);
			grid.EndEditingRow(true);
			Assert.That(grid.DataSource.GetItemValue(0, quantity), Is.EqualTo(31));
			if (name == "sourceGridObjects")
				Assert.That(grid.GetCell(2, 2).View.BackColor, Is.EqualTo(Color.FromArgb(255, 235, 214)));

			var caption = grid.DataSource.GetItemProperties()[nameof(SourceGridGalleryRow.Name)];
			grid.SortRangeRows(grid.CompleteRange, 1, true, null);
			Assert.That(grid.DataSource.GetItemValue(0, caption), Is.EqualTo("Atlas valve"));
			grid.SortRangeRows(grid.CompleteRange, 1, false, null);
			Assert.That(grid.DataSource.GetItemValue(0, caption), Is.EqualTo("Nova motor"));
		});
	}

	[Test]
	public void ListCompositeUsesItsActualEditAndRowManagementControls() {
		RunWithMessageLoop(async owner => {
			using var screen = new SourceGridGalleryScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<TabControl>(screen, "sourceGridExamples").SelectedIndex = 6;
			await Task.Yield();
			var editor = Find<SourceGrid.ListEditor>(screen, "sourceGridListEditor");
			var grid = editor.Grid;
			var first = (SourceGridGalleryRow)editor.List[0];
			var context = new SourceGrid.CellContext(grid, new SourceGrid.Position(1, 0));
			context.Value = "Renamed pump";
			Assert.That(first.Name, Is.EqualTo("Renamed pump"));
			Assert.That(grid.Selection.Focus(context.Position, true), Is.True);
			Assert.That(Find<Button>(editor, "btUp").Enabled, Is.False);
			Assert.That(Find<Button>(editor, "btDown").Enabled, Is.True);
			Find<Button>(editor, "btDown").PerformClick();
			Assert.That(editor.List[1], Is.SameAs(first));
			Find<Button>(editor, "btUp").PerformClick();
			Assert.That(editor.List[0], Is.SameAs(first));

			Assert.That(grid.Selection.Focus(new SourceGrid.Position(grid.Rows.Count - 1, 0), true), Is.True);
			Assert.That(Find<Button>(editor, "btDown").Enabled, Is.False);
			Find<Button>(editor, "btAdd").PerformClick();
			Assert.That(editor.List.Count, Is.EqualTo(4));
			Assert.That(((SourceGridGalleryRow)editor.List[3]).Name, Is.EqualTo("New item"));
			Assert.That(Find<Button>(editor, "btDown").Enabled, Is.True);
			Assert.That(grid.Selection.Focus(new SourceGrid.Position(grid.Rows.Count - 1, 0), true), Is.True);
			Assert.That(Find<Button>(editor, "btDown").Enabled, Is.False);
			Find<Button>(editor, "btRemove").PerformClick();
			Assert.That(editor.List.Count, Is.EqualTo(3));
			Find<Button>(editor, "btrRefreshList").PerformClick();
			Assert.That(new SourceGrid.CellContext(grid, new SourceGrid.Position(1, 0)).Value, Is.EqualTo("Renamed pump"));
			Assert.That(Find<TextBox>(screen, "sourceGridEvents").Text, Does.Contain("List editor changed"));
		});
	}

	[TestCase(true)]
	[TestCase(false)]
	public void PingListReadsPropertiesAndHonorsTheRequestedSortDirection(bool ascending) {
		var source = new SourceGrid.Extensions.PingGrids.ListPingSource<SourceGridGalleryRow> {
			new() { Name = "Zulu", Quantity = 2 },
			new() { Name = "Alpha", Quantity = 9 },
			new() { Name = "Beta", Quantity = 4 }
		};
		Assert.That(source.GetItemValue(1, nameof(SourceGridGalleryRow.Quantity)), Is.EqualTo(9));
		source.ApplySort(nameof(SourceGridGalleryRow.Quantity), ascending);
		Assert.That(source.Select(row => row.Name), Is.EqualTo(ascending ? new[] { "Zulu", "Beta", "Alpha" } : new[] { "Alpha", "Beta", "Zulu" }));
		source.ApplySort(nameof(SourceGridGalleryRow.Name), true);
		Assert.That(source.Select(row => row.Name), Is.EqualTo(new[] { "Alpha", "Beta", "Zulu" }));
		source.AllowSort = false;
		source.ApplySort(nameof(SourceGridGalleryRow.Name), false);
		Assert.That(source.Select(row => row.Name), Is.EqualTo(new[] { "Alpha", "Beta", "Zulu" }));
	}

	[Test]
	public void EmptyPingSourceIsAWorkingEmptyProjection() {
		var source = new SourceGrid.Extensions.PingGrids.EmptyPingSource();
		Assert.That(source.Count, Is.Zero);
		Assert.That(source.AllowSort, Is.False);
		Assert.That(() => source.ApplySort("Name", true), Throws.Nothing);
		Assert.That(() => source.GetItemValue(0, "Name"), Throws.TypeOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void PngSaveUsesTheGallerysSameOwnedPreview() {
		RunWithMessageLoop(async owner => {
			using var screen = new SourceGridGalleryScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var path = Path.Combine(Path.GetTempPath(), "sphere10-sourcegrid-" + Guid.NewGuid().ToString("N") + ".png");
			using var cleanup = Tools.Scope.ExecuteOnDispose(() => File.Delete(path));
			await screen.SavePreviewAsync(path);
			using var saved = Image.FromFile(path);
			Assert.That(saved.Width, Is.GreaterThan(30));
			Assert.That(saved.Height, Is.GreaterThan(30));
		});
	}

	// Inspect the same concrete controls shown on the gallery's design surface.
	private static T Find<T>(Control parent, string name)
		where T : Control {
		return (T)parent.Controls.Find(name, true).Single();
	}

	// Inspect every interior pixel of a known text cell, independently of theme colors or glyph placement.
	private static (Rectangle Area, int Minimum, int Maximum) MeasureCellContentContrast(Bitmap preview, SourceGrid.GridVirtual grid, SourceGrid.Position position) {
		var range = grid.PositionToCellRange(position);
		var x = Enumerable.Range(0, range.Start.Column).Sum(grid.Columns.GetWidth);
		var y = Enumerable.Range(0, range.Start.Row).Sum(grid.Rows.GetHeight);
		var bounds = new Rectangle(new Point(x, y), grid.RangeToSize(range));
		var view = grid.GetCell(position).View;
		var interior = view.Border == null ? bounds : view.Border.GetContentRectangle(bounds);
		var area = Rectangle.FromLTRB((int)Math.Ceiling(interior.Left) + 1, (int)Math.Ceiling(interior.Top) + 1,
			(int)Math.Floor(interior.Right) - 1, (int)Math.Floor(interior.Bottom) - 1);
		area.Intersect(new Rectangle(Point.Empty, preview.Size));

		var minimum = 255;
		var maximum = 0;
		for (var row = area.Top; row < area.Bottom; row++) {
			for (var column = area.Left; column < area.Right; column++) {
				var pixel = preview.GetPixel(column, row);
				var luminance = (299 * pixel.R + 587 * pixel.G + 114 * pixel.B) / 1000;
				minimum = Math.Min(minimum, luminance);
				maximum = Math.Max(maximum, luminance);
			}
		}
		return (area, minimum, maximum);
	}

	// Await every native operation inside one owning Windows Forms dispatcher.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previousContext = SynchronizationContext.Current;
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		using var context = new WindowsFormsSynchronizationContext();
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1250, 850) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The raw SourceGrid gallery did not complete.");
			System.Windows.Forms.Application.ExitThread();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
				completed = true;
			} catch (Exception error) {
				failure = error;
			} finally {
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}
}
