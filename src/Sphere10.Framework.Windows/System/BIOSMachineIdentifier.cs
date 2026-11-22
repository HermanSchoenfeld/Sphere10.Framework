// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Identifies a Windows machine by its SMBIOS system UUID (<c>Win32_ComputerSystemProduct.UUID</c>): the SHA-256 of the
/// normalised UUID as 64 upper-case hexadecimal digits. Register it in place of <see cref="WindowsMachineIdentifierGenerator"/>
/// when the firmware UUID alone should identify the machine.
/// </summary>
/// <remarks>
/// The UUID is composed and hashed exactly as the generator's sources are, under the name <c>SystemUuid</c>. Firmware that
/// reports no UUID, a placeholder or an all-zero or all-F UUID yields no value, and <see cref="Generate"/> then throws
/// <see cref="InvalidOperationException"/>. The first successful <see cref="Generate"/> caches the identifier for the life of
/// the instance; a failed WMI read is thrown and not cached.
/// </remarks>
public sealed class BIOSMachineIdentifier : IMachineIdentifierGenerator {
	private const string ComponentName = "SystemUuid";
	private readonly object _lock = new();
	private string _identifier;

	/// <inheritdoc />
	public string Generate() {
		// The firmware UUID does not change while the process runs, so WMI is read once per instance.
		lock (_lock)
			return _identifier ??= ComputeIdentifier(Tools.Windows.Computer.GetSystemProducts());
	}

	/// <summary>Hashes the system products' UUIDs into the identifier.</summary>
	/// <exception cref="InvalidOperationException">No product reports a meaningful UUID.</exception>
	internal static string ComputeIdentifier(IEnumerable<WindowsSystemProductInfo> products)
		=> MachineIdentity.ComputeIdentifier(products.Select(product => new MachineIdentity.Component(ComponentName, product.Uuid)));

}
