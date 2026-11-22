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
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// A buffer whose contents are paged in and out of memory and suitable for arbitrarily large buffers. Pages swap either to temporary files of
/// their own, or to their regions of a backing stream whose content the buffer then holds.
/// </summary>
/// <remarks>
/// The underlying implementation relies on a <see cref="MemoryPagedListBase{TItem}"/> whose pages are <see cref="MemoryBuffer"/>'s. With a backing
/// stream, page <c>n</c> holds the stream's bytes from <c>n × PageSize</c>; a dirty page is written when it is evicted or saved, so many small writes
/// to a cached page cost one stream write. The stream grows as pages are written and is resized to the buffer's length by <see cref="Save"/> and
/// <see cref="Flush"/>. Use the backing stream only through the buffer until disposal.
/// </remarks>
public class MemoryPagedBuffer : MemoryPagedListBase<byte>, IMemoryPagedBuffer {
	private readonly IPagedListDelegate<byte> _friend;

	/// <summary>Creates an empty buffer whose pages swap to temporary files and whose contents last for this instance only.</summary>
	/// <param name="pageSize">The bytes in each page.</param>
	/// <param name="maxMemory">The bytes of pages that may be cached at once.</param>
	/// <param name="pageReapPolicy">The order in which cached pages are evicted when <paramref name="maxMemory"/> is reached.</param>
	public MemoryPagedBuffer(long pageSize, long maxMemory, CacheReapPolicy pageReapPolicy = Sphere10FrameworkDefaults.PageCacheReapPolicy)
		: base(pageSize, maxMemory, pageReapPolicy: pageReapPolicy) {
		_friend = CreateFriendDelegate();
		Pages = InternalPages.AsReadOnly().WithProjection(x => (IBufferPage)x);
	}

	/// <summary>Creates a buffer over the content of a readable, seekable stream whose pages swap to their regions of that stream.</summary>
	/// <param name="backingStream">The stream holding the buffer's content. It must also be writable for the buffer to change.</param>
	/// <param name="pageSize">The bytes in each page.</param>
	/// <param name="maxMemory">The bytes of pages that may be cached at once.</param>
	/// <param name="leaveOpen">Whether disposing the buffer leaves <paramref name="backingStream"/> open.</param>
	/// <param name="autoLoad">Whether to map existing content now rather than on <see cref="PagedListBase{TItem}.Load"/>.</param>
	/// <param name="pageReapPolicy">The order in which cached pages are evicted when <paramref name="maxMemory"/> is reached.</param>
	public MemoryPagedBuffer(Stream backingStream, long pageSize, long maxMemory, bool leaveOpen = true, bool autoLoad = true,
		CacheReapPolicy pageReapPolicy = Sphere10FrameworkDefaults.PageCacheReapPolicy)
		: this(pageSize, maxMemory, pageReapPolicy) {
		Guard.ArgumentNotNull(backingStream, nameof(backingStream));
		Guard.Argument(backingStream.CanRead && backingStream.CanSeek, nameof(backingStream), "A backing stream must be readable and seekable.");
		BackingStream = backingStream;
		LeaveOpen = leaveOpen;
		FlushOnDispose = backingStream.CanWrite;
		RequiresLoad = backingStream.Length > 0;
		if (autoLoad)
			Load();
	}

	public new IReadOnlyList<IBufferPage> Pages { get; }

	/// <summary>The stream holding the pages, or null when they swap to temporary files.</summary>
	public Stream BackingStream { get; }

	/// <summary>Whether disposing the buffer leaves <see cref="BackingStream"/> open.</summary>
	public bool LeaveOpen { get; }

	public override bool IsReadOnly => BackingStream is { CanWrite: false };

	public override long Count {
		get {
			CheckLoaded();
			return base.Count;
		}
	}

	public override IEnumerable<byte> ReadRange(long index, long count)
		=> PagedBufferImplementationHelper.ReadRange(_friend, index, count);

	/// <summary>Copies bytes into caller-owned storage. Override this method to customize destination reads.</summary>
	public virtual void ReadRange(long index, Span<byte> destination)
		=> PagedBufferImplementationHelper.ReadRange(_friend, index, destination);

	public ReadOnlySpan<byte> ReadSpan(long index, long count)
		=> PagedBufferImplementationHelper.ReadRange(_friend, index, count);

	public override void AddRange(IEnumerable<byte> items)
		=> AddRange((items as byte[] ?? items?.ToArray()).AsSpan());

	public void AddRange(ReadOnlySpan<byte> span)
		=> PagedBufferImplementationHelper.AddRange(_friend, span);

	public override void UpdateRange(long index, IEnumerable<byte> items)
		=> UpdateRange(index, (items as byte[] ?? items?.ToArray()).AsSpan());

	public void UpdateRange(long index, ReadOnlySpan<byte> items)
		=> PagedBufferImplementationHelper.UpdateRange(_friend, index, items);

	public override void InsertRange(long index, IEnumerable<byte> items)
		=> InsertRange(index, (items as byte[] ?? items?.ToArray()).AsSpan());

	public void InsertRange(long index, ReadOnlySpan<byte> items)
		=> PagedBufferImplementationHelper.InsertRange(_friend, Count, index, items);

	public Span<byte> AsSpan(long index, long count)
		=> PagedBufferImplementationHelper.AsSpan(_friend, index, count);

	public void ExpandTo(long totalBytes) => PagedBufferImplementationHelper.ExpandTo(_friend, Count, totalBytes);

	public void ExpandBy(long newBytes) => PagedBufferImplementationHelper.ExpandBy(_friend, newBytes);

	/// <summary>Saves the dirty cached pages, keeping them cached, then resizes and flushes the backing stream.</summary>
	public override void Save() {
		base.Save();
		CommitBackingStream();
	}

