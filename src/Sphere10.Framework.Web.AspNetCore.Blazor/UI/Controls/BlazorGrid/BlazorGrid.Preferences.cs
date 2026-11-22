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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;

public partial class BlazorGrid<TItem> {
	private readonly Dictionary<string, double> _manualWidths = new(StringComparer.Ordinal);
	private readonly Dictionary<string, bool> _hiddenColumns = new(StringComparer.Ordinal);
	private ApplicationGridColumnPreferences[] _preferenceColumns = Array.Empty<ApplicationGridColumnPreferences>();
	private BlazorGridSettings _appliedPreferences;
	private BlazorGridSettings _pendingPreferences;
	private bool _bindPending;
	private BlazorGridSettings _settingsDraft;
	private ApplicationSettingsEditor _settingsEditor;
	private string[] _settingsErrors = Array.Empty<string>();
	private bool _savingPreferences;

	/// <summary>Non-null values select screen-owned preferences and suppress the grid's separate browser-local record.</summary>
	[Parameter] public BlazorGridSettings Preferences { get; set; }

	/// <summary>Publishes a detached value after an accepted page, sort or completed width change.</summary>
	[Parameter] public EventCallback<BlazorGridSettings> PreferencesChanged { get; set; }

	/// <summary>Displays every declared column regardless of its schema or preference visibility. A nested detail grid sets it to mirror the columns its parent displays.</summary>
	[Parameter] public bool ShowHiddenColumns { get; set; }

	// The settings value owns the schema; the flag controls editing without hiding its presence.
	private bool CanEditGridSettings {
		get {
			return Preferences != null && GridType.HasFlag(GridTypes.AllowsSettingsChange)
				&& Attribute.IsDefined(Preferences.GetType(), typeof(UserEditableAttribute), true);
		}
	}

