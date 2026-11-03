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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Displays and edits source-backed entities with native selection, paging and user preferences.</summary>
public partial class VisualGrid : UserControlEx, IVisualGrid {
	// Events
	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, object> EntitySelected;

	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, object> EntityDeselected;

	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, object> EntityCreated;

	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, object> EntityUpdated;

	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, object> EntityDeleted;

	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid, CrudEntityPropertyChangedEventArgs> EntityPropertyChanged;

	/// <summary>Raised on the owning UI thread after the current filtered query has been bound, including empty results.</summary>
	[Category("Behavior")]
	public event EventHandler DataBindingComplete;

	// Constants
	private const int _defaultRowHeight = 24;
	private const int _defaultPageSize = 100;

	// Fields
	private IDataSource<object> _dataSource;
	private CrudEntityEditorAdapter _entityEditorAdapter;
	private Type _entityEditorType;
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
	private Type _dataSourceItemType = typeof(object);
	private string _gridTitle;
	private DataSourceCapabilities _crudCapabilities;
	private IVisualGridColumn[] _columnsBindings;
	private IDictionary<int, object> _rowToEntityMap;
	private ILookup<object, int> _entityToRowLookup;
	private object _selectedEntity;
	private bool _allowCellEditing;
	private bool _leftClickToDeselect;
	private bool _selectOnMouseUp;
	private int _selectedRowOnMouseDown = -1;
	private Position _lastClickedCell = Position.Empty;
	private readonly SourceGrid.Cells.Controllers.MouseSelection _mouseSelectionController = new();
	private readonly Throttle _refreshThrottle;
	private Task? _refreshTask;
	private bool _updatingPageNumberText;
	private bool _updatingPageSizeDisplay;
	private bool _updatingPageModeControl;
	private bool _layingOutPageBar;
	private bool _sizingColumns;
	private bool _columnsSizedFromData;
	private IVisualGridColumn[] _sizedColumnBindings;
	private int[] _columnMinimumWidths = Array.Empty<int>();
	private int _columnViewportWidth;
	private readonly Dictionary<IVisualGridColumn, int> _columnWidthOverrides = new();
	private readonly Dictionary<IVisualGridColumn, SourceGrid.Cells.Views.Cell> _imageCellViews = new();
	private readonly AsyncSynchronizedGate _bindingGate = new();
	private bool _refreshing;
	private bool _awaitingInitialBinding;
	private int _loadingIndicatorScopes;
	private int _bindingVersion;
	private IDataSource<object> _boundDataSource;
	private IVisualGridColumn[] _boundColumns;
	private int _sourceAssignmentVersion;
	private int _dataPublicationVersion;
	private readonly VisualGridRowMeasurements _rowMeasurements = new();

	// Constructors
	public VisualGrid() {
		State = VisualState.Normal;
		using (EnterVisualState(VisualState.Loading)) {
			InitializeComponent();
			_pageModeButton.ItemTextProvider = mode => (VisualGridPageMode)mode == VisualGridPageMode.Paged ? "Manual" : mode.ToString();
			_pageModeButton.SetItems(new object[] { VisualGridPageMode.Single, VisualGridPageMode.Auto, VisualGridPageMode.Paged });
			Disposed += (_, _) => ResetRowHeightMeasurements();
			_grid.MouseDown += Grid_MouseDown;
			_grid.KeyDown += Grid_KeyDown;
			_grid.Columns.ColumnWidthChanged += GridColumnWidthChanged;
			_grid.MouseUp += GridPreferenceResizeCompleted;
			var preferenceEvents = new SourceGrid.Cells.Controllers.CustomEvents();
			preferenceEvents.EditEnded += GridPreferenceEditEnded;
			_grid.Controller.AddController(preferenceEvents);
			// Keep SourceGrid's controller order while giving this grid independent mouse-selection settings.
			_grid.Controller.RemoveController(SourceGrid.Cells.Controllers.MouseSelection.Default);
			_grid.Controller.RemoveController(SourceGrid.Cells.Controllers.CellEventDispatcher.Default);
			_grid.Controller.AddController(_mouseSelectionController);
			_grid.Controller.AddController(SourceGrid.Cells.Controllers.CellEventDispatcher.Default);
			//BorderStyle = BorderStyle.None;
			_selectedEntity = null;
			_dataSource = null;
			_sortColumnIndex = 0;
			_sortColumnName = null;
			_sortDirection = SortDirection.Ascending;
			_pageSize = _defaultPageSize;
			_manualPageSize = _defaultPageSize;
			_pageSizeUpDown.Minimum = 1;
			_pageSizeUpDown.Maximum = 9999;
			_pageSizeUpDown.Value = _pageSize;
			_pageMode = VisualGridPageMode.Paged;
			_pageNumber = 0;
			_endPageNumber = 0;
			_totalRecords = 0;
			_rowToEntityMap = new Dictionary<int, object>();
			CalculateEntityToRowLookup();
			_gridTitle = string.Empty;
			_crudCapabilities = DataSourceCapabilities.Default;
			_columnsBindings = new IVisualGridColumn[0];
			_entityEditorAdapter = new CrudEntityEditorAdapter<object>();
			_entityEditorType = typeof(DefaultCrudEntityEditor);
			_gridContainerPanel.Resize += new EventHandler(GridContainerPanel_Resize);
			LeftClickToDeselect = true;
			RightClickForContextMenu = true;
			UseEntityReferenceForLookup = false;
			AutoSelectOnCreate = false;
			_refreshThrottle = new Throttle(TimeSpan.FromMilliseconds(250));
			_grid.MinimumHeight = _defaultRowHeight;
			OrganizeLayout();
		}
		if (!Tools.Runtime.IsDesignMode) {
			_awaitingInitialBinding = true;
			State = VisualState.Loading;
			_layoutPanel.Visible = false;
			UpdateLoadingIndicator();
		}
	}

