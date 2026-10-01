// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Serializes source I/O and keeps editing values separate until an explicit save.</summary>
public class BlazorGridController<TItem> : BlazorGridControllerBase<TItem> {
	private readonly SemaphoreSlim _operations = new(1, 1);
	private IDataSource<TItem> _source;
	private DataSourceCapabilities _allowedCapabilities;
	private DataSourceCapabilities _capabilities;
	private TItem[] _items = Array.Empty<TItem>();
	private BlazorGridColumn<TItem>[] _columns = Array.Empty<BlazorGridColumn<TItem>>();
	private IEqualityComparer<TItem> _entityComparer = ComparerFactory.Default.GetEqualityComparer<TItem>();
	private string[] _validationErrors = Array.Empty<string>();
	private Task _pendingRead = Task.CompletedTask;
	private TaskCompletionSource _readCompletion;
	private EditSession _edit;
	private TItem _selectedItem;
	private bool _hasSelection;
	private bool _isLoading;
	private bool _isSaving;
	private bool _disposed;
	private int _currentPage;
	private int _pageSize = 10;
	private int _totalCount;
	private long _queryVersion;
	private long _configurationVersion;
	private long _writeVersion;
	private string _searchTerm = string.Empty;
	private string _sortProperty;
	private SortDirection _sortDirection;
	private string _errorMessage;

	public override TItem[] Items => Tools.Array.Clone(_items);

	public override int CurrentPage => _currentPage;

	public override int PageSize => _pageSize;

	public override int TotalCount => _totalCount;

	public override int TotalPages => HasCapability(DataSourceCapabilities.CanPage) ? Math.Max(1, (int)(((long)_totalCount + _pageSize - 1) / _pageSize)) : 1;

	public override DataSourceCapabilities Capabilities => _capabilities;

	public override string SearchTerm => _searchTerm;

	public override string SortProperty => _sortProperty;

	public override SortDirection SortDirection => _sortDirection;

	public override bool IsLoading => _isLoading;

	public override bool IsSaving => _isSaving;

	public override bool HasSelection => _hasSelection;

	public override TItem SelectedItem => _selectedItem;

	public override bool IsEditing => _edit != null;

	public override bool IsNewItem => _edit?.IsNew ?? false;

	public override TItem EditingItem => _edit == null ? default : _edit.Item;

	public override string[] ValidationErrors => Tools.Array.Clone(_validationErrors);

	public override string ErrorMessage => _errorMessage;

	public override BlazorGridColumn<TItem>[] Columns {
		get => Tools.Array.Clone(_columns);
		set {
			EnsureUsable();
			Guard.ArgumentNotNull(value, nameof(value));
			Guard.Argument(value.All(column => column != null), nameof(value), "Columns cannot contain null.");
			if (_columns.SequenceEqual(value))
				return;
			_columns = Tools.Array.Clone(value);
			InvalidateEdit();
			ClearProblems();
			OnChanged();
		}
	}

	public override IEqualityComparer<TItem> EntityComparer {
		get => _entityComparer;
		set {
			EnsureNoDraft();
			Guard.ArgumentNotNull(value, nameof(value));
			_entityComparer = value;
			ReconcileSelection();
			OnChanged();
		}
	}

	public override Task BindAsync(IDataSource<TItem> source, DataSourceCapabilities allowedCapabilities, int pageSize) {
		EnsureUsable();
		Guard.ArgumentInRange(pageSize, 1, 9999, nameof(pageSize));
		if (ReferenceEquals(_source, source) && _allowedCapabilities == allowedCapabilities && _pageSize == pageSize)
			return _pendingRead;
		_source = source;
		_allowedCapabilities = allowedCapabilities;
		_pageSize = pageSize;
		_capabilities = 0;
		_currentPage = 0;
		_totalCount = 0;
		_searchTerm = string.Empty;
		_sortProperty = null;
		_sortDirection = SortDirection.None;
		_items = Array.Empty<TItem>();
		ClearSelectionCore();
		InvalidateEdit();
		return QueueReadAsync();
	}

	public override Task RefreshAsync() {
		EnsureNoDraft();
		return QueueReadAsync();
	}

