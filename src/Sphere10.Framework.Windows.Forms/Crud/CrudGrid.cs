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
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

public partial class CrudGrid : UserControl, ICrudGrid {

	#region Constants

	private const int DefaultRowHeight = 24;
	private const int DefaultPageSize = 100;

	#endregion

	#region Events

	[Category("Behavior")]
	public event EventHandlerEx<CrudGrid, object> EntitySelected;
	[Category("Behavior")]
	public event EventHandlerEx<CrudGrid, object> EntityDeselected;
	[Category("Behavior")]
	public event EventHandlerEx<CrudGrid, object> EntityCreated;
	[Category("Behavior")]
	public event EventHandlerEx<CrudGrid, object> EntityUpdated;
	[Category("Behavior")]
	public event EventHandlerEx<CrudGrid, object> EntityDeleted;
	[Category("Behavior")]
	public event EventHandlerEx<CrudGrid, CrudEntityPropertyChangedEventArgs> EntityPropertyChanged;
	#endregion

	#region Fields

	private readonly object _threadLock;
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
	private CrudGridPageMode _pageMode;
	private int _totalRecords;
	private string _searchText;
	private string _gridTitle;
	private DataSourceCapabilities _crudCapabilities;
	private ICrudGridColumn[] _columnsBindings;
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
	private ICrudGridColumn[] _sizedColumnBindings;
	private int[] _columnMinimumWidths = Array.Empty<int>();
	private int _columnViewportWidth;
	private readonly Dictionary<ICrudGridColumn, int> _columnWidthOverrides = new();
	private readonly Dictionary<ICrudGridColumn, SourceGrid.Cells.Views.Cell> _imageCellViews = new();
	private readonly AsyncSynchronizedGate _bindingGate = new();
	private bool _refreshing;
	private bool _awaitingInitialBinding;
	private int _loadingIndicatorScopes;
	private int _bindingVersion;
	private IDataSource<object> _boundDataSource;
	private ICrudGridColumn[] _boundColumns;
	private int _sourceAssignmentVersion;
	private int _dataPublicationVersion;
	private IDataSource<object> _rowHeightDataSource;
	private ICrudGridColumn[] _rowHeightBindings;
	private int[] _rowHeightColumnWidths = Array.Empty<int>();
	private (int ImageHeight, bool HasText)[] _rowHeightSignatures = Array.Empty<(int, bool)>();
	private (System.Drawing.Font GridFont, System.Drawing.Font CellFont, int Dpi, int MinimumHeight, SourceGrid.Drawing.Padding Padding, System.Drawing.SizeF BorderExtent, object TextElement, object ImageElement, SourceGrid.Drawing.ElementsDrawMode DrawMode) _rowHeightGeometry;

	#endregion

	#region Constructors

	public CrudGrid() {
		State = VisualState.Normal;
		using (EnterVisualState(VisualState.Loading)) {
		_threadLock = new object();
			InitializeComponent();
			Disposed += (_, _) => ResetRowHeightMeasurements();
			_grid.MouseDown += _grid_MouseDown;
			_grid.KeyDown += _grid_KeyDown;
			_grid.Columns.ColumnWidthChanged += GridColumnWidthChanged;
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
			_pageSize = DefaultPageSize;
			_manualPageSize = DefaultPageSize;
			_pageSizeUpDown.Minimum = 1;
			_pageSizeUpDown.Maximum = 9999;
			_pageSizeUpDown.Value = _pageSize;
			_pageMode = CrudGridPageMode.Paged;
			_pageNumber = 0;
			_endPageNumber = 0;
			_totalRecords = 0;
			_rowToEntityMap = new Dictionary<int, object>();
			CalculateEntityToRowLookup();
			_gridTitle = string.Empty;
			_crudCapabilities = DataSourceCapabilities.Default;
			_columnsBindings = new ICrudGridColumn[0];
			_entityEditorAdapter = new CrudEntityEditorAdapter<object>();
			_entityEditorType = typeof(DefaultCrudEntityEditor);
			_gridContainerPanel.Resize += new EventHandler(_gridContainerPanel_Resize);
			LeftClickToDeselect = false;
			RightClickForContextMenu = true;
			UseEntityReferenceForLookup = false;
			AutoSelectOnCreate = false;
			_refreshThrottle = new Throttle(TimeSpan.FromMilliseconds(250));
			_grid.MinimumHeight = DefaultRowHeight;
			OrganizeLayout();
		}
		if (!Tools.Runtime.IsDesignMode) {
			_awaitingInitialBinding = true;
			State = VisualState.Loading;
			_layoutPanel.Visible = false;
			UpdateLoadingIndicator();
		}
	}

	#endregion

	#region Properties

