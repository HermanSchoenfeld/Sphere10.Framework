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

namespace Sphere10.Framework.Windows.Forms;

// Native bindings edit the actual screen settings; the existing provider owns their persistence.
internal sealed class ApplicationScreenSettings : IDisposable {
	private readonly SettingsObject _settings;
	private readonly List<GridBinding> _grids = new();
	private readonly List<LogBinding> _logs = new();
	private bool _applying;
	private bool _dirty;
	private bool _disposed;

	// Borrow the screen's actual provider-bound instance without creating a working copy.
	public ApplicationScreenSettings(SettingsObject settings) {
		Guard.ArgumentNotNull(settings, nameof(settings));
		_settings = settings;
	}

	// Bind an ordinary settings property or one named entry in its existing grid dictionary.
	public Task BindGridAsync(VisualGrid grid, string propertyName, string role, CancellationToken cancellationToken) {
		var property = GetProperty(propertyName, role == null);
		if (role == null)
			return BindGridAsync<SettingsObject>(grid, propertyName, values => (VisualGridSettings)property.GetValue(values),
				(values, preferences) => property.SetValue(values, preferences), cancellationToken);
		return BindGridAsync<SettingsObject>(grid, $"{propertyName}[{role}]",
			values => ((IDictionary<string, VisualGridSettings>)property.GetValue(values))[role],
			(values, preferences) => ((IDictionary<string, VisualGridSettings>)property.GetValue(values))[role] = preferences, cancellationToken);
	}

	// Bind a logging control to its declared settings property.
	public Task BindLogOutputAsync(LogOutputSelectorControl selector, string propertyName, CancellationToken cancellationToken) {
		var property = GetProperty(propertyName);
		return BindLogOutputAsync<SettingsObject>(selector, propertyName, values => (LogOutputSelectorPreferences)property.GetValue(values),
			(values, preferences) => property.SetValue(values, preferences), cancellationToken);
	}

