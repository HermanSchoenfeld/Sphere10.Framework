// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>AES-CTR stream encryption that preserves byte length and supports independent random access.</summary>
/// <remarks>
/// Writes encrypt and reads decrypt. Partial blocks and arbitrary byte offsets are supported.
/// No header, padding or authentication tag is stored in the stream. Store the initial counter separately.
/// Never reuse key/counter blocks for different plaintext, including overwritten revisions. Authenticate
/// ciphertext separately before decrypting untrusted input. Instances are not thread-safe.
/// Seekable streams use absolute positions from zero; non-seekable streams start at the current underlying position.
/// Do not access the underlying stream concurrently or resume after an underlying I/O failure.
/// The key and initial counter are copied. LeaveOpen applies only to the backing stream.
/// </remarks>
public class EncryptedStream<TStream> : StreamDecorator<TStream> where TStream : Stream {
	private const int BufferSize = 81920;
	private const int BlockSize = 16;
	private readonly Aes _aes;
	private readonly byte[] _initialCounter;
	private readonly byte[] _keyStream;
	private long _cachedBlock = -1;
	private readonly bool _leaveOpen;
	private long _position;
	private bool _disposed;

	public EncryptedStream(TStream stream, byte[] key, byte[] initialCounter, bool leaveOpen = false)
		: base(stream) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Guard.Argument(stream.CanRead || stream.CanWrite, nameof(stream), "Stream must be readable or writable.");
		Guard.ArgumentNotNull(key, nameof(key));
		Guard.Argument(key.Length is 16 or 24 or 32, nameof(key), "AES requires a 16-, 24- or 32-byte key.");
		Guard.ArgumentNotNull(initialCounter, nameof(initialCounter));
		Guard.Argument(initialCounter.Length == BlockSize, nameof(initialCounter), "AES-CTR requires a 16-byte initial counter.");
		_initialCounter = (byte[])initialCounter.Clone();
		_keyStream = new byte[BlockSize];
		_aes = Aes.Create();
		try {
			_aes.Key = key;
			// ECB encrypts counter blocks only; plaintext is encrypted by CTR below, without padding.
			_aes.EncryptEcb(_initialCounter, _keyStream, PaddingMode.None);
			CryptographicOperations.ZeroMemory(_keyStream);
		} catch {
			CryptographicOperations.ZeroMemory(_keyStream);
			_aes.Dispose();
			throw;
		}
		_leaveOpen = leaveOpen;
	}

	public override bool CanRead => !_disposed && InnerStream.CanRead;

	public override bool CanWrite => !_disposed && InnerStream.CanWrite;

	public override bool CanSeek => !_disposed && InnerStream.CanSeek;

	public override long Length {
		get {
			EnsureOpen();
			return InnerStream.Length;
		}
	}

	public override long Position {
		get {
			EnsureOpen();
			return InnerStream.Position;
		}
		set {
			EnsureOpen();
			Guard.ArgumentNotNegative(value, nameof(value));
			InnerStream.Position = value;
		}
	}

	private long CipherPosition => InnerStream.CanSeek ? InnerStream.Position : _position;

	public override int Read(byte[] buffer, int offset, int count) {
		ValidateBuffer(buffer, offset, count);
		return Read(buffer.AsSpan(offset, count));
	}

	public override int Read(Span<byte> buffer) {
		EnsureOpen();
		var position = CipherPosition;
		var readCount = GetReadCount(position, buffer.Length);
		ValidateRange(position, readCount);
		var count = InnerStream.Read(buffer[..readCount]);
		Transform(buffer[..count], position);
		_position = checked(position + count);
		return count;
	}

	public override int ReadByte() {
		Span<byte> buffer = stackalloc byte[1];
		return Read(buffer) == 0 ? -1 : buffer[0];
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
		ValidateBuffer(buffer, offset, count);
		return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
	}

	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
		EnsureOpen();
		cancellationToken.ThrowIfCancellationRequested();
		var position = CipherPosition;
		var readCount = GetReadCount(position, buffer.Length);
		ValidateRange(position, readCount);
		var count = await InnerStream.ReadAsync(buffer[..readCount], cancellationToken).ConfigureAwait(false);
		Transform(buffer.Span[..count], position);
		_position = checked(position + count);
		return count;
	}

	public override void Write(byte[] buffer, int offset, int count) {
		ValidateBuffer(buffer, offset, count);
		Write(buffer.AsSpan(offset, count));
	}

	public override void Write(ReadOnlySpan<byte> buffer) {
		EnsureWritableRange(buffer.Length);
		if (buffer.IsEmpty)
			return;
		var scratch = ArrayPool<byte>.Shared.Rent(Math.Min(BufferSize, buffer.Length));
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => ArrayPool<byte>.Shared.Return(scratch, clearArray: true));
		while (!buffer.IsEmpty) {
			var count = Math.Min(scratch.Length, buffer.Length);
			var position = CipherPosition;
			buffer[..count].CopyTo(scratch);
			Transform(scratch.AsSpan(0, count), position);
			InnerStream.Write(scratch, 0, count);
			_position = checked(position + count);
			buffer = buffer[count..];
		}
	}

	public override void WriteByte(byte value) {
		Span<byte> buffer = stackalloc byte[1];
		buffer[0] = value;
		Write(buffer);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
		ValidateBuffer(buffer, offset, count);
		return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
	}

	public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		EnsureWritableRange(buffer.Length);
		if (buffer.IsEmpty)
			return;
		var scratch = ArrayPool<byte>.Shared.Rent(Math.Min(BufferSize, buffer.Length));
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => ArrayPool<byte>.Shared.Return(scratch, clearArray: true));
		while (!buffer.IsEmpty) {
			var count = Math.Min(scratch.Length, buffer.Length);
			var position = CipherPosition;
			buffer.Span[..count].CopyTo(scratch);
			Transform(scratch.AsSpan(0, count), position);
			await InnerStream.WriteAsync(scratch.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
			_position = checked(position + count);
			buffer = buffer[count..];
		}
	}

	public override long Seek(long offset, SeekOrigin origin) {
		EnsureOpen();
		return InnerStream.Seek(offset, origin);
	}

	public override void SetLength(long value) {
		EnsureOpen();
		// Growing the backing stream with raw zeroes would decrypt to non-zero plaintext.
		throw new NotSupportedException("Resize by writing encrypted bytes to a new stream with a fresh counter.");
	}

	public override void Flush() {
		EnsureOpen();
		InnerStream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken) {
		EnsureOpen();
		return InnerStream.FlushAsync(cancellationToken);
	}

	public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
		Guard.ArgumentNotNull(destination, nameof(destination));
		Guard.ArgumentGT(bufferSize, 0, nameof(bufferSize));
		EnsureOpen();
		var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => ArrayPool<byte>.Shared.Return(buffer, clearArray: true));
		int count;
		while ((count = await ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken).ConfigureAwait(false)) != 0)
			await destination.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
	}

	// StreamDecorator forwards these directly to the backing stream; route every entry point through encryption instead.
	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
		ValidateBuffer(buffer, offset, count);
		EnsureOpen();
		var task = Task<int>.Factory.StartNew(_ => Read(buffer, offset, count), state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		if (callback != null)
			_ = task.ContinueWith(result => callback(result), TaskScheduler.Default);
		return task;
	}

	public override int EndRead(IAsyncResult asyncResult) {
		Guard.Argument(asyncResult is Task<int>, nameof(asyncResult), "Expected the result returned by BeginRead.");
		return ((Task<int>)asyncResult).GetAwaiter().GetResult();
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
		ValidateBuffer(buffer, offset, count);
		EnsureOpen();
		var task = Task.Factory.StartNew(_ => Write(buffer, offset, count), state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		if (callback != null)
			_ = task.ContinueWith(result => callback(result), TaskScheduler.Default);
		return task;
	}

	public override void EndWrite(IAsyncResult asyncResult) {
		Guard.Argument(asyncResult is Task, nameof(asyncResult), "Expected the result returned by BeginWrite.");
		((Task)asyncResult).GetAwaiter().GetResult();
	}

	public override void Close() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected override void Dispose(bool disposing) {
		if (_disposed)
			return;
		_disposed = true;
		if (!disposing)
			return;
		using var cleanup = Tools.Scope.ExecuteOnDispose(_aes.Dispose);
		CryptographicOperations.ZeroMemory(_keyStream);
		if (!_leaveOpen)
			InnerStream.Dispose();
	}

	private void EnsureOpen() => ObjectDisposedException.ThrowIf(_disposed, this);

	private void EnsureWritableRange(int count) {
		EnsureOpen();
		if (!InnerStream.CanWrite)
			throw new NotSupportedException("The underlying stream is not writable.");
		Guard.Ensure(!InnerStream.CanSeek || count == 0 || InnerStream.Position <= InnerStream.Length, "Writing past the end would create an unencrypted gap.");
		ValidateRange(CipherPosition, count);
	}

	private int GetReadCount(long position, int count)
		=> InnerStream.CanSeek ? (int)Math.Min(count, Math.Max(0, InnerStream.Length - position)) : count;

	private void ValidateRange(long position, int count) {
		if (count == 0)
			return;
		var lastPosition = checked(position + (count - 1));
		Guard.Ensure(lastPosition < long.MaxValue, "The resulting stream position would overflow.");
		Span<byte> counter = stackalloc byte[BlockSize];
		GetCounter(lastPosition / BlockSize, counter);
	}

	private void GetCounter(long block, Span<byte> counter) {
		_initialCounter.CopyTo(counter);
		var carry = (ulong)block;
		for (var index = BlockSize - 1; index >= 0; index--) {
			carry += counter[index];
			counter[index] = (byte)carry;
			carry >>= 8;
		}
		Guard.Ensure(carry == 0, "CTR counter exhausted. A new key/counter is required.");
	}

	private void Transform(Span<byte> buffer, long position) {
		ValidateRange(position, buffer.Length);
		Span<byte> counter = stackalloc byte[BlockSize];
		while (!buffer.IsEmpty) {
			var block = position / BlockSize;
			if (_cachedBlock != block) {
				GetCounter(block, counter);
				_aes.EncryptEcb(counter, _keyStream, PaddingMode.None);
				_cachedBlock = block;
			}
			var offset = (int)(position % BlockSize);
			var count = Math.Min(BlockSize - offset, buffer.Length);
			for (var index = 0; index < count; index++)
				buffer[index] ^= _keyStream[offset + index];
			position += count;
			buffer = buffer[count..];
		}
	}

	private static void ValidateBuffer(byte[] buffer, int offset, int count) {
		Guard.ArgumentNotNull(buffer, nameof(buffer));
		Guard.ArgumentNotNegative(offset, nameof(offset));
		Guard.ArgumentNotNegative(count, nameof(count));
		Guard.Argument(offset <= buffer.Length && count <= buffer.Length - offset, nameof(count), "Buffer range is invalid.");
	}
}

/// <summary>Stream-typed decorator for length-preserving AES-CTR encryption.</summary>
public sealed class EncryptedStream : EncryptedStream<Stream> {
	public EncryptedStream(Stream stream, byte[] key, byte[] initialCounter, bool leaveOpen = false)
		: base(stream, key, initialCounter, leaveOpen) {
	}
}
