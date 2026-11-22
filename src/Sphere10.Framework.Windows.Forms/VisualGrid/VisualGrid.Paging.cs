// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms;

// Owns search, sort, page navigation and automatic page capacity.
// Query changes refresh through Binding, while Preferences restores accepted user choices.
public partial class VisualGrid {
	#region Fields

	private const int _defaultPageSize = 100;

	private int _sortColumnIndex;

	private string _sortColumnName;

	private SortDirection _sortDirection;

	private int _pageNumber;

	private int _endPageNumber;

	private int _pageSize;

	private int _manualPageSize;

	private VisualGridPageMode _pageMode;

	private int _totalRecords;

	private string _searchText;

	private bool _updatingPageNumberText;

	private bool _updatingPageSizeDisplay;

	private bool _updatingPageModeControl;

	#endregion

	#region Properties

	[Description("Selects manual paging, viewport-fitted paging, or all matching rows in one scrollable page.")]
	[Category("Appearance")]
	[DefaultValue(VisualGridPageMode.Paged)]
	public VisualGridPageMode PageMode {
		get => _pageMode;

		set {
			Guard.Argument(Enum.IsDefined(value), nameof(value), "Select a supported CRUD grid page mode.");
			if (_pageMode == value)
				return;
			using (EnterVisualState(VisualState.Loading)) {
				_pageMode = value;
				_pageNumber = 0;
				SetVisiblePageNumberText(0);
				OrganizeLayout();
			}
			PublishGridPreferences();
			RefreshAutoPageSize(true);
		}
	}

	[Description("Compatibility alias for PageMode. True selects Auto; false selects Paged and restores the remembered manual size.")]
	[Category("Appearance")]
	[DefaultValue(false)]
	public bool AutoPageSize {
		get => PageMode == VisualGridPageMode.Auto;

		set => PageMode = value ? VisualGridPageMode.Auto : VisualGridPageMode.Paged;
	}

	/// <summary>The total matching records in the last accepted query, across every page.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int TotalRecords => _totalRecords;

	/// <summary>The current source search value. Capture on the owning UI thread before preparing a worker query.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string CurrentSearchText => _searchText;

	#endregion

	#region Methods

	/// <summary>Changes the ordinary search field on its UI owner and awaits the same source-backed first-page refresh as a typed search.</summary>
	public async Task SetSearchTextAsync(string text) {
		Guard.Ensure(!InvokeRequired, "Change the grid search on its owning UI thread.");
		text ??= string.Empty;
		_searchTextBox.TextChanged -= SearchTextBox_TextChanged;
		using (Tools.Scope.ExecuteOnDispose(() => _searchTextBox.TextChanged += SearchTextBox_TextChanged)) {
			_searchText = text;
			_searchTextBox.Text = text;
		}
		await ChangePageNumberAsync(0);
	}

	/// <summary>Locates an exact matching entity across the current filtered pages, navigates to its page and selects its accepted visible replacement.</summary>
	/// <remarks>The predicate runs on detached values on a worker. This does not enumerate child grids or collect resource data. A superseding query/publication returns false without selecting stale evidence.</remarks>
	public async Task<bool> LocateEntityAsync(Func<object, bool> matches, bool clearSearch = false, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(matches, nameof(matches));
		Guard.Ensure(!InvokeRequired, "Locate a grid entity on its owning UI thread.");
		cancellationToken.ThrowIfCancellationRequested();
		if (clearSearch) {
			await SetSearchTextAsync(string.Empty);
			if (_searchText.Length != 0)
				return false;
		}
		var source = _dataSource;
		var publicationVersion = _dataPublicationVersion;
		var publicationIdentity = PublicationIdentity;
		var publication = publicationIdentity?.Invoke();
		var query = (_searchText, _sortColumnName, _sortDirection, _pageSize, _pageNumber);
		bool QuerySuperseded()
			=> IsDisposed || Disposing || IsSourceBindingPending || source != _dataSource || publicationVersion != _dataPublicationVersion || !IsSourcePublicationCurrent(publicationIdentity, publication);
		bool PageSuperseded(int pageNumber)
			=> QuerySuperseded() || _pageNumber != pageNumber
				|| (query._searchText, query._sortColumnName, query._sortDirection, query._pageSize) != (_searchText, _sortColumnName, _sortDirection, _pageSize);

		// Find the match across the complete filtered result on a worker.
		if (source == null || IsSourceBindingPending || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead) || !IsSourcePublicationCurrent(publicationIdentity, publication))
			return false;
		var rows = await ReadMatchingEntitiesAsync(source, _dataSourceItemType, query._searchText, query._sortColumnName, query._sortDirection, cancellationToken);
		var index = await Task.Run(() => {
			for (var position = 0; position < rows.Length; position++) {
				cancellationToken.ThrowIfCancellationRequested();
				if (matches(rows.GetValue(position)))
					return position;
			}
			return -1;
		}, cancellationToken);
		if (index < 0 || QuerySuperseded() || query != (_searchText, _sortColumnName, _sortDirection, _pageSize, _pageNumber))
			return false;

