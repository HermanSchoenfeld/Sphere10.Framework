// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Sphere10.Framework.Windows.Files;

/// <summary>Bounded structural parsing over the framework's in-box metadata reader; it never loads or executes the inspected image.</summary>
internal sealed class PortableExecutableReader {
	private const int MaximumEntries = 65536;
	private const int MaximumRetainedCharacters = 8 * 1024 * 1024;
	private readonly Dictionary<StringHandle, string> _metadataStrings = new();
	private int _retainedCharacters;
	private bool _textTruncated;
	private readonly Stream _stream;
	private readonly Action _checkpoint;
	private readonly List<PortableExecutableEntry> _entries = new();
	private readonly List<string> _issues = new();
	private PortableExecutableInfo _image;

	private PortableExecutableReader(Stream stream, Action checkpoint) {
		_stream = stream;
		_checkpoint = checkpoint;
	}

	private bool CanAddEntries => _entries.Count < MaximumEntries && _retainedCharacters < MaximumRetainedCharacters;

	public static PortableExecutableInfo Read(string path, Action checkpoint) {
		Debug.AssertNotMainThread();
		Guard.ArgumentNotNullOrEmpty(path, nameof(path));
		checkpoint?.Invoke();
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		return new PortableExecutableReader(stream, checkpoint).ReadImage(Path.GetFullPath(path));
	}

