// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Net;
using Sphere10.Framework;

namespace Tools.Web;

public static partial class AspNetCore {
	public static IPNetwork ParseNetwork(string cidr) {
		Guard.ArgumentNotNullOrWhitespace(cidr, nameof(cidr));
		var parts = cidr.Split('/');
		Guard.Argument(parts.Length == 2, nameof(cidr), "Expected an IP address and prefix length separated by '/'.");
		Guard.Argument(IPAddress.TryParse(parts[0], out var address), nameof(cidr), "Invalid IP address.");
		Guard.Argument(int.TryParse(parts[1], out var prefixLength), nameof(cidr), "Invalid prefix length.");
		var networkBytes = address.GetAddressBytes();
		Guard.ArgumentInRange(prefixLength, 0, networkBytes.Length * 8, nameof(cidr));

		// Clear host bits independently in each byte for both IPv4 and IPv6.
		for (var index = 0; index < networkBytes.Length; index++) {
			var prefixBits = (prefixLength - index * 8).ClipTo(0, 8);
			networkBytes[index] &= (byte)(0xff << (8 - prefixBits));
		}
		return new IPNetwork(new IPAddress(networkBytes), prefixLength);
	}
}
