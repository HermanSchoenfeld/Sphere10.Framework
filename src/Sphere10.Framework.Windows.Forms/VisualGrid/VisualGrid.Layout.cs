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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

namespace Sphere10.Framework.Windows.Forms;

// Owns control layout, loading surfaces and native row/column measurements.
// Binding calls the sizing routines after publication; Paging uses the resulting viewport geometry.
public partial class VisualGrid {
	#region Fields

	private const int _defaultRowHeight = 24;

	private bool _layingOutPageBar;

	private bool _sizingColumns;

	private bool _columnsSizedFromData;

	private IVisualGridColumn[] _sizedColumnBindings;

	private int[] _columnMinimumWidths = Array.Empty<int>();

	private int _columnViewportWidth;

	private readonly Dictionary<IVisualGridColumn, int> _columnWidthOverrides = new();

	private int _loadingIndicatorScopes;

	private readonly VisualGridRowMeasurements _rowMeasurements = new();

	#endregion

	#region Properties

	[Description("Measures row heights on every refresh. Disable for frequently refreshed single-line, read-only text/image tables to retain measured heights; new rows and changes to layout, fonts or image heights are still measured. Multiline, wrapped, editable and custom cells always measure normally.")]
	[Category("Appearance")]
	[DefaultValue(true)]
	public bool AutoSizeRowsOnRefresh { get; set; } = true;

