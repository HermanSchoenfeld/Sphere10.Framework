// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Files;

/// <summary>Detached PE structure. Addresses are RVAs unless the entry explicitly identifies another address space.</summary>
public sealed class PortableExecutableInfo {

	public string FilePath { get; init; }
	public long FileLength { get; init; }
	public DateTime LastWriteUtc { get; init; }
	public string Machine { get; init; }
	public bool IsPe32Plus { get; init; }
	public ulong ImageBase { get; init; }
	public uint EntryPoint { get; init; }
	public uint HeaderSize { get; init; }
	public uint ImageSize { get; init; }
	public bool HasMetadata { get; init; }
	public PortableExecutableSection[] Sections { get; init; } = Array.Empty<PortableExecutableSection>();
	public PortableExecutableEntry[] Entries { get; init; } = Array.Empty<PortableExecutableEntry>();
	public string[] Issues { get; init; } = Array.Empty<string>();

	/// <summary>Maps only backed bytes; zero-filled virtual tails and ambiguous overlapping sections have no file mapping.</summary>
	public bool TryGetFileOffset(uint relativeVirtualAddress, int count, out long offset) {
		offset = 0;
		if (count < 0)
			return false;
		var end = (ulong)relativeVirtualAddress + (uint)count;
		var matches = end <= HeaderSize && end <= (ulong)FileLength ? 1 : 0;
		if (matches == 1)
			offset = relativeVirtualAddress;
		foreach (var section in Sections) {
			if (relativeVirtualAddress < section.RelativeVirtualAddress || end > (ulong)section.RelativeVirtualAddress + section.RawSize)
				continue;
			var candidate = (ulong)section.FileOffset + relativeVirtualAddress - section.RelativeVirtualAddress;
			if (candidate + (uint)count > (ulong)FileLength)
				continue;
			offset = (long)candidate;
			matches++;
		}
		return matches == 1;
	}

	public bool TryGetRelativeVirtualAddress(long offset, int count, out uint relativeVirtualAddress) {
		relativeVirtualAddress = 0;
		if (offset < 0 || count < 0 || offset > FileLength - count)
			return false;
		var matches = (ulong)offset + (uint)count <= HeaderSize ? 1 : 0;
		if (matches == 1)
			relativeVirtualAddress = (uint)offset;
		foreach (var section in Sections) {
			if (offset < section.FileOffset || (ulong)offset + (uint)count > (ulong)section.FileOffset + section.RawSize)
				continue;
			var candidate = (ulong)section.RelativeVirtualAddress + (ulong)offset - section.FileOffset;
			if (candidate > uint.MaxValue)
				continue;
			relativeVirtualAddress = (uint)candidate;
			matches++;
		}
		return matches == 1;
	}
}