	/// <summary>Saves the dirty pages and unloads every cached page, then resizes and flushes the backing stream.</summary>
	public override void Flush() {
		base.Flush();
		CommitBackingStream();
	}

	public override void Dispose() {
		try {
			base.Dispose();
		} finally {
			if (BackingStream != null && !LeaveOpen)
				BackingStream.Dispose();
		}
	}

	protected override IPage<byte> NewPageInstance(long pageNumber)
		=> BackingStream is null ? new BufferPage(PageSize) : new StreamBufferPage(BackingStream, pageNumber, PageSize);

	protected override IPage<byte>[] LoadPages() {
		if (BackingStream is null)
			throw new NotSupportedException("Pages swapped to temporary files are not loadable across runtime sessions. Construct the buffer over a backing stream.");
		var length = BackingStream.Length;
		var pages = new IPage<byte>[checked((int)((length + PageSize - 1) / PageSize))];
		for (var number = 0; number < pages.Length; number++) {
			var startIndex = number * PageSize;
			var count = Math.Min(PageSize, length - startIndex);
			pages[number] = new StreamBufferPage(BackingStream, number, PageSize) {
				Number = number,
				StartIndex = startIndex,
				EndIndex = startIndex + count - 1,
				Count = count,
				Size = count,
				State = PageState.Unloaded
			};
		}
		return pages;
	}

	protected override void OnPageSaving(IMemoryPage<byte> page) {
		base.OnPageSaving(page);
		// A page beyond the end is written from its region's start, which streams such as ExtendedMemoryStream cannot seek past.
		if (BackingStream is not null && BackingStream.Length < page.StartIndex)
			BackingStream.SetLength(page.StartIndex);
	}

	/// <summary>Flushes the backing stream after a save. Override this method to flush through to the device.</summary>
	protected virtual void FlushBackingStream() => BackingStream.Flush();

	private void CommitBackingStream() {
		if (BackingStream is not { CanWrite: true } || RequiresLoad)
			return;
		// Every page that is not cached was saved when evicted, so the stream now holds all content and only a buffered shrink remains.
		if (BackingStream.Length != base.Count)
			BackingStream.SetLength(base.Count);
		FlushBackingStream();
	}

	#region Inner Types

	/// <summary>
	/// The page is mapped to it's own page file.
	/// </summary>
	public sealed class BufferPage : FileSwappedMemoryPage<byte>, IBufferPage {

		public BufferPage(long pageSize)
			: base(pageSize, new ConstantLengthItemSizer<byte>(sizeof(byte), false), new MemoryBuffer(0, pageSize, pageSize)) {
		}

		public ReadOnlySpan<byte> ReadSpan(long index, long count)
			=> PagedBufferImplementationHelper.ReadPageSpan(this, MemoryStore as MemoryBuffer, index, count);

		public bool AppendSpan(ReadOnlySpan<byte> items, out ReadOnlySpan<byte> overflow)
			=> PagedBufferImplementationHelper.AppendPageSpan(this, MemoryStore as MemoryBuffer, items, out overflow);

		public void UpdateSpan(long index, ReadOnlySpan<byte> items) =>
			PagedBufferImplementationHelper.UpdatePageSpan(this, MemoryStore as MemoryBuffer, index, items);

		protected override void SaveInternal(IExtendedList<byte> memoryPage, Stream stream) {
			var memBuff = (MemoryBuffer)memoryPage;
			if (stream.Length != memBuff.Count)
				stream.SetLength(memBuff.Count); // eliminate unused end-space in page file (happens when add, save, remove, save)
			stream.Write(memBuff.AsSpan());
		}

		protected override void LoadInternal(Stream stream, IExtendedList<byte> memoryPage) {
			// Use byte streaming for perf
			var memBuff = (MemoryBuffer)memoryPage;
			memBuff.ExpandTo((int)stream.Length); // note: underlying allocation is still done in chunks of page-size
			stream.ReadExactly(memBuff.AsSpan());
		}
	}

	/// <summary>
	/// The page is mapped to its region of the buffer's backing stream.
	/// </summary>
	public sealed class StreamBufferPage : StreamSwappedMemoryPage<byte>, IBufferPage {

		public StreamBufferPage(Stream stream, long pageNumber, long pageSize)
			: base(stream, pageNumber, pageSize, new ConstantLengthItemSizer<byte>(sizeof(byte), false), new MemoryBuffer(0, pageSize, pageSize)) {
		}

		public ReadOnlySpan<byte> ReadSpan(long index, long count)
			=> PagedBufferImplementationHelper.ReadPageSpan(this, MemoryStore as MemoryBuffer, index, count);

		public bool AppendSpan(ReadOnlySpan<byte> items, out ReadOnlySpan<byte> overflow)
			=> PagedBufferImplementationHelper.AppendPageSpan(this, MemoryStore as MemoryBuffer, items, out overflow);

		public void UpdateSpan(long index, ReadOnlySpan<byte> items)
			=> PagedBufferImplementationHelper.UpdatePageSpan(this, MemoryStore as MemoryBuffer, index, items);

		protected override void SaveInternal(IExtendedList<byte> memoryPage, Stream stream) => stream.Write(((MemoryBuffer)memoryPage).AsSpan());

		protected override void LoadInternal(Stream stream, IExtendedList<byte> memoryPage) {
			// Read only this page's bytes: a shrink not yet saved can leave older bytes after them in the region.
			var memBuff = (MemoryBuffer)memoryPage;
			memBuff.ExpandBy(Count - memBuff.Count);
			stream.ReadExactly(memBuff.AsSpan());
		}
	}

	#endregion
}
