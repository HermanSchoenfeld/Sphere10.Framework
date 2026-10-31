// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.IO;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>Serializes ObjectSpace settings and its one-off serializer factory record.</summary>
public sealed class ObjectSpaceMetaDataSerializer : ItemSerializerBase<ObjectSpaceMetaData> {
	private readonly SerializerFactorySerializer _factorySerializer;

	/// <summary>Creates a metadata serializer with its own factory serializer component.</summary>
	public ObjectSpaceMetaDataSerializer() {
		// Each metadata serializer owns its factory component; primitive components use their singletons directly.
		_factorySerializer = new SerializerFactorySerializer();
	}

	/// <summary>Calculates the complete record size, including its used-length prefix.</summary>
	public override long CalculateSize(SerializationContext context, ObjectSpaceMetaData item) {
		// Include the record prefix and each construction-setting component.
		return PrimitiveSerializer<int>.Instance.ConstantSize + EnumSerializer<CHF>.Instance.CalculateSize(context, item.HashFunction)
			+ _factorySerializer.CalculateSize(context, item.SerializerFactory);
	}

	/// <summary>Writes the length-prefixed hash-function and shared-factory record.</summary>
	public override void Serialize(ObjectSpaceMetaData item, EndianBinaryWriter writer, SerializationContext context) {
		// The length includes its own prefix; header traits are deliberately absent from this record.
		PrimitiveSerializer<int>.Instance.Serialize(checked((int)CalculateSize(context, item)), writer, context);

		// Component serializers share the owner's writer, byte order and reference context.
		EnumSerializer<CHF>.Instance.Serialize(item.HashFunction, writer, context);
		_factorySerializer.Serialize(item.SerializerFactory, writer, context);
	}

	/// <summary>Reads the construction settings and verifies the record's declared byte length.</summary>
	public override ObjectSpaceMetaData Deserialize(EndianBinaryReader reader, SerializationContext context) {
		// The used length is independent of the enclosing blob's reserved capacity and padding.
		var start = reader.BaseStream.Position;
		var length = PrimitiveSerializer<int>.Instance.Deserialize(reader, context);
		if (length < PrimitiveSerializer<int>.Instance.ConstantSize + EnumSerializer<CHF>.Instance.ConstantSize || length > reader.BaseStream.Length - start)
			throw new InvalidDataException("Invalid ObjectSpace metadata record length.");

		// Construction data follows the same component order used during serialization.
		var item = new ObjectSpaceMetaData {
			HashFunction = EnumSerializer<CHF>.Instance.Deserialize(reader, context),
			SerializerFactory = _factorySerializer.Deserialize(reader, context)
		};

		// Reject a prefix that describes a different record boundary from the decoded fields.
		if (reader.BaseStream.Position - start != length)
			throw new InvalidDataException("ObjectSpace metadata length does not match its record.");
		return item;
	}
}
