// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;

namespace Sphere10.Framework;

/// <summary>
/// A re-implementation of <see cref="PagedListBase{TItem}"/> methods optimized for spans and used by <see cref="IBuffer"/> implementations.
/// </summary>
/// <remarks>PagedBuffer span operations implemented in a not-so-great way. Relying on page operations to do the bulk insertions</remarks>
internal static class PagedBufferImplementationHelper {
	public static byte[] ReadRange(IPagedListDelegate<byte> source, long index, long count) {
		source.CheckRequiresLoad();
		source.NotifyAccessing();
		source.CheckRange(index, count, false);
		var length = Tools.Collection.CheckNotImplemented64bitAddressingLength(count);
		var result = length == 0 ? Array.Empty<byte>() : new byte[length];
		ReadRangeInternal(source, index, result);
		source.NotifyAccessed();
		return result;
	}

	public static void ReadRange(IPagedListDelegate<byte> source, long index, Span<byte> destination) {
		source.CheckRequiresLoad();
		source.NotifyAccessing();
		CheckReadRange(source, index, destination.Length);
		ReadRangeInternal(source, index, destination);
		source.NotifyAccessed();
	}

	public static void AddRange(IPagedListDelegate<byte> source, ReadOnlySpan<byte> span) {
		if (span.IsEmpty)
			return;
		source.CheckRequiresLoad();
		source.NotifyAccessing();
		var pages = source.InternalPages();
		var page = pages.Count > 0 ? pages[pages.Count - 1] : source.CreateNextPage();
		var bufferPage = (IBufferPage)page;
		source.NotifyPageAccessing(page);
		var fittedCompletely = false;
		source.DecCount(bufferPage.Count); // discount pages item count from total count
		while (!fittedCompletely) {
			using (source.EnterOpenPageScope(page)) {
				source.NotifyPageWriting(page);
				source.UpdateVersion();
				fittedCompletely = bufferPage.AppendSpan(span, out span);
				source.IncCount(bufferPage.Count); // add page item count to total count
				source.NotifyPageWrite(page);
			}
			source.NotifyPageAccessed(page);
			if (!fittedCompletely) {
				page = source.CreateNextPage() as IBufferPage;
				bufferPage = (IBufferPage)page;
			}
		}
		source.NotifyAccessed();
	}

	public static void UpdateRange(IPagedListDelegate<byte> source, long index, ReadOnlySpan<byte> items) {
		source.CheckRange(index, items.Length, false);
		source.CheckRequiresLoad();
		source.NotifyAccessing();

		if (items.Length == 0)
			return;

		foreach (var pageSegment in source.GetPageSegments(index, items.Length)) {
			var page = pageSegment.Item1;
			var bufferPage = (IBufferPage)page;
			var pageStartIx = pageSegment.Item2;
			var pageCount = pageSegment.Item3;
			var indexI = Tools.Collection.CheckNotImplemented64bitAddressingIndex(index);
			var pageStartIxI = Tools.Collection.CheckNotImplemented64bitAddressingIndex(pageStartIx);
			var pageCountI = Tools.Collection.CheckNotImplemented64bitAddressingLength(pageCount);
			var pageSpan = items.Slice(pageStartIxI - indexI, pageCountI);
			source.DecCount(bufferPage.Count);
			using (source.EnterOpenPageScope(page)) {
				source.NotifyPageAccessing(page);
				source.NotifyPageWriting(page);
				source.UpdateVersion();
				bufferPage.UpdateSpan(pageStartIxI, pageSpan);
				source.IncCount(bufferPage.Count);
				source.NotifyPageWrite(page);
			}
			source.NotifyPageAccessed(page);
		}
		source.NotifyAccessed();
	}

	public static void InsertRange(IPagedListDelegate<byte> source, in long count, in long index, in ReadOnlySpan<byte> items) {
		if (index == count)
			AddRange(source, items);
		else throw new NotSupportedException("This collection can only be appended from the end");
	}

	public static Span<byte> AsSpan(IPagedListDelegate<byte> source, long index, long count) {
		// TODO: this needs to check if the span range covers a contiguous region of a page, and if not throw
		// returns span for the page (needs to provide a locking scope to ensure the page isn't swapped)
		return ReadRange(source, index, count).AsSpan();
	}

