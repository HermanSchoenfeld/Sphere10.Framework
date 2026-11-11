// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;

/// <summary>A data-source grid with buffered row editing, paging and optional user preferences.</summary>
public partial class BlazorGrid<TItem> : IAsyncDisposable {
	private readonly IBlazorGridController<TItem> _controller = new BlazorGridController<TItem>();
	private readonly Dictionary<BlazorGridColumn<TItem>, string> _editErrors = new();
	private BlazorGridColumn<TItem>[] _autoColumns;
	private BlazorGridColumn<TItem>[] _columns = Array.Empty<BlazorGridColumn<TItem>>();
	private ElementReference _element;
	private ElementReference _pageModeButton;
	private Task _initializationTask;
	private IJSObjectReference _module;
	private IJSObjectReference _behavior;
	private DotNetObjectReference<BlazorGrid<TItem>> _reference;
	private bool _disposed;
	private bool _confirmDelete;
	private bool _pageModeMenuOpen;
	private bool _performingAction;
	private bool _wasSelected;
	private TItem _lastSelection;
	private string _searchText = string.Empty;
	private string _operationError;
	private int _parameterPageSize;
	private BlazorGridPageMode _parameterPageMode;
	private string _settingsKey;
	private IDataSource<TItem> _boundSource;
	private DataSourceCapabilities _allowedCapabilities;

	/// <summary>The rendered grid element's unique ID.</summary>
	[Parameter] public string Id { get; set; } = $"sphere10-grid-{Guid.NewGuid():N}";

	/// <summary>The visible grid caption.</summary>
	[Parameter] public string Caption { get; set; } = "Data grid";

	/// <summary>The horizontal alignment of the caption.</summary>
	[Parameter] public BlazorGridHorizontalAlignment CaptionAlignment { get; set; } = BlazorGridHorizontalAlignment.Left;

	/// <summary>Measures natural column widths once for each column definition set, then retains them across query changes.</summary>
	[Parameter] public bool AutoSizeColumns { get; set; } = true;

	/// <summary>Keeps completed rows interactive during refresh. The first read always displays the loading surface.</summary>
	[Parameter] public bool SuppressLoadingIndicator { get; set; }

	/// <summary>Persists standalone preferences through UserSettings when an application-defined SettingsKey is supplied.</summary>
	[Parameter] public bool PersistSettings { get; set; } = true;

	/// <summary>The stable identity for standalone user preferences.</summary>
	[Parameter] public string SettingsKey { get; set; }

	/// <summary>The text displayed when a completed query has no rows.</summary>
	[Parameter] public string EmptyText { get; set; } = "No items found.";

	/// <summary>The label for accepting the current row edit.</summary>
	[Parameter] public string SaveButtonText { get; set; } = "Save";

	/// <summary>The explanation shown while editing a row.</summary>
	[Parameter] public string EditNotice { get; set; } = "Changes are saved only when you choose Save.";

	/// <summary>The requested manual page length.</summary>
	[Parameter] public int PageSize { get; set; } = 10;

	/// <summary>Optional rows-per-page choices. The active size is retained even when it is not listed.</summary>
	[Parameter] public int[] PageSizeOptions { get; set; }

	/// <summary>The source that supplies rows and CRUD operations.</summary>
	[Parameter] public IDataSource<TItem> DataSource { get; set; }

	/// <summary>The operations this grid permits from the source.</summary>
	[Parameter] public DataSourceCapabilities AllowedCapabilities { get; set; } = DataSourceCapabilities.Default;

	/// <summary>The declared column schema.</summary>
	[Parameter] public BlazorGridColumn<TItem>[] Columns { get; set; }

	/// <summary>Generates columns from public properties when no schema is supplied.</summary>
	[Parameter] public bool AutoGenerateColumns { get; set; } = true;

	/// <summary>Allows editable cells to enter a buffered row edit.</summary>
	[Parameter] public bool AllowCellEditing { get; set; } = true;

	/// <summary>Allows clicking the selected row to clear its selection.</summary>
	[Parameter] public bool LeftClickToDeselect { get; set; } = true;

	/// <summary>The requested paging mode.</summary>
	[Parameter] public BlazorGridPageMode PageMode { get; set; } = BlazorGridPageMode.Paged;