	// Properties
	[Description("Allows editing a cell directly. With LeftClickToDeselect enabled, double-click the cell or press F2 to edit; otherwise a single click edits.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool AllowCellEditing {
		get {
			return _allowCellEditing;
		}

		set {
			if (_allowCellEditing == value)
				return;
			_allowCellEditing = value;
			UpdateCellInteraction();
		}
	}

	[Description("A single click toggles row selection. When AllowCellEditing is enabled, double-click an editable cell or press F2 to edit without deselecting the row.")]
	[Category("Behavior")]
	[DefaultValue(true)]
	public bool LeftClickToDeselect {
		get {
			return _leftClickToDeselect;
		}

		set {
			if (_leftClickToDeselect == value)
				return;
			_leftClickToDeselect = value;
			UpdateCellInteraction();
		}
	}

	[Description("When clicking a selected row this will deselect that row")]
	[Category("Behavior")]
	[DefaultValue(true)]
	public bool RightClickForContextMenu {
		get; set;
	}

	/// <summary>The optional native menu shown only for a right-click on a column header; its supplying container owns it.</summary>
	[Category("Behavior")]
	[DefaultValue(null)]
	[Description("The menu shown when a column header is right-clicked. Row commands use their separate context menu.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public ContextMenuStrip HeaderContextMenuStrip { get; set; }

	[Description("When selecting a row, selection occurs on mouse up (as opposed to default behavior of mouse down)")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool SelectOnMouseUp {
		get {
			return _selectOnMouseUp;
		}

		set {
			if (_selectOnMouseUp == value)
				return;
			_selectOnMouseUp = value;
			UpdateCellInteraction();
		}
	}

	[Description("When a new entity is created successfully this will select that entity.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool AutoSelectOnCreate {
		get; set;
	}

	[Description("Selects manual paging, viewport-fitted paging, or all matching rows in one scrollable page.")]
	[Category("Appearance")]
	[DefaultValue(VisualGridPageMode.Paged)]
	public VisualGridPageMode PageMode {
		get {
			return _pageMode;
		}

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
			NotifyPreferencesChanged();
			RefreshAutoPageSize(true);
		}
	}

	[Description("Compatibility alias for PageMode. True selects Auto; false selects Paged and restores the remembered manual size.")]
	[Category("Appearance")]
	[DefaultValue(false)]
	public bool AutoPageSize {
		get {
			return PageMode == VisualGridPageMode.Auto;
		}

		set {
			PageMode = value ? VisualGridPageMode.Auto : VisualGridPageMode.Paged;
		}
	}

	[Description("Uses reference equality (in place of standard equality) when programatically selecting an entity.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool UseEntityReferenceForLookup {
		get; set;
	}

	[Description("Refreshes entire grid when a row is updated (will only refresh row when false)")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool RefreshEntireGridOnUpdate {
		get; set;
	}

	[Description("Refresh entire grid when a row is deleted (will only refresh row when false)")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool RefreshEntireGridOnDelete {
		get; set;
	}

	[Description("Refreshes immediately without a loading animation or disabling the grid. Logical refresh admission and stale-query protection remain active.")]
	[Category("Appearance")]
	[DefaultValue(false)]
	public bool SuppressLoadingIndicator {
		get; set;
	}

	[Description("Measures row heights on every refresh. Disable for frequently refreshed single-line, read-only text/image tables to retain measured heights; new rows and changes to layout, fonts or image heights are still measured. Multiline, wrapped, editable and custom cells always measure normally.")]
	[Category("Appearance")]
	[DefaultValue(true)]
	public bool AutoSizeRowsOnRefresh { get; set; } = true;

	[Description("Prepares read-only Text/Image pages on a worker, including source reads, enumeration, column delegates and display conversion. Opt in only when those operations and borrowed images are safe off the UI thread. Editable or other cell types retain their normal UI-thread binding.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool PrepareReadOnlyCellsInBackground {
		get; set;
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object SelectedEntity {
		get {
			return _selectedEntity;
		}
		set {
			if (value == null && _selectedEntity != null) {
				RaiseEntityDeselectedEvent(_selectedEntity);
				_selectedEntity = null;
				HighlightSelectedEntity();
				NotifyStateChanged();
				return;
			}

			if (value == _selectedEntity)
				return;

			if (_selectedEntity != null)
				RaiseEntityDeselectedEvent(_selectedEntity);

			_selectedEntity = value;
			HighlightSelectedEntity();

			RaiseEntitySelectedEvent(_selectedEntity);
			NotifyStateChanged();
		}
	}

	// Public Methods
	/// <summary>Binds a typed source and restores its applicable paging, layout and preferences.</summary>
	public Task SetDataSource<TEntity>(IDataSource<TEntity> dataSource) {
		return SetDataSource(dataSource, DataSourceCapabilities.Default);
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Type EntityEditorDisplay {
		get; set;
	}

	[Category("Appearance")]
	[DefaultValue("")]
	public string GridTitle {
		get {
			return _gridTitle;
		}
		set {
			if (IsDisposed || Disposing)
				return;
			_gridTitle = value ?? string.Empty;
			// Appearance properties are usable during construction and on the designer's owning thread.
			if (IsHandleCreated && InvokeRequired) {
				try {
					BeginInvoke(new Action(() => {
						if (!IsDisposed && !Disposing) {
							_titleLabel.Text = _gridTitle;
							UpdateButtonBarVisibility();
						}
					}));
				} catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated) {
					// A closing native surface no longer needs the queued appearance update.
				}
				return;
			}
			_titleLabel.Text = _gridTitle;
			UpdateButtonBarVisibility();
		}
	}

	[Description("Alignment of the grid title within its title area.")]
	[Category("Appearance")]
	[DefaultValue(System.Drawing.ContentAlignment.MiddleCenter)]
	public System.Drawing.ContentAlignment GridTitleAlignment {
		get {
			return _titleLabel.TextAlign;
		}

		set {
			Guard.Argument(Enum.IsDefined(value), nameof(value), "Select a supported title alignment.");
			_titleLabel.TextAlign = value;
		}
	}

	[Category("Behavior")]
	[DefaultValue(DataSourceCapabilities.Default)]
	public DataSourceCapabilities Capabilities {
		get {
			return _crudCapabilities;
		}
		set {
			_crudCapabilities = value;
			UpdateCellInteraction();
			OrganizeLayout();
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IEnumerable<IVisualGridColumn> GridBindings {
		get {
			return _columnsBindings;
		}
		set {
			var bindings = (value ?? Enumerable.Empty<IVisualGridColumn>()).ToArray();
			if (_preferences != null)
				ValidatePreferenceBindings(bindings);
			bindings = OrderColumnBindings(bindings, _preferences?.Columns);
			_columnsBindings = bindings;

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

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IEnumerable<object> VisibleEntities {
		get {
			return _rowToEntityMap.Keys.Select(row => _rowToEntityMap[row]);
		}
	}

	/// <summary>The total matching records in the last accepted query, across every page.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int TotalRecords {
		get {
			return _totalRecords;
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IDictionary<string, CrudReferenceBinding> ReferenceBindings { get; } = new Dictionary<string, CrudReferenceBinding>(StringComparer.Ordinal);

	internal object SelectedEntityDirect {
		get {
			return _selectedEntity;
		}

		set {
			_selectedEntity = value;
			UpdateDeleteButtonVisibility();
		}
	}

	internal Func<object, object> ResolvePublishedEntity {
		get; set;
	}

	private VisualState State {
		get; set;
	}

	// A suppressed background read keeps the last committed view usable. Internal selection/commit
	// scopes remain noninteractive; logical refresh admission is independent of native row input.
	private bool CanInteractWithCommittedRows {
		get {
			return State == VisualState.Normal
					|| State == VisualState.Loading && _refreshing && SuppressLoadingIndicator && !_awaitingInitialBinding
					&& _boundDataSource == _dataSource && _boundColumns == _columnsBindings && _crudCapabilities.HasFlag(DataSourceCapabilities.CanRead);
		}
	}

	/// <summary>Binds a typed source and restores its applicable paging, layout and preferences.</summary>
	public async Task SetDataSource<TEntity>(IDataSource<TEntity> dataSource, DataSourceCapabilities capabilities) {
		var assignmentVersion = ++_sourceAssignmentVersion;
		var projected = new ProjectedDataSource<TEntity, object>(dataSource, entity => entity, entity => (TEntity)entity);
		var available = await projected.CapabilitiesAsync;
		if (IsDisposed || Disposing || assignmentVersion != _sourceAssignmentVersion)
			return;
		_dataSource = projected;
		_dataSourceItemType = typeof(TEntity);
		ResolvePublishedEntity = null;
		ResetRowHeightMeasurements();
		_crudCapabilities = available & capabilities;
		RestoreColumnPreferences(_preferences?.Columns);
		UpdateCellInteraction();
		OrganizeLayout();
	}

	/// <summary>Releases the current source binding and invalidates retained row measurements.</summary>
	public void ClearDataSource() {
		_sourceAssignmentVersion++;
		_dataSource = null;
		_dataSourceItemType = typeof(object);
		ResolvePublishedEntity = null;
		_columnWidthOverrides.Clear();
		_columnStretchOverrides.Clear();
		ResetRowHeightMeasurements();
	}

	/// <summary>Refreshes the selected row without rereading or resizing the current page.</summary>
	public Task<bool> RefreshSelectedEntityAsync() {
		return _selectedEntity == null ? Task.FromResult(false) : RefreshEntityAsync(_selectedEntity);
	}

	/// <summary>Refreshes the supplied visible entity through the source and replaces only its displayed row. A null refreshed entity removes that row.</summary>
	/// <returns>False when the entity is no longer visible or its binding/query was superseded.</returns>
	public async Task<bool> RefreshEntityAsync(object entity) {
		Guard.ArgumentNotNull(entity, nameof(entity));
		using var admission = await _bindingGate.EnterScopeAsync();
		if (IsDisposed || Disposing || _awaitingInitialBinding || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead)
			|| _dataSource == null || _dataSource != _boundDataSource || _columnsBindings != _boundColumns || !_entityToRowLookup.Contains(entity))
			return false;
		var source = _dataSource;
		var bindings = _columnsBindings;
		var bindingVersion = _bindingVersion;
		var query = (_searchText, _pageSize, _pageNumber, _sortColumnName, _sortDirection);
		var rows = _entityToRowLookup[entity].ToArray();
		var refreshed = new[] { entity };
		var prepare = CanPrepareReadOnlyCells(bindings);
		var formats = prepare ? bindings.Select(column => (column.DisplayType, column.DataType)).ToArray() : null;
		VisualGridPreparedValue[][] prepared = null;
		if (prepare) {
			try {
				prepared = await Task.Run(async () => {
					await source.RefreshRangeAsync(refreshed).ConfigureAwait(false);
					return refreshed[0] == null ? null : PrepareReadOnlyRows(Enumerable.Repeat(refreshed[0], rows.Length).ToArray(), bindings, formats);
				});
			} catch when (IsDisposed || Disposing) {
				return false;
			}
		} else
			await source.RefreshRangeAsync(refreshed);

		if (IsDisposed || Disposing || source != _dataSource || bindings != _columnsBindings || bindingVersion != _bindingVersion
			|| !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead)
			|| query != (_searchText, _pageSize, _pageNumber, _sortColumnName, _sortDirection)
			|| prepare && (!CanPrepareReadOnlyCells(bindings) || bindings.Where((column, index) => (column.DisplayType, column.DataType) != formats[index]).Any())
			|| rows.Any(row => !_rowToEntityMap.TryGetValue(row, out var visible) || !ReferenceEquals(visible, entity)))
			return false;

		var replacement = refreshed[0];
		var wasSelected = ReferenceEquals(_selectedEntity, entity);
		_grid.SuspendLayout();
		using (Tools.Scope.ExecuteOnDispose(() => _grid.ResumeLayout()))
		using (EnterVisualState(VisualState.Selecting)) {
			if (replacement == null) {
				foreach (var row in rows.OrderByDescending(row => row))
					RemoveGridRow(row);
				_totalRecords = Math.Max(0, _totalRecords - rows.Length);
				_totalRecordsLabel.Text = _totalRecords.ToString();
				// Retain the current page even if removing its last row requires the next explicit read to clamp it.
				_endPageNumber = Math.Max(_pageNumber, (int)Math.Ceiling(_totalRecords / (decimal)_pageSize) - 1);
				_pageCountLabel.Text = $"/ {_endPageNumber + 1}";
			} else {
				foreach (var row in rows.WithDescriptions()) {
					if (prepared != null)
						BindPreparedRow(row.Item, replacement, prepared[row.Index], reuse: true);
					else
						BindRowInternal(row.Item, replacement);
				}
				CalculateEntityToRowLookup();
			}
			if (wasSelected)
				_selectedEntity = replacement;
			HighlightSelectedEntity();
			_grid.Invalidate();
		}
		if (wasSelected && !ReferenceEquals(entity, replacement)) {
			RaiseEntityDeselectedEvent(entity);
			if (replacement != null)
				RaiseEntitySelectedEvent(replacement);
		}
		return true;
	}

	/// <summary>Reads all matching rows using the current filter and sort, independent of the visible page. Capture on the owning UI thread.</summary>
	/// <remarks>Queries the bound source on a worker without rebinding, disabling or changing the selection. The returned one-dimensional array retains its source item type, including when empty.</remarks>
	public async Task<Array> ReadFilteredEntitiesAsync(CancellationToken cancellationToken = default) {
		Guard.Ensure(!InvokeRequired, "Capture the grid query on its owning UI thread.");
		cancellationToken.ThrowIfCancellationRequested();
		var source = _dataSource;
		var itemType = _dataSourceItemType;
		var search = _searchText;
		var sort = _sortColumnName;
		var direction = _sortDirection;
		if (source == null || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead))
			return Array.CreateInstance(itemType, 0);
		return await Task.Run(async () => {
			cancellationToken.ThrowIfCancellationRequested();
			var read = await source.ReadRangeAsync(search, int.MaxValue, 0, sort, direction).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			var rows = read.Items?.ToArray() ?? Array.Empty<object>();
			var snapshot = Array.CreateInstance(itemType, rows.Length);
			for (var index = 0; index < rows.Length; index++)
				snapshot.SetValue(rows[index], index);
			return snapshot;
		}, cancellationToken);
	}

	/// <summary>Reports accepted grid operations instead of native drafts, layout and snapshot publication.</summary>
	protected override bool ShouldObserveChildStateChanges(Control child) {
		return false;
	}

	/// <summary>Invalidates accepted row content after the owner publishes a new source snapshot.</summary>
	internal void NotifyDataSourceChanged() {
		_dataPublicationVersion++;
	}

	/// <summary>Updates displayed rows for an entity already present in the current binding.</summary>
	internal async Task NotifyEntityUpdated(object entity) {
		if (_entityToRowLookup.Contains(entity))
			await UpdateGridAfterEdit(entity);
	}

	// Layout
	private void OrganizeLayout() {
		_createButton.Visible = _createButton.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanCreate);
		_deleteButton.Enabled = _deleteToolStripMenuItem.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanDelete);
		UpdateDeleteButtonVisibility();
		_editToolStripMenuItem.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanUpdate);
		_searchTextBox.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanSearch);

		var shouldBind = !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead) && _grid.Rows.Count > 0;
		shouldBind = shouldBind || _crudCapabilities.HasFlag(DataSourceCapabilities.CanRead) && _grid.Rows.Count == 0;
		shouldBind = shouldBind || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanSort) && _sortColumnName != null;
		if (!_crudCapabilities.HasFlag(DataSourceCapabilities.CanSort)) {
			_sortColumnName = null;
			_sortColumnIndex = 0;
		}

		if (_crudCapabilities.HasFlag(DataSourceCapabilities.CanPage)) {
			shouldBind = shouldBind || _layoutPanel.RowStyles[2].Height == 0;
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
			shouldBind = shouldBind || _layoutPanel.RowStyles[2].Height != 0;
			HidePageBar();
			_pageSize = int.MaxValue;
			_pageNumber = 0;
			SetVisiblePageNumberText(0);
		}
	}

	private void UpdatePageModeControl() {
		_updatingPageModeControl = true;
		using var restoreFlag = Tools.Scope.ExecuteOnDispose(() => _updatingPageModeControl = false);
		_pageModeButton.SelectedItem = _pageMode;
		_pageModeButton.ToolTipText = "Page mode: " + _pageModeButton.Text;
		_pageModeButton.AccessibleDescription = _pageModeButton.ToolTipText;
	}

	private void PageBarLayout(object sender, LayoutEventArgs args) {
		if (_layingOutPageBar || _pageSizeUpDown == null)
			return;
		_layingOutPageBar = true;
		using var restoreFlag = Tools.Scope.ExecuteOnDispose(() => _layingOutPageBar = false);
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

	private void GridColumnWidthChanged(object sender, ColumnInfoEventArgs args) {
		var column = args.Column.Index;
		if (_sizingColumns || _restoringGridSettings || column < 0 || column >= _columnsBindings.Length)
			return;
		if (State == VisualState.Loading && PrepareReadOnlyCellsInBackground) {
			_columnResizePending = true;
			_columnWidthOverrides[_columnsBindings[column]] = args.Column.Width;
			args.Column.AutoSizeMode = SourceGrid.AutoSizeMode.None;
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
	}

	private void UpdateButtonBarVisibility() {
		// A disabled parent does not remove the title or the commands supported by its source.
		if (!string.IsNullOrEmpty(_gridTitle) || _crudCapabilities.HasFlag(DataSourceCapabilities.CanCreate)
			|| _crudCapabilities.HasFlag(DataSourceCapabilities.CanDelete) || _crudCapabilities.HasFlag(DataSourceCapabilities.CanSearch))
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
		_bottomPanel.Visible = true;
		_layoutPanel.RowStyles[2].SizeType = SizeType.AutoSize;
	}

	private void HidePageBar() {
		_bottomPanel.Visible = false;
		_layoutPanel.RowStyles[2].SizeType = SizeType.Absolute;
		_layoutPanel.RowStyles[2].Height = 0;
	}

	private void UpdateCellInteraction() {
		if (_grid == null || _rowToEntityMap == null)
			return;

		// VisualGrid owns row toggling; SourceGrid's Ctrl-click selection must not process the same press again.
		_mouseSelectionController.MouseButtons = LeftClickToDeselect ? MouseButtons.None : MouseButtons.Left;
		foreach (var row in _rowToEntityMap) {
			for (var column = 0; column < Math.Min(_columnsBindings.Length, _grid.ColumnsCount); column++) {
				var cell = _grid[row.Key, column];
				if (cell != null)
					ConfigureCellInteraction(cell, _columnsBindings[column], row.Value);
			}
		}
	}

	private async Task UpdateGridAfterEdit(object entity) {
		if (RefreshEntireGridOnUpdate)
			await RefreshGrid();
		else {
			var rowNumbers = Enumerable.Empty<int>();
			if (_entityToRowLookup.Contains(entity))
				rowNumbers = _entityToRowLookup[entity];

			//var refreshedEntity = _dataSource.Refresh(entity);
			foreach (var rowNumber in rowNumbers)
				BindRowInternal(rowNumber, entity);

			CalculateEntityToRowLookup();
		}
		RaiseEntityUpdatedEvent(_selectedEntity);
	}

	// Event Handlers
	private void LoadingIndicatorResized(object sender, EventArgs args) {
		// Initial loading and subsequent refreshes share this full-viewport circle and geometry.
		var sideLength = Math.Max(1, Math.Min(_initialLoadingCircle.ClientSize.Width, _initialLoadingCircle.ClientSize.Height) - 1);
		_initialLoadingCircle.InnerCircleRadius = Math.Max(1, sideLength / 5);
		_initialLoadingCircle.OuterCircleRadius = Math.Max(1, sideLength / 3);
		_initialLoadingCircle.SpokeThickness = Math.Max(1, _initialLoadingCircle.InnerCircleRadius / 3);
	}

	private void GridContainerPanel_Resize(object sender, EventArgs args) {
		RefreshAutoPageSize();
	}

	private void GridViewportChanged(object sender, EventArgs args) {
		if (_grid == null)
			return;

		// Custom scrollbars alter the usable viewport without changing the control's client size.
		if (!_sizingColumns && _columnViewportWidth != _grid.DisplayRectangle.Width)
			StretchColumnsToViewport();
		RefreshAutoPageSize();
	}

	private void GridMetricsChanged(object sender, EventArgs args) {
		if (AutoPageSize)
			RefreshAutoPageSize(true);
	}

	private void PageModeSelectedIndexChanged(object sender, EventArgs args) {
		if (!_updatingPageModeControl && _pageModeButton.SelectedItem is VisualGridPageMode mode)
			PageMode = mode;
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
			NotifyPreferencesChanged();
			await RefreshGrid();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void PageSizeUpDown_ValueChanged(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (_restoringGridSettings || _updatingPageSizeDisplay || PageMode != VisualGridPageMode.Paged || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanPage))
				return;
			_manualPageSize = _pageSize = (int)_pageSizeUpDown.Value;
			NotifyPreferencesChanged();
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
	private int GetVisiblePageNumberText() {
		return int.Parse(_pageNumberBox.Text) - 1;
	}

	// TODO: Should be SetPageNumberAsync
	private async Task ChangePageNumberAsync(int pageNumber) {
		_pageNumber = PageMode == VisualGridPageMode.Single || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanPage)
			? 0 : pageNumber.ClipTo(0, _endPageNumber);
		SetVisiblePageNumberText(_pageNumber);
		NotifyPreferencesChanged();
		await RefreshGrid();
	}

	private async void CreateButton_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (State != VisualState.Normal)
				return;

			await CreateNewRecord();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	/// <summary>Uses the configured entity editor to create a record and refresh its visible result.</summary>
	public virtual async Task CreateNewRecord() {
		var newEntity = _dataSource.New();
		if (await ShowEntityEditor(newEntity, true) == CrudAction.Create) {
			await RefreshGrid();
			using var update = EnterUpdateScope(FinishedUpdateBehaviour.NotifyStateChanged);
			RaiseEntityCreatedEvent(newEntity);
			if (AutoSelectOnCreate)
				SelectedEntity = newEntity;
		}
	}

	protected void RaiseEntityCreatedEvent(object createdEntity) {
		if (this.CanRaiseEvents) {
			OnEntityCreated(createdEntity);
			if (EntityCreated != null)
				EntityCreated(this, createdEntity);
		}
		NotifyStateChanged();
	}

	protected virtual void OnEntityCreated(object deselectedEntity) {
	}

	private async void DeleteButton_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (State != VisualState.Normal)
				return;

			await DeleteSelectedRecord();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void Grid_MouseDoubleClick(object sender, MouseEventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (!CanInteractWithCommittedRows)
				return;
			if (LeftClickToDeselect && args.Button == MouseButtons.Left) {
				if (!TryStartCellEdit(_grid.PositionAtPoint(args.Location)))
					ToggleRowSelection(args);
			} else if (!LeftClickToDeselect)
				await EditSelectedEntity();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void Grid_MouseDown(object sender, MouseEventArgs args) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			_selectedRowOnMouseDown = -1;
			if (!CanInteractWithCommittedRows)
				return;

			var position = _grid.PositionAtPoint(args.Location);
			if (position.IsEmpty() || !_rowToEntityMap.ContainsKey(position.Row))
				return;

			if (args.Button == MouseButtons.Left)
				_lastClickedCell = position;
			if (LeftClickToDeselect)
				_grid.Focus(false);

			// Remember the selection before SourceGrid processes the press, regardless of its duration.
			if (args.Button == MouseButtons.Left && _grid.Selection.IsSelectedRow(position.Row))
				_selectedRowOnMouseDown = position.Row;
			if (args.Button == MouseButtons.Right || LeftClickToDeselect && args.Button == MouseButtons.Left && !SelectOnMouseUp)
				_grid.Selection.SelectRow(position.Row, true);
		}, out var Error);
		if (Error != null)
			await ExceptionDialog.ShowAsync(this, Error);
	}

	private async void Grid_MouseClick(object sender, MouseEventArgs args) {
		Tools.Exceptions.ExecuteCapturingException(() => {
			switch (args.Button) {
				case MouseButtons.Left:
					if (LeftClickToDeselect)
						ToggleRowSelection(args);
					break;
				case MouseButtons.Right:
					var position = _grid.PositionAtPoint(args.Location);
					if (CanInteractWithCommittedRows && HeaderContextMenuStrip != null && !position.IsEmpty() && position.Row == 0
						&& _grid.GetCell(position) is SourceGrid.Cells.ColumnHeader && _grid.PositionToRectangle(position).Contains(args.Location)) {
						// Native SourceControl identifies this public grid without retaining a menu-to-grid binding.
						HeaderContextMenuStrip.Show(this, PointToClient(_grid.PointToScreen(args.Location)));
						break;
					}

					if (CanInteractWithCommittedRows && RightClickForContextMenu && !position.IsEmpty()
						&& _rowToEntityMap.ContainsKey(position.Row) && _grid.Selection.IsSelectedRow(position.Row))
						_selectionContextMenuStrip.Show(_grid.PointToScreen(args.Location));
					break;
			}
		}, out var error);
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void Grid_KeyDown(object sender, KeyEventArgs args) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			if (!LeftClickToDeselect || !AllowCellEditing || args.KeyCode != Keys.F2)
				return;

			var position = _lastClickedCell;
			if (position.IsEmpty() || !_grid.Selection.IsSelectedRow(position.Row)) {
				var row = _grid.Selection.GetSelectionRegion().GetRowsIndex().FirstOrDefault(_rowToEntityMap.ContainsKey, -1);
				if (row < 0)
					return;
				var column = Enumerable.Range(0, _grid.ColumnsCount).FirstOrDefault(index => _grid[row, index]?.Editor?.EnableEdit == true, -1);
				if (column < 0)
					return;
				position = new Position(row, column);
			}
			args.Handled = TryStartCellEdit(position);
			args.SuppressKeyPress = args.Handled;
		}, out var Error);
		if (Error != null)
			await ExceptionDialog.ShowAsync(this, Error);
	}

	private bool TryStartCellEdit(Position position) {
		if (State != VisualState.Normal || position.IsEmpty() || !_rowToEntityMap.ContainsKey(position.Row) || position.Column >= _grid.ColumnsCount)
			return false;

		var context = new CellContext(_grid, position);
		if (context.Cell?.Editor?.EnableEdit != true)
			return false;
		if (context.IsEditing())
			return true;

		_grid.Selection.SelectRow(position.Row, true);
		context.Cell.RemoveController(SourceGrid.Cells.Controllers.Unselectable.Default);
		using var restoreFocusBehavior = new ActionDisposable(() => {
			if (LeftClickToDeselect)
				context.Cell.AddController(SourceGrid.Cells.Controllers.Unselectable.Default);
		});
		// Activating an editor resets SourceGrid's focus internally without changing the selected entity.
		using (EnterVisualState(VisualState.Selecting))
			context.StartEdit();
		if (context.Cell is SourceGrid.Cells.CheckBox)
			return context.Cell.Editor.SetCellValue(context, !((bool?)context.Value ?? false));
		return context.IsEditing();
	}

	private void ToggleRowSelection(MouseEventArgs args) {
		if (!CanInteractWithCommittedRows)
			return;

		var position = _grid.PositionAtPoint(args.Location);
		if (position.IsEmpty() || !_rowToEntityMap.ContainsKey(position.Row))
			return;

		if (_selectedRowOnMouseDown == position.Row)
			_grid.Selection.ResetSelection(false);
		else
			_grid.Selection.SelectRow(position.Row, true);
	}

	private async void DeselectToolStripMenuItem_Click(object sender, EventArgs args) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			_grid.Selection.ResetSelection(false);
		}, out var error);
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void EditToolStripMenuItem_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await EditSelectedEntity();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	/// <summary>Opens the configured editor for the current selection when an entity is selected.</summary>
	public virtual async Task EditSelectedEntity() {
		if (_selectedEntity == null)
			return;
		await EditEntity(_selectedEntity);
	}

	private async void DeleteToolStripMenuItem_Click(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await DeleteSelectedRecord();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	/// <summary>Deletes the currently selected entity when a selection exists.</summary>
	public virtual async Task DeleteSelectedRecord() {
		if (_selectedEntity == null)
			return;
		await DeleteEntity(_selectedEntity);
	}

	/// <summary>Queues the current filtered page for refresh through the existing refresh throttle.</summary>
	public Task RefreshGrid() {
		if (_pendingQueryPreferences != null && IsPreferenceCellEditing())
			return Task.CompletedTask;
		if (_refreshTask is not { IsCompleted: false })
			_refreshTask = RefreshGridInternal();
		return _refreshTask;
	}

	/// <summary>Uses the configured editor to update or delete an existing entity.</summary>
	public virtual async Task EditEntity(object entity) {
		switch (await ShowEntityEditor(entity, false)) {
			case CrudAction.Update:
				await RefreshGrid();
				RaiseEntityUpdatedEvent(entity);
				break;
			case CrudAction.Delete:
				await UpdateGridAfterDelete(entity);
				break;
		}
	}

	/// <summary>Confirms and deletes an entity through the bound source, then updates the visible grid.</summary>
	public virtual async Task DeleteEntity(object entity) {
		if (await DialogEx.ShowAsync(this, SystemIconType.Question, "Confirm Delete", "Are you sure you want to delete this record?", "&No", "&Yes") == DialogExResult.Button2) {
			using (EnterVisualState(VisualState.Loading))
			using (EnterLoadingIndicator()) {
				var deleteValidationResult = await _dataSource.ValidateAsync(entity, CrudAction.Delete);
				if (deleteValidationResult.IsSuccess) {
					await _dataSource.DeleteAsync(entity);
					await UpdateGridAfterDelete(entity);
				} else
					await DialogEx.ShowAsync(this, SystemIconType.Shield, "Validation Error", deleteValidationResult.ErrorMessages.ToParagraphCase(), "OK");
			}
		}
	}

	/// <summary>Selects the entity editor and typed adapter used by subsequent editing commands.</summary>
	public void SetEntityEditor<TEntity>(Type entityEditorType) {
		if (!typeof(ICrudEntityEditor<TEntity>).IsAssignableFrom(entityEditorType) && !typeof(ICrudEntityEditor<object>).IsAssignableFrom(entityEditorType))
			throw new ArgumentException(string.Format("Does not implement {0}", typeof(ICrudEntityEditor<TEntity>)), "entityEditorType");
		_entityEditorAdapter = new CrudEntityEditorAdapter<TEntity>();
		_entityEditorType = entityEditorType;
	}

	/// <summary>Measures current columns and rows using the existing native sizing path.</summary>
	internal void AutoSizeCells() {
		if (_sizedColumnBindings != _columnsBindings)
			return;
		SizeBoundColumns();
		RememberRowHeightGeometry();
		RefreshAutoPageSize();
	}

	protected virtual void OnEntitySelected(object selectedEntity) {
	}

	protected virtual void OnEntityDeselected(object deselectedEntity) {
	}

	protected virtual void OnEntityEditing(object selectedEntity, string propertyName, object oldValue, object newValue) {
	}

	protected virtual void OnEntityUpdated(object updatedEntity) {
	}

	protected virtual void OnEntityDeleted(object deletedEntity) {
	}

	private async Task<CrudAction?> ShowEntityEditor(object entity, bool isNewEntity) {
		using var entityEditorDialog = new CrudEntityEditorDialog();
		var entityEditor = Tools.Object.Create(_entityEditorType);
		if (entityEditor is DefaultCrudEntityEditor DefaultEditor)
			ConfigureReferenceBindings(DefaultEditor, entity);
		_entityEditorAdapter.SetAdaptee(entityEditor);
		_entityEditorAdapter.PropertyChanged += RaiseEntityEditingEvent;
		entityEditorDialog.SetEntityEditor(_dataSource, _entityEditorAdapter, _crudCapabilities, entity, isNewEntity);
		await entityEditorDialog.ShowDialogAsync(this);
		return entityEditorDialog.UserAction;
	}

	private void ConfigureReferenceBindings(DefaultCrudEntityEditor editor, object entity) {
		// Exact self references use this grid's datasource unless the model declares its own editor/converter.
		foreach (PropertyDescriptor Property in TypeDescriptor.GetProperties(entity))
			if (Property.PropertyType == entity.GetType() && Property.GetEditor(typeof(System.Drawing.Design.UITypeEditor)) == null
				&& Property.Converter.GetType() == typeof(TypeConverter))
				editor.ReferenceBindings[Property.Name] = new CrudReferenceBinding<object>(_dataSource, _columnsBindings);
		foreach (var column in _columnsBindings)
			if (column.ReferenceBinding != null && !string.IsNullOrEmpty(column.PropertyName))
				editor.ReferenceBindings[column.PropertyName] = column.ReferenceBinding;
		foreach (var binding in ReferenceBindings)
			editor.ReferenceBindings[binding.Key] = binding.Value;
	}

	private void UpdateDeleteButtonVisibility() {
		_deleteButton.Visible = _crudCapabilities.HasFlag(DataSourceCapabilities.CanDelete) && _selectedEntity != null &&
							   _entityToRowLookup != null && _entityToRowLookup[_selectedEntity].Any(row => _grid.Selection.IsSelectedRow(row));
	}

	private void SizeBoundColumns() {
		if (_grid.ColumnsCount == 0)
			return;
		_sizingColumns = true;
		using var restoreFlag = Tools.Scope.ExecuteOnDispose(() => _sizingColumns = false);
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
			_columnMinimumWidths[column] = _grid.Columns[column].Width;
		}
		StretchColumnsToViewport();
	}

	private void StretchColumnsToViewport() {
		if (_columnMinimumWidths.Length != _grid.ColumnsCount || _sizedColumnBindings != _columnsBindings)
			return;
		var previousSizing = _sizingColumns;
		_sizingColumns = true;
		using var restoreFlag = Tools.Scope.ExecuteOnDispose(() => _sizingColumns = previousSizing);
		_grid.Columns.SuspendLayout();
		using (Tools.Scope.ExecuteOnDispose(() => _grid.Columns.ResumeLayout())) {
			for (var column = 0; column < _grid.ColumnsCount; column++)
				_grid.Columns[column].Width = _columnMinimumWidths[column];
		}
		_grid.Columns.StretchToFit();
		_columnViewportWidth = _grid.DisplayRectangle.Width;
	}

	/// <summary>Serializes refresh admission and publishes the latest current page.</summary>
	private async Task RefreshGridInternal() {
		if (DesignMode || IsDisposed || State == VisualState.Loading && !_awaitingInitialBinding)
			return;
		using var admission = await _bindingGate.EnterScopeAsync();
		if (IsDisposed || Disposing)
			return;
		_refreshing = true;
		using var restoreRefreshing = Tools.Scope.ExecuteOnDispose(() => _refreshing = false);
		using (EnterVisualState(VisualState.Loading)) {
			var initialBinding = _awaitingInitialBinding;
			var suppressLoading = SuppressLoadingIndicator || initialBinding;
			if (!suppressLoading && !await _refreshThrottle.IsCallerFirstInStampede() || IsDisposed)
				return;
			using ActionScope? animation = suppressLoading ? null : EnterLoadingIndicator();
			var previousEnabled = _grid.Enabled;
			using var restoreEnabled = Tools.Scope.ExecuteOnDispose(() => {
				if (!suppressLoading && !IsDisposed)
					_grid.Enabled = previousEnabled;
			});
			if (!suppressLoading)
				_grid.Enabled = false;
			var pageSizing = new AutoPageSizing();
			while (!IsDisposed) {
				while (await BindInternal(pageSizing))
					;
				if (!initialBinding || _dataSource == null || IsDisposed)
					break;
				// Hidden controls retain their old bounds. Lay out accepted data under the opaque loading mask
				// and settle the actual page capacity before publishing the first completed binding.
				var previousPageSize = _pageSize;
				_layoutPanel.Visible = true;
				_initialLoadingCircle.BringToFront();
				PerformLayout();
				_layoutPanel.PerformLayout();
				_bottomPanel.PerformLayout();
				if (AutoPageSize && _crudCapabilities.HasFlag(DataSourceCapabilities.CanPage)) {
					_pageSize = CalculateAutoPageSize(pageSizing);
					UpdatePageSizeDisplay();
				}
				if (_pageSize == previousPageSize)
					break;
			}
			if (initialBinding && _dataSource != null && !IsDisposed) {
				_awaitingInitialBinding = false;
				UpdateLoadingIndicator();
				_layoutPanel.Visible = true;
			}
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

	// Selection & Misc
	private void InitializeGridSelectionMode() {
		_grid.SelectionMode = SourceGrid.GridSelectionMode.Row;
		_grid.Selection.EnableMultiSelection = false;
		_grid.Selection.FocusStyle = SourceGrid.FocusStyle.RemoveFocusCellOnLeave;
		_grid.Selection.SelectionChanged -= Grid_Selection_SelectionChanged;
		_grid.Selection.SelectionChanged += Grid_Selection_SelectionChanged;
		_grid.Selection.CellGotFocus -= Grid_Selection_CellGotFocus;
		_grid.Selection.CellGotFocus += Grid_Selection_CellGotFocus;
	}

	private void ConfigureCellInteraction(ICell cell, IVisualGridColumn column, object entity) {
		var canEdit = AllowCellEditing && _crudCapabilities.HasFlag(DataSourceCapabilities.CanUpdate) && column.CanEditCell && column.CellHasValue(entity);
		// Display-only text cells keep conversion support without constructing an unused native editor control.
		// Promote the existing cell when editing is enabled later; do not require another data-source read.
		if (canEdit && column.DisplayType == CrudCellDisplayType.Text && cell.Editor is not SourceGrid.Cells.Editors.EditorControlBase)
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

	private void HighlightSelectedEntity() {
		using (EnterVisualState(VisualState.Selecting)) {
			_grid.Selection.ResetSelection(false);
			if (_selectedEntity != null && _entityToRowLookup.Contains(_selectedEntity)) {
				foreach (var rowNum in _entityToRowLookup[_selectedEntity])
					_grid.Selection.SelectRow(rowNum, true);
			}
			UpdateDeleteButtonVisibility();
		}
	}

	private void SetVisiblePageNumberText(int number) {
		_updatingPageNumberText = true;
		using var restoreFlag = Tools.Scope.ExecuteOnDispose(() => _updatingPageNumberText = false);
		_pageNumberBox.Text = (number + 1).ToString();
	}

	private async Task UpdateGridAfterDelete(object entity) {
		var wasSelectedEntity = ReferenceEquals(entity, _selectedEntity);
		if (wasSelectedEntity)
			_selectedEntity = null; // set null here so as to avoid highlight when refreshing grid

		if (RefreshEntireGridOnDelete)
			await RefreshGrid();
		else {
			var rowNumbers = Enumerable.Empty<int>();
			if (_entityToRowLookup.Contains(entity))
				rowNumbers = _entityToRowLookup[entity];

			foreach (var rowNumber in rowNumbers)
				RemoveGridRow(rowNumber);
			_totalRecords -= rowNumbers.Count();
			_totalRecordsLabel.Text = _totalRecords.ToString();
		}

		if (wasSelectedEntity)
			RaiseEntityDeselectedEvent(entity);

		RaiseEntityDeletedEvent(entity);
	}

	private int CalculateAutoPageSize(AutoPageSizing sizing = null) {
		// Native row, header and viewport measurements already use the current window's device pixels.
		// Scale only the unmeasured fallback; a tall image must not inflate the budget for every ordinary row.
		var fallbackHeight = Math.Max(_grid.MinimumHeight, (int)Math.Round(_defaultRowHeight * _grid.DeviceDpi / 96.0));
		var headerHeight = _grid.Rows.Count > 0 ? _grid.Rows[0].Height : fallbackHeight;
		var available = Math.Max(0, _grid.DisplayRectangle.Height - headerHeight);
		var fitted = 0;
		var used = 0;
		var nextHeight = int.MaxValue;
		for (var row = 1; row < _grid.Rows.Count; row++) {
			var height = Math.Max(1, _grid.Rows[row].Height);
			if (height > available - used)
				break;
			used += height;
			fitted++;
			nextHeight = Math.Min(nextHeight, height);
		}
		var overflow = fitted < _grid.Rows.Count - 1;
		var capacity = overflow ? Math.Max(1, fitted)
			: Math.Max(1, fitted + (available - used) / (nextHeight == int.MaxValue ? Math.Max(1, fallbackHeight) : nextHeight));
		// A short final result has no additional rows to fill the viewport. Do not move its source offset by probing indefinitely.
		if (!overflow && _boundDataSource == _dataSource && _boundColumns == _columnsBindings && _grid.Rows.Count - 1 < _pageSize)
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
		using var restoreFlag = Tools.Scope.ExecuteOnDispose(() => _updatingPageSizeDisplay = false);
		_pageSizeUpDown.Value = Math.Max((int)_pageSizeUpDown.Minimum, Math.Min(value, (int)_pageSizeUpDown.Maximum));
	}

	// Binding
	/// <summary>
	/// Binds the datasource to the view.
	/// </summary>
	/// <returns>True if search parameters changed during search, false otherwise.</returns>
	private async Task<bool> BindInternal(AutoPageSizing pageSizing) {
		var dataSource = _dataSource;
		var dataPublicationVersion = _dataPublicationVersion;
		var columns = _columnsBindings;
		var searchText = _searchText;
		var pageSize = _pageSize;
		var pageNumber = _pageNumber;
		var sortColumnName = _sortColumnName;
		var sortDirection = _sortDirection;
		var prepareInBackground = PrepareReadOnlyCellsInBackground;
		var referenceLookup = UseEntityReferenceForLookup;
		bool QueryChanged() {
			return dataSource != _dataSource || dataPublicationVersion != _dataPublicationVersion || columns != _columnsBindings || searchText != _searchText || pageSize != _pageSize
						|| pageNumber != _pageNumber || sortColumnName != _sortColumnName || sortDirection != _sortDirection || prepareInBackground != PrepareReadOnlyCellsInBackground
						|| referenceLookup != UseEntityReferenceForLookup || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead);
		}

		if (pageSize == 0)
			return false;

		// Take care of null grid
		if (_columnsBindings == null || _dataSource == null || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead)) {
			ResetRowHeightMeasurements();
			_boundDataSource = null;
			_boundColumns = null;
			_rowToEntityMap.Clear();
			CalculateEntityToRowLookup();
			_grid.Redim(0, 0);
			_sizedColumnBindings = null;
			_totalRecords = 0;
			_totalRecordsLabel.Text = "0";
			DataBindingComplete?.Invoke(this, EventArgs.Empty);
			return false;
		}

		// Only the explicit worker-safe, read-only contract permits moving arbitrary source/column delegates off the owner thread.
		var prepareCells = CanPrepareReadOnlyCells(columns);
		var formats = prepareCells ? columns.Select(column => (column.DisplayType, column.DataType)).ToArray() : null;
		var reusableRowCount = _boundDataSource == dataSource && _boundColumns == columns ? Math.Max(0, _grid.RowsCount - 1) : 0;
		DataSourceItems<object> readResult;
		object[] data;
		IDictionary<int, object> preparedRows = null;
		ILookup<object, int> preparedLookup = null;
		VisualGridPreparedValue[][] prepared = null;
		VisualGridPreparedCell[][] newCells = null;
		if (prepareCells) {
			try {
				var result = await Task.Run(async () => {
					var read = await dataSource.ReadRangeAsync(searchText, pageSize, pageNumber, sortColumnName, sortDirection).ConfigureAwait(false);
					var entities = read.Items.ToArray();
					var visible = entities.Take(pageSize).ToArray();
					var rows = visible.Select((entity, index) => (Entity: entity, Row: index + 1)).ToDictionary(item => item.Row, item => item.Entity);
					var lookup = referenceLookup ? rows.InverseUsingValueReferenceAsKey() : rows.Inverse();
					var values = PrepareReadOnlyRows(visible, columns, formats);
					var cells = new VisualGridPreparedCell[values.Length][];
					for (var row = reusableRowCount; row < values.Length; row++)
						cells[row] = values[row].Select(value => new VisualGridPreparedCell(value)).ToArray();
					return (Read: read, Entities: entities, Cells: values, NewCells: cells, Rows: rows, Lookup: lookup);
				});
				readResult = result.Read;
				data = result.Entities;
				prepared = result.Cells;
				newCells = result.NewCells;
				preparedRows = result.Rows;
				preparedLookup = result.Lookup;
			} catch when (IsDisposed || Disposing) {
				return false;
			}
		} else {
			readResult = await dataSource.ReadRangeAsync(searchText, pageSize, pageNumber, sortColumnName, sortDirection);
			data = null;
		}
		if (IsDisposed || Disposing)
			return false;
		// A superseded result must not change the requested page, visible rows or persisted selection options.
		if (QueryChanged() || prepareCells && (!CanPrepareReadOnlyCells(columns)
			|| columns.Where((column, index) => (column.DisplayType, column.DataType) != formats[index]).Any()))
			return true;
		data ??= readResult.Items.ToArray();
		var previousSelection = _selectedEntity;
		var publishedSelection = previousSelection != null && ResolvePublishedEntity != null ? ResolvePublishedEntity(previousSelection) : previousSelection;
		var reusePreparedCells = prepared != null && _boundDataSource == dataSource && _boundColumns == columns;
		_bindingVersion++;
		_boundDataSource = dataSource;
		_boundColumns = columns;
		_totalRecords = readResult.TotalCount;
		_endPageNumber = ((int)Math.Ceiling(_totalRecords / (decimal)pageSize) - 1).ClipTo(0, int.MaxValue);

		var resultPage = Math.Max(0, Math.Min(readResult.Page, _endPageNumber));
		if (resultPage != _pageNumber) {
			_pageNumber = resultPage;
			SetVisiblePageNumberText(resultPage);
		}
		pageNumber = resultPage;

		// Grid header
		_titleLabel.Text = GridTitle;
		var sizeColumns = _sizedColumnBindings != columns || !_columnsSizedFromData && data.Length > 0;
		var acceptedVersion = _bindingVersion;
		bool AcceptedBindingIsCurrent() {
			return !IsDisposed && !Disposing && !QueryChanged() && _bindingVersion == acceptedVersion
						&& CanPrepareReadOnlyCells(columns) && !columns.Where((column, index) => (column.DisplayType, column.DataType) != formats[index]).Any();
		}

		// SourceGrid already buffers painting. WM_SETREDRAW would remove WS_VISIBLE and make native clicks miss these committed rows.
		// Keep its HWND visible while synchronously publishing cells, and defer layout until the publication is complete.
		using (EnterVisualState(VisualState.Selecting)) {
			_selectedEntity = publishedSelection;
			if (preparedRows != null)
				_rowToEntityMap = preparedRows;
			else
				_rowToEntityMap.Clear();
			var previousSizing = _sizingColumns;
			if (prepared != null)
				_sizingColumns = true;
			using var restoreSizing = Tools.Scope.ExecuteOnDispose(() => _sizingColumns = previousSizing);
			_grid.SuspendLayout();
			using (Tools.Scope.ExecuteOnDispose(() => _grid.ResumeLayout())) {
				_grid.Redim(Math.Min(pageSize, data.Length) + 1, columns.Length);
				for (var column = 0; column < columns.Length; column++)
					BindColumnHeaders(column);
				foreach (var entity in data.WithDescriptions()) {
					if (pageSize <= entity.Index)
						break;
					if (prepared != null)
						BindPreparedRow(entity.Index + 1, entity.Item, prepared[entity.Index], reusePreparedCells, newCells[entity.Index]);
					else
						BindRowInternal(entity.Index + 1, entity.Item);
				}
				if (preparedLookup != null)
					_entityToRowLookup = preparedLookup;
				else
					CalculateEntityToRowLookup();
				_totalRecordsLabel.Text = _totalRecords.ToString();
				_pageCountLabel.Text = string.Format("/ {0}", _endPageNumber + 1);
				InitializeGridSelectionMode();
				HighlightSelectedEntity();
				if (prepared == null && sizeColumns) {
					SizeBoundColumns();
					RememberRowHeightGeometry();
				} else if (prepared == null)
					SizeBoundRows();
			}
		}
		// Replace the selected immutable row with the same identity in the same publication as its cells.
		// Geometry can yield afterward; native selection and row commands must already refer to the published model.
		if (!ReferenceEquals(previousSelection, publishedSelection)) {
			if (previousSelection != null)
				RaiseEntityDeselectedEvent(previousSelection);
			if (publishedSelection != null)
				RaiseEntitySelectedEvent(publishedSelection);
		}
		if (prepared != null) {
			// A resize or font change invalidates native measurements, not the accepted data.
			// Retry geometry over those same cells; only a changed query may read and prepare another page.
			while (AcceptedBindingIsCurrent()) {
				var sized = sizeColumns ? await SizeBoundColumnsAsync(AcceptedBindingIsCurrent) : await SizeBoundRowsAsync(AcceptedBindingIsCurrent);
				if (sized)
					break;
			}
			if (!AcceptedBindingIsCurrent())
				return !IsDisposed && !Disposing;

			// Publishing prepared rows can change scrollbars while guarded layout events defer stretching.
			if (_columnViewportWidth != _grid.DisplayRectangle.Width)
				StretchColumnsToViewport();
		}
		_bottomPanel.PerformLayout();
		if (prepared != null)
			_grid.Invalidate();

		// Reconcile the estimate with the actual rows, allowing both growth and shrinkage under one bounded settle cycle.
		if (AutoPageSize && _crudCapabilities.HasFlag(DataSourceCapabilities.CanPage)) {
			_pageSize = CalculateAutoPageSize(pageSizing);
			UpdatePageSizeDisplay();
		}

		if (!QueryChanged())
			DataBindingComplete?.Invoke(this, EventArgs.Empty);
		return QueryChanged();
	}

	/// <summary>Checks whether the current bindings support detached read-only value preparation.</summary>
	private bool CanPrepareReadOnlyCells(IVisualGridColumn[] columns) {
		return PrepareReadOnlyCellsInBackground && columns.All(column => column.DisplayType is CrudCellDisplayType.Text or CrudCellDisplayType.Image)
					&& (!AllowCellEditing || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanUpdate) || columns.All(column => !column.CanEditCell));
	}

	// Measure columns and rows in bounded UI batches, accepting only the captured native layout.
	/// <summary>Measures native column batches while retaining the owner thread's dispatcher.</summary>
	private async Task<bool> SizeBoundColumnsAsync(Func<bool> isCurrent) {
		var columns = _columnsBindings;
		var widths = Enumerable.Range(0, _grid.ColumnsCount).Select(column => _grid.Columns[column].Width).ToArray();
		var geometryWidths = (int[])widths.Clone();
		var rowCount = _grid.RowsCount;
		var geometry = GetRowHeightGeometry();
		var headerFont = SourceGrid.Cells.Views.ColumnHeader.Default.Font;
		var minimumWidth = _grid.MinimumWidth;
		var signatures = new VisualGridRowMeasurements.Signature[rowCount];
		bool Current() {
			return isCurrent() && rowCount == _grid.RowsCount && widths.Length == _grid.ColumnsCount
						&& geometry.Equals(GetRowHeightGeometry()) && Equals(headerFont, SourceGrid.Cells.Views.ColumnHeader.Default.Font)
						&& minimumWidth == _grid.MinimumWidth && NativeWidthsAreCurrent(columns, geometryWidths);
		}

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
				var width = minimumWidth;
				for (var row = 0; row < rowCount; row++) {
					if (_columnWidthOverrides.ContainsKey(columns[column]))
						break;
					width = Math.Max(width, _grid.Columns.MeasureColumnWidth(column, false, row, row));
					yield return true;
				}
				if (_columnWidthOverrides.TryGetValue(columns[column], out var manualWidth))
					width = manualWidth;
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
		bool Current() {
			return isCurrent() && rowCount == _grid.RowsCount && widths.Length == _grid.ColumnsCount
						&& geometry.Equals(GetRowHeightGeometry()) && autoSize == AutoSizeRowsOnRefresh && NativeWidthsAreCurrent(columns, widths);
		}

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
			foreach (var step in MeasureNativeRows(measure))
				yield return step;
		}
	}

