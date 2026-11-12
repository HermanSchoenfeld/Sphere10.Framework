// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text;

namespace Sphere10.Framework.Windows;

/// <summary>A manifest property with its original encoded bytes.</summary>
public sealed record EtwTraceProperty(string Name, byte[] Data) {
	/// <summary>The manifest TDH input type, retained so consumers can decode values without guessing their layout.</summary>
	public ushort InputType { get; init; }

	public int ElementCount { get; init; } = 1;

	/// <summary>Reads a scalar unsigned integer, pointer or FILETIME using its manifest type and original width.</summary>
	public bool TryGetUInt64(out ulong value) {
		value = 0;
		if (ElementCount != 1 || Data == null)
			return false;
		if (InputType == 4 && Data.Length == 1)
			value = Data[0];
		else if (InputType == 6 && Data.Length == 2)
			value = EndianBitConverter.Little.ToUInt16(Data);
		else if (InputType is 8 or 20 && Data.Length == 4)
			value = EndianBitConverter.Little.ToUInt32(Data);
		else if (InputType is 10 or 17 or 21 && Data.Length == 8)
			value = EndianBitConverter.Little.ToUInt64(Data);
		else if (InputType is 16 or 30 && Data.Length is 4 or 8)
			value = Data.Length == 4 ? EndianBitConverter.Little.ToUInt32(Data) : EndianBitConverter.Little.ToUInt64(Data);
		else
			return false;
		return true;
	}

	/// <summary>Reads a manifest Unicode string. Other encodings remain raw rather than being guessed.</summary>
	public bool TryGetUnicodeString(out string value) {
		value = null;
		if (InputType != 1 || ElementCount != 1 || Data == null || Data.Length % 2 != 0)
			return false;
		value = Encoding.Unicode.GetString(Data).TrimEnd('\0');
		return true;
	}
}
