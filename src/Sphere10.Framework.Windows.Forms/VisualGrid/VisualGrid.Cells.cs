// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

namespace Sphere10.Framework.Windows.Forms;

// Turns column bindings and entity values into native cells and shared cell views.
// Binding uses these routines for both UI-thread creation and worker-prepared read-only pages.
public partial class VisualGrid {
	private readonly record struct StyledViewKey(SourceGrid.Cells.Views.Cell Prototype, VisualGridRowStyle Style);

	#region Fields

	// The displayed columns in native column order; hidden columns of the declared schema are omitted.
	private IVisualGridColumn[] _columnsBindings;

	private IVisualGridColumn[] _schemaBindings = Array.Empty<IVisualGridColumn>();

	private readonly Dictionary<IVisualGridColumn, SourceGrid.Cells.Views.Cell> _imageCellViews = new();

	private ActionCache<StyledViewKey, StyledCellView> _rowCellViews;

	#endregion

	#region Properties

	/// <summary>Chooses row colors. With worker preparation enabled, this delegate runs off the UI thread and must read detached values only.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<object, VisualGridRowStyle> RowStyleProvider { get; set; }

	/// <summary>The declared column schema in presentation order, including hidden columns.</summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IVisualGridColumn[] GridBindings {
		get => _schemaBindings;
		set {
			using (Tools.Scope.ExecuteOnDispose(NotifyColumnLayoutChanged)) {
				var bindings = value?.ToArray() ?? Array.Empty<IVisualGridColumn>();
				if (_preferences != null)
					ValidatePreferenceBindings(bindings);
				_schemaBindings = OrderColumnBindings(bindings, _preferences?.Columns);
				RestoreColumnVisibility(_preferences?.Columns);
				_columnsBindings = _schemaBindings.Where(column => !IsColumnHidden(column)).ToArray();

				// Screen preferences can arrive before the source supplies its column schema.
				if (_preferences != null && !_restoringGridSettings)
					ApplyGridPreferencesCore(_preferences);
				else
					RestoreColumnPreferences(_preferences?.Columns);
				_imageCellViews.Clear();
				ResetRowHeightMeasurements();
				if (_sortColumnName == null)
					return;
				var sortColumn = Array.FindIndex(_columnsBindings, column => column.SortName == _sortColumnName);
				if (sortColumn >= 0) {
					_sortColumnIndex = sortColumn;
					return;
				}
				_sortColumnName = null;
				_sortColumnIndex = 0;
				_sortDirection = SortDirection.Ascending;
			}
		}
	}

	/// <summary>The displayed columns in native column order; hidden columns are omitted.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IVisualGridColumn[] VisibleGridBindings => _columnsBindings;

	#endregion

	#region Methods

	private void ConfigureCellInteraction(ICell cell, IVisualGridColumn column, object entity) {
		var canEdit = AllowCellEditing && _crudCapabilities.HasFlag(DataSourceCapabilities.CanUpdate) && column.CanEditCell && column.CellHasValue(entity);
		// Display-only text cells keep conversion support without constructing an unused native editor control.
		// Promote the existing cell when editing is enabled later; do not require another data-source read.
		if (canEdit && column.DisplayType == VisualGridCellDisplayType.Text && cell.Editor is not SourceGrid.Cells.Editors.EditorControlBase)
			cell.Editor = SourceGrid.Cells.Editors.Factory.Create(column.DataType);
		if (cell.Editor != null) {
			if (!canEdit && cell.Editor.IsEditing)
				new CellContext(_grid, new Position(cell.Row.Index, cell.Column.Index)).EndEdit(true);
			cell.Editor.EnableEdit = canEdit;
			cell.Editor.EditableMode = LeftClickToDeselect ? EditableMode.None : EditableMode.Focus | EditableMode.SingleClick;
		}
		var valueController = cell.FindController<UpdateEntityOnValueChangedController>();
		if (canEdit && cell.Editor != null && valueController == null)
			cell.AddController(new UpdateEntityOnValueChangedController(this, _dataSource, column, entity));
		else if (!canEdit && valueController != null)
			cell.RemoveController(valueController);

		// Selection-only cells must not activate SourceGrid's focus-driven editor or row selection.
		cell.RemoveController(SourceGrid.Cells.Controllers.Unselectable.Default);
		if (!AllowCellEditing || LeftClickToDeselect)
			cell.AddController(SourceGrid.Cells.Controllers.Unselectable.Default);

		// Checkbox clicks normally change the value directly; reserve that action for the explicit edit gesture when rows toggle.
		if (cell is SourceGrid.Cells.CheckBox) {
			cell.RemoveController(SourceGrid.Cells.Controllers.CheckBox.Default);
			if (!LeftClickToDeselect)
				cell.AddController(SourceGrid.Cells.Controllers.CheckBox.Default);
		}

		var rowSelector = cell.FindController<SourceGrid.Cells.Controllers.RowSelector>();
		if (rowSelector != null)
			cell.RemoveController(rowSelector);
		if (!LeftClickToDeselect)
			cell.AddController(new SourceGrid.Cells.Controllers.RowSelector(!SelectOnMouseUp));
	}

	/// <summary>Checks whether prepared values still match the columns' current display and data types under the read-only contract.</summary>
	private bool PreparedFormatsAreCurrent(IVisualGridColumn[] columns, (VisualGridCellDisplayType DisplayType, Type DataType)[] formats)
		=> CanPrepareReadOnlyCells(columns) && !columns.Where((column, index) => (column.DisplayType, column.DataType) != formats[index]).Any();

	/// <summary>Checks whether the current bindings support detached read-only value preparation.</summary>
	private bool CanPrepareReadOnlyCells(IVisualGridColumn[] columns)
		=> PrepareReadOnlyCellsInBackground && columns.All(column => column.DisplayType is VisualGridCellDisplayType.Text or VisualGridCellDisplayType.Image)
					&& (!AllowCellEditing || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanUpdate) || columns.All(column => !column.CanEditCell));

	/// <summary>Captures source values and display strings before native cell publication.</summary>
	private static VisualGridPreparedValue[][] PrepareReadOnlyRows(object[] entities, IVisualGridColumn[] columns, (VisualGridCellDisplayType DisplayType, Type DataType)[] formats,
		Func<object, VisualGridRowStyle> styleProvider) {
		// Conversion metadata is initialized once per populated text column on the worker; rows carry only values and prepared strings.
		var editors = new VisualGridPreparedTextEditor[columns.Length];
		var rows = new VisualGridPreparedValue[entities.Length][];
		for (var row = 0; row < entities.Length; row++) {
			var rowStyle = styleProvider?.Invoke(entities[row]) ?? default;
			// Keep one optional style per row rather than copying two Color values into every prepared cell.
			var preparedStyle = rowStyle.Foreground.IsEmpty && rowStyle.Background.IsEmpty ? null : new VisualGridPreparedRowStyle(rowStyle);
			rows[row] = new VisualGridPreparedValue[columns.Length];
			for (var column = 0; column < columns.Length; column++) {
				var hasValue = columns[column].CellHasValue(entities[row]);
				var value = hasValue ? columns[column].GetCellValue(entities[row]) : null;
				var editor = editors[column];
				if (hasValue && formats[column].DisplayType == VisualGridCellDisplayType.Text && editor == null)
					editor = editors[column] = new VisualGridPreparedTextEditor(formats[column].DataType);
				if (hasValue && formats[column].DisplayType == VisualGridCellDisplayType.Image)
					Guard.Argument(value == null || value is System.Drawing.Image, nameof(value), "An image cell requires a System.Drawing.Image value.");
				var displayText = hasValue && editor != null ? editor.PrepareValue(value) : string.Empty;
				rows[row][column] = new VisualGridPreparedValue(hasValue, value, displayText, editor, preparedStyle);
			}
		}
		return rows;
	}

	/// <summary>Updates or creates stable native cell slots from one prepared row.</summary>
	private void BindPreparedRow(int row, object entity, VisualGridPreparedValue[] cells, bool reuse, VisualGridPreparedCell[] newCells = null) {
		_rowToEntityMap[row] = entity;
		for (var column = 0; column < cells.Length; column++) {
			var prepared = cells[column];
			var view = GetColumnCellView(_columnsBindings[column], prepared.RowStyle);
			if (reuse && _grid[row, column] is VisualGridPreparedCell existing && existing.CanApplyPreparedValue
				&& existing.RowSpan == 1 && existing.ColumnSpan == 1 && existing.Editor is not SourceGrid.Cells.Editors.EditorControlBase
				&& existing.FindController<UpdateEntityOnValueChangedController>() == null) {
				// Retain the native cell's complete graph; publishing new data does not allocate or fire edit callbacks.
				existing.View = view;
				existing.ApplyPreparedValue(prepared);
			} else {
				var cell = newCells?[column] ?? new VisualGridPreparedCell(prepared);
				cell.View = view;
				ConfigureCellInteraction(cell, _columnsBindings[column], entity);
				_grid[row, column] = cell;
			}
		}
	}

	private void BindColumnHeaders(int col) {
		_grid[0, col] = new SourceGrid.Cells.ColumnHeader(_columnsBindings[col].ColumnName);
		_grid[0, col].ToolTipText = _columnsBindings[col].ColumnName
			+ (_crudCapabilities.HasFlag(DataSourceCapabilities.CanSort) && !string.IsNullOrEmpty(_columnsBindings[col].SortName)
				? ". Click to sort; click again to reverse the order." : string.Empty)
			+ (_columnLayoutSource == null ? " Drag the header boundary to resize this column." : " Column widths and order follow the linked table.");
		_grid[0, col].AddController(new CustomSortHeaderCellController(this));

		// if we are sorting by this column, set the sort icon on the column
		if (_sortDirection != SortDirection.None && _sortColumnName != null && _sortColumnIndex == col)
			((SourceGrid.Cells.Models.ISortableHeader)_grid[0, col].Model.FindModel(typeof(SourceGrid.Cells.Models.ISortableHeader))).SetSortMode(
				SourceGrid.CellContext.Empty,
				_sortDirection == SortDirection.Ascending ? Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.Ascending : Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.Descending
			);
	}

	private void BindRowInternal(int row, object entity) {
		_rowToEntityMap[row] = entity;
		var style = RowStyleProvider?.Invoke(entity) ?? default;
		for (var col = 0; col < _columnsBindings.Length; col++) {
			var columnBinding = _columnsBindings[col];
			var cell = CreateCell(columnBinding, entity);
			if (style != default)
				cell.View = GetColumnCellView(columnBinding, style);
			_grid[row, col] = cell;
		}
	}

	private ICell CreateCell(IVisualGridColumn column, object entity) {
		Cell cell;
		if (column.CellHasValue(entity)) {
			var cellValue = column.GetCellValue(entity);
			switch (column.DisplayType) {
				case VisualGridCellDisplayType.Text:
					cell = CreateTextBoxCell(column, entity, cellValue);
					break;
				case VisualGridCellDisplayType.Boolean:
					cell = CreateCheckBoxCell(column, entity, cellValue);
					break;
				case VisualGridCellDisplayType.Currency:
					cell = CreateCurrencyCell(column, entity, cellValue);
					break;
				case VisualGridCellDisplayType.Numeric:
					cell = CreateNumericCell(column, entity, cellValue);
					break;
				case VisualGridCellDisplayType.DateTime:
				case VisualGridCellDisplayType.Date:
				case VisualGridCellDisplayType.Time:
					cell = CreateDateTimeCell(column, entity, cellValue);
					break;
				case VisualGridCellDisplayType.DropDownList:
					cell = CreateDropDownListCell(column, entity, cellValue);
					break;
				case VisualGridCellDisplayType.Button:
					cell = CreateButtonCell(column, entity, cellValue);
					break;
				case VisualGridCellDisplayType.EditCommand:
					cell = CreateCommandCell(column, entity, CrudAction.Update);
					break;
				case VisualGridCellDisplayType.DeleteCommand:
					cell = CreateCommandCell(column, entity, CrudAction.Delete);
					break;
				case VisualGridCellDisplayType.Empty:
					cell = CreateEmptyCell();
					break;
				case VisualGridCellDisplayType.Image:
					Guard.Argument(cellValue == null || cellValue is System.Drawing.Image, nameof(cellValue), "An image cell requires a System.Drawing.Image value.");
					// The column owner retains image ownership; diagnostic icons are read-only display cells.
					cell = new Cell(string.Empty) { Image = cellValue as System.Drawing.Image, Editor = null };
					break;
				default:
					throw new NotImplementedException(string.Format("VisualGridCellDisplayType not supported '{0}'", column.DisplayType));
			}
		} else
			cell = CreateEmptyCell();

		if (column.DisplayType == VisualGridCellDisplayType.Image)
			cell.View = GetColumnCellView(column);
		ConfigureCellInteraction(cell, column, entity);
		return cell;
	}

	private SourceGrid.Cells.Views.Cell GetColumnCellView(IVisualGridColumn column) {
		var standard = SourceGrid.Cells.Views.Cell.Default;
		if (column.DisplayType != VisualGridCellDisplayType.Image)
			return standard;
		Guard.Argument(Enum.IsDefined(column.ImageAlignment), nameof(column.ImageAlignment), "Select a supported image alignment.");
		Guard.Argument(!column.ImageResizeMethod.HasValue || Enum.IsDefined(column.ImageResizeMethod.Value), nameof(column.ImageResizeMethod), "Select a supported image resize method.");
		if (!_imageCellViews.TryGetValue(column, out var view) || view.ElementImage is not Sphere10.Framework.Windows.Forms.Drawing.VisualElements.Image) {
			// Share one view per image binding, including the ordinary and prepared-cell paths.
			// Borrow default styling without cloning its transient image model or owned fonts.
			view = new SourceGrid.Cells.Views.Cell {
				Font = standard.Font,
				ForeColor = standard.ForeColor,
				BackColor = standard.BackColor,
				Border = standard.Border,
				Padding = standard.Padding,
				WordWrap = standard.WordWrap,
				TrimmingMode = standard.TrimmingMode,
				TextAlignment = standard.TextAlignment,
				ElementsDrawMode = standard.ElementsDrawMode,
				ElementText = standard.ElementText
			};
			_imageCellViews[column] = view;
		}
		view.ImageAlignment = column.ImageAlignment switch {
			Sphere10.Framework.ResizeAlignment.TopLeft => System.Drawing.ContentAlignment.TopLeft,
			Sphere10.Framework.ResizeAlignment.TopCenter => System.Drawing.ContentAlignment.TopCenter,
			Sphere10.Framework.ResizeAlignment.TopRight => System.Drawing.ContentAlignment.TopRight,
			Sphere10.Framework.ResizeAlignment.CenterLeft => System.Drawing.ContentAlignment.MiddleLeft,
			Sphere10.Framework.ResizeAlignment.CenterCenter => System.Drawing.ContentAlignment.MiddleCenter,
			Sphere10.Framework.ResizeAlignment.CenterRight => System.Drawing.ContentAlignment.MiddleRight,
			Sphere10.Framework.ResizeAlignment.BottomLeft => System.Drawing.ContentAlignment.BottomLeft,
			Sphere10.Framework.ResizeAlignment.BottomCenter => System.Drawing.ContentAlignment.BottomCenter,
			_ => System.Drawing.ContentAlignment.BottomRight
		};
		view.ImageStretch = column.ImageResizeMethod.HasValue;
		var image = (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.Image)view.ElementImage;
		image.ResizeMethod = column.ImageResizeMethod;
		image.ResizeAlignment = column.ImageAlignment;
		return view;
	}

	private SourceGrid.Cells.Views.Cell GetColumnCellView(IVisualGridColumn column, VisualGridRowStyle style) {
		var standard = GetColumnCellView(column);
		if (style == default)
			return standard;
		// Palette entries borrow fonts and image elements. Capacity is bounded even when a caller generates changing colors.
		_rowCellViews ??= new ActionCache<StyledViewKey, StyledCellView>(key => new StyledCellView(key.Prototype, key.Style),
			_ => 1, reapStrategy: CacheReapPolicy.LongestIdle, maxCapacity: 512);
		return _rowCellViews[new StyledViewKey(standard, style)];
	}

	private Cell CreateEmptyCell() => new Cell(string.Empty);

	private Cell CreateTextBoxCell(IVisualGridColumn columnBinding, object entity, object cellValue) {
		if (AllowCellEditing && _crudCapabilities.HasFlag(DataSourceCapabilities.CanUpdate) && columnBinding.CanEditCell && columnBinding.CellHasValue(entity))
			return new Cell(cellValue, columnBinding.DataType);
		// EditorBase uses the same type converter as the visual editor, without EditorControlBase.CreateControl().
		var display = new SourceGrid.Cells.Editors.EditorBase(columnBinding.DataType) { EnableEdit = false, EditableMode = EditableMode.None };
		return new Cell(cellValue, display);
	}

	private Cell CreateCheckBoxCell(IVisualGridColumn columnBinding, object entity, object cellValue) {
		var checkbox = new SourceGrid.Cells.CheckBox(null, (bool?)cellValue) {
			Editor = { EnableEdit = AllowCellEditing && columnBinding.CanEditCell }
		};
		return checkbox;
	}

	private Cell CreateCurrencyCell(IVisualGridColumn columnBinding, object entity, object cellValue)
		=> new Cell(cellValue, new SourceGrid.Cells.Editors.TextBoxCurrency(columnBinding.DataType));

	private Cell CreateNumericCell(IVisualGridColumn columnBinding, object entity, object cellValue)
		=> new Cell(cellValue, new SourceGrid.Cells.Editors.TextBoxNumeric(columnBinding.DataType));

	private Cell CreateDateTimeCell(IVisualGridColumn columnBinding, object entity, object cellValue) {
		const DateTimeStyles dtStyles = System.Globalization.DateTimeStyles.AllowInnerWhite | System.Globalization.DateTimeStyles.AllowLeadingWhite | System.Globalization.DateTimeStyles.AllowTrailingWhite |
										System.Globalization.DateTimeStyles.AllowWhiteSpaces;
		var editor = columnBinding.DisplayType == VisualGridCellDisplayType.Time ? new SourceGrid.Cells.Editors.TimePicker() : new SourceGrid.Cells.Editors.DateTimePicker();
		editor.AllowNull = columnBinding.DataType == typeof(DateTime?);
		var customFormat = columnBinding.GetDateTimeFormat(entity);
		editor.Control.CustomFormat = customFormat;
		var dtParseFormats = new[] { customFormat };
		var dtConverter = new Sphere10.Framework.ComponentModel.DateTimeTypeConverter(customFormat, dtParseFormats, dtStyles);
		editor.TypeConverter = dtConverter;
		return new Cell(cellValue, editor);
	}

	private Cell CreateDropDownListCell(IVisualGridColumn columnBinding, object entity, object cellValue) {
		if (columnBinding.ReferenceBinding != null)
			return new Cell(cellValue, new SourceGrid.Cells.Editors.VisualGridReference(columnBinding.DataType, columnBinding.ReferenceBinding));
		var editor = new SourceGrid.Cells.Editors.DropDownList(columnBinding.DataType, columnBinding.DropDownItemDisplayMember);
		editor.Control.ValueMember = columnBinding.DropDownItemDisplayMember;
		editor.Control.DisplayMember = columnBinding.DropDownItemDisplayMember;
		editor.EditException += async (o, args) => {
			args.Handled = true;
			editor.UndoEditValue();
			await ExceptionDialog.ShowAsync(this, args.Exception);
		};
		var cell = new Cell(columnBinding.DataType, editor);
		editor.Control.DropDownStyle = ComboBoxStyle.DropDownList;
		cell.AddController(new AutoPopulateDropDownListOnEditStarting(editor, columnBinding, entity));
		cell.Value = cellValue;
		return cell;
	}

	private Cell CreateButtonCell(IVisualGridColumn columnBinding, object entity, object cellValue)
		=> CreateButtonCell(columnBinding, entity, columnBinding.GetButtonCaption(entity), columnBinding.GetButtonImage(entity), columnBinding.ButtonPressed);

	private Cell CreateCommandCell(IVisualGridColumn columnBinding, object entity, CrudAction action) {
		System.Drawing.Image image;
		Func<object, Task> callback;
		switch (action) {
			case CrudAction.Update:
				image = Resources.SmallEditIcon;
				callback = EditEntity;
				break;
			case CrudAction.Delete:
				image = Resources.Cross;
				callback = DeleteEntity;
				break;
			case CrudAction.Create:
			default:
				throw new ArgumentException(string.Format("Invalid command '{0}'", action), "action");
		}
		return CreateButtonCell(columnBinding, entity, string.Empty, image, async entity => {
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => callback(entity));
			if (error != null)
				await ExceptionDialog.ShowAsync(this, error);
		});
	}

	private Cell CreateButtonCell(IVisualGridColumn columnBinding, object entity, string caption, System.Drawing.Image image, Action<object> clickHandler) {
		var button = new SourceGrid.Cells.Button(caption) {
			Image = image
		};
		var clickHandlerController = new SourceGrid.Cells.Controllers.Button();
		clickHandlerController.Executed += (o, args) => clickHandler(entity);
		button.AddController(clickHandlerController);
		return button;
	}

	#endregion

	#region Inner Types

	private sealed class StyledCellView : SourceGrid.Cells.Views.Cell {
		public StyledCellView(SourceGrid.Cells.Views.Cell source, VisualGridRowStyle style) {
			Font = source.Font;
			ForeColor = style.Foreground.IsEmpty ? source.ForeColor : style.Foreground;
			BackColor = style.Background.IsEmpty ? source.BackColor : style.Background;
			Border = source.Border;
			Padding = source.Padding;
			WordWrap = source.WordWrap;
			TrimmingMode = source.TrimmingMode;
			TextAlignment = source.TextAlignment;
			ElementsDrawMode = source.ElementsDrawMode;
			ElementText = source.ElementText;
			ElementImage = source.ElementImage;
		}
	}

	#endregion
}
