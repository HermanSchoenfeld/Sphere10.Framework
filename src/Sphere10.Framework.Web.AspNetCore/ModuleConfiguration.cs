// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Web.AspNetCore;

public class ModuleConfiguration : ModuleConfigurationBase {

	public override int Priority => int.MinValue; // last to execute

	public override void RegisterComponents(IServiceCollection serviceCollection) {

		// register initializers
		serviceCollection.AddInitializer<AspNetCoreLifecycleMonitorInitializer>();
	}

	public override void OnInitialize(IServiceProvider serviceProvider) {
		base.OnInitialize(serviceProvider);
	}
}