	/// <summary>Uses the viewport to determine the automatic page length.</summary>
	[Parameter] public bool AutoPageSize { get; set; }

	/// <summary>The grid viewport height in CSS pixels.</summary>
	[Parameter] public int Height { get; set; } = 420;

	/// <summary>The enabled presentation and interaction traits.</summary>
	[Parameter] public GridTypes GridType { get; set; } = GridTypes.ActionColumn | GridTypes.NewItem | GridTypes.AllowsSettingsChange;

	/// <summary>The declared row actions.</summary>
	[Parameter] public GridAction<TItem>[] Actions { get; set; } = Array.Empty<GridAction<TItem>>();

	/// <summary>Legacy columns rendered after the typed schema.</summary>
	[Parameter] public object[] ColumnDefinitions { get; set; } = Array.Empty<object>();

	/// <summary>Reports changes to the selected entity.</summary>
	[Parameter] public EventCallback<TItem> SelectedItemChanged { get; set; }

	/// <summary>Reports a successfully created entity.</summary>
	[Parameter] public EventCallback<TItem> ItemCreated { get; set; }

	/// <summary>Reports a successfully updated entity.</summary>
	[Parameter] public EventCallback<TItem> ItemUpdated { get; set; }

	/// <summary>Reports a successfully deleted entity.</summary>
	[Parameter] public EventCallback<TItem> ItemDeleted { get; set; }

	/// <summary>The controller that owns querying, selection and buffered edits.</summary>
	public IBlazorGridController<TItem> Controller => _controller;

	/// <summary>The current zero-based page number.</summary>
	public int CurrentPage => Controller.CurrentPage;

	/// <summary>The total row count reported by the source.</summary>
	public int TotalDataCount => Controller.TotalCount;

	/// <summary>The current number of available pages.</summary>
	public int TotalPages => Controller.TotalPages;

	/// <summary>The entity currently selected in the grid.</summary>
	public TItem SelectedItem => Controller.SelectedItem;

	/// <summary>The current action or controller error.</summary>
	public string ErrorMessage => _operationError ?? Controller.ErrorMessage;


	// Provides the grid's browser interop.
	[Inject] private IJSRuntime JSRuntime { get; set; }

	// Reports work that prevents conflicting interaction.
	private bool IsBusy => Controller.IsLoading || Controller.IsSaving || _performingAction;

	// Prevents navigation during work, edits and confirmation.
	private bool NavigationDisabled => IsBusy || Controller.IsEditing || _confirmDelete || _settingsDraft != null;

	// Determines whether the row-action column is visible.
	private bool ShowActions => Actions.Length > 0 && (GridType == 0 || GridType.HasFlag(GridTypes.ActionColumn));

	// Counts the rendered typed, legacy and action columns.
	private int ColumnCount => Math.Max(1, _columns.Length + ColumnDefinitions.Length + (ShowActions ? 1 : 0));

	// Checks whether paging currently follows the viewport.
	private bool UsesViewportPageSize => Controller.PageMode == BlazorGridPageMode.Auto && Can(DataSourceCapabilities.CanPage);

	// Builds the height constraints for the current paging mode.
	private string ViewportStyle {
		get {
			return UsesViewportPageSize || Controller.PageMode == BlazorGridPageMode.Single
				? $"height: {Height}px" : $"max-height: {Height}px;{(Controller.IsLoading && Controller.Items.Length == 0 ? $" min-height: {Height}px;" : null)}";
		}
	}

	// Combines column requirements into the minimum table width.
	private string MinimumTableWidth {
		get {
			return Math.Max(40D, _columns.Sum(PreferredColumnWidth) + ColumnDefinitions.Length * 140D + (ShowActions ? 120 : 0))
				.ToString(CultureInfo.InvariantCulture);
		}
	}

	/// <summary>Refreshes the current query and publishes any selection change.</summary>
	public Task RefreshAsync() => RefreshAsync(!SuppressLoadingIndicator);

	/// <summary>Refreshes the current query and publishes any selection change.</summary>
	public async Task RefreshAsync(bool showLoadingIndicator) {
		_operationError = null;
		await Controller.RefreshAsync(showLoadingIndicator);
		await NotifySelectionAsync();
	}