	private PortableExecutableInfo ReadImage(string path) {
		using var reader = new PEReader(_stream, PEStreamOptions.LeaveOpen);
		var headers = reader.PEHeaders;
		var header = headers.PEHeader;
		if (header == null)
			throw new BadImageFormatException("This file has no PE optional header.");
		_image = new PortableExecutableInfo {
			FilePath = path, FileLength = _stream.Length, LastWriteUtc = File.GetLastWriteTimeUtc(path), Machine = headers.CoffHeader.Machine.ToString(), IsPe32Plus = header.Magic == PEMagic.PE32Plus,
			ImageBase = header.ImageBase, EntryPoint = (uint)header.AddressOfEntryPoint, HeaderSize = (uint)header.SizeOfHeaders,
			ImageSize = (uint)header.SizeOfImage, HasMetadata = reader.HasMetadata,
			Sections = headers.SectionHeaders.Select(section => new PortableExecutableSection(section.Name, (uint)section.VirtualAddress,
				(uint)section.VirtualSize, (uint)section.PointerToRawData, (uint)section.SizeOfRawData, (uint)section.SectionCharacteristics)).ToArray()
		};
		Add("Header", "Machine", null, headers.CoffHeaderStartOffset, 2, headers.CoffHeader.Machine.ToString());
		Add("Header", "Characteristics", null, headers.CoffHeaderStartOffset + 18, 2, headers.CoffHeader.Characteristics.ToString());
		Add("Header", "ImageBase", null, null, 0, Hex(header.ImageBase));
		Add("Header", "EntryPoint", (uint)header.AddressOfEntryPoint, Map((uint)header.AddressOfEntryPoint), 0, Hex((uint)header.AddressOfEntryPoint));
		Add("Header", "Subsystem", null, null, 0, header.Subsystem.ToString());
		Add("Header", "DllCharacteristics", null, null, 0, header.DllCharacteristics.ToString());
		Add("Header", "ImageSize", null, null, 0, header.SizeOfImage.ToString(CultureInfo.InvariantCulture));
		Add("Header", "SectionAlignment", null, null, 0, header.SectionAlignment.ToString(CultureInfo.InvariantCulture));
		Add("Header", "FileAlignment", null, null, 0, header.FileAlignment.ToString(CultureInfo.InvariantCulture));
		Add("Header", "LinkerVersion", null, null, 0, header.MajorLinkerVersion + "." + header.MinorLinkerVersion);
		Add("Header", "OperatingSystemVersion", null, null, 0, header.MajorOperatingSystemVersion + "." + header.MinorOperatingSystemVersion);
		Add("Header", "ImageVersion", null, null, 0, header.MajorImageVersion + "." + header.MinorImageVersion);
		Add("Header", "SubsystemVersion", null, null, 0, header.MajorSubsystemVersion + "." + header.MinorSubsystemVersion);
		Add("Header", "StackReserve", null, null, 0, header.SizeOfStackReserve.ToString(CultureInfo.InvariantCulture));
		Add("Header", "StackCommit", null, null, 0, header.SizeOfStackCommit.ToString(CultureInfo.InvariantCulture));
		Add("Header", "HeapReserve", null, null, 0, header.SizeOfHeapReserve.ToString(CultureInfo.InvariantCulture));
		Add("Header", "HeapCommit", null, null, 0, header.SizeOfHeapCommit.ToString(CultureInfo.InvariantCulture));
		Add("Header", "Checksum", null, null, 0, Hex(header.CheckSum));
		Add("Header", "TimeDateStamp", null, headers.CoffHeaderStartOffset + 4, 4, unchecked((uint)headers.CoffHeader.TimeDateStamp).ToString(CultureInfo.InvariantCulture));
		foreach (var section in _image.Sections)
			Add("Section", section.Name, section.RelativeVirtualAddress, section.FileOffset, section.RawSize, $"VirtualSize={section.VirtualSize}; Characteristics={Hex(section.Characteristics)}");

		// The certificate table is the PE exception: its directory address is a file offset, not an RVA.
		Directory("Export", header.ExportTableDirectory);
		Directory("Import", header.ImportTableDirectory);
		Directory("Resource", header.ResourceTableDirectory);
		Directory("Exception", header.ExceptionTableDirectory);
		Directory("Certificate", header.CertificateTableDirectory, true);
		Directory("Relocation", header.BaseRelocationTableDirectory);
		Directory("Debug", header.DebugTableDirectory);
		Directory("Architecture", header.CopyrightTableDirectory);
		Directory("GlobalPointer", header.GlobalPointerTableDirectory);
		Directory("TLS", header.ThreadLocalStorageTableDirectory);
		Directory("LoadConfiguration", header.LoadConfigTableDirectory);
		Directory("BoundImport", header.BoundImportTableDirectory);
		Directory("ImportAddress", header.ImportAddressTableDirectory);
		Directory("DelayImport", header.DelayImportTableDirectory);
		Directory("CLR", header.CorHeaderTableDirectory);
		Parse("Import", () => ReadImports(header.ImportTableDirectory));
		Parse("Export", () => ReadExports(header.ExportTableDirectory));
		Parse("Resource", () => ReadResources(header.ResourceTableDirectory));
		Parse("Relocation", () => ReadRelocations(header.BaseRelocationTableDirectory));
		Parse("Exception", () => ReadRuntimeFunctions(header.ExceptionTableDirectory));
		Parse("TLS", () => ReadTls(header.ThreadLocalStorageTableDirectory));
		Parse("LoadConfiguration", () => ReadLoadConfiguration(header.LoadConfigTableDirectory));
		Parse("Debug", () => ReadDebug(reader));
		Parse("CLR", () => ReadMetadata(reader));
		if (!CanAddEntries)
			_issues.Add("The structural entry or aggregate text limit was reached; remaining entries were omitted.");
		if (_textTruncated)
			_issues.Add("Some structural names or values were truncated to their text preview bound; raw ranges remain available.");
		return new PortableExecutableInfo {
			FilePath = _image.FilePath, FileLength = _image.FileLength, LastWriteUtc = _image.LastWriteUtc, Machine = _image.Machine, IsPe32Plus = _image.IsPe32Plus,
			ImageBase = _image.ImageBase, EntryPoint = _image.EntryPoint, HeaderSize = _image.HeaderSize, ImageSize = _image.ImageSize,
			HasMetadata = _image.HasMetadata, Sections = _image.Sections, Entries = _entries.ToArray(), Issues = _issues.ToArray()
		};
	}