	/// <summary>Captures query choices, deliberate widths and column visibility without retaining controls or row edits.</summary>
	public BlazorGridSettings CaptureGridPreferences() {
		var settings = _pendingPreferences ?? CaptureSettings();
		// Hidden columns stay in the captured schema so their width and visibility can be edited.
		var values = ToPreferences(settings, _schemaColumns);
		var declared = _schemaColumns.Where(column => !string.IsNullOrWhiteSpace(column.PreferenceKey))
			.GroupBy(column => column.PreferenceKey, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
		foreach (var column in values.Columns)
			if (declared.TryGetValue(column.Key, out var definition))
				column.Hidden = _hiddenColumns.TryGetValue(column.Key, out var hidden) ? hidden : definition.Hidden;
		var keys = values.Columns.Select(column => column.Key).ToHashSet(StringComparer.Ordinal);
		values.Columns = values.Columns.Concat(_preferenceColumns
			.Where(column => column != null && !string.IsNullOrWhiteSpace(column.Key) && keys.Add(column.Key))
			.Select(column => new ApplicationGridColumnPreferences { Key = column.Key, Title = column.Title, Hidden = column.Hidden })).ToArray();
		foreach (var column in values.Columns)
			column.Width = _manualWidths.TryGetValue(column.Key, out var width) ? width : null;
		return values;
	}

	/// <summary>Applies accepted preferences, deferring query changes until any active row draft ends.</summary>
	public async Task ApplyGridPreferencesAsync(BlazorGridSettings preferences) {
		Guard.ArgumentNotNull(preferences, nameof(preferences));
		var settings = GetPreferenceSettings(preferences);
		var queryChanged = !QuerySettingsEqual(_appliedPreferences, settings);
		AcceptPreferenceValues(settings, preferences);
		var displayed = _columns;
		ApplyColumnVisibility();
		if (!ReferenceEquals(displayed, _columns))
			StateHasChanged();
		if (_behavior != null)
			await _behavior.InvokeVoidAsync("ApplyWidths", _manualWidths);

		if (Controller.IsEditing) {
			_pendingPreferences = queryChanged ? settings : _pendingPreferences;
			return;
		}
		_pendingPreferences = null;
		if (queryChanged && _boundSource != null && !_bindPending) {
			await Controller.ApplyViewSettingsAsync(settings);
			await NotifySelectionAsync();
		}
	}

	/// <summary>Reads a legacy browser value without writing it or choosing its new settings owner.</summary>
	public static async Task<BlazorGridSettings> ReadLegacyPreferencesAsync(IJSRuntime runtime, string settingsKey, BlazorGridColumn<TItem>[] columns = null) {
		Guard.ArgumentNotNull(runtime, nameof(runtime));
		Guard.ArgumentNotNullOrEmpty(settingsKey, nameof(settingsKey));
		await using var module = await runtime.InvokeAsync<IJSObjectReference>("import", "./_content/Sphere10.Framework.Web.AspNetCore.Blazor/UI/Controls/BlazorGrid/BlazorGrid.razor.js");
		var settings = await module.InvokeAsync<BlazorGridSettings>("ReadLegacySettings", settingsKey);
		return settings == null ? null : ToPreferences(settings, columns);
	}

	/// <summary>Accepts one completed pointer resize; pixel movement and layout never publish preferences.</summary>
	[JSInvokable]
	public async Task SetColumnWidthAsync(string key, double width) {
		if (_disposed || string.IsNullOrWhiteSpace(key) || !_schemaColumns.Any(column => column.PreferenceKey == key) || !double.IsFinite(width))
			return;
		_manualWidths[key] = Math.Clamp(width, 40, 100000);
		await SaveSettingsAsync();
	}

	// Open a detached value editor; row drafts and source data remain owned by the existing controller.
	private void BeginGridSettings() {
		if (!CanEditGridSettings || NavigationDisabled)
			return;
		_settingsDraft = CaptureGridPreferences();
		_settingsErrors = Array.Empty<string>();
		ClosePageModeMenu();
	}

	// Accept through the same owner callback used by completed paging, sorting and manual resizing.
	private async Task ApplyGridSettingsAsync() {
		if (_savingPreferences || _settingsDraft == null || !CanEditGridSettings)
			return;
		_savingPreferences = true;
		try {
			var validation = await _settingsEditor.ValidateAsync();
			validation.Merge(_settingsDraft.Validate());
			_settingsErrors = validation.ErrorMessages.ToArray();
			if (validation.IsFailure)
				return;

			await ApplyGridPreferencesAsync(_settingsDraft);
			await PreferencesChanged.InvokeAsync(CaptureGridPreferences());
			_settingsDraft = null;
		} catch (Exception error) {
			_settingsErrors = new[] { error.Message };
		} finally {
			_savingPreferences = false;
		}
	}

	// Cancelling discards only this local draft; accepted Options and grid values remain unchanged.
	private void CancelGridSettings() {
		if (!_savingPreferences)
			_settingsDraft = null;
	}

	// Imports the standalone record before binding, so its query is the first query issued.
	private async Task ReadInitialSettingsAsync() {
		_settingsKey = SettingsKey;
		var settings = await ReadStandaloneSettingsAsync(_settingsKey)
			?? new BlazorGridSettings { PageMode = _parameterPageMode, PageSize = PageSize };
		AcceptPreferenceValues(GetPreferenceSettings(settings), settings);
		ApplyColumnVisibility();
	}

	// Typed user records take precedence; the original raw browser key is a read-only migration fallback.
	private async Task<BlazorGridSettings> ReadStandaloneSettingsAsync(string key) {
		BlazorGridSettings settings = null;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			if (await UserSettings.HasAsync<BlazorGridSettings>(key))
				settings = await UserSettings.GetAsync<BlazorGridSettings>(key);
			else {
				var legacy = await _module.InvokeAsync<BlazorGridSettings>("ReadLegacySettings", key);
				settings = legacy == null ? null : ToPreferences(legacy, _schemaColumns);
			}
		}, exception => exception is JSException or Newtonsoft.Json.JsonException);
		if (error != null)
			SystemLog.Exception(error);
		return settings;
	}

	// Completes a deferred browser-local initialization after its values and geometry have been seeded.
	private async Task BindRestoredSourceAsync() {
		_bindPending = false;
		await Controller.BindAsync(DataSource, AllowedCapabilities, PageSize, _parameterPageMode, _appliedPreferences);
		_searchText = Controller.SearchTerm;
		await NotifySelectionAsync();
		StateHasChanged();
	}

	// Applies the last accepted query after Save or Cancel without replacing the live editing buffer.
	private async Task ApplyPendingPreferencesAsync() {
		if (_pendingPreferences == null || _disposed || Controller.IsEditing)
			return;
		var settings = _pendingPreferences;
		_pendingPreferences = null;
		await Controller.ApplyViewSettingsAsync(settings);
		await NotifySelectionAsync();
	}

	// Stores plain widths and visibility independently of the rendered columns so Reset can release every override.
	private void AcceptPreferenceValues(BlazorGridSettings settings, BlazorGridSettings values = null) {
		_appliedPreferences = settings;
		_preferenceColumns = (values?.Columns ?? Array.Empty<ApplicationGridColumnPreferences>())
			.Where(column => column != null && !string.IsNullOrWhiteSpace(column.Key))
			.Select(column => new ApplicationGridColumnPreferences { Key = column.Key, Title = column.Title, Hidden = column.Hidden }).ToArray();
		// A normalized query value carries no column schema; only supplied preference values replace visibility.
		if (values != null) {
			_hiddenColumns.Clear();
			foreach (var column in _preferenceColumns)
				if (column.Hidden.HasValue)
					_hiddenColumns[column.Key] = column.Hidden.Value;
		}
		_manualWidths.Clear();
		foreach (var pair in settings?.ColumnWidths ?? new Dictionary<string, double>())
			if (!string.IsNullOrWhiteSpace(pair.Key) && double.IsFinite(pair.Value) && pair.Value > 0)
				_manualWidths[pair.Key] = Math.Clamp(pair.Value, 40, 100000);
	}

	// Persistent identities are explicit schema; captions and column positions are presentation only.
	private void ValidatePreferenceKeys() {
		if (Preferences == null && (!PersistSettings || string.IsNullOrWhiteSpace(SettingsKey)))
			return;
		var keys = _schemaColumns.Select(column => column.PreferenceKey).ToArray();
		Guard.Argument(keys.All(key => !string.IsNullOrWhiteSpace(key)) && keys.Distinct(StringComparer.Ordinal).Count() == keys.Length,
			nameof(Columns), "Persistent grid columns require distinct stable PreferenceKey values.");
	}

	// The displayed columns follow the declared schema and accepted visibility; an unchanged result keeps its array.
	private void ApplyColumnVisibility() {
		var visible = _schemaColumns.Where(column => !IsColumnHidden(column)).ToArray();
		if (visible.Length == _schemaColumns.Length)
			visible = _schemaColumns;
		if (!visible.SequenceEqual(_columns))
			_columns = visible;
		Controller.Columns = _columns;
	}

	// An accepted preference overrides the schema unless every declared column is requested.
	private bool IsColumnHidden(BlazorGridColumn<TItem> column)
		=> !ShowHiddenColumns && (!string.IsNullOrWhiteSpace(column.PreferenceKey) && _hiddenColumns.TryGetValue(column.PreferenceKey, out var hidden) ? hidden : column.Hidden);

	// Normalize query choices and prefer the declared width schema over an old standalone dictionary.
	private BlazorGridSettings GetPreferenceSettings(BlazorGridSettings preferences) {
		var columns = preferences.Columns ?? Array.Empty<ApplicationGridColumnPreferences>();
		var widths = columns.Length == 0
			? preferences.ColumnWidths ?? new Dictionary<string, double>()
			: columns.Where(column => column != null && !string.IsNullOrWhiteSpace(column.Key) && column.Width.HasValue)
				.GroupBy(column => column.Key, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First().Width.Value, StringComparer.Ordinal);
		return new BlazorGridSettings {
			PageMode = Enum.IsDefined(preferences.PageMode) ? preferences.PageMode : BlazorGridPageMode.Paged,
			PageSize = Math.Clamp(preferences.PageSize, 1, 9999),
			PageNumber = Math.Max(0, preferences.PageNumber),
			SortColumnName = preferences.SortColumnName,
			SortDirection = Enum.IsDefined(preferences.SortDirection) ? preferences.SortDirection : SortDirection.None,
			ColumnWidths = widths.Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && double.IsFinite(pair.Value) && pair.Value > 0)
				.ToDictionary(pair => pair.Key, pair => Math.Clamp(pair.Value, 40, 100000), StringComparer.Ordinal)
		};
	}

	// Returns detached schema values; an unambiguous old caption can be imported into its stable key once.
	private static BlazorGridSettings ToPreferences(BlazorGridSettings settings, BlazorGridColumn<TItem>[] columns) {
		var widths = settings.ColumnWidths ?? new Dictionary<string, double>();
		var schema = columns == null
			? widths.Select(pair => new ApplicationGridColumnPreferences { Key = pair.Key, Title = pair.Key, Width = pair.Value }).ToArray()
			: columns.Where(column => !string.IsNullOrWhiteSpace(column.PreferenceKey)).Select(column => {
				var hasWidth = widths.TryGetValue(column.PreferenceKey, out var width);
				if (!hasWidth && columns.Count(other => other.ColumnName == column.ColumnName) == 1)
					hasWidth = widths.TryGetValue(column.ColumnName ?? string.Empty, out width);
				return new ApplicationGridColumnPreferences { Key = column.PreferenceKey, Title = column.ColumnName, Width = hasWidth ? width : null };
			}).ToArray();
		return new BlazorGridSettings {
			PageMode = settings.PageMode,
			PageSize = settings.PageSize,
			PageNumber = settings.PageNumber,
			SortColumnName = settings.SortColumnName,
			SortDirection = settings.SortDirection,
			Columns = schema
		};
	}

	// Width-only accepted changes need no source read.
	private static bool QuerySettingsEqual(BlazorGridSettings left, BlazorGridSettings right) {
		return left != null && right != null && left.PageMode == right.PageMode && left.PageSize == right.PageSize
			&& left.PageNumber == right.PageNumber && left.SortColumnName == right.SortColumnName && left.SortDirection == right.SortDirection;
	}

	// Parameter rerenders must not reapply unchanged values or disturb a row draft.
	private static bool PreferenceSettingsEqual(BlazorGridSettings left, BlazorGridSettings right) {
		return QuerySettingsEqual(left, right) && left.ColumnWidths.Count == right.ColumnWidths.Count
			&& left.ColumnWidths.All(pair => right.ColumnWidths.TryGetValue(pair.Key, out var width) && width == pair.Value);
	}

	// Keep optional declared columns and their migration version even when they are not in this binding set.
	private bool PreferenceSchemaEqual(BlazorGridSettings values) {
		return _preferenceColumns.Select(column => (column.Key, column.Title, column.Hidden))
			.SequenceEqual((values.Columns ?? Array.Empty<ApplicationGridColumnPreferences>())
				.Where(column => column != null && !string.IsNullOrWhiteSpace(column.Key)).Select(column => (column.Key, column.Title, column.Hidden)));
	}
}
