// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Reads ahead into a bounded buffer, with optional lazy length caching and immediate writes.</summary>
/// <remarks>
/// Use the source only through this decorator while it is active. Sequential reads require neither seeking nor sizing.
/// Read-ahead can advance the source beyond this stream's logical position; <see cref="FlushUnreadData"/> explicitly
/// returns unread bytes on seekable sources. Disposal never seeks or flushes the source. Instances are not thread safe.
/// <para>
/// Indicative performance versus ordinary FileStream with a 4 KiB buffer: .NET 10.0.14, Windows x64,
/// warm OS cache, median of five rounds after two warmups, measured 3 November 2026. This stream uses
/// 64 KiB read-ahead over an unbuffered FileStream; setup and validation are outside the timed loop.
/// </para>
/// <list type="table">
/// <listheader><term>Workload</term><description>Measured relative performance</description></listheader>
/// <item><term>4 KiB reads over 64 MiB</term><description>2.61 times faster.</description></item>
/// <item><term>8-256 byte reads over 256 KiB</term><description>1.21-4.83 times faster.</description></item>
/// <item><term>Single-byte reads over 256 KiB</term><description>Near parity; about 3 percent slower in this run.</description></item>
/// <item><term>64 KiB-1 MiB reads and Length queries</term><description>Near parity.</description></item>
/// </list>
/// <para>
/// Results vary with workload and environment; the .NET 64 KiB BufferedStream was faster for 1-64 byte reads.
/// Timed loops allocated no managed memory after warmup; the retained read buffer still costs 64 KiB.
/// See the Streams README and BufferedStreamPerformanceTests for conditions and reproducible comparisons.
/// </para>
/// </remarks>
public class BufferedStream : StreamDecorator {

	/// <summary>The default read-ahead capacity, in bytes.</summary>
	public const int DefaultBufferSize = 64 * 1024;

	private byte[] _buffer;
	private int _bufferOffset;
	private int _bufferCount;
	private long _position;
	private long _streamLength;
	private bool _positionKnown;
	private bool _streamLengthKnown;
	private bool _enableReadBuffering;
	private bool _enableStreamLengthCaching;
	private bool _disposed;

	/// <summary>Creates a decorator without probing the source's capabilities, position or length.</summary>
	/// <param name="stream">The source, accessed exclusively through this decorator while it is in use.</param>
	/// <param name="bufferSize">The positive read-ahead capacity, retained even while buffering is disabled.</param>
	/// <param name="leaveOpen">Whether disposal leaves the source open; defaults to borrowing it.</param>
	/// <param name="enableStreamLengthCaching">Whether successful length queries are cached independently of read buffering.</param>
	/// <param name="enableReadBuffering">Whether reads use the private buffer rather than forwarding directly.</param>
	public BufferedStream(Stream stream, int bufferSize = DefaultBufferSize, bool leaveOpen = true,
		bool enableStreamLengthCaching = true, bool enableReadBuffering = true)
		: base(stream) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Guard.ArgumentGT(bufferSize, 0, nameof(bufferSize));

