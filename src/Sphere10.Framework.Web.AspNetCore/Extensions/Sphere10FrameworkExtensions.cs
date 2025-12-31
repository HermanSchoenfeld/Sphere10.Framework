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

public static class Sphere10FrameworkExtensions {
	private static IHost _host;
	public static void SetAspNetCoreHost(this Sphere10Framework framework, IHost host)
		=> _host = host;

	public static IHost GetAspNetCoreHost(this Sphere10Framework framework) {
		CheckSet();
		return _host;
	}

	private static void CheckSet()
		=> Guard.Ensure(_host != null, $"Sphere10.Framework framework is not informed of the AspNetCore host. Please call {nameof(IHostExtensions.StartSphere10Framework)} before running host.");

}

