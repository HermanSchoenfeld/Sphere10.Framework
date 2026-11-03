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

/// <summary>Forwards settings operations and notifications to an existing provider without changing its execution direction.</summary>
public class SettingsProviderDecorator : ISettingsProvider {
	/// <inheritdoc />
	public event EventHandlerEx Changed {
		add {
			InternalSettingsProvider.Changed += value;
		}
		remove {
			InternalSettingsProvider.Changed -= value;
		}
	}

	/// <summary>Borrows the provider wrapped by this decorator.</summary>
	public SettingsProviderDecorator(ISettingsProvider internalSettingsProvider) {
		InternalSettingsProvider = internalSettingsProvider;
	}

	/// <inheritdoc />
	public virtual bool AutoSaveNewSettings {
		get {
			return InternalSettingsProvider.AutoSaveNewSettings;
		}
	}

	/// <inheritdoc />
	public virtual bool EncryptSettings {
		get {
			return InternalSettingsProvider.EncryptSettings;
		}
	}

	/// <summary>The borrowed provider, available for inspecting an existing provider chain.</summary>
	public ISettingsProvider Inner {
		get {
			return InternalSettingsProvider;
		}
	}

	/// <summary>The provider used by every forwarded operation and notification.</summary>
	protected virtual ISettingsProvider InternalSettingsProvider { get; }

	/// <inheritdoc />
	public virtual SettingsObject NewSetting(Type settingsObjectType, object id = null) {
		return InternalSettingsProvider.NewSetting(settingsObjectType, id);
	}

	/// <inheritdoc />
	public virtual Task<SettingsObject> NewSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
		return InternalSettingsProvider.NewSettingAsync(settingsObjectType, id, cancellationToken);
	}

	/// <inheritdoc />
	public virtual SettingsObject LoadSetting(Type settingsObjectType, object id = null) {
		return InternalSettingsProvider.LoadSetting(settingsObjectType, id);
	}

	/// <inheritdoc />
	public virtual Task<SettingsObject> LoadSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
		return InternalSettingsProvider.LoadSettingAsync(settingsObjectType, id, cancellationToken);
	}

	/// <inheritdoc />
	public virtual SettingsObject LoadSetting(SettingsObject defaults) {
		return InternalSettingsProvider.LoadSetting(defaults);
	}

	/// <inheritdoc />
	public virtual Task<SettingsObject> LoadSettingAsync(SettingsObject defaults, CancellationToken cancellationToken = default) {
		return InternalSettingsProvider.LoadSettingAsync(defaults, cancellationToken);
	}

	/// <inheritdoc />
	public virtual void SaveSetting(SettingsObject settings) {
		InternalSettingsProvider.SaveSetting(settings);
	}

	/// <inheritdoc />
	public virtual Task SaveSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
		return InternalSettingsProvider.SaveSettingAsync(settings, cancellationToken);
	}

	/// <inheritdoc />
	public virtual bool ContainsSetting(Type settingsObjectType, object id = null) {
		return InternalSettingsProvider.ContainsSetting(settingsObjectType, id);
	}

	/// <inheritdoc />
	public virtual Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
		return InternalSettingsProvider.ContainsSettingAsync(settingsObjectType, id, cancellationToken);
	}

	/// <inheritdoc />
	public virtual void DeleteSetting(SettingsObject settings) {
		InternalSettingsProvider.DeleteSetting(settings);
	}

	/// <inheritdoc />
	public virtual Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
		return InternalSettingsProvider.DeleteSettingAsync(settings, cancellationToken);
	}

	/// <inheritdoc />
	public virtual void ClearSettings() {
		InternalSettingsProvider.ClearSettings();
	}

	/// <inheritdoc />
	public virtual Task ClearSettingsAsync(CancellationToken cancellationToken = default) {
		return InternalSettingsProvider.ClearSettingsAsync(cancellationToken);
	}
}
