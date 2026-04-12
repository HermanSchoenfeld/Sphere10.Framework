// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// A constant-size serializer for <see cref="KeyValuePair{TKey, TValue}"/> that concatenates
/// the key and value serializations without size descriptors. Both component serializers must be constant-size.
/// This is used where fixed-layout storage is required (e.g. B-tree node records).
/// </summary>
public class ConstantSizeKeyValuePairSerializer<TKey, TValue> : ConstantSizeItemSerializerBase<KeyValuePair<TKey, TValue>> {
	private readonly IItemSerializer<TKey> _keySerializer;
	private readonly IItemSerializer<TValue> _valueSerializer;

	public ConstantSizeKeyValuePairSerializer(IItemSerializer<TKey> keySerializer, IItemSerializer<TValue> valueSerializer)
		: base(keySerializer.ConstantSize + valueSerializer.ConstantSize, false) {
		Guard.ArgumentNotNull(keySerializer, nameof(keySerializer));
		Guard.ArgumentNotNull(valueSerializer, nameof(valueSerializer));
		Guard.Argument(keySerializer.IsConstantSize, nameof(keySerializer), "Key serializer must be constant-size.");
		Guard.Argument(valueSerializer.IsConstantSize, nameof(valueSerializer), "Value serializer must be constant-size.");
		_keySerializer = keySerializer;
		_valueSerializer = valueSerializer;
	}

	public override void Serialize(KeyValuePair<TKey, TValue> item, EndianBinaryWriter writer, SerializationContext context) {
		_keySerializer.Serialize(item.Key, writer, context);
		_valueSerializer.Serialize(item.Value, writer, context);
	}

	public override KeyValuePair<TKey, TValue> Deserialize(EndianBinaryReader reader, SerializationContext context) {
		var Key = _keySerializer.Deserialize(reader, context);
		var Value = _valueSerializer.Deserialize(reader, context);
		return new KeyValuePair<TKey, TValue>(Key, Value);
	}
}
