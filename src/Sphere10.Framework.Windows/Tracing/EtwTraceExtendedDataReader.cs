// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

/// <summary>Copies documented EVENT_HEADER_EXTENDED_DATA_ITEM payloads while callback-owned native pointers are valid.</summary>
internal static class EtwTraceExtendedDataReader {
	internal static EtwTraceExtendedData[] Read(IntPtr items, ushort count, out int omitted) {
		const int maximumItems = 64;
		const int maximumBytes = 256 * 1024;
		omitted = 0;
		var result = new List<EtwTraceExtendedData>();
		var bytes = 0;
		if (items == IntPtr.Zero) {
			omitted = count;
			return Array.Empty<EtwTraceExtendedData>();
		}
		for (var index = 0; index < count; index++) {
			if (index >= maximumItems) { omitted += count - index; break; }
			var item = Marshal.PtrToStructure<WinAPI.ADVAPI32.EVENT_HEADER_EXTENDED_DATA_ITEM>(IntPtr.Add(items, index * 16));
			if (bytes + item.DataSize > maximumBytes || item.DataPtr == 0 && item.DataSize > 0) { omitted++; continue; }
			var data = new byte[item.DataSize];
			if (data.Length > 0)
				Marshal.Copy(unchecked((IntPtr)(long)item.DataPtr), data, 0, data.Length);
			bytes += data.Length;
			result.Add(Decode(item.ExtType, data));
		}
		return result.ToArray();
	}

	internal static EtwTraceExtendedData Decode(ushort type, byte[] data) {
		var pointerSize = type == WinAPI.ADVAPI32.EVENT_HEADER_EXT_TYPE_STACK_TRACE32 ? 4 : type == WinAPI.ADVAPI32.EVENT_HEADER_EXT_TYPE_STACK_TRACE64 ? 8 : 0;
		if (pointerSize == 0)
			return new EtwTraceExtendedData(type, data, null, Array.Empty<ulong>(), "Raw Extended Header Payload");
		if (data.Length < 8 || (data.Length - 8) % pointerSize != 0)
			return new EtwTraceExtendedData(type, data, null, Array.Empty<ulong>(), "Unsupported Stack Payload Length; Raw Bytes Retained");
		var frames = new ulong[(data.Length - 8) / pointerSize];
		for (var index = 0; index < frames.Length; index++) {
			var address = data.AsSpan(8 + index * pointerSize, pointerSize);
			frames[index] = pointerSize == 4 ? EndianBitConverter.Little.ToUInt32(address) : EndianBitConverter.Little.ToUInt64(address);
		}
		return new EtwTraceExtendedData(type, data, EndianBitConverter.Little.ToUInt64(data), frames, "Captured Writer Stack Addresses; Symbols Not Implied");
	}
}