		// Navigate to its page, then select the accepted visible replacement from that same publication.
		var page = PageMode == VisualGridPageMode.Single ? 0 : index / Math.Max(1, _pageSize);
		await ChangePageNumberAsync(page);
		cancellationToken.ThrowIfCancellationRequested();
		if (PageSuperseded(page))
			return false;
		var binding = _bindingVersion;
		var visible = VisibleEntities;
		var selected = await Task.Run(() => visible.FirstOrDefault(matches), cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		if (binding != _bindingVersion || PageSuperseded(page) || selected == null)
			return false;
		SelectedEntity = selected;
		return true;
	}

	internal async void _grid_SortColumnPressed(int col) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (!_crudCapabilities.HasFlag(DataSourceCapabilities.CanSort) || _columnsBindings == null || col < 0 || col >= _columnsBindings.Length || string.IsNullOrEmpty(_columnsBindings[col].SortName))
				return;

			_sortDirection = _sortColumnName != null && _sortColumnIndex == col
				? _sortDirection switch {
					SortDirection.Ascending => SortDirection.Descending,
					SortDirection.Descending => SortDirection.None,
					_ => SortDirection.Ascending
				}
				: SortDirection.Ascending;
			_sortColumnIndex = col;
			_sortColumnName = _sortDirection == SortDirection.None ? null : _columnsBindings[col].SortName;
			PublishGridPreferences();
			await RefreshGrid();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private void UpdatePageModeControl() {
		_updatingPageModeControl = true;
		using (Tools.Scope.ExecuteOnDispose(() => _updatingPageModeControl = false)) {
			_pageModeButton.SelectedItem = _pageMode;
			_pageModeButton.ToolTipText = _pageMode switch {
				VisualGridPageMode.Single => "Single page: show every matching record with vertical scrolling.",
				VisualGridPageMode.Auto => "Automatic paging: fit complete rows to the table's current height.",
				_ => "Manual paging: choose the number of matching records shown on each page."
			};
			_pageModeButton.AccessibleDescription = _pageModeButton.ToolTipText;
		}
	}

	private void PageModeSelectedIndexChanged(object sender, EventArgs args) {
		if (!_updatingPageModeControl && _pageModeButton.SelectedItem is VisualGridPageMode mode)
			PageMode = mode;
	}

