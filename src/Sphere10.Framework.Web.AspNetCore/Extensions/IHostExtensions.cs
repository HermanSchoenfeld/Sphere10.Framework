// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application;
using Sphere10.Framework.Web.AspNetCore;

namespace Microsoft.Extensions.Hosting;

public static class IHostExtensions {
	public static IHost StartSphere10Framework(this IHost host, Sphere10FrameworkOptions options = Sphere10FrameworkOptions.Default) {
		Sphere10Framework.Instance.SetAspNetCoreHost(host);
		Sphere10Framework.Instance.StartFramework(host.Services, options);
		return host;
	}

}

