// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;
using FormsUI = global::Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

public class ModuleConfiguration : ModuleConfigurationBase {
	public override void RegisterComponents(IServiceCollection serviceCollection) {
		serviceCollection.AddInitializer<IncrementUsageByOneInitializer>();
	}

	// Configure application-owned commands before application startup activates its fixed command collections.
	public override void OnInitialize(IServiceProvider serviceProvider) {
		base.OnInitialize(serviceProvider);
		WinFormsDemoPlugin.ConfigureApplicationCommands((FormsUI.Application)serviceProvider.GetRequiredService<FormsUI.IApplication>());
	}
}
