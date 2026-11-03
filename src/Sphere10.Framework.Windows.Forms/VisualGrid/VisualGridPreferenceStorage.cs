// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Owns detached standalone preference IO and ordered saves for one grid lifetime.</summary>
/// <remarks>Queue and status state stay on the owning grid's UI thread. Provider IO runs on workers; providers remain caller-owned.</remarks>
internal sealed class VisualGridPreferenceStorage {
	private Task _pendingSave = Task.CompletedTask;
	private long _version;
	private Exception _saveError;
	private bool _saveFailed;

	/// <summary>The current identity/save generation, also used to reject obsolete initialization reads.</summary>
	public long Version {
		get {
			return _version;
		}
	}

	/// <summary>The latest published failure, retained while a retry is in progress.</summary>
	public Exception SaveError {
		get {
			return _saveError;
		}
	}

	/// <summary>Whether the latest completed write needs to be retried.</summary>
	public bool SaveFailed {
		get {
			return _saveFailed;
		}
	}

	/// <summary>Queues an accepted value using its captured provider and identity; callers retain provider ownership.</summary>
	public void Save(ISettingsProvider provider, string key, ILogger logger, VisualGridSettings values) {
		// Copy before queueing so later UI edits cannot change the accepted payload.
		var snapshot = Copy(values);
		snapshot.ID = key;
		snapshot.Provider = provider;
		var version = ++_version;
		_saveFailed = false;
		_pendingSave = SaveCoreAsync(_pendingSave, provider, snapshot, logger, version);
	}

	/// <summary>Waits for accepted writes and reports the latest failure without disposing a borrowed provider.</summary>
	public async Task FlushAsync() {
		await _pendingSave;
		if (_saveError != null)
			throw new InvalidOperationException("Could not save CRUD grid preferences.", _saveError);
	}

	/// <summary>Invalidates old completion status while retaining write ordering across identity changes.</summary>
	public void ResetIdentity() {
		_version++;
		_saveError = null;
		_saveFailed = false;
	}

	/// <summary>Reads a detached current or legacy record on a worker without creating or saving defaults.</summary>
	public static Task<VisualGridSettings> ReadAsync(ISettingsProvider provider, string key, CancellationToken cancellationToken = default) {
		return Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			return Read(provider, key);
		}, cancellationToken);
	}

	// One tail orders all accepted writes, including older identities; only the current generation publishes status.
	private async Task SaveCoreAsync(Task previous, ISettingsProvider provider, VisualGridSettings snapshot, ILogger logger, long version) {
		await previous;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(() => Task.Run(() => provider.SaveSetting(snapshot)));
		if (error != null)
			(logger ?? SystemLog.Logger).Exception(error, "Could not save CRUD grid preferences.");
		if (version != _version)
			return;

		_saveError = error;
		_saveFailed = error != null;
	}

	// Prefer the current record; old CLR names are retained solely for existing provider files.
	private static VisualGridSettings Read(ISettingsProvider provider, string key) {
		if (provider.ContainsSetting(typeof(VisualGridSettings), key))
			return Copy((VisualGridSettings)provider.LoadSetting(typeof(VisualGridSettings), key));
		if (!provider.ContainsSetting(typeof(CrudGridSettings), key))
			return null;

		var legacy = (CrudGridSettings)provider.LoadSetting(typeof(CrudGridSettings), key);
		return Copy(new VisualGridSettings {
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

	/// <summary>Copies only preference values; storage identity and provider bindings stay with their owner.</summary>
	public static VisualGridSettings Copy(VisualGridSettings settings) {
		return new VisualGridSettings {
			ConsumerVersion = settings.ConsumerVersion,
			PageMode = settings.PageMode,
			AutoPageSize = settings.AutoPageSize,
			PageSize = settings.PageSize,
			PageNumber = settings.PageNumber,
			SortColumnName = settings.SortColumnName,
			SortDirection = settings.SortDirection,
			Columns = (settings.Columns ?? Array.Empty<ApplicationGridColumnPreferences>()).Where(column => column != null)
				.Select(column => new ApplicationGridColumnPreferences { Key = column.Key, Title = column.Title, Width = column.Width, ExpandsToFit = column.ExpandsToFit }).ToArray()
		};
	}
}
