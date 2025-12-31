// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

public class DictionarySerializer<TKey, TValue> : CollectionSerializerBase<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>> {

	public DictionarySerializer(IItemSerializer<TKey> keySerializer, IItemSerializer<TValue> valueSerializer) 
		: this(new KeyValuePairSerializer<TKey, TValue>(keySerializer, valueSerializer), SizeDescriptorStrategy.UseCVarInt) {
	}

	public DictionarySerializer(IItemSerializer<KeyValuePair<TKey, TValue>> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy) 
		: base(itemSerializer, sizeDescriptorStrategy) {
	}

	protected override long GetLength(Dictionary<TKey, TValue> collection) => collection.Count;

	protected override Dictionary<TKey, TValue> Activate(long capacity) => new(checked((int)capacity));

	protected override void SetItem(Dictionary<TKey, TValue> collection, long index, KeyValuePair<TKey, TValue> item) {
		Guard.Ensure(collection.Count == index, "Unexpected index");
		collection.Add(item.Key, item.Value);
	}

}

