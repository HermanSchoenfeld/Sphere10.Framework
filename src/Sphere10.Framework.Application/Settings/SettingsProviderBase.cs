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

/// <summary>Shares settings identity, encryption and notifications while leaving both storage directions to their implementations.</summary>
public abstract class SettingsProviderBase : ISettingsProvider {

	/// <inheritdoc />
	public virtual event EventHandlerEx Changed;

	/// <inheritdoc />
	public virtual bool AutoSaveNewSettings => false;

	/// <inheritdoc />
	public virtual bool EncryptSettings => true;

	/// <inheritdoc />
	public virtual SettingsObject NewSetting(Type settingsObjectType, object id = null) {
		var settings = CreateSetting(settingsObjectType, id);
		if (AutoSaveNewSettings)
			SaveSetting(settings);
		return settings;
	}

	/// <inheritdoc />
	public virtual async Task<SettingsObject> NewSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();

		var settings = CreateSetting(settingsObjectType, id);
		if (AutoSaveNewSettings)
			await SaveSettingAsync(settings, cancellationToken);
		return settings;
	}

	/// <inheritdoc />
	public abstract SettingsObject LoadSetting(Type settingsObjectType, object id = null);

	/// <inheritdoc />
	public abstract Task<SettingsObject> LoadSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	public abstract SettingsObject LoadSetting(SettingsObject defaults);

	/// <inheritdoc />
	public abstract Task<SettingsObject> LoadSettingAsync(SettingsObject defaults, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	public abstract void SaveSetting(SettingsObject settings);

	/// <inheritdoc />
	public abstract Task SaveSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	public abstract bool ContainsSetting(Type settingsObjectType, object id = null);

	/// <inheritdoc />
	public abstract Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	public abstract void DeleteSetting(SettingsObject settings);

	/// <inheritdoc />
	public abstract Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default);

	/// <inheritdoc />
	public abstract void ClearSettings();

	/// <inheritdoc />
	public abstract Task ClearSettingsAsync(CancellationToken cancellationToken = default);

	// Create only the declared settings value and attach its original provider identity.
	protected SettingsObject CreateSetting(Type settingsObjectType, object id) {
		Guard.ArgumentNotNull(settingsObjectType, nameof(settingsObjectType));
		Guard.Argument(typeof(SettingsObject).IsAssignableFrom(settingsObjectType), nameof(settingsObjectType), "Settings must derive from SettingsObject.");

		var settings = (SettingsObject)Tools.Object.Create(settingsObjectType);
		settings.ID = id;
		settings.Provider = this;
		return settings;
	}

	// Complete ordinary loads through the same decryption and provider attachment in either direction.
	protected SettingsObject InitializeLoadedSetting(SettingsObject settings) {
		Guard.Ensure(settings != null, "The settings provider returned no settings value.");

		if (EncryptSettings)
			Tools.Object.DecryptMembers(settings);
		settings.Provider = this;
		return settings;
	}

	// Remember whether this operation encrypted its exclusive input so completion or failure can restore it.
	protected bool EncryptSetting(SettingsObject settings) {
		Guard.ArgumentNotNull(settings, nameof(settings));

		var encrypt = EncryptSettings;
		if (encrypt)
			Tools.Object.EncryptMembers(settings);
		return encrypt;
	}

	// Restore the operation's cleartext without consulting a possibly changed provider policy.
	protected void DecryptSetting(SettingsObject settings, bool encrypted) {
		if (encrypted)
			Tools.Object.DecryptMembers(settings);
	}

	// Storage implementations retain their existing notification boundaries.
	protected void FireChanged() {
		Changed?.Invoke();
	}
}