	/// <summary>Reads all entities matching the current search and sort.</summary>
	public Task<TItem[]> ReadFilteredEntitiesAsync(CancellationToken cancellationToken = default) => Controller.ReadFilteredEntitiesAsync(cancellationToken);

	/// <summary>Refreshes selected row values without replacing the current query.</summary>
	public async Task RefreshRowsAsync(IEnumerable<TItem> entities) {
		await Controller.RefreshRowsAsync(entities);
		await NotifySelectionAsync();
	}

	/// <summary>Refreshes one visible entity and reports whether it was found.</summary>
	public async Task<bool> RefreshEntityAsync(TItem entity) {
		var refreshed = await Controller.RefreshEntityAsync(entity);
		await NotifySelectionAsync();
		return refreshed;
	}

	/// <summary>Refreshes the selected entity while retaining its identity.</summary>
	public async Task<bool> RefreshSelectedEntityAsync() {
		var refreshed = await Controller.RefreshSelectedEntityAsync();
		await NotifySelectionAsync();
		return refreshed;
	}

	/// <summary>Removes the supplied entities from the rendered snapshot.</summary>
	public void RemoveRows(IEnumerable<TItem> entities) => Controller.RemoveRows(entities);

	/// <summary>Changes paging mode and saves the completed preference change.</summary>
	public async Task SetPageModeAsync(BlazorGridPageMode mode) {
		var read = Controller.SetPageModeAsync(mode);
		await SaveSettingsAsync();
		await read;
		await NotifySelectionAsync();
	}

	/// <summary>Changes the page and saves the completed preference change.</summary>
	public async Task SetPageAsync(int page) {
		var read = Controller.SetPageAsync(Math.Clamp(page, 0, TotalPages - 1));
		await SaveSettingsAsync();
		await read;
		await NotifySelectionAsync();
	}

	/// <summary>Changes the manual page length and saves the completed preference change.</summary>
	public async Task SetPageSizeAsync(int pageSize) {
		var read = Controller.SetPageSizeAsync(pageSize);
		await SaveSettingsAsync();
		await read;
		await NotifySelectionAsync();
	}

	/// <summary>Starts a buffered new-row edit.</summary>
	public void BeginCreate() {
		ResetEditor();
		Controller.BeginCreate();
	}

	/// <summary>Starts a buffered edit of the supplied entity.</summary>
	public void BeginEdit(TItem item) {
		ResetEditor();
		Controller.BeginEdit(item);
	}

	/// <summary>Discards the current buffered row edit.</summary>
	public void CancelEdit() {
		Controller.CancelEdit();
		ResetEditor();
		_ = InvokeAsync(ApplyPendingPreferencesAsync);
	}

	/// <summary>Validates and accepts the buffered row edit.</summary>
	public async Task<bool> SaveAsync() {
		if (_editErrors.Count != 0)
			return false;
		var item = Controller.EditingItem;
		var creating = Controller.IsNewItem;
		if (!await Controller.SaveAsync() || _disposed)
			return false;
		ResetEditor();
		await ApplyPendingPreferencesAsync();
		await (creating ? ItemCreated : ItemUpdated).InvokeAsync(item);
		await NotifySelectionAsync();
		return true;
	}

	// Kept for callers of the original grid. Interop attaches once and is owned by this component.
	public async Task LoadJavascript() {
		if (_disposed)
			return;
		_initializationTask ??= InitializeJavascriptAsync();
		try {
			await _initializationTask;
		} catch (JSDisconnectedException) {
			_initializationTask = null;
			_reference?.Dispose();
			_reference = null;
			throw;
		}
		if (_behavior != null && !_disposed) {
			if (_bindPending) {
				if (_settingsKey != SettingsKey)
					await ReadInitialSettingsAsync();
				await _behavior.InvokeVoidAsync("ApplyWidths", _manualWidths);
				await BindRestoredSourceAsync();
			} else if (Preferences == null && PersistSettings && !string.IsNullOrWhiteSpace(SettingsKey) && _settingsKey != SettingsKey) {
				var key = SettingsKey;
				var settings = await ReadStandaloneSettingsAsync(key);
				if (_disposed || Preferences != null || !PersistSettings || SettingsKey != key)
					return;
				_settingsKey = key;
				if (settings == null) {
					settings = CaptureSettings();
					settings.ColumnWidths.Clear();
				}
				await ApplyGridPreferencesAsync(settings);
			}
			await _behavior.InvokeVoidAsync("Update", UsesViewportPageSize && !NavigationDisabled);
		}
	}

