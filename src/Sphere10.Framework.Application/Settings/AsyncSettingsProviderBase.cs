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

/// <summary>Implements asynchronous settings storage and adapts synchronous calls outside the caller's synchronization context.</summary>
public abstract class AsyncSettingsProviderBase : SettingsProviderBase {

	/// <inheritdoc />
	public override SettingsObject NewSetting(Type settingsObjectType, object id = null) => Task.ExecuteSyncOverAsync(() => NewSettingAsync(settingsObjectType, id));

	/// <inheritdoc />
	public override async Task<SettingsObject> LoadSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();

		return InitializeLoadedSetting(await LoadInternalAsync(settingsObjectType, id, cancellationToken));
	}

	/// <inheritdoc />
	public override SettingsObject LoadSetting(Type settingsObjectType, object id = null) => Task.ExecuteSyncOverAsync(() => LoadSettingAsync(settingsObjectType, id));

	/// <inheritdoc />
	public override async Task<SettingsObject> LoadSettingAsync(SettingsObject defaults, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(defaults, nameof(defaults));
		cancellationToken.ThrowIfCancellationRequested();

		var id = defaults.ID;

		// Missing encrypted members retain their supplied defaults through the same overlay contract as synchronous providers.
		var encrypted = EncryptSetting(defaults);

		try {
			var settings = await LoadInternalAsync(defaults, cancellationToken);
			if (!ReferenceEquals(settings, defaults))
				DecryptSetting(settings, encrypted);
			settings.ID = id;
			settings.Provider = this;
			return settings;
		} finally {
			DecryptSetting(defaults, encrypted);
		}
	}

	/// <inheritdoc />
	public override SettingsObject LoadSetting(SettingsObject defaults) => Task.ExecuteSyncOverAsync(() => LoadSettingAsync(defaults));

	/// <inheritdoc />
	public override async Task SaveSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		Guard.ArgumentNotNull(settings, nameof(settings));

		// Root string encryption must not transform shared settings while storage is pending.
		var encrypted = EncryptSettings;
		var storedSettings = encrypted ? Tools.Object.Clone(settings) : settings;
		if (encrypted)
			Tools.Object.EncryptMembers(storedSettings);

		try {
			await SaveInternalAsync(storedSettings, cancellationToken);

			// An asynchronous store publishes the change only after its write has been acknowledged.
			FireChanged();
		} finally {
			DecryptSetting(storedSettings, encrypted);
		}
	}

	/// <inheritdoc />
	public override void SaveSetting(SettingsObject settings) => Task.ExecuteSyncOverAsync(() => SaveSettingAsync(settings));

	/// <inheritdoc />
	public override bool ContainsSetting(Type settingsObjectType, object id = null) => Task.ExecuteSyncOverAsync(() => ContainsSettingAsync(settingsObjectType, id));

	/// <inheritdoc />
	public override void DeleteSetting(SettingsObject settings) => Task.ExecuteSyncOverAsync(() => DeleteSettingAsync(settings));

	/// <inheritdoc />
	public override void ClearSettings() => Task.ExecuteSyncOverAsync(() => ClearSettingsAsync());

	/// <summary>Reads serialized settings through the concrete asynchronous store.</summary>
	protected abstract Task<SettingsObject> LoadInternalAsync(Type settingsObjectType, object id, CancellationToken cancellationToken);

	// Asynchronous providers can overlay stored members onto caller-owned constructor defaults.
	protected virtual Task<SettingsObject> LoadInternalAsync(SettingsObject defaults, CancellationToken cancellationToken)
		=> LoadInternalAsync(defaults.GetType(), defaults.ID, cancellationToken);

	/// <summary>Persists an already-encrypted settings value and awaits acceptance.</summary>
	protected abstract Task SaveInternalAsync(SettingsObject settings, CancellationToken cancellationToken);
}