	public override Task SetPageAsync(int page) {
		EnsureNoDraft();
		Guard.ArgumentGTE(page, 0, nameof(page));
		if (!RequireCapability(DataSourceCapabilities.CanPage))
			return Task.CompletedTask;
		_currentPage = Math.Min(page, TotalPages - 1);
		ClearVisibleItems();
		return QueueReadAsync();
	}

	public override Task SetPageSizeAsync(int pageSize) {
		EnsureNoDraft();
		Guard.ArgumentInRange(pageSize, 1, 9999, nameof(pageSize));
		_pageSize = pageSize;
		_currentPage = 0;
		ClearVisibleItems();
		return QueueReadAsync();
	}

	public override Task SearchAsync(string searchTerm) {
		EnsureNoDraft();
		if (!RequireCapability(DataSourceCapabilities.CanSearch))
			return Task.CompletedTask;
		_searchTerm = searchTerm ?? string.Empty;
		_currentPage = 0;
		ClearVisibleItems();
		return QueueReadAsync();
	}

	public override Task SortAsync(string property, SortDirection direction) {
		EnsureNoDraft();
		Guard.Argument(direction is SortDirection.None or SortDirection.Ascending or SortDirection.Descending, nameof(direction), "Unknown sort direction.");
		if (!RequireCapability(DataSourceCapabilities.CanSort))
			return Task.CompletedTask;
		Guard.Argument(direction == SortDirection.None || !string.IsNullOrWhiteSpace(property), nameof(property), "A sort property is required.");
		_sortProperty = direction == SortDirection.None ? null : property;
		_sortDirection = direction;
		_currentPage = 0;
		ClearVisibleItems();
		return QueueReadAsync();
	}

	public override void Select(TItem item) {
		EnsureNoDraft();
		var index = FindVisibleIndex(item);
		Guard.Argument(index >= 0, nameof(item), "Only a visible item can be selected.");
		_selectedItem = _items[index];
		_hasSelection = true;
		OnChanged();
	}

	public override void ClearSelection() {
		EnsureNoDraft();
		ClearSelectionCore();
		OnChanged();
	}

	public override void BeginCreate() {
		EnsureNoDraft();
		Guard.Ensure(!_isLoading, "Wait for loading to finish before editing.");
		if (!RequireCapability(DataSourceCapabilities.CanCreate))
			return;
		ClearSelectionCore();
		BeginEditCore(() => _source.New(), true);
	}

	public override void BeginEdit(TItem item) {
		EnsureNoDraft();
		Guard.Ensure(!_isLoading, "Wait for loading to finish before editing.");
		if (!RequireCapability(DataSourceCapabilities.CanUpdate))
			return;
		var index = FindVisibleIndex(item);
		Guard.Argument(index >= 0, nameof(item), "Only a visible item can be edited.");
		_selectedItem = _items[index];
		_hasSelection = true;
		BeginEditCore(() => _items[index], false);
	}

	public override void SetEditValue(BlazorGridColumn<TItem> column, object value) {
		EnsureUsable();
		Guard.Ensure(_edit != null && !_isSaving, "Begin an edit before changing values.");
		Guard.ArgumentNotNull(column, nameof(column));
		Guard.Argument(_edit.Values.ContainsKey(column), nameof(column), "The column is not editable for this item.");
		_edit.Values[column] = value;
		ClearProblems();
		OnChanged();
	}

	public override object GetEditValue(BlazorGridColumn<TItem> column) {
		EnsureUsable();
		Guard.ArgumentNotNull(column, nameof(column));
		Guard.Ensure(_edit != null, "There is no edit in progress.");
		return _edit.Values.TryGetValue(column, out var value) ? value : column.PropertyValue(_edit.Item);
	}

	public override void CancelEdit() {
		EnsureUsable();
		Guard.Ensure(!_isSaving, "Wait for the save to finish before cancelling.");
		_edit = null;
		ClearProblems();
		OnChanged();
	}

