// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Runtime.InteropServices;

namespace Sphere10.Framework.Application;

internal class AssemblyAttributeConfiguredProductLicenseClient : RestProductLicenseClient {
	public AssemblyAttributeConfiguredProductLicenseClient()
		: base(GetDrmBaseUrl()) {
	}

	private static string GetDrmBaseUrl() {
		var url = Sphere10AssemblyAttributesHelper.GetProductDrmApi();
		if (string.IsNullOrEmpty(url))
			throw new InvalidOleVariantTypeException($"Application is missing assembly-wide {nameof(AssemblyProductDrmApi)} attribute");
		return url;
	}
}

