// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

public class StackSerializer<T> : CollectionSerializerBase<Stack<T>, T> {

	public StackSerializer(IItemSerializer<T> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy = SizeDescriptorStrategy.UseCVarInt)
		: base(itemSerializer, sizeDescriptorStrategy) {
	}

	protected override long GetLength(Stack<T> collection) => collection.Count;

	protected override Stack<T> Activate(long capacity) => new(checked((int)capacity));

	protected override void SetItem(Stack<T> collection, long index, T item) {
		Guard.Ensure(collection.Count == index, "Unexpected index");
		collection.Push(item);
	}

	protected override void OnItemsSet(Stack<T> collection) {
		// A stack enumerates from its top, so the items were pushed top first. Push them again in reverse to restore the pop order.
		var items = collection.ToArray();
		collection.Clear();
		foreach (var item in items)
			collection.Push(item);
	}
}