	public override async Task<bool> SaveAsync() {
		EnsureUsable();
		if (_edit == null || _isSaving)
			return false;
		var edit = _edit;
		var source = _source;
		var configuration = _configurationVersion;
		if (!RequireCapability(edit.IsNew ? DataSourceCapabilities.CanCreate : DataSourceCapabilities.CanUpdate))
			return false;
		var write = BeginWrite();
		var saved = false;
		using (Tools.Scope.ExecuteOnDispose(() => EndWrite(write))) {
			await _operations.WaitAsync();
			using var operation = Tools.Scope.ExecuteOnDispose(() => _operations.Release());
			if (!IsCurrentEdit(edit, configuration))
				return false;
			var applied = new List<BlazorGridColumn<TItem>>();
			var committed = false;
			using var rollback = Tools.Scope.ExecuteOnDispose(() => {
				if (!committed)
					RestoreValues(edit, applied);
			});
			try {
				var available = await source.CapabilitiesAsync;
				if (!IsCurrentEdit(edit, configuration))
					return false;
				_capabilities = available & _allowedCapabilities;
				if (!RequireCapability(edit.IsNew ? DataSourceCapabilities.CanCreate : DataSourceCapabilities.CanUpdate)) {
					InvalidateEdit();
					OnChanged();
					return false;
				}
				// Apply only changed buffered values; referenced objects retain their identity.
				foreach (var value in edit.Values.Where(value => ValuesDiffer(edit.OriginalValues[value.Key], value.Value))) {
					Guard.Ensure(value.Key.CanEdit(edit.Item), $"{value.Key.ColumnName} is no longer editable.");
					applied.Add(value.Key);
					value.Key.SetPropertyValue(edit.Item, value.Value);
				}
				var validation = await source.ValidateAsync(edit.Item, edit.IsNew ? CrudAction.Create : CrudAction.Update);
				if (!IsCurrentEdit(edit, configuration))
					return false;
				if (!AcceptValidation(validation))
					return false;
				var currentCapabilities = await source.CapabilitiesAsync;
				if (!IsCurrentEdit(edit, configuration))
					return false;
				_capabilities = currentCapabilities & _allowedCapabilities;
				if (!RequireCapability(edit.IsNew ? DataSourceCapabilities.CanCreate : DataSourceCapabilities.CanUpdate)) {
					_edit = null;
					return false;
				}
				if (edit.IsNew)
					await source.CreateAsync(edit.Item);
				else
					await source.UpdateAsync(edit.Item);
				committed = true;
				if (!IsCurrentEdit(edit, configuration))
					return false;
				_edit = null;
				saved = true;
			} catch (Exception error) {
				if (IsCurrentEdit(edit, configuration))
					_errorMessage = error.Message;
			}
		}
		if (saved)
			await QueueReadAsync();
		return saved;
	}

	public override async Task<bool> DeleteAsync() {
		EnsureNoDraft();
		if (!_hasSelection || !RequireCapability(DataSourceCapabilities.CanDelete))
			return false;
		var item = _selectedItem;
		var source = _source;
		var configuration = _configurationVersion;
		var write = BeginWrite();
		var deleted = false;
		using (Tools.Scope.ExecuteOnDispose(() => EndWrite(write))) {
			await _operations.WaitAsync();
			using var operation = Tools.Scope.ExecuteOnDispose(() => _operations.Release());
			if (!IsCurrentConfiguration(configuration))
				return false;
			try {
				var available = await source.CapabilitiesAsync;
				if (!IsCurrentConfiguration(configuration))
					return false;
				_capabilities = available & _allowedCapabilities;
				if (!RequireCapability(DataSourceCapabilities.CanDelete))
					return false;
				var validation = await source.ValidateAsync(item, CrudAction.Delete);
				if (!IsCurrentConfiguration(configuration) || !AcceptValidation(validation))
					return false;
				var currentCapabilities = await source.CapabilitiesAsync;
				if (!IsCurrentConfiguration(configuration))
					return false;
				_capabilities = currentCapabilities & _allowedCapabilities;
				if (!RequireCapability(DataSourceCapabilities.CanDelete))
					return false;
				await source.DeleteAsync(item);
				if (!IsCurrentConfiguration(configuration))
					return false;
				ClearSelectionCore();
				deleted = true;
			} catch (Exception error) {
				if (IsCurrentConfiguration(configuration))
					_errorMessage = error.Message;
			}
		}
		if (deleted)
			await QueueReadAsync();
		return deleted;
	}

