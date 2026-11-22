// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// A stream implementation that connects multiple disparate byte fragments into one logical stream. The fragments are managed through a <see cref="IStreamFragmentProvider"/>.
/// </summary>
/// <remarks>Array and single-byte operations delegate to the corresponding virtual span overload. Positions and lengths are 64-bit. As with other
/// seekable streams, the position may lie past the end: reading there returns nothing, and writing extends the stream, the gap reading as zeros.
/// <see cref="Flush"/> commits what the provider buffers, and disposal releases the provider.</remarks>
public class FragmentedStream : Stream {

	private long _position;
	private bool _disposed;

	public FragmentedStream(IStreamFragmentProvider fragmentProvider)
		: this(fragmentProvider, fragmentProvider?.TotalBytes ?? 0) {
	}

	public FragmentedStream(IStreamFragmentProvider fragmentProvider, long streamLength) {
		Guard.ArgumentNotNull(fragmentProvider, nameof(fragmentProvider));
		Guard.ArgumentInRange(streamLength, 0, fragmentProvider.TotalBytes, nameof(streamLength));
		FragmentProvider = fragmentProvider;
		StreamLength = streamLength;
		Position = 0;
	}

	public IStreamFragmentProvider FragmentProvider { get; }

	public override bool CanRead => !_disposed;

	public override bool CanSeek => !_disposed;

	public override bool CanWrite => !_disposed;

	public override long Length {
		get {
			EnsureOpen();
			return StreamLength;
		}
	}

	public long StreamLength { get; private set; }

	public override long Position {
		get => _position;
		set {
			EnsureOpen();
			Guard.ArgumentNotNegative(value, nameof(value));
			_position = value;
		}
	}

	public override void SetLength(long value) {
		EnsureOpen();
		Guard.ArgumentNotNegative(value, nameof(value));
		FragmentProvider.SetTotalBytes(value);

		if (Position > value)
			Position = value;
		StreamLength = value;
	}

	public override long Seek(long offset, SeekOrigin origin) {
		EnsureOpen();
		var position = origin switch {
			SeekOrigin.Begin => offset,
			SeekOrigin.Current => checked(Position + offset),
			SeekOrigin.End => checked(Length + offset),
			_ => throw new ArgumentOutOfRangeException(nameof(origin))
		};
		if (position < 0)
			throw new IOException("Cannot seek before the start of a stream.");
		return Position = position;
	}

	public override int ReadByte() {
		Span<byte> value = stackalloc byte[1];
		return Read(value) == 0 ? -1 : value[0];
	}

	public override int Read(byte[] buffer, int offset, int count) {
		Guard.ArgumentNotNull(buffer, nameof(buffer));
		Guard.ArgumentInRange(offset, 0, Math.Max(0, buffer.Length - 1), nameof(offset));
		Guard.ArgumentInRange(count, 0, int.MaxValue, nameof(count));

		if (Length == 0)
			return 0;

		var remainingBufferBytes = buffer.Length - offset;
		return Read(buffer.AsSpan(offset, Math.Min(count, remainingBufferBytes)));
	}

	public override int Read(Span<byte> buffer) {
		EnsureOpen();
		var bytesToRead = Math.Max(0, Math.Min(buffer.Length, Length - Position));
		var bufferOffset = 0;
		var totalRead = (int)bytesToRead;

		while (bytesToRead > 0) {
			FragmentProvider.MapStreamPosition(Position, out var fragmentIndex, out var fragmentPosition);
			var fragmentPositionI = Tools.Collection.CheckNotImplemented64bitAddressingLength(fragmentPosition);

			var bufferSlice = buffer.Slice(bufferOffset, (int)bytesToRead);
			var bytesToReadFromFragment = FragmentProvider.ReadFragment(fragmentIndex, fragmentPositionI, bufferSlice);
			if (bytesToReadFromFragment <= 0 || bytesToReadFromFragment > bytesToRead)
				throw new EndOfStreamException("Fragment provider returned an invalid read count before the logical end of the stream.");

			bufferOffset += bytesToReadFromFragment;
			Position += bytesToReadFromFragment;
			bytesToRead -= bytesToReadFromFragment;
		}
		Debug.Assert(bytesToRead == 0);
		return totalRead;
	}

