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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;

public partial class BlazorGrid<TItem> : IAsyncDisposable {
	private readonly IBlazorGridController<TItem> _controller = new BlazorGridController<TItem>();
	private readonly Dictionary<BlazorGridColumn<TItem>, string> _editErrors = new();
	private BlazorGridColumn<TItem>[] _autoColumns;
	private BlazorGridColumn<TItem>[] _columns = Array.Empty<BlazorGridColumn<TItem>>();
	private ElementReference _element;
	private Task _initializationTask;
	private IJSObjectReference _module;
	private IJSObjectReference _behavior;
	private DotNetObjectReference<BlazorGrid<TItem>> _reference;
	private bool _disposed;
	private bool _confirmDelete;
	private bool _performingAction;
	private bool _wasSelected;
	private TItem _lastSelection;
	private string _searchText = string.Empty;
	private string _operationError;
	private int _parameterPageSize;
	private IDataSource<TItem> _boundSource;
	private DataSourceCapabilities _allowedCapabilities;

	[Inject] private IJSRuntime JSRuntime { get; set; }

	[Parameter] public string Id { get; set; } = $"sphere10-grid-{Guid.NewGuid():N}";

	[Parameter] public string Caption { get; set; } = "Data grid";

	[Parameter] public string EmptyText { get; set; } = "No items found.";

	[Parameter] public int PageSize { get; set; } = 10;

	[Parameter] public IDataSource<TItem> DataSource { get; set; }

	[Parameter] public DataSourceCapabilities AllowedCapabilities { get; set; } = DataSourceCapabilities.Default;

	[Parameter] public BlazorGridColumn<TItem>[] Columns { get; set; }

	[Parameter] public bool AutoGenerateColumns { get; set; } = true;

	[Parameter] public bool AllowCellEditing { get; set; } = true;

	[Parameter] public bool LeftClickToDeselect { get; set; } = true;

	[Parameter] public bool AutoPageSize { get; set; }

	[Parameter] public int Height { get; set; } = 420;

	[Parameter] public GridTypes GridType { get; set; }

	[Parameter] public GridAction<TItem>[] Actions { get; set; } = Array.Empty<GridAction<TItem>>();

	[Parameter] public object[] ColumnDefinitions { get; set; } = Array.Empty<object>();

	[Parameter] public EventCallback<TItem> SelectedItemChanged { get; set; }

	[Parameter] public EventCallback<TItem> ItemCreated { get; set; }

	[Parameter] public EventCallback<TItem> ItemUpdated { get; set; }

	[Parameter] public EventCallback<TItem> ItemDeleted { get; set; }

	public IBlazorGridController<TItem> Controller => _controller;

	public int CurrentPage => Controller.CurrentPage;

	public int TotalDataCount => Controller.TotalCount;

	public int TotalPages => Controller.TotalPages;

	public TItem SelectedItem => Controller.SelectedItem;

	public string ErrorMessage => _operationError ?? Controller.ErrorMessage;

	private bool IsBusy => Controller.IsLoading || Controller.IsSaving || _performingAction;

	private bool NavigationDisabled => IsBusy || Controller.IsEditing || _confirmDelete;

	private bool ShowActions => Actions.Length > 0 && (GridType == 0 || GridType.HasFlag(GridTypes.ActionColumn));

	private int ColumnCount => Math.Max(1, _columns.Length + ColumnDefinitions.Length + (ShowActions ? 1 : 0));

	private long MinimumTableWidth => Math.Max(40L, _columns.Sum(column => (long)Math.Max(40, column.Width)) + ColumnDefinitions.Length * 140L + (ShowActions ? 120 : 0));

	public async Task RefreshAsync() {
		_operationError = null;
		await Controller.RefreshAsync();
		await NotifySelectionAsync();
	}

	public async Task SetPageAsync(int page) {
		await Controller.SetPageAsync(Math.Clamp(page, 0, TotalPages - 1));
		await NotifySelectionAsync();
	}

	public async Task SetPageSizeAsync(int pageSize) {
		await Controller.SetPageSizeAsync(pageSize);
		await NotifySelectionAsync();
	}

	public void BeginCreate() {
		ResetEditor();
		Controller.BeginCreate();
	}

	public void BeginEdit(TItem item) {
		ResetEditor();
		Controller.BeginEdit(item);
	}

	public void CancelEdit() {
		Controller.CancelEdit();
		ResetEditor();
	}

	public async Task<bool> SaveAsync() {
		if (_editErrors.Count != 0)
			return false;
		var item = Controller.EditingItem;
		var creating = Controller.IsNewItem;
		if (!await Controller.SaveAsync() || _disposed)
			return false;
		ResetEditor();
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
		if (_behavior != null && !_disposed)
			await _behavior.InvokeVoidAsync("update", AutoPageSize && Can(DataSourceCapabilities.CanPage) && !NavigationDisabled);
	}