		// Allocate before retaining ownership; failed construction must leave the supplied source with its caller.
		_buffer = enableReadBuffering ? new byte[bufferSize] : Array.Empty<byte>();
		BufferSize = bufferSize;
		LeaveOpen = leaveOpen;
		_enableReadBuffering = enableReadBuffering;
		_enableStreamLengthCaching = enableStreamLengthCaching;
	}

	/// <summary>The source whose physical cursor may be ahead of this stream's logical position.</summary>
	public Stream WrappedStream => InnerStream;

	/// <summary>The configured read-ahead capacity, including while its allocation is released.</summary>
	public int BufferSize { get; }

	/// <summary>Whether disposal leaves the supplied source open.</summary>
	public bool LeaveOpen { get; }

	/// <summary>Enables read-ahead or direct forwarding; disabling requires no unread buffered bytes.</summary>
	public bool EnableReadBuffering {
		get => _enableReadBuffering;
		set {
			EnsureOpen();
			if (value == _enableReadBuffering)
				return;

			Guard.Ensure(value || UnreadCount == 0, "Call FlushUnreadData before disabling buffering with unread bytes.");

			// Allocation precedes publication so an unsuccessful enable leaves direct forwarding usable.
			var buffer = value ? new byte[BufferSize] : Array.Empty<byte>();
			_buffer = buffer;
			DiscardReadBuffer();
			_positionKnown = false;
			_enableReadBuffering = value;
		}
	}

	/// <summary>Caches successful length queries independently of read buffering; disable for independently growing sources.</summary>
	public bool EnableStreamLengthCaching {
		get => _enableStreamLengthCaching;
		set {
			EnsureOpen();
			if (value == _enableStreamLengthCaching)
				return;

			_streamLengthKnown = false;
			_enableStreamLengthCaching = value;
		}
	}

	/// <inheritdoc />
	public override bool CanRead => !_disposed && InnerStream.CanRead;

	/// <inheritdoc />
	public override bool CanWrite => !_disposed && InnerStream.CanWrite;

	/// <inheritdoc />
	public override bool CanSeek => !_disposed && InnerStream.CanSeek;

	/// <inheritdoc />
	public override bool CanTimeout => !_disposed && InnerStream.CanTimeout;

	/// <summary>The source's length, cached lazily when length caching is enabled.</summary>
	public override long Length {
		get {
			EnsureOpen();
			if (_enableStreamLengthCaching && _streamLengthKnown)
				return _streamLength;

			try {
				var length = InnerStream.Length;
				if (length < 0)
					throw new IOException("The source returned a negative stream length.");
				if (_enableStreamLengthCaching) {
					_streamLength = length;
					_streamLengthKnown = true;
				}
				return length;
			} catch {
				// Sizing can move a custom source before failing; retain unread bytes and rediscover position lazily.
				_positionKnown = false;
				throw;
			}
		}
	}

	/// <summary>The logical cursor, discovered from the source only when first requested or invalidated.</summary>
	public override long Position {
		get {
			EnsureOpen();
			if (!_enableReadBuffering)
				return ReadSourcePosition();
			if (!_positionKnown) {
				var position = checked(ReadSourcePosition() - UnreadCount);
				if (position < 0)
					throw new IOException("The source position precedes its unread buffered data.");
				_position = position;
				_positionKnown = true;
			}
			return _position;
		}
		set {
			Guard.ArgumentNotNegative(value, nameof(value));
			Seek(value, SeekOrigin.Begin);
		}
	}

	/// <inheritdoc />
	public override int ReadTimeout {
		get {
			EnsureOpen();
			return InnerStream.ReadTimeout;
		}
		set {
			EnsureOpen();
			InnerStream.ReadTimeout = value;
		}
	}

	/// <inheritdoc />
	public override int WriteTimeout {
		get {
			EnsureOpen();
			return InnerStream.WriteTimeout;
		}
		set {
			EnsureOpen();
			InnerStream.WriteTimeout = value;
		}
	}

	// Track only the unread portion; the consumed portion remains available for in-buffer seeks.
	private int UnreadCount => _bufferCount - _bufferOffset;

	/// <summary>Reads buffered bytes plus at most one source read, preserving positive short-read results.</summary>
	public override int Read(byte[] buffer, int offset, int count) {
		ValidateBufferArguments(buffer, offset, count);
		return Read(buffer.AsSpan(offset, count));
	}

	/// <inheritdoc />
	public override int Read(Span<byte> buffer) {
		EnsureOpen();
		if (buffer.IsEmpty)
			return 0;

		try {
			if (!_enableReadBuffering) {
				var count = InnerStream.Read(buffer);
				ValidateReadCount(count, buffer.Length);
				return count;
			}

			var copied = CopyBufferedBytes(buffer);
			if (copied == buffer.Length)
				return copied;

			DiscardReadBuffer();
			var remaining = buffer[copied..];
			if (remaining.Length >= BufferSize) {
				// Caller-managed bulk reads bypass the intermediate allocation and copy.
				var count = InnerStream.Read(remaining);
				ValidateReadCount(count, remaining.Length);
				AdvancePosition(count);
				return copied + count;
			}

			_bufferCount = InnerStream.Read(_buffer.AsSpan());
			ValidateReadCount(_bufferCount, _buffer.Length);
			return copied + CopyBufferedBytes(remaining);
		} catch {
			InvalidateAfterFailure();
			throw;
		}
	}

	/// <summary>Reads one byte through the same read-ahead path without allocating a temporary array.</summary>
	public override int ReadByte() {
		Span<byte> value = stackalloc byte[1];
		return Read(value) == 0 ? -1 : value[0];
	}

	/// <inheritdoc />
	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
		ValidateBufferArguments(buffer, offset, count);
		return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
	}

	/// <summary>Uses buffered data first, then makes at most one genuine asynchronous source read.</summary>
	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
		EnsureOpen();
		cancellationToken.ThrowIfCancellationRequested();
		if (buffer.IsEmpty)
			return 0;

		try {
			if (!_enableReadBuffering) {
				var count = await InnerStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
				ValidateReadCount(count, buffer.Length);
				return count;
			}

			var copied = CopyBufferedBytes(buffer.Span);
			if (copied == buffer.Length)
				return copied;

			DiscardReadBuffer();
			var remaining = buffer[copied..];
			if (remaining.Length >= BufferSize) {
				var count = await InnerStream.ReadAsync(remaining, cancellationToken).ConfigureAwait(false);
				ValidateReadCount(count, remaining.Length);
				AdvancePosition(count);
				return copied + count;
			}

			_bufferCount = await InnerStream.ReadAsync(_buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
			ValidateReadCount(_bufferCount, _buffer.Length);
			return copied + CopyBufferedBytes(remaining.Span);
		} catch {
			InvalidateAfterFailure();
			throw;
		}
	}

	/// <summary>Copies through this decorator's reads so retained read-ahead reaches the destination.</summary>
	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
		ValidateCopyToArguments(destination, bufferSize);
		EnsureOpen();
		if (!CanRead) {
			ObjectDisposedException.ThrowIf(!CanWrite, this);
			throw new NotSupportedException("The stream does not support reading.");
		}
		if (cancellationToken.IsCancellationRequested)
			return Task.FromCanceled(cancellationToken);

		return CopyBufferedToAsync(destination, bufferSize, cancellationToken);
	}

	/// <summary>Runs the legacy asynchronous read through this decorator, preserving callback state.</summary>
	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
		ValidateBufferArguments(buffer, offset, count);
		EnsureOpen();
		var task = Task<int>.Factory.StartNew(_ => Read(buffer, offset, count), state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		if (callback != null)
			_ = task.ContinueWith(result => callback(result), TaskScheduler.Default);
		return task;
	}

	/// <summary>Completes the read started by this decorator and propagates its original failure.</summary>
	public override int EndRead(IAsyncResult asyncResult) {
		Guard.ArgumentNotNull(asyncResult, nameof(asyncResult));
		Guard.Argument(asyncResult is Task<int>, nameof(asyncResult), "Expected the result returned by BeginRead.");
		return ((Task<int>)asyncResult).GetAwaiter().GetResult();
	}

	/// <summary>Writes immediately at the logical cursor, first returning unread read-ahead to the source.</summary>
	public override void Write(byte[] buffer, int offset, int count) {
		ValidateBufferArguments(buffer, offset, count);
		Write(buffer.AsSpan(offset, count));
	}

	/// <inheritdoc />
	public override void Write(ReadOnlySpan<byte> buffer) {
		EnsureOpen();
		if (buffer.IsEmpty)
			return;

		// A mutation attempt invalidates length independently of read buffering.
		_streamLengthKnown = false;
		if (!_enableReadBuffering) {
			InnerStream.Write(buffer);
			return;
		}

		// Synchronize unread data before any write; a failed rewind must retain it.
		FlushUnreadData();
		try {
			InnerStream.Write(buffer);
			AdvancePosition(buffer.Length);
		} catch {
			InvalidateAfterFailure();
			throw;
		}
	}

	/// <summary>Writes one byte through the same cursor synchronization and metadata invalidation.</summary>
	public override void WriteByte(byte value) {
		Span<byte> buffer = stackalloc byte[1];
		buffer[0] = value;
		Write(buffer);
	}

	/// <inheritdoc />
	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
		ValidateBufferArguments(buffer, offset, count);
		return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
	}

	/// <summary>Synchronizes read-ahead before awaiting the source's immediate asynchronous write.</summary>
	public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
		EnsureOpen();
		cancellationToken.ThrowIfCancellationRequested();
		if (buffer.IsEmpty)
			return;

		_streamLengthKnown = false;
		if (!_enableReadBuffering) {
			await InnerStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
			return;
		}

		FlushUnreadData();
		try {
			await InnerStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
			AdvancePosition(buffer.Length);
		} catch {
			InvalidateAfterFailure();
			throw;
		}
	}

	/// <summary>Runs the legacy asynchronous write through this decorator's cursor synchronization.</summary>
	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
		ValidateBufferArguments(buffer, offset, count);
		EnsureOpen();
		var task = Task.Factory.StartNew(_ => Write(buffer, offset, count), state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		if (callback != null)
			_ = task.ContinueWith(result => callback(result), TaskScheduler.Default);
		return task;
	}

	/// <summary>Completes the write started by this decorator and propagates its original failure.</summary>
	public override void EndWrite(IAsyncResult asyncResult) {
		Guard.ArgumentNotNull(asyncResult, nameof(asyncResult));
		Guard.Argument(asyncResult is Task, nameof(asyncResult), "Expected the result returned by BeginWrite.");
		((Task)asyncResult).GetAwaiter().GetResult();
	}

	/// <summary>Seeks in the retained window when possible, otherwise forwards an adjusted logical offset.</summary>
	public override long Seek(long offset, SeekOrigin origin) {
		EnsureOpen();
		Guard.Argument(origin is SeekOrigin.Begin or SeekOrigin.Current or SeekOrigin.End, nameof(origin), "Unknown seek origin.");
		if (_enableReadBuffering && origin == SeekOrigin.Current && offset == 0)
			return Position;

		var adjusted = offset;
		if (_enableReadBuffering) {
			if (origin == SeekOrigin.Begin && TrySeekInBuffer(offset))
				return offset;
			if (origin == SeekOrigin.Current) {
				if (_positionKnown && TrySeekInBuffer(checked(_position + offset)))
					return _position;
				adjusted = checked(offset - UnreadCount);
			}
		}

		try {
			var position = InnerStream.Seek(adjusted, origin);
			ValidatePosition(position);
			DiscardReadBuffer();
			_position = position;
			_positionKnown = _enableReadBuffering;
			return position;
		} catch {
			// A rejected seek must retain unread bytes; a later explicit position query can rediscover the cursor.
			_positionKnown = false;
			throw;
		}
	}

	/// <summary>Resizes the source after returning unread data; subsequent metadata queries obtain its actual state.</summary>
	public override void SetLength(long value) {
		EnsureOpen();
		Guard.ArgumentNotNegative(value, nameof(value));
		_streamLengthKnown = false;
		if (!_enableReadBuffering) {
			InnerStream.SetLength(value);
			return;
		}

		FlushUnreadData();
		try {
			InnerStream.SetLength(value);
			_positionKnown = false;
		} catch {
			InvalidateAfterFailure();
			throw;
		}
	}

	/// <summary>Forwards output flushing without implicitly rewinding or discarding unread data.</summary>
	public override void Flush() {
		EnsureOpen();
		InnerStream.Flush();
	}

	/// <inheritdoc />
	public override Task FlushAsync(CancellationToken cancellationToken) {
		EnsureOpen();
		cancellationToken.ThrowIfCancellationRequested();
		return InnerStream.FlushAsync(cancellationToken);
	}

	/// <summary>Enables both settings, allocating read-ahead before changing either disabled setting.</summary>
	public void EnableBuffering() {
		EnableReadBuffering = true;
		EnableStreamLengthCaching = true;
	}

	/// <summary>Disables both settings, rejecting unread data before changing either setting.</summary>
	public void DisableBuffering() {
		EnableReadBuffering = false;
		EnableStreamLengthCaching = false;
	}

	/// <summary>Returns unread bytes to the source cursor; a failed rewind leaves those bytes available.</summary>
	public void FlushUnreadData() {
		EnsureOpen();
		var unread = UnreadCount;
		if (unread > 0) {
			try {
				var position = InnerStream.Seek(-unread, SeekOrigin.Current);
				ValidatePosition(position);
				_position = position;
				_positionKnown = true;
			} catch {
				_positionKnown = false;
				throw;
			}
		}
		DiscardReadBuffer();
	}

	/// <summary>Returns unread data to the source using <see cref="FlushUnreadData"/>.</summary>
	public void SynchronizePosition() => FlushUnreadData();

	/// <summary>Closes this decorator while respecting its source ownership policy.</summary>
	public override void Close() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>Releases the private buffer and awaits only an owned source's asynchronous disposal.</summary>
	public override async ValueTask DisposeAsync() {
		if (_disposed)
			return;

		_disposed = true;
		_buffer = Array.Empty<byte>();
		DiscardReadBuffer();
		if (!LeaveOpen)
			await InnerStream.DisposeAsync().ConfigureAwait(false);
		GC.SuppressFinalize(this);
	}

	/// <summary>Releases the private buffer and closes only an owned source, without flushing or seeking.</summary>
	protected override void Dispose(bool disposing) {
		if (_disposed)
			return;

		_disposed = true;
		_buffer = Array.Empty<byte>();
		DiscardReadBuffer();
		if (disposing && !LeaveOpen)
			InnerStream.Dispose();
	}

	// Keep the borrowed copy buffer alive until both decorated reads and destination writes complete.
	private async Task CopyBufferedToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
		using var bufferScope = Tools.Scope.EnterBorrowArrayScope(bufferSize);
		var buffer = bufferScope.Item.AsMemory(0, bufferSize);
		int count;
		while ((count = await ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
			await destination.WriteAsync(buffer[..count], cancellationToken).ConfigureAwait(false);
	}

	// Consume only retained bytes and update an already-known logical cursor without probing the source.
	private int CopyBufferedBytes(Span<byte> destination) {
		var count = Math.Min(UnreadCount, destination.Length);
		_buffer.AsSpan(_bufferOffset, count).CopyTo(destination);
		_bufferOffset += count;
		AdvancePosition(count);
		return count;
	}

	// An absolute position is queried only at the caller's explicit metadata boundary.
	private long ReadSourcePosition() {
		var position = InnerStream.Position;
		ValidatePosition(position);
		return position;
	}

	// Retain consumed bytes for inexpensive backwards seeks inside the current read-ahead window.
	private bool TrySeekInBuffer(long position) {
		if (!_positionKnown)
			return false;

		var start = _position - _bufferOffset;
		if (position < start || position - start > _bufferCount)
			return false;

		_bufferOffset = (int)(position - start);
		_position = position;
		return true;
	}

	// Advance a discovered cursor; sequential reads otherwise leave absolute position unknown.
	private void AdvancePosition(int count) {
		if (_positionKnown)
			_position = checked(_position + count);
	}

	// Forget retained bytes without asking the source to rewind or reveal its position.
	private void DiscardReadBuffer() {
		_bufferOffset = 0;
		_bufferCount = 0;
	}

	// A failing read or write can already have advanced its source before throwing.
	private void InvalidateAfterFailure() {
		DiscardReadBuffer();
		_positionKnown = false;
	}

	// Invalid custom-source results violate the .NET Stream contract and must not become cached state.
	private static void ValidateReadCount(int count, int requested) {
		if ((uint)count > (uint)requested)
			throw new IOException("The source returned an invalid read count.");
	}

	// Negative metadata sentinels used by some Pascal streams have no valid .NET Stream equivalent.
	private static void ValidatePosition(long position) {
		if (position < 0)
			throw new IOException("The source returned a negative stream position.");
	}

	// Enforce this wrapper's lifetime independently of a borrowed source's lifetime.
	private void EnsureOpen() => ObjectDisposedException.ThrowIf(_disposed, this);
}
