// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

/// <summary>Caches manifest layouts and walks flat properties once. Unsupported layouts retain raw event data without guessing field offsets.</summary>
internal sealed class EtwTracePropertyDecoder {
	private readonly Dictionary<(Guid, WinAPI.ADVAPI32.EVENT_DESCRIPTOR), Property[]> _schemas = new();

	public EtwTraceProperty[] Decode(IntPtr record, WinAPI.ADVAPI32.EVENT_HEADER header, byte[] data) {
		var key = (header.ProviderId, header.EventDescriptor);
		if (!_schemas.TryGetValue(key, out var schema)) {
			schema = ReadSchema(record);
			if (_schemas.Count < 512)
				_schemas.Add(key, schema);
		}
		return Decode(schema, data, (header.Flags & 0x20) != 0 ? 4 : 8);
	}

	internal static EtwTraceProperty[] Decode(Property[] schema, byte[] data, int pointerSize) {
		var result = new EtwTraceProperty[schema.Length];
		var integers = new ulong?[schema.Length];
		var offset = 0;
		for (var index = 0; index < schema.Length; index++) {
			var property = schema[index];
			if ((property.Flags & (1 | 128)) != 0)
				return Array.Empty<EtwTraceProperty>();
			var count = Resolve(property.Count, (property.Flags & 4) != 0, integers, index);
			var length = Resolve(property.Length, (property.Flags & 2) != 0, integers, index);
			if (count < 0 || count > ushort.MaxValue || length < 0 || length > ushort.MaxValue)
				return Array.Empty<EtwTraceProperty>();
			var start = offset;
			if (property.InType == 14 && length == 0 && (property.Flags & (2 | 16)) == 0)
				return Array.Empty<EtwTraceProperty>();
			for (var item = 0; item < count; item++) {
				var size = GetSize(property.InType, length, (property.Flags & (2 | 16)) != 0, data.AsSpan(offset), pointerSize);
				if (size < 0 || size > data.Length - offset)
					return Array.Empty<EtwTraceProperty>();
				offset += size;
			}
			var value = data.AsSpan(start, offset - start);
			result[index] = new EtwTraceProperty(property.Name, value.ToArray());
			if (count == 1)
				integers[index] = ReadInteger(property.InType, value);
		}
		return result;
	}

	private static long Resolve(ushort value, bool reference, ulong?[] integers, int index) {
		if (!reference)
			return value;
		return value < index && integers[value] is { } integer && integer <= ushort.MaxValue ? (long)integer : -1;
	}

	private static int GetSize(ushort type, long length, bool explicitLength, ReadOnlySpan<byte> remaining, int pointerSize) {
		if (type is 1 or 2) {
			var width = type == 1 ? 2 : 1;
			if (length > 0 || explicitLength)
				return checked((int)length * width);
			for (var index = 0; index + width <= remaining.Length; index += width)
				if (remaining[index] == 0 && (width == 1 || remaining[index + 1] == 0))
					return index + width;
			return -1;
		}
		return type switch {
			3 or 4 => 1,
			5 or 6 => 2,
			7 or 8 or 11 or 13 or 20 => 4,
			9 or 10 or 12 or 17 or 21 => 8,
			14 => (int)length,
			15 or 18 => 16,
			16 or 30 => pointerSize,
			19 => remaining.Length >= 8 ? 8 + remaining[1] * 4 : -1,
			_ => -1
		};
	}

	private static ulong? ReadInteger(ushort type, ReadOnlySpan<byte> value) => type switch {
		3 or 4 when value.Length == 1 => value[0],
		5 or 6 when value.Length == 2 => BinaryPrimitives.ReadUInt16LittleEndian(value),
		7 or 8 or 20 when value.Length == 4 => BinaryPrimitives.ReadUInt32LittleEndian(value),
		9 or 10 or 21 when value.Length == 8 => BinaryPrimitives.ReadUInt64LittleEndian(value),
		_ => null
	};

	private static Property[] ReadSchema(IntPtr record) {
		uint size = 0;
		if (WinAPI.TDH.TdhGetEventInformation(record, 0, IntPtr.Zero, IntPtr.Zero, ref size) != 122 || size is < 112 or > 1048576)
			return Array.Empty<Property>();
		using (var allocation = new UnmanagedMemoryScope((int)size)) {
			var buffer = allocation.Item;
			if (WinAPI.TDH.TdhGetEventInformation(record, 0, IntPtr.Zero, buffer, ref size) != 0)
				return Array.Empty<Property>();
			var info = Marshal.PtrToStructure<WinAPI.TDH.TRACE_EVENT_INFO>(buffer);
			var headerSize = Marshal.SizeOf<WinAPI.TDH.TRACE_EVENT_INFO>();
			var propertySize = Marshal.SizeOf<WinAPI.TDH.EVENT_PROPERTY_INFO>();
			// TraceLogging schemas may vary for a descriptor; only manifest schemas are cached.
			if (info.DecodingSource != 0 || info.PropertyCount > 1024 || info.TopLevelPropertyCount > info.PropertyCount
				|| headerSize + (long)info.PropertyCount * propertySize > size)
				return Array.Empty<Property>();
			var result = new Property[info.TopLevelPropertyCount];
			for (var index = 0; index < result.Length; index++) {
				var property = Marshal.PtrToStructure<WinAPI.TDH.EVENT_PROPERTY_INFO>(IntPtr.Add(buffer, headerSize + index * propertySize));
				var name = ReadName(buffer, property.NameOffset, size);
				if (name == null)
					return Array.Empty<Property>();
				result[index] = new Property(name, property.Flags, property.InType, property.Count, property.Length);
			}
			return result;
		}
	}

	private static string ReadName(IntPtr buffer, uint offset, uint size) {
		if (offset == 0 || offset % 2 != 0 || offset >= size)
			return null;
		for (var index = offset; index + 2 <= size; index += 2)
			if (Marshal.ReadInt16(buffer, (int)index) == 0)
				return Marshal.PtrToStringUni(IntPtr.Add(buffer, (int)offset), (int)((index - offset) / 2));
		return null;
	}

	internal sealed record Property(string Name, uint Flags, ushort InType, ushort Count, ushort Length);
}
