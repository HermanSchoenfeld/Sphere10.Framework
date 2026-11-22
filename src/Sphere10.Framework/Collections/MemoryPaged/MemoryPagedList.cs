// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

/// <summary>
/// An in-memory paged list that serializes items into binary pages sized by a supplied <see cref="IItemSizer{T}"/>.
/// </summary>
public class MemoryPagedList<TItem> : MemoryPagedListBase<TItem> {
	private readonly IItemSizer<TItem> _sizer;

	/// <summary>
	/// Creates a paged list with a fixed-size item serializer.
	/// </summary>
	/// <param name="pageSize">Logical items per page.</param>
	/// <param name="maxMemory">Maximum bytes available for loaded pages.</param>
	/// <param name="fixedItemSize">Fixed serialized size of an item.</param>
	/// <param name="pageReapPolicy">The order in which loaded pages are evicted when <paramref name="maxMemory"/> is reached.</param>
	public MemoryPagedList(long pageSize, long maxMemory, int fixedItemSize, CacheReapPolicy pageReapPolicy = Sphere10FrameworkDefaults.PageCacheReapPolicy)
		: this(pageSize, maxMemory, new ConstantLengthItemSizer<TItem>(fixedItemSize, false), pageReapPolicy) {
	}

	/// <summary>
	/// Creates a paged list using a delegate to compute serialized item sizes.
	/// </summary>
	/// <param name="pageSize">Logical items per page.</param>
	/// <param name="maxMemory">Maximum bytes available for loaded pages.</param>
	/// <param name="itemSizer">Function that estimates the serialized size for each item.</param>
	/// <param name="pageReapPolicy">The order in which loaded pages are evicted when <paramref name="maxMemory"/> is reached.</param>
	public MemoryPagedList(long pageSize, long maxMemory, Func<TItem, long> itemSizer, CacheReapPolicy pageReapPolicy = Sphere10FrameworkDefaults.PageCacheReapPolicy)
		: this(pageSize, maxMemory, new ActionItemSizer<TItem>(itemSizer), pageReapPolicy) {
	}

	/// <summary>
	/// Creates a paged list whose items are measured by an item sizer, such as an item serializer.
	/// </summary>
	/// <param name="pageSize">Bytes per page.</param>
	/// <param name="maxMemory">Maximum bytes available for loaded pages.</param>
	/// <param name="sizer">Measures the serialized size of each item.</param>
	/// <param name="pageReapPolicy">The order in which loaded pages are evicted when <paramref name="maxMemory"/> is reached.</param>
	public MemoryPagedList(long pageSize, long maxMemory, IItemSizer<TItem> sizer, CacheReapPolicy pageReapPolicy = Sphere10FrameworkDefaults.PageCacheReapPolicy)
		: base(pageSize, maxMemory, pageReapPolicy: pageReapPolicy) {
		Guard.ArgumentNotNull(sizer, nameof(sizer));
		_sizer = sizer;
	}

	/// <inheritdoc />
	protected override IPage<TItem> NewPageInstance(long pageNumber) {
		return new BinarySerializedPage<TItem>(this.PageSize, _sizer);
	}

	/// <summary>
	/// Memory-backed lists cannot reload state across process boundaries; consumers should use file-mapped implementations instead.
	/// </summary>
	/// <exception cref="NotSupportedException">Always thrown to signal the lack of persistence.</exception>
	protected override IPage<TItem>[] LoadPages() {
		throw new NotSupportedException("Pages are not loadable across runtime sessions in this implementation. See FileMappedList class.");
	}
}