	[JSInvokable]
	public async Task SetViewportPageSizeAsync(int pageSize) {
		if (_disposed || !AutoPageSize || NavigationDisabled || !Can(DataSourceCapabilities.CanPage) || pageSize == Controller.PageSize)
			return;
		await SetPageSizeAsync(Math.Clamp(pageSize, 1, 9999));
	}

	public async ValueTask DisposeAsync() {
		if (_disposed)
			return;
		_disposed = true;
		Controller.Changed -= HandleChanged;
		Controller.Dispose();
		using var referenceScope = Tools.Scope.ExecuteOnDispose(() => _reference?.Dispose());
		try {
			if (_initializationTask != null)
				await _initializationTask;
		} catch (OperationCanceledException) {
			// Browser shutdown can cancel initialization during disposal.
		} catch (JSDisconnectedException) {
			// Initialization can outlive the circuit.
		}
		foreach (var reference in new[] { _behavior, _module }) {
			if (reference == null)
				continue;
			try {
				await reference.DisposeAsync();
			} catch (OperationCanceledException) {
				// Cancellation while releasing owned browser objects is ordinary teardown.
			} catch (JSDisconnectedException) {
				// The browser-side observer releases listeners when the grid leaves the DOM.
			}
		}
	}
	protected override void OnInitialized() => Controller.Changed += HandleChanged;

