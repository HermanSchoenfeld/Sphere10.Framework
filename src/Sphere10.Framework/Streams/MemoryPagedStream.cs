// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;

namespace Sphere10.Framework;

/// <summary>
/// A stream over an <see cref="IMemoryPagedBuffer"/>, so only a bounded set of its pages is held in memory. Over a backing stream it reads and
/// writes that stream through the page cache: many small random reads and writes to a cached page cost one access to the backing stream.
/// </summary>
/// <remarks>
/// <see cref="Flush"/> saves the dirty pages and keeps them cached, so a caller that flushes after every operation keeps its working set. Over a
/// backing stream it then resizes and flushes that stream; use the backing stream only through this stream until disposal. Disposing the stream
/// disposes an owned buffer, which saves it once; a failure is reported and not retried by a later disposal. Instances are not thread safe.
/// </remarks>
public class MemoryPagedStream : ExtendedMemoryStream {
	public const long DefaultPageSize = 64 * 1024;
	public const long DefaultMaxMemory = 16 * DefaultPageSize;
	private bool _disposed;

	/// <summary>Creates a stream over a memory-paged buffer.</summary>
	public MemoryPagedStream(IMemoryPagedBuffer buffer, bool disposeBuffer = true)
		: base(buffer, disposeBuffer) {
		Buffer = buffer;
	}

	/// <summary>Creates a stream over the content of a readable, seekable stream, read and written through a page cache.</summary>
	/// <param name="backingStream">The stream holding the content. It must also be writable for this stream to change it.</param>
	/// <param name="pageSize">The bytes in each page.</param>
	/// <param name="maxMemory">The bytes of pages that may be cached at once.</param>
	/// <param name="leaveOpen">Whether disposing this stream leaves <paramref name="backingStream"/> open.</param>
	/// <param name="pageReapPolicy">The order in which cached pages are evicted when <paramref name="maxMemory"/> is reached.</param>
	public MemoryPagedStream(Stream backingStream, long pageSize = DefaultPageSize, long maxMemory = DefaultMaxMemory, bool leaveOpen = true,
		CacheReapPolicy pageReapPolicy = Sphere10FrameworkDefaults.PageCacheReapPolicy)
		: this(new MemoryPagedBuffer(backingStream, pageSize, maxMemory, leaveOpen, pageReapPolicy: pageReapPolicy)) {
	}

	/// <summary>The paged buffer holding this stream's bytes.</summary>
	public IMemoryPagedBuffer Buffer { get; }

	public override bool CanRead => !_disposed;

	public override bool CanSeek => !_disposed;

	public override bool CanWrite => !_disposed && !Buffer.IsReadOnly;

	public override long Length {
		get {
			EnsureOpen();
			return base.Length;
		}
	}

	public override long Position {
		get {
			EnsureOpen();
			return base.Position;
		}
		set {
			EnsureOpen();
			base.Position = value;
		}
	}

	public override void Flush() {
		EnsureOpen();
		Buffer.Save();
	}

	protected override void Dispose(bool disposing) {
		if (_disposed)
			return;
		_disposed = true;
		base.Dispose(disposing);
	}

	private void EnsureOpen() => ObjectDisposedException.ThrowIf(_disposed, this);
}
