// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Owns accepted values, migration presence and ordered persistence for one registration or application profile.</summary>
public sealed class ApplicationSettingsProfile {
	/// <summary>Reports successfully persisted revisions with their runtime producer identity.</summary>
	public event EventHandlerEx<ApplicationSettingsChangedEventArgs> Changed;

	private readonly object _state = new();
	private readonly IApplicationSettingsStore _store;
	private readonly ILogger _logger;
	private readonly object _defaults;
	private readonly Func<object> _settingsFactory;
	private object _values;
	private Dictionary<string, int> _sections = new(StringComparer.Ordinal);
	private Task _pendingWrites = Task.CompletedTask;
	private bool _initialized;
	private long _revision;
	private object _lastSource;
	private long _lastSourceVersion;
	private Exception _lastLoadError;
	private Exception _lastSaveError;

	// The catalog supplies canonical ownership metadata; this profile never constructs its operational screen.
	internal ApplicationSettingsProfile(string profileKey, IApplicationBlock block, ApplicationScreenDefinition screen,
		ApplicationSettingsDefinition application, IApplicationSettingsStore store, ILogger logger) {
		ProfileKey = profileKey;
		Block = block;
		ScreenDefinition = screen;
		ApplicationDefinition = application;
		var settings = screen?.Settings ?? application?.Settings;
		_defaults = settings == null ? null : ApplicationSettingsValueCloner.Copy(settings);
		_settingsFactory = screen?.SettingsFactory ?? application?.SettingsFactory;
		Title = screen?.Title ?? application?.Title;
		_store = store;
		_logger = logger;
	}

	/// <summary>The caption-independent persistence identity; no-options pages need no persistent key.</summary>
	public string ProfileKey { get; }

	/// <summary>The readable Options navigation title.</summary>
	public string Title { get; }

	/// <summary>The source plugin, or null for a root or standalone block.</summary>
	public IApplicationPlugin Plugin => Block?.Source;

	/// <summary>The owning block for a screen profile, or null for application preferences.</summary>
	public IApplicationBlock Block { get; }

	/// <summary>The canonical screen registration, including unopened and no-options screens.</summary>
	public ApplicationScreenDefinition ScreenDefinition { get; }

	/// <summary>The application-root contribution, or null for a screen profile.</summary>
	public ApplicationSettingsDefinition ApplicationDefinition { get; }

	/// <summary>The ordinary settings object's type, or null for a no-options screen.</summary>
	public Type SettingsType => _defaults?.GetType();

	/// <summary>Whether an application-root contribution preserves an explicitly supplied legacy provider ID.</summary>
	public bool HasExplicitStorageId => ApplicationDefinition?.HasExplicitStorageId == true;

	/// <summary>The original provider ID, including null, when explicitly configured.</summary>
	public object StorageId => ApplicationDefinition?.StorageId;

	/// <summary>Whether this page declares editable preferences.</summary>
	public bool HasOptions => _defaults != null;

	/// <summary>Returns fresh default values from the registration's factory or its original settings object.</summary>
	public object CreateDefaults() {
		Guard.Ensure(HasOptions, "This registration has no configurable preferences.");
		var values = _settingsFactory == null ? _defaults : _settingsFactory();
		Guard.Ensure(values != null && SettingsType.IsInstanceOfType(values), "The settings factory returned an incompatible value.");
		return ApplicationSettingsValueCloner.Copy(values);
	}

	/// <summary>Uses SettingsObject validation when available, or standard object and property annotations for ordinary values.</summary>
	public Result Validate(object values) {
		Guard.Argument(values != null && SettingsType.IsInstanceOfType(values), nameof(values), "The settings value has the wrong type.");
		if (values is SettingsObject settings)
			return settings.Validate();

		var failures = new List<ValidationResult>();
		Validator.TryValidateObject(values, new ValidationContext(values), failures, true);
		var result = new Result();
		foreach (var failure in failures)
			result.AddError(failure.ErrorMessage ?? "The settings value is invalid.");
		return result;
	}

