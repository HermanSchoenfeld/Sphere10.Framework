// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application;
using Microsoft.Extensions.Hosting;

namespace Sphere10.Framework.Web.AspNetCore;

internal class AspNetCoreLifecycleMonitorInitializer : ApplicationInitializerBase {

	public AspNetCoreLifecycleMonitorInitializer(IHostApplicationLifetime hostApplicationLifetime) {
		HostApplicationLifetime = hostApplicationLifetime;
	}
	protected IHostApplicationLifetime HostApplicationLifetime { get; }

	public override void Initialize() {
		HostApplicationLifetime.ApplicationStopped.Register(Sphere10Framework.Instance.EndFramework);
	}

}

