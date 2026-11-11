// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A streaming byte-sequence search over a <see cref="HexEditorDocument"/>. Chunks are scanned with a carried
/// overlap of pattern length minus one, so matches spanning chunk or cache-block boundaries are found without
/// ever loading the whole stream.
/// </summary>
public class HexEditorSearch {
	public const int DefaultChunkSize = 64 * 1024;
	private readonly byte[] _pattern;

	public HexEditorSearch(byte[] pattern)
		: this(pattern, DefaultChunkSize) {
	}

	public HexEditorSearch(byte[] pattern, int chunkSize) {
		Guard.ArgumentNotNull(pattern, nameof(pattern));
		Guard.Argument(pattern.Length > 0, nameof(pattern), "The search pattern cannot be empty.");
		Guard.ArgumentGTE(chunkSize, pattern.Length, nameof(chunkSize), "The chunk size must be at least the pattern length.");
		_pattern = (byte[])pattern.Clone();
		ChunkSize = chunkSize;
	}

	/// <summary>Creates a search for whitespace-separated hexadecimal byte pairs, such as "DE AD BE EF".</summary>
	public static HexEditorSearch ForHex(string text) {
		Guard.ArgumentNotNull(text, nameof(text));
		var compact = string.Concat(text.Where(character => !char.IsWhiteSpace(character)));
		Guard.Argument(compact.Length > 0, nameof(text), "Enter at least one hexadecimal byte pair.");
		Guard.Argument(HexEncoding.IsValid(compact), nameof(text), "Enter hexadecimal byte pairs only (00-FF).");
		return new HexEditorSearch(HexEncoding.Decode(compact));
	}

	/// <summary>Creates a search for text encoded with the supplied encoding.</summary>
	public static HexEditorSearch ForText(string text, Encoding encoding) {
		Guard.ArgumentNotNull(text, nameof(text));
		Guard.ArgumentNotNull(encoding, nameof(encoding));
		Guard.Argument(text.Length > 0, nameof(text), "Enter the text to find.");
		return new HexEditorSearch(encoding.GetBytes(text));
	}

	/// <summary>A copy of the byte sequence being searched for.</summary>
	public byte[] Pattern => (byte[])_pattern.Clone();

	/// <summary>The number of new bytes read per scan step.</summary>
	public int ChunkSize { get; }

	/// <summary>Finds the nearest match in the requested direction, returning its offset or -1.</summary>
	public long Find(HexEditorDocument document, long startOffset, bool forward, CancellationToken cancellationToken = default)
		=> forward ? FindForward(document, startOffset, cancellationToken) : FindBackward(document, startOffset, cancellationToken);

	/// <summary>Finds the first match starting at or after <paramref name="startOffset"/>, returning its offset or -1.</summary>
	public long FindForward(HexEditorDocument document, long startOffset, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(document, nameof(document));
		var overlap = _pattern.Length - 1;
		var buffer = new byte[overlap + ChunkSize];
		var carried = 0;
		var position = Math.Max(0, startOffset);
		while (position < document.Length) {
			cancellationToken.ThrowIfCancellationRequested();
			// The carried tail of the previous chunk precedes the new data so boundary-spanning matches are visible.
			var read = document.ReadUncached(position, buffer.AsSpan(carried, ChunkSize));
			if (read == 0)
				break;
			var window = buffer.AsSpan(0, carried + read);
			var index = window.IndexOf(_pattern);
			if (index >= 0)
				return position - carried + index;
			var keep = Math.Min(overlap, window.Length);
			window.Slice(window.Length - keep).CopyTo(buffer);
			carried = keep;
			position += read;
		}
		return -1;
	}

	/// <summary>Finds the last match starting at or before <paramref name="startOffset"/>, returning its offset or -1.</summary>
	public long FindBackward(HexEditorDocument document, long startOffset, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(document, nameof(document));
		if (startOffset < 0)
			return -1;
		var overlap = _pattern.Length - 1;
		var buffer = new byte[ChunkSize + overlap];
		var carry = new byte[overlap];
		var carried = 0;
		var end = Math.Min(document.Length, startOffset + _pattern.Length);
		while (end > 0) {
			cancellationToken.ThrowIfCancellationRequested();
			// New data precedes the carried head of the previously scanned chunk.
			var count = (int)Math.Min(ChunkSize, end);
			var begin = end - count;
			var read = document.ReadUncached(begin, buffer.AsSpan(0, count));
			carry.AsSpan(0, carried).CopyTo(buffer.AsSpan(read));
			var window = buffer.AsSpan(0, read + carried);
			var index = window.LastIndexOf(_pattern);
			if (index >= 0)
				return begin + index;
			var keep = Math.Min(overlap, window.Length);
			window.Slice(0, keep).CopyTo(carry);
			carried = keep;
			end = begin;
		}
		return -1;
	}
}