	/// <summary>Updates automatic paging from measured complete rows.</summary>
	[JSInvokable]
	public async Task SetViewportPageSizeAsync(int pageSize) {
		if (_disposed || !UsesViewportPageSize || NavigationDisabled || pageSize == Controller.PageSize)
			return;
		await Controller.SetViewportPageSizeAsync(Math.Clamp(pageSize, 1, 9999));
		await NotifySelectionAsync();
	}

	/// <summary>Releases grid subscriptions and browser interop after pending initialization.</summary>
	public async ValueTask DisposeAsync() {
		if (_disposed)
			return;
		_disposed = true;
		Controller.Changed -= HandleChanged;
		Controller.Dispose();
		using var referenceScope = Tools.Scope.ExecuteOnDispose(() => _reference?.Dispose());
		await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			if (_initializationTask != null)
				await _initializationTask;
		}, error => error is OperationCanceledException or JSDisconnectedException);
		foreach (var reference in new[] { _behavior, _module }) {
			if (reference == null)
				continue;
			await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
				if (ReferenceEquals(reference, _behavior))
					await reference.InvokeVoidAsync("Dispose");
				await reference.DisposeAsync();
			}, error => error is OperationCanceledException or JSDisconnectedException);
		}
	}

	/// <summary>Attaches the controller's change notification.</summary>
	protected override void OnInitialized() => Controller.Changed += HandleChanged;

	/// <summary>Reconciles parameters while preserving a valid row draft.</summary>
	protected override async Task OnParametersSetAsync() {
		Guard.ArgumentNotNullOrEmpty(Id, nameof(Id));
		Guard.ArgumentNotNull(DataSource, nameof(DataSource));
		Guard.ArgumentNotNull(Actions, nameof(Actions));
		Guard.ArgumentNotNull(ColumnDefinitions, nameof(ColumnDefinitions));
		Guard.Argument(ColumnDefinitions.All(column => column is IBlazorGridLegacyColumn), nameof(ColumnDefinitions), "Legacy columns must implement IBlazorGridLegacyColumn.");
		Guard.ArgumentInRange(PageSize, 1, 9999, nameof(PageSize));
		Guard.ArgumentGT(Height, 0, nameof(Height));
		Guard.Argument(Enum.IsDefined(CaptionAlignment), nameof(CaptionAlignment), "Select a supported caption alignment.");
		Guard.Argument(Enum.IsDefined(PageMode), nameof(PageMode), "Select a supported page mode.");
		var requestedPageMode = AutoPageSize ? BlazorGridPageMode.Auto : PageMode;
		if (Preferences != null || !PersistSettings || string.IsNullOrWhiteSpace(SettingsKey))
			_settingsKey = null;

		// Column changes can request a render while the first read is pending; seed saved geometry before that render.
		if (_boundSource == null && Preferences != null)
			AcceptPreferenceValues(GetPreferenceSettings(Preferences), Preferences);
		Controller.Columns = Columns ?? (AutoGenerateColumns ? _autoColumns ??= BlazorGridColumn<TItem>.AutoGenerate() : Array.Empty<BlazorGridColumn<TItem>>());
		_columns = Controller.Columns;
		ValidatePreferenceKeys();
		foreach (var column in _columns) {
			Guard.ArgumentGT(column.ImageWidth, 0, nameof(column.ImageWidth));
			Guard.ArgumentGT(column.ImageHeight, 0, nameof(column.ImageHeight));
			Guard.Argument(Enum.IsDefined(column.ImageAlignment), nameof(column.ImageAlignment), "Select a supported image alignment.");
			Guard.Argument(!column.ImageResizeMethod.HasValue || Enum.IsDefined(column.ImageResizeMethod.Value), nameof(column.ImageResizeMethod), "Select a supported image resize method.");
		}
		if (!ReferenceEquals(_boundSource, DataSource) || _allowedCapabilities != AllowedCapabilities || _parameterPageSize != PageSize) {
			_boundSource = DataSource;
			_allowedCapabilities = AllowedCapabilities;
			_parameterPageSize = PageSize;
			_parameterPageMode = requestedPageMode;
			_confirmDelete = false;
			ResetEditor();
			if (Preferences == null && PersistSettings && !string.IsNullOrWhiteSpace(SettingsKey) && _settingsKey != SettingsKey) {
				_bindPending = true;
				return;
			}
			var settings = Preferences == null ? _appliedPreferences : GetPreferenceSettings(Preferences);
			AcceptPreferenceValues(settings, Preferences);
			await Controller.BindAsync(DataSource, AllowedCapabilities, PageSize, requestedPageMode, settings);
			_searchText = Controller.SearchTerm;
			await NotifySelectionAsync();
		} else if (_parameterPageMode != requestedPageMode) {
			_parameterPageMode = requestedPageMode;
			await SetPageModeAsync(requestedPageMode);
		}
		if (Preferences != null && (!PreferenceSettingsEqual(_appliedPreferences, GetPreferenceSettings(Preferences)) || !PreferenceSchemaEqual(Preferences)))
			await ApplyGridPreferencesAsync(Preferences);
	}

	/// <summary>Loads browser behavior after the grid has rendered.</summary>
	protected override async Task OnAfterRenderAsync(bool firstRender) {
		await Tools.Exceptions.ExecuteCapturingExceptionAsync(LoadJavascript,
			error => error is JSDisconnectedException || error is OperationCanceledException && _disposed);
	}

	// Creates the grid's browser behavior and callback reference.
	private async Task InitializeJavascriptAsync() {
		_module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Sphere10.Framework.Web.AspNetCore.Blazor/UI/Controls/BlazorGrid/BlazorGrid.razor.js");
		if (_disposed || _module == null)
			return;
		if (_bindPending)
			await ReadInitialSettingsAsync();
		_reference = DotNetObjectReference.Create(this);
		_behavior = await _module.InvokeAsync<IJSObjectReference>("Initialize", _element, _reference, _manualWidths);
	}

	// Checks that the source supplies every requested capability.
	private bool Can(DataSourceCapabilities capability) => (Controller.Capabilities & capability) == capability;

	// Checks the controller's selected entity identity.
	private bool IsSelected(TItem item) => Controller.HasSelection && Controller.EntityComparer.Equals(Controller.SelectedItem, item);

	// Schedules a render after controller state changes.
	private void HandleChanged() {
		if (!_disposed)
			_ = InvokeAsync(async () => {
				if (!_disposed) {
					if (NavigationDisabled)
						ClosePageModeMenu();
					StateHasChanged();
					await NotifySelectionAsync();
				}
			});
	}

	// Publishes a changed selection once.
	private async Task NotifySelectionAsync() {
		if (_disposed || (_wasSelected == Controller.HasSelection && (!_wasSelected || (typeof(TItem).IsValueType
			? Controller.EntityComparer.Equals(_lastSelection, Controller.SelectedItem) : ReferenceEquals(_lastSelection, Controller.SelectedItem)))))
			return;
		_wasSelected = Controller.HasSelection;
		_lastSelection = Controller.SelectedItem;
		await SelectedItemChanged.InvokeAsync(Controller.SelectedItem);
	}

	// Changes row selection when interaction permits.
	private async Task SelectRowAsync(TItem item, MouseEventArgs args) {
		if (NavigationDisabled || args.Detail > 1)
			return;
		if (IsSelected(item) && LeftClickToDeselect)
			Controller.ClearSelection();
		else
			Controller.Select(item);
		await NotifySelectionAsync();
	}

	// Starts a row edit from an editable cell.
	private async Task BeginInlineEditAsync(TItem item) {
		if (!AllowCellEditing || NavigationDisabled || !Can(DataSourceCapabilities.CanUpdate))
			return;
		BeginEdit(item);
		await NotifySelectionAsync();
	}

	// Handles keyboard selection and editing for a row.
	private async Task RowKeyDownAsync(TItem item, KeyboardEventArgs args) {
		if (args.Key == "F2")
			await BeginInlineEditAsync(item);
		else if (args.Key == "Escape" && Controller.IsEditing && !IsBusy)
			CancelEdit();
		else if ((args.Key == "Enter" || args.Key == " ") && !NavigationDisabled) {
			Controller.Select(item);
			await NotifySelectionAsync();
		}
	}

	// Starts the new-row editor and requests a render.
	private async Task BeginCreateAsync() {
		BeginCreate();
		await NotifySelectionAsync();
	}

	// Starts editing the selected entity.
	private async Task EditSelectedAsync() {
		BeginEdit(Controller.SelectedItem);
		await NotifySelectionAsync();
	}

	// Opens confirmation for deleting the selected entity.
	private void RequestDelete() {
		_operationError = null;
		_confirmDelete = true;
	}

	// Dismisses deletion confirmation.
	private void CancelDelete() => _confirmDelete = false;

	// Deletes the selected entity and publishes the completed change.
	private async Task ConfirmDeleteAsync() {
		var item = Controller.SelectedItem;
		_confirmDelete = false;
		if (await Controller.DeleteAsync() && !_disposed)
			await ItemDeleted.InvokeAsync(item);
		await NotifySelectionAsync();
	}

	// Applies the current search text.
	private async Task SearchAsync() {
		await Controller.SearchAsync(_searchText);
		await NotifySelectionAsync();
	}

	// Clears the search and refreshes the query.
	private async Task ClearSearchAsync() {
		_searchText = string.Empty;
		await SearchAsync();
	}

	// Applies a search when Enter is pressed.
	private Task SearchKeyDownAsync(KeyboardEventArgs args) => args.Key == "Enter" && !NavigationDisabled ? SearchAsync() : Task.CompletedTask;

	// Cycles the requested column's sort and saves the preference.
	private async Task SortAsync(BlazorGridColumn<TItem> column) {
		var direction = Controller.SortProperty != column.SortName ? SortDirection.Ascending : Controller.SortDirection switch {
			SortDirection.Ascending => SortDirection.Descending,
			SortDirection.Descending => SortDirection.None,
			_ => SortDirection.Ascending
		};
		var read = Controller.SortAsync(column.SortName, direction);
		await SaveSettingsAsync();
		await read;
		await NotifySelectionAsync();
	}

	// Publishes bound preferences or saves standalone values through UserSettings.
	private async Task SaveSettingsAsync() {
		if (_disposed)
			return;
		if (_pendingPreferences == null)
			_appliedPreferences = CaptureSettings();
		if (Preferences != null) {
			await PreferencesChanged.InvokeAsync(CaptureGridPreferences());
			return;
		}
		if (_behavior == null || _settingsKey == null || !PersistSettings)
			return;

		// Capture the completed edit before awaiting the facade; no JS storage path remains.
		var key = _settingsKey;
		var captured = CaptureGridPreferences();
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			var settings = await UserSettings.GetAsync<BlazorGridSettings>(key);
			if (_disposed || key != _settingsKey || Preferences != null || !PersistSettings)
				return;
			settings.PageMode = captured.PageMode;
			settings.PageSize = captured.PageSize;
			settings.PageNumber = captured.PageNumber;
			settings.SortColumnName = captured.SortColumnName;
			settings.SortDirection = captured.SortDirection;
			settings.Columns = captured.Columns;
			settings.ColumnWidths = captured.ColumnWidths;
			await settings.SaveAsync();
		}, exception => exception is JSException or Newtonsoft.Json.JsonException);
		if (error != null)
			SystemLog.Exception(error);
	}

	// Captures the current query and manual column widths.
	private BlazorGridSettings CaptureSettings() {
		return new() {
			PageMode = Controller.PageMode,
			PageSize = Controller.ManualPageSize,
			PageNumber = Controller.CurrentPage,
			SortColumnName = Controller.SortProperty,
			SortDirection = Controller.SortDirection,
			ColumnWidths = new Dictionary<string, double>(_manualWidths, StringComparer.Ordinal)
		};
	}

	// Parses the page navigator's one-based input.
	private Task ChangePageAsync(ChangeEventArgs args) => int.TryParse(args.Value?.ToString(), out var page) ? SetPageAsync(page > 0 ? page - 1 : 0) : Task.CompletedTask;

	// Mode selection changes only the existing controller mode; Manual is the presentation label for Paged.
	private async Task SelectPageModeAsync(BlazorGridPageMode mode) {
		ClosePageModeMenu();
		await SetPageModeAsync(mode);
		if (!_disposed)
			await _pageModeButton.FocusAsync();
	}

	// The compact trigger follows the same dismissal-button pattern as other Framework popup menus.
	private void TogglePageModeMenu() {
		if (!NavigationDisabled)
			_pageModeMenuOpen = !_pageModeMenuOpen;
	}

	// Dismissal never changes the saved paging choice.
	private void ClosePageModeMenu() => _pageModeMenuOpen = false;

	// Escape restores the compact trigger without accepting a menu item.
	private async Task PageModeKeyDownAsync(KeyboardEventArgs args) {
		if (args.Key == "Escape" && _pageModeMenuOpen) {
			ClosePageModeMenu();
			await _pageModeButton.FocusAsync();
		}
	}

	// Keep the persisted enum and its three user-facing choices independent.
	private static string PageModeLabel(BlazorGridPageMode mode) {
		return mode switch {
			BlazorGridPageMode.Single => "Single",
			BlazorGridPageMode.Auto => "Auto",
			_ => "Manual"
		};
	}

	// Runs a row action and refreshes after successful completion.
	private async Task PerformActionAsync(GridAction<TItem> action, TItem item) {
		if (NavigationDisabled || !(action.IsActionAvailable?.Invoke(item) ?? true))
			return;
		_performingAction = true;
		_operationError = null;
		using var scope = Tools.Scope.ExecuteOnDispose(() => _performingAction = false);
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (action.ActionWorkAsync != null)
				await action.ActionWorkAsync(item);
			else
				action.ActionWork?.Invoke(item);
			if (!_disposed)
				await RefreshAsync();
		});
		if (error != null) {
			if (!_disposed)
				_operationError = error.Message;
		}
	}

	// Clears editor validation and confirmation state.
	private void ResetEditor() {
		_editErrors.Clear();
		_operationError = null;
	}

	// Parses a cell input into the row draft and records conversion errors.
	private Task SetEditValueAsync(BlazorGridColumn<TItem> column, object value) {
		if (_disposed || !Controller.IsEditing || Controller.IsSaving)
			return Task.CompletedTask;
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			Controller.SetEditValue(column, Tools.BlazorGrid.ParseValue(value, column.DataType));
			_editErrors.Remove(column);
		}, out var error);
		if (error != null) {
			_editErrors[column] = error.Message;
		}
		StateHasChanged();
		return Task.CompletedTask;
	}

	// Builds the configured or default buffered cell editor.
	private RenderFragment RenderEditor(BlazorGridColumn<TItem> column) {
		return builder => {
			if (!column.CanEdit(Controller.EditingItem)) {
				builder.AddContent(0, FormatCell(Controller.EditingItem, column));
				return;
			}
			var context = new BlazorGridCellEditContext<TItem> {
				Item = Controller.EditingItem,
				Column = column,
				Value = Controller.GetEditValue(column),
				ValueChanged = EventCallback.Factory.Create<object>(this, value => SetEditValueAsync(column, value)),
				IsNewItem = Controller.IsNewItem
			};
			builder.OpenElement(1, "fieldset");
			builder.AddAttribute(2, "class", "sphere10-grid-editor");
			builder.AddAttribute(11, "disabled", IsBusy);
			if (column.EditorTemplate != null)
				builder.AddContent(3, column.EditorTemplate(context));
			else {
				builder.OpenComponent<BlazorGridCellEditor<TItem>>(4);
				builder.AddAttribute(5, nameof(BlazorGridCellEditor<TItem>.Context), context);
				builder.AddAttribute(6, nameof(BlazorGridCellEditor<TItem>.Disabled), IsBusy);
				builder.CloseComponent();
			}
			if (_editErrors.TryGetValue(column, out var message)) {
				builder.OpenElement(7, "span");
				builder.AddAttribute(8, "role", "alert");
				builder.AddAttribute(9, "class", "sphere10-grid-error");
				builder.AddContent(10, message);
				builder.CloseElement();
			}
			builder.CloseElement();
		};
	}

	// The initial HTML uses restored manual widths before JavaScript performs any natural sizing.
	private string ColumnStyle(BlazorGridColumn<TItem> column) => $"width: {PreferredColumnWidth(column).ToString(CultureInfo.InvariantCulture)}px";

	// Unspecified columns retain their configured natural/stretch minimum.
	private double PreferredColumnWidth(BlazorGridColumn<TItem> column)
		=> column.PreferenceKey != null && _manualWidths.TryGetValue(column.PreferenceKey, out var width) ? width : Math.Max(40, column.Width);

	// Maps shared image alignment to CSS object position.
	private static string ImagePosition(ResizeAlignment alignment) {
		return alignment switch {
			ResizeAlignment.TopLeft => "left top",
			ResizeAlignment.TopCenter => "center top",
			ResizeAlignment.TopRight => "right top",
			ResizeAlignment.CenterLeft => "left center",
			ResizeAlignment.CenterRight => "right center",
			ResizeAlignment.BottomLeft => "left bottom",
			ResizeAlignment.BottomCenter => "center bottom",
			ResizeAlignment.BottomRight => "right bottom",
			_ => "center center"
		};
	}

	// Maps shared image alignment to CSS vertical alignment.
	private static string ImageVerticalAlignment(ResizeAlignment alignment) {
		return alignment switch {
			ResizeAlignment.TopLeft or ResizeAlignment.TopCenter or ResizeAlignment.TopRight => "top",
			ResizeAlignment.BottomLeft or ResizeAlignment.BottomCenter or ResizeAlignment.BottomRight => "bottom",
			_ => "middle"
		};
	}

	// Maps shared resize behavior to CSS object fit.
	private static string ImageFit(ResizeMethod? fit) {
		return fit switch {
			ResizeMethod.Stretch => "fill",
			ResizeMethod.AspectFill => "cover",
			ResizeMethod.AspectFit or ResizeMethod.AspectFitPadded => "contain",
			_ => "none"
		};
	}

	// Checks whether the cell has an available image value.
	private static bool HasImage(TItem item, BlazorGridColumn<TItem> column) => column.ImageSource != null && (column.PropertyHasValue?.Invoke(item) ?? true);

	// Describes the column's current sort for assistive technology.
	private string GetAriaSort(BlazorGridColumn<TItem> column) {
		return Controller.SortProperty != column.SortName || string.IsNullOrEmpty(column.SortName) || Controller.SortDirection == SortDirection.None ? null
				: Controller.SortDirection == SortDirection.Ascending ? "ascending" : "descending";
	}

	// Formats a cell value with the current culture.
	private static string FormatCell(TItem item, BlazorGridColumn<TItem> column) {
		if (!(column.PropertyHasValue?.Invoke(item) ?? true))
			return string.Empty;
		var value = column.PropertyValue?.Invoke(item);
		return value is IFormattable formattable
			? formattable.ToString(column.Format, CultureInfo.CurrentCulture)
			: value?.ToString() ?? string.Empty;
	}

	// Reads the caption from a legacy column.
	private static string GetLegacyColumnName(object column) => ((IBlazorGridLegacyColumn)column).Name;

	// Renders the supplied legacy column for one row.
	private static RenderFragment RenderLegacyColumn(TItem item, object column) => builder => ((IBlazorGridLegacyColumn)column).Render(item, builder);

	#region Inner Types

	/// <summary>Traits controlling grid presentation and interaction.</summary>
	[Flags]
	public enum GridTypes {
		/// <summary>Rows are supplied directly by the configured source.</summary>
		DataSupplied = 1,
		/// <summary>Rows are requested as the visible query changes.</summary>
		DataOnDemand = 1 << 1,
		/// <summary>Shows the declared row actions.</summary>
		ActionColumn = 1 << 2,
		/// <summary>Shows the create-row command when the source permits it.</summary>
		NewItem = 1 << 3,

		/// <summary>Allows the grid's Settings button to edit its supplied settings value.</summary>
		AllowsSettingsChange = 1 << 4
	}

	#endregion
}