	// Retain only the native binding and its accessors; no profile or publication state is required.
	public async Task BindGridAsync<TSettings>(VisualGrid grid, string sectionPath, Func<TSettings, VisualGridSettings> get,
		Action<TSettings, VisualGridSettings> set, CancellationToken cancellationToken)
		where TSettings : SettingsObject {
		Guard.ArgumentNotNull(grid, nameof(grid));
		Guard.ArgumentNotNullOrEmpty(sectionPath, nameof(sectionPath));
		Guard.ArgumentNotNull(get, nameof(get));
		Guard.ArgumentNotNull(set, nameof(set));
		Guard.Ensure(!_grids.Exists(binding => ReferenceEquals(binding.Grid, grid)), "Each grid can be bound only once.");

		// Apply the screen's declared columns before reading an optional old control record.
		var binding = new GridBinding(grid, values => get((TSettings)values), (values, preferences) => set((TSettings)values, preferences));
		ApplyGrid(binding);
		if (await ShouldReadLegacyAsync(cancellationToken)) {
			var legacy = await ReadOptionalLegacyAsync(() => ReadLegacyGridPreferencesAsync(grid, cancellationToken), cancellationToken);
			if (legacy != null && !_dirty) {
				// Old control records may contain only width overrides; retain the screen's complete column schema.
				var columns = binding.Get(_settings).Columns;
				var overrides = (legacy.Columns ?? Array.Empty<ApplicationGridColumnPreferences>())
					.Where(column => column != null && !string.IsNullOrWhiteSpace(column.Key))
					.GroupBy(column => column.Key, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
				legacy.Columns = VisualGridSettings.CopyColumns(columns);
				foreach (var column in legacy.Columns) {
					if (!overrides.Remove(column.Key, out var previous))
						continue;
					column.Width = previous.Width;
					column.ExpandsToFit = previous.ExpandsToFit;
				}
				legacy.Columns = legacy.Columns.Concat(VisualGridSettings.CopyColumns(overrides.Values.ToArray())).ToArray();
				binding.Set(_settings, legacy);
			}
		}
		cancellationToken.ThrowIfCancellationRequested();
		ObjectDisposedException.ThrowIf(_disposed, this);
		ApplyGrid(binding);

		// Ordinary grid operations remain local until the user explicitly saves that grid's configuration.
		_grids.Add(binding);
	}

	// The standard header menu is available only for an existing provider-bound grid section.
	public bool CanConfigureGrid(VisualGrid grid) => !_disposed && _settings.Provider != null && _grids.Exists(binding => ReferenceEquals(binding.Grid, grid));

	// Capture only this grid; unrelated grids keep both their saved values and temporary presentation.
	public async Task SaveGridConfigurationAsync(VisualGrid grid, CancellationToken cancellationToken) {
		var binding = GetGridBinding(grid);
		var preferences = grid.CaptureGridPreferences();
		binding.Set(_settings, preferences);

		// A successful save notification must not replace newer live edits made while storage was pending.
		binding.AppliedPreferences = preferences.Copy();
		_dirty = true;
		await SaveAsync(cancellationToken);
	}

	// Use the settings class's declared grid defaults without changing any accepted owner values.
	public async Task RestoreGridDefaultsAsync(VisualGrid grid, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		var binding = GetGridBinding(grid);
		var defaults = (SettingsObject)Tools.Object.Create(_settings.GetType());
		var preferences = binding.Get(defaults);
		Guard.Ensure(preferences != null, "Declare the grid's defaults on its screen settings class.");
		grid.ApplyGridPreferences(preferences);
		await grid.RefreshGrid();
	}

	// Restore remembered output values without starting resources during the binding operation.
	public async Task BindLogOutputAsync<TSettings>(LogOutputSelectorControl selector, string sectionPath,
		Func<TSettings, LogOutputSelectorPreferences> get, Action<TSettings, LogOutputSelectorPreferences> set, CancellationToken cancellationToken)
		where TSettings : SettingsObject {
		Guard.ArgumentNotNull(selector, nameof(selector));
		Guard.ArgumentNotNullOrEmpty(sectionPath, nameof(sectionPath));
		Guard.ArgumentNotNull(get, nameof(get));
		Guard.ArgumentNotNull(set, nameof(set));
		Guard.Ensure(!_logs.Exists(binding => ReferenceEquals(binding.Selector, selector)), "Each output selector can be bound only once.");

		// Assign ownership before the optional standalone legacy read can suspend.
		var binding = new LogBinding(selector, values => get((TSettings)values), (values, preferences) => set((TSettings)values, preferences));
		selector.Preferences = binding.Get(_settings) ?? new LogOutputSelectorPreferences();
		if (await ShouldReadLegacyAsync(cancellationToken)) {
			var legacy = await ReadOptionalLegacyAsync(() => selector.ReadLegacyPreferencesAsync(_settings.Provider, cancellationToken), cancellationToken);
			if (legacy?.Configuration?.RememberAfterClose == true && !_dirty)
				binding.Set(_settings, legacy);
		}
		cancellationToken.ThrowIfCancellationRequested();
		ObjectDisposedException.ThrowIf(_disposed, this);
		var preferences = binding.Get(_settings);
		selector.ApplyPreferences(preferences?.Configuration?.RememberAfterClose == true ? preferences : new LogOutputSelectorPreferences());

		// The logging control supplies plain settings values; the root remains the persistence owner.
		binding.Changed = values => {
			if (_applying)
				return;
			binding.Set(_settings, values);
			_dirty = true;
		};
		selector.PreferencesChanged += binding.Changed;
		_logs.Add(binding);
	}

	// Mark an intentional scalar edit for the same explicit save boundary as native control edits.
	public void MarkChanged() => _dirty = true;

	// Save the actual settings object; successful persistence emits its existing OnChanged notification.
	public async Task SaveAsync(CancellationToken cancellationToken) {
		if (!_dirty || _disposed)
			return;
		// Keep edits made during persistence pending for the next accepted save.
		_dirty = false;
		try {
			await _settings.SaveAsync(cancellationToken);
		} catch {
			_dirty = true;
			throw;
		}
	}

	// Reapply actual settings after an editor saves and publishes their change notification.
	public async Task ApplyAsync() {
		if (_disposed)
			return;
		_applying = true;
		try {
			foreach (var binding in _grids) {
				if (ApplyGrid(binding))
					await binding.Grid.RefreshGrid();
				if (_disposed)
					return;
			}
			foreach (var binding in _logs) {
				await binding.Selector.StopLoggingAsync();
				if (_disposed)
					return;
				var preferences = binding.Get(_settings) ?? new LogOutputSelectorPreferences();
				binding.Selector.ApplyPreferences(preferences);
				if (preferences.HasConfiguration && preferences.Enabled && preferences.Configuration?.RememberAfterClose == true)
					binding.Selector.StartLogging();
			}
		} finally {
			_applying = false;
		}
	}

	// Release native event subscriptions without inventing a disposal-time write.
	public void Dispose() {
		_disposed = true;
		_grids.Clear();
		foreach (var binding in _logs)
			binding.Selector.PreferencesChanged -= binding.Changed;
		_logs.Clear();
	}

	// Legacy grid records are read only by the screen owner, never by the visual control.
	private async Task<VisualGridSettings> ReadLegacyGridPreferencesAsync(VisualGrid grid, CancellationToken cancellationToken) {
		var key = GetLegacyGridSettingsKey(grid);
		if (key == null)
			return null;
		var provider = _settings.Provider;
		VisualGridSettings values;
		if (await provider.ContainsSettingAsync(typeof(VisualGridSettings), key, cancellationToken)) {
			values = ((VisualGridSettings)await provider.LoadSettingAsync(typeof(VisualGridSettings), key, cancellationToken)).Copy();
		} else if (await provider.ContainsSettingAsync(typeof(CrudGridSettings), key, cancellationToken)) {
			var legacy = (CrudGridSettings)await provider.LoadSettingAsync(typeof(CrudGridSettings), key, cancellationToken);
			values = new VisualGridSettings {
				ConsumerVersion = legacy.ConsumerVersion,
				PageMode = legacy.PageMode.HasValue ? (VisualGridPageMode)legacy.PageMode.Value : null,
				AutoPageSize = legacy.AutoPageSize,
				PageSize = legacy.PageSize,
				PageNumber = legacy.PageNumber,
				SortColumnName = legacy.SortColumnName,
				SortDirection = legacy.SortDirection,
				Columns = VisualGridSettings.CopyColumns(legacy.Columns)
			};
		} else {
			return null;
		}
		if (grid.IsDisposed || grid.Disposing)
			return null;

		values.PageMode ??= values.AutoPageSize ? VisualGridPageMode.Auto : VisualGridPageMode.Paged;
		return values;
	}

	// Preserve the former control identity solely for importing an existing record into its screen.
	private static string GetLegacyGridSettingsKey(VisualGrid grid) {
		var path = new List<string>();
		for (Control control = grid; control != null; control = control.Parent) {
			if (control is ApplicationScreen || control != grid && control is UserControl && control.GetType() != typeof(UserControl))
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

	// A typed record is authoritative; old standalone control records are consulted only before it exists.
	private async Task<bool> ShouldReadLegacyAsync(CancellationToken cancellationToken)
		=> _settings.Provider != null && !await _settings.Provider.ContainsSettingAsync(_settings.GetType(), _settings.ID, cancellationToken);

	// Preserve optional-load failure reporting while cancellation keeps its normal control-flow contract.
	private async Task<T> ReadOptionalLegacyAsync<T>(Func<Task<T>> read, CancellationToken cancellationToken)
		where T : class {
		try {
			return await read();
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		} catch (Exception error) {
			Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Exception(error));
			return null;
		}
	}

	// Reuse component-model property metadata rather than introducing another settings schema.
	private PropertyDescriptor GetProperty(string name, bool requiresSetter = true) {
		Guard.ArgumentNotNullOrEmpty(name, nameof(name));
		var property = TypeDescriptor.GetProperties(_settings)[name];
		Guard.Argument(property != null && (!requiresSetter || !property.IsReadOnly), nameof(name), "Select a writable settings property.");
		return property;
	}

	// Reapply accepted configuration changes while leaving temporary views intact after unrelated saves.
	private bool ApplyGrid(GridBinding binding) {
		var preferences = binding.Get(_settings);
		if (preferences == null) {
			preferences = new VisualGridSettings();
			binding.Set(_settings, preferences);
		}
		if (binding.AppliedPreferences?.HasSameConfiguration(preferences) == true)
			return false;
		binding.Grid.Preferences = preferences;
		// Options edits the owner object, so populate its full column list instead of hiding gaps in a local editor.
		preferences.Columns = binding.Grid.CaptureGridPreferences().Columns;
		binding.AppliedPreferences = preferences.Copy();
		return true;
	}

	// Configuration commands must target a grid already bound to this settings owner.
	private GridBinding GetGridBinding(VisualGrid grid) {
		Guard.ArgumentNotNull(grid, nameof(grid));
		ObjectDisposedException.ThrowIf(_disposed, this);
		var binding = _grids.Find(binding => ReferenceEquals(binding.Grid, grid));
		Guard.Ensure(binding != null, "Bind the grid to its screen settings before configuring it.");
		return binding;
	}

	#region Inner Types

	// Remember owner values last applied or explicitly captured so later saves preserve temporary views.
	private sealed class GridBinding(VisualGrid grid, Func<SettingsObject, VisualGridSettings> get, Action<SettingsObject, VisualGridSettings> set) {
		public VisualGrid Grid { get; } = grid;

		public Func<SettingsObject, VisualGridSettings> Get { get; } = get;

		public Action<SettingsObject, VisualGridSettings> Set { get; } = set;

		public VisualGridSettings AppliedPreferences { get; set; }
	}

	// Logging bindings retain the same minimal ownership information as grid bindings.
	private sealed class LogBinding(LogOutputSelectorControl selector, Func<SettingsObject, LogOutputSelectorPreferences> get,
		Action<SettingsObject, LogOutputSelectorPreferences> set) {
		public LogOutputSelectorControl Selector { get; } = selector;

		public Func<SettingsObject, LogOutputSelectorPreferences> Get { get; } = get;

		public Action<SettingsObject, LogOutputSelectorPreferences> Set { get; } = set;

		public EventHandlerEx<LogOutputSelectorPreferences> Changed { get; set; }
	}

	#endregion

}
