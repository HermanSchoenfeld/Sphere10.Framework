// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application;

public class AssemblyAttributesProductInformationProvider : IProductInformationProvider {
	private ProductInformation _productInformation;

	public AssemblyAttributesProductInformationProvider() {
		_productInformation = null;
	}

	public ProductInformation ProductInformation {
		get {
			if (_productInformation != null)
				return _productInformation;
			lock (this) {
				_productInformation ??= GetProductInformation();
			}
			return _productInformation;
		}
	}

	protected ProductInformation GetProductInformation() {

		var version = ApplicationVersion.Parse(Sphere10AssemblyAttributesHelper.GetAssemblyVersion());
		version.Distribution = Sphere10AssemblyAttributesHelper.GetAssemblyProductDistribution() ?? ProductDistribution.ReleaseCandidate;

		return new ProductInformation {
			CompanyName = Sphere10AssemblyAttributesHelper.GetAssemblyCompany() ?? string.Empty,
			CompanyNumber = Sphere10AssemblyAttributesHelper.GetAssemblyCompanyNumber() ?? string.Empty,
			CompanyUrl = Sphere10AssemblyAttributesHelper.GetAssemblyCompanyLink() ?? string.Empty,
			CopyrightNotice = Sphere10AssemblyAttributesHelper.GetAssemblyCopyright() ?? string.Empty,
			DefaultProductLicense = Sphere10AssemblyAttributesHelper.GetAssemblyDefaultProductLicenseActivation(),
			ProductDrmApiUrl = Sphere10AssemblyAttributesHelper.GetProductDrmApi() ?? string.Empty,
			ProductCode = Sphere10AssemblyAttributesHelper.GetAssemblyProductCode() ?? Guid.Empty,
			ProductDescription = Sphere10AssemblyAttributesHelper.GetAssemblyDescription() ?? string.Empty,
			ProductName = Sphere10AssemblyAttributesHelper.GetAssemblyProduct() ?? string.Empty,
			ProductPurchaseUrl = Sphere10AssemblyAttributesHelper.GetAssemblyProductPurchaseLink() ?? string.Empty,
			ProductUrl = Sphere10AssemblyAttributesHelper.GetAssemblyProductLink() ?? string.Empty,
			AuthorName = Sphere10AssemblyAttributesHelper.GetAssemblyAuthorName() ?? string.Empty,
			AuthorEmail = Sphere10AssemblyAttributesHelper.GetAssemblyAuthorEmail() ?? string.Empty,
			ProductVersion = version,
			HelpResources = Sphere10AssemblyAttributesHelper.GetAssemblyProductHelpResources()
		};
	}

}

