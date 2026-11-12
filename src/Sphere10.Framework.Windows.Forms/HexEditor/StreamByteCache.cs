// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A bounded, least-recently-used block cache over a seekable stream. Only the blocks a viewport touches are
/// fetched, and never more than <see cref="MaxBlocks"/> blocks of <see cref="BlockSize"/> bytes are retained,
/// so arbitrarily large streams can be browsed without loading them into memory.
/// </summary>
public class StreamByteCache {
	public const int DefaultBlockSize = 4096;
	public const int DefaultMaxBlocks = 64;
	private readonly Stream _stream;
	private readonly Dictionary<long, LinkedListNode<CacheBlock>> _blocks;
	private readonly LinkedList<CacheBlock> _recency;
	private long _cachedBytes;

	public StreamByteCache(Stream stream)
		: this(stream, DefaultBlockSize, DefaultMaxBlocks) {
	}

	public StreamByteCache(Stream stream, int blockSize, int maxBlocks) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Guard.Argument(stream.CanSeek, nameof(stream), "The stream must support seeking.");
		Guard.Argument(stream.CanRead, nameof(stream), "The stream must support reading.");
		Guard.ArgumentGT(blockSize, 0, nameof(blockSize));
		Guard.ArgumentGT(maxBlocks, 0, nameof(maxBlocks));
		_stream = stream;
		BlockSize = blockSize;
		MaxBlocks = maxBlocks;
		_blocks = new Dictionary<long, LinkedListNode<CacheBlock>>();
		_recency = new LinkedList<CacheBlock>();
	}

	/// <summary>The number of bytes fetched from the stream per block.</summary>
	public int BlockSize { get; }

	/// <summary>The maximum number of blocks retained at once.</summary>
	public int MaxBlocks { get; }

	/// <summary>The memory budget of the cache: <see cref="BlockSize"/> multiplied by <see cref="MaxBlocks"/>.</summary>
	public long MaxCachedBytes => (long)BlockSize * MaxBlocks;

	/// <summary>The number of blocks currently retained.</summary>
	public int CachedBlockCount => _blocks.Count;

	/// <summary>The number of buffer bytes currently retained, never exceeding <see cref="MaxCachedBytes"/>.</summary>
	public long CachedBytes => _cachedBytes;

	/// <summary>The number of block fetches performed against the stream since construction or the last <see cref="Clear"/>.</summary>
	public long BlockFetchCount { get; private set; }

	/// <summary>The number of bytes read from the stream since construction or the last <see cref="Clear"/>.</summary>
	public long BytesFetched { get; private set; }

	/// <summary>Reads bytes starting at an absolute offset, returning how many were available before the end of the stream.</summary>
	public int Read(long offset, Span<byte> destination) {
		Guard.ArgumentNotNegative(offset, nameof(offset));
		var total = 0;
		while (!destination.IsEmpty) {
			// Serve each block-aligned slice from the retained block, fetching it when absent.
			var blockIndex = offset / BlockSize;
			var block = GetBlock(blockIndex);
			var blockOffset = (int)(offset - blockIndex * BlockSize);
			var available = block.Length - blockOffset;
			if (available <= 0)
				break;
			var count = Math.Min(available, destination.Length);
			block.Data.AsSpan(blockOffset, count).CopyTo(destination);
			destination = destination.Slice(count);
			offset += count;
			total += count;
			if (block.Length < BlockSize)
				break;
		}
		return total;
	}

	/// <summary>Discards retained blocks overlapping a byte range, so later reads observe the stream's current content.</summary>
	public void Invalidate(long offset, long count) {
		Guard.ArgumentNotNegative(offset, nameof(offset));
		Guard.ArgumentNotNegative(count, nameof(count));
		if (count == 0)
			return;
		var firstBlock = offset / BlockSize;
		var lastBlock = (offset + count - 1) / BlockSize;
		for (var blockIndex = firstBlock; blockIndex <= lastBlock; blockIndex++)
			if (_blocks.Remove(blockIndex, out var node))
				RemoveNode(node);
	}

	/// <summary>Discards every retained block and resets the fetch statistics.</summary>
	public void Clear() {
		_blocks.Clear();
		_recency.Clear();
		_cachedBytes = 0;
		BlockFetchCount = 0;
		BytesFetched = 0;
	}

	// Blocks are promoted to most-recent on every access; fetching evicts the least recent block beyond the budget.
	private CacheBlock GetBlock(long blockIndex) {
		if (_blocks.TryGetValue(blockIndex, out var existing)) {
			_recency.Remove(existing);
			_recency.AddFirst(existing);
			return existing.Value;
		}
		var block = FetchBlock(blockIndex);
		if (block.Length == 0)
			return block;
		var node = _recency.AddFirst(block);
		_blocks.Add(blockIndex, node);
		_cachedBytes += block.Data.Length;
		while (_blocks.Count > MaxBlocks) {
			var oldest = _recency.Last;
			_blocks.Remove(oldest.Value.Index);
			RemoveNode(oldest);
		}
		return block;
	}

	// A block is filled with repeated reads because streams may return fewer bytes than requested.
	private CacheBlock FetchBlock(long blockIndex) {
		var data = new byte[BlockSize];
		var length = 0;
		_stream.Position = blockIndex * BlockSize;
		while (length < BlockSize) {
			var read = _stream.Read(data, length, BlockSize - length);
			if (read <= 0)
				break;
			length += read;
		}
		BlockFetchCount++;
		BytesFetched += length;
		return new CacheBlock(blockIndex, data, length);
	}

	private void RemoveNode(LinkedListNode<CacheBlock> node) {
		_recency.Remove(node);
		_cachedBytes -= node.Value.Data.Length;
	}

	#region Inner Types

	/// <summary>A fetched block and the number of valid bytes it holds (shorter only at the end of the stream).</summary>
	private sealed class CacheBlock {
		public CacheBlock(long index, byte[] data, int length) {
			Index = index;
			Data = data;
			Length = length;
		}

		public long Index { get; }

		public byte[] Data { get; }

		public int Length { get; }
	}

	#endregion
}
