// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>
/// The hardware values <see cref="WindowsMachineIdentifierGenerator"/> combines into a machine identifier. Every source is
/// read from firmware or devices installed in the machine through WMI; removable, virtual and software identities are never used.
/// </summary>
/// <remarks>
/// Adding a source to a selection changes the identifier, so a service compares only identifiers generated with the same
/// sources. The SMBIOS system UUID is not a source: <see cref="BIOSMachineIdentifier"/> identifies a machine by it alone.
/// </remarks>
[Flags]
public enum MachineIdentitySources {

	/// <summary>No source; a generator rejects it.</summary>
	None = 0,

	/// <summary>The baseboard manufacturer, product and serial number (<c>Win32_BaseBoard</c>).</summary>
	BaseBoard = 1 << 0,

	/// <summary>Each processor's CPUID-derived <c>Win32_Processor.ProcessorId</c>.</summary>
	Processor = 1 << 1,

	/// <summary>The BIOS serial number (<c>Win32_BIOS.SerialNumber</c>).</summary>
	Bios = 1 << 2,

	/// <summary>The serial number of the physical disk that holds the Windows system drive (<c>Win32_DiskDrive.SerialNumber</c>).</summary>
	SystemDisk = 1 << 3,

	/// <summary>The MAC addresses of network adapters installed on the machine's PCI or PCI Express bus (<c>Win32_NetworkAdapter</c> with <c>PhysicalAdapter</c> true); USB, virtual and software adapters are excluded.</summary>
	PhysicalNetworkAdapters = 1 << 4,

	/// <summary>The sources that stay fixed for the life of a machine: baseboard, processor and BIOS.</summary>
	Default = BaseBoard | Processor | Bios

}
