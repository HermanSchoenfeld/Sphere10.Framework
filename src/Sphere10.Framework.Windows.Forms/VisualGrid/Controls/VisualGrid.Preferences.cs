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
using System.Globalization;
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

	private const double _maximumPreferenceColumnWidth = 100000;
	private VisualGridSettings _preferences;
	private VisualGridSettings _pendingQueryPreferences;
	private bool _columnResizePending;
	private VisualGridSettings _gridSettings;
	private readonly VisualGridPreferenceStorage _preferenceStorage = new();
	private readonly Dictionary<IVisualGridColumn, bool> _columnStretchOverrides = new();
	private string _settingsKey = string.Empty;
	private bool _persistSettings = true;
	private bool _restoringGridSettings;
	private bool _savingGridSettings;
	private bool _gridSettingsDirty;
	private (VisualGridPageMode PageMode, int PageSize, int PageNumber, string SortColumnName, SortDirection SortDirection, string Columns) _capturedPreferenceState;
	private ISettingsProvider _settingsProvider;
	private string _resolvedSettingsKey;
	private Task _settingsLoadTask;
	private ILogger _settingsLogger;

	/// <summary>Optional screen-owned values. Assigning a non-null value disables the separate standalone settings record.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public VisualGridSettings Preferences {
		get {
			return _preferences == null ? null : CaptureGridPreferences();
		}
		set {
			if (value != null)
				ValidatePreferenceBindings(_columnsBindings);
			_preferences = value == null ? null : VisualGridPreferenceStorage.Copy(value);
			UpdateSettingsButton();
			if (_preferences != null)
				ApplyGridPreferencesCore(_preferences);
		}
	}

	[Category("Behavior")]
	[DefaultValue(true)]
	[Description("Remembers paging and sorting through UserSettings. A stable owner/control name or an explicit SettingsKey identifies the grid.")]
	public bool PersistSettings {
		get {
			return _persistSettings;
		}

		set {
			_persistSettings = value;
		}
	}

	[Category("Behavior")]
	[DefaultValue("")]
	[Description("Optional stable UserSettings identifier. An empty value uses the owning screen/control type and the named child path, or the owning form.")]
	public string SettingsKey {
		get {
			return _settingsKey;
		}

		set {
			value ??= string.Empty;
			if (_settingsKey == value)
				return;
			SaveGridSettings();
			_settingsKey = value;
			_gridSettings = null;
			_settingsLoadTask = null;
			_settingsProvider = null;
			_resolvedSettingsKey = null;
			// A late completion for the previous identity must not mark this new identity dirty or failed.
			_preferenceStorage.ResetIdentity();
			_gridSettingsDirty = false;
		}
	}

	/// <summary>The last standalone save failure; the captured values remain retryable until a successful flush.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Exception PreferenceSaveError {
		get {
			return _preferenceStorage.SaveError;
		}
	}

	/// <summary>Applies values without saving, changing persistence ownership or replacing an active cell's draft.</summary>
	public void ApplyGridPreferences(VisualGridSettings preferences) {
		Guard.ArgumentNotNull(preferences, nameof(preferences));
		if (_preferences != null)
			_preferences = VisualGridPreferenceStorage.Copy(preferences);
		else if (_gridSettings != null)
			_gridSettings.Columns = VisualGridPreferenceStorage.Copy(preferences).Columns;
		ApplyGridPreferencesCore(preferences);
	}

	/// <summary>Reads an existing standalone record through the supplied provider without creating or saving one.</summary>
	public async Task<VisualGridSettings> ReadLegacyPreferencesAsync(ISettingsProvider provider, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(provider, nameof(provider));
		Guard.Ensure(!InvokeRequired, "Read legacy grid preferences from the owning UI thread.");
		var key = ResolveSettingsKey();
		if (key == null)
			return null;

		var settings = await VisualGridPreferenceStorage.ReadAsync(provider, key, cancellationToken);
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
		await _preferenceStorage.FlushAsync();
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

	// Captures provider and identity before worker IO; a standalone grid may retry after Framework startup.
	private Task RestoreGridSettingsAsync() {
		if (_preferences != null || !_persistSettings || _gridSettings != null || _dataSource == null || DesignMode || Tools.Runtime.IsDesignMode || IsDisposed || Disposing)
			return Task.CompletedTask;
		if (_settingsLoadTask != null)
			return _settingsLoadTask;
		var key = ResolveSettingsKey();
		if (key == null)
			return Task.CompletedTask;
		Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => {
			_settingsProvider = UserSettings.Provider;
		}, out var capturedError);
		if (capturedError != null)
			return Task.CompletedTask;
		_resolvedSettingsKey = key;
		_settingsLogger = ResolveSettingsLogger();
		return _settingsLoadTask = RestoreGridSettingsCoreAsync(_settingsProvider, key, _preferenceStorage.Version);
	}

	// Loading never creates a provider-bound default record or writes merely because a control opens.
	private async Task RestoreGridSettingsCoreAsync(ISettingsProvider provider, string key, long version) {
		VisualGridSettings settings = null;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			settings = await VisualGridPreferenceStorage.ReadAsync(provider, key);
		});
		if (IsDisposed || Disposing || _preferences != null || version != _preferenceStorage.Version
			|| !ReferenceEquals(provider, _settingsProvider) || key != _resolvedSettingsKey)
			return;
		if (error != null)
			_settingsLogger.Exception(error, "Could not restore CRUD grid preferences.");

		_restoringGridSettings = true;
		using var restoring = Tools.Scope.ExecuteOnDispose(() => _restoringGridSettings = false);
		_gridSettings = settings ?? new VisualGridSettings();
		if (settings == null)
			CaptureGridSettings(_gridSettings);
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			// Consumer upgrades receive a detached root on the owner thread, never the provider's cached object.
			UpgradeGridSettings(_gridSettings);
			_gridSettings.PageMode ??= _gridSettings.AutoPageSize ? VisualGridPageMode.Auto : VisualGridPageMode.Paged;
			ApplyGridPreferencesCore(_gridSettings);
			_capturedPreferenceState = CapturePreferenceState();
		}, out var applyError);
		if (applyError != null)
			_settingsLogger.Exception(applyError, "Could not apply CRUD grid preferences.");
	}

	/// <summary>Upgrades a detached preference payload before restoration. Changes persist with the next accepted preference change.</summary>
	/// <remarks>Called for stored preferences and new control defaults; never receives the provider's cached settings object.</remarks>
	protected virtual void UpgradeGridSettings(VisualGridSettings settings) {
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
		SaveGridSettings(true);
	}

	// Completed actions capture detached values synchronously; the captured provider serializes them off the UI thread.
	private void SaveGridSettings(bool acceptedChange = false) {
		if (acceptedChange && !_restoringGridSettings && !_savingGridSettings)
			NotifyStateChanged();
		if (_preferences != null) {
			if (acceptedChange)
				PublishGridPreferences();
			return;
		}
		if (!_persistSettings || _gridSettings == null || _settingsProvider == null || _restoringGridSettings || _savingGridSettings || DesignMode || Tools.Runtime.IsDesignMode)
			return;
		var preferences = CapturePreferenceState();
		if (acceptedChange && preferences != _capturedPreferenceState)
			_gridSettingsDirty = true;
		if (!_gridSettingsDirty && !_preferenceStorage.SaveFailed)
			return;

		_savingGridSettings = true;
		using var saving = Tools.Scope.ExecuteOnDispose(() => _savingGridSettings = false);
		CaptureGridSettings(_gridSettings);
		_preferenceStorage.Save(_settingsProvider, _resolvedSettingsKey, _settingsLogger, _gridSettings);
		_capturedPreferenceState = preferences;
		_gridSettingsDirty = false;
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

	// Restores widths immediately; query preferences wait until an active cell editor has finished.
	private void ApplyGridPreferencesCore(VisualGridSettings preferences) {
		Guard.Ensure(!InvokeRequired, "Apply grid preferences on the owning UI thread.");
		var previousRestoring = _restoringGridSettings;
		_restoringGridSettings = true;
		using var restoring = Tools.Scope.ExecuteOnDispose(() => _restoringGridSettings = previousRestoring);
		RestoreColumnPreferences(preferences.Columns);
		if (IsPreferenceCellEditing()) {
			_pendingQueryPreferences = VisualGridPreferenceStorage.Copy(preferences);
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

	// Publishes values only after a completed user action; application owners queue their own detached save.
	private void PublishGridPreferences() {
		if (_restoringGridSettings || DesignMode || Tools.Runtime.IsDesignMode)
			return;
		var values = CaptureGridPreferences();
		if (_preferences != null)
			_preferences = VisualGridPreferenceStorage.Copy(values);
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

	// Includes only deliberate widths; viewport stretching and natural measurement never mark preferences dirty.
	private (VisualGridPageMode PageMode, int PageSize, int PageNumber, string SortColumnName, SortDirection SortDirection, string Columns) CapturePreferenceState() {
		var columns = string.Join(";", CaptureGridPreferences().Columns.Where(column => column.Width.HasValue || column.ExpandsToFit.HasValue).OrderBy(column => column.Key, StringComparer.Ordinal)
			.Select(column => column.Key.Length + ":" + column.Key + "=" + column.Width?.ToString("R", CultureInfo.InvariantCulture) + "," + column.ExpandsToFit));
		return (_pageMode, _manualPageSize, _pageNumber, _sortColumnName ?? string.Empty, _sortDirection, columns);
	}

	// The legacy root and the screen-owned root use the same detached capture, with one persistence authority at a time.
	private void CaptureGridSettings(VisualGridSettings settings) {
		var values = CaptureGridPreferences();
		settings.ConsumerVersion = values.ConsumerVersion;
		settings.PageMode = (VisualGridPageMode?)values.PageMode;
		settings.AutoPageSize = values.AutoPageSize;
		settings.PageSize = values.PageSize;
		settings.PageNumber = values.PageNumber;
		settings.SortColumnName = values.SortColumnName;
		settings.SortDirection = values.SortDirection;
		settings.Columns = values.Columns;
	}

	/// <summary>Captures current query and deliberate widths as detached values on the owning UI thread.</summary>
	public VisualGridSettings CaptureGridPreferences() {
		Guard.Ensure(!InvokeRequired, "Capture grid preferences on the owning UI thread.");
		var result = _pendingQueryPreferences == null ? new VisualGridSettings {
			ConsumerVersion = _preferences?.ConsumerVersion ?? _gridSettings?.ConsumerVersion ?? 0,
			PageMode = _pageMode,
			AutoPageSize = AutoPageSize,
			PageSize = _manualPageSize,
			PageNumber = _pageNumber,
			SortColumnName = _sortColumnName ?? string.Empty,
			SortDirection = _sortDirection
		} : VisualGridPreferenceStorage.Copy(_pendingQueryPreferences);

		// Keep optional schema columns while updating the metadata of currently bound columns.
		var columns = new Dictionary<string, ApplicationGridColumnPreferences>(StringComparer.Ordinal);
		foreach (var column in (_preferences?.Columns ?? _gridSettings?.Columns) ?? Array.Empty<ApplicationGridColumnPreferences>()) {
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
