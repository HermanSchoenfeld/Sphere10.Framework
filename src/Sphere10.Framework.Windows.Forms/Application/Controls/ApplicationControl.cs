// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A base class for all controls in the application. Provides access to application services and 
/// features like automatically detect child control state changes. Draws theme-aware borders.
/// </summary>
public partial class ApplicationControl : UserControlEx {

	private readonly IFuture<IUserInterfaceServices> _userInterfaceServices;

	public ApplicationControl() {
		if (!Tools.Runtime.IsDesignMode) {
			SettingsServices = Sphere10Framework.Instance.ServiceProvider.GetService<ISettingsServices>();
			_userInterfaceServices = Tools.Values.Future.LazyLoad(() => Sphere10Framework.Instance.ServiceProvider.GetService<IUserInterfaceServices>());
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected ISettingsServices SettingsServices { get; }

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected IUserInterfaceServices UserInterfaceServices => _userInterfaceServices.Value;
}