	protected override async Task OnParametersSetAsync() {
		Guard.ArgumentNotNullOrEmpty(Id, nameof(Id));
		Guard.ArgumentNotNull(DataSource, nameof(DataSource));
		Guard.ArgumentNotNull(Actions, nameof(Actions));
		Guard.ArgumentNotNull(ColumnDefinitions, nameof(ColumnDefinitions));
		Guard.Argument(ColumnDefinitions.All(column => column is IBlazorGridLegacyColumn), nameof(ColumnDefinitions), "Legacy columns must implement IBlazorGridLegacyColumn.");
		Guard.ArgumentInRange(PageSize, 1, 9999, nameof(PageSize));
		Guard.ArgumentGT(Height, 0, nameof(Height));
		Controller.Columns = Columns ?? (AutoGenerateColumns ? _autoColumns ??= BlazorGridColumn<TItem>.AutoGenerate() : Array.Empty<BlazorGridColumn<TItem>>());
		_columns = Controller.Columns;
		if (!ReferenceEquals(_boundSource, DataSource) || _allowedCapabilities != AllowedCapabilities || _parameterPageSize != PageSize) {
			_boundSource = DataSource;
			_allowedCapabilities = AllowedCapabilities;
			_parameterPageSize = PageSize;
			_confirmDelete = false;
			ResetEditor();
			await Controller.BindAsync(DataSource, AllowedCapabilities, PageSize);
			_searchText = Controller.SearchTerm;
			await NotifySelectionAsync();
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender) {
		try {
			await LoadJavascript();
		} catch (OperationCanceledException) when (_disposed) {
			// A pending render can observe browser teardown cancellation.
		} catch (JSDisconnectedException) {
			// No interop work can continue after the circuit disconnects.
		}
	}

	private async Task InitializeJavascriptAsync() {
		_module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Sphere10.Framework.Web.AspNetCore.Blazor/UI/Controls/BlazorGrid/BlazorGrid.razor.js");
		if (_disposed || _module == null)
			return;
		_reference = DotNetObjectReference.Create(this);
		_behavior = await _module.InvokeAsync<IJSObjectReference>("initialize", _element, _reference);
	}

	private bool Can(DataSourceCapabilities capability) => (Controller.Capabilities & capability) == capability;

	private bool IsSelected(TItem item) => Controller.HasSelection && Controller.EntityComparer.Equals(Controller.SelectedItem, item);

	private void HandleChanged() {
		if (!_disposed)
			_ = InvokeAsync(() => {
				if (!_disposed)
					StateHasChanged();
			});
	}

	private async Task NotifySelectionAsync() {
		if (_disposed || (_wasSelected == Controller.HasSelection && (!_wasSelected || Controller.EntityComparer.Equals(_lastSelection, Controller.SelectedItem))))
			return;
		_wasSelected = Controller.HasSelection;
		_lastSelection = Controller.SelectedItem;
		await SelectedItemChanged.InvokeAsync(Controller.SelectedItem);
	}

	private async Task SelectRowAsync(TItem item, MouseEventArgs args) {
		if (NavigationDisabled || args.Detail > 1)
			return;
		if (IsSelected(item) && LeftClickToDeselect)
			Controller.ClearSelection();
		else
			Controller.Select(item);
		await NotifySelectionAsync();
	}

	private async Task BeginInlineEditAsync(TItem item) {
		if (!AllowCellEditing || NavigationDisabled || !Can(DataSourceCapabilities.CanUpdate))
			return;
		BeginEdit(item);
		await NotifySelectionAsync();
	}

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

	private async Task BeginCreateAsync() {
		BeginCreate();
		await NotifySelectionAsync();
	}

	private async Task EditSelectedAsync() {
		BeginEdit(Controller.SelectedItem);
		await NotifySelectionAsync();
	}

	private void RequestDelete() {
		_operationError = null;
		_confirmDelete = true;
	}

	private void CancelDelete() => _confirmDelete = false;

	private async Task ConfirmDeleteAsync() {
		var item = Controller.SelectedItem;
		_confirmDelete = false;
		if (await Controller.DeleteAsync() && !_disposed)
			await ItemDeleted.InvokeAsync(item);
		await NotifySelectionAsync();
	}

	private async Task SearchAsync() {
		await Controller.SearchAsync(_searchText);
		await NotifySelectionAsync();
	}

	private async Task ClearSearchAsync() {
		_searchText = string.Empty;
		await SearchAsync();
	}

	private Task SearchKeyDownAsync(KeyboardEventArgs args) => args.Key == "Enter" && !NavigationDisabled ? SearchAsync() : Task.CompletedTask;

	private async Task SortAsync(BlazorGridColumn<TItem> column) {
		var direction = Controller.SortProperty == column.SortName && Controller.SortDirection == SortDirection.Ascending
			? SortDirection.Descending : SortDirection.Ascending;
		await Controller.SortAsync(column.SortName, direction);
		await NotifySelectionAsync();
	}

	private Task ChangePageAsync(ChangeEventArgs args) =>
		int.TryParse(args.Value?.ToString(), out var page) ? SetPageAsync(page > 0 ? page - 1 : 0) : Task.CompletedTask;

	private Task ChangePageSizeAsync(ChangeEventArgs args) =>
		int.TryParse(args.Value?.ToString(), out var pageSize) && pageSize is >= 1 and <= 9999 ? SetPageSizeAsync(pageSize) : Task.CompletedTask;

	private async Task PerformActionAsync(GridAction<TItem> action, TItem item) {
		if (NavigationDisabled || !(action.IsActionAvailable?.Invoke(item) ?? true))
			return;
		_performingAction = true;
		_operationError = null;
		using var scope = Tools.Scope.ExecuteOnDispose(() => _performingAction = false);
		try {
			if (action.ActionWorkAsync != null)
				await action.ActionWorkAsync(item);
			else
				action.ActionWork?.Invoke(item);
			if (!_disposed)
				await RefreshAsync();
		} catch (Exception error) {
			if (!_disposed)
				_operationError = error.Message;
		}
	}

	private void ResetEditor() {
		_editErrors.Clear();
		_operationError = null;
	}

	private Task SetEditValueAsync(BlazorGridColumn<TItem> column, object value) {
		if (_disposed || !Controller.IsEditing || Controller.IsSaving)
			return Task.CompletedTask;
		try {
			Controller.SetEditValue(column, Tools.BlazorGrid.ParseValue(value, column.DataType));
			_editErrors.Remove(column);
		} catch (Exception error) {
			_editErrors[column] = error.Message;
		}
		StateHasChanged();
		return Task.CompletedTask;
	}

	private RenderFragment RenderEditor(BlazorGridColumn<TItem> column) => builder => {
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

	private static string ColumnStyle(BlazorGridColumn<TItem> column) => $"width: {Math.Max(40, column.Width)}px";

	private string GetAriaSort(BlazorGridColumn<TItem> column) =>
		Controller.SortProperty != column.SortName || string.IsNullOrEmpty(column.SortName) ? null
			: Controller.SortDirection == SortDirection.Ascending ? "ascending" : "descending";

	private static string FormatCell(TItem item, BlazorGridColumn<TItem> column) {
		if (!(column.PropertyHasValue?.Invoke(item) ?? true))
			return string.Empty;
		var value = column.PropertyValue?.Invoke(item);
		return value is IFormattable formattable
			? formattable.ToString(column.Format, CultureInfo.CurrentCulture)
			: value?.ToString() ?? string.Empty;
	}

	private static string GetLegacyColumnName(object column) => ((IBlazorGridLegacyColumn)column).Name;

	private static RenderFragment RenderLegacyColumn(TItem item, object column) => builder => ((IBlazorGridLegacyColumn)column).Render(item, builder);

	[Flags]
	public enum GridTypes {
		DataSupplied = 1,
		DataOnDemand = 1 << 1,
		ActionColumn = 1 << 2,
		NewItem = 1 << 3
	}
}
