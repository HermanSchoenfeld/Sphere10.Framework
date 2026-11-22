// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Reads the installed Windows product and its product keys: the installed key and the key kept from before an upgrade, decoded from the
/// registry's DigitalProductId values, and the manufacturer's key from the firmware's ACPI MSDM table. Everything is read; nothing is changed.
/// </summary>
public static class WindowsProductKeys {
	private const string CurrentVersionPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
	private const string KeyDigits = "BCDFGHJKMPQRTVWXY2346789";
	// DigitalProductId holds the encoded key in bytes 52 to 66; DigitalProductId4 holds the edition and channel as UTF-16 text.
	private const int KeyOffset = 52;
	private const int KeyLength = 15;
	private const int EditionOffset = 280;
	private const int ChannelOffset = 1016;
	private const int TextLength = 256;
	// The MSDM table's 36-byte ACPI header is followed by version, reserved, data type, data reserved and data length, then the key.
	private const int FirmwareKeyOffset = 56;
	private const int FirmwareKeyLength = 29;
	private const int ErrorNotFound = 1168;

	public static WindowsProductInfo Read() => ReadCore(null);

	public static Task<WindowsProductInfo> ReadAsync(CancellationToken cancellationToken = default) => Task.Run(() => ReadCore(cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	/// <summary>Decodes the product key in a DigitalProductId value, in the five groups of five characters Windows shows.</summary>
	/// <returns>The key, or null when the value holds none: too short, or its key bytes are zero, as a volume activation leaves them.</returns>
	public static string DecodeKey(byte[] digitalProductId) {
		if (digitalProductId == null || digitalProductId.Length < KeyOffset + KeyLength)
			return null;
		var key = digitalProductId.AsSpan(KeyOffset, KeyLength).ToArray();
		if (key.All(value => value == 0))
			return null;
		// Keys since Windows 8 mark themselves with the last byte's fourth bit and carry the position of an "N" in their leading digit. The script
		// most tools copied tests (byte / 6) & 1, which misreads keys whose N is late in the key; the bit itself is tested here.
		var hasN = (key[KeyLength - 1] & 0x08) != 0;
		key[KeyLength - 1] = (byte)(key[KeyLength - 1] & 0xF7);
		var digits = new char[25];
		var last = 0;
		for (var digit = digits.Length - 1; digit >= 0; digit--) {
			var current = 0;
			for (var index = KeyLength - 1; index >= 0; index--) {
				current = current * 256 + key[index];
				key[index] = (byte)(current / 24);
				current %= 24;
			}
			digits[digit] = KeyDigits[current];
			last = current;
		}
		var text = new string(digits);
		if (hasN)
			text = text[1..].Insert(last, "N");
		return string.Join("-", Enumerable.Range(0, 5).Select(group => text.Substring(group * 5, 5)));
	}

	/// <summary>Reads the key the manufacturer wrote into the firmware's ACPI MSDM table.</summary>
	/// <returns>The key, or null when the firmware has no MSDM table.</returns>
	/// <exception cref="Win32Exception">The firmware table could not be read for another reason.</exception>
	public static string ReadFirmwareKey() {
		var size = WinAPI.KERNEL32.GetSystemFirmwareTable(WinAPI.KERNEL32.FIRMWARE_TABLE_PROVIDER_ACPI, WinAPI.KERNEL32.ACPI_TABLE_MSDM, null, 0);
		if (size == 0)
			return ReadFailed();
		var table = new byte[size];
		var read = WinAPI.KERNEL32.GetSystemFirmwareTable(WinAPI.KERNEL32.FIRMWARE_TABLE_PROVIDER_ACPI, WinAPI.KERNEL32.ACPI_TABLE_MSDM, table, size);
		if (read == 0)
			return ReadFailed();
		if (read < FirmwareKeyOffset + FirmwareKeyLength)
			return null;
		var key = Encoding.ASCII.GetString(table, FirmwareKeyOffset, FirmwareKeyLength).TrimEnd('\0', ' ');
		return key.Length == 0 ? null : key;
	}

	private static WindowsProductInfo ReadCore(Action checkpoint) {
		Debug.AssertNotMainThread();
		using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
		using var current = root.OpenSubKey(CurrentVersionPath);
		Guard.Ensure(current != null, $"The registry key HKEY_LOCAL_MACHINE\\{CurrentVersionPath} is missing.");
		checkpoint?.Invoke();
		var installed = ReadKey(current);
		checkpoint?.Invoke();
		WindowsProductKeyInfo original;
		// Windows 10 keeps the key it was upgraded from in DefaultProductKey2; earlier versions only in DefaultProductKey.
		using (var originalKey = root.OpenSubKey(CurrentVersionPath + @"\DefaultProductKey2") ?? root.OpenSubKey(CurrentVersionPath + @"\DefaultProductKey"))
			original = originalKey == null ? null : ReadKey(originalKey);
		checkpoint?.Invoke();
		var firmwareKey = ReadFirmwareKey();
		var build = current.GetValue("CurrentBuild") as string ?? current.GetValue("CurrentBuildNumber") as string;
		var revision = current.GetValue("UBR");
		return new WindowsProductInfo(current.GetValue("ProductName") as string, current.GetValue("EditionID") as string, current.GetValue("DisplayVersion") as string,
			current.GetValue("ProductId") as string, revision is int number && build != null ? $"{build}.{number}" : build, Environment.Is64BitOperatingSystem,
			installed, original, firmwareKey);
	}

	private static WindowsProductKeyInfo ReadKey(RegistryKey key) {
		var digitalProductId = key.GetValue("DigitalProductId") as byte[];
		var digitalProductId4 = key.GetValue("DigitalProductId4") as byte[];
		var productId = key.GetValue("ProductId") as string;
		if (digitalProductId == null && digitalProductId4 == null && productId == null)
			return null;
		return new WindowsProductKeyInfo(DecodeKey(digitalProductId), productId, Text(digitalProductId4, EditionOffset), Text(digitalProductId4, ChannelOffset));
	}

	private static string Text(byte[] value, int offset) {
		if (value == null || value.Length <= offset)
			return null;
		var text = Encoding.Unicode.GetString(value, offset, Math.Min(TextLength, value.Length - offset));
		var end = text.IndexOf('\0');
		text = end >= 0 ? text[..end] : text;
		return text.Length == 0 ? null : text;
	}

	private static string ReadFailed() {
		var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
		return error == ErrorNotFound ? null : throw new Win32Exception(error, "The firmware's ACPI MSDM table could not be read.");
	}
}
