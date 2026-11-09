// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Web.AspNetCore;

namespace Microsoft.Extensions.Hosting;

public static class IHostExtensions {
	/// <summary>
	/// Starts the configuration registered by <c>AddSphere10Framework</c> on the host's service provider.
	/// Set <see cref="Sphere10FrameworkOptions"/> with <see cref="Sphere10FrameworkBuilder.WithOptions"/> inside the <c>AddSphere10Framework</c> callback, because modules read them during registration.
	/// </summary>
	public static IHost StartSphere10Framework(this IHost host) {
		var Builder = Sphere10Framework.Instance.PendingBuilder;
		Guard.Ensure(Builder != null, "AddSphere10Framework must be called before StartSphere10Framework");
		Sphere10Framework.Instance.SetAspNetCoreHost(host);
		Builder.Start(host.Services);
		Sphere10Framework.Instance.PendingBuilder = null;
		return host;
	}

}

