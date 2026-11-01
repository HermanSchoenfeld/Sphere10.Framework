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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms;

public partial class VisualGrid {
	/// <summary>Reports a detached value snapshot after a deliberate grid preference change; its owner controls persistence.</summary>
	[Category("Behavior")]
	public event EventHandlerEx<VisualGridSettings> PreferencesChanged;

	private const double MaximumPreferenceColumnWidth = 100000;

	private VisualGridSettings _preferences;
	private VisualGridSettings _pendingQueryPreferences;
	private bool _columnResizePending;

	/// <summary>Optional screen-owned values. Assigning a non-null value disables the separate standalone settings record.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public VisualGridSettings Preferences {
		get => _preferences == null ? null : CaptureGridPreferences();
		set {
			if (value != null)
				ValidatePreferenceBindings(_columnsBindings);
			_preferences = value == null ? null : CopyGridSettings(value);
			UpdateSettingsButton();
			if (_preferences != null)
				ApplyGridPreferencesCore(_preferences);
		}
	}

	/// <summary>Captures current query and deliberate widths as detached values on the owning UI thread.</summary>
	public VisualGridSettings CaptureGridPreferences() {
		Guard.Ensure(!InvokeRequired, "Capture grid preferences on the owning UI thread.");
		var result = _pendingQueryPreferences == null ? new VisualGridSettings {
			ConsumerVersion = _preferences?.ConsumerVersion ?? _settingsConsumerVersion,
			PageMode = _pageMode,
			AutoPageSize = AutoPageSize,
			PageSize = _manualPageSize,
			PageNumber = _pageNumber,
			SortColumnName = _sortColumnName ?? string.Empty,
			SortDirection = _sortDirection
		} : CopyGridSettings(_pendingQueryPreferences);

		// Keep optional schema columns while updating the metadata of currently bound columns.
		var columns = new Dictionary<string, ApplicationGridColumnPreferences>(StringComparer.Ordinal);
		foreach (var column in (_preferences?.Columns ?? _gridSettings?.Columns) ?? Array.Empty<ApplicationGridColumnPreferences>()) {
			if (column != null && !string.IsNullOrWhiteSpace(column.Key))
				columns[column.Key] = new ApplicationGridColumnPreferences {
					Key = column.Key,
					Title = column.Title,
					Width = column.Width.HasValue && double.IsFinite(column.Width.Value) && column.Width.Value > 0
						? Math.Min(column.Width.Value, MaximumPreferenceColumnWidth) : null
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
				Width = _columnWidthOverrides.TryGetValue(binding, out var width) && width > 0
					? Math.Min(width * 96D / DeviceDpi, MaximumPreferenceColumnWidth)
					: columns.TryGetValue(key, out var previous) ? previous.Width : null
			};
		}
		result.Columns = columns.Values.ToArray();
		return result;
	}

	/// <summary>Applies values without saving, changing persistence ownership or replacing an active cell's draft.</summary>
	public void ApplyGridPreferences(VisualGridSettings preferences) {
		Guard.ArgumentNotNull(preferences, nameof(preferences));
		if (_preferences != null)
			_preferences = CopyGridSettings(preferences);
		else if (_gridSettings != null)
			_gridSettings.Columns = CopyGridSettings(preferences).Columns;
		ApplyGridPreferencesCore(preferences);
	}

	/// <summary>Reads an existing standalone record through the supplied provider without creating or saving one.</summary>
	public async Task<VisualGridSettings> ReadLegacyPreferencesAsync(ISettingsProvider provider, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(provider, nameof(provider));
		Guard.Ensure(!InvokeRequired, "Read legacy grid preferences from the owning UI thread.");
		var key = ResolveSettingsKey();
		if (key == null)
			return null;

		var settings = await Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			return ReadGridSettings(provider, key);
		}, cancellationToken);
		if (settings == null || IsDisposed || Disposing)
			return null;

		// Consumer migrations inspect only this detached payload, back on their normal UI context.
		UpgradeGridSettings(settings);
		settings.PageMode ??= settings.AutoPageSize ? VisualGridPageMode.Auto : VisualGridPageMode.Paged;
		return settings;
	}

	/// <summary>Awaits accepted standalone snapshots and retries a previous failure before the provider is disposed.</summary>
	public async Task FlushPreferencesAsync() {
		SaveGridSettings();
		await _settingsSaveTask;
		if (_settingsSaveError != null)
			throw new InvalidOperationException("Could not save CRUD grid preferences.", _settingsSaveError);
	}

	/// <summary>The last standalone save failure; the captured values remain retryable until a successful flush.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Exception PreferenceSaveError => _settingsSaveError;

	/// <summary>Lets derived grids observe an accepted detached preference value before public subscribers.</summary>
	protected virtual void OnPreferencesChanged(VisualGridSettings preferences) {
	}

	/// <summary>Raises the template hook and public notification through the control's existing event gate.</summary>
	protected void RaisePreferencesChangedEvent(VisualGridSettings preferences) {
		if (!CanRaiseEvents)
			return;
		OnPreferencesChanged(preferences);
		PreferencesChanged?.Invoke(preferences);
	}

	// Stored widths are logical units; a monitor-DPI change reuses the same values without creating a save.
	protected override void OnDpiChangedAfterParent(EventArgs args) {
		base.OnDpiChangedAfterParent(args);
		if (_grid == null || _columnsBindings == null || _preferences == null && _gridSettings == null)
			return;
		RestoreColumnPreferences(_preferences?.Columns ?? _gridSettings?.Columns);
		if (_grid.ColumnsCount == _columnsBindings.Length && !IsPreferenceCellEditing())
			SizeBoundColumns();
	}

	// Restores widths immediately; query preferences wait until an active cell editor has finished.
	private void ApplyGridPreferencesCore(VisualGridSettings preferences) {
		Guard.Ensure(!InvokeRequired, "Apply grid preferences on the owning UI thread.");
		var previousRestoring = _restoringGridSettings;
		_restoringGridSettings = true;
		using var restoring = Tools.Scope.ExecuteOnDispose(() => _restoringGridSettings = previousRestoring);
		RestoreColumnPreferences(preferences.Columns);
		if (IsPreferenceCellEditing()) {
			_pendingQueryPreferences = CopyGridSettings(preferences);
			return;
		}
		_pendingQueryPreferences = null;

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
		var previous = _columnWidthOverrides.Keys.ToHashSet();
		_columnWidthOverrides.Clear();
		for (var index = 0; index < _columnsBindings.Length; index++) {
			var binding = _columnsBindings[index];
			if (string.IsNullOrWhiteSpace(binding.PreferenceKey) || !uniqueKeys.Contains(binding.PreferenceKey) || !widths.TryGetValue(binding.PreferenceKey, out var logical)) {
				if (previous.Contains(binding)) {
					// Reset releases only manual overrides; the normal sizing pass restores natural/stretch geometry.
					_columnsSizedFromData = false;
					_sizedColumnBindings = null;
					if (index < _grid.ColumnsCount)
						_grid.Columns[index].AutoSizeMode = binding.ExpandsToFit
							? SourceGrid.AutoSizeMode.EnableAutoSize | SourceGrid.AutoSizeMode.EnableStretch : SourceGrid.AutoSizeMode.EnableAutoSize;
				}
				continue;
			}
			var width = Math.Max(1, (int)Math.Round(Math.Min(logical, MaximumPreferenceColumnWidth) * DeviceDpi / 96D));
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
			var width = Math.Clamp(_columnWidthOverrides[binding], 1, Math.Max(1, (int)Math.Round(MaximumPreferenceColumnWidth * DeviceDpi / 96D)));
			_grid.Columns[index].Width = width;
			_columnWidthOverrides[binding] = _grid.Columns[index].Width;
			if (index < _columnMinimumWidths.Length)
				_columnMinimumWidths[index] = _grid.Columns[index].Width;
		}
		SaveGridSettings(true);
	}

	// Accepted query preferences wait for SourceGrid to finish its independent row-edit lifecycle.
	private async void GridPreferenceEditEnded(object sender, EventArgs args) {
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

	// Query changes never end or replace an active native cell editor.
	private bool IsPreferenceCellEditing() {
		var position = _grid.Selection.ActivePosition;
		if (position.IsEmpty())
			return false;
		var context = new CellContext(_grid, position);
		return context.Cell != null && context.IsEditing();
	}

	// Publishes values only after a completed user action; application owners queue their own detached save.
	private void PublishGridPreferences() {
		if (_restoringGridSettings || DesignMode || Tools.Runtime.IsDesignMode)
			return;
		var values = CaptureGridPreferences();
		if (_preferences != null)
			_preferences = CopyGridSettings(values);
		RaisePreferencesChangedEvent(values);
	}

	// Prefer the current record; the old CLR names are used only to import existing provider files.
	private static VisualGridSettings ReadGridSettings(ISettingsProvider provider, string key) {
		if (provider.ContainsSetting(typeof(VisualGridSettings), key))
			return CopyGridSettings((VisualGridSettings)provider.LoadSetting(typeof(VisualGridSettings), key));
		if (!provider.ContainsSetting(typeof(CrudGridSettings), key))
			return null;

		var legacy = (CrudGridSettings)provider.LoadSetting(typeof(CrudGridSettings), key);
		return CopyGridSettings(new VisualGridSettings {
			ConsumerVersion = legacy.ConsumerVersion,
			PageMode = legacy.PageMode.HasValue ? (VisualGridPageMode)legacy.PageMode.Value : null,
			AutoPageSize = legacy.AutoPageSize,
			PageSize = legacy.PageSize,
			PageNumber = legacy.PageNumber,
			SortColumnName = legacy.SortColumnName,
			SortDirection = legacy.SortDirection,
			Columns = legacy.Columns
		});
	}

	// Detaches the provider's mutable cached root before a consumer migration inspects it.
	private static VisualGridSettings CopyGridSettings(VisualGridSettings settings) {
		return new VisualGridSettings {
			ConsumerVersion = settings.ConsumerVersion,
			PageMode = settings.PageMode,
			AutoPageSize = settings.AutoPageSize,
			PageSize = settings.PageSize,
			PageNumber = settings.PageNumber,
			SortColumnName = settings.SortColumnName,
			SortDirection = settings.SortDirection,
			Columns = (settings.Columns ?? Array.Empty<ApplicationGridColumnPreferences>()).Where(column => column != null)
				.Select(column => new ApplicationGridColumnPreferences { Key = column.Key, Title = column.Title, Width = column.Width }).ToArray()
		};
	}
}
