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
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

// One live screen owns detached working values; the application profile owns accepted values and persistence.
internal sealed class WinFormsApplicationScreenSettings : IDisposable {
	private readonly Control _owner;
	private readonly Action<object, object> _changed;
	private readonly ApplicationSettingsProfile _profile;
	private readonly ISettingsProvider _provider;
	private readonly SynchronizationContext _uiContext;
	private readonly object _producer = new();
	private readonly object _changesSync = new();
	private Task _pendingChanges = Task.CompletedTask;
	private long _latestAcceptedRevision;
	private readonly List<GridBinding> _grids = new();
	private readonly List<LogBinding> _logs = new();
	private int _applyingLogPreferences;
	private object _values;
	private long _baseRevision;
	private long _version;
	private bool _dirty;
	private bool _disposed;
	private Exception _lastLoadError;

	// Attachment precedes native Load, so typed values are available before any first query.
	public WinFormsApplicationScreenSettings(Control owner, ApplicationSettingsProfile profile, ISettingsProvider provider, Action<object, object> changed = null) {
		_owner = owner;
		_changed = changed;
		_profile = profile;
		_provider = provider;
		_uiContext = SynchronizationContext.Current;
		_profile.Changed += ProfileChanged;
		_values = profile.GetValues(out _baseRevision);
		_latestAcceptedRevision = _baseRevision;
	}

	internal Exception LastLoadError => _lastLoadError;

	// Values are already detached from the provider and catalog; callers treat this snapshot as read-only.
	public TSettings GetValues<TSettings>() {
		return Guard.ArgumentCast<TSettings>(_values, nameof(TSettings));
	}

	// Ordinary public properties already provide the getter/setter needed by the native editors.
	public Task BindGridAsync(VisualGrid grid, string propertyName, string role, CancellationToken token) {
		var property = GetProperty(propertyName, role == null);
		if (role == null)
			return BindGridAsync<object>(grid, propertyName, values => (VisualGridSettings)property.GetValue(values),
				(values, preferences) => property.SetValue(values, preferences), token);
		return BindGridAsync<object>(grid, $"{propertyName}[{role}]",
			values => ((IDictionary<string, VisualGridSettings>)property.GetValue(values))[role],
			(values, preferences) => ((IDictionary<string, VisualGridSettings>)property.GetValue(values))[role] = preferences, token);
	}

	// A logging property is enough; callers need no path plus duplicate getter/setter delegates.
	public Task BindLogOutputAsync(LogOutputSelectorControl selector, string propertyName, CancellationToken token) {
		var property = GetProperty(propertyName);
		return BindLogOutputAsync<object>(selector, propertyName, values => (LogOutputSelectorPreferences)property.GetValue(values),
			(values, preferences) => property.SetValue(values, preferences), token);
	}

	// An absent role imports the old grid record once, in memory, without writing during first display.
	public async Task BindGridAsync<TSettings>(VisualGrid grid, string sectionPath, Func<TSettings, VisualGridSettings> get,
		Action<TSettings, VisualGridSettings> set, CancellationToken token) {
		Guard.ArgumentNotNull(grid, nameof(grid));
		Guard.ArgumentNotNullOrEmpty(sectionPath, nameof(sectionPath));
		Guard.ArgumentNotNull(get, nameof(get));
		Guard.ArgumentNotNull(set, nameof(set));
		Guard.Ensure(!_grids.Exists(binding => ReferenceEquals(binding.Grid, grid) || binding.SectionPath == sectionPath), "Each grid role can be bound only once in a screen.");
		// Select ownership before the first await, so a child Load cannot launch competing standalone restoration.
		var binding = new GridBinding(grid, sectionPath, values => get((TSettings)values), (values, preferences) => set((TSettings)values, preferences));
		ApplyGrid(binding);
		if (!_profile.SectionVersions.ContainsKey(sectionPath)) {
			await _profile.WaitForPendingWritesAsync(token);
			await CompletePendingChangesAsync(token);
			var legacy = _provider == null ? null : await ReadOptionalLegacyAsync(() => grid.ReadLegacyPreferencesAsync(_provider, token), token);
			token.ThrowIfCancellationRequested();
			ObjectDisposedException.ThrowIf(_disposed, this);
			await InitializeOptionalSectionAsync(sectionPath, values => {
				if (legacy != null)
					set((TSettings)values, legacy);
			}, (source, target) => set((TSettings)target, get((TSettings)source)), token);
		}
		ApplyGrid(binding);
		binding.Changed = preferences => {
			binding.Set(_values, preferences);
			_dirty = true;
			_version++;
		};
		grid.PreferencesChanged += binding.Changed;
		_grids.Add(binding);
	}

