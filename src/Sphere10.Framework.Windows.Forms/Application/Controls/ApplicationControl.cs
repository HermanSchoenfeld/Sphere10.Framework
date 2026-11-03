// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Provides application services and refreshes the existing control lifecycle when its actual settings change.</summary>
public partial class ApplicationControl : UserControlEx {
	private IServiceProvider _serviceProvider;
	private SettingsObject[] _observedSettings;
	private ApplicationScreenSettings _settingsBinding;
	private IFuture<ISettingsServices> _settingsServices;
	private IFuture<IUserInterfaceServices> _userInterfaceServices;

	/// <summary>The existing user and system settings services for the running application.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected ISettingsServices SettingsServices => TryInitializeServiceCache() ? _settingsServices.Value : null;

	/// <summary>The existing interaction service for the running application.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected IUserInterfaceServices UserInterfaceServices => TryInitializeServiceCache() ? _userInterfaceServices.Value : null;

	/// <summary>Whether this control has an actual settings instance attached before model initialization.</summary>
	protected bool HasSettings => DeclaredSettings != null;

	// Keep the actual declared instance available to typed screen properties.
	private protected SettingsObject DeclaredSettings { get; private set; }

	// Create native grid/log bindings only when this screen has settings to bind.
	private protected ApplicationScreenSettings SettingsBinding => DeclaredSettings == null ? null : _settingsBinding ??= new ApplicationScreenSettings(DeclaredSettings);

	// Attach actual owner instances before the existing Load/CopyModelToUI lifecycle runs.
	internal void AttachSettings(SettingsObject screenSettings, SettingsObject blockSettings, SettingsObject pluginSettings) {
		Guard.Ensure(_observedSettings == null, "A hosted control cannot change its settings owners.");
		DeclaredSettings = screenSettings;
		_observedSettings = new[] { screenSettings, blockSettings, pluginSettings }.Where(settings => settings != null)
			.Distinct<SettingsObject>(ReferenceEqualityComparer.Instance).ToArray();
		foreach (var settings in _observedSettings)
			settings.OnChanged += SettingsChanged;
		Disposables.Add(ReleaseSettings);
		if (Loaded)
			SettingsChanged();
	}

	// Stop observing settings before the control's native destruction callbacks run.
	private protected void ReleaseSettings() {
		_settingsBinding?.Dispose();
		if (_observedSettings == null)
			return;
		foreach (var settings in _observedSettings)
			settings.OnChanged -= SettingsChanged;
		_observedSettings = null;
	}

	// Handle recreation reapplies current values on the owning UI thread without queuing state while the handle is absent.
	protected override void OnHandleCreated(EventArgs e) {
		base.OnHandleCreated(e);
		if (Loaded && _observedSettings != null)
			ApplySettings();
	}

	// Settings may be saved on a worker; presentation always returns to the control's UI thread.
	private void SettingsChanged() {
		if (!Loaded || IsDisposed || Disposing || _observedSettings == null)
			return;
		if (InvokeRequired) {
			BeginInvoke((Action)ApplySettings);
			return;
		}
		if (IsHandleCreated)
			ApplySettings();
	}

	// Reuse grid/log application and the normal model-to-UI refresh without replacing settings identities.
	private async void ApplySettings() {
		if (!Loaded || !IsHandleCreated || IsDisposed || Disposing || _observedSettings == null)
			return;
		try {
			if (_settingsBinding != null)
				await _settingsBinding.ApplyAsync();
			if (!IsDisposed && !Disposing && _observedSettings != null)
				Refresh();
		} catch (Exception error) {
			Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Exception(error));
		}
	}

	// Cache services from the running Framework owner, leaving designer and standalone controls usable.
	private bool TryInitializeServiceCache() {
		var framework = Sphere10Framework.Instance;
		if (DesignMode || Tools.Runtime.IsDesignMode || !framework.IsStarted)
			return false;
		var provider = framework.ServiceProvider;
		if (provider == null)
			return false;
		if (!ReferenceEquals(_serviceProvider, provider)) {
			_serviceProvider = provider;
			_settingsServices = Tools.Values.Future.LazyLoad(() => provider.GetService<ISettingsServices>());
			_userInterfaceServices = Tools.Values.Future.LazyLoad(() => provider.GetService<IUserInterfaceServices>());
		}
		return true;
	}
}
