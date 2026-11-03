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
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.ComponentModel;
using Sphere10.Framework.Data;
using Sphere10.Framework.Windows.Forms;
using SourceGrid = Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates the raw cell, bound, virtual and planning SourceGrid families.</summary>
public partial class SourceGridGalleryScreen : WinFormsApplicationScreen {
	private const int _previewRows = 40;
	private readonly Disposables _sampleEditors = new();
	private bool _initialized;
	private BoundDataView _dataBinding;
	private DataTable _dataTable;

	/// <summary>Creates the designer-owned gallery; sample data loads on first presentation.</summary>
	public SourceGridGalleryScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		Disposables.Add(_eventHeading.Font);
		Disposables.Add(_eventLog.Font);
		Title = "SourceGrid Gallery";
	}

	// The design-time tabs own their concrete engine surfaces.
	private SourceGrid.GridVirtual SelectedGrid {
		get {
			return _examples.SelectedIndex switch {
				1 => _listGrid,
				2 => _viewGrid,
				3 => _arrayGrid,
				4 => _planningGrid.Grid,
				5 => _pingGrid,
				6 => _listEditor.Grid,
				_ => _cellGrid
			};
		}
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override SourceGridGalleryScreenSettings ScreenSettings {
		get {
			return (SourceGridGalleryScreenSettings)base.ScreenSettings;
		}
	}

	/// <summary>Initializes grid controllers and data only outside the Visual Studio designer.</summary>
	protected override void InitializeUIPrimingData() {
		base.InitializeUIPrimingData();
		if (_initialized || Tools.Runtime.IsDesignMode)
			return;

		ObserveGrid(_cellGrid);
		ObserveGrid(_listGrid);
		ObserveGrid(_viewGrid);
		ObserveGrid(_arrayGrid);
		ObserveGrid(_planningGrid.Grid);
		ObserveGrid(_pingGrid);
		ObserveGrid(_listEditor.Grid);
		ResetExamples();
		ApplyPreferences();
	}

	// Accepted Options selects the same existing example without replacing edited demonstration values.
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		ApplyPreferences();
	}

	/// <summary>Releases the data-view adapter before its borrowed table leaves the screen.</summary>
	protected override void OnDestroyScreen() {
		CancelEditing();
		ReleaseEditors();
		ReleaseDataView();
		base.OnDestroyScreen();
	}

	// Observe the real engine controller callbacks without introducing another controller hierarchy.
	private void ObserveGrid(SourceGrid.GridVirtual grid) {
		var events = new SourceGrid.Cells.Controllers.CustomEvents();
		events.FocusEntered += CellFocused;
		events.ValueChanged += CellValueChanged;
		events.EditStarted += CellEditStarted;
		grid.Controller.AddController(events);
		grid.Selection.SelectionChanged += (_, _) => UpdateStatus();
	}

	// Native command handlers retain their actual asynchronous modal owner.
	private async void CsvClicked(object sender, EventArgs args) {
		try {
			await ReviewTextDialog.ShowAsync(this, "SourceGrid CSV export", "Review the active grid's complete CSV output, then copy or save it.", ExportCsv(), "source-grid.csv", "CSV files (*.csv)|*.csv");
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	/// <summary>Exports the active example through SourceGrid's CSV exporter.</summary>
	public string ExportCsv() {
		var grid = PrepareExport();
		using var output = new StringWriter();
		new SourceGrid.Exporter.CSV { FieldSeparator = "," }.Export(grid, output);
		return output.ToString();
	}

	// HTML output demonstrates the engine's cell colors, borders and text formatting.
	private async void HtmlClicked(object sender, EventArgs args) {
		try {
			await ReviewTextDialog.ShowAsync(this, "SourceGrid HTML export", "Review or save the formatted HTML table. PNG export includes cell images.", ExportHtml(), "source-grid.html", "HTML files (*.html)|*.html");
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	/// <summary>Exports the active example's text and cell formatting as a standalone HTML table.</summary>
	public string ExportHtml() {
		var grid = PrepareExport();
		using var output = new MemoryStream();
		var exporter = new SourceGrid.Exporter.HTML(SourceGrid.Exporter.ExportHTMLMode.Default & ~SourceGrid.Exporter.ExportHTMLMode.CellImages, string.Empty, string.Empty, output);
		exporter.Export(grid);
		return Encoding.UTF8.GetString(output.ToArray()).TrimStart('\uFEFF');
	}

	// Save an image only to the path chosen in the native file picker.
	private async void PngClicked(object sender, EventArgs args) {
		try {
			using var picker = new SaveFileDialog { Filter = "PNG images (*.png)|*.png", DefaultExt = "png", FileName = "source-grid.png", AddExtension = true, OverwritePrompt = true };
			if (picker.ShowDialog(this) == DialogResult.OK)
				await SavePreviewAsync(picker.FileName);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	/// <summary>Writes the same bounded PNG image offered by the native toolbar.</summary>
	public async Task SavePreviewAsync(string path) {
		Guard.ArgumentNotNullOrEmpty(path, nameof(path));
		using var image = CreatePreviewImage();
		using var output = new MemoryStream();
		image.Save(output, ImageFormat.Png);
		await File.WriteAllBytesAsync(path, output.ToArray());
	}

	/// <summary>Renders the active grid's first forty rows into an owned bitmap.</summary>
	public Bitmap CreatePreviewImage() {
		var grid = PrepareExport();
		return new SourceGrid.Exporter.Image().Export(grid, GetPreviewRange(grid));
	}

	// The preview dialog and print document remain owned until the modal operation completes.
	private async void PrintClicked(object sender, EventArgs args) {
		try {
			var grid = PrepareExport();
			using var document = new SourceGrid.Exporter.GridPrintDocument(grid) { RangeToPrint = GetPreviewRange(grid), PageTitleText = "SourceGrid Gallery" };
			using var preview = new PrintPreviewDialog { Document = document, UseAntiAlias = true, ClientSize = new Size(1000, 700) };
			await preview.ShowDialogAsync(this);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	// Finish any active cell edit before reading its current model value.
	private SourceGrid.GridVirtual PrepareExport() {
		if (!_initialized)
			ResetExamples();
		var grid = SelectedGrid;
		if (!grid.Selection.Focus(SourceGrid.Position.Empty, false))
			throw new InvalidOperationException("Finish or cancel the current cell edit before exporting.");
		return grid;
	}

	// Bound rendering allocations for large virtual datasets while retaining every column.
	private static SourceGrid.CellRange GetPreviewRange(SourceGrid.GridVirtual grid) {
		return new SourceGrid.CellRange(0, 0, Math.Min(grid.Rows.Count, _previewRows) - 1, grid.Columns.Count - 1);
	}

	// Reset every source using the same button path shown in the native toolbar.
	private void ResetClicked(object sender, EventArgs args) {
		ResetExamples();
	}

	/// <summary>Restores the editable examples and their independent grid data sources.</summary>
	public void ResetExamples() {
		CancelEditing();
		ReleaseEditors();
		ReleaseDataView();
		BuildCellLab();
		BuildBoundList();
		BuildDataView();
		BuildVirtualArray();
		BuildPlanning();
		BuildPing();
		BuildListEditor();
		_initialized = true;
		UpdateStatus();
		RecordEvent("Loaded seven raw SourceGrid examples. All values are demonstration data.");
	}

	// Populate real cells, editors and span models rather than wrapping a VisualGrid.
	private void BuildCellLab() {
		_cellGrid.Redim(10, 6);
		_cellGrid.FixedRows = 2;
		_cellGrid.FixedColumns = 1;
		_cellGrid[0, 0] = new SourceGrid.Cells.Cell("Cell laboratory — double-click or press F2 to edit") {
			ColumnSpan = 6,
			View = new SourceGrid.Cells.Views.Cell { BackColor = Color.FromArgb(34, 61, 91), ForeColor = Color.White }
		};
		var headers = new[] { "Example", "Text", "Value", "Choice", "Due date", "Ready" };
		for (var column = 0; column < headers.Length; column++)
			_cellGrid[1, column] = new SourceGrid.Cells.ColumnHeader(headers[column]);

		for (var row = 2; row < 5; row++) {
			_cellGrid[row, 0] = new SourceGrid.Cells.RowHeader($"Record {row - 1}");
			_cellGrid[row, 1] = new SourceGrid.Cells.Cell(new[] { "Filter cartridge", "Motor assembly", "Sensor module" }[row - 2], typeof(string));
			_cellGrid[row, 2] = new SourceGrid.Cells.Cell(12.50m * (row - 1), typeof(decimal));
			_cellGrid[row, 3] = new SourceGrid.Cells.Cell(DayOfWeek.Monday, typeof(DayOfWeek));
			_cellGrid[row, 4] = new SourceGrid.Cells.Cell(new DateTime(2026, 11, row), typeof(DateTime));
			_cellGrid[row, 5] = new SourceGrid.Cells.CheckBox("Ready", row != 4);
		}

		_cellGrid[5, 0] = new SourceGrid.Cells.RowHeader("Spans");
		_cellGrid[5, 1] = new SourceGrid.Cells.Cell("One cell spans three columns") { ColumnSpan = 3 };
		_cellGrid[5, 4] = new SourceGrid.Cells.Cell("Two rows") { RowSpan = 2 };
		_cellGrid[5, 5] = new SourceGrid.Cells.CheckBox(null, true);
		_cellGrid[6, 0] = new SourceGrid.Cells.RowHeader("Actions");
		var action = new SourceGrid.Cells.Controllers.Button();
		action.Executed += (_, _) => RecordEvent("The cell button executed its controller action.");
		_cellGrid[6, 1] = new SourceGrid.Cells.Button("Run action");
		_cellGrid[6, 1].AddController(action);
		var link = new SourceGrid.Cells.Controllers.Button();
		link.Executed += (_, _) => RecordEvent("The link cell executed its controller action.");
		_cellGrid[6, 2] = new SourceGrid.Cells.Link("Inspect this cell");
		_cellGrid[6, 2].AddController(link);
		_cellGrid[6, 3] = new SourceGrid.Cells.Cell(Color.Teal, typeof(Color));
		_cellGrid[6, 5] = new SourceGrid.Cells.CheckBox(null, false);
		_cellGrid[7, 0] = new SourceGrid.Cells.RowHeader("Rich text");
		_cellGrid[7, 1] = new SourceGrid.Cells.RichTextBox(new RichText(@"{\rtf1\ansi SourceGrid supports \b rich text\b0  cells.}")) { ColumnSpan = 4 };
		_cellGrid[7, 5] = new SourceGrid.Cells.CheckBox(null, true);
		_cellGrid[8, 0] = new SourceGrid.Cells.RowHeader("Images");
		_cellGrid[8, 1] = new SourceGrid.Cells.Cell("Caption and image") { Image = Resources.Test16x16, ColumnSpan = 2 };
		_cellGrid[8, 3] = new SourceGrid.Cells.Cell("Images render in PNG export") { ColumnSpan = 3 };
		_cellGrid[9, 0] = new SourceGrid.Cells.RowHeader("Validation");
		_cellGrid[9, 1] = new SourceGrid.Cells.Cell(50, new SourceGrid.Cells.Editors.TextBox(typeof(int)) { MinimumValue = 0, MaximumValue = 100, AllowNull = false });
		_cellGrid[9, 2] = new SourceGrid.Cells.Cell("Enter an integer from 0 to 100") { ColumnSpan = 4 };
		for (var row = 0; row < _cellGrid.Rows.Count; row++)
			for (var column = 0; column < _cellGrid.Columns.Count; column++)
				OwnEditor(_cellGrid[row, column]?.Editor);
		_cellGrid.AutoSizeCells();
		_cellGrid.Rows[7].Height = 42;
		_cellGrid.AutoStretchColumnsToFitWidth = true;
	}

	// Bind ordinary row objects through the extracted general-purpose list adapter.
	private void BuildBoundList() {
		_listGrid.DataSource = new BoundList<SourceGridGalleryRow>(CreateRows().ToList()) {
			AllowEdit = true,
			AllowNew = true,
			AllowDelete = true,
			AllowSort = true
		};
		foreach (SourceGrid.DataGridColumn column in _listGrid.Columns) {
			OwnEditor(column.DataCell?.Editor);
			if (column.PropertyColumn?.Name != nameof(SourceGridGalleryRow.Quantity))
				continue;
			column.Conditions.Add(new SourceGrid.Conditions.ConditionView(new SourceGrid.Cells.Views.Cell {
				BackColor = Color.FromArgb(255, 235, 214),
				ForeColor = Color.FromArgb(141, 53, 12)
			}) {
				EvaluateFunction = (_, _, row) => row is SourceGridGalleryRow { Quantity: < 3 }
			});
		}
		_listGrid.AutoSizeCells();
	}

	// Show DataTable edits and header sorting through the same native grid binding contract.
	private void BuildDataView() {
		_dataTable = new DataTable("Orders");
		_dataTable.Columns.Add("Name", typeof(string)).DefaultValue = "New order";
		_dataTable.Columns.Add("Quantity", typeof(int)).DefaultValue = 0;
		_dataTable.Columns.Add("UnitPrice", typeof(decimal)).DefaultValue = 0m;
		_dataTable.Columns.Add("DueDate", typeof(DateTime)).DefaultValue = new DateTime(2026, 11, 2);
		_dataTable.Columns.Add("Approved", typeof(bool)).DefaultValue = false;
		foreach (var row in CreateRows())
			_dataTable.Rows.Add(row.Name, row.Quantity, row.UnitPrice, row.DueDate, row.Approved);
		_dataBinding = new BoundDataView(_dataTable.DefaultView);
		_viewGrid.DataSource = _dataBinding;
		foreach (SourceGrid.DataGridColumn column in _viewGrid.Columns)
			OwnEditor(column.DataCell?.Editor);
		_viewGrid.AutoSizeCells();
	}

	// A shared cell model projects a sizeable array without materializing a cell per value.
	private void BuildVirtualArray() {
		var values = new int[10000, 5];
		for (var row = 0; row < values.GetLength(0); row++)
			for (var column = 0; column < values.GetLength(1); column++)
				values[row, column] = (row + 1) * 100 + column;
		_arrayGrid.FixedRows = 1;
		_arrayGrid.FixedColumns = 1;
		while (_arrayGrid.Columns.Count < values.GetLength(1) + _arrayGrid.FixedColumns)
			_arrayGrid.Columns.Add(new SourceGrid.ColumnInfo(_arrayGrid));
		_arrayGrid.DataSource = values;
		OwnEditor(_arrayGrid.ValueCell?.Editor);
		for (var column = 0; column < _arrayGrid.Columns.Count; column++)
			_arrayGrid.Columns[column].Width = column == 0 ? 90 : 140;
	}

	// PlanningGrid renders appointment duration as row spans over a three-day schedule.
	private void BuildPlanning() {
		var start = new DateTime(2026, 11, 2, 8, 0, 0);
		_planningGrid.Appointments.Clear();
		_planningGrid.Appointments.Add(new SourceGrid.Planning.AppointmentBase("Design review", start.AddHours(1), start.AddHours(2)) {
			View = new SourceGrid.Cells.Views.Cell { BackColor = Color.FromArgb(211, 232, 249), ForeColor = Color.FromArgb(30, 71, 102) }
		});
		_planningGrid.Appointments.Add(new SourceGrid.Planning.AppointmentBase("Integration workshop", start.AddDays(1).AddHours(3), start.AddDays(1).AddHours(5)) {
			View = new SourceGrid.Cells.Views.Cell { BackColor = Color.FromArgb(218, 239, 224), ForeColor = Color.FromArgb(35, 90, 50) }
		});
		_planningGrid.LoadPlanning(start, start.AddDays(2).AddHours(10), 30);
	}

	// Adopt the completed IPingData list implementation for virtual property reads and sorting.
	private void BuildPing() {
		var source = new SourceGrid.Extensions.PingGrids.ListPingSource<SourceGridGalleryRow>();
		source.AddRange(CreateRows());
		_pingGrid.Columns.Clear();
		_pingGrid.Columns.Add(nameof(SourceGridGalleryRow.Name), "Machine", SourceGrid.Extensions.PingGrids.Cells.Cell.Create(typeof(string), false));
		_pingGrid.Columns.Add(nameof(SourceGridGalleryRow.Quantity), "Available units", SourceGrid.Extensions.PingGrids.Cells.Cell.Create(typeof(int), false));
		_pingGrid.Columns.Add(nameof(SourceGridGalleryRow.UnitPrice), "Unit price", SourceGrid.Extensions.PingGrids.Cells.Cell.Create(typeof(decimal), false));
		_pingGrid.Columns.Add(nameof(SourceGridGalleryRow.Approved), "Approved", SourceGrid.Extensions.PingGrids.Cells.Cell.Create(typeof(bool), false));
		_pingGrid.DataSource = source;
		foreach (SourceGrid.Extensions.PingGrids.PingGridColumn column in _pingGrid.Columns)
			OwnEditor(column.DataCell?.Editor);
		_pingGrid.AutoSizeCells();
	}

	// Adopt the original list composite and its own add/remove/reorder toolbar.
	private void BuildListEditor() {
		_listEditor.ItemType = typeof(SourceGridGalleryRow);
		_listEditor.List = new List<SourceGridGalleryRow>(CreateRows());
		_listEditor.LoadList();
	}

	// Share deterministic data between object, DataView and property-driven examples.
	private static SourceGridGalleryRow[] CreateRows() {
		return new[] {
			new SourceGridGalleryRow { Name = "Lumen pump", Quantity = 8, UnitPrice = 125.50m, DueDate = new DateTime(2026, 11, 2), Approved = true },
			new SourceGridGalleryRow { Name = "Atlas valve", Quantity = 2, UnitPrice = 39.95m, DueDate = new DateTime(2026, 11, 3), Approved = false },
			new SourceGridGalleryRow { Name = "Nova motor", Quantity = 14, UnitPrice = 210.00m, DueDate = new DateTime(2026, 11, 4), Approved = true }
		};
	}

	// Reset intentionally discards only this gallery's editable demonstration values.
	private void CancelEditing() {
		foreach (var grid in new SourceGrid.GridVirtual[] { _cellGrid, _listGrid, _viewGrid, _arrayGrid, _planningGrid.Grid, _pingGrid, _listEditor.Grid }) {
			var position = grid.Selection.ActivePosition;
			if (!position.IsEmpty())
				new SourceGrid.CellContext(grid, position).EndEdit(true);
		}
		_listGrid.EndEditingRow(true);
		_viewGrid.EndEditingRow(true);
	}

	// Detach the grid before disposing its adapter and the table owned by this screen.
	private void ReleaseDataView() {
		_viewGrid.DataSource = null;
		_dataBinding?.Dispose();
		_dataBinding = null;
		_dataTable?.Dispose();
		_dataTable = null;
	}

	// These samples use new per-cell/column editors; shared engine defaults are never registered here.
	private void OwnEditor(SourceGrid.Cells.Editors.EditorBase editor) {
		if (editor != null && !_sampleEditors.Contains(editor))
			_sampleEditors.Add(editor);
	}

	// Ended edits can release their native controls before new samples replace the old cell models.
	private void ReleaseEditors() {
		_sampleEditors.Dispose();
		_sampleEditors.Clear();
	}

	// Exercise multi-cell selection through the selected engine's real selection contract.
	private void SelectAllClicked(object sender, EventArgs args) {
		SelectedGrid.Selection.SelectRange(SelectedGrid.CompleteRange, true);
		UpdateStatus();
	}

	// Clear the selected ranges and active focus together.
	private void ClearSelectionClicked(object sender, EventArgs args) {
		SelectedGrid.Selection.ResetSelection(false);
		UpdateStatus();
	}

	// Switching examples updates export bounds and the engine summary.
	private void ExampleChanged(object sender, EventArgs args) {
		UpdateStatus();
		if (HasSettings && !Updating && _initialized) {
			ScreenSettings.SelectedExample = _examples.SelectedIndex;
			MarkSettingsChanged();
		}
	}

	// Describe the active native surface and make bounded image/print export visible to the user.
	private void UpdateStatus() {
		if (!_initialized)
			return;
		var grid = SelectedGrid;
		_status.Text = $"{_examples.SelectedTab.Text} / {grid.GetType().Name}  •  {grid.Rows.Count:N0} rows × {grid.Columns.Count} columns  •  {grid.Selection.GetSelectionRegion().Count} selected region(s)";
		_previewRange.Text = grid.Rows.Count > _previewRows ? $"PNG / print: first {_previewRows} rows" : "PNG / print: all rows";
	}

	// Standalone samples keep their first tab; hosted samples restore only their presentation choice.
	private void ApplyPreferences() {
		if (!HasSettings || IsDisposed || Disposing)
			return;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing))
			_examples.SelectedIndex = Math.Clamp(ScreenSettings.SelectedExample, 0, _examples.TabCount - 1);
	}

	// Record the focused cell through the grid's global controller dispatch.
	private void CellFocused(object sender, EventArgs args) {
		var context = (SourceGrid.CellContext)sender;
		RecordEvent($"{context.Grid.GetType().Name}: focused {context.Position}.");
	}

	// Record a committed model change instead of reflecting editor keystrokes.
	private void CellValueChanged(object sender, EventArgs args) {
		var context = (SourceGrid.CellContext)sender;
		RecordEvent($"{context.Grid.GetType().Name}: value changed at {context.Position}.");
	}

	// Show the real in-place editor lifecycle in the event panel.
	private void CellEditStarted(object sender, EventArgs args) {
		var context = (SourceGrid.CellContext)sender;
		RecordEvent($"{context.Grid.GetType().Name}: editing {context.Position}.");
	}

	// Planning emits domain-specific appointment and empty-slot notifications.
	private void PlanningClicked(object sender, SourceGrid.Planning.AppointmentEventArgs args) {
		RecordEvent($"Planning: {args.DateTimeStart:ddd HH:mm} — {args.Appointment?.Title ?? "available time slot"}.");
	}

	// Report real composite list edits and row-management commands.
	private void ListEditorChanged(object sender, EventArgs args) {
		RecordEvent($"List editor changed: {_listEditor.List?.Count ?? 0} rows.");
	}

	// Publish a concise, bounded event trace on the screen's owning UI thread.
	private void RecordEvent(string message) {
		_eventLog.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
		if (_eventLog.Lines.Length > 80)
			_eventLog.Lines = _eventLog.Lines.TakeLast(60).ToArray();
		_eventLog.SelectionStart = _eventLog.TextLength;
		_eventLog.ScrollToCaret();
	}

	#region Inner Types

	/// <summary>The raw engine gallery's selected example, separate from its editable demonstration rows.</summary>
	[UserEditable]
	public class SourceGridGalleryScreenSettings : SettingsObject {
		/// <summary>The initial tab, from the cell laboratory through the list editor.</summary>
		[DefaultValue(0)]
		[DisplayName("Selected example (0–6)"), Range(0, 6)]
		public int SelectedExample {
			get; set;
		}
	}
	#endregion
}