	private async void PageSizeUpDown_ValueChanged(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (_restoringGridSettings || _updatingPageSizeDisplay || PageMode != VisualGridPageMode.Paged || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanPage))
				return;
			_manualPageSize = _pageSize = (int)_pageSizeUpDown.Value;
			PublishGridPreferences();
			await RefreshGrid();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void SearchTextBox_TextChanged(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			_searchText = _searchTextBox.Text;
			await ChangePageNumberAsync(0);
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void FirstPageButton_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (!CanInteractWithCommittedRows)
				return;

			await ChangePageNumberAsync(0);
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void PreviousPageButton_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (!CanInteractWithCommittedRows)
				return;

			await ChangePageNumberAsync(_pageNumber - 1);
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void NextPageButton_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (!CanInteractWithCommittedRows)
				return;

			await ChangePageNumberAsync(_pageNumber + 1);
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void LastPageButton_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (!CanInteractWithCommittedRows)
				return;

			await ChangePageNumberAsync(_endPageNumber);
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void PageNumberBox_ValueChanged(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (_updatingPageNumberText || State == VisualState.Selecting)
				return;

			await ChangePageNumberAsync(GetVisiblePageNumberText());
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	// Paging
	// TODO: SHould be property
	private int GetVisiblePageNumberText() => int.Parse(_pageNumberBox.Text) - 1;

	// TODO: Should be SetPageNumberAsync
	private async Task ChangePageNumberAsync(int pageNumber) {
		_pageNumber = PageMode == VisualGridPageMode.Single || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanPage)
			? 0 : pageNumber.ClipTo(0, _endPageNumber);
		SetVisiblePageNumberText(_pageNumber);
		PublishGridPreferences();
		await RefreshGrid();
	}

	private void SetVisiblePageNumberText(int number) {
		_updatingPageNumberText = true;
		using (Tools.Scope.ExecuteOnDispose(() => _updatingPageNumberText = false)) {
			_pageNumberBox.Text = (number + 1).ToString();
		}
	}

	private int CalculateAutoPageSize(AutoPageSizing sizing = null) {
		// Native row, header and viewport measurements already use the current window's device pixels.
		// Scale only the unmeasured fallback; a tall image must not inflate the budget for every ordinary row.
		var fallbackHeight = Math.Max(_grid.MinimumHeight, (int)Math.Round(_defaultRowHeight * _grid.DeviceDpi / 96.0));
		var headerHeight = _grid.Rows.Count > 0 ? _grid.Rows[0].Height : fallbackHeight;
		// Fit the ordinary rows to the viewport they have with every detail closed. An expanded detail makes the grid scroll rather than shrink its
		// page, including through the horizontal scrollbar that the vertical one can bring in (owner, 19 November 2026).
		// A detail sized to its rows would grow with every page it fits, so it fits the height its parent allows instead of its own viewport.
		var viewport = FittedDetailHeight > 0 ? FittedDetailHeight - DetailChromeHeight : _grid.UnscrolledDisplayHeight;
		var available = Math.Max(0, viewport - headerHeight);
		var fitted = 0;
		var used = 0;
		var nextHeight = int.MaxValue;
		for (var row = 1; row < _grid.Rows.Count; row++) {
			if (_rowDetails?.IsDetailRow(row) == true)
				continue;
			var height = Math.Max(1, _grid.Rows[row].Height);
			if (height > available - used)
				break;
			used += height;
			fitted++;
			nextHeight = Math.Min(nextHeight, height);
		}
		var overflow = fitted < _rowToEntityMap.Count;
		var capacity = overflow ? Math.Max(1, fitted)
			: Math.Max(1, fitted + (available - used) / (nextHeight == int.MaxValue ? Math.Max(1, fallbackHeight) : nextHeight));
		// A short final result has no additional rows to fill the viewport. Do not move its source offset by probing indefinitely.
		if (!overflow && _boundDataSource == _dataSource && _boundColumns == _columnsBindings && _rowToEntityMap.Count < _pageSize)
			capacity = Math.Min(capacity, _pageSize);
		if (sizing != null) {
			// A probe's temporary vertical scrollbar must not reset its own fitting boundary.
			var query = (_dataSource, _dataPublicationVersion, _columnsBindings, _searchText, _pageNumber, _sortColumnName, _sortDirection,
				_grid.ClientSize, Enumerable.Range(0, _grid.ColumnsCount).Sum(column => _grid.Columns[column].Width), headerHeight, GetRowHeightGeometry());
			if (!Equals(sizing.Query, query)) {
				sizing.Query = query;
				sizing.MaximumPageSize = int.MaxValue;
			}
			// Probing can discover a tall next row. Retain that fitting boundary when the shorter page is rebound.
			// A capacity change shifts nonzero page offsets, so do not assume arbitrary row heights are monotonic.
			if (overflow)
				sizing.MaximumPageSize = Math.Min(sizing.MaximumPageSize, capacity);
			capacity = Math.Min(capacity, sizing.MaximumPageSize);
		}
		return capacity;
	}

	private void UpdatePageSizeDisplay() {
		// The editor is visible only in Paged mode and always retains its manual preference.
		var value = _manualPageSize;
		_updatingPageSizeDisplay = true;
		using (Tools.Scope.ExecuteOnDispose(() => _updatingPageSizeDisplay = false)) {
			_pageSizeUpDown.Value = Math.Max((int)_pageSizeUpDown.Minimum, Math.Min(value, (int)_pageSizeUpDown.Maximum));
		}
	}

	private async void RefreshAutoPageSize(bool forceRefresh = false) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (_grid == null || _dataSource == null || IsDisposed || Disposing || DesignMode || Tools.Runtime.IsDesignMode)
				return;
			if (_crudCapabilities.HasFlag(DataSourceCapabilities.CanPage) && AutoPageSize) {
				var pageSize = CalculateAutoPageSize();
				if (_pageSize == pageSize && !forceRefresh)
					return;
				_pageSize = pageSize;
				UpdatePageSizeDisplay();
			} else if (!forceRefresh)
				return;

			// A pending read detects the new page size and binds again when its result arrives.
			if (State == VisualState.Normal || _awaitingInitialBinding && !_refreshing)
				await RefreshGrid();
		});
		if (error != null) {
			if (!IsDisposed && !Disposing)
				await ExceptionDialog.ShowAsync(this, error);
		}
	}

	#endregion

	#region Inner Types

	private sealed class AutoPageSizing {
		public object Query { get; set; }

		public int MaximumPageSize { get; set; } = int.MaxValue;
	}

	#endregion
}
