// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;

namespace Sphere10.Framework.Windows;

/// <summary>Detached decoding of the documented SampledProfile/StackWalk classes. It configures no sampler and infers no process creation identity.</summary>
public static class EtwCpuProfileDecoder {
	public static readonly Guid ControlProviderId = new("bfeb0324-1cee-496f-a409-2ac2b48a6322");
	public static readonly Guid SampleProviderId = new("ce1dbfb4-137e-4da6-87b0-3f59aa102cbc");
	public static readonly Guid StackProviderId = new("def2fe46-7bd6-4b80-bd94-f57fe20d0ce3");
	public const ulong ProfileKeyword = 1;
	public const byte SampleType = 46;
	public const byte StackType = 32;

	/// <summary>True identifies the known event class/type, including an unavailable or malformed known payload described by the result.</summary>
	public static bool TryReadSample(EtwTraceEventRecord record, out EtwCpuProfileSample sample) {
		Guard.ArgumentNotNull(record, nameof(record));
		sample = null;
		if (record.ProviderId != SampleProviderId || record.Opcode != SampleType)
			return false;
		var pointer = record.Properties?.FirstOrDefault(property => property.Name == "InstructionPointer");
		var thread = record.Properties?.FirstOrDefault(property => property.Name == "ThreadId");
		if (pointer != null && thread != null && pointer.TryGetUInt64(out var instruction) && thread.TryGetUInt64(out var id) && id <= uint.MaxValue) {
			sample = new EtwCpuProfileSample(instruction, (uint)id, string.Empty);
			return true;
		}
		var width = PointerWidth(record.HeaderFlags);
		if (record.Version > 2 || width == 0 || record.Data == null || record.Data.Length < width + 4) {
			sample = new EtwCpuProfileSample(null, null, "SampledProfile raw layout/version/pointer width is unavailable; original bytes are retained. Header writer PID/TID is not the sampled actor.");
			return true;
		}
		var address = width == 8 ? EndianBitConverter.Little.ToUInt64(record.Data) : EndianBitConverter.Little.ToUInt32(record.Data);
		sample = new EtwCpuProfileSample(address, EndianBitConverter.Little.ToUInt32(record.Data, width), string.Empty);
		return true;
	}

	/// <summary>Reads only documented classic StackWalk type 32. OriginalEventTimestamp is not converted using an assumed QPC/FILETIME clock.</summary>
	public static bool TryReadStack(EtwTraceEventRecord record, out EtwCpuProfileStack stack) {
		Guard.ArgumentNotNull(record, nameof(record));
		stack = null;
		if (record.ProviderId != StackProviderId || record.Opcode != StackType)
			return false;
		var width = PointerWidth(record.HeaderFlags);
		if (record.Version > 2 || width == 0 || record.Data == null || record.Data.Length < 16 || (record.Data.Length - 16) % width != 0) {
			stack = new EtwCpuProfileStack(null, null, null, Array.Empty<ulong>(), "StackWalk raw layout/version/pointer width is unavailable; original bytes are retained.");
			return true;
		}
		var count = Math.Min(192, (record.Data.Length - 16) / width);
		var addresses = new ulong[count];
		for (var index = 0; index < count; index++)
			addresses[index] = width == 8 ? EndianBitConverter.Little.ToUInt64(record.Data, 16 + index * width) : EndianBitConverter.Little.ToUInt32(record.Data, 16 + index * width);
		stack = new EtwCpuProfileStack(EndianBitConverter.Little.ToUInt64(record.Data), EndianBitConverter.Little.ToUInt32(record.Data, 8),
			EndianBitConverter.Little.ToUInt32(record.Data, 12), addresses, (record.Data.Length - 16) / width > 192 ? "Captured stack addresses exceed the documented 192-frame bound; remaining bytes remain raw." : string.Empty);
		return true;
	}

	private static int PointerWidth(ushort flags) => (flags & 0x60) switch { 0x20 => 4, 0x40 => 8, _ => 0 };
}
