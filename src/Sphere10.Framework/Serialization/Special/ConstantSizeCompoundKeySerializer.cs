// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
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
		: base(GetConstantSize(key1Serializer, key2Serializer), false) {
		_key1Serializer = key1Serializer;
		_key2Serializer = key2Serializer;
	}

	public override void Serialize(CompoundKey<TKey1, TKey2> item, EndianBinaryWriter writer, SerializationContext context) {
		_key1Serializer.Serialize(item.Key1, writer, context);
		_key2Serializer.Serialize(item.Key2, writer, context);
	}

	public override CompoundKey<TKey1, TKey2> Deserialize(EndianBinaryReader reader, SerializationContext context) {
		var key1 = _key1Serializer.Deserialize(reader, context);
		var key2 = _key2Serializer.Deserialize(reader, context);
		return CompoundKey.From(key1, key2);
	}

	private static long GetConstantSize(IItemSerializer<TKey1> key1Serializer, IItemSerializer<TKey2> key2Serializer) {
		Guard.ArgumentNotNull(key1Serializer, nameof(key1Serializer));
		Guard.ArgumentNotNull(key2Serializer, nameof(key2Serializer));
		Guard.Argument(key1Serializer.IsConstantSize, nameof(key1Serializer), "Key1 serializer must be constant-size.");
		Guard.Argument(key2Serializer.IsConstantSize, nameof(key2Serializer), "Key2 serializer must be constant-size.");
		return checked(key1Serializer.ConstantSize + key2Serializer.ConstantSize);
	}
}

/// <summary>Concatenates three constant-size component serializations in key order without size descriptors.</summary>
public class ConstantSizeCompoundKeySerializer<TKey1, TKey2, TKey3> : ConstantSizeItemSerializerBase<CompoundKey<TKey1, TKey2, TKey3>> {
	private readonly IItemSerializer<TKey1> _key1Serializer;
	private readonly IItemSerializer<TKey2> _key2Serializer;
	private readonly IItemSerializer<TKey3> _key3Serializer;

	public ConstantSizeCompoundKeySerializer(IItemSerializer<TKey1> key1Serializer, IItemSerializer<TKey2> key2Serializer, IItemSerializer<TKey3> key3Serializer)
		: base(GetConstantSize(key1Serializer, key2Serializer, key3Serializer), false) {
		_key1Serializer = key1Serializer;
		_key2Serializer = key2Serializer;
		_key3Serializer = key3Serializer;
	}

	public override void Serialize(CompoundKey<TKey1, TKey2, TKey3> item, EndianBinaryWriter writer, SerializationContext context) {
		_key1Serializer.Serialize(item.Key1, writer, context);
		_key2Serializer.Serialize(item.Key2, writer, context);
		_key3Serializer.Serialize(item.Key3, writer, context);
	}

	public override CompoundKey<TKey1, TKey2, TKey3> Deserialize(EndianBinaryReader reader, SerializationContext context) {
		var key1 = _key1Serializer.Deserialize(reader, context);
		var key2 = _key2Serializer.Deserialize(reader, context);
		var key3 = _key3Serializer.Deserialize(reader, context);
		return CompoundKey.From(key1, key2, key3);
	}

	private static long GetConstantSize(IItemSerializer<TKey1> key1Serializer, IItemSerializer<TKey2> key2Serializer, IItemSerializer<TKey3> key3Serializer) {
		Guard.ArgumentNotNull(key1Serializer, nameof(key1Serializer));
		Guard.ArgumentNotNull(key2Serializer, nameof(key2Serializer));
		Guard.ArgumentNotNull(key3Serializer, nameof(key3Serializer));
		Guard.Argument(key1Serializer.IsConstantSize, nameof(key1Serializer), "Key1 serializer must be constant-size.");
		Guard.Argument(key2Serializer.IsConstantSize, nameof(key2Serializer), "Key2 serializer must be constant-size.");
		Guard.Argument(key3Serializer.IsConstantSize, nameof(key3Serializer), "Key3 serializer must be constant-size.");
		return checked(key1Serializer.ConstantSize + key2Serializer.ConstantSize + key3Serializer.ConstantSize);
	}
}

/// <summary>Concatenates four constant-size component serializations in key order without size descriptors.</summary>
public class ConstantSizeCompoundKeySerializer<TKey1, TKey2, TKey3, TKey4> : ConstantSizeItemSerializerBase<CompoundKey<TKey1, TKey2, TKey3, TKey4>> {
	private readonly IItemSerializer<TKey1> _key1Serializer;
	private readonly IItemSerializer<TKey2> _key2Serializer;
	private readonly IItemSerializer<TKey3> _key3Serializer;
	private readonly IItemSerializer<TKey4> _key4Serializer;

	public ConstantSizeCompoundKeySerializer(IItemSerializer<TKey1> key1Serializer, IItemSerializer<TKey2> key2Serializer, IItemSerializer<TKey3> key3Serializer, IItemSerializer<TKey4> key4Serializer)
		: base(GetConstantSize(key1Serializer, key2Serializer, key3Serializer, key4Serializer), false) {
		_key1Serializer = key1Serializer;
		_key2Serializer = key2Serializer;
		_key3Serializer = key3Serializer;
		_key4Serializer = key4Serializer;
	}

	public override void Serialize(CompoundKey<TKey1, TKey2, TKey3, TKey4> item, EndianBinaryWriter writer, SerializationContext context) {
		_key1Serializer.Serialize(item.Key1, writer, context);
		_key2Serializer.Serialize(item.Key2, writer, context);
		_key3Serializer.Serialize(item.Key3, writer, context);
		_key4Serializer.Serialize(item.Key4, writer, context);
	}

	public override CompoundKey<TKey1, TKey2, TKey3, TKey4> Deserialize(EndianBinaryReader reader, SerializationContext context) {
		var key1 = _key1Serializer.Deserialize(reader, context);
		var key2 = _key2Serializer.Deserialize(reader, context);
		var key3 = _key3Serializer.Deserialize(reader, context);
		var key4 = _key4Serializer.Deserialize(reader, context);
		return CompoundKey.From(key1, key2, key3, key4);
	}

	private static long GetConstantSize(IItemSerializer<TKey1> key1Serializer, IItemSerializer<TKey2> key2Serializer, IItemSerializer<TKey3> key3Serializer, IItemSerializer<TKey4> key4Serializer) {
		Guard.ArgumentNotNull(key1Serializer, nameof(key1Serializer));
		Guard.ArgumentNotNull(key2Serializer, nameof(key2Serializer));
		Guard.ArgumentNotNull(key3Serializer, nameof(key3Serializer));
		Guard.ArgumentNotNull(key4Serializer, nameof(key4Serializer));
		Guard.Argument(key1Serializer.IsConstantSize, nameof(key1Serializer), "Key1 serializer must be constant-size.");
		Guard.Argument(key2Serializer.IsConstantSize, nameof(key2Serializer), "Key2 serializer must be constant-size.");
		Guard.Argument(key3Serializer.IsConstantSize, nameof(key3Serializer), "Key3 serializer must be constant-size.");
		Guard.Argument(key4Serializer.IsConstantSize, nameof(key4Serializer), "Key4 serializer must be constant-size.");
		return checked(key1Serializer.ConstantSize + key2Serializer.ConstantSize + key3Serializer.ConstantSize + key4Serializer.ConstantSize);
	}
}
