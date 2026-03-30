// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Net.Http.Headers;
using Sphere10.Framework.Application;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceProviderNamedRegistrationsExtensions {

	public static T GetNamedService<T>(this IServiceProvider servicerProvider, string name) where T : class {
		var namedLookup = servicerProvider.GetService<INamedLookup<T>>();
		if (namedLookup != null && namedLookup.ContainsKey(name))
			return namedLookup[name];
		return default;
	}

	public static T GetRequiredNamedService<T>(this IServiceProvider servicerProvider, string name) where T : class {
		var namedLookup = servicerProvider.GetRequiredService<INamedLookup<T>>();
		return namedLookup[name];
	}

	public static bool HasNamedService<T>(this IServiceProvider servicerProvider, string name) where T : class 
		=> servicerProvider.GetService<INamedLookup<T>>()?.ContainsKey(name) ?? false;

}