	/// <summary>Yields after bounded native measurement work for each requested row.</summary>
	private IEnumerable<bool> MeasureNativeRows(bool[] measure) {
		for (var row = measure.Length - 1; row >= 0; row--) {
			if (!measure[row] || (_grid.Rows.GetAutoSizeMode(row) & SourceGrid.AutoSizeMode.EnableAutoSize) != SourceGrid.AutoSizeMode.EnableAutoSize
				|| !_grid.Rows.IsRowVisible(row))
				continue;
			var height = _grid.MinimumHeight;
			if ((_grid.Rows.GetAutoSizeMode(row) & SourceGrid.AutoSizeMode.MinimumSize) != SourceGrid.AutoSizeMode.MinimumSize) {
				for (var column = 0; column < _grid.ColumnsCount; column++) {
					height = Math.Max(height, _grid.Rows.MeasureRowHeight(row, false, column, column));
					yield return true;
				}
			}
			_grid.Rows.SetHeight(row, height);
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
		using var timer = new System.Windows.Forms.Timer { Interval = 1 };
		TaskCompletionSource resume = null;
		timer.Tick += (_, _) => { timer.Stop(); resume?.TrySetResult(); };
		void DisposedWhileSizing(object sender, EventArgs args) {
			resume?.TrySetResult();
		}

		Disposed += DisposedWhileSizing;
		using var removeDisposed = Tools.Scope.ExecuteOnDispose(() => Disposed -= DisposedWhileSizing);
		using var work = steps.GetEnumerator();
		while (!IsDisposed && !Disposing && isCurrent()) {
			var more = true;
			{
				var previousSizing = _sizingColumns;
				_sizingColumns = true;
				using var restoreSizing = Tools.Scope.ExecuteOnDispose(() => _sizingColumns = previousSizing);
				_grid.SuspendLayout();
				_grid.Rows.SuspendLayout();
				_grid.Columns.SuspendLayout();
				using var resumeLayout = Tools.Scope.ExecuteOnDispose(() => {
					_grid.Rows.ResumeLayout();
					_grid.Columns.ResumeLayout();
					_grid.ResumeLayout(false);
				});
				var started = Stopwatch.GetTimestamp();
				var count = 0;
				do {
					more = work.MoveNext();
				} while (more && ++count < 4096 && Stopwatch.GetElapsedTime(started).TotalMilliseconds < 6);
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

	/// <summary>Captures source values and display strings before native cell publication.</summary>
	private static VisualGridPreparedValue[][] PrepareReadOnlyRows(object[] entities, IVisualGridColumn[] columns, (CrudCellDisplayType DisplayType, Type DataType)[] formats) {
		// Conversion metadata is initialized once per populated text column on the worker; rows carry only values and prepared strings.
		var editors = new VisualGridPreparedTextEditor[columns.Length];
		var rows = new VisualGridPreparedValue[entities.Length][];
		for (var row = 0; row < entities.Length; row++) {
			rows[row] = new VisualGridPreparedValue[columns.Length];
			for (var column = 0; column < columns.Length; column++) {
				var hasValue = columns[column].CellHasValue(entities[row]);
				var value = hasValue ? columns[column].GetCellValue(entities[row]) : null;
				var editor = editors[column];
				if (hasValue && formats[column].DisplayType == CrudCellDisplayType.Text && editor == null)
					editor = editors[column] = new VisualGridPreparedTextEditor(formats[column].DataType);
				if (hasValue && formats[column].DisplayType == CrudCellDisplayType.Image)
					Guard.Argument(value == null || value is System.Drawing.Image, nameof(value), "An image cell requires a System.Drawing.Image value.");
				var displayText = hasValue && editor != null ? editor.PrepareValue(value) : string.Empty;
				rows[row][column] = new VisualGridPreparedValue(hasValue, value, displayText, editor);
			}
		}
		return rows;
	}

	/// <summary>Updates or creates stable native cell slots from one prepared row.</summary>
	private void BindPreparedRow(int row, object entity, VisualGridPreparedValue[] cells, bool reuse, VisualGridPreparedCell[] newCells = null) {
		_rowToEntityMap[row] = entity;
		for (var column = 0; column < cells.Length; column++) {
			var prepared = cells[column];
			var view = GetColumnCellView(_columnsBindings[column]);
			if (reuse && _grid[row, column] is VisualGridPreparedCell existing && existing.CanApplyPreparedValue && existing.View == view
				&& existing.RowSpan == 1 && existing.ColumnSpan == 1 && existing.Editor is not SourceGrid.Cells.Editors.EditorControlBase
				&& existing.FindController<UpdateEntityOnValueChangedController>() == null)                 // Retain the native cell's complete graph; publishing new data does not allocate or fire edit callbacks.
				existing.ApplyPreparedValue(prepared);
			else {
				var cell = newCells?[column] ?? new VisualGridPreparedCell(prepared);
				cell.View = view;
				ConfigureCellInteraction(cell, _columnsBindings[column], entity);
				_grid[row, column] = cell;
			}
		}
	}

	// Remeasure changed content while retaining actual native heights for compatible rows.
	private void SizeBoundRows() {
		if (AutoSizeRowsOnRefresh || !_rowMeasurements.Matches(_dataSource, _columnsBindings, GetRowHeightGeometry(), _grid)) {
			_grid.Rows.AutoSize(false);
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
	private void ResetRowHeightMeasurements() {
		_rowMeasurements.Clear();
	}

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
		var view = SourceGrid.Cells.Views.Cell.Default;
		if (row == 0 || view.WordWrap || view.ElementText?.GetType() != typeof(Sphere10.Framework.Windows.Forms.Drawing.VisualElements.TextGDI)
			|| view.ElementImage?.GetType() != typeof(Sphere10.Framework.Windows.Forms.Drawing.VisualElements.Image))
			return new VisualGridRowMeasurements.Signature(-1, false);
		var imageHeight = 0;
		var hasText = false;
		for (var column = 0; column < _grid.ColumnsCount; column++) {
			if (_columnsBindings[column].DisplayType is not (CrudCellDisplayType.Text or CrudCellDisplayType.Image)
				|| _grid[row, column] is not Cell cell || cell.GetType() != typeof(Cell) && cell is not VisualGridPreparedCell
				|| cell.View != GetColumnCellView(_columnsBindings[column]) || cell.Editor is SourceGrid.Cells.Editors.EditorControlBase)
				return new VisualGridRowMeasurements.Signature(-1, false);
			var cellView = (SourceGrid.Cells.Views.Cell)cell.View;
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

	private void BindColumnHeaders(int col) {
		_grid[0, col] = new SourceGrid.Cells.ColumnHeader(_columnsBindings[col].ColumnName);
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
		for (var col = 0; col < _columnsBindings.Length; col++) {
			var columnBinding = _columnsBindings[col];
			_grid[row, col] = CreateCell(columnBinding, entity);
		}
	}

	private ICell CreateCell(IVisualGridColumn column, object entity) {
		Cell cell;
		if (column.CellHasValue(entity)) {
			var cellValue = column.GetCellValue(entity);
			switch (column.DisplayType) {
				case CrudCellDisplayType.Text:
					cell = CreateTextBoxCell(column, entity, cellValue);
					break;
				case CrudCellDisplayType.Boolean:
					cell = CreateCheckBoxCell(column, entity, cellValue);
					break;
				case CrudCellDisplayType.Currency:
					cell = CreateCurrencyCell(column, entity, cellValue);
					break;
				case CrudCellDisplayType.Numeric:
					cell = CreateNumericCell(column, entity, cellValue);
					break;
				case CrudCellDisplayType.DateTime:
				case CrudCellDisplayType.Date:
				case CrudCellDisplayType.Time:
					cell = CreateDateTimeCell(column, entity, cellValue);
					break;
				case CrudCellDisplayType.DropDownList:
					cell = CreateDropDownListCell(column, entity, cellValue);
					break;
				case CrudCellDisplayType.Button:
					cell = CreateButtonCell(column, entity, cellValue);
					break;
				case CrudCellDisplayType.EditCommand:
					cell = CreateCommandCell(column, entity, CrudAction.Update);
					break;
				case CrudCellDisplayType.DeleteCommand:
					cell = CreateCommandCell(column, entity, CrudAction.Delete);
					break;
				case CrudCellDisplayType.Empty:
					cell = CreateEmptyCell();
					break;
				case CrudCellDisplayType.Image:
					Guard.Argument(cellValue == null || cellValue is System.Drawing.Image, nameof(cellValue), "An image cell requires a System.Drawing.Image value.");
					// The column owner retains image ownership; diagnostic icons are read-only display cells.
					cell = new Cell(string.Empty) { Image = cellValue as System.Drawing.Image, Editor = null };
					break;
				default:
					throw new NotImplementedException(string.Format("CrudColumnType not supported '{0}'", column.DisplayType));
			}
		} else
			cell = CreateEmptyCell();

		if (column.DisplayType == CrudCellDisplayType.Image)
			cell.View = GetColumnCellView(column);
		ConfigureCellInteraction(cell, column, entity);
		return cell;
	}

	private SourceGrid.Cells.Views.Cell GetColumnCellView(IVisualGridColumn column) {
		var standard = SourceGrid.Cells.Views.Cell.Default;
		if (column.DisplayType != CrudCellDisplayType.Image)
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

	private Cell CreateEmptyCell() {
		return new Cell(string.Empty);
	}

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

	private Cell CreateCurrencyCell(IVisualGridColumn columnBinding, object entity, object cellValue) {
		return new Cell(cellValue, new SourceGrid.Cells.Editors.TextBoxCurrency(columnBinding.DataType));
	}

	private Cell CreateNumericCell(IVisualGridColumn columnBinding, object entity, object cellValue) {
		return new Cell(cellValue, new SourceGrid.Cells.Editors.TextBoxNumeric(columnBinding.DataType));
	}

	private Cell CreateDateTimeCell(IVisualGridColumn columnBinding, object entity, object cellValue) {
		const DateTimeStyles dtStyles = System.Globalization.DateTimeStyles.AllowInnerWhite | System.Globalization.DateTimeStyles.AllowLeadingWhite | System.Globalization.DateTimeStyles.AllowTrailingWhite |
										System.Globalization.DateTimeStyles.AllowWhiteSpaces;
		var editor = columnBinding.DisplayType == CrudCellDisplayType.Time ? new SourceGrid.Cells.Editors.TimePicker() : new SourceGrid.Cells.Editors.DateTimePicker();
		editor.AllowNull = columnBinding.DataType == typeof(DateTime?);
		var customFormat = columnBinding.GetDateTimeFormat(entity);
		editor.Control.CustomFormat = customFormat;
		var dtParseFormats = new string[] { customFormat };
		var dtConverter = new Sphere10.Framework.ComponentModel.DateTimeTypeConverter(customFormat, dtParseFormats, dtStyles);
		editor.TypeConverter = dtConverter;
		return new Cell(cellValue, editor);
	}

	private Cell CreateDropDownListCell(IVisualGridColumn columnBinding, object entity, object cellValue) {
		if (columnBinding.ReferenceBinding != null)
			return new Cell(cellValue, new SourceGrid.Cells.Editors.CrudReference(columnBinding.DataType, columnBinding.ReferenceBinding));
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

	private Cell CreateButtonCell(IVisualGridColumn columnBinding, object entity, object cellValue) {
		return CreateButtonCell(columnBinding, entity, columnBinding.GetButtonCaption(entity), columnBinding.GetButtonImage(entity), columnBinding.ButtonPressed);
	}

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

	private void RemoveGridRow(int row) {
		if (row == 0)
			throw new ArgumentOutOfRangeException("Cannot remove header row (at 0)", "row");

		// Remove from the lookup tables
		_rowToEntityMap.Remove(row);
		var followingRows = new List<object>();
		for (var index = row + 1; index < _grid.RowsCount; index++) {
			followingRows.Add(_rowToEntityMap[index]);
			_rowToEntityMap.Remove(index);
		}
		foreach (var rowObject in followingRows.WithDescriptions())
			_rowToEntityMap[rowObject.Index + row] = rowObject.Item;

		// Remove the grid row
		_grid.Rows.Remove(row);

		// Recalculate the entity-to-row lookup
		CalculateEntityToRowLookup();
	}

	private void CalculateEntityToRowLookup() {
		_entityToRowLookup = UseEntityReferenceForLookup ? _rowToEntityMap.InverseUsingValueReferenceAsKey() : _rowToEntityMap.Inverse();
	}

	// Event Triggers
	protected void RaiseEntitySelectedEvent(object selectedEntity) {
		UpdateDeleteButtonVisibility();
		if (this.CanRaiseEvents) {
			OnEntitySelected(selectedEntity);
			if (EntitySelected != null)
				EntitySelected(this, selectedEntity);
		}
	}

	protected void RaiseEntityDeselectedEvent(object deselectedEntity) {
		UpdateDeleteButtonVisibility();
		if (this.CanRaiseEvents) {
			OnEntityDeselected(deselectedEntity);
			if (EntityDeselected != null)
				EntityDeselected(this, deselectedEntity);
		}
	}

	protected void RaiseEntityEditingEvent(CrudEntityPropertyChangedEventArgs entityChangedEventArgs) {
		if (this.CanRaiseEvents) {
			OnEntityEditing(entityChangedEventArgs.Entity, entityChangedEventArgs.PropertyName, entityChangedEventArgs.OldValue, entityChangedEventArgs.NewValue);
			if (EntityPropertyChanged != null)
				EntityPropertyChanged(this, entityChangedEventArgs);
		}
	}

	protected void RaiseEntityUpdatedEvent(object updatedEntity) {
		if (this.CanRaiseEvents) {
			OnEntityUpdated(updatedEntity);
			if (EntityUpdated != null)
				EntityUpdated(this, updatedEntity);
		}
		NotifyStateChanged();
	}

	protected void RaiseEntityDeletedEvent(object deletedEntity) {
		if (this.CanRaiseEvents) {
			OnEntityDeleted(deletedEntity);
			if (EntityDeleted != null)
				EntityDeleted(this, deletedEntity);
		}
		NotifyStateChanged();
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

	private async void Grid_Selection_SelectionChanged(object sender, SourceGrid.RangeRegionChangedEventArgs args) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			if (!CanInteractWithCommittedRows)
				return;

			var selectedRows = _grid.Selection.GetSelectionRegion().GetRowsIndex();
			var entity = selectedRows.Where(_rowToEntityMap.ContainsKey).Select(row => _rowToEntityMap[row]).FirstOrDefault();
			if (ReferenceEquals(entity, _selectedEntity))
				return;

			var previousEntity = _selectedEntity;
			_selectedEntity = entity;
			if (previousEntity != null)
				RaiseEntityDeselectedEvent(previousEntity);
			if (entity != null)
				RaiseEntitySelectedEvent(entity);
			NotifyStateChanged();
		}, out var error);
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void Grid_Selection_CellGotFocus(SourceGrid.Selection.SelectionBase sender, SourceGrid.ChangeActivePositionEventArgs args) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
		}, out var error);
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	/// <summary>Restores the previous interaction state when the returned scope is disposed.</summary>
	private IDisposable EnterVisualState(VisualState newState) {
		var previousState = State;
		var initialBinding = _awaitingInitialBinding;
		State = newState;
		return new ActionDisposable(() => State = initialBinding && !_awaitingInitialBinding ? VisualState.Normal : previousState);
	}

	#region Inner Types

	/// <summary>Publishes a completed native interaction after SourceGrid finishes its synchronous controllers.</summary>
	private sealed class InputGrid : SourceGrid.Grid {
		/// <summary>Allows ordinary parameterless activation by the native designer.</summary>
		public InputGrid() {
		}

		/// <summary>Groups row replacement and focus changes made by one native press.</summary>
		protected override void OnMouseDown(MouseEventArgs e) {
			using var update = EnterOwnerUpdateScope();
			base.OnMouseDown(e);
		}

		/// <summary>Groups click-driven row selection without changing SourceGrid's callback order.</summary>
		protected override void OnMouseClick(MouseEventArgs e) {
			using var update = EnterOwnerUpdateScope();
			base.OnMouseClick(e);
		}

		/// <summary>Groups the selection and editor activation performed by a double click.</summary>
		protected override void OnMouseDoubleClick(MouseEventArgs e) {
			using var update = EnterOwnerUpdateScope();
			base.OnMouseDoubleClick(e);
		}

		/// <summary>Groups keyboard selection and editor activation until their synchronous work completes.</summary>
		protected override void OnKeyDown(KeyEventArgs e) {
			using var update = EnterOwnerUpdateScope();
			base.OnKeyDown(e);
		}

		// Resolve the actual owner only for input; an unparented designer control has no state to publish.
		private UpdateScope EnterOwnerUpdateScope() {
			for (var parent = Parent; parent != null; parent = parent.Parent)
				if (parent is VisualGrid owner)
					return owner.EnterUpdateScope(FinishedUpdateBehaviour.NotifyStateChanged);
			return default;
		}
	}

	private sealed class AutoPageSizing {
		public object Query {
			get; set;
		}

		public int MaximumPageSize { get; set; } = int.MaxValue;
	}

	private enum VisualState {
		Normal,
		Selecting,
		Loading
	}
	#endregion
}
