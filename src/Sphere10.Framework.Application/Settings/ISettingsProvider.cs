// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

/// <summary>Creates and persists typed settings in synchronous and asynchronous execution paths.</summary>
public interface ISettingsProvider {

	/// <summary>Signals the provider's established settings-change boundary.</summary>
	event EventHandlerEx Changed;

	/// <summary>Whether creating a new settings value also saves it through this provider.</summary>
	bool AutoSaveNewSettings { get; }

	/// <summary>Whether encrypted members are transformed at the persistence boundary.</summary>
	bool EncryptSettings { get; }

	/// <summary>Creates the declared settings type and saves it when automatic saving is enabled.</summary>
	SettingsObject NewSetting(Type settingsObjectType, object id = null);

	/// <summary>Creates settings using the provider's execution direction and awaits any automatic save.</summary>
	Task<SettingsObject> NewSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default);

	/// <summary>Tests whether the declared type and identity have a stored record.</summary>
	bool ContainsSetting(Type settingsObjectType, object id = null);

	/// <summary>Checks persistence asynchronously without blocking an interactive caller.</summary>
	Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default);

	/// <summary>Restores the declared settings type and attaches its provider.</summary>
	SettingsObject LoadSetting(Type settingsObjectType, object id = null);

	/// <summary>Loads the declared settings type through this provider's serialization contract.</summary>
	Task<SettingsObject> LoadSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default);

	/// <summary>Loads persisted values over caller-owned defaults when supported by this provider.</summary>
	SettingsObject LoadSetting(SettingsObject defaults);

	/// <summary>Restores persisted values over a caller-owned default instance.</summary>
	Task<SettingsObject> LoadSettingAsync(SettingsObject defaults, CancellationToken cancellationToken = default);

	/// <summary>Persists the supplied settings value through this provider.</summary>
	void SaveSetting(SettingsObject settings);

	/// <summary>Persists caller-owned settings and awaits the provider's acknowledgement.</summary>
	Task SaveSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default);

	/// <summary>Deletes the record identified by the supplied settings type and ID.</summary>
	void DeleteSetting(SettingsObject settings);

	/// <summary>Deletes one settings record without blocking the caller.</summary>
	Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default);

	/// <summary>Deletes this provider's stored settings records.</summary>
	void ClearSettings();

	/// <summary>Clears this provider's settings asynchronously.</summary>
	Task ClearSettingsAsync(CancellationToken cancellationToken = default);

	/// <summary>Tests for a record of the requested settings type and identity.</summary>
	public bool Has<T>(object id = null)
		where T : SettingsObject {
		return ContainsSetting(typeof(T), id);
	}

	/// <summary>Awaits the record check for the requested settings type and identity.</summary>
	public Task<bool> HasAsync<T>(object id = null, CancellationToken cancellationToken = default)
		where T : SettingsObject {
		return ContainsSettingAsync(typeof(T), id, cancellationToken);
	}

	/// <summary>Loads existing settings or creates them using the provider's automatic-save policy.</summary>
	public T Get<T>(object id = null)
		where T : SettingsObject {
		SettingsObject settings;
		if (!ContainsSetting(typeof(T), id)) {
			settings = NewSetting(typeof(T), id) as T;
			if (settings == null)
				throw new Exception(string.Format("Settings provider did not create a settings object of requested type '{0}'", typeof(T).FullName));

			return (T)settings;
		}
		settings = LoadSetting(typeof(T), id) as T;
		if (settings == null)
			throw new Exception(string.Format("Settings provider did not load a settings object of requested type '{0}'", typeof(T).FullName));
		return (T)settings;
	}

	/// <summary>Awaits existing settings or creates them, including the provider's automatic-save policy.</summary>
	public async Task<T> GetAsync<T>(object id = null, CancellationToken cancellationToken = default)
		where T : SettingsObject {
		var exists = await ContainsSettingAsync(typeof(T), id, cancellationToken);
		var settings = exists
			? await LoadSettingAsync(typeof(T), id, cancellationToken)
			: await NewSettingAsync(typeof(T), id, cancellationToken);
		if (settings is not T typed)
			throw new Exception(string.Format("Settings provider did not {0} a settings object of requested type '{1}'", exists ? "load" : "create", typeof(T).FullName));
		return typed;
	}

	/// <summary>Reloads the supplied settings instance through its current type and ID.</summary>
	public void ReloadSetting(SettingsObject setting) {
		var reloaded = LoadSetting(setting.GetType(), setting.ID);
		Tools.Object.CopyMembers(setting, reloaded);
	}
}
