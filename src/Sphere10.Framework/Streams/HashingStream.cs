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

public sealed class HashingStream : WriteOnlyStream {
	private readonly IHashFunction _hashFunction;
	private readonly Hashers.HasherScope _hasherScope;
	private bool _canWrite;
	private bool _disposed;

	public HashingStream(CHF hashFunction)
		: base(new MemoryStream()) {
		_hasherScope = Hashers.EnterBorrowHasherScope(hashFunction, out _hashFunction);
		_canWrite = true;
	}

	public byte[] GetDigest() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		_canWrite = false;
		return _hashFunction.GetResult();
	}

	public override void Write(byte[] buffer, int offset, int count) {
		CheckCanWrite();
		_hashFunction.Transform(buffer.AsSpan(offset, count));
	}

	public override void Write(ReadOnlySpan<byte> buffer) {
		CheckCanWrite();
		_hashFunction.Transform(buffer);
	}

	public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
		CheckCanWrite();
		_hashFunction.Transform(buffer.AsSpan(offset, count));
	}

	public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken()) {
		CheckCanWrite();
		_hashFunction.Transform(buffer.Span);
	}

	public override void WriteByte(byte value) {
		CheckCanWrite();
		Span<byte> singleByte = stackalloc byte[1];
		singleByte[0] = value;
		_hashFunction.Transform(singleByte);
	}

	public override void Close() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected override void Dispose(bool disposing) {
		if (_disposed)
			return;
		base.Dispose(disposing);
		_hasherScope.Dispose();
		_canWrite = false;
		_disposed = true;
	}

	private void CheckCanWrite() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (!_canWrite)
			throw new InvalidOperationException("Hashing stream cannot get written to after digest has been computed");
	}
}

