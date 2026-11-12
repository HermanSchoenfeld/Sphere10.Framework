// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// The numeric and character interpretations of up to eight bytes at one offset. A value is null when fewer
/// bytes than its width were available.
/// </summary>
public sealed class HexEditorInspection {
	public const string Unavailable = "-";
	private HexEditorInspectionEntry[] _entries;

	public HexEditorInspection(ReadOnlySpan<byte> bytes, bool bigEndian) {
		Guard.Argument(bytes.Length <= 8, nameof(bytes), "At most eight bytes are inspected.");
		AvailableBytes = bytes.Length;
		IsBigEndian = bigEndian;
		if (bytes.Length >= 1) {
			UInt8 = bytes[0];
			Int8 = (sbyte)bytes[0];
			AsciiCharacter = bytes[0] >= 0x20 && bytes[0] <= 0x7E ? ((char)bytes[0]).ToString() : null;
		}
		if (bytes.Length >= 2) {
			Int16 = bigEndian ? BinaryPrimitives.ReadInt16BigEndian(bytes) : BinaryPrimitives.ReadInt16LittleEndian(bytes);
			UInt16 = bigEndian ? BinaryPrimitives.ReadUInt16BigEndian(bytes) : BinaryPrimitives.ReadUInt16LittleEndian(bytes);
		}
		if (bytes.Length >= 4) {
			Int32 = bigEndian ? BinaryPrimitives.ReadInt32BigEndian(bytes) : BinaryPrimitives.ReadInt32LittleEndian(bytes);
			UInt32 = bigEndian ? BinaryPrimitives.ReadUInt32BigEndian(bytes) : BinaryPrimitives.ReadUInt32LittleEndian(bytes);
			Single = bigEndian ? BinaryPrimitives.ReadSingleBigEndian(bytes) : BinaryPrimitives.ReadSingleLittleEndian(bytes);
		}
		if (bytes.Length >= 8) {
			Int64 = bigEndian ? BinaryPrimitives.ReadInt64BigEndian(bytes) : BinaryPrimitives.ReadInt64LittleEndian(bytes);
			UInt64 = bigEndian ? BinaryPrimitives.ReadUInt64BigEndian(bytes) : BinaryPrimitives.ReadUInt64LittleEndian(bytes);
			Double = bigEndian ? BinaryPrimitives.ReadDoubleBigEndian(bytes) : BinaryPrimitives.ReadDoubleLittleEndian(bytes);
		}
		Utf8Character = DecodeUtf8(bytes);
	}

	/// <summary>How many bytes were available at the inspected offset (0 to 8).</summary>
	public int AvailableBytes { get; }

	/// <summary>Whether multi-byte values were decoded most-significant byte first.</summary>
	public bool IsBigEndian { get; }

	public sbyte? Int8 { get; }

	public byte? UInt8 { get; }

	public short? Int16 { get; }

	public ushort? UInt16 { get; }

	public int? Int32 { get; }

	public uint? UInt32 { get; }

	public long? Int64 { get; }

	public ulong? UInt64 { get; }

	public float? Single { get; }

	public double? Double { get; }

	/// <summary>The printable ASCII character at the offset, or null when the byte is not printable or unavailable.</summary>
	public string AsciiCharacter { get; }

	/// <summary>The UTF-8 scalar beginning at the offset, or null when the bytes do not start a valid sequence.</summary>
	public string Utf8Character { get; }

	/// <summary>The display rows in inspector order.</summary>
	public HexEditorInspectionEntry[] Entries => _entries ??= BuildEntries();

	private HexEditorInspectionEntry[] BuildEntries() {
		return new[] {
			new HexEditorInspectionEntry("Int8", Format(Int8)),
			new HexEditorInspectionEntry("UInt8", Format(UInt8)),
			new HexEditorInspectionEntry("Int16", Format(Int16)),
			new HexEditorInspectionEntry("UInt16", Format(UInt16)),
			new HexEditorInspectionEntry("Int32", Format(Int32)),
			new HexEditorInspectionEntry("UInt32", Format(UInt32)),
			new HexEditorInspectionEntry("Int64", Format(Int64)),
			new HexEditorInspectionEntry("UInt64", Format(UInt64)),
			new HexEditorInspectionEntry("Single", Format(Single)),
			new HexEditorInspectionEntry("Double", Format(Double)),
			new HexEditorInspectionEntry("ASCII", AsciiCharacter ?? Unavailable),
			new HexEditorInspectionEntry("UTF-8", Utf8Character ?? Unavailable)
		};
	}

	private static string Format<T>(T? value)
		where T : struct, IFormattable {
		return value.HasValue ? value.Value.ToString(null, CultureInfo.InvariantCulture) : Unavailable;
	}

	// Only a complete, valid scalar is reported; control characters are not displayable.
	private static string DecodeUtf8(ReadOnlySpan<byte> bytes) {
		if (bytes.IsEmpty || Rune.DecodeFromUtf8(bytes, out var rune, out _) != OperationStatus.Done)
			return null;
		return Rune.IsControl(rune) ? null : rune.ToString();
	}
}