	// Logging configuration imports values only; the product chooses when its ready/preflight boundary may resume output.
	public async Task BindLogOutputAsync<TSettings>(LogOutputSelectorControl selector, string sectionPath,
		Func<TSettings, LogOutputSelectorPreferences> get, Action<TSettings, LogOutputSelectorPreferences> set, CancellationToken token) {
		Guard.ArgumentNotNull(selector, nameof(selector));
		Guard.ArgumentNotNullOrEmpty(sectionPath, nameof(sectionPath));
		Guard.ArgumentNotNull(get, nameof(get));
		Guard.ArgumentNotNull(set, nameof(set));
		Guard.Ensure(!_logs.Exists(binding => ReferenceEquals(binding.Selector, selector) || binding.SectionPath == sectionPath), "Each logging role can be bound only once in a screen.");
		var binding = new LogBinding(selector, sectionPath, values => get((TSettings)values), (values, preferences) => set((TSettings)values, preferences));
		var preferences = binding.Get(_values);
		selector.Preferences = preferences?.Configuration?.RememberAfterClose == true ? preferences : new LogOutputSelectorPreferences();
		if (!_profile.SectionVersions.ContainsKey(sectionPath)) {
			await _profile.WaitForPendingWritesAsync(token);
			await CompletePendingChangesAsync(token);
			var legacy = _provider == null ? null : await ReadOptionalLegacyAsync(() => selector.ReadLegacyPreferencesAsync(_provider, token), token);
			token.ThrowIfCancellationRequested();
			ObjectDisposedException.ThrowIf(_disposed, this);
			await InitializeOptionalSectionAsync(sectionPath, values => {
				if (legacy?.Configuration?.RememberAfterClose == true)
					set((TSettings)values, legacy);
			}, (source, target) => set((TSettings)target, get((TSettings)source)), token);
		}
		var restored = binding.Get(_values);
		selector.ApplyPreferences(restored?.Configuration?.RememberAfterClose == true ? restored : new LogOutputSelectorPreferences());
		binding.Changed = preferences => {
			if (_applyingLogPreferences != 0)
				return;
			binding.Set(_values, preferences);
			MarkChanged();
		};
		selector.PreferencesChanged += binding.Changed;
		_logs.Add(binding);
	}

	// A deliberate scalar edit shares the same producer/version as grid changes.
	public void MarkChanged() {
		_dirty = true;
		_version++;
	}

	// Accepted close/shutdown queues an exclusive snapshot and keeps failures dirty for a deliberate retry.
	public async Task SaveAsync(CancellationToken token) {
		await CompletePendingChangesAsync(token);
		while (_dirty && !_disposed) {
			token.ThrowIfCancellationRequested();
			var capture = _profile.Capture(_values, _producer, _baseRevision, _version);
			await _profile.SaveCaptureAsync(capture, token);
			// Own publication clears only the captured local version; newer accepted edits or section deltas run first.
			await CompletePendingChangesAsync(token);
			if (_dirty)
				await Task.Yield();
		}
	}

	// Disposal only releases subscriptions; it never invents a late preference write.
	public void Dispose() {
		_disposed = true;
		_profile.Changed -= ProfileChanged;
		foreach (var binding in _grids)
			binding.Grid.PreferencesChanged -= binding.Changed;
		_grids.Clear();
		foreach (var binding in _logs)
			binding.Selector.PreferencesChanged -= binding.Changed;
		_logs.Clear();
	}

