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
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

// Native bindings edit the actual screen settings; the existing provider owns their persistence.
internal sealed class WinFormsApplicationScreenSettings : IDisposable {
	private readonly SettingsObject _settings;
	private readonly List<GridBinding> _grids = new();
	private readonly List<LogBinding> _logs = new();
	private bool _applying;
	private bool _dirty;
	private bool _disposed;

	// Borrow the screen's actual provider-bound instance without creating a working copy.
	public WinFormsApplicationScreenSettings(SettingsObject settings) {
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

		// Claim settings ownership before legacy IO, preventing the grid's standalone restore path.
		var binding = new GridBinding(grid, values => get((TSettings)values), (values, preferences) => set((TSettings)values, preferences));
		ApplyGrid(binding);
		if (await ShouldReadLegacyAsync(cancellationToken)) {
			var legacy = await ReadOptionalLegacyAsync(() => grid.ReadLegacyPreferencesAsync(_settings.Provider, cancellationToken), cancellationToken);
			if (legacy != null && !_dirty)
				binding.Set(_settings, legacy);
		}
		cancellationToken.ThrowIfCancellationRequested();
		ObjectDisposedException.ThrowIf(_disposed, this);
		ApplyGrid(binding);

		// Native edits modify the actual owner object and save at the existing accepted-close boundary.
		binding.Changed = preferences => {
			if (_applying)
				return;
			binding.Set(_settings, preferences);
			_dirty = true;
		};
		grid.PreferencesChanged += binding.Changed;
		_grids.Add(binding);
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
	public void MarkChanged() {
		_dirty = true;
	}

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
				ApplyGrid(binding);
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
		foreach (var binding in _grids)
			binding.Grid.PreferencesChanged -= binding.Changed;
		_grids.Clear();
		foreach (var binding in _logs)
			binding.Selector.PreferencesChanged -= binding.Changed;
		_logs.Clear();
	}

	// A typed record is authoritative; old standalone control records are consulted only before it exists.
	private async Task<bool> ShouldReadLegacyAsync(CancellationToken cancellationToken) {
		return _settings.Provider != null && !await _settings.Provider.ContainsSettingAsync(_settings.GetType(), _settings.ID, cancellationToken);
	}

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

	// Native grids borrow the corresponding actual nested settings value from their screen.
	private void ApplyGrid(GridBinding binding) {
		var preferences = binding.Get(_settings);
		if (preferences == null) {
			preferences = new VisualGridSettings();
			binding.Set(_settings, preferences);
		}
		binding.Grid.Preferences = preferences;
		binding.Grid.ApplyGridPreferences(preferences);
	}

	#region Inner Types

	// Retain only the native control, its actual-value accessors and its removable subscription.
	private sealed class GridBinding(VisualGrid grid, Func<SettingsObject, VisualGridSettings> get, Action<SettingsObject, VisualGridSettings> set) {
		public VisualGrid Grid { get; } = grid;

		public Func<SettingsObject, VisualGridSettings> Get { get; } = get;

		public Action<SettingsObject, VisualGridSettings> Set { get; } = set;

		public EventHandlerEx<VisualGridSettings> Changed { get; set; }
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
