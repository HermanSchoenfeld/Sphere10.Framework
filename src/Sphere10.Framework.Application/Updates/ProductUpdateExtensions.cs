// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Application;

/// <summary>Enables background update checks for an application. Without one of these calls no check is scheduled.</summary>
public static class ProductUpdateExtensions {

	/// <summary>Registers the <see cref="ProductUpdateChecker"/> with the process tasks that start and stop it.</summary>
	public static Sphere10FrameworkBuilder UseProductUpdateChecks(this Sphere10FrameworkBuilder builder, Action<ProductUpdateCheckOptions> configure = null) {
		Guard.ArgumentNotNull(builder, nameof(builder));
		var options = new ProductUpdateCheckOptions();
		configure?.Invoke(options);
		return builder.AddServices(services => services.AddProductUpdateChecks(options));
	}

	/// <summary>
	/// Registers the <see cref="ProductUpdateChecker"/> as a singleton with its process initializer and finalizer. Repeated calls
	/// register them once, and the options of the last call apply. The checker asks the registered <see cref="ProductUpdaterBase"/>.
	/// </summary>
	public static IServiceCollection AddProductUpdateChecks(this IServiceCollection services, ProductUpdateCheckOptions options = null) {
		Guard.ArgumentNotNull(services, nameof(services));
		services.AddSingleton(options ?? new ProductUpdateCheckOptions());
		if (services.HasImplementationFor<ProductUpdateChecker>())
			return services;
		services.AddSingleton(provider => new ProductUpdateChecker(
			provider.GetRequiredService<ProductUpdaterBase>(),
			provider.GetRequiredService<ProductUpdateCheckOptions>(),
			provider.GetService<ILogger>() ?? SystemLog.Logger
		));
		services.AddInitializer<ProductUpdateCheckerInitializer>();
		services.AddFinalizer<ProductUpdateCheckerFinalizer>();
		return services;
	}
}
