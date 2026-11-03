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

/// <summary>Implements synchronous settings storage and adapts asynchronous calls through a worker.</summary>
public abstract class SyncSettingsProviderBase : SettingsProviderBase {

	/// <inheritdoc />
	public override Task<SettingsObject> NewSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
		return Task.Run(() => NewSetting(settingsObjectType, id), cancellationToken);
	}

	/// <inheritdoc />
	public override SettingsObject LoadSetting(Type settingsObjectType, object id = null) {
		return InitializeLoadedSetting(LoadInternal(settingsObjectType, id));
	}

	/// <inheritdoc />
	public override Task<SettingsObject> LoadSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
		return Task.Run(() => LoadSetting(settingsObjectType, id), cancellationToken);
	}

	/// <inheritdoc />
	public override SettingsObject LoadSetting(SettingsObject defaults) {
		Guard.ArgumentNotNull(defaults, nameof(defaults));

		var id = defaults.ID;

		// Missing encrypted members must retain their supplied cleartext default after the usual load decryption.
		var encrypted = EncryptSetting(defaults);

		try {
			var settings = LoadInternal(defaults);
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
	public override Task<SettingsObject> LoadSettingAsync(SettingsObject defaults, CancellationToken cancellationToken = default) {
		return Task.Run(() => LoadSetting(defaults), cancellationToken);
	}

	/// <inheritdoc />
	public override void SaveSetting(SettingsObject settings) {
		Guard.ArgumentNotNull(settings, nameof(settings));

		// The async adapter can persist this copy without exposing ciphertext on the shared settings root.
		var encrypted = EncryptSettings;
		var storedSettings = encrypted ? Tools.Object.Clone(settings) : settings;
		if (encrypted)
			Tools.Object.EncryptMembers(storedSettings);

		try {
			FireChanged();
			SaveInternal(storedSettings);
		} finally {
			DecryptSetting(storedSettings, encrypted);
		}
	}

	/// <inheritdoc />
	public override Task SaveSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
		return Task.Run(() => SaveSetting(settings), cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
		return Task.Run(() => ContainsSetting(settingsObjectType, id), cancellationToken);
	}

	/// <inheritdoc />
	public override Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
		return Task.Run(() => DeleteSetting(settings), cancellationToken);
	}

	/// <inheritdoc />
	public override Task ClearSettingsAsync(CancellationToken cancellationToken = default) {
		return Task.Run(ClearSettings, cancellationToken);
	}

	/// <summary>Reads the serialized settings through the concrete synchronous store.</summary>
	protected abstract SettingsObject LoadInternal(Type settingsObjectType, object id = null);

	// Providers that can read individual stored members may preserve supplied constructor defaults.
	protected virtual SettingsObject LoadInternal(SettingsObject defaults) {
		return LoadInternal(defaults.GetType(), defaults.ID);
	}

	/// <summary>Persists an already-encrypted settings value through the concrete synchronous store.</summary>
	protected abstract void SaveInternal(SettingsObject settings);
}
