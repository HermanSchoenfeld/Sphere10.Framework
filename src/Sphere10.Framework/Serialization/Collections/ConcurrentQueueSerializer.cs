// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Concurrent;

namespace Sphere10.Framework;

public class ConcurrentQueueSerializer<T> : CollectionSerializerBase<ConcurrentQueue<T>, T> {

	public ConcurrentQueueSerializer(IItemSerializer<T> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy = SizeDescriptorStrategy.UseCVarInt)
		: base(itemSerializer, sizeDescriptorStrategy) {
	}

	protected override long GetLength(ConcurrentQueue<T> collection) => collection.Count;

	protected override ConcurrentQueue<T> Activate(long capacity) => new();

	protected override void SetItem(ConcurrentQueue<T> collection, long index, T item) => collection.Enqueue(item);
}