	public override void Write(byte[] buffer, int offset, int count) {
		Guard.ArgumentNotNull(buffer, nameof(buffer));
		if (count == 0)
			return;
		Guard.ArgumentInRange(offset, 0, buffer.Length - 1, nameof(offset));
		Guard.ArgumentInRange(count, 0, buffer.Length - offset, nameof(count));
		Write(buffer.AsSpan(offset, count));
	}

	public override void Write(ReadOnlySpan<byte> buffer) {
		EnsureOpen();
		var remainingStreamBytes = Length - Position;
		var bytesToWrite = buffer.Length;
		if (bytesToWrite == 0)
			return;

		// Grow stream if required; from a position past the end, the provider's zero-filled growth is the gap.
		if (bytesToWrite > remainingStreamBytes) {
			var newStreamLength = Length + (bytesToWrite - remainingStreamBytes);
			var prePos = Position;
			FragmentProvider.SetTotalBytes(newStreamLength);

			StreamLength = newStreamLength;
			var postPos = Position;
			Guard.Ensure(postPos == prePos, "Stream pointer has moved during resize");
		}

		var bufferOffset = 0;
		while (bytesToWrite > 0) {
			FragmentProvider.MapStreamPosition(Position, out var fragmentIndex, out var fragmentPosition);
			var fragmentPositionI = Tools.Collection.CheckNotImplemented64bitAddressingLength(fragmentPosition);

			// The provider owns the destination; pass only the caller's changed bytes.
			var fragmentLength = FragmentProvider.GetFragmentLength(fragmentIndex);
			var bytesToWriteToFragment = Math.Min(fragmentLength - fragmentPositionI, bytesToWrite);
			FragmentProvider.UpdateFragment(fragmentIndex, fragmentPosition, buffer.Slice(bufferOffset, bytesToWriteToFragment));

			bufferOffset += bytesToWriteToFragment;
			Position += bytesToWriteToFragment;
			bytesToWrite -= bytesToWriteToFragment;
		}
		Debug.Assert(bytesToWrite == 0);
	}

	public override void WriteByte(byte value) {
		ReadOnlySpan<byte> bytes = stackalloc byte[] { value };
		Write(bytes);
	}

	/// <summary>Commits what the fragment provider buffers.</summary>
	public override void Flush() {
		EnsureOpen();
		FragmentProvider.Flush();
	}

	public virtual byte[] ToArray() {
		var result = new byte[Tools.Collection.CheckNotImplemented64bitAddressingLength(Length)];
		var position = 0L;
		var bytesToRead = Length;
		while (bytesToRead > 0) {
			FragmentProvider.MapStreamPosition(position, out var fragmentIndex, out var fragmentPosition);
			var fragmentPositionI = Tools.Collection.CheckNotImplemented64bitAddressingLength(fragmentPosition);

			var bytesToReadFromFragment = FragmentProvider.ReadFragment(fragmentIndex, fragmentPositionI, result.AsSpan((int)position, (int)bytesToRead));
			if (bytesToReadFromFragment <= 0 || bytesToReadFromFragment > bytesToRead)
				throw new EndOfStreamException("Fragment provider returned an invalid read count before the logical end of the stream.");

			position += bytesToReadFromFragment;
			bytesToRead -= bytesToReadFromFragment;
		}
		Debug.Assert(bytesToRead == 0);
		return result;
	}

	protected override void Dispose(bool disposing) {
		if (_disposed)
			return;
		_disposed = true;
		base.Dispose(disposing);
		// A provider that buffers commits on its own disposal, which this releases once.
		if (disposing) {
			if (FragmentProvider is IDisposable disposable)
				disposable.Dispose();
		}
	}

	public override async ValueTask DisposeAsync() {
		// Preserve virtual Close/Dispose(true) cleanup for this synchronous-first stream and its subclasses.
		await Task.Run(Dispose).ConfigureAwait(false);
		// The synchronous path already releases IDisposable providers; only async-only providers remain.
		if (FragmentProvider is IAsyncDisposable asyncDisposable && FragmentProvider is not IDisposable)
			await asyncDisposable.DisposeAsync().ConfigureAwait(false);
	}

	private void EnsureOpen() => ObjectDisposedException.ThrowIf(_disposed, this);

}