	private void Directory(string name, DirectoryEntry directory, bool fileOffset = false) {
		if (directory.Size == 0)
			return;
		var address = unchecked((uint)directory.RelativeVirtualAddress);
		if (directory.Size < 0)
			_issues.Add(name + ": the declared directory exceeds the supported inspection size.");
		Add("Directory", name, fileOffset ? null : address, fileOffset ? address : Map(address), unchecked((uint)directory.Size),
			fileOffset ? "File-offset address space" : "RVA address space");
	}

	private void ReadImports(DirectoryEntry directory) {
		if (directory.Size == 0)
			return;
		var count = Math.Min(directory.Size / 20, 4096);
		var terminated = false;
		for (var index = 0; index < count && CanAddEntries; index++) {
			_checkpoint?.Invoke();
			var descriptor = ReadRva(AddRva(directory.RelativeVirtualAddress, index * 20), 20);
			var lookup = U32(descriptor, 0);
			var name = U32(descriptor, 12);
			var addressTable = U32(descriptor, 16);
			if (lookup == 0 && name == 0 && addressTable == 0) {
				terminated = true;
				break;
			}
			var library = ReadName(name);
			var table = lookup == 0 ? addressTable : lookup;
			var width = _image.IsPe32Plus ? 8 : 4;
			for (var item = 0; item < 16384 && CanAddEntries; item++) {
				var entryAddress = AddRva(table, checked(item * width));
				var bytes = ReadRva(entryAddress, width);
				var value = width == 8 ? EndianBitConverter.Little.ToUInt64(bytes) : U32(bytes, 0);
				if (value == 0)
					break;
				var ordinalFlag = width == 8 ? 0x8000000000000000UL : 0x80000000UL;
				var symbol = (value & ordinalFlag) != 0 ? "#" + (value & 0xffff).ToString(CultureInfo.InvariantCulture) : ReadName(checked((uint)value + 2));
				Add("Import", library + "!" + symbol, entryAddress, Map(entryAddress), width, "IAT=" + Hex(AddRva(addressTable, checked(item * width))));
			}
		}
		if (!terminated)
			_issues.Add("Import descriptors reached the declared directory or parser limit without a terminator.");
	}

	private void ReadExports(DirectoryEntry directory) {
		if (directory.Size == 0)
			return;
		var header = ReadRva((uint)directory.RelativeVirtualAddress, 40);
		var ordinalBase = U32(header, 16);
		var functions = U32(header, 20);
		var names = U32(header, 24);
		Guard.Ensure(functions <= MaximumEntries && names <= functions, "The PE export counts exceed the parser bound.");
		var functionTable = U32(header, 28);
		var nameTable = U32(header, 32);
		var ordinalTable = U32(header, 36);
		var byOrdinal = new Dictionary<uint, string>();
		var nameCharacters = 0;
		for (var index = 0; index < names; index++) {
			_checkpoint?.Invoke();
			var ordinal = EndianBitConverter.Little.ToUInt16(ReadRva(AddRva(ordinalTable, checked(index * 2)), 2));
			Guard.Ensure(ordinal < functions, "An export name has an invalid function ordinal.");
			var name = ReadName(U32(ReadRva(AddRva(nameTable, checked(index * 4)), 4), 0));
			nameCharacters += name.Length;
			Guard.Ensure(nameCharacters <= MaximumRetainedCharacters, "The export name table exceeds its aggregate text bound.");
			byOrdinal[ordinal] = name;
		}
		for (uint index = 0; index < functions && CanAddEntries; index++) {
			_checkpoint?.Invoke();
			var address = U32(ReadRva(AddRva(functionTable, checked((long)index * 4)), 4), 0);
			if (address == 0)
				continue;
			var forwarded = address >= (uint)directory.RelativeVirtualAddress && address < (ulong)(uint)directory.RelativeVirtualAddress + (uint)directory.Size;
			Add("Export", byOrdinal.TryGetValue(index, out var name) ? name : "#" + (ordinalBase + index), address, Map(address), 0,
				forwarded ? "Forwarder=" + ReadName(address) : "Ordinal=" + (ordinalBase + index));
		}
	}

