// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>A reusable buffer for one rectangle of a display frame: rows of pixels with four bytes each, blue, green, red and alpha.</summary>
/// <remarks>A frame source resizes and fills the buffer on every read, so one instance serves a rectangle frame after frame without allocating.</remarks>
public sealed class DisplayFramePixels {
	public const int BytesPerPixel = 4;
	private byte[] _data = Array.Empty<byte>();

	public int Width { get; private set; }

	public int Height { get; private set; }

	/// <summary>The pixels of one row, left to right.</summary>
	public ReadOnlySpan<byte> GetRow(int row) {
		Guard.ArgumentInRange(row, 0, Height - 1, nameof(row));
		return _data.AsSpan(row * Width * BytesPerPixel, Width * BytesPerPixel);
	}

	/// <summary>Sets the buffer's size and returns its storage, row after row, for a frame source to fill. The previous content is not kept.</summary>
	public Span<byte> Resize(int width, int height) {
		Guard.Argument(width >= 0 && height >= 0, nameof(width), "A pixel buffer cannot have a negative size.");
		Width = width;
		Height = height;
		var length = width * height * BytesPerPixel;
		if (_data.Length < length)
			_data = new byte[length];
		return _data.AsSpan(0, length);
	}
}
