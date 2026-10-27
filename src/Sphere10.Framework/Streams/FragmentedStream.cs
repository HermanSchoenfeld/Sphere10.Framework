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
/// <remarks>Array and single-byte operations delegate to the corresponding virtual span overload.</remarks>
public class FragmentedStream : Stream {

	private long _position;

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

	public override bool CanRead => true;

	public override bool CanSeek => true;

	public override bool CanWrite => true;

	public override long Length => StreamLength;

	public long StreamLength { get; private set; }

	public override long Position {
		get => _position;
		set {
			Guard.ArgumentInRange(value, 0, Length, "Value");
			_position = value;
		}
	}

	public override void SetLength(long value) {
		Guard.ArgumentInRange(value, 0, int.MaxValue, nameof(value));
		FragmentProvider.SetTotalBytes(value);

		if (Position > value)
			Position = value;
		StreamLength = value;
	}

	public override long Seek(long offset, SeekOrigin origin) {
		switch (origin) {
			case SeekOrigin.Begin:
				Guard.ArgumentInRange(offset, 0, Math.Max(0, Length), nameof(offset));
				Position = offset;
				break;

			case SeekOrigin.Current:
				Guard.ArgumentInRange(offset, -Position, Math.Max(0, Length - Position), nameof(offset));
				Position += offset;
				break;

			case SeekOrigin.End:
				Guard.ArgumentInRange(offset, -Length, 0, nameof(offset));
				Position = Length + offset;
				break;
		}

		return Position;
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
		var remainingStreamBytes = Length - Position;
		var bytesToWrite = buffer.Length;

		// Grow stream if required
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

	public override void Flush() {
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
		base.Dispose(disposing);
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

}