	private void ReadResources(DirectoryEntry directory) {
		if (directory.Size == 0)
			return;
		var visited = new HashSet<uint>();
		ReadResourceNode((uint)directory.RelativeVirtualAddress, (uint)directory.Size, 0, string.Empty, 0, visited);
	}

	private void ReadResourceNode(uint root, uint size, uint offset, string path, int depth, HashSet<uint> visited) {
		Guard.Ensure(depth <= 8 && visited.Count < 4096 && visited.Add(offset), "The resource tree exceeds its depth/node bound or contains a cycle.");
		var header = ResourceBytes(root, size, offset, 16);
		var count = (uint)EndianBitConverter.Little.ToUInt16(header, 12) + EndianBitConverter.Little.ToUInt16(header, 14);
		Guard.Ensure(count <= 4096, "The resource directory exceeds its entry bound.");
		for (var index = 0; index < count && CanAddEntries; index++) {
			_checkpoint?.Invoke();
			var item = ResourceBytes(root, size, checked(offset + 16 + (uint)index * 8), 8);
			var identifier = U32(item, 0);
			var target = U32(item, 4);
			var name = (identifier & 0x80000000) == 0 ? identifier.ToString(CultureInfo.InvariantCulture) : ResourceName(root, size, identifier & 0x7fffffff);
			var fullName = path.Length == 0 ? name : path + "/" + name;
			if ((target & 0x80000000) != 0) {
				ReadResourceNode(root, size, target & 0x7fffffff, fullName, depth + 1, visited);
				continue;
			}
			var data = ResourceBytes(root, size, target, 16);
			var address = U32(data, 0);
			var length = U32(data, 4);
			var value = "CodePage=" + U32(data, 8);
			// Resource type 24 is an XML manifest. Keep the payload bounded and never resolve XML entities.
			if (fullName.StartsWith("24/", StringComparison.Ordinal) && length <= 262144)
				value += "; Manifest=" + Encoding.UTF8.GetString(ReadRva(address, (int)length)).TrimEnd('\0');
			Add("Resource", fullName, address, Map(address), length, value);
		}
	}

	private byte[] ResourceBytes(uint root, uint size, uint offset, int count) {
		Guard.Ensure((ulong)offset + (uint)count <= size, "A resource entry exceeds its declared directory.");
		return ReadRva(AddRva(root, offset), count);
	}

	private string ResourceName(uint root, uint size, uint offset) {
		var length = EndianBitConverter.Little.ToUInt16(ResourceBytes(root, size, offset, 2));
		Guard.Ensure(length <= 4096, "The resource name exceeds its bound.");
		return Encoding.Unicode.GetString(ResourceBytes(root, size, checked(offset + 2), length * 2));
	}

	private void ReadRelocations(DirectoryEntry directory) {
		var offset = 0;
		while (offset < directory.Size && CanAddEntries) {
			_checkpoint?.Invoke();
			Guard.Ensure(directory.Size - offset >= 8, "The relocation block header is truncated.");
			var header = ReadRva(AddRva(directory.RelativeVirtualAddress, offset), 8);
			var page = U32(header, 0);
			var length = U32(header, 4);
			Guard.Ensure(length >= 8 && length % 2 == 0 && length <= directory.Size - offset, "The relocation block has an invalid size.");
			for (var index = 8; index < length && CanAddEntries; index += 2) {
				var value = EndianBitConverter.Little.ToUInt16(ReadRva(AddRva(directory.RelativeVirtualAddress, offset + index), 2));
				var address = AddRva(page, value & 0xfff);
				Add("Relocation", "Type=" + (value >> 12), address, Map(address), 0, "Page=" + Hex(page));
			}
			offset = checked(offset + (int)length);
		}
	}

