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
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms;

public partial class VisualGrid {
	/// <summary>Reports a detached value snapshot after a deliberate grid preference change; its owner controls persistence.</summary>
	[Category("Behavior")]
	public event EventHandlerEx<VisualGridSettings> PreferencesChanged;

	private const double _maximumPreferenceColumnWidth = 100000;
	private VisualGridSettings _preferences;
	private VisualGridSettings _pendingQueryPreferences;
	private bool _columnResizePending;
	private readonly Dictionary<IVisualGridColumn, bool> _columnStretchOverrides = new();
	private bool _restoringGridSettings;
	private VisualGrid _columnLayoutSource;
	private bool _applyingSharedColumns;
	private bool _publishingColumnLayout;
	internal event EventHandler ColumnLayoutChanged;

	/// <summary>Optionally borrows another grid's column schema and resolved widths. Query, paging and selection remain local; the source owns column preferences.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public VisualGrid ColumnLayoutSource {
		get => _columnLayoutSource;
		set {
			Guard.Ensure(!InvokeRequired && (value == null || !value.InvokeRequired), "Share column layouts on the controls' owning UI thread.");
			for (var ancestor = value; ancestor != null; ancestor = ancestor._columnLayoutSource)
				Guard.Argument(!ReferenceEquals(ancestor, this), nameof(value), "Column layout sources cannot form a cycle.");
			Guard.Argument(value == null || !value.IsDisposed, nameof(value), "A column layout source must be alive.");
			if (ReferenceEquals(value, _columnLayoutSource))
				return;
			ReleaseColumnLayoutSource();
			_columnLayoutSource = value;
			_grid.HorizontalScrollingEnabled = value == null;
			if (value == null)
				return;
			value.ColumnLayoutChanged += SharedColumnLayoutChanged;
			value.Disposed += SharedColumnSourceDisposed;
			SharedColumnLayoutChanged(value, EventArgs.Empty);
		}
	}

	/// <summary>Applies screen-owned values without saving or replacing an active cell's draft.</summary>
	public void ApplyGridPreferences(VisualGridSettings preferences) {
		Guard.ArgumentNotNull(preferences, nameof(preferences));
		ValidatePreferenceBindings(_columnsBindings);
		_preferences = preferences.Copy();
		if (_columnLayoutSource != null)
			_preferences.Columns = _columnLayoutSource.CaptureGridPreferences().Columns;
		ApplyGridPreferencesCore(_preferences);
		ApplySharedColumnGeometry();
	}

	/// <summary>Captures current query and deliberate widths as detached values on the owning UI thread.</summary>
	public VisualGridSettings CaptureGridPreferences() {
		Guard.Ensure(!InvokeRequired, "Capture grid preferences on the owning UI thread.");
		var result = _pendingQueryPreferences == null ? new VisualGridSettings {
			ConsumerVersion = _preferences?.ConsumerVersion ?? 0,
			PageMode = _pageMode,
			AutoPageSize = AutoPageSize,
			PageSize = _manualPageSize,
			PageNumber = _pageNumber,
			SortColumnName = _sortColumnName ?? string.Empty,
			SortDirection = _sortDirection
		} : _pendingQueryPreferences.Copy();

		// Keep optional schema columns while updating the metadata of currently bound columns.
		var columns = new Dictionary<string, ApplicationGridColumnPreferences>(StringComparer.Ordinal);
		foreach (var column in (_preferences?.Columns) ?? Array.Empty<ApplicationGridColumnPreferences>()) {
			if (column != null && !string.IsNullOrWhiteSpace(column.Key))
				columns[column.Key] = new ApplicationGridColumnPreferences {
					Key = column.Key,
					Title = column.Title,
					ExpandsToFit = column.ExpandsToFit,
					Width = column.Width.HasValue && double.IsFinite(column.Width.Value) && column.Width.Value > 0
						? Math.Min(column.Width.Value, _maximumPreferenceColumnWidth) : null
				};
		}
		foreach (var binding in _columnsBindings.Where(column => !string.IsNullOrWhiteSpace(column.PreferenceKey))
			.GroupBy(column => column.PreferenceKey, StringComparer.Ordinal).Where(group => group.Count() == 1).Select(group => group.Single())) {
			var key = binding.PreferenceKey;
			if (string.IsNullOrWhiteSpace(key))
				continue;
			columns[key] = new ApplicationGridColumnPreferences {
				Key = key,
				Title = binding.ColumnName,
				ExpandsToFit = _columnStretchOverrides.TryGetValue(binding, out var expands) ? expands : binding.ExpandsToFit,
				Width = _columnWidthOverrides.TryGetValue(binding, out var width) && width > 0
					? Math.Min(width * 96D / DeviceDpi, _maximumPreferenceColumnWidth)
					: columns.TryGetValue(key, out var previous) ? previous.Width : null
			};
		}
		result.Columns = columns.Values.ToArray();
		return result;
	}

	private void ReleaseColumnLayoutSource() {
		if (_columnLayoutSource == null)
			return;
		_columnLayoutSource.ColumnLayoutChanged -= SharedColumnLayoutChanged;
		_columnLayoutSource.Disposed -= SharedColumnSourceDisposed;
		_columnLayoutSource = null;
		if (!_grid.IsDisposed)
			_grid.HorizontalScrollingEnabled = true;
	}

	private void SharedColumnSourceDisposed(object sender, EventArgs args) => ReleaseColumnLayoutSource();

	private async void SharedColumnLayoutChanged(object sender, EventArgs args) {
		if (IsDisposed || Disposing || _columnLayoutSource == null || _applyingSharedColumns)
			return;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			var source = _columnLayoutSource;
			var schemaChanged = !_columnsBindings.SequenceEqual(source._columnsBindings);
			if (_preferences != null)
				_preferences.Columns = source.CaptureGridPreferences().Columns;
			if (schemaChanged)
				GridBindings = source._columnsBindings;
			ApplySharedColumnGeometry();
			if (schemaChanged && _dataSource != null)
				await RefreshGrid();
		});
		if (error != null && !IsDisposed && !Disposing)
			(ResolveSettingsLogger() ?? SystemLog.Logger).Exception(error, "Could not update shared grid columns.");
	}

	/// <summary>Applies externally owned presentation values; the grid never loads or saves a settings record.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public VisualGridSettings Preferences {
		get => _preferences == null ? null : CaptureGridPreferences();
		set {
			if (value == null) {
				_preferences = null;
				return;
			}
			ApplyGridPreferences(value);
		}
	}

	private bool ApplySharedColumnGeometry() {
		var source = _columnLayoutSource;
		if (source == null || source.IsDisposed || _applyingSharedColumns || _grid.ColumnsCount != _columnsBindings.Length
			|| source._grid.ColumnsCount != _columnsBindings.Length || !_columnsBindings.SequenceEqual(source._columnsBindings))
			return false;
		_applyingSharedColumns = true;
		var previousSizing = _sizingColumns;
		_sizingColumns = true;
		using var restore = Tools.Scope.ExecuteOnDispose(() => { _sizingColumns = previousSizing; _applyingSharedColumns = false; });
		var scale = DeviceDpi / (double)source.DeviceDpi;
		_columnWidthOverrides.Clear();
		_columnStretchOverrides.Clear();
		foreach (var column in source._columnWidthOverrides)
			_columnWidthOverrides[column.Key] = Math.Max(1, (int)Math.Round(column.Value * scale));
		foreach (var column in source._columnStretchOverrides)
			_columnStretchOverrides[column.Key] = column.Value;
		_columnMinimumWidths = new int[_columnsBindings.Length];
		_grid.Columns.SuspendLayout();
		using (Tools.Scope.ExecuteOnDispose(() => _grid.Columns.ResumeLayout())) {
			for (var index = 0; index < _columnsBindings.Length; index++) {
				var width = Math.Max(1, (int)Math.Round(source._grid.Columns[index].Width * scale));
				_columnMinimumWidths[index] = width;
				_grid.Columns[index].AutoSizeMode = SourceGrid.AutoSizeMode.None;
				if (_grid.Columns[index].Width != width)
					_grid.Columns[index].Width = width;
			}
		}
		_sizedColumnBindings = _columnsBindings;
		_columnsSizedFromData = source._columnsSizedFromData;
		_columnViewportWidth = _grid.DisplayRectangle.Width;
		NotifyColumnLayoutChanged();
		return true;
	}

	private void NotifyColumnLayoutChanged() {
		if (_publishingColumnLayout || IsDisposed || Disposing)
			return;
		if (_dataSource != null && !ReferenceEquals(_boundColumns, _columnsBindings))
			return;
		_publishingColumnLayout = true;
		using var restore = Tools.Scope.ExecuteOnDispose(() => _publishingColumnLayout = false);
		ColumnLayoutChanged?.Invoke(this, EventArgs.Empty);
	}

	// Stored widths are logical units; a monitor-DPI change reuses the same values without creating a save.
	protected override void OnDpiChangedAfterParent(EventArgs args) {
		base.OnDpiChangedAfterParent(args);
		if (_grid == null || _columnsBindings == null || _preferences == null)
			return;
		RestoreColumnPreferences(_preferences?.Columns);
		if (_grid.ColumnsCount == _columnsBindings.Length && !IsPreferenceCellEditing())
			SizeBoundColumns();
	}

	// A screen-owned column schema cannot assign two meanings to one persistent preference identity.
	private static void ValidatePreferenceBindings(IVisualGridColumn[] bindings) {
		Guard.Argument(bindings.All(column => !string.IsNullOrWhiteSpace(column.PreferenceKey)) && bindings
			.GroupBy(column => column.PreferenceKey, StringComparer.Ordinal).All(group => group.Count() == 1), nameof(GridBindings),
			"Column preference keys must be nonempty and unique. Supply an explicit PreferenceKey for computed or repeated projections.");
	}

	// A resize is accepted once, after the header releases its pointer; layout/pixel events never save.
	private void GridPreferenceResizeCompleted(object sender, MouseEventArgs args) {
		if (args.Button != MouseButtons.Left || !_columnResizePending)
			return;
		_columnResizePending = false;
		var previousSizing = _sizingColumns;
		_sizingColumns = true;
		using var sizing = Tools.Scope.ExecuteOnDispose(() => _sizingColumns = previousSizing);
		foreach (var binding in _columnWidthOverrides.Keys.ToArray()) {
			var index = Array.IndexOf(_columnsBindings, binding);
			if (index < 0 || index >= _grid.ColumnsCount || string.IsNullOrWhiteSpace(binding.PreferenceKey))
				continue;
			var width = Math.Clamp(_columnWidthOverrides[binding], 1, Math.Max(1, (int)Math.Round(_maximumPreferenceColumnWidth * DeviceDpi / 96D)));
			_grid.Columns[index].Width = width;
			_columnWidthOverrides[binding] = _grid.Columns[index].Width;
			if (index < _columnMinimumWidths.Length)
				_columnMinimumWidths[index] = _grid.Columns[index].Width;
		}
		NotifyPreferencesChanged();
		NotifyColumnLayoutChanged();
	}

	// Deliberate grid operations notify their owner; persistence is outside the control.
	private void NotifyPreferencesChanged() {
		if (_restoringGridSettings || DesignMode || Tools.Runtime.IsDesignMode)
			return;
		NotifyStateChanged();
		PublishGridPreferences();
	}

	// Accepted query preferences wait for SourceGrid to finish its independent row-edit lifecycle.
	private async void GridPreferenceEditEnded(object sender, EventArgs args) {
		QueueDataSourceNotification();
		if (_pendingQueryPreferences == null || IsDisposed || Disposing)
			return;
		await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			ApplyGridPreferencesCore(_pendingQueryPreferences);
			await RefreshGrid();
		}, error => {
			(ResolveSettingsLogger() ?? SystemLog.Logger).Exception(error, "Could not apply CRUD grid preferences after editing.");
			return true;
		});
	}

	// Restores widths immediately; query preferences wait until an active cell editor has finished.
	private void ApplyGridPreferencesCore(VisualGridSettings preferences) {
		Guard.Ensure(!InvokeRequired, "Apply grid preferences on the owning UI thread.");
		var previousRestoring = _restoringGridSettings;
		_restoringGridSettings = true;
		using var restoring = Tools.Scope.ExecuteOnDispose(() => _restoringGridSettings = previousRestoring);
		RestoreColumnPreferences(preferences.Columns);
		if (IsPreferenceCellEditing()) {
			_pendingQueryPreferences = preferences.Copy();
			return;
		}
		_pendingQueryPreferences = null;
		var orderedBindings = OrderColumnBindings(_columnsBindings, preferences.Columns);
		if (!ReferenceEquals(orderedBindings, _columnsBindings))
			GridBindings = orderedBindings;

		using (EnterVisualState(VisualState.Loading)) {
			_manualPageSize = Math.Clamp(preferences.PageSize, 1, 9999);
			_pageMode = preferences.PageMode.HasValue && Enum.IsDefined(preferences.PageMode.Value)
				? preferences.PageMode.Value : preferences.AutoPageSize ? VisualGridPageMode.Auto : _pageMode;
			_pageNumber = _pageMode == VisualGridPageMode.Single || preferences.PageNumber < 0 || preferences.PageNumber >= int.MaxValue / _manualPageSize
				? 0 : preferences.PageNumber;
			SetVisiblePageNumberText(_pageNumber);
			var sortColumn = Array.FindIndex(_columnsBindings, column => !string.IsNullOrEmpty(column.SortName)
				&& string.Equals(column.SortName, preferences.SortColumnName, StringComparison.Ordinal));
			_sortColumnIndex = Math.Max(0, sortColumn);
			_sortColumnName = sortColumn >= 0 && preferences.SortDirection != SortDirection.None ? _columnsBindings[sortColumn].SortName : null;
			_sortDirection = preferences.SortDirection == SortDirection.None ? SortDirection.None
				: _sortColumnName != null && Enum.IsDefined(preferences.SortDirection) ? preferences.SortDirection : SortDirection.Ascending;
			UpdatePageModeControl();
			OrganizeLayout();
		}
		if (_grid.ColumnsCount == _columnsBindings.Length && _columnMinimumWidths.Length == _grid.ColumnsCount)
			StretchColumnsToViewport();
	}

	// Maps logical widths to the current native bindings; unknown or invalid stored columns cannot affect layout.
	private void RestoreColumnPreferences(ApplicationGridColumnPreferences[] preferences) {
		var previousRestoring = _restoringGridSettings;
		_restoringGridSettings = true;
		using var restoring = Tools.Scope.ExecuteOnDispose(() => _restoringGridSettings = previousRestoring);
		var uniqueKeys = _columnsBindings.Where(column => !string.IsNullOrWhiteSpace(column.PreferenceKey))
			.GroupBy(column => column.PreferenceKey, StringComparer.Ordinal).Where(group => group.Count() == 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
		var widths = (preferences ?? Array.Empty<ApplicationGridColumnPreferences>())
			.Where(column => column != null && !string.IsNullOrWhiteSpace(column.Key) && column.Width.HasValue && double.IsFinite(column.Width.Value) && column.Width.Value > 0)
			.GroupBy(column => column.Key, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Last().Width.Value, StringComparer.Ordinal);
		var stretching = (preferences ?? Array.Empty<ApplicationGridColumnPreferences>())
			.Where(column => column != null && !string.IsNullOrWhiteSpace(column.Key) && column.ExpandsToFit.HasValue)
			.GroupBy(column => column.Key, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Last().ExpandsToFit.Value, StringComparer.Ordinal);
		var previous = _columnWidthOverrides.Keys.ToHashSet();
		_columnWidthOverrides.Clear();
		_columnStretchOverrides.Clear();
		for (var index = 0; index < _columnsBindings.Length; index++) {
			var binding = _columnsBindings[index];
			if (!string.IsNullOrWhiteSpace(binding.PreferenceKey) && uniqueKeys.Contains(binding.PreferenceKey) && stretching.TryGetValue(binding.PreferenceKey, out var expands))
				_columnStretchOverrides[binding] = expands;
			if (index < _grid.ColumnsCount)
				_grid.Columns[index].AutoSizeMode = GetColumnAutoSizeMode(binding);
			if (string.IsNullOrWhiteSpace(binding.PreferenceKey) || !uniqueKeys.Contains(binding.PreferenceKey) || !widths.TryGetValue(binding.PreferenceKey, out var logical)) {
				if (previous.Contains(binding)) {
					// Reset releases only manual overrides; the normal sizing pass restores natural/stretch geometry.
					_columnsSizedFromData = false;
					_sizedColumnBindings = null;
				}
				continue;
			}
			var width = Math.Max(1, (int)Math.Round(Math.Min(logical, _maximumPreferenceColumnWidth) * DeviceDpi / 96D));
			_columnWidthOverrides[binding] = width;
			if (index < _grid.ColumnsCount) {
				_grid.Columns[index].AutoSizeMode = SourceGrid.AutoSizeMode.None;
				_grid.Columns[index].Width = width;
				_columnWidthOverrides[binding] = _grid.Columns[index].Width;
				if (index < _columnMinimumWidths.Length)
					_columnMinimumWidths[index] = _grid.Columns[index].Width;
			}
		}
	}

	// Manual widths override stretching; absent preferences retain the column schema's choice.
	private SourceGrid.AutoSizeMode GetColumnAutoSizeMode(IVisualGridColumn binding) {
		if (_columnWidthOverrides.ContainsKey(binding))
			return SourceGrid.AutoSizeMode.None;
		return (_columnStretchOverrides.TryGetValue(binding, out var expands) ? expands : binding.ExpandsToFit)
			? SourceGrid.AutoSizeMode.EnableAutoSize | SourceGrid.AutoSizeMode.EnableStretch : SourceGrid.AutoSizeMode.EnableAutoSize;
	}

	// Query changes never end or replace an active native cell editor.
	private bool IsPreferenceCellEditing() {
		var position = _grid.Selection.ActivePosition;
		if (position.IsEmpty())
			return false;
		var context = new CellContext(_grid, position);
		return context.Cell != null && context.IsEditing();
	}

	// Publishes values after a completed user action; the screen owner chooses when to save.
	private void PublishGridPreferences() {
		if (_restoringGridSettings || DesignMode || Tools.Runtime.IsDesignMode)
			return;
		var values = CaptureGridPreferences();
		if (_preferences != null)
			_preferences = values.Copy();
		RaisePreferencesChangedEvent(values);
	}

	/// <summary>Raises the template hook and public notification through the control's existing event gate.</summary>
	protected void RaisePreferencesChangedEvent(VisualGridSettings preferences) {
		if (!CanRaiseEvents)
			return;
		OnPreferencesChanged(preferences);
		PreferencesChanged?.Invoke(preferences);
	}

	/// <summary>Lets derived grids observe an accepted detached preference value before public subscribers.</summary>
	protected virtual void OnPreferencesChanged(VisualGridSettings preferences) {
	}

	// The settings array determines presentation order; newly declared columns follow in binding order.
	private static IVisualGridColumn[] OrderColumnBindings(IVisualGridColumn[] bindings, ApplicationGridColumnPreferences[] columns) {
		if (bindings.Length < 2 || columns == null || columns.Length == 0)
			return bindings;
		var order = new Dictionary<string, int>(StringComparer.Ordinal);
		for (var index = 0; index < columns.Length; index++)
			if (columns[index] is { } column && !string.IsNullOrWhiteSpace(column.Key))
				order.TryAdd(column.Key, index);
		var result = bindings.OrderBy(column => order.TryGetValue(column.PreferenceKey ?? string.Empty, out var index) ? index : int.MaxValue).ToArray();
		return result.SequenceEqual(bindings) ? bindings : result;
	}

	private ILogger ResolveSettingsLogger() {
		for (var control = Parent; control != null; control = control.Parent)
			if (control is ApplicationScreen screen && screen.Block?.Parent?.Logger is { } logger)
				return logger;
		return SystemLog.Logger;
	}
}
