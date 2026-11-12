// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Interprets the bytes at a document offset as the fixed-width values a data inspector displays.</summary>
public class HexEditorDataInspector {
	/// <summary>Whether multi-byte values are decoded most-significant byte first. Defaults to little endian.</summary>
	public bool IsBigEndian { get; set; }

	/// <summary>Inspects up to eight bytes at an offset; offsets at or beyond the end yield an empty inspection.</summary>
	public HexEditorInspection Inspect(HexEditorDocument document, long offset) {
		Guard.ArgumentNotNull(document, nameof(document));
		Span<byte> buffer = stackalloc byte[8];
		var available = offset >= 0 && offset < document.Length ? document.Read(offset, buffer) : 0;
		return new HexEditorInspection(buffer.Slice(0, available), IsBigEndian);
	}
}