	private void ReadRuntimeFunctions(DirectoryEntry directory) {
		if (directory.Size == 0)
			return;
		if (_image.Machine != Machine.Amd64.ToString()) {
			_issues.Add("Exception directory retained as a raw range; structured runtime-function decoding currently supports AMD64 only.");
			return;
		}
		Guard.Ensure(directory.Size % 12 == 0, "The AMD64 exception directory is not a whole runtime-function array.");
		var unwindRecords = new HashSet<uint>();
		for (var index = 0; index < directory.Size / 12 && CanAddEntries; index++) {
			_checkpoint?.Invoke();
			var bytes = ReadRva(AddRva(directory.RelativeVirtualAddress, index * 12), 12);
			var start = U32(bytes, 0);
			var end = U32(bytes, 4);
			Guard.Ensure(end >= start, "A runtime function ends before its start.");
			var unwindAddress = U32(bytes, 8);
			Add("RuntimeFunction", Hex(start), start, Map(start), end - start, "UnwindInfo=" + Hex(unwindAddress));
			if (unwindRecords.Add(unwindAddress))
				ReadUnwindInfo(unwindAddress);
		}
	}

	private void ReadUnwindInfo(uint address) {
		var header = ReadRva(address, 4);
		var version = header[0] & 7;
		var flags = header[0] >> 3;
		var codeBytes = header[2] * 2;
		var alignedSize = (4 + codeBytes + 3) & ~3;
		var detail = $"Version={version}; Flags={Hex((uint)flags)}; PrologSize={header[1]}; FrameRegister={header[3] & 15}; FrameOffset={header[3] >> 4}";
		if (version is not 1 and not 2) {
			Add("UnwindInfo", Hex(address), address, Map(address), 4, detail + "; UnsupportedVersion");
			return;
		}
		if (codeBytes > 0)
			detail += "; Codes=" + ReadRva(AddRva(address, 4), codeBytes).ToHexString();
		if ((flags & 4) != 0) {
			Guard.Ensure((flags & 3) == 0, "Chained unwind information also declares a handler.");
			var chained = ReadRva(AddRva(address, alignedSize), 12);
			detail += "; ChainedStart=" + Hex(U32(chained, 0)) + "; ChainedEnd=" + Hex(U32(chained, 4)) + "; ChainedUnwind=" + Hex(U32(chained, 8));
			alignedSize += 12;
		} else if ((flags & 3) != 0) {
			detail += "; Handler=" + Hex(U32(ReadRva(AddRva(address, alignedSize), 4), 0));
			alignedSize += 4;
		}
		Add("UnwindInfo", Hex(address), address, Map(address), alignedSize, detail);
	}

	private void ReadTls(DirectoryEntry directory) {
		if (directory.Size == 0)
			return;
		var width = _image.IsPe32Plus ? 8 : 4;
		var bytes = ReadRva((uint)directory.RelativeVirtualAddress, width * 4 + 8);
		var names = new[] { "RawDataStartVA", "RawDataEndVA", "IndexVA", "CallbacksVA" };
		for (var index = 0; index < names.Length; index++) {
			var value = width == 8 ? EndianBitConverter.Little.ToUInt64(bytes, index * width) : U32(bytes, index * width);
			var field = AddRva(directory.RelativeVirtualAddress, index * width);
			Add("TLS", names[index], field, Map(field), width, Hex(value));
		}
		Add("TLS", "ZeroFillSize", null, null, 4, U32(bytes, width * 4).ToString(CultureInfo.InvariantCulture));
		Add("TLS", "Characteristics", null, null, 4, Hex(U32(bytes, width * 4 + 4)));
	}

