// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	public static partial class KERNEL32 {
		/// <summary>The ACPI firmware table provider, 'ACPI' as Windows spells provider signatures.</summary>
		public const uint FIRMWARE_TABLE_PROVIDER_ACPI = 0x41435049;
		/// <summary>The ACPI Microsoft Data Management table, which holds a Windows product key written into the firmware by the manufacturer.</summary>
		public const uint ACPI_TABLE_MSDM = 0x4D44534D;

		/// <summary>Copies a firmware table into the buffer and returns its size; a buffer too small (or null with size 0) returns the size needed
		/// without copying, and 0 is a failure, such as ERROR_NOT_FOUND for a table the firmware does not have.</summary>
		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern uint GetSystemFirmwareTable(uint firmwareTableProviderSignature, uint firmwareTableId, [Out] byte[] firmwareTableBuffer, uint bufferSize);
	}
}
