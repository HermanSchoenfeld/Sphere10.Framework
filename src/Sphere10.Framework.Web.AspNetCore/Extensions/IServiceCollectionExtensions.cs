// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application;
using Sphere10.Framework.Web.AspNetCore;
using Microsoft.Extensions.Logging;
using Sphere10.Framework;


namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions {
	public static IServiceCollection AddSphere10Framework(this IServiceCollection serviceCollection) {
		Sphere10Framework.Instance.RegisterModules(serviceCollection);
		return serviceCollection;
	}

	public static IServiceCollection AddSphere10FrameworkLogger(this IServiceCollection serviceCollection, Sphere10.Framework.ILogger logger) {
		return serviceCollection.AddTransient<ILoggerProvider>(_ => new Sphere10LoggerProvider(logger));
	}

}