	// Preserve publication order across worker callbacks and awaited native output drains.
	private void ProfileChanged(ApplicationSettingsChangedEventArgs change) {
		if (_disposed)
			return;
		Task previous;
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		lock (_changesSync) {
			previous = _pendingChanges;
			_pendingChanges = completion.Task;
			if (!change.IsSectionInitialization)
				_latestAcceptedRevision = Math.Max(_latestAcceptedRevision, change.Revision);
		}
		async void Apply() {
			try {
				await previous;
				await ApplyChangeAsync(change);
			} catch (Exception error) {
				// Failed runtime preflight remains visible on the selector without undoing the accepted preference record.
				Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Exception(error));
			} finally {
				completion.TrySetResult();
			}
		}
		if (_uiContext != null && SynchronizationContext.Current != _uiContext)
			_uiContext.Post(_ => Apply(), null);
		else if (_owner.IsHandleCreated && _owner.InvokeRequired)
			_owner.BeginInvoke((Action)Apply);
		else
			Apply();
	}

	// Initialization changes only its declared section, never another live draft or its retryable save state.
	private async Task ApplyChangeAsync(ApplicationSettingsChangedEventArgs change) {
		if (_disposed || _owner.IsDisposed || change.Revision <= _baseRevision)
			return;
		if (change.IsSectionInitialization) {
			change.ApplyInitialization(_values);
			_baseRevision = change.Revision;
			foreach (var binding in _grids)
				ApplyGrid(binding);
			return;
		}
		if (change.Revision < Volatile.Read(ref _latestAcceptedRevision))
			return;
		if (ReferenceEquals(change.Source, _producer)) {
			_baseRevision = change.Revision;
			if (change.SourceVersion == _version)
				_dirty = false;
			return;
		}

		// An accepted foreign edit is authoritative; its snapshot can include later section materialization too.
		var previousValues = _values;
		_values = _profile.GetValues(out _baseRevision);
		_dirty = false;
		foreach (var binding in _grids) {
			ApplyGrid(binding);
			await binding.Grid.RefreshGrid();
			if (_disposed || change.Revision < Volatile.Read(ref _latestAcceptedRevision))
				return;
		}
		_applyingLogPreferences++;
		try {
			foreach (var binding in _logs) {
				await binding.Selector.StopLoggingAsync();
				if (_disposed || change.Revision < Volatile.Read(ref _latestAcceptedRevision))
					return;
				var preferences = binding.Get(_values) ?? new LogOutputSelectorPreferences();
				binding.Selector.ApplyPreferences(preferences);
				if (preferences.HasConfiguration && preferences.Enabled && preferences.Configuration?.RememberAfterClose == true)
					binding.Selector.StartLogging();
			}
		} finally {
			_applyingLogPreferences--;
		}
		_changed?.Invoke(previousValues, _values);
	}

	// Bind and close wait for already published UI rebases, including initialization deltas queued from workers.
	private async Task CompletePendingChangesAsync(CancellationToken token) {
		while (true) {
			Task pending;
			lock (_changesSync)
				pending = _pendingChanges;
			await pending.WaitAsync(token);
			lock (_changesSync)
				if (ReferenceEquals(pending, _pendingChanges))
					return;
		}
	}

	// Optional legacy IO errors belong to the owner policy; cancellation and control readers retain their normal contracts.
	private async Task<TPreferences> ReadOptionalLegacyAsync<TPreferences>(Func<Task<TPreferences>> read, CancellationToken token)
		where TPreferences : class {
		try {
			return await read();
		} catch (OperationCanceledException) when (token.IsCancellationRequested) {
			throw;
		} catch (Exception error) {
			RecordLoadError(error);
			return null;
		}
	}

	// Invalid old values fall back to declared defaults and record presence without saving during restoration.
	private async Task InitializeOptionalSectionAsync(string path, Action<object> import, Action<object, object> copySection, CancellationToken token) {
		await _profile.WaitForPendingWritesAsync(token);
		await CompletePendingChangesAsync(token);
		try {
			_profile.TryInitializeSection(path, 0, import, copySection);
		} catch (OperationCanceledException) when (token.IsCancellationRequested) {
			throw;
		} catch (Exception error) {
			RecordLoadError(error);
			await _profile.WaitForPendingWritesAsync(token);
			await CompletePendingChangesAsync(token);
			_profile.TryInitializeSection(path, 0, _ => { }, copySection);
		}
		await CompletePendingChangesAsync(token);
	}

	// Keep a readable failure on the live screen while allowing its first data query with validated defaults.
	private void RecordLoadError(Exception error) {
		_lastLoadError = error;
		Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Exception(error));
	}

	// Reuse the same normal property metadata as the object editor, without another schema model.
	private PropertyDescriptor GetProperty(string name, bool requiresSetter = true) {
		Guard.ArgumentNotNullOrEmpty(name, nameof(name));
		var property = TypeDescriptor.GetProperties(_values)[name];
		Guard.Argument(property != null && (!requiresSetter || !property.IsReadOnly), nameof(name), "Select a writable settings property.");
		return property;
	}

	// Selecting owned preferences suppresses the grid's independent legacy persistence path.
	private void ApplyGrid(GridBinding binding) {
		var preferences = binding.Get(_values) ?? new VisualGridSettings();
		binding.Grid.Preferences = preferences;
		binding.Grid.ApplyGridPreferences(preferences);
	}

	// Logging and grid roles share the root capture/version authority while retaining their existing control lifetimes.
	private sealed class LogBinding(LogOutputSelectorControl selector, string sectionPath, Func<object, LogOutputSelectorPreferences> get,
		Action<object, LogOutputSelectorPreferences> set) {
		public LogOutputSelectorControl Selector { get; } = selector;

		public string SectionPath { get; } = sectionPath;

		public Func<object, LogOutputSelectorPreferences> Get { get; } = get;

		public Action<object, LogOutputSelectorPreferences> Set { get; } = set;

		public EventHandlerEx<LogOutputSelectorPreferences> Changed { get; set; }
	}

	// Role accessors belong to product schemas; the grid receives values without knowing profiles/providers.
	private sealed class GridBinding(VisualGrid grid, string sectionPath, Func<object, VisualGridSettings> get,
		Action<object, VisualGridSettings> set) {
		public VisualGrid Grid { get; } = grid;

		public string SectionPath { get; } = sectionPath;

		public Func<object, VisualGridSettings> Get { get; } = get;

		public Action<object, VisualGridSettings> Set { get; } = set;

		public EventHandlerEx<VisualGridSettings> Changed { get; set; }
	}
}