	private void ReadLoadConfiguration(DirectoryEntry directory) {
		if (directory.Size == 0)
			return;
		var address = (uint)directory.RelativeVirtualAddress;
		var declared = U32(ReadRva(address, 4), 0);
		Guard.Ensure(declared >= 4 && declared <= directory.Size && declared <= 4096, "The load configuration exceeds its declared directory or inspection bound.");
		var bytes = ReadRva(address, (int)declared);
		Add("LoadConfiguration", "Size", address, Map(address), 4, declared.ToString(CultureInfo.InvariantCulture));
		AddLoadField(bytes, address, "TimeDateStamp", 4, 4);
		AddLoadField(bytes, address, "GlobalFlagsClear", 12, 4);
		AddLoadField(bytes, address, "GlobalFlagsSet", 16, 4);
		AddLoadField(bytes, address, "CriticalSectionDefaultTimeout", 20, 4);
		var width = _image.IsPe32Plus ? 8 : 4;
		var securityCookieOffset = _image.IsPe32Plus ? 88 : 60;
		AddLoadField(bytes, address, "SecurityCookieVA", securityCookieOffset, width);
		AddLoadField(bytes, address, "SEHandlerTableVA", securityCookieOffset + width, width);
		AddLoadField(bytes, address, "SEHandlerCount", securityCookieOffset + 2 * width, width);
		AddLoadField(bytes, address, "GuardCFCheckFunctionVA", securityCookieOffset + 3 * width, width);
		AddLoadField(bytes, address, "GuardCFDispatchFunctionVA", securityCookieOffset + 4 * width, width);
		AddLoadField(bytes, address, "GuardCFFunctionTableVA", securityCookieOffset + 5 * width, width);
		AddLoadField(bytes, address, "GuardCFFunctionCount", securityCookieOffset + 6 * width, width);
		AddLoadField(bytes, address, "GuardFlags", securityCookieOffset + 7 * width, 4);
	}

	private void AddLoadField(byte[] bytes, uint address, string name, int offset, int width) {
		if (offset + width > bytes.Length)
			return;
		var value = width == 8 ? EndianBitConverter.Little.ToUInt64(bytes, offset) : U32(bytes, offset);
		var field = AddRva(address, offset);
		Add("LoadConfiguration", name, field, Map(field), width, Hex(value));
	}

	private void ReadDebug(PEReader reader) {
		Guard.Ensure(reader.PEHeaders.PEHeader.DebugTableDirectory.Size / 28 <= MaximumEntries, "The debug directory exceeds its entry bound.");
		foreach (var entry in reader.ReadDebugDirectory()) {
			_checkpoint?.Invoke();
			Guard.Ensure(entry.DataSize is >= 0 and <= 1024 * 1024, "A debug payload exceeds the 1 MiB inspection bound.");
			var detail = "Version=" + entry.MajorVersion + "." + entry.MinorVersion;
			if (entry.Type == DebugDirectoryEntryType.CodeView) {
				var codeView = reader.ReadCodeViewDebugDirectoryData(entry);
				detail += "; Guid=" + codeView.Guid + "; Age=" + codeView.Age + "; Path=" + codeView.Path;
			}
			Add("Debug", entry.Type.ToString(), (uint)entry.DataRelativeVirtualAddress, entry.DataPointer, entry.DataSize, detail);
		}
	}

	private void ReadMetadata(PEReader reader) {
		if (!reader.HasMetadata)
			return;
		var clr = reader.PEHeaders.CorHeader;
		Guard.Ensure(clr.MetadataDirectory.Size <= 16 * 1024 * 1024, "The CLR metadata exceeds the 16 MiB inspection bound.");
		Add("CLR", "Flags", null, null, 0, clr.Flags.ToString());
		Add("CLR", "EntryPointTokenOrRVA", null, null, 0, Hex((uint)clr.EntryPointTokenOrRelativeVirtualAddress));
		var metadata = reader.GetMetadataReader();
		Add("CLR", "MetadataVersion", (uint)clr.MetadataDirectory.RelativeVirtualAddress, Map((uint)clr.MetadataDirectory.RelativeVirtualAddress), clr.MetadataDirectory.Size, metadata.MetadataVersion);
		if (metadata.IsAssembly) {
			var assembly = metadata.GetAssemblyDefinition();
			Add("CLR", "Assembly", null, null, 0, ReadMetadataString(metadata, assembly.Name) + ", Version=" + assembly.Version);
		}
		foreach (var handle in metadata.AssemblyReferences) {
			_checkpoint?.Invoke();
			if (!CanAddEntries)
				break;
			var reference = metadata.GetAssemblyReference(handle);
			Add("AssemblyReference", ReadMetadataString(metadata, reference.Name), null, null, 0, reference.Version.ToString());
		}
		foreach (var handle in metadata.ManifestResources) {
			_checkpoint?.Invoke();
			if (!CanAddEntries)
				break;
			var resource = metadata.GetManifestResource(handle);
			Add("ManagedResource", ReadMetadataString(metadata, resource.Name), null, null, 0, "Offset=" + resource.Offset + "; Attributes=" + resource.Attributes);
		}
		foreach (var handle in metadata.TypeDefinitions) {
			_checkpoint?.Invoke();
			if (!CanAddEntries)
				break;
			var type = metadata.GetTypeDefinition(handle);
			var name = ReadMetadataString(metadata, type.Namespace) + "." + ReadMetadataString(metadata, type.Name);
			Add("ManagedType", name.TrimStart('.'), null, null, 0, "Attributes=" + type.Attributes + "; Methods=" + type.GetMethods().Count + "; Fields=" + type.GetFields().Count);
		}
	}