	[Category("Appearance")]
	[DefaultValue("")]
	public string GridTitle {
		get;
		set {
			if (IsDisposed || Disposing)
				return;
			field = value ?? string.Empty;
			// Appearance properties are usable during construction and on the designer's owning thread.
			if (IsHandleCreated && InvokeRequired) {
				try {
					BeginInvoke(new Action(() => {
						if (!IsDisposed && !Disposing) {
							_titleLabel.Text = field;
							UpdateButtonBarVisibility();
						}
					}));
				} catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated) {
					// A closing native surface no longer needs the queued appearance update.
				}
				return;
			}
			_titleLabel.Text = field;
			UpdateButtonBarVisibility();
		}
	} = string.Empty;

	[Description("Alignment of the grid title within its title area.")]
	[Category("Appearance")]
	[DefaultValue(System.Drawing.ContentAlignment.MiddleCenter)]
	public System.Drawing.ContentAlignment GridTitleAlignment {
		get => _titleLabel.TextAlign;

		set {
			Guard.Argument(Enum.IsDefined(value), nameof(value), "Select a supported title alignment.");
			_titleLabel.TextAlign = value;
		}
	}

	/// <summary>Controls the independent title/search/CRUD surface above the table.</summary>
	[Category("Appearance"), DefaultValue(true)]
	public bool HeaderVisible {
		get;
		set {
			if (field == value)
				return;
			field = value;
			if (_topPanel != null)
				UpdateButtonBarVisibility();
		}
	} = true;

	/// <summary>Controls native column headings independently of the title/search surface.</summary>
	[Category("Appearance"), DefaultValue(true)]
	public bool ColumnHeadersVisible {
		get;
		set {
			if (field == value)
				return;
			field = value;
			ResetRowHeightMeasurements();
			ApplyColumnHeaderVisibility();
		}
	} = true;

	/// <summary>Controls totals and paging controls. A footerless consumer must choose a reachable query mode, such as Single Paged.</summary>
	[Category("Appearance"), DefaultValue(true)]
	public bool FooterVisible {
		get;
		set {
			if (field == value)
				return;
			field = value;
			if (_bottomPanel != null)
				OrganizeLayout();
		}
	} = true;

	#endregion

	#region Methods

	/// <summary>Measures current columns and rows using the existing native sizing path.</summary>
	internal void AutoSizeCells() {
		if (_sizedColumnBindings != _columnsBindings)
			return;
		SizeBoundColumns();
		RememberRowHeightGeometry();
		RefreshAutoPageSize();
	}

	private void OrganizeLayout() {
		UpdateButtonBarVisibility();
		_createButton.Visible = _createButton.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanCreate);
		_deleteButton.Enabled = _deleteToolStripMenuItem.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanDelete);
		UpdateDeleteButtonVisibility();
		_editToolStripMenuItem.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanUpdate);
		_searchTextBox.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanSearch);

		if (!_crudCapabilities.HasFlag(DataSourceCapabilities.CanSort)) {
			_sortColumnName = null;
			_sortColumnIndex = 0;
		}

		if (_crudCapabilities.HasFlag(DataSourceCapabilities.CanPage)) {
			ShowPageBar();
			_pageSize = PageMode switch {
				VisualGridPageMode.Auto => CalculateAutoPageSize(),
				VisualGridPageMode.Single => int.MaxValue,
				_ => _manualPageSize
			};
			UpdatePageSizeDisplay();
			UpdatePageModeControl();
			_pageModeToolStrip.Enabled = true;
			_manualPageSizePanel.Visible = _manualPageSizePanel.Enabled = PageMode == VisualGridPageMode.Paged;
			_pageNavigationPanel.Visible = _pageNavigationPanel.Enabled = PageMode != VisualGridPageMode.Single;
		} else {
			HidePageBar();
			_pageSize = int.MaxValue;
			_pageNumber = 0;
			SetVisiblePageNumberText(0);
		}
	}

	private void PageBarLayout(object sender, LayoutEventArgs args) {
		if (_layingOutPageBar || _pageSizeUpDown == null)
			return;
		_layingOutPageBar = true;
		using (Tools.Scope.ExecuteOnDispose(() => _layingOutPageBar = false)) {
			// Keep every footer element on the same baseline at the current font and DPI.
			var height = Math.Max(_pageSizeUpDown.PreferredHeight, _pageNumberBox.PreferredHeight);
			var spacing = Math.Max(1, (int)Math.Round(3 * DeviceDpi / 96.0));
			foreach (var button in new[] { _firstPageButton, _previousPageButton, _nextPageButton, _lastPageButton }) {
				button.Size = new System.Drawing.Size(height, height);
				button.Margin = new Padding(spacing);
			}
			_pageSizeUpDown.Margin = _pageNumberBox.Margin = new Padding(spacing);
			_pageModeToolStrip.Margin = new Padding(spacing, spacing, spacing * 2, spacing);
			_pageNavigationPanel.Margin = new Padding(spacing, 0, 0, 0);
			_totalRecordsPanel.Margin = new Padding(0, 0, spacing * 2, 0);
			_pageSizeUpDown.Width = TextRenderer.MeasureText("9999", Font).Width + SystemInformation.VerticalScrollBarWidth + 12;
			_pageModeToolStrip.Font = Font;
			_pageModeToolStrip.ImageScalingSize = new System.Drawing.Size(16 * DeviceDpi / 96, 16 * DeviceDpi / 96);
			_pageModeButton.Size = new System.Drawing.Size(height + spacing * 4, height);
			_pageModeToolStrip.Size = _pageModeButton.Size;
			_pageNumberBox.Width = TextRenderer.MeasureText(Math.Max(9999, _endPageNumber + 1).ToString(), Font).Width + 12;
			foreach (var control in new Control[] { _label3, _totalRecordsLabel, _pageSizeLabel, _pageCountLabel })
				control.Margin = new Padding(spacing, Math.Max(0, (height - control.PreferredSize.Height) / 2) + spacing, spacing, spacing);
			// Narrow hosts put the right-aligned paging groups below Total, without clipping a group.
			var pagingWidth = _pageModeToolStrip.Width + _pageModeToolStrip.Margin.Horizontal;
			if (PageMode == VisualGridPageMode.Paged)
				pagingWidth += _manualPageSizePanel.PreferredSize.Width + _manualPageSizePanel.Margin.Horizontal;
			if (PageMode != VisualGridPageMode.Single)
				pagingWidth += _pageNavigationPanel.PreferredSize.Width + _pageNavigationPanel.Margin.Horizontal;
			var requiredWidth = _totalRecordsPanel.PreferredSize.Width + _totalRecordsPanel.Margin.Horizontal + pagingWidth;
			var separateRow = requiredWidth > _bottomPanel.ClientSize.Width - _bottomPanel.Padding.Horizontal;
			// This setter schedules layout even for an unchanged value; an AutoSize child can queue another parent pass.
			if (_pagingControlsPanel.WrapContents != separateRow)
				_pagingControlsPanel.WrapContents = separateRow;
			var position = new TableLayoutPanelCellPosition(separateRow ? 0 : 1, separateRow ? 1 : 0);
			if (separateRow && _bottomPanel.RowCount == 1) {
				_bottomPanel.RowCount = 2;
				_bottomPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			}
			if (!_bottomPanel.GetCellPosition(_pagingControlsPanel).Equals(position)) {
				_bottomPanel.SetColumnSpan(_pagingControlsPanel, separateRow ? 2 : 1);
				_bottomPanel.SetCellPosition(_pagingControlsPanel, position);
			}
			if (!separateRow && _bottomPanel.RowCount != 1) {
				_bottomPanel.RowCount = 1;
				while (_bottomPanel.RowStyles.Count > 1)
					_bottomPanel.RowStyles.RemoveAt(_bottomPanel.RowStyles.Count - 1);
			}
		}
	}

	private void GridColumnWidthChanged(object sender, ColumnInfoEventArgs args) {
		var column = args.Column.Index;
		if (_sizingColumns || _restoringGridSettings || column < 0 || column >= _columnsBindings.Length)
			return;
		if (_columnLayoutSource != null) {
			ApplySharedColumnGeometry();
			return;
		}
		// A boundary the user is dragging is theirs even while a live refresh is reading; otherwise the next layout puts the width back.
		var dragging = SourceGrid.Cells.Controllers.Resizable.ResizeWidth.IsWidthResizing;
		if (State == VisualState.Loading && (PrepareReadOnlyCellsInBackground || dragging)) {
			_columnResizePending = true;
			_columnWidthOverrides[_columnsBindings[column]] = args.Column.Width;
			args.Column.AutoSizeMode = SourceGrid.AutoSizeMode.None;
			NotifyColumnLayoutChanged();
			return;
		}
		if (State != VisualState.Normal || column >= _columnMinimumWidths.Length)
			return;
		// A user's explicit width takes precedence over both natural sizing and viewport stretching.
		_columnResizePending = true;
		_columnWidthOverrides[_columnsBindings[column]] = args.Column.Width;
		_columnMinimumWidths[column] = args.Column.Width;
		_columnsSizedFromData = true;
		args.Column.AutoSizeMode = SourceGrid.AutoSizeMode.None;
		NotifyColumnLayoutChanged();
	}

	private void UpdateButtonBarVisibility() {
		// A disabled parent does not remove the title or the commands supported by its source.
		if (HeaderVisible && (!string.IsNullOrEmpty(GridTitle) || _crudCapabilities.HasFlag(DataSourceCapabilities.CanCreate)
			|| _crudCapabilities.HasFlag(DataSourceCapabilities.CanDelete) || _crudCapabilities.HasFlag(DataSourceCapabilities.CanSearch)))
			ShowButtonBar();
		else
			HideButtonBar();
	}

	private void ShowButtonBar() {
		_topPanel.Visible = true;
		_layoutPanel.RowStyles[0].SizeType = SizeType.AutoSize;
	}

	private void HideButtonBar() {
		_topPanel.Visible = false;
		_layoutPanel.RowStyles[0].SizeType = SizeType.Absolute;
		_layoutPanel.RowStyles[0].Height = 0;
	}

	private void ShowPageBar() {
		if (!FooterVisible) {
			HidePageBar();
			return;
		}
		_bottomPanel.Visible = true;
		_layoutPanel.RowStyles[2].SizeType = SizeType.AutoSize;
	}

	private void HidePageBar() {
		_bottomPanel.Visible = false;
		_layoutPanel.RowStyles[2].SizeType = SizeType.Absolute;
		_layoutPanel.RowStyles[2].Height = 0;
	}

	private void ApplyColumnHeaderVisibility() {
		if (_grid == null || _grid.RowsCount == 0)
			return;
		_grid.Rows[0].AutoSizeMode = ColumnHeadersVisible ? SourceGrid.AutoSizeMode.Default : SourceGrid.AutoSizeMode.None;
		_grid.Rows.ShowRow(0, ColumnHeadersVisible);
		_grid.Invalidate();
	}

	private void LoadingIndicatorResized(object sender, EventArgs args) {
		// Initial loading and subsequent refreshes share this full-viewport circle and geometry.
		var sideLength = Math.Max(1, Math.Min(_initialLoadingCircle.ClientSize.Width, _initialLoadingCircle.ClientSize.Height) - 1);
		_initialLoadingCircle.InnerCircleRadius = Math.Max(1, sideLength / 5);
		_initialLoadingCircle.OuterCircleRadius = Math.Max(1, sideLength / 3);
		_initialLoadingCircle.SpokeThickness = Math.Max(1, _initialLoadingCircle.InnerCircleRadius / 3);
	}

	private void GridContainerPanel_Resize(object sender, EventArgs args) => RefreshAutoPageSize();

	private void GridViewportChanged(object sender, EventArgs args) {
		if (_grid == null)
			return;

		// Custom scrollbars alter the usable viewport without changing the control's client size. While a boundary is dragged, a scrollbar the
		// new width brings or removes must not refit the columns before the width is the user's: with rows that just fill the grid, a horizontal
		// scrollbar brought a vertical one and the refit put the dragged column back (21 November 2026). The drag's end fits them once.
		if (!_sizingColumns && _columnViewportWidth != _grid.DisplayRectangle.Width && !SourceGrid.Cells.Controllers.Resizable.ResizeWidth.IsWidthResizing)
			StretchColumnsToViewport();
		RefreshAutoPageSize();
	}

	private void GridMetricsChanged(object sender, EventArgs args) {
		if (AutoPageSize)
			RefreshAutoPageSize(true);
	}

	private void SizeBoundColumns() {
		if (ApplySharedColumnGeometry()) {
			SizeBoundRows();
			return;
		}
		if (_grid.ColumnsCount == 0)
			return;
		_sizingColumns = true;
		using (Tools.Scope.ExecuteOnDispose(() => _sizingColumns = false)) {
			_grid.AutoStretchColumnsToFitWidth = false;
			_sizedColumnBindings = _columnsBindings;
			_columnsSizedFromData = _rowToEntityMap.Count > 0;
			_columnMinimumWidths = new int[_grid.ColumnsCount];
			for (var column = 0; column < _grid.ColumnsCount; column++)
				_grid.Columns[column].AutoSizeMode = GetColumnAutoSizeMode(_columnsBindings[column]);
			_grid.AutoSizeCells();
			for (var column = 0; column < _grid.ColumnsCount; column++) {
				if (_columnWidthOverrides.TryGetValue(_columnsBindings[column], out var manual))
					_grid.Columns[column].Width = manual;
				else
					_grid.Columns[column].Width = BoundNaturalWidth(_columnsBindings[column], _grid.Columns[column].Width);
				_columnMinimumWidths[column] = _grid.Columns[column].Width;
			}
			StretchColumnsToViewport();
		}
	}

	// A natural width within the column's declared minimum and maximum, in device pixels.
	private int BoundNaturalWidth(IVisualGridColumn column, int width) {
		if (column.MinimumWidth > 0)
			width = Math.Max(width, LogicalToDeviceUnits(column.MinimumWidth));
		if (column.MaximumWidth > 0)
			width = Math.Min(width, Math.Max(LogicalToDeviceUnits(column.MaximumWidth), LogicalToDeviceUnits(column.MinimumWidth)));
		return width;
	}

	private void StretchColumnsToViewport() {
		if (ApplySharedColumnGeometry())
			return;
		if (_columnMinimumWidths.Length != _grid.ColumnsCount || _sizedColumnBindings != _columnsBindings)
			return;
		var previousSizing = _sizingColumns;
		_sizingColumns = true;
		using (Tools.Scope.ExecuteOnDispose(() => _sizingColumns = previousSizing)) {
			_grid.Columns.SuspendLayout();
			using (Tools.Scope.ExecuteOnDispose(() => _grid.Columns.ResumeLayout())) {
				for (var column = 0; column < _grid.ColumnsCount; column++)
					_grid.Columns[column].Width = _columnMinimumWidths[column];
			}
			_grid.Columns.StretchToFit();
			_columnViewportWidth = _grid.DisplayRectangle.Width;
			NotifyColumnLayoutChanged();
		}
	}

	private ActionScope EnterLoadingIndicator() {
		// An inner operation cannot hide the indicator still owned by its enclosing operation.
		_loadingIndicatorScopes++;
		UpdateLoadingIndicator();
		return Tools.Scope.ExecuteOnDispose(() => {
			_loadingIndicatorScopes--;
			UpdateLoadingIndicator();
		});
	}

	private void UpdateLoadingIndicator() {
		if (IsDisposed || Disposing)
			return;
		if (_awaitingInitialBinding || _loadingIndicatorScopes > 0) {
			_initialLoadingCircle.BringToFront();
			_initialLoadingCircle.StartAnimating();
		} else
			_initialLoadingCircle.StopAnimating();
	}

	/// <summary>Measures the published prepared cells natively, retrying while only their layout inputs change.</summary>
	/// <returns>False when a superseding binding or query interrupted the measurement.</returns>
	private async Task<bool> SettlePreparedGeometryAsync(BindingQuery query, PreparedPage page, int acceptedVersion, bool sizeColumns) {
		bool AcceptedBindingIsCurrent()
			=> !IsDisposed && !Disposing && query.IsCurrent(this) && _bindingVersion == acceptedVersion && PreparedFormatsAreCurrent(query.Columns, page.Formats);

		// A resize or font change invalidates native measurements, not the accepted data.
		// Retry geometry over those same cells; only a changed query may read and prepare another page.
		while (AcceptedBindingIsCurrent()) {
			var sized = sizeColumns ? await SizeBoundColumnsAsync(AcceptedBindingIsCurrent) : await SizeBoundRowsAsync(AcceptedBindingIsCurrent);
			if (sized)
				break;
		}
		if (!AcceptedBindingIsCurrent())
			return false;

		// Publishing prepared rows can change scrollbars while guarded layout events defer stretching.
		if (_columnViewportWidth != _grid.DisplayRectangle.Width)
			StretchColumnsToViewport();
		return true;
	}

	// Measure columns and rows in bounded UI batches, accepting only the captured native layout.
	/// <summary>Measures native column batches while retaining the owner thread's dispatcher.</summary>
	private async Task<bool> SizeBoundColumnsAsync(Func<bool> isCurrent) {
		if (ApplySharedColumnGeometry())
			return await SizeBoundRowsAsync(isCurrent);
		var columns = _columnsBindings;
		var widths = Enumerable.Range(0, _grid.ColumnsCount).Select(column => _grid.Columns[column].Width).ToArray();
		var geometryWidths = (int[])widths.Clone();
		var rowCount = _grid.RowsCount;
		var geometry = GetRowHeightGeometry();
		var headerFont = SourceGrid.Cells.Views.ColumnHeader.Default.Font;
		var minimumWidth = _grid.MinimumWidth;
		var signatures = new VisualGridRowMeasurements.Signature[rowCount];
		bool Current()
			=> isCurrent() && rowCount == _grid.RowsCount && widths.Length == _grid.ColumnsCount
						&& geometry.Equals(GetRowHeightGeometry()) && Equals(headerFont, SourceGrid.Cells.Views.ColumnHeader.Default.Font)
						&& minimumWidth == _grid.MinimumWidth && NativeWidthsAreCurrent(columns, geometryWidths);

		_grid.AutoStretchColumnsToFitWidth = false;
		for (var column = 0; column < columns.Length; column++)
			_grid.Columns[column].AutoSizeMode = GetColumnAutoSizeMode(columns[column]);
		if (!await RunNativeSizingAsync(MeasureColumnsAndRows(), Current)) {
			_sizedColumnBindings = null;
			_columnsSizedFromData = false;
			ResetRowHeightMeasurements();
			return false;
		}
		_sizedColumnBindings = columns;
		_columnsSizedFromData = _rowToEntityMap.Count > 0;
		for (var column = 0; column < widths.Length; column++)
			if (_columnWidthOverrides.TryGetValue(columns[column], out var manual))
				widths[column] = manual;
		_columnMinimumWidths = widths;
		StretchColumnsToViewport();
		RememberRowHeightGeometry(signatures);
		return true;

		IEnumerable<bool> MeasureColumnsAndRows() {
			// Match SourceGrid.AutoSizeCells: descending columns, ascending cells in each column, then descending rows.
			for (var column = columns.Length - 1; column >= 0; column--) {
				if (!_grid.Columns.IsColumnVisible(column))
					continue;
				var width = Math.Max(minimumWidth, LogicalToDeviceUnits(Math.Max(0, columns[column].MinimumWidth)));
				for (var row = 0; row < rowCount; row++) {
					if (_rowDetails?.IsDetailRow(row) == true)
						continue;
					if (_columnWidthOverrides.ContainsKey(columns[column]))
						break;
					width = Math.Max(width, _grid.Columns.MeasureColumnWidth(column, false, row, row));
					yield return true;
				}
				width = _columnWidthOverrides.TryGetValue(columns[column], out var manualWidth) ? manualWidth : BoundNaturalWidth(columns[column], width);
				_grid.Columns.SetWidth(column, width);
				widths[column] = geometryWidths[column] = _grid.Columns[column].Width;
			}
			foreach (var step in MeasureNativeRows(Enumerable.Repeat(true, rowCount).ToArray()))
				yield return step;
			for (var row = 0; row < rowCount; row++) {
				signatures[row] = GetRowHeightSignature(row);
				yield return true;
			}
		}
	}

	// Retain compatible native heights and measure changed rows without monopolizing the dispatcher.
	/// <summary>Measures invalidated native rows and retains successful geometry captures.</summary>
	private async Task<bool> SizeBoundRowsAsync(Func<bool> isCurrent) {
		var rowCount = _grid.RowsCount;
		var columns = _columnsBindings;
		var widths = Enumerable.Range(0, _grid.ColumnsCount).Select(column => _grid.Columns[column].Width).ToArray();
		var geometry = GetRowHeightGeometry();
		var autoSize = AutoSizeRowsOnRefresh;
		var full = autoSize || !_rowMeasurements.Matches(_dataSource, columns, geometry, widths);
		var signatures = new VisualGridRowMeasurements.Signature[rowCount];
		var measure = new bool[rowCount];
		bool Current()
			=> isCurrent() && rowCount == _grid.RowsCount && widths.Length == _grid.ColumnsCount
						&& geometry.Equals(GetRowHeightGeometry()) && autoSize == AutoSizeRowsOnRefresh && NativeWidthsAreCurrent(columns, widths);

		if (!await RunNativeSizingAsync(MeasureRows(), Current))
			return false;
		if (full)
			RememberRowHeightGeometry(signatures);
		else
			_rowMeasurements.UpdateSignatures(signatures, widths);
		return true;

		IEnumerable<bool> MeasureRows() {
			// Signature inspection is part of the bounded native work too; a large page must not
			// monopolize the dispatcher before the existing yielding measurement loop begins.
			for (var row = 0; row < rowCount; row++) {
				var signature = GetRowHeightSignature(row);
				signatures[row] = signature;
				measure[row] = full || _rowMeasurements.RequiresMeasurement(row, signature);
				yield return true;
			}
			foreach (var step in MeasureNativeRows(measure, signatures))
				yield return step;
		}
	}

	/// <summary>Yields after bounded native measurement work for each requested row.</summary>
	/// <remarks>
	/// A row with a supported single-line signature (<see cref="GetRowHeightSignature"/>) draws only the default view's font, padding and border,
	/// so its height follows from its signature alone: each signature is measured once per pass and its other rows take that height. Measuring
	/// every row cell by cell made each expander of a 4,000-row outline take 1.5 s (22 November 2026).
	/// </remarks>
	private IEnumerable<bool> MeasureNativeRows(bool[] measure, VisualGridRowMeasurements.Signature[] signatures = null) {
		var measured = new Dictionary<VisualGridRowMeasurements.Signature, int>();
		for (var row = measure.Length - 1; row >= 0; row--) {
			if (!measure[row] || (_grid.Rows.GetAutoSizeMode(row) & SourceGrid.AutoSizeMode.EnableAutoSize) != SourceGrid.AutoSizeMode.EnableAutoSize
				|| !_grid.Rows.IsRowVisible(row))
				continue;
			var signature = signatures?[row] ?? GetRowHeightSignature(row);
			if (signature.ImageHeight >= 0 && measured.TryGetValue(signature, out var known)) {
				_grid.Rows.SetHeight(row, known);
				yield return true;
				continue;
			}
			var height = _grid.MinimumHeight;
			if ((_grid.Rows.GetAutoSizeMode(row) & SourceGrid.AutoSizeMode.MinimumSize) != SourceGrid.AutoSizeMode.MinimumSize) {
				for (var column = 0; column < _grid.ColumnsCount; column++) {
					height = Math.Max(height, _grid.Rows.MeasureRowHeight(row, false, column, column));
					yield return true;
				}
			}
			_grid.Rows.SetHeight(row, height);
			if (signature.ImageHeight >= 0)
				measured[signature] = height;
			yield return true;
		}
	}

	/// <summary>Checks whether awaited work still matches the current native column widths.</summary>
	private bool NativeWidthsAreCurrent(IVisualGridColumn[] columns, int[] expected) {
		for (var column = 0; column < expected.Length; column++) {
			var actual = _grid.Columns[column].Width;
			if (actual == expected[column])
				continue;
			if (!_columnWidthOverrides.TryGetValue(columns[column], out var manual) || actual != manual)
				return false;
			expected[column] = actual;
		}
		return true;
	}

	/// <summary>Runs native measurement batches until completion or a superseding binding.</summary>
	private async Task<bool> RunNativeSizingAsync(IEnumerable<bool> steps, Func<bool> isCurrent) {
		using (var timer = new System.Windows.Forms.Timer { Interval = 1 }) {
			TaskCompletionSource resume = null;
			timer.Tick += (_, _) => {
				timer.Stop();
				resume?.TrySetResult();
			};
			void DisposedWhileSizing(object sender, EventArgs args) => resume?.TrySetResult();

			Disposed += DisposedWhileSizing;
			using (Tools.Scope.ExecuteOnDispose(() => Disposed -= DisposedWhileSizing)) {
				using (var work = steps.GetEnumerator()) {
					while (!IsDisposed && !Disposing && isCurrent()) {
						var more = true;
						{
							var previousSizing = _sizingColumns;
							_sizingColumns = true;
							using (Tools.Scope.ExecuteOnDispose(() => _sizingColumns = previousSizing)) {
								_grid.SuspendLayout();
								_grid.Rows.SuspendLayout();
								_grid.Columns.SuspendLayout();
								using (Tools.Scope.ExecuteOnDispose(() => {
									_grid.Rows.ResumeLayout();
									_grid.Columns.ResumeLayout();
									_grid.ResumeLayout(false);
								})) {
									var started = Stopwatch.GetTimestamp();
									var count = 0;
									do {
										more = work.MoveNext();
									} while (more && ++count < 4096 && Stopwatch.GetElapsedTime(started).TotalMilliseconds < 6);
								}
							}
						}
						if (IsDisposed || Disposing || !isCurrent())
							return false;
						if (!more)
							return true;
						// WM_TIMER is delivered after pending input/paint, unlike an uninterrupted chain of posted continuations.
						resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
						timer.Start();
						await resume.Task;
						resume = null;
					}
					return false;
				}
			}
		}
	}

	// Remeasure changed content while retaining actual native heights for compatible rows.
	private void SizeBoundRows() {
		if (AutoSizeRowsOnRefresh || !_rowMeasurements.Matches(_dataSource, _columnsBindings, GetRowHeightGeometry(), _grid)) {
			// Every row, measuring each single-line signature once (see MeasureNativeRows).
			_grid.Rows.SuspendLayout();
			using (Tools.Scope.ExecuteOnDispose(() => _grid.Rows.ResumeLayout()))
				foreach (var _ in MeasureNativeRows(Enumerable.Repeat(true, _grid.RowsCount).ToArray())) {
				}
			RememberRowHeightGeometry();
			return;
		}
		// Redim preserves existing RowInfo instances. Keep their actual measured heights, rather than imposing a fixed height.
		var signatures = new VisualGridRowMeasurements.Signature[_grid.RowsCount];
		for (var row = 0; row < _grid.RowsCount; row++) {
			var signature = GetRowHeightSignature(row);
			if (_rowMeasurements.RequiresMeasurement(row, signature))
				_grid.Rows.AutoSizeRow(row, false, 0, _grid.ColumnsCount - 1);
			signatures[row] = signature;
		}
		_rowMeasurements.UpdateSignatures(signatures);
	}

	// Accept only completed native measurements, or clear them when reuse is disabled.
	/// <summary>Retains geometry and content signatures after successful row sizing.</summary>
	private void RememberRowHeightGeometry(VisualGridRowMeasurements.Signature[] signatures = null) {
		if (AutoSizeRowsOnRefresh) {
			ResetRowHeightMeasurements();
			return;
		}
		_rowMeasurements.Remember(_dataSource, _columnsBindings, GetRowHeightGeometry(),
			Enumerable.Range(0, _grid.ColumnsCount).Select(column => _grid.Columns[column].Width).ToArray(),
			signatures ?? Enumerable.Range(0, _grid.RowsCount).Select(GetRowHeightSignature).ToArray()
		);
	}

	// Release borrowed source and visual references when the binding or control lifetime ends.
	/// <summary>Invalidates retained native row geometry and content signatures.</summary>
	private void ResetRowHeightMeasurements() => _rowMeasurements.Clear();

	// Capture layout inputs on the native control's owning UI thread.
	/// <summary>Captures the native metrics used to validate row-height reuse.</summary>
	private VisualGridRowMeasurements.Geometry GetRowHeightGeometry() {
		var view = SourceGrid.Cells.Views.Cell.Default;
		return new VisualGridRowMeasurements.Geometry(_grid.Font, view.Font, _grid.DeviceDpi, _grid.MinimumHeight, view.Padding,
			view.Border?.GetExtent(System.Drawing.SizeF.Empty) ?? System.Drawing.SizeF.Empty, view.ElementText, view.ElementImage, view.ElementsDrawMode
		);
	}

	// Restrict reuse to native single-line text/image cells with the default measurement behavior.
	/// <summary>Summarizes visible cell content that can reuse its last measured height.</summary>
	private VisualGridRowMeasurements.Signature GetRowHeightSignature(int row) {
		if (_rowDetails?.IsDetailRow(row) == true)
			return new VisualGridRowMeasurements.Signature(0, false);
		var view = SourceGrid.Cells.Views.Cell.Default;
		if (row == 0 || view.WordWrap || view.ElementText?.GetType() != typeof(Sphere10.Framework.Windows.Forms.Drawing.VisualElements.TextGDI)
			|| view.ElementImage?.GetType() != typeof(Sphere10.Framework.Windows.Forms.Drawing.VisualElements.Image))
			return new VisualGridRowMeasurements.Signature(-1, false);
		var imageHeight = 0;
		var hasText = false;
		for (var column = 0; column < _grid.ColumnsCount; column++) {
			if (_columnsBindings[column].DisplayType is not (VisualGridCellDisplayType.Text or VisualGridCellDisplayType.Image)
				|| _grid[row, column] is not Cell cell || cell.GetType() != typeof(Cell) && cell is not VisualGridPreparedCell
				|| cell.View != GetColumnCellView(_columnsBindings[column]) && cell.View is not StyledCellView && cell.View is not VisualGridDetailCellView || cell.Editor is SourceGrid.Cells.Editors.EditorControlBase)
				return new VisualGridRowMeasurements.Signature(-1, false);
			var cellView = cell.View is VisualGridDetailCellView detailView ? detailView.Prototype : (SourceGrid.Cells.Views.Cell)cell.View;
			if (cellView != view && (cellView.WordWrap || !Equals(cellView.Font, view.Font) || !ReferenceEquals(cellView.Border, view.Border)
				|| cellView.Padding.Left != view.Padding.Left || cellView.Padding.Right != view.Padding.Right
				|| cellView.Padding.Top != view.Padding.Top || cellView.Padding.Bottom != view.Padding.Bottom
				|| cellView.ElementsDrawMode != view.ElementsDrawMode || cellView.ElementText != view.ElementText))
				return new VisualGridRowMeasurements.Signature(-1, false);
			var text = new CellContext(_grid, new Position(row, column)).DisplayText;
			if (text.AsSpan().IndexOfAny("\r\n\u0085\u2028\u2029") >= 0)
				return new VisualGridRowMeasurements.Signature(-1, false);
			hasText |= !string.IsNullOrEmpty(text);
			imageHeight = Math.Max(imageHeight, cell.Image?.Height ?? 0);
		}
		return new VisualGridRowMeasurements.Signature(imageHeight, hasText);
	}

	/// <summary>Suspends the native table's layout until the returned scope is disposed.</summary>
	private ActionScope SuspendGridLayout() {
		_grid.SuspendLayout();
		return Tools.Scope.ExecuteOnDispose(() => _grid.ResumeLayout());
	}

	#endregion
}
