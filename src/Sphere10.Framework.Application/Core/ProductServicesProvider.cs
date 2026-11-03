// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Application;

/// <summary>Shares selected product information and delegates usage and instance operations to their registered services.</summary>
public class ProductServicesProvider : IProductServicesProvider {
	private readonly IFuture<ProductInformation> _productInformation;

	private readonly IServiceProvider _services;

	/// <summary>Retains the supplied information object without activating operational product services.</summary>
	public ProductServicesProvider(ProductInformation productInformation, IServiceProvider services) {
		Guard.ArgumentNotNull(productInformation, nameof(productInformation));
		Guard.ArgumentNotNull(services, nameof(services));
		_productInformation = Tools.Values.Future.Explicit(productInformation);
		_services = services;
	}

	/// <summary>Retains an existing information provider and reads it only when product information is requested.</summary>
	public ProductServicesProvider(IProductInformationProvider productInformationProvider, IServiceProvider services) {
		Guard.ArgumentNotNull(productInformationProvider, nameof(productInformationProvider));
		Guard.ArgumentNotNull(services, nameof(services));
		_productInformation = Tools.Values.Future.LazyLoad(() => productInformationProvider.ProductInformation);
		_services = services;
	}

	/// <inheritdoc />
	public ProductInformation ProductInformation => _productInformation.Value;

	/// <inheritdoc />
	public ProductUsageInformation ProductUsageInformation => UsageServices.ProductUsageInformation;

	/// <inheritdoc />
	public IDictionary<string, object> UserEncryptedProperties => UsageServices.UserEncryptedProperties;

	/// <inheritdoc />
	public IDictionary<string, object> SystemEncryptedProperties => UsageServices.SystemEncryptedProperties;

	// Resolve the existing usage service only when the caller requests its state or operations.
	private IProductUsageServices UsageServices => _services.GetRequiredService<IProductUsageServices>();

	/// <inheritdoc />
	public void IncrementUsageByOne() {
		UsageServices.IncrementUsageByOne();
	}

	/// <inheritdoc />
	public int CountNumberOfRunningInstances() {
		return _services.GetRequiredService<IProductInstancesCounter>().CountNumberOfRunningInstances();
	}
}
