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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms;

// Owns source assignment, refresh admission and the read/accept/publish pipeline.
// Cells prepares the content; Layout settles its geometry; Notifications schedules source changes.
public partial class VisualGrid {
	#region Events

	/// <summary>Raised on the owning UI thread after the current filtered query has been bound, including empty results.</summary>
	[Category("Behavior")]
	public event EventHandlerEx<VisualGrid> DataBindingComplete;

	#endregion

	#region Fields

	private readonly int _ownerThread = Environment.CurrentManagedThreadId;

	private VisualGridDataSourceController _dataSourceController;

	private IDataSource<object> _dataSource;

	private Type _dataSourceItemType = typeof(object);

	private DataSourceCapabilities _crudCapabilities;

	private readonly Throttle _refreshThrottle;

	private Task _refreshTask;

	private readonly AsyncSynchronizedGate _bindingGate = new();

	private bool _refreshing;

	private bool _sourceNotificationOnlyRefresh;

	private bool _awaitingInitialBinding;

	private int _bindingVersion;

	private IDataSource<object> _boundDataSource;

	private IVisualGridColumn[] _boundColumns;

	private int _sourceAssignmentVersion;

	private int _dataPublicationVersion;

	#endregion

	#region Properties

	[Description("Refreshes immediately without a loading animation or disabling the grid. Logical refresh admission and stale-query protection remain active.")]
	[Category("Appearance")]
	[DefaultValue(false)]
	public bool SuppressLoadingIndicator { get; set; }

	[Description("Prepares read-only Text/Image pages on a worker, including source reads, enumeration, column delegates and display conversion. Opt in only when those operations and borrowed images are safe off the UI thread. Editable or other cell types retain their normal UI-thread binding.")]
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool PrepareReadOnlyCellsInBackground { get; set; }

	[Category("Behavior")]
	[DefaultValue(DataSourceCapabilities.Default)]
	public DataSourceCapabilities Capabilities {
		get => _crudCapabilities;
		set {
			_crudCapabilities = value & ~DataSourceCapabilities.CanNotify;
			if (_observedSource != null)
				_crudCapabilities |= DataSourceCapabilities.CanNotify;
			UpdateCellInteraction();
			OrganizeLayout();
			QueueDataSourceNotification();
		}
	}

	/// <summary>Whether the configured source has completed capability binding, independently of its first page read.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool IsDataSourceBound => _dataSourceController?.IsBound == true;

	/// <summary>Joins the most recent admitted page refresh without starting another query.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Task RefreshCompletion => _refreshTask ?? Task.CompletedTask;

