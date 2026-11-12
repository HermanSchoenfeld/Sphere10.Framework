// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Serialization;

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

public class SortedListSerializer<TKey, TValue> : CollectionSerializerBase<SortedList<TKey, TValue>, KeyValuePair<TKey, TValue>> {

	public SortedListSerializer(IItemSerializer<TKey> keySerializer, IItemSerializer<TValue> valueSerializer)
		: this(new KeyValuePairSerializer<TKey, TValue>(keySerializer, valueSerializer), SizeDescriptorStrategy.UseCVarInt) {
	}

	public SortedListSerializer(IItemSerializer<KeyValuePair<TKey, TValue>> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy)
		: base(itemSerializer, sizeDescriptorStrategy) {
	}

	protected override long GetLength(SortedList<TKey, TValue> collection) => collection.Count;

	protected override SortedList<TKey, TValue> Activate(long capacity) => new(checked((int)capacity));

	protected override void SetItem(SortedList<TKey, TValue> collection, long index, KeyValuePair<TKey, TValue> item) {
		Guard.Ensure(collection.Count == index, "Unexpected index");
		collection.Add(item.Key, item.Value);
	}

}

public class SortedDictionarySerializer<TKey, TValue> : CollectionSerializerBase<SortedDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>> {

	public SortedDictionarySerializer(IItemSerializer<TKey> keySerializer, IItemSerializer<TValue> valueSerializer)
		: this(new KeyValuePairSerializer<TKey, TValue>(keySerializer, valueSerializer), SizeDescriptorStrategy.UseCVarInt) {
	}

	public SortedDictionarySerializer(IItemSerializer<KeyValuePair<TKey, TValue>> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy)
		: base(itemSerializer, sizeDescriptorStrategy) {
	}

	protected override long GetLength(SortedDictionary<TKey, TValue> collection) => collection.Count;

	protected override SortedDictionary<TKey, TValue> Activate(long capacity) => new();

	protected override void SetItem(SortedDictionary<TKey, TValue> collection, long index, KeyValuePair<TKey, TValue> item) {
		Guard.Ensure(collection.Count == index, "Unexpected index");
		collection.Add(item.Key, item.Value);
	}

}

public class ConcurrentDictionarySerializer<TKey, TValue> : CollectionSerializerBase<ConcurrentDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>> {

	public ConcurrentDictionarySerializer(IItemSerializer<TKey> keySerializer, IItemSerializer<TValue> valueSerializer)
		: this(new KeyValuePairSerializer<TKey, TValue>(keySerializer, valueSerializer), SizeDescriptorStrategy.UseCVarInt) {
	}

	public ConcurrentDictionarySerializer(IItemSerializer<KeyValuePair<TKey, TValue>> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy)
		: base(itemSerializer, sizeDescriptorStrategy) {
	}

	protected override long GetLength(ConcurrentDictionary<TKey, TValue> collection) => collection.Count;

	protected override ConcurrentDictionary<TKey, TValue> Activate(long capacity) => new();

	protected override void SetItem(ConcurrentDictionary<TKey, TValue> collection, long index, KeyValuePair<TKey, TValue> item) {
		// A serialized dictionary never repeats a key, so a failed add means the data is corrupt
		if (!collection.TryAdd(item.Key, item.Value))
			throw new SerializationException("Duplicate key in concurrent dictionary");
	}

}

