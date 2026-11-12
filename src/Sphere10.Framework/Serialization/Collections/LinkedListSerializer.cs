// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

public class LinkedListSerializer<T> : CollectionSerializerBase<LinkedList<T>, T> {

	public LinkedListSerializer(IItemSerializer<T> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy = SizeDescriptorStrategy.UseCVarInt)
		: base(itemSerializer, sizeDescriptorStrategy) {
	}

	protected override long GetLength(LinkedList<T> collection) => collection.Count;

	protected override LinkedList<T> Activate(long capacity) => new();

	protected override void SetItem(LinkedList<T> collection, long index, T item) {
		Guard.Ensure(collection.Count == index, "Unexpected index");
		collection.AddLast(item);
	}
}