	protected override void FreeManagedResources() {
		if (_disposed)
			return;
		_disposed = true;
		_queryVersion++;
		InvalidateEdit();
		_source = null;
		_items = Array.Empty<TItem>();
		_isLoading = false;
		ClearSelectionCore();
		// Outstanding source calls cannot be cancelled; their completions release the gate and are ignored.
	}

	protected override ValueTask FreeManagedResourcesAsync() {
		FreeManagedResources();
		return ValueTask.CompletedTask;
	}

	private Task QueueReadAsync() {
		_queryVersion++;
		_isLoading = true;
		ClearProblems();
		if (_readCompletion == null) {
			_readCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_pendingRead = _readCompletion.Task;
			OnChanged();
			// The worker routes every completion/failure to the shared task returned to all callers.
			_ = DrainReadsAsync(_readCompletion);
		} else {
			OnChanged();
		}
		return _pendingRead;
	}

	private async Task DrainReadsAsync(TaskCompletionSource completion) {
		try {
			while (!_disposed) {
				var version = _queryVersion;
				await ReadAsync(version);
				if (version == _queryVersion)
					break;
			}
			_readCompletion = null;
			_isLoading = false;
			if (!_disposed)
				OnChanged();
			completion.TrySetResult();
		} catch (Exception error) {
			_readCompletion = null;
			_isLoading = false;
			completion.TrySetException(error);
		}
	}

	private async Task ReadAsync(long version) {
		await _operations.WaitAsync();
		using var operation = Tools.Scope.ExecuteOnDispose(() => _operations.Release());

		if (!IsCurrentQuery(version))
			return;
		var source = _source;
		if (source == null)
			return;
		try {
			var capabilities = await source.CapabilitiesAsync;
			if (!IsCurrentQuery(version))
				return;
			_capabilities = capabilities & _allowedCapabilities;
			if (!HasCapability(DataSourceCapabilities.CanRead)) {
				ClearVisibleItems();
				_totalCount = 0;
				_currentPage = 0;
				return;
			}
			if (!HasCapability(DataSourceCapabilities.CanSearch))
				_searchTerm = string.Empty;
			if (!HasCapability(DataSourceCapabilities.CanSort)) {
				_sortProperty = null;
				_sortDirection = SortDirection.None;
			}
			var canPage = HasCapability(DataSourceCapabilities.CanPage);
			var page = canPage ? _currentPage : 0;
			var pageSize = canPage ? _pageSize : int.MaxValue;
			var result = await source.ReadRangeAsync(_searchTerm, pageSize, page, _sortProperty, _sortDirection);
			if (!IsCurrentQuery(version))
				return;
			Guard.Ensure(result.TotalCount >= 0, "The data source returned a negative total count.");
			var totalPages = canPage ? Math.Max(1, (int)(((long)result.TotalCount + _pageSize - 1) / _pageSize)) : 1;
			var lastPage = totalPages - 1;
			// Some sources report an empty obsolete page after the last row is deleted.
			if (canPage && result.TotalCount > 0 && result.Page > lastPage) {
				result = await source.ReadRangeAsync(_searchTerm, pageSize, lastPage, _sortProperty, _sortDirection);
				if (!IsCurrentQuery(version))
					return;
			}
			_items = (result.Items ?? Array.Empty<TItem>()).ToArray();
			_totalCount = Math.Max(0, result.TotalCount);
			_currentPage = canPage ? Math.Clamp(result.Page, 0, TotalPages - 1) : 0;
			ReconcileSelection();
		} catch (Exception error) {
			if (IsCurrentQuery(version))
				_errorMessage = error.Message;
		}
	}

