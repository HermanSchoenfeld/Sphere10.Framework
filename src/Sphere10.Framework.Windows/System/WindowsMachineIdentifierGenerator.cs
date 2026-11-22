// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Computes the Windows machine identifier from the hardware sources a <see cref="MachineIdentitySources"/> selection names:
/// the SHA-256 of the sources' normalised values, as 64 upper-case hexadecimal digits.
/// </summary>
/// <remarks>
/// Each value is trimmed and upper-cased; empty values and firmware placeholders (such as "To be filled by O.E.M.",
/// "Default string" and all-zero or all-F identifiers) are dropped. The remaining values are written as one
/// <c>Source=Value</c> line each, ordered by ordinal comparison so that WMI enumeration order never changes the result,
/// and hashed through <see cref="Hashers"/> with <see cref="CHF.SHA2_256"/>. The first successful <see cref="Generate"/>
/// caches the identifier for the life of the instance; a failed WMI read is thrown and not cached.
/// <see cref="BIOSMachineIdentifier"/> is the alternative that identifies a machine by its SMBIOS system UUID alone.
/// </remarks>
public sealed class WindowsMachineIdentifierGenerator : IMachineIdentifierGenerator {
	private const MachineIdentitySources AllSources = MachineIdentitySources.BaseBoard | MachineIdentitySources.Processor | MachineIdentitySources.Bios |
		MachineIdentitySources.SystemDisk | MachineIdentitySources.PhysicalNetworkAdapters;
	private const string PhysicalAdaptersQuery = "SELECT MACAddress,PNPDeviceID FROM Win32_NetworkAdapter WHERE PhysicalAdapter=TRUE";
	private readonly object _lock = new();
	private string _identifier;

	/// <summary>Creates a generator over the selected sources; <see cref="MachineIdentitySources.Default"/> unless another selection is given.</summary>
	/// <exception cref="ArgumentException">The selection is empty or names an undefined source.</exception>
	public WindowsMachineIdentifierGenerator(MachineIdentitySources sources = MachineIdentitySources.Default) {
		Guard.Argument(sources != MachineIdentitySources.None && (sources & ~AllSources) == 0, nameof(sources), "Select at least one defined machine identity source.");
		Sources = sources;
	}

	/// <summary>The hardware sources this generator combines.</summary>
	public MachineIdentitySources Sources { get; }

	/// <inheritdoc />
	public string Generate() {
		// Hardware identity does not change while the process runs, so WMI is read once per instance.
		lock (_lock)
			return _identifier ??= MachineIdentity.ComputeIdentifier(ReadComponents(Sources));
	}

	/// <summary>Reads the raw values of the selected sources, before normalisation and placeholder filtering.</summary>
	internal static MachineIdentity.Component[] ReadComponents(MachineIdentitySources sources) {
		var components = new List<MachineIdentity.Component>();
		if (sources.HasFlag(MachineIdentitySources.BaseBoard))
			components.AddRange(Tools.Windows.Computer.GetBaseboards().SelectMany(board => new[] {
				new MachineIdentity.Component(nameof(MachineIdentitySources.BaseBoard) + ".Manufacturer", board.Manufacturer),
				new MachineIdentity.Component(nameof(MachineIdentitySources.BaseBoard) + ".Product", board.Product),
				new MachineIdentity.Component(nameof(MachineIdentitySources.BaseBoard) + ".SerialNumber", board.SerialNumber)
			}));
		if (sources.HasFlag(MachineIdentitySources.Processor))
			components.AddRange(Tools.Windows.Computer.GetProcessors().Select(processor => new MachineIdentity.Component(nameof(MachineIdentitySources.Processor), processor.ProcessorId)));
		if (sources.HasFlag(MachineIdentitySources.Bios))
			components.AddRange(Tools.Windows.Computer.GetBios().Select(bios => new MachineIdentity.Component(nameof(MachineIdentitySources.Bios), bios.SerialNumber)));
		if (sources.HasFlag(MachineIdentitySources.SystemDisk))
			components.AddRange(ReadSystemDisks());
		if (sources.HasFlag(MachineIdentitySources.PhysicalNetworkAdapters))
			components.AddRange(ReadPhysicalNetworkAdapters());
		return components.ToArray();
	}

	/// <summary>
	/// Whether a PnP device ID names a device installed on the machine's PCI or PCI Express bus. USB adapters are removable, and virtual
	/// and software adapters (Hyper-V, VPN, Bluetooth PAN, network bridges) enumerate under ROOT, SWD, VMBUS, BTH and similar.
	/// </summary>
	internal static bool IsInstalledPciDevice(string pnpDeviceId) => pnpDeviceId != null && pnpDeviceId.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase);

	private static MachineIdentity.Component[] ReadSystemDisks() {
		// The system disk is the physical disk with the partition that holds the Windows system drive.
		var systemRoot = Path.GetPathRoot(Environment.SystemDirectory);
		var diskIndexes = Tools.Windows.Drives.GetPartitions()
			.Where(partition => partition.ReadError == null && partition.DiskIndex.HasValue && partition.DriveRoots.Contains(systemRoot, StringComparer.OrdinalIgnoreCase))
			.Select(partition => partition.DiskIndex.Value)
			.ToHashSet();
		return Tools.Windows.Drives.GetPhysicalDisks()
			.Where(disk => disk.ReadError == null && disk.Index.HasValue && diskIndexes.Contains(disk.Index.Value))
			.Select(disk => new MachineIdentity.Component(nameof(MachineIdentitySources.SystemDisk), disk.SerialNumber))
			.ToArray();
	}

	private static MachineIdentity.Component[] ReadPhysicalNetworkAdapters() {
		// PhysicalAdapter is also true for some VPN, virtual switch and USB adapters, so only adapters installed on the PCI bus count.
		var components = new List<MachineIdentity.Component>();
		using var query = Tools.Windows.COM.CreateManagementQuery(TimeSpan.FromSeconds(5));
		foreach (var adapter in query.Read(PhysicalAdaptersQuery)) {
			using (adapter) {
				if (IsInstalledPciDevice(Convert.ToString(adapter["PNPDeviceID"], CultureInfo.InvariantCulture)))
					components.Add(new MachineIdentity.Component(nameof(MachineIdentitySources.PhysicalNetworkAdapters), Convert.ToString(adapter["MACAddress"], CultureInfo.InvariantCulture)));
			}
		}
		return components.ToArray();
	}

}