	public static void ExpandTo(IPagedListDelegate<byte> source, long totalBytes) {
		throw new NotImplementedException();
	}

	public static void ExpandBy(IPagedListDelegate<byte> source, long newBytes) {
		throw new NotImplementedException();
	}


	public static ReadOnlySpan<byte> ReadPageSpan(MemoryPageBase<byte> page, MemoryBuffer memoryStore, long index, long count) {
		page.CheckPageState(PageState.Loaded);
		page.CheckRange(index, count);
		return memoryStore.ReadSpan(index - page.StartIndex, count);
	}

	public static bool AppendPageSpan(MemoryPageBase<byte> page, MemoryBuffer memoryStore, ReadOnlySpan<byte> items, out ReadOnlySpan<byte> overflow) {
		page.CheckPageState(PageState.Loaded);

		// Nothing to write case
		if (items.Length == 0) {
			overflow = ReadOnlySpan<byte>.Empty;
			return true;
		}

		var remainingPageBytes = page.MaxSize - page.Size;
		var writeSpan = items.Slice(0, (int)Math.Min(remainingPageBytes, items.Length));

		memoryStore.Write(page.Size, writeSpan);

		page.Count += writeSpan.Length;
		page.EndIndex += writeSpan.Length;
		page.Size += writeSpan.Length;

		var totalWriteCount = writeSpan.Length;

		// source.SetCount(source.GetCount() + writeSpan.Length); // When updating a page, the owner objectStream should be aware of acquiring the count

		if (page.Count == 0 && totalWriteCount == 0) // Was unable to write the first element in an empty page, item too large
			throw new InvalidOperationException("Span cannot be fitted onto a page of this collection");
		if (totalWriteCount > 0)
			page.Dirty = true;
		overflow = totalWriteCount < items.Length ? items.Slice(totalWriteCount) : ReadOnlySpan<byte>.Empty;

		Debug.Assert(totalWriteCount <= items.Length);
		return totalWriteCount == items.Length;
	}

	public static void UpdatePageSpan(MemoryPageBase<byte> page, MemoryBuffer memoryStore, in long index, in ReadOnlySpan<byte> items) {
		page.CheckPageState(PageState.Loaded);
		Guard.ArgumentInRange(index, page.StartIndex, Math.Max(page.StartIndex, page.EndIndex) + 1, nameof(index));
		Guard.Ensure(index - page.StartIndex + items.Length - 1 <= page.MaxSize, "Update span would not fit on page.");

		// Nothing to write case
		if (items.Length == 0)
			return;

		var newBytesCount = Math.Max(index - page.StartIndex + items.Length - 1, page.Size) - page.Size;
		memoryStore.Write(index - page.StartIndex, items);

		page.Count += newBytesCount;
		page.EndIndex += newBytesCount;
		page.Size += newBytesCount;
		page.Dirty = true;

		//source.SetCount(source.GetCount() + newBytesCount);  // When updating a page, the owner objectStream should be aware of acquiring the count
	}

	private static void CheckReadRange(IPagedListDelegate<byte> source, long index, long count) {
		if (count == 0 && source.InternalPages().Count == 0)
			Guard.ArgumentEquals(index, 0L, nameof(index));
		else
			source.CheckRange(index, count, false);
	}

	private static void ReadRangeInternal(IPagedListDelegate<byte> source, long index, Span<byte> destination) {
		var destinationOffset = 0;
		foreach (var pageSegment in source.GetPageSegments(index, destination.Length)) {
			var page = (IBufferPage)pageSegment.Item1;
			var pageStartIndex = pageSegment.Item2;
			var pageItemCount = pageSegment.Item3;
			source.NotifyPageAccessing(page);
			using (source.EnterOpenPageScope(page)) {
				source.NotifyPageReading(page);
				if (page is IBufferPageReader reader) {
					var count = Tools.Collection.CheckNotImplemented64bitAddressingLength(pageItemCount);
					reader.ReadRange(pageStartIndex, destination.Slice(destinationOffset, count));
					destinationOffset += count;
				} else {
					var pageSpan = page.ReadSpan(pageStartIndex, pageItemCount);
					pageSpan.CopyTo(destination.Slice(destinationOffset, pageSpan.Length));
					destinationOffset += pageSpan.Length;
				}
				source.NotifyPageRead(page);
			}
			source.NotifyPageAccessed(page);
		}
	}

}