	[Description("Allows editing a cell directly. With LeftClickToDeselect enabled, double-click the cell or press F2 to edit; otherwise a single click edits.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool AllowCellEditing {
		get => _allowCellEditing;
		set {
			if (_allowCellEditing == value)
				return;
			_allowCellEditing = value;
			UpdateCellInteraction();
		}
	}

	[Description("A single click toggles row selection. When AllowCellEditing is enabled, double-click an editable cell or press F2 to edit without deselecting the row.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool LeftClickToDeselect {
		get => _leftClickToDeselect;
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
	public bool RightClickForContextMenu { get; set; }

	[Description("When selecting a row, selection occurs on mouse up (as opposed to default behavior of mouse down)")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool SelectOnMouseUp {
		get => _selectOnMouseUp;
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
	public bool AutoSelectOnCreate { get; set; }

	[Description("Selects manual paging, viewport-fitted paging, or all matching rows in one scrollable page.")]
	[Category("Appearance")]
	[DefaultValue(CrudGridPageMode.Paged)]
	public CrudGridPageMode PageMode {
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
			SaveGridSettings(true);
			RefreshAutoPageSize(true);
		}
	}

	[Description("Compatibility alias for PageMode. True selects Auto; false selects Paged and restores the remembered manual size.")]
	[Category("Appearance")]
	[DefaultValue(false)]
	public bool AutoPageSize {
		get => PageMode == CrudGridPageMode.Auto;
		set => PageMode = value ? CrudGridPageMode.Auto : CrudGridPageMode.Paged;
	}

	[Description("Uses reference equality (in place of standard equality) when programatically selecting an entity.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool UseEntityReferenceForLookup { get; set; }

	[Description("Refreshes entire grid when a row is updated (will only refresh row when false)")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool RefreshEntireGridOnUpdate { get; set; }

	[Description("Refresh entire grid when a row is deleted (will only refresh row when false)")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool RefreshEntireGridOnDelete { get; set; }

	[Description("Refreshes immediately without a loading animation or disabling the grid. Logical refresh admission and stale-query protection remain active.")]
	[Category("Appearance")]
	[DefaultValue(false)]
	public bool SuppressLoadingIndicator { get; set; }

	[Description("Measures row heights on every refresh. Disable for frequently refreshed single-line, read-only text/image tables to retain measured heights; new rows and changes to layout, fonts or image heights are still measured. Multiline, wrapped, editable and custom cells always measure normally.")]
	[Category("Appearance")]
	[DefaultValue(true)]
	public bool AutoSizeRowsOnRefresh { get; set; } = true;

	[Description("Prepares read-only Text/Image pages on a worker, including source reads, enumeration, column delegates and display conversion. Opt in only when those operations and borrowed images are safe off the UI thread. Editable or other cell types retain their normal UI-thread binding.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool PrepareReadOnlyCellsInBackground { get; set; }

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object SelectedEntity {
		get { return _selectedEntity; }
		set {
			if (value == null && _selectedEntity != null) {
				RaiseEntityDeselectedEvent(_selectedEntity);
				_selectedEntity = null;
				HighlightSelectedEntity();
				return;
			}

			if (value == _selectedEntity) {
				return;
			}

			if (_selectedEntity != null) {
				RaiseEntityDeselectedEvent(_selectedEntity);
			}

			_selectedEntity = value;
			HighlightSelectedEntity();

			RaiseEntitySelectedEvent(_selectedEntity);
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Type EntityEditorDisplay { get; set; }

	[Category("Appearance")]
	[DefaultValue("")]
	public string GridTitle {
		get { return _gridTitle; }
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
		get => _titleLabel.TextAlign;
		set {
			Guard.Argument(Enum.IsDefined(value), nameof(value), "Select a supported title alignment.");
			_titleLabel.TextAlign = value;
		}
	}

	[Category("Behavior")]
	[DefaultValue(DataSourceCapabilities.Default)]
	public DataSourceCapabilities Capabilities {
		get { return _crudCapabilities; }
		set {
			_crudCapabilities = value;
			UpdateCellInteraction();
			OrganizeLayout();
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IEnumerable<ICrudGridColumn> GridBindings {
		get { return _columnsBindings; }
		set {
			_columnsBindings = (value ?? Enumerable.Empty<ICrudGridColumn>()).ToArray();
			_columnWidthOverrides.Clear();
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
		get { return _rowToEntityMap.Keys.Select(row => _rowToEntityMap[row]); }
	}

	internal object SelectedEntityDirect {
		get => _selectedEntity;
		set {
			_selectedEntity = value;
			UpdateDeleteButtonVisibility();
		}
	}

	internal Func<object, object> ResolvePublishedEntity { get; set; }

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IDictionary<string, CrudReferenceBinding> ReferenceBindings { get; } = new Dictionary<string, CrudReferenceBinding>(StringComparer.Ordinal);

	private VisualState State { get; set; }

	// A suppressed background read keeps the last committed view usable. Internal selection/commit
	// scopes remain noninteractive; logical refresh admission is independent of native row input.
	private bool CanInteractWithCommittedRows => State == VisualState.Normal
		|| State == VisualState.Loading && _refreshing && SuppressLoadingIndicator && !_awaitingInitialBinding
		&& _boundDataSource == _dataSource && _boundColumns == _columnsBindings && _crudCapabilities.HasFlag(DataSourceCapabilities.CanRead);

	#endregion

	#region Public Methods

	public Task SetDataSource<TEntity>(IDataSource<TEntity> dataSource) => SetDataSource(dataSource, DataSourceCapabilities.Default);

	public async Task SetDataSource<TEntity>(IDataSource<TEntity> dataSource, DataSourceCapabilities capabilities) {
		var assignmentVersion = ++_sourceAssignmentVersion;
		var projected = new ProjectedDataSource<TEntity, object>(dataSource, entity => entity, entity => (TEntity)entity);
		var available = await projected.CapabilitiesAsync;
		if (IsDisposed || Disposing || assignmentVersion != _sourceAssignmentVersion)
			return;
		_dataSource = projected;
		ResolvePublishedEntity = null;
		ResetRowHeightMeasurements();
		_crudCapabilities = available & capabilities;
		RestoreGridSettings();
		UpdateCellInteraction();
		OrganizeLayout();
	}

	public void ClearDataSource() {
		_sourceAssignmentVersion++;
		_dataSource = null;
		ResolvePublishedEntity = null;
		_columnWidthOverrides.Clear();
		ResetRowHeightMeasurements();
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
		CrudGridPreparedValue[][] prepared = null;
		if (prepare) {
			try {
				prepared = await Task.Run(async () => {
					await source.RefreshRangeAsync(refreshed).ConfigureAwait(false);
					return refreshed[0] == null ? null : PrepareReadOnlyRows(Enumerable.Repeat(refreshed[0], rows.Length).ToArray(), bindings, formats);
				});
			} catch when (IsDisposed || Disposing) {
				return false;
			}
		} else {
			await source.RefreshRangeAsync(refreshed);
		}
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

	/// <summary>Refreshes the selected row without rereading or resizing the current page.</summary>
	public Task<bool> RefreshSelectedEntityAsync() => _selectedEntity == null ? Task.FromResult(false) : RefreshEntityAsync(_selectedEntity);

	public void SetEntityEditor<TEntity>(Type entityEditorType) {
		if (!typeof(ICrudEntityEditor<TEntity>).IsAssignableFrom(entityEditorType) && !typeof(ICrudEntityEditor<object>).IsAssignableFrom(entityEditorType))
			throw new ArgumentException(string.Format("Does not implement {0}", typeof(ICrudEntityEditor<TEntity>)), "entityEditorType");
		_entityEditorAdapter = new CrudEntityEditorAdapter<TEntity>();
		_entityEditorType = entityEditorType;
	}

	public virtual async Task CreateNewRecord() {
		var newEntity = _dataSource.New();
		if (await ShowEntityEditor(newEntity, true) == CrudAction.Create) {
			await RefreshGrid();
			RaiseEntityCreatedEvent(newEntity);
			if (AutoSelectOnCreate)
				SelectedEntity = newEntity;
		}
	}

	public virtual async Task DeleteSelectedRecord() {
		if (_selectedEntity == null)
			return;
		await DeleteEntity(_selectedEntity);
	}

	public virtual async Task EditSelectedEntity() {
		if (_selectedEntity == null)
			return;
		await EditEntity(_selectedEntity);
	}

	public virtual async Task DeleteEntity(object entity) {
		if (await DialogEx.ShowAsync(this, SystemIconType.Question, "Confirm Delete", "Are you sure you want to delete this record?", "&No", "&Yes") == DialogExResult.Button2) {
			using (EnterVisualState(VisualState.Loading))
			using (EnterLoadingIndicator()) {
				var deleteValidationResult = await _dataSource.ValidateAsync(entity, CrudAction.Delete);
				if (deleteValidationResult.IsSuccess) {
					await _dataSource.DeleteAsync(entity);
					await UpdateGridAfterDelete(entity);
				} else {
					await DialogEx.ShowAsync(this, SystemIconType.Shield, "Validation Error", deleteValidationResult.ErrorMessages.ToParagraphCase(), "OK");
				}
			}
		}
	}

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

	protected virtual void OnEntitySelected(object selectedEntity) {
	}

	protected virtual void OnEntityDeselected(object deselectedEntity) {
	}
	
	protected virtual void OnEntityEditing(object selectedEntity, string propertyName, object oldValue, object newValue) {
	}

	protected virtual void OnEntityCreated(object deselectedEntity) {
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

	private void ConfigureReferenceBindings(DefaultCrudEntityEditor Editor, object Entity) {
		// Exact self references use this grid's datasource unless the model declares its own editor/converter.
		foreach (PropertyDescriptor Property in TypeDescriptor.GetProperties(Entity))
			if (Property.PropertyType == Entity.GetType() && Property.GetEditor(typeof(System.Drawing.Design.UITypeEditor)) == null
				&& Property.Converter.GetType() == typeof(TypeConverter))
				Editor.ReferenceBindings[Property.Name] = new CrudReferenceBinding<object>(_dataSource, _columnsBindings);
		foreach (var Column in _columnsBindings)
			if (Column.ReferenceBinding != null && !string.IsNullOrEmpty(Column.PropertyName))
				Editor.ReferenceBindings[Column.PropertyName] = Column.ReferenceBinding;
		foreach (var Binding in ReferenceBindings)
			Editor.ReferenceBindings[Binding.Key] = Binding.Value;
	}
	#endregion

	#region Layout

	private void OrganizeLayout() {
		_createButton.Visible = _createButton.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanCreate);
		_deleteButton.Enabled = _deleteToolStripMenuItem.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanDelete);
		UpdateDeleteButtonVisibility();
		_editToolStripMenuItem.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanUpdate);
		_searchTextBox.Enabled = _crudCapabilities.HasFlag(DataSourceCapabilities.CanSearch);

		UpdateButtonBarVisibility();
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
				CrudGridPageMode.Auto => CalculateAutoPageSize(),
				CrudGridPageMode.Single => int.MaxValue,
				_ => _manualPageSize
			};
			UpdatePageSizeDisplay();
			UpdatePageModeControl();
			_pageModeComboBox.Enabled = true;
			_manualPageSizePanel.Visible = _manualPageSizePanel.Enabled = PageMode == CrudGridPageMode.Paged;
			_pageNavigationPanel.Visible = _pageNavigationPanel.Enabled = PageMode != CrudGridPageMode.Single;
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
		_pageModeComboBox.SelectedIndex = (int)_pageMode;
	}

	private void UpdateDeleteButtonVisibility() {
		_deleteButton.Visible = _crudCapabilities.HasFlag(DataSourceCapabilities.CanDelete) && _selectedEntity != null &&
		                       _entityToRowLookup != null && _entityToRowLookup[_selectedEntity].Any(Row => _grid.Selection.IsSelectedRow(Row));
	}
	internal void NotifyDataSourceChanged() => _dataPublicationVersion++;

	internal void AutoSizeCells() {
		if (_sizedColumnBindings != _columnsBindings)
			return;
		_columnWidthOverrides.Clear();
		SizeBoundColumns();
		RememberRowHeightGeometry();
		RefreshAutoPageSize();
	}

	private void PageBarLayout(object sender, LayoutEventArgs args) {
		if (_layingOutPageBar || _pageSizeUpDown == null)
			return;
		_layingOutPageBar = true;
		using var restoreFlag = Tools.Scope.ExecuteOnDispose(() => _layingOutPageBar = false);
		// Keep every footer element on the same baseline at the current font and DPI.
		var height = Math.Max(_pageModeComboBox.PreferredHeight, Math.Max(_pageSizeUpDown.PreferredHeight, _pageNumberBox.PreferredHeight));
		var spacing = Math.Max(1, (int)Math.Round(3 * DeviceDpi / 96.0));
		foreach (var button in new[] { _firstPageButton, _previousPageButton, _nextPageButton, _lastPageButton }) {
			button.Size = new System.Drawing.Size(height, height);
			button.Margin = new Padding(spacing);
		}
		_pageSizeUpDown.Margin = _pageNumberBox.Margin = new Padding(spacing);
		_pageModeComboBox.Margin = new Padding(spacing, spacing, spacing * 2, spacing);
		_pageNavigationPanel.Margin = new Padding(spacing, 0, 0, 0);
		_totalRecordsPanel.Margin = new Padding(0, 0, spacing * 2, 0);
		_pageSizeUpDown.Width = TextRenderer.MeasureText("9999", Font).Width + SystemInformation.VerticalScrollBarWidth + 12;
		_pageModeComboBox.Width = _pageModeComboBox.Items.Cast<object>().Max(item => TextRenderer.MeasureText(item.ToString(), Font).Width)
			+ SystemInformation.VerticalScrollBarWidth + spacing * 4;
		_pageNumberBox.Width = TextRenderer.MeasureText(Math.Max(9999, _endPageNumber + 1).ToString(), Font).Width + 12;
		foreach (var control in new Control[] { label3, _totalRecordsLabel, _pageSizeLabel, _pageCountLabel })
			control.Margin = new Padding(spacing, Math.Max(0, (height - control.PreferredSize.Height) / 2) + spacing, spacing, spacing);
		// Narrow hosts put the right-aligned paging groups below Total, without clipping a group.
		var pagingWidth = _pageModeComboBox.Width + _pageModeComboBox.Margin.Horizontal;
		if (PageMode == CrudGridPageMode.Paged)
			pagingWidth += _manualPageSizePanel.PreferredSize.Width + _manualPageSizePanel.Margin.Horizontal;
		if (PageMode != CrudGridPageMode.Single)
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
			_grid.Columns[column].AutoSizeMode = _columnsBindings[column].ExpandsToFit
				? SourceGrid.AutoSizeMode.EnableAutoSize | SourceGrid.AutoSizeMode.EnableStretch : SourceGrid.AutoSizeMode.EnableAutoSize;
		_grid.AutoSizeCells();
		for (var column = 0; column < _grid.ColumnsCount; column++)
			_columnMinimumWidths[column] = _grid.Columns[column].Width;
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
		_columnViewportWidth = _grid.ClientSize.Width;
	}

	private void GridColumnWidthChanged(object sender, ColumnInfoEventArgs args) {
		var column = args.Column.Index;
		if (_sizingColumns || column < 0 || column >= _columnsBindings.Length)
			return;
		if (State == VisualState.Loading && PrepareReadOnlyCellsInBackground) {
			_columnWidthOverrides[_columnsBindings[column]] = args.Column.Width;
			args.Column.AutoSizeMode = SourceGrid.AutoSizeMode.None;
			return;
		}
		if (State != VisualState.Normal || column >= _columnMinimumWidths.Length)
			return;
		// A user's explicit width takes precedence over both natural sizing and viewport stretching.
		_columnWidthOverrides[_columnsBindings[column]] = args.Column.Width;
		_columnMinimumWidths[column] = args.Column.Width;
		_columnsSizedFromData = true;
		args.Column.AutoSizeMode = SourceGrid.AutoSizeMode.None;
	}

	private void ShowButtonBar() {
		_topPanel.Visible = true;
		_layoutPanel.RowStyles[0].SizeType = SizeType.AutoSize;
	}

	private void UpdateButtonBarVisibility() {
		// A disabled parent does not remove the title or the commands supported by its source.
		if (!string.IsNullOrEmpty(_gridTitle) || _crudCapabilities.HasFlag(DataSourceCapabilities.CanCreate)
			|| _crudCapabilities.HasFlag(DataSourceCapabilities.CanDelete) || _crudCapabilities.HasFlag(DataSourceCapabilities.CanSearch))
			ShowButtonBar();
		else
			HideButtonBar();
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

	public Task RefreshGrid() {
		RestoreGridSettings();
		if (_refreshTask is { IsCompleted: false })
			return _refreshTask;
		return _refreshTask = RefreshGridInternal();
	}

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
			using var animation = suppressLoading ? null : EnterLoadingIndicator();
			var previousEnabled = _grid.Enabled;
			using var restoreEnabled = Tools.Scope.ExecuteOnDispose(() => {
				if (!suppressLoading && !IsDisposed)
					_grid.Enabled = previousEnabled;
			});
			if (!suppressLoading)
				_grid.Enabled = false;
			var pageSizing = new AutoPageSizing();
			while (!IsDisposed) {
				while (await BindInternal(pageSizing)) ;
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

	private IDisposable EnterLoadingIndicator() {
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

	#endregion

	#region Selection & Misc

	private void InitializeGridSelectionMode() {
		_grid.SelectionMode = SourceGrid.GridSelectionMode.Row;
		_grid.Selection.EnableMultiSelection = false;
		_grid.Selection.FocusStyle = SourceGrid.FocusStyle.RemoveFocusCellOnLeave;
		_grid.Selection.SelectionChanged -= _grid_Selection_SelectionChanged;
		_grid.Selection.SelectionChanged += _grid_Selection_SelectionChanged;
		_grid.Selection.CellGotFocus -= _grid_Selection_CellGotFocus;
		_grid.Selection.CellGotFocus += _grid_Selection_CellGotFocus;
	}

	private void UpdateCellInteraction() {
		if (_grid == null || _rowToEntityMap == null)
			return;

		// CrudGrid owns row toggling; SourceGrid's Ctrl-click selection must not process the same press again.
		_mouseSelectionController.MouseButtons = LeftClickToDeselect ? MouseButtons.None : MouseButtons.Left;
		foreach (var Row in _rowToEntityMap) {
			for (var Column = 0; Column < Math.Min(_columnsBindings.Length, _grid.ColumnsCount); Column++) {
				var Cell = _grid[Row.Key, Column];
				if (Cell != null)
					ConfigureCellInteraction(Cell, _columnsBindings[Column], Row.Value);
			}
		}
	}

	private void ConfigureCellInteraction(ICell Cell, ICrudGridColumn Column, object Entity) {
		var CanEdit = AllowCellEditing && _crudCapabilities.HasFlag(DataSourceCapabilities.CanUpdate) && Column.CanEditCell && Column.CellHasValue(Entity);
		// Display-only text cells keep conversion support without constructing an unused native editor control.
		// Promote the existing cell when editing is enabled later; do not require another data-source read.
		if (CanEdit && Column.DisplayType == CrudCellDisplayType.Text && Cell.Editor is not SourceGrid.Cells.Editors.EditorControlBase)
			Cell.Editor = SourceGrid.Cells.Editors.Factory.Create(Column.DataType);
		if (Cell.Editor != null) {
			if (!CanEdit && Cell.Editor.IsEditing)
				new CellContext(_grid, new Position(Cell.Row.Index, Cell.Column.Index)).EndEdit(true);
			Cell.Editor.EnableEdit = CanEdit;
			Cell.Editor.EditableMode = LeftClickToDeselect ? EditableMode.None : EditableMode.Focus | EditableMode.SingleClick;
		}
		var valueController = Cell.FindController<UpdateEntityOnValueChangedController>();
		if (CanEdit && Cell.Editor != null && valueController == null)
			Cell.AddController(new UpdateEntityOnValueChangedController(this, _dataSource, Column, Entity));
		else if (!CanEdit && valueController != null)
			Cell.RemoveController(valueController);

		// Selection-only cells must not activate SourceGrid's focus-driven editor or row selection.
		Cell.RemoveController(SourceGrid.Cells.Controllers.Unselectable.Default);
		if (!AllowCellEditing || LeftClickToDeselect)
			Cell.AddController(SourceGrid.Cells.Controllers.Unselectable.Default);

		// Checkbox clicks normally change the value directly; reserve that action for the explicit edit gesture when rows toggle.
		if (Cell is SourceGrid.Cells.CheckBox) {
			Cell.RemoveController(SourceGrid.Cells.Controllers.CheckBox.Default);
			if (!LeftClickToDeselect)
				Cell.AddController(SourceGrid.Cells.Controllers.CheckBox.Default);
		}

		var RowSelector = Cell.FindController<SourceGrid.Cells.Controllers.RowSelector>();
		if (RowSelector != null)
			Cell.RemoveController(RowSelector);
		if (!LeftClickToDeselect)
			Cell.AddController(new SourceGrid.Cells.Controllers.RowSelector(!SelectOnMouseUp));
	}

	private void HighlightSelectedEntity() {
		using (EnterVisualState(VisualState.Selecting)) {
			_grid.Selection.ResetSelection(false);
			if (_selectedEntity != null && _entityToRowLookup.Contains(_selectedEntity)) {
				foreach (var rowNum in _entityToRowLookup[_selectedEntity]) {
					_grid.Selection.SelectRow(rowNum, true);
				}
			}
			UpdateDeleteButtonVisibility();
		}
	}
	

	private void SetVisiblePageNumberText(int number) {
		_updatingPageNumberText = true;
		using var restoreFlag = Tools.Scope.ExecuteOnDispose(() => _updatingPageNumberText = false);
		_pageNumberBox.Text = (number + 1).ToString();
	}

	internal async Task NotifyEntityUpdated(object entity) {
		if (_entityToRowLookup.Contains(entity))
			await UpdateGridAfterEdit(entity);
	}

	private async Task UpdateGridAfterEdit(object entity) {
		if (RefreshEntireGridOnUpdate) {
			await RefreshGrid();
		} else {
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

	private async Task UpdateGridAfterDelete(object entity) {
		var wasSelectedEntity = ReferenceEquals(entity, _selectedEntity);
		if (wasSelectedEntity)
			_selectedEntity = null; // set null here so as to avoid highlight when refreshing grid

		if (RefreshEntireGridOnDelete) {
			await RefreshGrid();
		} else {
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

	#endregion

	#region Paging

	private int GetVisiblePageNumberText() {
		return int.Parse(_pageNumberBox.Text) - 1;
	}

	private async Task ChangePageNumberAsync(int pageNumber) {
		_pageNumber = PageMode == CrudGridPageMode.Single || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanPage)
			? 0 : pageNumber.ClipTo(0, _endPageNumber);
		SetVisiblePageNumberText(_pageNumber);
		await RefreshGrid();
		SaveGridSettings(true);
	}

	private int CalculateAutoPageSize(AutoPageSizing sizing = null) {
		// Native row, header and viewport measurements already use the current window's device pixels.
		// Scale only the unmeasured fallback; a tall image must not inflate the budget for every ordinary row.
		var fallbackHeight = Math.Max(_grid.MinimumHeight, (int)Math.Round(DefaultRowHeight * _grid.DeviceDpi / 96.0));
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

	#endregion

	#region Binding

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
		bool QueryChanged() => dataSource != _dataSource || dataPublicationVersion != _dataPublicationVersion || columns != _columnsBindings || searchText != _searchText || pageSize != _pageSize
			|| pageNumber != _pageNumber || sortColumnName != _sortColumnName || sortDirection != _sortDirection || prepareInBackground != PrepareReadOnlyCellsInBackground
			|| referenceLookup != UseEntityReferenceForLookup || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead);
		if (pageSize == 0) {
			return false;
		}


		// Take care of null grid
		if (_columnsBindings == null || _dataSource == null || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead)) {
			ResetRowHeightMeasurements();
			_boundDataSource = null;
			_boundColumns = null;
			_rowToEntityMap.Clear();
			CalculateEntityToRowLookup();
			_grid.Redim(0, 0);
			_sizedColumnBindings = null;
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
		CrudGridPreparedValue[][] prepared = null;
		CrudGridPreparedCell[][] newCells = null;
		if (prepareCells) {
			try {
				var result = await Task.Run(async () => {
					var read = await dataSource.ReadRangeAsync(searchText, pageSize, pageNumber, sortColumnName, sortDirection).ConfigureAwait(false);
					var entities = read.Items.ToArray();
					var visible = entities.Take(pageSize).ToArray();
					var rows = visible.Select((entity, index) => (Entity: entity, Row: index + 1)).ToDictionary(item => item.Row, item => item.Entity);
					var lookup = referenceLookup ? rows.InverseUsingValueReferenceAsKey() : rows.Inverse();
					var values = PrepareReadOnlyRows(visible, columns, formats);
					var cells = new CrudGridPreparedCell[values.Length][];
					for (var row = reusableRowCount; row < values.Length; row++)
						cells[row] = values[row].Select(value => new CrudGridPreparedCell(value)).ToArray();
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
		bool AcceptedBindingIsCurrent() => !IsDisposed && !Disposing && !QueryChanged() && _bindingVersion == acceptedVersion
			&& CanPrepareReadOnlyCells(columns) && !columns.Where((column, index) => (column.DisplayType, column.DataType) != formats[index]).Any();

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
				} else if (prepared == null) {
					SizeBoundRows();
				}
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
		}
		_bottomPanel.PerformLayout();
		if (prepared != null)
			_grid.Invalidate();

		// Reconcile the estimate with the actual rows, allowing both growth and shrinkage under one bounded settle cycle.
		if (AutoPageSize && _crudCapabilities.HasFlag(DataSourceCapabilities.CanPage)) {
			_pageSize = CalculateAutoPageSize(pageSizing);
			UpdatePageSizeDisplay();
		}

		return QueryChanged();
	}

	private bool CanPrepareReadOnlyCells(ICrudGridColumn[] columns)
		=> PrepareReadOnlyCellsInBackground && columns.All(column => column.DisplayType is CrudCellDisplayType.Text or CrudCellDisplayType.Image)
			&& (!AllowCellEditing || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanUpdate) || columns.All(column => !column.CanEditCell));

	private async Task<bool> SizeBoundColumnsAsync(Func<bool> isCurrent) {
		var columns = _columnsBindings;
		var widths = Enumerable.Range(0, _grid.ColumnsCount).Select(column => _grid.Columns[column].Width).ToArray();
		var geometryWidths = (int[])widths.Clone();
		var rowCount = _grid.RowsCount;
		var geometry = GetRowHeightGeometry();
		var headerFont = SourceGrid.Cells.Views.ColumnHeader.Default.Font;
		var minimumWidth = _grid.MinimumWidth;
		var signatures = new (int ImageHeight, bool HasText)[rowCount];
		bool Current() => isCurrent() && rowCount == _grid.RowsCount && widths.Length == _grid.ColumnsCount
			&& geometry.Equals(GetRowHeightGeometry()) && Equals(headerFont, SourceGrid.Cells.Views.ColumnHeader.Default.Font)
			&& minimumWidth == _grid.MinimumWidth && NativeWidthsAreCurrent(columns, geometryWidths);
		_grid.AutoStretchColumnsToFitWidth = false;
		for (var column = 0; column < columns.Length; column++)
			_grid.Columns[column].AutoSizeMode = _columnWidthOverrides.ContainsKey(columns[column]) ? SourceGrid.AutoSizeMode.None
				: columns[column].ExpandsToFit ? SourceGrid.AutoSizeMode.EnableAutoSize | SourceGrid.AutoSizeMode.EnableStretch : SourceGrid.AutoSizeMode.EnableAutoSize;
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

	private async Task<bool> SizeBoundRowsAsync(Func<bool> isCurrent) {
		var rowCount = _grid.RowsCount;
		var columns = _columnsBindings;
		var widths = Enumerable.Range(0, _grid.ColumnsCount).Select(column => _grid.Columns[column].Width).ToArray();
		var geometry = GetRowHeightGeometry();
		var autoSize = AutoSizeRowsOnRefresh;
		var full = autoSize || _rowHeightDataSource != _dataSource || _rowHeightBindings != columns
			|| !_rowHeightGeometry.Equals(geometry) || !_rowHeightColumnWidths.SequenceEqual(widths);
		var signatures = new (int ImageHeight, bool HasText)[rowCount];
		var measure = new bool[rowCount];
		bool Current() => isCurrent() && rowCount == _grid.RowsCount && widths.Length == _grid.ColumnsCount
			&& geometry.Equals(GetRowHeightGeometry()) && autoSize == AutoSizeRowsOnRefresh && NativeWidthsAreCurrent(columns, widths);
		if (!await RunNativeSizingAsync(MeasureRows(), Current))
			return false;
		if (full)
			RememberRowHeightGeometry(signatures);
		else {
			_rowHeightSignatures = signatures;
			_rowHeightColumnWidths = widths;
		}
		return true;

		IEnumerable<bool> MeasureRows() {
			// Signature inspection is part of the bounded native work too; a large page must not
			// monopolize the dispatcher before the existing yielding measurement loop begins.
			for (var row = 0; row < rowCount; row++) {
				var signature = GetRowHeightSignature(row);
				signatures[row] = signature;
				measure[row] = full || signature.ImageHeight < 0 || row >= _rowHeightSignatures.Length || signature != _rowHeightSignatures[row];
				yield return true;
			}
			foreach (var step in MeasureNativeRows(measure))
				yield return step;
		}
	}

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

	private bool NativeWidthsAreCurrent(ICrudGridColumn[] columns, int[] expected) {
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

	private async Task<bool> RunNativeSizingAsync(IEnumerable<bool> steps, Func<bool> isCurrent) {
		using var timer = new System.Windows.Forms.Timer { Interval = 1 };
		TaskCompletionSource resume = null;
		timer.Tick += (_, _) => { timer.Stop(); resume?.TrySetResult(); };
		void DisposedWhileSizing(object sender, EventArgs args) => resume?.TrySetResult();
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

	private static CrudGridPreparedValue[][] PrepareReadOnlyRows(object[] entities, ICrudGridColumn[] columns, (CrudCellDisplayType DisplayType, Type DataType)[] formats) {
		// Conversion metadata is initialized once per populated text column on the worker; rows carry only values and prepared strings.
		var editors = new CrudGridPreparedTextEditor[columns.Length];
		var rows = new CrudGridPreparedValue[entities.Length][];
		for (var row = 0; row < entities.Length; row++) {
			rows[row] = new CrudGridPreparedValue[columns.Length];
			for (var column = 0; column < columns.Length; column++) {
				var hasValue = columns[column].CellHasValue(entities[row]);
				var value = hasValue ? columns[column].GetCellValue(entities[row]) : null;
				var editor = editors[column];
				if (hasValue && formats[column].DisplayType == CrudCellDisplayType.Text && editor == null)
					editor = editors[column] = new CrudGridPreparedTextEditor(formats[column].DataType);
				if (hasValue && formats[column].DisplayType == CrudCellDisplayType.Image)
					Guard.Argument(value == null || value is System.Drawing.Image, nameof(value), "An image cell requires a System.Drawing.Image value.");
				var displayText = hasValue && editor != null ? editor.PrepareValue(value) : string.Empty;
				rows[row][column] = new CrudGridPreparedValue(hasValue, value, displayText, editor);
			}
		}
		return rows;
	}

	private void BindPreparedRow(int row, object entity, CrudGridPreparedValue[] cells, bool reuse, CrudGridPreparedCell[] newCells = null) {
		_rowToEntityMap[row] = entity;
		for (var column = 0; column < cells.Length; column++) {
			var prepared = cells[column];
			var view = GetColumnCellView(_columnsBindings[column]);
			if (reuse && _grid[row, column] is CrudGridPreparedCell existing && existing.CanApplyPreparedValue && existing.View == view
				&& existing.RowSpan == 1 && existing.ColumnSpan == 1 && existing.Editor is not SourceGrid.Cells.Editors.EditorControlBase
				&& existing.FindController<UpdateEntityOnValueChangedController>() == null) {
				// Retain the native cell's complete graph; publishing new data does not allocate or fire edit callbacks.
				existing.ApplyPreparedValue(prepared);
			} else {
				var cell = newCells?[column] ?? new CrudGridPreparedCell(prepared);
				cell.View = view;
				ConfigureCellInteraction(cell, _columnsBindings[column], entity);
				_grid[row, column] = cell;
			}
		}
	}

	private void SizeBoundRows() {
		if (AutoSizeRowsOnRefresh || _rowHeightDataSource != _dataSource || _rowHeightBindings != _columnsBindings
			|| !_rowHeightGeometry.Equals(GetRowHeightGeometry()) || _rowHeightColumnWidths.Length != _grid.ColumnsCount
			|| Enumerable.Range(0, _grid.ColumnsCount).Any(column => _rowHeightColumnWidths[column] != _grid.Columns[column].Width)) {
			_grid.Rows.AutoSize(false);
			RememberRowHeightGeometry();
			return;
		}
		// Redim preserves existing RowInfo instances. Keep their actual measured heights, rather than imposing a fixed height.
		var signatures = new (int ImageHeight, bool HasText)[_grid.RowsCount];
		for (var row = 0; row < _grid.RowsCount; row++) {
			var signature = GetRowHeightSignature(row);
			if (signature.ImageHeight < 0 || row >= _rowHeightSignatures.Length || signature != _rowHeightSignatures[row])
				_grid.Rows.AutoSizeRow(row, false, 0, _grid.ColumnsCount - 1);
			signatures[row] = signature;
		}
		_rowHeightSignatures = signatures;
	}

	private void RememberRowHeightGeometry((int ImageHeight, bool HasText)[] signatures = null) {
		if (AutoSizeRowsOnRefresh) {
			ResetRowHeightMeasurements();
			return;
		}
		_rowHeightDataSource = _dataSource;
		_rowHeightBindings = _columnsBindings;
		_rowHeightGeometry = GetRowHeightGeometry();
		_rowHeightColumnWidths = Enumerable.Range(0, _grid.ColumnsCount).Select(column => _grid.Columns[column].Width).ToArray();
		_rowHeightSignatures = signatures ?? Enumerable.Range(0, _grid.RowsCount).Select(GetRowHeightSignature).ToArray();
	}

	private void ResetRowHeightMeasurements() {
		_rowHeightDataSource = null;
		_rowHeightBindings = null;
		_rowHeightColumnWidths = Array.Empty<int>();
		_rowHeightSignatures = Array.Empty<(int, bool)>();
		_rowHeightGeometry = default;
	}

	private (System.Drawing.Font, System.Drawing.Font, int, int, SourceGrid.Drawing.Padding, System.Drawing.SizeF, object, object, SourceGrid.Drawing.ElementsDrawMode) GetRowHeightGeometry() {
		var view = SourceGrid.Cells.Views.Cell.Default;
		return (_grid.Font, view.Font, _grid.DeviceDpi, _grid.MinimumHeight, view.Padding, view.Border?.GetExtent(System.Drawing.SizeF.Empty) ?? System.Drawing.SizeF.Empty, view.ElementText, view.ElementImage, view.ElementsDrawMode);
	}

	private (int ImageHeight, bool HasText) GetRowHeightSignature(int row) {
		var view = SourceGrid.Cells.Views.Cell.Default;
		if (row == 0 || view.WordWrap || view.ElementText?.GetType() != typeof(SourceGrid.Drawing.VisualElements.TextGDI)
			|| view.ElementImage?.GetType() != typeof(SourceGrid.Drawing.VisualElements.Image))
			return (-1, false);
		var imageHeight = 0;
		var hasText = false;
		for (var column = 0; column < _grid.ColumnsCount; column++) {
			if (_columnsBindings[column].DisplayType is not (CrudCellDisplayType.Text or CrudCellDisplayType.Image)
				|| _grid[row, column] is not Cell cell || cell.GetType() != typeof(Cell) && cell is not CrudGridPreparedCell
				|| cell.View != GetColumnCellView(_columnsBindings[column]) || cell.Editor is SourceGrid.Cells.Editors.EditorControlBase)
				return (-1, false);
			var cellView = (SourceGrid.Cells.Views.Cell)cell.View;
			if (cellView != view && (cellView.WordWrap || !Equals(cellView.Font, view.Font) || !ReferenceEquals(cellView.Border, view.Border)
				|| cellView.Padding.Left != view.Padding.Left || cellView.Padding.Right != view.Padding.Right
				|| cellView.Padding.Top != view.Padding.Top || cellView.Padding.Bottom != view.Padding.Bottom
				|| cellView.ElementsDrawMode != view.ElementsDrawMode || cellView.ElementText != view.ElementText))
				return (-1, false);
			var text = new CellContext(_grid, new Position(row, column)).DisplayText;
			if (text.AsSpan().IndexOfAny("\r\n\u0085\u2028\u2029") >= 0)
				return (-1, false);
			hasText |= !string.IsNullOrEmpty(text);
			imageHeight = Math.Max(imageHeight, cell.Image?.Height ?? 0);
		}
		return (imageHeight, hasText);
	}


	private void BindColumnHeaders(int col) {
		_grid[0, col] = new SourceGrid.Cells.ColumnHeader(_columnsBindings[col].ColumnName);
		_grid[0, col].AddController(new CustomSortHeaderCellController(this));

		// if we are sorting by this column, set the sort icon on the column
		if (_sortDirection != SortDirection.None && _sortColumnName != null && _sortColumnIndex == col) {
			((SourceGrid.Cells.Models.ISortableHeader)_grid[0, col].Model.FindModel(typeof(SourceGrid.Cells.Models.ISortableHeader))).SetSortMode(
				SourceGrid.CellContext.Empty,
				_sortDirection == SortDirection.Ascending ? Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.HeaderSortStyle.Ascending : Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.HeaderSortStyle.Descending
			);
		}
	}

	private void BindRowInternal(int row, object entity) {
		_rowToEntityMap[row] = entity;
		for (var col = 0; col < _columnsBindings.Length; col++) {
			var columnBinding = _columnsBindings[col];
			_grid[row, col] = CreateCell(columnBinding, entity);
		}
	}

	private ICell CreateCell(ICrudGridColumn column, object entity) {
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

		} else {
			cell = CreateEmptyCell();
		}

		if (column.DisplayType == CrudCellDisplayType.Image)
			cell.View = GetColumnCellView(column);
		ConfigureCellInteraction(cell, column, entity);
		return cell;
	}

	private SourceGrid.Cells.Views.Cell GetColumnCellView(ICrudGridColumn column) {
		var standard = SourceGrid.Cells.Views.Cell.Default;
		if (column.DisplayType != CrudCellDisplayType.Image)
			return standard;
		Guard.Argument(Enum.IsDefined(column.ImageAlignment), nameof(column.ImageAlignment), "Select a supported image alignment.");
		Guard.Argument(!column.ImageResizeMethod.HasValue || Enum.IsDefined(column.ImageResizeMethod.Value), nameof(column.ImageResizeMethod), "Select a supported image resize method.");
		if (!_imageCellViews.TryGetValue(column, out var view) || view.ElementImage is not SourceGrid.Drawing.VisualElements.Image) {
			// Share one view per image binding, including the ordinary and prepared-cell paths.
			// Borrow default styling without cloning its transient image model or owned fonts.
			view = new SourceGrid.Cells.Views.Cell {
				Font = standard.Font, ForeColor = standard.ForeColor, BackColor = standard.BackColor,
				Border = standard.Border, Padding = standard.Padding, WordWrap = standard.WordWrap,
				TrimmingMode = standard.TrimmingMode, TextAlignment = standard.TextAlignment,
				ElementsDrawMode = standard.ElementsDrawMode, ElementText = standard.ElementText
			};
			_imageCellViews[column] = view;
		}
		view.ImageAlignment = column.ImageAlignment switch {
			Sphere10.Framework.ResizeAlignment.TopLeft => SourceGrid.Drawing.ContentAlignment.TopLeft,
			Sphere10.Framework.ResizeAlignment.TopCenter => SourceGrid.Drawing.ContentAlignment.TopCenter,
			Sphere10.Framework.ResizeAlignment.TopRight => SourceGrid.Drawing.ContentAlignment.TopRight,
			Sphere10.Framework.ResizeAlignment.CenterLeft => SourceGrid.Drawing.ContentAlignment.MiddleLeft,
			Sphere10.Framework.ResizeAlignment.CenterCenter => SourceGrid.Drawing.ContentAlignment.MiddleCenter,
			Sphere10.Framework.ResizeAlignment.CenterRight => SourceGrid.Drawing.ContentAlignment.MiddleRight,
			Sphere10.Framework.ResizeAlignment.BottomLeft => SourceGrid.Drawing.ContentAlignment.BottomLeft,
			Sphere10.Framework.ResizeAlignment.BottomCenter => SourceGrid.Drawing.ContentAlignment.BottomCenter,
			_ => SourceGrid.Drawing.ContentAlignment.BottomRight
		};
		view.ImageStretch = column.ImageResizeMethod.HasValue;
		var image = (SourceGrid.Drawing.VisualElements.Image)view.ElementImage;
		image.ResizeMethod = column.ImageResizeMethod;
		image.ResizeAlignment = column.ImageAlignment;
		return view;
	}

	private Cell CreateEmptyCell() {
		return new Cell(string.Empty);
	}

	private Cell CreateTextBoxCell(ICrudGridColumn columnBinding, object entity, object cellValue) {
		if (AllowCellEditing && _crudCapabilities.HasFlag(DataSourceCapabilities.CanUpdate) && columnBinding.CanEditCell && columnBinding.CellHasValue(entity))
			return new Cell(cellValue, columnBinding.DataType);
		// EditorBase uses the same type converter as the visual editor, without EditorControlBase.CreateControl().
		var display = new SourceGrid.Cells.Editors.EditorBase(columnBinding.DataType) { EnableEdit = false, EditableMode = EditableMode.None };
		return new Cell(cellValue, display);
	}

	private Cell CreateCheckBoxCell(ICrudGridColumn columnBinding, object entity, object cellValue) {
		var checkbox = new SourceGrid.Cells.CheckBox(null, (bool?)cellValue) {
			Editor = { EnableEdit = AllowCellEditing && columnBinding.CanEditCell }
		};
		return checkbox;
	}

	private Cell CreateCurrencyCell(ICrudGridColumn columnBinding, object entity, object cellValue) {
		return new Cell(cellValue, new SourceGrid.Cells.Editors.TextBoxCurrency(columnBinding.DataType));
	}

	private Cell CreateNumericCell(ICrudGridColumn columnBinding, object entity, object cellValue) {
		return new Cell(cellValue, new SourceGrid.Cells.Editors.TextBoxNumeric(columnBinding.DataType));
	}

	private Cell CreateDateTimeCell(ICrudGridColumn columnBinding, object entity, object cellValue) {
		const DateTimeStyles dtStyles = System.Globalization.DateTimeStyles.AllowInnerWhite | System.Globalization.DateTimeStyles.AllowLeadingWhite | System.Globalization.DateTimeStyles.AllowTrailingWhite |
		                                System.Globalization.DateTimeStyles.AllowWhiteSpaces;
		var editor = columnBinding.DisplayType == CrudCellDisplayType.Time ? new SourceGrid.Cells.Editors.TimePicker() : new SourceGrid.Cells.Editors.DateTimePicker();
		editor.AllowNull = columnBinding.DataType == typeof(DateTime?);
		var customFormat = columnBinding.GetDateTimeFormat(entity);
		editor.Control.CustomFormat = customFormat;
		var dtParseFormats = new string[] { customFormat };
		var dtConverter = new Sphere10.Framework.Windows.Forms.SourceGrid.ComponentModel.Converter.DateTimeTypeConverter(customFormat, dtParseFormats, dtStyles);
		editor.TypeConverter = dtConverter;
		return new Cell(cellValue, editor);
	}

	private Cell CreateDropDownListCell(ICrudGridColumn columnBinding, object entity, object cellValue) {
		if (columnBinding.ReferenceBinding != null)
			return new Cell(cellValue, new SourceGrid.Cells.Editors.CrudReference(columnBinding.DataType, columnBinding.ReferenceBinding));
		var editor = new SourceGrid.Cells.Editors.DropDownList(columnBinding.DataType, columnBinding.DropDownItemDisplayMember);
		editor.Control.ValueMember = columnBinding.DropDownItemDisplayMember;
		editor.Control.DisplayMember = columnBinding.DropDownItemDisplayMember;
		editor.EditException += async (o, e) => {
			e.Handled = true;
			editor.UndoEditValue();
			await ExceptionDialog.ShowAsync(this, e.Exception);
		};
		var cell = new Cell(columnBinding.DataType, editor);
		editor.Control.DropDownStyle = ComboBoxStyle.DropDownList;
		cell.AddController(new AutoPopulateDropDownListOnEditStarting(editor, columnBinding, entity));
		cell.Value = cellValue;
		return cell;
	}

	private Cell CreateButtonCell(ICrudGridColumn columnBinding, object entity, object cellValue) {
		return CreateButtonCell(columnBinding, entity, columnBinding.GetButtonCaption(entity), columnBinding.GetButtonImage(entity), columnBinding.ButtonPressed);
	}

	private Cell CreateCommandCell(ICrudGridColumn columnBinding, object entity, CrudAction action) {
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
		return CreateButtonCell(columnBinding, entity, string.Empty, image, async Entity => {
			try {
				await callback(Entity);
			} catch (Exception Error) {
				await ExceptionDialog.ShowAsync(this, Error);
			}
		});
	}

	private Cell CreateButtonCell(ICrudGridColumn columnBinding, object entity, string caption, System.Drawing.Image image, Action<object> clickHandler) {
		var button = new SourceGrid.Cells.Button(caption) {
			Image = image
		};
		var clickHandlerController = new SourceGrid.Cells.Controllers.Button();
		clickHandlerController.Executed += (o, e) => clickHandler(entity);
		button.AddController(clickHandlerController);
		return button;
	}

	private void RemoveGridRow(int row) {
		if (row == 0)
			throw new ArgumentOutOfRangeException("Cannot remove header row (at 0)", "row");


		// Remove from the lookup tables
		_rowToEntityMap.Remove(row);
		var followingRows = new List<object>();
		for (int i = row + 1; i < _grid.RowsCount; i++) {
			followingRows.Add(_rowToEntityMap[i]);
			_rowToEntityMap.Remove(i);
		}
		foreach (var rowObject in followingRows.WithDescriptions()) {
			_rowToEntityMap[rowObject.Index + row] = rowObject.Item;
		}

		// Remove the grid row
		_grid.Rows.Remove(row);

		// Recalculate the entity-to-row lookup
		CalculateEntityToRowLookup();
	}

	private void CalculateEntityToRowLookup() {
		_entityToRowLookup = UseEntityReferenceForLookup ? _rowToEntityMap.InverseUsingValueReferenceAsKey() : _rowToEntityMap.Inverse();
	}

	#endregion

	#region Event Triggers

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

	protected void RaiseEntityCreatedEvent(object createdEntity) {
		if (this.CanRaiseEvents) {
			OnEntityCreated(createdEntity);
			if (EntityCreated != null)
				EntityCreated(this, createdEntity);
		}
	}

	protected void RaiseEntityUpdatedEvent(object updatedEntity) {
		if (this.CanRaiseEvents) {
			OnEntityUpdated(updatedEntity);
			if (EntityUpdated != null)
				EntityUpdated(this, updatedEntity);
		}
	}

	protected void RaiseEntityDeletedEvent(object deletedEntity) {
		if (this.CanRaiseEvents) {
			OnEntityDeleted(deletedEntity);
			if (EntityDeleted != null)
				EntityDeleted(this, deletedEntity);
		}
	}

	#endregion

	#region Event Handlers

	private void LoadingIndicatorResized(object sender, EventArgs args) {
		// Initial loading and subsequent refreshes share this full-viewport circle and geometry.
		var sideLength = Math.Max(1, Math.Min(_initialLoadingCircle.ClientSize.Width, _initialLoadingCircle.ClientSize.Height) - 1);
		_initialLoadingCircle.InnerCircleRadius = Math.Max(1, sideLength / 5);
		_initialLoadingCircle.OuterCircleRadius = Math.Max(1, sideLength / 3);
		_initialLoadingCircle.SpokeThickness = Math.Max(1, _initialLoadingCircle.InnerCircleRadius / 3);
	}

	private void _gridContainerPanel_Resize(object Sender, EventArgs Args) {
		RefreshAutoPageSize();
	}

	private void GridViewportChanged(object sender, EventArgs args) {
		if (_grid == null)
			return;
		if (!_sizingColumns && _columnViewportWidth != _grid.ClientSize.Width)
			StretchColumnsToViewport();
		RefreshAutoPageSize();
	}

	private void GridMetricsChanged(object sender, EventArgs args) {
		if (AutoPageSize)
			RefreshAutoPageSize(true);
	}

	private void PageModeSelectedIndexChanged(object sender, EventArgs args) {
		if (!_updatingPageModeControl && _pageModeComboBox.SelectedIndex >= 0)
			PageMode = (CrudGridPageMode)_pageModeComboBox.SelectedIndex;
	}

	private async void RefreshAutoPageSize(bool forceRefresh = false) {
		try {
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
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				await ExceptionDialog.ShowAsync(this, error);
		}
	}

	internal async void _grid_SortColumnPressed(int col) {
		try {
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
			await RefreshGrid();
			SaveGridSettings(true);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _pageSizeUpDown_ValueChanged(object sender, EventArgs e) {
		try {
			if (_restoringGridSettings || _updatingPageSizeDisplay || PageMode != CrudGridPageMode.Paged || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanPage))
				return;
			_manualPageSize = _pageSize = (int)_pageSizeUpDown.Value;
			await RefreshGrid();
			SaveGridSettings(true);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _searchTextBox_TextChanged(object sender, EventArgs e) {
		try {
			_searchText = _searchTextBox.Text;
			await ChangePageNumberAsync(0);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _firstPageButton_Click(object sender, EventArgs e) {
		try {
			if (!CanInteractWithCommittedRows)
				return;

			await ChangePageNumberAsync(0);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _previousPageButton_Click(object sender, EventArgs e) {
		try {
			if (!CanInteractWithCommittedRows)
				return;

			await ChangePageNumberAsync(_pageNumber - 1);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _nextPageButton_Click(object sender, EventArgs e) {
		try {
			if (!CanInteractWithCommittedRows)
				return;

			await ChangePageNumberAsync(_pageNumber + 1);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _lastPageButton_Click(object sender, EventArgs e) {
		try {
			if (!CanInteractWithCommittedRows)
				return;

			await ChangePageNumberAsync(_endPageNumber);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _pageNumberBox_ValueChanged(object sender, EventArgs e) {
		try {
			if (_updatingPageNumberText || State == VisualState.Selecting)
				return;

			await ChangePageNumberAsync(GetVisiblePageNumberText());
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _createButton_Click(object sender, EventArgs e) {
		try {
			if (State != VisualState.Normal)
				return;

			await CreateNewRecord();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _deleteButton_Click(object sender, EventArgs e) {
		try {
			if (State != VisualState.Normal)
				return;

			await DeleteSelectedRecord();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _grid_Selection_SelectionChanged(object sender, SourceGrid.RangeRegionChangedEventArgs e) {
		try {
			if (!CanInteractWithCommittedRows)
				return;

			var SelectedRows = _grid.Selection.GetSelectionRegion().GetRowsIndex();
			var Entity = SelectedRows.Where(_rowToEntityMap.ContainsKey).Select(Row => _rowToEntityMap[Row]).FirstOrDefault();
			if (ReferenceEquals(Entity, _selectedEntity))
				return;

			var PreviousEntity = _selectedEntity;
			_selectedEntity = Entity;
			if (PreviousEntity != null)
				RaiseEntityDeselectedEvent(PreviousEntity);
			if (Entity != null)
				RaiseEntitySelectedEvent(Entity);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _grid_Selection_CellGotFocus(SourceGrid.Selection.SelectionBase sender, SourceGrid.ChangeActivePositionEventArgs e) {
		try {
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _grid_MouseDoubleClick(object sender, MouseEventArgs e) {
		try {
			if (!CanInteractWithCommittedRows)
				return;
			if (LeftClickToDeselect && e.Button == MouseButtons.Left) {
				if (!TryStartCellEdit(_grid.PositionAtPoint(e.Location)))
					ToggleRowSelection(e);
			} else if (!LeftClickToDeselect)
				await EditSelectedEntity();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _grid_MouseDown(object Sender, MouseEventArgs Event) {
		try {
			_selectedRowOnMouseDown = -1;
			if (!CanInteractWithCommittedRows)
				return;

			var Position = _grid.PositionAtPoint(Event.Location);
			if (Position.IsEmpty() || !_rowToEntityMap.ContainsKey(Position.Row))
				return;

			if (Event.Button == MouseButtons.Left)
				_lastClickedCell = Position;
			if (LeftClickToDeselect)
				_grid.Focus(false);

			// Remember the selection before SourceGrid processes the press, regardless of its duration.
			if (Event.Button == MouseButtons.Left && _grid.Selection.IsSelectedRow(Position.Row))
				_selectedRowOnMouseDown = Position.Row;
			if (Event.Button == MouseButtons.Right || LeftClickToDeselect && Event.Button == MouseButtons.Left && !SelectOnMouseUp)
				_grid.Selection.SelectRow(Position.Row, true);
		} catch (Exception Error) {
			await ExceptionDialog.ShowAsync(this, Error);
		}
	}

	private async void _grid_MouseClick(object sender, MouseEventArgs e) {
		try {
			switch (e.Button) {
				case MouseButtons.Left:
					if (LeftClickToDeselect)
						ToggleRowSelection(e);
					break;
				case MouseButtons.Right:
					var position = _grid.PositionAtPoint(e.Location);
					if (CanInteractWithCommittedRows && RightClickForContextMenu && !position.IsEmpty()
						&& _rowToEntityMap.ContainsKey(position.Row) && _grid.Selection.IsSelectedRow(position.Row))
						_selectionContextMenuStrip.Show(_grid.PointToScreen(e.Location));
					break;
			}
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _grid_KeyDown(object Sender, KeyEventArgs Event) {
		try {
			if (!LeftClickToDeselect || !AllowCellEditing || Event.KeyCode != Keys.F2)
				return;

			var Position = _lastClickedCell;
			if (Position.IsEmpty() || !_grid.Selection.IsSelectedRow(Position.Row)) {
				var Row = _grid.Selection.GetSelectionRegion().GetRowsIndex().FirstOrDefault(_rowToEntityMap.ContainsKey, -1);
				if (Row < 0)
					return;
				var Column = Enumerable.Range(0, _grid.ColumnsCount).FirstOrDefault(Index => _grid[Row, Index]?.Editor?.EnableEdit == true, -1);
				if (Column < 0)
					return;
				Position = new Position(Row, Column);
			}
			Event.Handled = TryStartCellEdit(Position);
			Event.SuppressKeyPress = Event.Handled;
		} catch (Exception Error) {
			await ExceptionDialog.ShowAsync(this, Error);
		}
	}

	private bool TryStartCellEdit(Position Position) {
		if (State != VisualState.Normal || Position.IsEmpty() || !_rowToEntityMap.ContainsKey(Position.Row) || Position.Column >= _grid.ColumnsCount)
			return false;

		var Context = new CellContext(_grid, Position);
		if (Context.Cell?.Editor?.EnableEdit != true)
			return false;
		if (Context.IsEditing())
			return true;

		_grid.Selection.SelectRow(Position.Row, true);
		Context.Cell.RemoveController(SourceGrid.Cells.Controllers.Unselectable.Default);
		using var RestoreFocusBehavior = new ActionDisposable(() => {
			if (LeftClickToDeselect)
				Context.Cell.AddController(SourceGrid.Cells.Controllers.Unselectable.Default);
		});
		// Activating an editor resets SourceGrid's focus internally without changing the selected entity.
		using (EnterVisualState(VisualState.Selecting))
			Context.StartEdit();
		if (Context.Cell is SourceGrid.Cells.CheckBox)
			return Context.Cell.Editor.SetCellValue(Context, !((bool?)Context.Value ?? false));
		return Context.IsEditing();
	}

	private void ToggleRowSelection(MouseEventArgs Event) {
		if (!CanInteractWithCommittedRows)
			return;

		var Position = _grid.PositionAtPoint(Event.Location);
		if (Position.IsEmpty() || !_rowToEntityMap.ContainsKey(Position.Row))
			return;

		if (_selectedRowOnMouseDown == Position.Row)
			_grid.Selection.ResetSelection(false);
		else
			_grid.Selection.SelectRow(Position.Row, true);
	}

	private async void _deselectToolStripMenuItem_Click(object sender, EventArgs e) {
		try {
			_grid.Selection.ResetSelection(false);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _editToolStripMenuItem_Click(object sender, EventArgs e) {
		try {
			await EditSelectedEntity();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _deleteToolStripMenuItem_Click(object sender, EventArgs e) {
		try {
			await DeleteSelectedRecord();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	#endregion

	#region Internal Types

	private sealed class AutoPageSizing {
		public object Query { get; set; }

		public int MaximumPageSize { get; set; } = int.MaxValue;
	}

	private enum VisualState {
		Normal,
		Selecting,
		Loading
	}

	private IDisposable EnterVisualState(VisualState newState) {
		var previousState = State;
		var initialBinding = _awaitingInitialBinding;
		State = newState;
		return new ActionDisposable(() => State = initialBinding && !_awaitingInitialBinding ? VisualState.Normal : previousState);
	}

	#endregion

	private CrudGridSettings _gridSettings;
	private string _settingsKey = string.Empty;
	private bool _persistSettings = true;
	private bool _restoringGridSettings;
	private bool _savingGridSettings;
	private bool _settingsSaveFailed;
	private bool _gridSettingsDirty;
	private int _settingsConsumerVersion;
	private (CrudGridPageMode PageMode, int PageSize, int PageNumber, string SortColumnName, SortDirection SortDirection) _savedPreferenceState;
	private ILogger _settingsLogger;

	[Category("Behavior")]
	[DefaultValue(true)]
	[Description("Remembers paging and sorting through UserSettings. A stable owner/control name or an explicit SettingsKey identifies the grid.")]
	public bool PersistSettings {
		get => _persistSettings;
		set => _persistSettings = value;
	}

	[Category("Behavior")]
	[DefaultValue("")]
	[Description("Optional stable UserSettings identifier. An empty value uses the owning screen/control type and the named child path, or the owning form.")]
	public string SettingsKey {
		get => _settingsKey;
		set {
			value ??= string.Empty;
			if (_settingsKey == value)
				return;
			SaveGridSettings();
			_settingsKey = value;
			_gridSettings = null;
			_settingsConsumerVersion = 0;
			_settingsSaveFailed = false;
			_gridSettingsDirty = false;
		}
	}

	/// <summary>Upgrades a detached preference payload before restoration. Changes persist with the next accepted preference change.</summary>
	/// <remarks>Called for stored preferences and new control defaults; never receives the provider's cached settings object.</remarks>
	protected virtual void UpgradeGridSettings(CrudGridSettings settings) {
	}

	private void RestoreGridSettings() {
		if (!_persistSettings || _gridSettings != null || _dataSource == null || DesignMode || Tools.Runtime.IsDesignMode || IsDisposed || Disposing)
			return;
		var key = ResolveSettingsKey();
		if (key == null)
			return;
		ISettingsProvider provider;
		try {
			provider = UserSettings.Provider;
		} catch (InvalidOperationException) {
			// A standalone control can be created before framework initialization; retry on its next explicit refresh.
			return;
		}
		_settingsLogger = ResolveSettingsLogger();
		_restoringGridSettings = true;
		using var restoreFlag = Tools.Scope.ExecuteOnDispose(() => _restoringGridSettings = false);
		try {
			var exists = UserSettings.Has<CrudGridSettings>(key);
			_gridSettings = UserSettings.Get<CrudGridSettings>(key);
			if (!exists) {
				CaptureGridSettings(_gridSettings);
			}
			var restoredSettings = new CrudGridSettings {
				ConsumerVersion = _gridSettings.ConsumerVersion,
				PageMode = _gridSettings.PageMode,
				AutoPageSize = _gridSettings.AutoPageSize,
				PageSize = _gridSettings.PageSize,
				PageNumber = _gridSettings.PageNumber,
				SortColumnName = _gridSettings.SortColumnName,
				SortDirection = _gridSettings.SortDirection
			};
			UpgradeGridSettings(restoredSettings);
			_settingsConsumerVersion = restoredSettings.ConsumerVersion;
			using (EnterVisualState(VisualState.Loading)) {
				_manualPageSize = Math.Max(1, Math.Min(restoredSettings.PageSize, 9999));
				var storedMode = restoredSettings.PageMode;
				_pageMode = storedMode.HasValue && Enum.IsDefined(storedMode.Value) ? storedMode.Value
					: restoredSettings.AutoPageSize ? CrudGridPageMode.Auto : CrudGridPageMode.Paged;
				UpdatePageModeControl();
				_pageNumber = _pageMode == CrudGridPageMode.Single || restoredSettings.PageNumber < 0 || restoredSettings.PageNumber >= int.MaxValue / _manualPageSize
					? 0 : restoredSettings.PageNumber;
				SetVisiblePageNumberText(_pageNumber);
				var sortColumn = Array.FindIndex(_columnsBindings, column => !string.IsNullOrEmpty(column.SortName)
					&& string.Equals(column.SortName, restoredSettings.SortColumnName, StringComparison.Ordinal));
				if (_crudCapabilities.HasFlag(DataSourceCapabilities.CanSort) && restoredSettings.SortDirection == SortDirection.None) {
					_sortColumnIndex = 0;
					_sortColumnName = null;
					_sortDirection = SortDirection.None;
				} else if (_crudCapabilities.HasFlag(DataSourceCapabilities.CanSort) && sortColumn >= 0) {
					_sortColumnIndex = sortColumn;
					_sortColumnName = _columnsBindings[sortColumn].SortName;
					_sortDirection = Enum.IsDefined(restoredSettings.SortDirection) ? restoredSettings.SortDirection : SortDirection.Ascending;
				}
				OrganizeLayout();
			}
			// Validate this instance's controls without changing a cached settings object shared with another instance.
			_savedPreferenceState = CapturePreferenceState();
		} catch (Exception error) {
			_settingsLogger.Exception(error, "Could not restore CRUD grid preferences.");
			try {
				_gridSettings = (CrudGridSettings)provider.NewSetting(typeof(CrudGridSettings), key);
				CaptureGridSettings(_gridSettings);
				_savedPreferenceState = CapturePreferenceState();
			} catch (Exception recoveryError) {
				_settingsLogger.Exception(recoveryError, "Could not initialize CRUD grid preferences.");
			}
		}
	}

	private void SaveGridSettings(bool acceptedChange = false) {
		if (!_persistSettings || _gridSettings == null || _restoringGridSettings || _savingGridSettings || DesignMode || Tools.Runtime.IsDesignMode)
			return;
		var preferences = CapturePreferenceState();
		if (acceptedChange && preferences != _savedPreferenceState)
			_gridSettingsDirty = true;
		if (!_gridSettingsDirty && !_settingsSaveFailed)
			return;
		_savingGridSettings = true;
		using var restoreFlag = Tools.Scope.ExecuteOnDispose(() => _savingGridSettings = false);
		try {
			CaptureGridSettings(_gridSettings);
			_gridSettings.Save();
			_settingsSaveFailed = false;
			_gridSettingsDirty = false;
			_savedPreferenceState = preferences;
		} catch (Exception error) {
			_settingsSaveFailed = true;
			(_settingsLogger ?? SystemLog.Logger).Exception(error, "Could not save CRUD grid preferences.");
		}
	}

	private (CrudGridPageMode PageMode, int PageSize, int PageNumber, string SortColumnName, SortDirection SortDirection) CapturePreferenceState()
		=> (_pageMode, _manualPageSize, _pageNumber, _sortColumnName ?? string.Empty, _sortDirection);

	private void CaptureGridSettings(CrudGridSettings settings) {
		settings.ConsumerVersion = _settingsConsumerVersion;
		settings.PageMode = _pageMode;
		settings.AutoPageSize = AutoPageSize;
		settings.PageSize = _manualPageSize;
		settings.PageNumber = _pageNumber;
		settings.SortColumnName = _sortColumnName ?? string.Empty;
		settings.SortDirection = _sortDirection;
	}

	private string ResolveSettingsKey() {
		if (!string.IsNullOrWhiteSpace(_settingsKey))
			return _settingsKey;
		var path = new List<string>();
		for (var control = (Control)this; control != null; control = control.Parent) {
			if (control is WinFormsApplicationScreen || control != this && control is UserControl && control.GetType() != typeof(UserControl))
				return $"{control.GetType().FullName}.{string.Join(".", path)}";
			if (control is Form form) {
				if (form.GetType() == typeof(Form) && string.IsNullOrWhiteSpace(form.Name))
					return null;
				return $"{form.GetType().FullName}.{form.Name}.{string.Join(".", path)}";
			}
			if (!string.IsNullOrWhiteSpace(control.Name))
				path.Insert(0, control.Name);
		}
		return null;
	}

	private ILogger ResolveSettingsLogger() {
		for (var control = Parent; control != null; control = control.Parent)
			if (control is WinFormsApplicationScreen screen && screen.Block?.Parent?.Logger is { } logger)
				return logger;
		return SystemLog.Logger;
	}
}
