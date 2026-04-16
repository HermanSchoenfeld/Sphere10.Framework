// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>
/// Globally unique object identifier within an ObjectSpace.
/// Composed of a <see cref="DimensionIndex"/> (ordinal position in <see cref="ObjectSpaceDefinition.Dimensions"/>)
/// and an <see cref="ObjectIndex"/> (row position within that dimension's <see cref="StreamMappedRecyclableList{T}"/>).
/// Together these form a lightweight cross-dimension pointer used for external reference serialization and GC tracking.
/// </summary>
/// <remarks>
/// Constant serialized size: 10 bytes (2-byte short + 8-byte long).
/// Negative <see cref="ObjectIndex"/> values represent provisional IDs for newly created objects not yet persisted.
/// </remarks>
public readonly record struct ObjectSpaceObjectReference(short DimensionIndex, long ObjectIndex)
	: IEquatable<ObjectSpaceObjectReference>, IComparable<ObjectSpaceObjectReference> {

	/// <summary>
	/// Compares two references by dimension index first, then by object index within the dimension.
	/// This ordering groups references by dimension and sorts them by row position.
	/// </summary>
	public int CompareTo(ObjectSpaceObjectReference other) {
		// Primary sort: by dimension index so references in the same dimension are adjacent
		var dimensionComparison = DimensionIndex.CompareTo(other.DimensionIndex);
		if (dimensionComparison != 0)
			return dimensionComparison;

		// Secondary sort: by object (row) index within the dimension
		return ObjectIndex.CompareTo(other.ObjectIndex);
	}

	/// <summary>
	/// Human-readable representation for debugging, e.g. "(Dim:2, Row:17)".
	/// </summary>
	public override string ToString() => $"(Dim:{DimensionIndex}, Row:{ObjectIndex})";
}

/// <summary>
/// Constant-size binary serializer for <see cref="ObjectSpaceObjectReference"/>.
/// Serializes as exactly 10 bytes: 2 bytes for <see cref="ObjectSpaceObjectReference.DimensionIndex"/> (short)
/// followed by 8 bytes for <see cref="ObjectSpaceObjectReference.ObjectIndex"/> (long).
/// </summary>
public sealed class ObjectSpaceObjectReferenceSerializer : ConstantSizeItemSerializerBase<ObjectSpaceObjectReference> {

	/// <summary>
	/// Serialized size in bytes: sizeof(short) + sizeof(long) = 2 + 8 = 10.
	/// </summary>
	public const int SerializedSize = sizeof(short) + sizeof(long); // 10 bytes

	/// <summary>
	/// Singleton instance for reuse — the serializer is stateless and safe to share.
	/// </summary>
	public static readonly ObjectSpaceObjectReferenceSerializer Instance = new();

	public ObjectSpaceObjectReferenceSerializer()
		: base(SerializedSize, supportsNull: false) {
	}

	/// <summary>
	/// Writes the dimension index (2 bytes) followed by the object index (8 bytes) to the output stream.
	/// </summary>
	public override void Serialize(ObjectSpaceObjectReference item, EndianBinaryWriter writer, SerializationContext context) {
		// Write dimension ordinal as a 16-bit integer
		writer.Write(item.DimensionIndex);
		// Write row/object index as a 64-bit integer
		writer.Write(item.ObjectIndex);
	}

	/// <summary>
	/// Reads the dimension index (2 bytes) and object index (8 bytes) from the input stream to reconstruct the reference.
	/// </summary>
	public override ObjectSpaceObjectReference Deserialize(EndianBinaryReader reader, SerializationContext context) {
		// Read dimension ordinal
		var dimensionIndex = reader.ReadInt16();
		// Read row/object index
		var objectIndex = reader.ReadInt64();
		return new ObjectSpaceObjectReference(dimensionIndex, objectIndex);
	}
}