	private byte[] ReadRva(uint address, int count) {
		_checkpoint?.Invoke();
		Guard.Ensure(count >= 0 && count <= 1024 * 1024 && _image.TryGetFileOffset(address, count, out _), "A PE range has no unique, backed file mapping or exceeds the read bound.");
		_image.TryGetFileOffset(address, count, out var offset);
		_stream.Position = offset;
		var bytes = new byte[count];
		_stream.ReadExactly(bytes);
		return bytes;
	}

	private string ReadName(uint address) {
		var bytes = new List<byte>();
		Guard.Ensure(_image.TryGetFileOffset(address, 1, out var offset), "The PE string has no backed file mapping.");
		_stream.Position = offset;
		for (var index = 0; index < 4096; index++) {
			Guard.Ensure(_image.TryGetFileOffset(AddRva(address, index), 1, out var current) && current == offset + index,
				"The PE string exceeds its uniquely mapped file range.");
			var value = _stream.ReadByte();
			if (value < 0)
				throw new EndOfStreamException();
			if (value == 0)
				return Encoding.UTF8.GetString(bytes.ToArray());
			bytes.Add((byte)value);
		}
		throw new InvalidDataException("A PE string has no terminator within the parser bound.");
	}

	private long? Map(uint address) => _image.TryGetFileOffset(address, 1, out var offset) ? offset : null;

	private void Parse(string name, Action operation) {
		if (!CanAddEntries)
			return;
		Tools.Exceptions.ExecuteCapturingException(operation, out var captured,
			failure => failure is BadImageFormatException or InvalidDataException or EndOfStreamException or OverflowException or ArgumentException or InvalidOperationException);
		if (captured != null)
			_issues.Add(name + ": " + captured.Message);
	}

	private void Add(string kind, string name, uint? address, long? offset, long size, string value) {
		if (!CanAddEntries)
			return;
		var capacity = MaximumRetainedCharacters - _retainedCharacters - kind.Length;
		if (capacity <= 0) {
			_retainedCharacters = MaximumRetainedCharacters;
			return;
		}
		name = BoundText(name, Math.Min(4096, capacity));
		value = BoundText(value, Math.Min(16384, capacity - name.Length));
		_retainedCharacters += kind.Length + name.Length + value.Length;
		_entries.Add(new PortableExecutableEntry(kind, name, address, offset, size, value));
	}

	private string ReadMetadataString(MetadataReader metadata, StringHandle handle) {
		if (!_metadataStrings.TryGetValue(handle, out var value)) {
			value = BoundText(metadata.GetString(handle), 4096);
			_metadataStrings.Add(handle, value);
		}
		return value;
	}

	private string BoundText(string text, int maximum) {
		if (text.Length <= maximum)
			return text;
		_textTruncated = true;
		if (maximum > 0 && char.IsHighSurrogate(text[maximum - 1]) && char.IsLowSurrogate(text[maximum]))
			maximum--;
		return text[..maximum];
	}

	private static uint AddRva(long address, long displacement) => checked((uint)(address + displacement));

	private static uint U32(byte[] bytes, int offset) => EndianBitConverter.Little.ToUInt32(bytes, offset);

	private static string Hex(ulong value) => "0x" + value.ToString("X", CultureInfo.InvariantCulture);
}
