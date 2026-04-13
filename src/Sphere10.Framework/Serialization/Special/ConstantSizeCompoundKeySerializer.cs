// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// A constant-size serializer for <see cref="CompoundKey{TKey1, TKey2}"/> that concatenates
/// the two key serializations without size descriptors. Both component serializers must be constant-size.
/// This is used where fixed-layout storage is required (e.g. B-tree node records).
/// </summary>
public class ConstantSizeCompoundKeySerializer<TKey1, TKey2> : ConstantSizeItemSerializerBase<CompoundKey<TKey1, TKey2>> {
	private readonly IItemSerializer<TKey1> _key1Serializer;
	private readonly IItemSerializer<TKey2> _key2Serializer;

	public ConstantSizeCompoundKeySerializer(IItemSerializer<TKey1> key1Serializer, IItemSerializer<TKey2> key2Serializer)
		: base(key1Serializer.ConstantSize + key2Serializer.ConstantSize, false) {
		Guard.ArgumentNotNull(key1Serializer, nameof(key1Serializer));
		Guard.ArgumentNotNull(key2Serializer, nameof(key2Serializer));
		Guard.Argument(key1Serializer.IsConstantSize, nameof(key1Serializer), "Key1 serializer must be constant-size.");
		Guard.Argument(key2Serializer.IsConstantSize, nameof(key2Serializer), "Key2 serializer must be constant-size.");
		_key1Serializer = key1Serializer;
		_key2Serializer = key2Serializer;
	}

	public override void Serialize(CompoundKey<TKey1, TKey2> item, EndianBinaryWriter writer, SerializationContext context) {
		_key1Serializer.Serialize(item.Key1, writer, context);
		_key2Serializer.Serialize(item.Key2, writer, context);
	}

	public override CompoundKey<TKey1, TKey2> Deserialize(EndianBinaryReader reader, SerializationContext context) {
		var Key1 = _key1Serializer.Deserialize(reader, context);
		var Key2 = _key2Serializer.Deserialize(reader, context);
		return new CompoundKey<TKey1, TKey2>(Key1, Key2);
	}
}
