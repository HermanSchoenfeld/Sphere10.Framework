// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A base class for all controls in the application. Provides access to application services and 
/// features like automatically detect child control state changes. Draws theme-aware borders.
/// </summary>
public partial class ApplicationControl : UserControlEx {

	private IServiceProvider _serviceProvider;
	private WinFormsApplicationScreenSettings _applicationSettings;
	private IFuture<ISettingsServices> _settingsServices;
	private IFuture<IUserInterfaceServices> _userInterfaceServices;

	/// <summary>Resolves and retains the active runtime's settings service; unavailable on the design surface or outside its lifetime.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected ISettingsServices SettingsServices => TryInitializeServiceCache() ? _settingsServices.Value : null;

	/// <summary>Resolves and retains the active runtime's UI service; unavailable on the design surface or outside its lifetime.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected IUserInterfaceServices UserInterfaceServices => TryInitializeServiceCache() ? _userInterfaceServices.Value : null;

	// Plain application controls borrow the same class-valued preferences and ordered saves as hosted screens.
	protected bool TryGetApplicationSettings<TSettings>(string key, out TSettings settings) {
		if (_applicationSettings == null) {
			var framework = Sphere10Framework.Instance;
			var catalog = framework.IsStarted ? framework.ServiceProvider.GetService<ApplicationSettingsCatalog>() : null;
			if (catalog?.IsInitialized != true) {
				settings = default;
				return false;
			}
			_applicationSettings = new WinFormsApplicationScreenSettings(this, catalog.GetProfile(key), null, OnApplicationSettingsChanged);
			Disposables.Add(_applicationSettings);
		}
		settings = _applicationSettings.GetValues<TSettings>();
		return true;
	}

	// Deliberate accepted control edits are snapshotted and persisted by the existing internal coordinator.
	protected Task SaveApplicationSettingsAsync(CancellationToken cancellationToken = default) {
		Guard.Ensure(_applicationSettings != null, "Load declared application settings before saving them.");
		_applicationSettings.MarkChanged();
		return _applicationSettings.SaveAsync(cancellationToken);
	}

	// Products react to accepted values without observing profiles, producer tokens or revisions.
	protected virtual void OnApplicationSettingsChanged(object previous, object settings) {
	}

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