	/// <summary>The accepted value revision within this catalog lifetime.</summary>
	public long Revision {
		get {
			lock (_state)
				return _revision;
		}
	}

	/// <summary>An optional load failure for which validated defaults were used.</summary>
	public Exception LastLoadError {
		get {
			lock (_state)
				return _lastLoadError;
		}
	}

	/// <summary>The last failed save, cleared only by a later successful persistence.</summary>
	public Exception LastSaveError {
		get {
			lock (_state)
				return _lastSaveError;
		}
	}

	/// <summary>A snapshot of initialized named sections, preserved when option values are reset.</summary>
	public IReadOnlyDictionary<string, int> SectionVersions {
		get {
			lock (_state)
				return new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(_sections, StringComparer.Ordinal));
		}
	}

	/// <summary>Returns detached accepted values; editing them cannot change the catalog or provider cache.</summary>
	public object GetValues() {
		return GetValues(out _);
	}

	/// <summary>Atomically copies accepted values and their matching revision for a live instance's capture baseline.</summary>
	public object GetValues(out long revision) {
		lock (_state) {
			RequireValues();
			var values = ApplicationSettingsValueCloner.Copy(_values);
			revision = _revision;
			return values;
		}
	}

	/// <summary>Returns a detached copy of the declared typed values before a screen begins its data work.</summary>
	public TSettings GetValues<TSettings>() {
		return GetValues<TSettings>(out _);
	}

	/// <summary>Atomically returns typed detached values and the exact accepted revision represented by them.</summary>
	public TSettings GetValues<TSettings>(out long revision) {
		return Guard.ArgumentCast<TSettings>(GetValues(out revision), nameof(TSettings));
	}

	/// <summary>Starts one detached Options page without writing or creating its screen.</summary>
	public ApplicationSettingsEditSession BeginEdit() {
		return new ApplicationSettingsEditSession(this);
	}

	/// <summary>Copies UI-owned values now, recording the producer's applied baseline and increasing local version.</summary>
	public ApplicationSettingsCapture Capture(object values, object owner, long baseRevision, long ownerVersion) {
		Guard.ArgumentNotNull(owner, nameof(owner));
		Guard.Argument(baseRevision >= 0, nameof(baseRevision), "A baseline revision cannot be negative.");
		Guard.Argument(ownerVersion > 0, nameof(ownerVersion), "A capture version must be positive.");
		RequireValues();
		Guard.Argument(values != null && SettingsType.IsInstanceOfType(values), nameof(values), "The captured settings have the wrong type.");
		return new ApplicationSettingsCapture(this, ApplicationSettingsValueCloner.Copy(values), owner, baseRevision, ownerVersion);
	}

	/// <summary>Orders a detached background snapshot, returning false if a newer accepted owner has superseded it.</summary>
	public Task<bool> SaveCaptureAsync(ApplicationSettingsCapture capture, CancellationToken cancellationToken = default) {
		Guard.Argument(capture != null && ReferenceEquals(capture.Profile, this), nameof(capture), "The capture belongs to another profile.");
		Validate(capture.Values).ThrowOnFailure();
		return QueueWrite(capture.Values, capture.Owner, capture.OwnerVersion, capture.BaseRevision, false, cancellationToken);
	}

	/// <summary>Initializes an absent section and notifies consumers through a source-to-target section copier; no values are saved.</summary>
	/// <param name="copySection">Copies only the initialized section from the source snapshot to consumer-owned target values.</param>
	public bool TryInitializeSection(string sectionKey, int version, Action<object> initialize,
		Action<object, object> copySection) {
		Guard.ArgumentNotNullOrEmpty(sectionKey, nameof(sectionKey));
		Guard.Argument(version >= 0, nameof(version), "A section version cannot be negative.");
		Guard.ArgumentNotNull(initialize, nameof(initialize));
		Guard.ArgumentNotNull(copySection, nameof(copySection));
		ApplicationSettingsChangedEventArgs change;
		lock (_state) {
			RequireValues();
			if (_sections.ContainsKey(sectionKey))
				return false;
			Guard.Ensure(_pendingWrites.IsCompleted, "Initialize a preference section before accepting writes, or await catalog FlushAsync first.");
			var values = ApplicationSettingsValueCloner.Copy(_values);
			initialize(values);
			Validate(values).ThrowOnFailure();
			change = new ApplicationSettingsChangedEventArgs(_revision + 1, null, 0, values, copySection);
			_values = values;
			_sections.Add(sectionKey, version);
			_revision = change.Revision;
			_lastSource = null;
			_lastSourceVersion = 0;
		}
		// Initializers run once under the state boundary; consumers only copy the frozen initialized section outside it.
		NotifyChanged(change);
		return true;
	}

	// Startup owns loading; a failed optional read keeps validated defaults without creating a record.
	internal async Task InitializeAsync(CancellationToken token) {
		if (_initialized)
			return;
		if (!HasOptions) {
			_initialized = true;
			return;
		}
		Guard.Ensure(_store != null, "Declared application settings require an application-owned settings store.");
		var values = CreateDefaults();
		Validate(values).ThrowOnFailure();
		var sections = new Dictionary<string, int>(StringComparer.Ordinal);
		try {
			var stored = await _store.LoadAsync(ProfileKey, ApplicationSettingsValueCloner.Copy(values), HasExplicitStorageId, StorageId, token).ConfigureAwait(false);
			if (stored != null) {
				// Migration presence remains authoritative even if old option values themselves need defaults.
				foreach (var section in stored.SectionVersions ?? new Dictionary<string, int>())
					if (!string.IsNullOrWhiteSpace(section.Key) && section.Value >= 0)
						sections[section.Key] = section.Value;
				Guard.Ensure(stored.Values != null && SettingsType.IsInstanceOfType(stored.Values), "Stored settings have the wrong declared type.");
				var loaded = ApplicationSettingsValueCloner.Copy(stored.Values);
				Validate(loaded).ThrowOnFailure();
				values = loaded;
			}
		} catch (OperationCanceledException) when (token.IsCancellationRequested) {
			throw;
		} catch (Exception error) {
			_lastLoadError = error;
			LogError(error);
		}
		token.ThrowIfCancellationRequested();
		lock (_state) {
			_values = values;
			_sections = sections;
			_initialized = true;
		}
	}

	// Explicit Apply is authoritative after previously queued writes, regardless of its editor's old baseline.
	internal Task<bool> ApplyAsync(object values, object session, CancellationToken token) {
		RequireValues();
		return QueueWrite(values, session, 0, 0, true, token);
	}

	/// <summary>Awaits this profile's queued writes and reports an outstanding save failure without capturing new values.</summary>
	public async Task FlushAsync(CancellationToken token = default) {
		await WaitForPendingWritesAsync(token).ConfigureAwait(false);
		if (LastSaveError is { } error)
			throw error;
	}

	/// <summary>Awaits the existing write queue without clearing or rethrowing an earlier save failure.</summary>
	public async Task WaitForPendingWritesAsync(CancellationToken token = default) {
		Task pending;
		lock (_state)
			pending = _pendingWrites;
		await pending.WaitAsync(token).ConfigureAwait(false);
	}

	// Completion tails always succeed so an earlier reported failure cannot poison a later retry's queue.
	private Task<bool> QueueWrite(object values, object source, long sourceVersion, long baseRevision, bool explicitApply, CancellationToken token) {
		Task previous;
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		lock (_state) {
			previous = _pendingWrites;
			_pendingWrites = completion.Task;
		}
		return WriteAsync(previous, completion, values, source, sourceVersion, baseRevision, explicitApply, token);
	}

	// Only one detached snapshot enters the owning store at a time; successful persistence precedes publication.
	private async Task<bool> WriteAsync(Task previous, TaskCompletionSource completion, object values, object source,
		long sourceVersion, long baseRevision, bool explicitApply, CancellationToken token) {
		try {
			await previous.ConfigureAwait(false);
			token.ThrowIfCancellationRequested();
			Dictionary<string, int> sections;
			lock (_state) {
				if (!explicitApply && (baseRevision > _revision || baseRevision < _revision && !ReferenceEquals(source, _lastSource)
					|| ReferenceEquals(source, _lastSource) && sourceVersion <= _lastSourceVersion))
					return false;
				sections = new Dictionary<string, int>(_sections, StringComparer.Ordinal);
			}
			if (explicitApply && !HasExplicitStorageId)
				foreach (var section in GetSectionPaths(values, string.Empty, new HashSet<object>(ReferenceEqualityComparer.Instance)))
					sections.TryAdd(section, 0);

			// Store implementations may temporarily encrypt this exclusive copy, never the editor or accepted root.
			var snapshot = new ApplicationSettingsStoredProfile { Values = ApplicationSettingsValueCloner.Copy(values), SectionVersions = sections };
			await _store.SaveAsync(ProfileKey, snapshot, HasExplicitStorageId, StorageId, token).ConfigureAwait(false);
			ApplicationSettingsChangedEventArgs change;
			lock (_state) {
				_values = ApplicationSettingsValueCloner.Copy(values);
				_sections = sections;
				_lastSource = source;
				_lastSourceVersion = sourceVersion;
				_lastSaveError = null;
				change = new ApplicationSettingsChangedEventArgs(++_revision, source, sourceVersion);
			}
			NotifyChanged(change);
			return true;
		} catch (Exception error) {
			lock (_state)
				_lastSaveError = error;
			LogError(error);
			throw;
		} finally {
			completion.TrySetResult();
		}
	}

	// Accepted Options values own their public nested sections, independently of a control or platform's settings type.
	private static IEnumerable<string> GetSectionPaths(object value, string path, HashSet<object> visited) {
		if (!string.IsNullOrEmpty(path))
			yield return path;
		if (value == null || ApplicationSettingsValueCloner.IsScalar(value.GetType()) || !visited.Add(value))
			yield break;
		try {
			if (value is IDictionary dictionary) {
				foreach (DictionaryEntry item in dictionary)
					if (item.Key is string key)
						foreach (var section in GetSectionPaths(item.Value, path + "[" + key.Replace("\\", "\\\\").Replace("]", "\\]") + "]", visited))
							yield return section;
				yield break;
			}
			if (value is IEnumerable)
				yield break;
			foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
				if (property.GetIndexParameters().Length == 0 && property.GetMethod?.IsPublic == true && property.SetMethod?.IsPublic == true
					&& !ApplicationSettingsValueCloner.IsIgnored(value, property, property.PropertyType))
					foreach (var section in GetSectionPaths(property.GetValue(value), string.IsNullOrEmpty(path) ? property.Name : path + "." + property.Name, visited))
						yield return section;
			foreach (var field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
				if (!field.IsInitOnly && !ApplicationSettingsValueCloner.IsIgnored(value, field, field.FieldType))
					foreach (var section in GetSectionPaths(field.GetValue(value), string.IsNullOrEmpty(path) ? field.Name : path + "." + field.Name, visited))
						yield return section;
		} finally {
			visited.Remove(value);
		}
	}

	// Subscriber failures cannot turn a successfully persisted and published revision into a failed save.
	private void NotifyChanged(ApplicationSettingsChangedEventArgs change) {
		foreach (var handler in Changed?.GetInvocationList().Cast<EventHandlerEx<ApplicationSettingsChangedEventArgs>>() ?? Enumerable.Empty<EventHandlerEx<ApplicationSettingsChangedEventArgs>>())
			try {
				handler(change);
			} catch (Exception error) {
				LogError(error);
			}
	}

	// Optional preference failures remain visible even when a logger's own sink is unavailable.
	private void LogError(Exception error) {
		if (_logger != null)
			Tools.Exceptions.ExecuteIgnoringException(() => _logger.Exception(error));
	}

	// Metadata enumeration is available before initialization; value access deliberately is not.
	private void RequireValues() {
		Guard.Ensure(HasOptions, "This registration has no configurable preferences.");
		Guard.Ensure(_initialized, "Application settings must initialize before their values are used.");
	}
}
