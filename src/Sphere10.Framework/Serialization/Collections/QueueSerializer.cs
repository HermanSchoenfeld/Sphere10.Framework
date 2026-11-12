// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

public class QueueSerializer<T> : CollectionSerializerBase<Queue<T>, T> {

	public QueueSerializer(IItemSerializer<T> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy = SizeDescriptorStrategy.UseCVarInt)
		: base(itemSerializer, sizeDescriptorStrategy) {
	}

	protected override long GetLength(Queue<T> collection) => collection.Count;

	protected override Queue<T> Activate(long capacity) => new(checked((int)capacity));

	protected override void SetItem(Queue<T> collection, long index, T item) {
		Guard.Ensure(collection.Count == index, "Unexpected index");
		collection.Enqueue(item);
	}
}
