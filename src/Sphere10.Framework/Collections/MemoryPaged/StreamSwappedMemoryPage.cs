// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.IO;

namespace Sphere10.Framework;

/// <summary>
/// A memory page that swaps to its own region of a stream shared by every page of its list: page <c>n</c> occupies the
/// <c>pageSize</c> bytes from <c>n × pageSize</c>.
/// </summary>
/// <remarks>The shared stream belongs to the list, so disposing a page leaves it open.</remarks>
public abstract class StreamSwappedMemoryPage<TItem> : MemoryPageBase<TItem> {

	protected StreamSwappedMemoryPage(Stream stream, long pageNumber, long pageSize, IItemSizer<TItem> sizer, IExtendedList<TItem> memoryStore)
		: base(pageSize, sizer, memoryStore) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Region = new BoundedStream(stream, checked(pageNumber * pageSize), pageSize);
	}

	/// <summary>The page's region of the shared stream, addressed from the region's start after a seek to its beginning.</summary>
	protected BoundedStream Region { get; }

	public override void Dispose() {
	}

	protected override Stream OpenReadStream() {
		Region.Seek(0L, SeekOrigin.Begin);
		return new NonClosingStream(Region);
	}

	protected override Stream OpenWriteStream() {
		Region.Seek(0L, SeekOrigin.Begin);
		return new NonClosingStream(Region);
	}
}