	/// <summary>Optional resource-publication stamp captured before a query and checked before publication.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<object> PublicationIdentity {
		get;
		set {
			EnsureDataSourceOwnerThread();
			field = value;
		}
	}

	internal Func<bool> AcceptSourcePublication { get; set; }

	internal Func<object, object> ResolvePublishedEntity { get; set; }

	private bool IsSourceBindingPending => _dataSourceController is { IsBound: false };

	#endregion

	#region Methods

	/// <summary>Configures a borrowed source without querying it. The first source refresh binds it on this grid's dispatcher.</summary>
	public void ConfigureDataSource<T>(IDataSource<T> source, Func<T, object> resolvePublishedEntity = null, Func<T, object> visibleSelectionIdentity = null) {
		EnsureDataSourceOwnerThread();
		Guard.ArgumentNotNull(source, nameof(source));
		var projected = Tools.DataSources.AsObjects(source);
		ConfigureDataSourceCore(projected, typeof(T), Resolve,
			visibleSelectionIdentity == null ? null : entity => entity is T typed ? visibleSelectionIdentity(typed) : null, DataSourceCapabilities.Default);

		object Resolve(object selected) {
			if (selected is not T typed)
				return null;
			var resolved = resolvePublishedEntity == null ? selected : resolvePublishedEntity(typed);
			Guard.Ensure(resolved == null || resolved is T, "A published grid selection must have the source's item type.");
			return resolved;
		}
	}

	/// <summary>Invalidates and presents the configured source, binding it on its first refresh.</summary>
	public Task RefreshDataSourceAsync(CancellationToken cancellationToken = default) {
		EnsureDataSourceOwnerThread();
		if (_dataSourceController != null)
			return _dataSourceController.RefreshAsync(cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		InvalidateDataSource();
		return RefreshGrid();
	}

	/// <summary>Rejects in-flight source results without disabling or clearing the committed rows.</summary>
	public void InvalidateDataSource() {
		EnsureDataSourceOwnerThread();
		if (!IsDisposed)
			NotifyDataSourceChanged();
	}

	/// <summary>Binds a typed source and restores its applicable paging, layout and preferences.</summary>
	public Task SetDataSource<TEntity>(IDataSource<TEntity> dataSource) => SetDataSource(dataSource, DataSourceCapabilities.Default);

	/// <summary>Binds a typed source immediately without reading its rows.</summary>
	public async Task SetDataSource<TEntity>(IDataSource<TEntity> dataSource, DataSourceCapabilities capabilities) {
		var projected = Tools.DataSources.AsObjects(dataSource);
		var controller = ConfigureDataSourceCore(projected, typeof(TEntity), null, null, capabilities);
		await controller.BindAsync();
	}

	/// <summary>Releases the current source binding and invalidates retained row measurements.</summary>
	public void ClearDataSource() {
		ReleaseDataSourceController();
		_sourceAssignmentVersion++;
		ReleaseDataSourceNotifications();
		_dataSource = null;
		ConfigureDetails(null);
		_dataSourceItemType = typeof(object);
		ResolvePublishedEntity = null;
		PublishedSelectionIdentity = null;
		_columnWidthOverrides.Clear();
		_columnStretchOverrides.Clear();
		ResetRowHeightMeasurements();
	}

	/// <summary>Refreshes the selected row without rereading or resizing the current page.</summary>
	public Task<bool> RefreshSelectedEntityAsync() {
		if (_rowDetails?.SelectedGrid is { } child)
			return child.RefreshSelectedEntityAsync();
		return _selectedEntity == null ? Task.FromResult(false) : RefreshEntityAsync(_selectedEntity);
	}

	/// <summary>Refreshes the supplied visible entity through the source and replaces only its displayed row. A null refreshed entity removes that row.</summary>
	/// <returns>False when the entity is no longer visible or its binding/query was superseded.</returns>
	public async Task<bool> RefreshEntityAsync(object entity) {
		Guard.ArgumentNotNull(entity, nameof(entity));
		using (await _bindingGate.EnterScopeAsync()) {
			if (IsDisposed || Disposing || IsSourceBindingPending || _awaitingInitialBinding || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead)
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
			var styleProvider = RowStyleProvider;
			var details = DetailConfiguration;
			var detailVersion = _rowDetails?.Version ?? 0;
			VisualGridDetailRow[] replacementDetails = null;
			VisualGridPreparedValue[][] prepared = null;
			if (prepare) {
				try {
					prepared = await Task.Run(async () => {
						await source.RefreshRangeAsync(refreshed).ConfigureAwait(false);
						if (refreshed[0] != null && details != null)
							replacementDetails = _rowDetails.PrepareRows(new[] { refreshed[0] }, details);
						return refreshed[0] == null ? null : PrepareReadOnlyRows(Enumerable.Repeat(refreshed[0], rows.Length).ToArray(), bindings, formats, styleProvider);
					});
				} catch when (IsDisposed || Disposing) {
					return false;
				}
			} else {
				await source.RefreshRangeAsync(refreshed);
				if (refreshed[0] != null && details != null)
					replacementDetails = await Task.Run(() => _rowDetails.PrepareRows(new[] { refreshed[0] }, details));
			}

			if (IsDisposed || Disposing || IsSourceBindingPending || source != _dataSource || bindings != _columnsBindings || bindingVersion != _bindingVersion
				|| styleProvider != RowStyleProvider
				|| !ReferenceEquals(details, DetailConfiguration) || detailVersion != (_rowDetails?.Version ?? 0)
				|| !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead)
				|| query != (_searchText, _pageSize, _pageNumber, _sortColumnName, _sortDirection)
				|| prepare && !PreparedFormatsAreCurrent(bindings, formats)
				|| rows.Any(row => !_rowToEntityMap.TryGetValue(row, out var visible) || !ReferenceEquals(visible, entity)))
				return false;

			var replacement = refreshed[0];
			var wasSelected = ReferenceEquals(_selectedEntity, entity);
			using (SuspendGridLayout())
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
						DecorateOutlineRow(row.Item, replacement);
					}
					CalculateEntityToRowLookup();
					if (_rowDetails?.Layout is { } layout && replacementDetails != null)
						ReflowRowDetails(layout.Rows.Select(row => ReferenceEquals(row.Entity, entity) ? replacementDetails[0] : row).ToArray());
				}
				if (wasSelected)
					_selectedEntity = replacement;
				HighlightSelectedEntity();
				_grid.Invalidate();
			}
			if (wasSelected && !ReferenceEquals(entity, replacement)) {
				NotifyEntityDeselected(entity);
				if (replacement != null)
					NotifyEntitySelected(replacement);
			}
			if (replacement != null && _rowDetails != null)
				await _rowDetails.RefreshAsync();
			return true;
		}
	}

	/// <summary>The entities of the displayed page in display order, as an array of the bound item type. Nothing is queried, so the
	/// result is exactly what the user sees; a grid in Single page mode displays, and so returns, every matching row.</summary>
	public Array CaptureVisibleEntities() {
		Guard.Ensure(!InvokeRequired, "Capture the displayed page on its owning UI thread.");
		var itemType = _dataSourceItemType;
		var rows = (_rowToEntityMap ?? new Dictionary<int, object>()).OrderBy(pair => pair.Key).Select(pair => pair.Value)
			.Where(itemType.IsInstanceOfType).Distinct().ToArray();
		var snapshot = Array.CreateInstance(itemType, rows.Length);
		for (var index = 0; index < rows.Length; index++)
			snapshot.SetValue(rows[index], index);
		return snapshot;
	}

	/// <summary>Reads all matching rows using the current filter and sort, independent of the visible page. Capture on the owning UI thread.</summary>
	/// <remarks>Queries the bound source on a worker without rebinding, disabling or changing the selection. The returned one-dimensional array retains its source item type, including when empty.</remarks>
	public async Task<Array> ReadFilteredEntitiesAsync(CancellationToken cancellationToken = default) {
		Guard.Ensure(!InvokeRequired, "Capture the grid query on its owning UI thread.");
		cancellationToken.ThrowIfCancellationRequested();
		var assignmentVersion = _sourceAssignmentVersion;
		Guard.Ensure(!IsSourceBindingPending, "The grid source changed; refresh before exporting the filtered data.");
		var publicationIdentity = PublicationIdentity;
		var publication = publicationIdentity?.Invoke();
		Guard.Ensure(IsSourcePublicationCurrent(publicationIdentity, publication), "The grid publication changed; refresh before exporting the filtered data.");
		var source = _dataSource;
		var itemType = _dataSourceItemType;
		var search = _searchText;
		var sort = _sortColumnName;
		var direction = _sortDirection;
		if (source == null || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead))
			return Array.CreateInstance(itemType, 0);
		var matching = await ReadMatchingEntitiesAsync(source, itemType, search, sort, direction, cancellationToken);
		Guard.Ensure(assignmentVersion == _sourceAssignmentVersion && IsSourcePublicationCurrent(publicationIdentity, publication), "The grid publication changed during export; refresh before exporting the filtered data.");
		return matching;
	}

	/// <summary>Queues the current filtered page for refresh through the existing refresh throttle.</summary>
	public Task RefreshGrid() => RequestGridRefresh(false);

	internal bool OwnsDataSourceController(VisualGridDataSourceController controller)
		=> ReferenceEquals(_dataSourceController, controller) && !IsDisposed && !Disposing;

	internal async Task<bool> BindDataSourceAsync(IDataSource<object> source, Type itemType, DataSourceCapabilities capabilities, int assignmentVersion) {
		var available = await source.CapabilitiesAsync;
		if (IsDisposed || Disposing || assignmentVersion != _sourceAssignmentVersion)
			return false;
		ReleaseDataSourceNotifications();
		_dataSource = source;
		ConfigureDetails(source);
		_dataSourceItemType = itemType;
		ResolvePublishedEntity = null;
		PublishedSelectionIdentity = null;
		ResetRowHeightMeasurements();
		_crudCapabilities = available & (capabilities | DataSourceCapabilities.CanNotify);
		ObserveDataSourceNotifications(source, available);
		RestoreColumnPreferences(_preferences?.Columns);
		UpdateCellInteraction();
		OrganizeLayout();
		return true;
	}

	/// <summary>Invalidates accepted row content after the owner publishes a new source snapshot.</summary>
	internal void NotifyDataSourceChanged() => Interlocked.Increment(ref _dataPublicationVersion);

	protected void NotifyDataBindingComplete() {
		OnDataBindingComplete();
		DataBindingComplete?.Invoke(this);
	}

	protected virtual void OnDataBindingComplete() {
	}

	private VisualGridDataSourceController ConfigureDataSourceCore(IDataSource<object> source, Type itemType, Func<object, object> resolver,
		Func<object, object> visibleIdentity, DataSourceCapabilities capabilities) {
		EnsureDataSourceOwnerThread();
		ReleaseDataSourceController();
		ReleaseDataSourceNotifications();
		var assignmentVersion = ++_sourceAssignmentVersion;
		NotifyDataSourceChanged();
		_dataSourceController = new VisualGridDataSourceController(this, source, itemType, resolver, capabilities, assignmentVersion);
		DataBindingComplete += _dataSourceController.ReconcileSelection;
		VisibleSelectionIdentity = visibleIdentity;
		return _dataSourceController;
	}

	private void EnsureDataSourceOwnerThread()
		=> Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThread, "Configure and refresh the grid source on its owning UI dispatcher.");

	private void ReleaseDataSourceController() {
		if (_dataSourceController == null)
			return;
		DataBindingComplete -= _dataSourceController.ReconcileSelection;
		_dataSourceController = null;
		VisibleSelectionIdentity = null;
		ResolvePublishedEntity = null;
		PublishedSelectionIdentity = null;
		AcceptSourcePublication = null;
	}

	private static Task<Array> ReadMatchingEntitiesAsync(IDataSource<object> source, Type itemType, string search, string sort, SortDirection direction, CancellationToken cancellationToken)
		=> Task.Run(async () => {
			cancellationToken.ThrowIfCancellationRequested();
			var read = await source.ReadRangeAsync(search, int.MaxValue, 0, sort, direction).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			var rows = read.Items?.ToArray() ?? Array.Empty<object>();
			var snapshot = Array.CreateInstance(itemType, rows.Length);
			for (var index = 0; index < rows.Length; index++)
				snapshot.SetValue(rows[index], index);
			return snapshot;
		}, cancellationToken);

	private bool IsSourcePublicationCurrent(Func<object> identity, object publication)
		=> identity == PublicationIdentity && (identity == null || Equals(publication, identity())) && AcceptSourcePublication?.Invoke() != false;

	private Task RequestGridRefresh(bool sourceNotification) {
		if (_pendingQueryPreferences != null && IsPreferenceCellEditing())
			return Task.CompletedTask;
		if (!sourceNotification)
			_sourceNotificationOnlyRefresh = false;
		if (_refreshTask is not { IsCompleted: false }) {
			_sourceNotificationOnlyRefresh = sourceNotification;
			_refreshTask = RefreshGridInternal(sourceNotification);
		}
		return _refreshTask;
	}

	/// <summary>Serializes refresh admission and publishes the latest current page.</summary>
	private async Task RefreshGridInternal(bool sourceNotification = false) {
		if (DesignMode || IsDisposed || State == VisualState.Loading && !_awaitingInitialBinding)
			return;
		using (await _bindingGate.EnterScopeAsync()) {
			if (IsDisposed || Disposing)
				return;
			_refreshing = true;
			_sourceNotificationRefreshing = sourceNotification;
			using (Tools.Scope.ExecuteOnDispose(() => {
				_refreshing = false;
				_sourceNotificationRefreshing = false;
				_sourceNotificationOnlyRefresh = false;
				QueueDataSourceNotification();
			})) {
				using (EnterVisualState(VisualState.Loading)) {
					var initialBinding = _awaitingInitialBinding;
					var suppressLoading = SuppressLoadingIndicator || sourceNotification || initialBinding;
					if (!suppressLoading && !await _refreshThrottle.IsCallerFirstInStampede() || IsDisposed)
						return;
					using (ActionScope? animation = suppressLoading ? null : EnterLoadingIndicator()) {
						var previousEnabled = _grid.Enabled;
						using (Tools.Scope.ExecuteOnDispose(() => {
							if (!suppressLoading && !IsDisposed)
								_grid.Enabled = previousEnabled;
						})) {
							if (!suppressLoading)
								_grid.Enabled = false;
							var pageSizing = new AutoPageSizing();
							while (!IsDisposed) {
								while (await BindInternal(pageSizing))
									;
								if (DeferInactiveSourceRefresh())
									return;
								if (!initialBinding || _dataSource == null || IsDisposed || IsSourceBindingPending)
									break;
								// Hidden controls retain their old bounds. Lay out accepted data under the opaque loading mask
								// and settle the actual page capacity before publishing the first completed binding.
								var previousPageSize = _pageSize;
								_layoutPanel.Visible = true;
								_initialLoadingCircle.BringToFront();
								PerformLayout();
								_layoutPanel.PerformLayout();
								_bottomPanel.PerformLayout();
								if (IsSourceBindingPending)
									break;
								if (AutoPageSize && _crudCapabilities.HasFlag(DataSourceCapabilities.CanPage)) {
									_pageSize = CalculateAutoPageSize(pageSizing);
									UpdatePageSizeDisplay();
								}
								if (_pageSize == previousPageSize)
									break;
							}
							if (initialBinding && _dataSource != null && !IsDisposed && !IsSourceBindingPending) {
								_awaitingInitialBinding = false;
								UpdateLoadingIndicator();
								_layoutPanel.Visible = true;
							}
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Binds the datasource to the view.
	/// </summary>
	/// <returns>True if search parameters changed during search, false otherwise.</returns>
	private async Task<bool> BindInternal(AutoPageSizing pageSizing) {
		// Deferred source configuration retains committed rows until its first capability binding finishes.
		if (IsSourceBindingPending || DeferInactiveSourceRefresh())
			return false;
		if (_sourceNotificationEdits > 0 || IsPreferenceCellEditing()) {
			Interlocked.Exchange(ref _pendingSourceNotification, 1);
			return false;
		}
		Interlocked.Exchange(ref _pendingSourceNotification, 0);
		var query = BindingQuery.Capture(this);
		if (query.PageSize == 0)
			return false;

		// Take care of null grid
		if (_columnsBindings == null || _dataSource == null || !_crudCapabilities.HasFlag(DataSourceCapabilities.CanRead)) {
			ClearBinding();
			return false;
		}

		// Read the page, then accept it only while the captured query, publication and prepared formats still describe this grid.
		var page = await ReadPageAsync(query);
		if (page == null || IsDisposed || Disposing || IsSourceBindingPending || DeferInactiveSourceRefresh())
			return false;
		if (_sourceNotificationEdits > 0 || IsPreferenceCellEditing()) {
			Interlocked.Exchange(ref _pendingSourceNotification, 1);
			return false;
		}
		// Stop a superseded resource query; retrying its prepared source under a new identity would publish old evidence.
		if (!IsSourcePublicationCurrent(query.PublicationIdentity, query.Publication))
			return false;
		// A superseded result must not change the requested page, visible rows or persisted selection options.
		if (!query.IsCurrent(this) || page.Prepared != null && !PreparedFormatsAreCurrent(query.Columns, page.Formats))
			return true;

		// Publish the accepted rows, then settle prepared geometry over those same cells. Settling yields to the message loop between its
		// sizing steps; the grid keeps showing its previous frame meanwhile and paints the settled page once.
		using (_grid.SuspendPainting()) {
			var sizeColumns = PublishPage(query, page);
			if (page.Prepared != null && !await SettlePreparedGeometryAsync(query, page, _bindingVersion, sizeColumns))
				return !IsDisposed && !Disposing;
			_bottomPanel.PerformLayout();
		}

		// Reconcile the estimate with the actual rows, allowing both growth and shrinkage under one bounded settle cycle.
		if (AutoPageSize && _crudCapabilities.HasFlag(DataSourceCapabilities.CanPage)) {
			_pageSize = CalculateAutoPageSize(pageSizing);
			UpdatePageSizeDisplay();
		}

		if (query.IsCurrent(this))
			NotifyDataBindingComplete();
		if (query.IsCurrent(this) && _rowDetails != null)
			await _rowDetails.RefreshAsync();
		return !query.IsCurrent(this);
	}

	/// <summary>Empties the table when no readable source and columns are bound, completing the binding with no rows.</summary>
	private void ClearBinding() {
		ResetRowHeightMeasurements();
		_boundDataSource = null;
		_boundColumns = null;
		_rowToEntityMap.Clear();
		CalculateEntityToRowLookup();
		_rowDetails?.PrepareLayout(null);
		_rowDetails?.BeginBinding(null);
		_grid.Redim(0, 0);
		_sizedColumnBindings = null;
		_totalRecords = 0;
		_totalRecordsLabel.Text = "0";
		NotifyDataBindingComplete();
	}

	/// <summary>Reads the captured query's page. Only the explicit worker-safe, read-only contract moves source/column delegates off the owner thread.</summary>
	/// <returns>The read page, or null when this control was disposed during worker preparation.</returns>
	private async Task<PreparedPage> ReadPageAsync(BindingQuery query) {
		var dataSource = query.DataSource;
		var columns = query.Columns;
		var pageSize = query.PageSize;
		var rowDetails = query.DetailConfiguration;
		if (!CanPrepareReadOnlyCells(columns)) {
			// Ordinary cells bind on the owner thread; only row-detail metadata is prepared on a worker.
			var read = await dataSource.ReadRangeAsync(query.SearchText, pageSize, query.PageNumber, query.SortColumnName, query.SortDirection);
			object[] data = null;
			VisualGridDetailRow[] details = null;
			if (rowDetails != null && columns.Length > 0) {
				data = read.Items.ToArray();
				details = await Task.Run(() => _rowDetails.PrepareRows(data.Take(pageSize).ToArray(), rowDetails));
			}
			return new PreparedPage { Read = read, Data = data, Details = details };
		}

		// Prepare detached values, new cell slots and row metadata off the owner thread.
		var formats = columns.Select(column => (column.DisplayType, column.DataType)).ToArray();
		var reusableRowCount = _boundDataSource == dataSource && _boundColumns == columns ? _rowToEntityMap.Count : 0;
		var referenceLookup = query.ReferenceLookup;
		var styleProvider = query.StyleProvider;
		try {
			return await Task.Run(async () => {
				var read = await dataSource.ReadRangeAsync(query.SearchText, pageSize, query.PageNumber, query.SortColumnName, query.SortDirection).ConfigureAwait(false);
				var entities = read.Items.ToArray();
				var visible = entities.Take(pageSize).ToArray();
				var rows = rowDetails == null ? visible.Select((entity, index) => (Entity: entity, Row: index + 1)).ToDictionary(item => item.Row, item => item.Entity) : null;
				var lookup = rows == null ? null : referenceLookup ? rows.InverseUsingValueReferenceAsKey() : rows.Inverse();
				var details = rowDetails == null || columns.Length == 0 ? null : _rowDetails.PrepareRows(visible, rowDetails);
				var values = PrepareReadOnlyRows(visible, columns, formats, styleProvider);
				var cells = new VisualGridPreparedCell[values.Length][];
				for (var row = reusableRowCount; row < values.Length; row++)
					cells[row] = values[row].Select(value => new VisualGridPreparedCell(value)).ToArray();
				return new PreparedPage { Read = read, Data = entities, Formats = formats, Prepared = values, NewCells = cells, Rows = rows, Lookup = lookup, Details = details };
			});
		} catch when (IsDisposed || Disposing) {
			return null;
		}
	}

	/// <summary>Publishes an accepted page's totals, rows and selection on the owner thread.</summary>
	/// <returns>True when the published columns still need their natural sizing.</returns>
	private bool PublishPage(BindingQuery query, PreparedPage page) {
		var columns = query.Columns;
		var pageSize = query.PageSize;
		var data = page.Data ??= page.Read.Items.ToArray();
		var detailLayout = _rowDetails?.PrepareLayout(page.Details);
		var previousSelection = _selectedEntity;
		var publishedSelection = previousSelection != null && ResolvePublishedEntity != null ? ResolvePublishedEntity(previousSelection) : previousSelection;
		publishedSelection = MatchPublishedSelection(publishedSelection, detailLayout != null ? detailLayout.Entities.Values : data);
		var reusePreparedCells = page.Prepared != null && _boundDataSource == query.DataSource && _boundColumns == columns;
		_bindingVersion++;
		_boundDataSource = query.DataSource;
		_boundColumns = columns;
		_totalRecords = page.Read.TotalCount;
		_endPageNumber = ((int)Math.Ceiling(_totalRecords / (decimal)pageSize) - 1).ClipTo(0, int.MaxValue);

		// The source's actual page becomes the current query page.
		var resultPage = Math.Max(0, Math.Min(page.Read.Page, _endPageNumber));
		if (resultPage != _pageNumber) {
			_pageNumber = resultPage;
			SetVisiblePageNumberText(resultPage);
		}
		query.PageNumber = resultPage;

		// Grid header
		_titleLabel.Text = GridTitle;
		var sizeColumns = _sizedColumnBindings != columns || !_columnsSizedFromData && data.Length > 0;

		// SourceGrid already buffers painting. WM_SETREDRAW would remove WS_VISIBLE and make native clicks miss these committed rows.
		// Keep its HWND visible while synchronously publishing cells, and defer layout until the publication is complete.
		using (EnterVisualState(VisualState.Selecting)) {
			_selectedEntity = publishedSelection;
			if (detailLayout != null)
				_rowToEntityMap = detailLayout.Entities;
			else if (page.Rows != null)
				_rowToEntityMap = page.Rows;
			else
				_rowToEntityMap.Clear();
			var previousSizing = _sizingColumns;
			if (page.Prepared != null)
				_sizingColumns = true;
			using (Tools.Scope.ExecuteOnDispose(() => _sizingColumns = previousSizing)) {
				using (SuspendGridLayout()) {
					_rowDetails?.BeginBinding(detailLayout, _grid.ColumnsCount != columns.Length);
					_grid.Redim(detailLayout?.RowCount ?? Math.Min(pageSize, data.Length) + 1, columns.Length);
					for (var column = 0; column < columns.Length; column++)
						BindColumnHeaders(column);
					ApplyColumnHeaderVisibility();
					foreach (var entity in data.WithDescriptions()) {
						if (pageSize <= entity.Index)
							break;
						var physicalRow = detailLayout?.EntityRows[entity.Index] ?? entity.Index + 1;
						_grid.Rows[physicalRow].AutoSizeMode = SourceGrid.AutoSizeMode.Default;
						if (page.Prepared != null)
							BindPreparedRow(physicalRow, entity.Item, page.Prepared[entity.Index], reusePreparedCells, page.NewCells[entity.Index]);
						else
							BindRowInternal(physicalRow, entity.Item);
						if (detailLayout != null)
							_rowDetails.DecorateRow(physicalRow, detailLayout.Rows[entity.Index]);
						DecorateOutlineRow(physicalRow, entity.Item);
					}
					_rowDetails?.EndBinding();
					if (page.Lookup != null)
						_entityToRowLookup = page.Lookup;
					else
						CalculateEntityToRowLookup();
					_totalRecordsLabel.Text = _totalRecords.ToString();
					_pageCountLabel.Text = string.Format("/ {0}", _endPageNumber + 1);
					InitializeGridSelectionMode();
					HighlightSelectedEntity();
					if (page.Prepared == null && sizeColumns) {
						SizeBoundColumns();
						RememberRowHeightGeometry();
					} else if (page.Prepared == null)
						SizeBoundRows();
				}
			}
		}

		// Replace the selected immutable row with the same identity in the same publication as its cells.
		// Geometry can yield afterward; native selection and row commands must already refer to the published model.
		if (query.IsCurrent(this) && !ReferenceEquals(previousSelection, publishedSelection)) {
			IsReconcilingSelection = true;
			using (Tools.Scope.ExecuteOnDispose(() => IsReconcilingSelection = false)) {
				if (previousSelection != null)
					NotifyEntityDeselected(previousSelection);
				if (publishedSelection != null)
					NotifyEntitySelected(publishedSelection);
			}
		}
		return sizeColumns;
	}

	#endregion

	#region Inner Types

	/// <summary>The query inputs captured before a page read. A later difference supersedes that read's result.</summary>
	private sealed class BindingQuery {
		private BindingQuery() {
		}

		public IDataSource<object> DataSource { get; private init; }
		public Func<object> PublicationIdentity { get; private init; }
		public object Publication { get; private init; }
		public int DataPublicationVersion { get; private init; }
		public IVisualGridColumn[] Columns { get; private init; }
		public string SearchText { get; private init; }
		public int PageSize { get; private init; }

		/// <summary>The requested page, replaced by the source's actual page once that result is published.</summary>
		public int PageNumber { get; set; }

		public string SortColumnName { get; private init; }
		public SortDirection SortDirection { get; private init; }
		public bool PrepareInBackground { get; private init; }
		public bool ReferenceLookup { get; private init; }
		public Func<object, VisualGridRowStyle> StyleProvider { get; private init; }
		public VisualGridDetailConfiguration DetailConfiguration { get; private init; }
		public int DetailVersion { get; private init; }

		/// <summary>Captures the grid's current query inputs on its owner thread.</summary>
		public static BindingQuery Capture(VisualGrid grid) {
			var publicationIdentity = grid.PublicationIdentity;
			return new BindingQuery {
				DataSource = grid._dataSource,
				PublicationIdentity = publicationIdentity,
				Publication = publicationIdentity?.Invoke(),
				DataPublicationVersion = grid._dataPublicationVersion,
				Columns = grid._columnsBindings,
				SearchText = grid._searchText,
				PageSize = grid._pageSize,
				PageNumber = grid._pageNumber,
				SortColumnName = grid._sortColumnName,
				SortDirection = grid._sortDirection,
				PrepareInBackground = grid.PrepareReadOnlyCellsInBackground,
				ReferenceLookup = grid.UseEntityReferenceForLookup,
				StyleProvider = grid.RowStyleProvider,
				DetailConfiguration = grid.DetailConfiguration,
				DetailVersion = grid._rowDetails?.Version ?? 0
			};
		}

		/// <summary>Checks whether the grid still describes exactly this captured, readable query.</summary>
		public bool IsCurrent(VisualGrid grid)
			=> DataSource == grid._dataSource && DataPublicationVersion == grid._dataPublicationVersion
				&& Columns == grid._columnsBindings && SearchText == grid._searchText && PageSize == grid._pageSize
				&& PageNumber == grid._pageNumber && SortColumnName == grid._sortColumnName && SortDirection == grid._sortDirection && PrepareInBackground == grid.PrepareReadOnlyCellsInBackground
				&& ReferenceLookup == grid.UseEntityReferenceForLookup && StyleProvider == grid.RowStyleProvider && DetailConfiguration == grid.DetailConfiguration
				&& DetailVersion == (grid._rowDetails?.Version ?? 0) && grid._crudCapabilities.HasFlag(DataSourceCapabilities.CanRead);
	}

	/// <summary>One read page with any worker-prepared values that publication consumes on the owner thread.</summary>
	private sealed class PreparedPage {
		public DataSourceItems<object> Read { get; init; }

		/// <summary>The read entities, materialized by publication when the worker did not enumerate them.</summary>
		public object[] Data { get; set; }

		/// <summary>The display/data type of each column at preparation time, or null when cells bind on the owner thread.</summary>
		public (VisualGridCellDisplayType DisplayType, Type DataType)[] Formats { get; init; }

		/// <summary>Worker-prepared values per visible row, or null when cells bind on the owner thread.</summary>
		public VisualGridPreparedValue[][] Prepared { get; init; }

		/// <summary>New native cell slots for rows beyond the reusable count; reusable rows are null.</summary>
		public VisualGridPreparedCell[][] NewCells { get; init; }

		/// <summary>The prepared row-to-entity map, or null when a row-detail layout supplies it.</summary>
		public IDictionary<int, object> Rows { get; init; }

		/// <summary>The prepared entity-to-row lookup matching <see cref="Rows"/>.</summary>
		public ILookup<object, int> Lookup { get; init; }

		/// <summary>Prepared row-detail metadata, or null without row details.</summary>
		public VisualGridDetailRow[] Details { get; init; }
	}

	#endregion
}
