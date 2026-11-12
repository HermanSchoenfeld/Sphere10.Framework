// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Concurrent;

namespace Sphere10.Framework;

/// <summary>Serializes the items of a <see cref="ConcurrentBag{T}"/>. A bag is unordered, so the order of its items is not preserved.</summary>
public class ConcurrentBagSerializer<T> : CollectionSerializerBase<ConcurrentBag<T>, T> {

	public ConcurrentBagSerializer(IItemSerializer<T> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy = SizeDescriptorStrategy.UseCVarInt)
		: base(itemSerializer, sizeDescriptorStrategy) {
	}

	protected override long GetLength(ConcurrentBag<T> collection) => collection.Count;

	protected override ConcurrentBag<T> Activate(long capacity) => new();

	protected override void SetItem(ConcurrentBag<T> collection, long index, T item) => collection.Add(item);
}
