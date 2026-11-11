// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>
/// A seekable stream of any length whose bytes are computed from their offset instead of stored, so a
/// multi-gigabyte document costs no memory. Each 4 KiB block starts with a readable "BLK" header followed by
/// pseudo-random bytes; writes are kept in a sparse overlay. Read statistics show how little a viewer touches.
/// </summary>
public sealed class SyntheticByteStream : Stream {
	public const int BlockSize = 4096;
	public const int HeaderLength = 16;
	private readonly long _length;
	private readonly bool _writable;
	private readonly Dictionary<long, byte> _overrides;
	private long _position;

	public SyntheticByteStream(long length)
		: this(length, true) {
	}

	public SyntheticByteStream(long length, bool writable) {
		Guard.ArgumentNotNegative(length, nameof(length));
		_length = length;
		_writable = writable;
		_overrides = new Dictionary<long, byte>();
	}

	public override bool CanRead => true;

	public override bool CanSeek => true;

	public override bool CanWrite => _writable;

	public override long Length => _length;

	public override long Position {
		get => _position;
		set {
			Guard.ArgumentNotNegative(value, nameof(value));
			_position = value;
		}
	}

	/// <summary>The number of <see cref="Read(byte[], int, int)"/> calls served.</summary>
	public long ReadCallCount { get; private set; }

	/// <summary>The total bytes handed to readers.</summary>
	public long BytesRead { get; private set; }

	/// <summary>The number of writes applied to the overlay.</summary>
	public long WriteCallCount { get; private set; }

	/// <summary>The number of offsets whose byte differs from the generated content.</summary>
	public int OverriddenByteCount => _overrides.Count;

	/// <summary>The deterministic byte at an offset before any write: a block header or a hashed pseudo-random value.</summary>
	public static byte GenerateByte(long offset) {
		var withinBlock = (int)(offset % BlockSize);
		if (withinBlock < HeaderLength)
			return (byte)BlockHeader(offset / BlockSize)[withinBlock];
		var hash = Mix(offset >> 3);
		return (byte)(hash >> (int)((offset & 7) * 8));
	}

	/// <summary>The sixteen-character header starting each block, such as "BLK 000000001F\r\n".</summary>
	public static string BlockHeader(long blockIndex) => "BLK " + blockIndex.ToString("X10", CultureInfo.InvariantCulture) + "\r\n";

	public override int Read(byte[] buffer, int offset, int count) {
		Guard.ArgumentNotNull(buffer, nameof(buffer));
		Guard.ArgumentInRange(offset, 0, buffer.Length, nameof(offset));
		Guard.ArgumentInRange(count, 0, buffer.Length - offset, nameof(count));
		return Read(buffer.AsSpan(offset, count));
	}

	public override int Read(Span<byte> buffer) {
		var available = (int)Math.Clamp(_length - _position, 0, buffer.Length);
		for (var index = 0; index < available; index++) {
			var position = _position + index;
			buffer[index] = _overrides.TryGetValue(position, out var overridden) ? overridden : GenerateByte(position);
		}
		_position += available;
		ReadCallCount++;
		BytesRead += available;
		return available;
	}

	public override void Write(byte[] buffer, int offset, int count) {
		Guard.ArgumentNotNull(buffer, nameof(buffer));
		Guard.ArgumentInRange(offset, 0, buffer.Length, nameof(offset));
		Guard.ArgumentInRange(count, 0, buffer.Length - offset, nameof(count));
		Write(buffer.AsSpan(offset, count));
	}

	public override void Write(ReadOnlySpan<byte> buffer) {
		if (!_writable)
			throw new NotSupportedException("The synthetic stream is read-only.");
		if (_position + buffer.Length > _length)
			throw new NotSupportedException("The synthetic stream has a fixed length.");
		for (var index = 0; index < buffer.Length; index++) {
			var position = _position + index;
			if (buffer[index] == GenerateByte(position))
				_overrides.Remove(position);
			else
				_overrides[position] = buffer[index];
		}
		_position += buffer.Length;
		WriteCallCount++;
	}

	public override long Seek(long offset, SeekOrigin origin) {
		var target = origin switch {
			SeekOrigin.Begin => offset,
			SeekOrigin.Current => _position + offset,
			SeekOrigin.End => _length + offset,
			_ => throw new ArgumentOutOfRangeException(nameof(origin))
		};
		Guard.Argument(target >= 0, nameof(offset), "Cannot seek before the start of the stream.");
		_position = target;
		return _position;
	}

	public override void Flush() {
	}

	public override void SetLength(long value) => throw new NotSupportedException("The synthetic stream has a fixed length.");

	// SplitMix64 finalizer: cheap, well distributed and stateless per 8-byte lane.
	private static ulong Mix(long value) {
		var z = (ulong)value + 0x9E3779B97F4A7C15UL;
		z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
		z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
		return z ^ (z >> 31);
	}
}
