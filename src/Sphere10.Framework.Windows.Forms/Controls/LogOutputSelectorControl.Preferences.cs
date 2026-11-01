// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

public partial class LogOutputSelectorControl {
	/// <summary>Reports detached accepted values; the screen owns persistence when Preferences is non-null.</summary>
	public event EventHandlerEx<LogOutputSelectorPreferences> PreferencesChanged;

	private bool _ownsPreferences;

	/// <summary>Non-null values select screen ownership and disable the selector's separate legacy record.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public LogOutputSelectorPreferences Preferences {
		get => _ownsPreferences ? CapturePreferences() : null;
		set {
			EnsureOwnerThread();
			if (value != null)
				ApplyPreferences(value);
			_ownsPreferences = value != null;
			_settingsDirty = false;
			_lastSettingsError = null;
		}
	}

	/// <summary>Captures plain options and the remembered enabled choice without retaining an active logger.</summary>
	public LogOutputSelectorPreferences CapturePreferences() {
		EnsureOwnerThread();
		return new LogOutputSelectorPreferences {
			HasConfiguration = _hasConfiguration,
			Enabled = _rememberedEnabled,
			Configuration = Configuration
		};
	}

	/// <summary>Applies values to a stopped selector without saving, opening output or changing persistence ownership.</summary>
	public void ApplyPreferences(LogOutputSelectorPreferences preferences) {
		EnsureOwnerThread();
		Guard.ArgumentNotNull(preferences, nameof(preferences));
		Guard.Ensure(!IsLogging && !_configuring && !_stopping, "Stop logging and await its drain before applying preferences.");
		var configuration = preferences.Configuration?.Clone() ?? new LogOutputConfiguration();
		if (preferences.HasConfiguration)
			configuration = configuration.ValidateOptionsAndNormalize();
		_configuration = configuration;
		_hasConfiguration = preferences.HasConfiguration;
		_rememberedEnabled = preferences.HasConfiguration && preferences.Enabled;
		UpdateUiState();
	}

	/// <summary>Reads a detached legacy value through a captured provider; it never creates a record or opens remembered output.</summary>
	public async Task<LogOutputSelectorPreferences> ReadLegacyPreferencesAsync(ISettingsProvider provider, CancellationToken cancellationToken = default) {
		EnsureOwnerThread();
		Guard.ArgumentNotNull(provider, nameof(provider));
		var key = ResolveLogSettingsKey();
		if (key == null)
			return null;
		var preferences = await Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			if (!provider.ContainsSetting(typeof(LogOutputSelectorSettings), key))
				return null;
			var settings = (LogOutputSelectorSettings)provider.LoadSetting(typeof(LogOutputSelectorSettings), key);
			return new LogOutputSelectorPreferences {
				HasConfiguration = settings.HasConfiguration,
				Enabled = settings.IsLogging,
				Configuration = settings.Configuration?.Clone()
			};
		}, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		return IsDisposed || Disposing ? null : preferences;
	}

	/// <summary>Lets derived selectors observe accepted values before the public preference notification.</summary>
	protected virtual void OnPreferencesChanged(LogOutputSelectorPreferences preferences) {
	}

	// The control's template hook and public event share one normal native event boundary.
	private void NotifyPreferencesChanged(LogOutputSelectorPreferences preferences) {
		if (!CanRaiseEvents)
			return;
		OnPreferencesChanged(preferences);
		PreferencesChanged?.Invoke(preferences);
	}
}
