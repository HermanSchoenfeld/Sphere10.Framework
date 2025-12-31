// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Newtonsoft.Json;

namespace Sphere10.Framework.Application;

public class ProductLicenseActivationDTO {

	[JsonProperty("authority")] public ProductLicenseAuthorityDTO Authority { get; set; }

	[JsonProperty("license")] public SignedItem<ProductLicenseDTO> License { get; set; }

	[JsonProperty("command")] public SignedItem<ProductLicenseCommandDTO> Command { get; set; }

}

