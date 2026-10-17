// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A base class for all controls in the application. Provides access to application services and 
/// features like automatically detect child control state changes. Draws theme-aware borders.
/// </summary>
public partial class ApplicationControl : UserControlEx {

	private IServiceProvider _serviceProvider;
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

