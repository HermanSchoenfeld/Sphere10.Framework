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

/// <summary>Caches settings identities by type and ID while borrowing the wrapped provider.</summary>
public class CachedSettingsProvider : SettingsProviderDecorator {

	// A record identity combines its declared settings type with the provider-specific ID.
	private readonly record struct SettingsKey(Type Type, object Id);

	private readonly SynchronizedDictionary<SettingsKey, SettingsObject> _cache;

	/// <summary>Borrows the provider and starts an empty settings-identity cache.</summary>
	public CachedSettingsProvider(ISettingsProvider internalSettingsProvider)
		: base(internalSettingsProvider) {
		_cache = new SynchronizedDictionary<SettingsKey, SettingsObject>();
	}

	/// <inheritdoc />
	public override SettingsObject NewSetting(Type settingsObjectType, object id = null) {
		var key = new SettingsKey(settingsObjectType, id);
		var setting = base.NewSetting(settingsObjectType, id);
		_cache[key] = setting;
		return setting;
	}

	/// <inheritdoc />
	public override async Task<SettingsObject> NewSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();

		var key = new SettingsKey(settingsObjectType, id);
		var setting = await base.NewSettingAsync(settingsObjectType, id, cancellationToken).ConfigureAwait(false);
		_cache[key] = setting;
		return setting;
	}

	/// <inheritdoc />
	public override SettingsObject LoadSetting(Type settingsObjectType, object id = null) {
		var key = new SettingsKey(settingsObjectType, id);

		if (!_cache.TryGetValue(key, out var setting)) {
			setting = base.LoadSetting(settingsObjectType, id);
			_cache[key] = setting;
		}

		return setting;
	}

	/// <inheritdoc />
	public override async Task<SettingsObject> LoadSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();

		var key = new SettingsKey(settingsObjectType, id);

		if (!_cache.TryGetValue(key, out var setting)) {
			setting = await base.LoadSettingAsync(settingsObjectType, id, cancellationToken).ConfigureAwait(false);
			_cache[key] = setting;
		}

		return setting;
	}

	/// <inheritdoc />
	public override SettingsObject LoadSetting(SettingsObject defaults) {
		var key = new SettingsKey(defaults.GetType(), defaults.ID);

		if (!_cache.TryGetValue(key, out var setting)) {
			setting = base.LoadSetting(defaults);
			_cache[key] = setting;
		}

		return setting;
	}

	/// <inheritdoc />
	public override async Task<SettingsObject> LoadSettingAsync(SettingsObject defaults, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();

		var key = new SettingsKey(defaults.GetType(), defaults.ID);

		if (!_cache.TryGetValue(key, out var setting)) {
			setting = await base.LoadSettingAsync(defaults, cancellationToken).ConfigureAwait(false);
			_cache[key] = setting;
		}

		return setting;
	}

	/// <inheritdoc />
	public override void SaveSetting(SettingsObject setting) {
		var key = new SettingsKey(setting.GetType(), setting.ID);

		// Provider callbacks may read this value; a rejected save restores the previous cached identity.
		var hadPrevious = _cache.TryGetValue(key, out var previous);
		_cache[key] = setting;

		try {
			base.SaveSetting(setting);
		} catch {
			if (hadPrevious)
				_cache[key] = previous;
			else
				_cache.Remove(key);
			throw;
		}
	}

	/// <inheritdoc />
	public override async Task SaveSettingAsync(SettingsObject setting, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();

		var key = new SettingsKey(setting.GetType(), setting.ID);

		// Provider callbacks may read this value; a rejected save restores the previous cached identity.
		var hadPrevious = _cache.TryGetValue(key, out var previous);
		_cache[key] = setting;

		try {
			await base.SaveSettingAsync(setting, cancellationToken).ConfigureAwait(false);
		} catch {
			if (hadPrevious)
				_cache[key] = previous;
			else
				_cache.Remove(key);
			throw;
		}
	}

	/// <inheritdoc />
	public override bool ContainsSetting(Type settingsObjectType, object id = null) {
		var key = new SettingsKey(settingsObjectType, id);
		if (_cache.ContainsKey(key))
			return true;
		return base.ContainsSetting(settingsObjectType, id);
	}

	/// <inheritdoc />
	public override async Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();

		return _cache.ContainsKey(new SettingsKey(settingsObjectType, id)) || await base.ContainsSettingAsync(settingsObjectType, id, cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public override void DeleteSetting(SettingsObject setting) {
		var key = new SettingsKey(setting.GetType(), setting.ID);
		_cache.Remove(key);
		base.DeleteSetting(setting);
	}

	/// <inheritdoc />
	public override async Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
		await base.DeleteSettingAsync(settings, cancellationToken).ConfigureAwait(false);
		_cache.Remove(new SettingsKey(settings.GetType(), settings.ID));
	}

	/// <inheritdoc />
	public override void ClearSettings() {
		base.ClearSettings();
		_cache.Clear();
	}

	/// <inheritdoc />
	public override async Task ClearSettingsAsync(CancellationToken cancellationToken = default) {
		await base.ClearSettingsAsync(cancellationToken).ConfigureAwait(false);
		_cache.Clear();
	}
}