	private void BeginEditCore(Func<TItem> getItem, bool isNew) {
		ClearProblems();
		try {
			Guard.Ensure(!typeof(TItem).IsValueType, "Editable rows must be reference types.");
			var item = getItem();
			Guard.Ensure(item is not null, "The data source returned an empty new item.");
			var values = _columns.Where(column => column.CanEdit(item)).ToDictionary(column => column, column => column.PropertyValue(item));
			_edit = new EditSession(item, isNew, values);
		} catch (Exception error) {
			_errorMessage = error.Message;
		}
		OnChanged();
	}

	private bool AcceptValidation(Result result) {
		Guard.Ensure(result != null, "The data source returned no validation result.");
		if (result.IsSuccess)
			return true;
		var errors = result.ErrorMessages.ToArray();
		_validationErrors = errors.Length == 0 ? new[] { "The data source rejected this change." } : errors;
		return false;
	}

	private static bool ValuesDiffer(object original, object value) {
		if (ReferenceEquals(original, value))
			return false;
		if (original == null || value == null)
			return true;
		return original is string || original.GetType().IsValueType ? !Equals(original, value) : true;
	}

	private void RestoreValues(EditSession edit, List<BlazorGridColumn<TItem>> applied) {
		foreach (var column in applied.AsEnumerable().Reverse()) {
			try {
				column.SetPropertyValue(edit.Item, edit.OriginalValues[column]);
			} catch (Exception error) {
				if (!_disposed && ReferenceEquals(_edit, edit))
					_errorMessage = $"Could not restore {column.ColumnName}: {error.Message}";
			}
		}
	}

	private long BeginWrite() {
		_isSaving = true;
		ClearProblems();
		var write = ++_writeVersion;
		OnChanged();
		return write;
	}

	private void EndWrite(long write) {
		if (!_disposed && write == _writeVersion) {
			_isSaving = false;
			OnChanged();
		}
	}

	private void InvalidateEdit() {
		_configurationVersion++;
		_writeVersion++;
		_edit = null;
		_isSaving = false;
	}

	private bool IsCurrentConfiguration(long version) => !_disposed && version == _configurationVersion;

	private bool IsCurrentEdit(EditSession edit, long version) => IsCurrentConfiguration(version) && ReferenceEquals(_edit, edit);

	private bool IsCurrentQuery(long version) => !_disposed && version == _queryVersion;

	private bool HasCapability(DataSourceCapabilities capability) => (_capabilities & capability) == capability;

	private bool RequireCapability(DataSourceCapabilities capability) {
		if (_source != null && HasCapability(capability))
			return true;
		_errorMessage = "The data source or current permissions do not allow this operation.";
		OnChanged();
		return false;
	}

	private int FindVisibleIndex(TItem item) {
		for (var index = 0; index < _items.Length; index++)
			if (_entityComparer.Equals(_items[index], item))
				return index;
		return -1;
	}

	private void ReconcileSelection() {
		if (!_hasSelection)
			return;
		var index = FindVisibleIndex(_selectedItem);
		if (index >= 0)
			_selectedItem = _items[index];
		else
			ClearSelectionCore();
	}

	private void ClearSelectionCore() {
		_hasSelection = false;
		_selectedItem = default;
	}

	private void ClearVisibleItems() {
		_items = Array.Empty<TItem>();
		ClearSelectionCore();
	}

	private void ClearProblems() {
		_errorMessage = null;
		_validationErrors = Array.Empty<string>();
	}

	private void EnsureNoDraft() {
		EnsureUsable();
		Guard.Ensure(_edit == null && !_isSaving, "Finish or cancel the current edit before changing the grid.");
	}

	private void EnsureUsable() => Guard.Ensure(!_disposed, "The grid controller has been disposed.");

	private sealed class EditSession {
		public EditSession(TItem item, bool isNew, Dictionary<BlazorGridColumn<TItem>, object> values) {
			Item = item;
			IsNew = isNew;
			OriginalValues = values;
			Values = new Dictionary<BlazorGridColumn<TItem>, object>(values);
		}

		public TItem Item { get; }

		public bool IsNew { get; }

		public Dictionary<BlazorGridColumn<TItem>, object> OriginalValues { get; }

		public Dictionary<BlazorGridColumn<TItem>, object> Values { get; }
	}
}