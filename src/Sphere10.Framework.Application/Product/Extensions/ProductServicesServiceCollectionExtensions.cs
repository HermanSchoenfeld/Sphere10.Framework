// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sphere10.Framework.Application;

/// <summary>Selects the product-information source shared by application hosts and existing product consumers.</summary>
public static class ProductServicesServiceCollectionExtensions {
	/// <summary>Uses the supplied product-information instance and the existing operational product services.</summary>
	public static IServiceCollection AddProductServices(this IServiceCollection services, ProductInformation productInformation) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(productInformation, nameof(productInformation));

		services.Replace(ServiceDescriptor.Singleton<IProductServicesProvider>(provider => new ProductServicesProvider(productInformation, provider)));
		services.Replace(ServiceDescriptor.Singleton<IProductInformationProvider>(provider => provider.GetRequiredService<IProductServicesProvider>()));
		return services;
	}

	/// <summary>Uses assembly attributes for product information, defaulting to the application entry assembly.</summary>
	public static IServiceCollection AddAssemblyInfoProductServices(this IServiceCollection services, Assembly assembly = null) {
		Guard.ArgumentNotNull(services, nameof(services));
		var productAssembly = assembly ?? Tools.Runtime.GetEntryAssembly();

		services.Replace(ServiceDescriptor.Singleton<IProductServicesProvider>(provider => new AssemblyInfoProductServicesProvider(provider, productAssembly)));
		services.Replace(ServiceDescriptor.Singleton<IProductInformationProvider>(provider => provider.GetRequiredService<IProductServicesProvider>()));
		return services;
	}
}
