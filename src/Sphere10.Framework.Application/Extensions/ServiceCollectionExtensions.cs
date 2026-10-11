// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using Sphere10.Framework.Application;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions {
	public static bool HasImplementationFor<T>(this IServiceCollection serviceCollection)
		=> serviceCollection.Any(x => x.ServiceType == typeof(T));

	public static bool HasImplementation<T>(this IServiceCollection serviceCollection)
		=> serviceCollection.Any(x => x.ImplementationType == typeof(T));

	public static IServiceCollection AddInitializer<T>(this IServiceCollection serviceCollection)
		where T : class, IApplicationInitializer
		=> serviceCollection.AddTransient<IApplicationInitializer, T>();

	/// <summary>Registers initialization tasks for the specified owner without including them in the process task inventory.</summary>
	public static IServiceCollection AddInitializer<T>(this IServiceCollection serviceCollection, object serviceKey)
		where T : class, IApplicationInitializer
		=> serviceCollection.AddKeyedTransient<IApplicationInitializer, T>(serviceKey);

	public static IServiceCollection AddFinalizer<T>(this IServiceCollection serviceCollection)
		where T : class, IApplicationFinalizer
		=> serviceCollection.AddTransient<IApplicationFinalizer, T>();

	/// <summary>Registers finalization tasks for the specified owner without including them in the process task inventory.</summary>
	public static IServiceCollection AddFinalizer<T>(this IServiceCollection serviceCollection, object serviceKey)
		where T : class, IApplicationFinalizer
		=> serviceCollection.AddKeyedTransient<IApplicationFinalizer, T>(serviceKey);

	//public static void AddProxy<TSource, TDest>(this IServiceCollection serviceCollection) {
	//	var dest = serviceCollection.Where(x => x.ImplementationType == typeof(TDest)).SingleOrDefault();
	//}

}

