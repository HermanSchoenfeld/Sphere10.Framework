// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.IO;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework;

/// <summary>Serializes ObjectStream settings, index rows and an optional standalone serializer factory.</summary>
public sealed class ObjectStreamMetaDataSerializer : ItemSerializerBase<ObjectStreamMetaData> {
	private readonly IndexSerializer _indexSerializer;
	private readonly SerializerFactorySerializer _factorySerializer;

	/// <summary>Creates independent index and factory serializer components for this record codec.</summary>
	public ObjectStreamMetaDataSerializer() {
		// Complex components belong to this serializer; existing primitive components are accessed directly.
		_indexSerializer = new IndexSerializer();
		_factorySerializer = new SerializerFactorySerializer();
	}

	/// <summary>Calculates the complete record size, including ordered indexes and any standalone factory.</summary>
	public override long CalculateSize(SerializationContext context, ObjectStreamMetaData item) {
		// Dimensions use their owner's factory; only standalone streams store one here.
		var size = PrimitiveSerializer<int>.Instance.ConstantSize + TypeSerializer.Instance.CalculateSize(context, item.ItemType)
			+ PrimitiveSerializer<long>.Instance.CalculateSize(context, item.ItemTypeCode)
			+ PrimitiveSerializer<bool>.Instance.CalculateSize(context, item.PreAllocateOptimization)
			+ PrimitiveSerializer<bool>.Instance.CalculateSize(context, item.IsRoot) + StringSize(context, item.ChangeTrackingMemberName)
			+ PrimitiveSerializer<int>.Instance.CalculateSize(context, item.Indexes.Length)
			+ PrimitiveSerializer<bool>.Instance.CalculateSize(context, item.SerializerFactory != null);
		foreach (var index in item.Indexes)
			size += _indexSerializer.CalculateSize(context, index);
		if (item.SerializerFactory != null)
			size += _factorySerializer.CalculateSize(context, item.SerializerFactory);
		return size;
	}

	/// <summary>Writes the length-prefixed stream settings, attachment rows and optional factory.</summary>
	public override void Serialize(ObjectStreamMetaData item, EndianBinaryWriter writer, SerializationContext context) {
		// Prefix the used record size, independently of its enclosing blob's allocated capacity.
		PrimitiveSerializer<int>.Instance.Serialize(checked((int)CalculateSize(context, item)), writer, context);

		// These settings belong to this ObjectStream, including its optional ObjectSpace role and dirty member.
		TypeSerializer.Instance.Serialize(item.ItemType, writer, context);
		PrimitiveSerializer<long>.Instance.Serialize(item.ItemTypeCode, writer, context);
		PrimitiveSerializer<bool>.Instance.Serialize(item.PreAllocateOptimization, writer, context);
		PrimitiveSerializer<bool>.Instance.Serialize(item.IsRoot, writer, context);
		WriteString(item.ChangeTrackingMemberName, writer, context);

		// Index rows retain the physical attachment order used by their reserved streams.
		PrimitiveSerializer<int>.Instance.Serialize(item.Indexes.Length, writer, context);
		foreach (var index in item.Indexes)
			_indexSerializer.Serialize(index, writer, context);

		// The owning ObjectSpace supplies the factory for dimensions, avoiding a copy in each child.
		PrimitiveSerializer<bool>.Instance.Serialize(item.SerializerFactory != null, writer, context);
		if (item.SerializerFactory != null)
			_factorySerializer.Serialize(item.SerializerFactory, writer, context);
	}

	/// <summary>Reads a complete stream record and checks its declared length and attachment count.</summary>
	public override ObjectStreamMetaData Deserialize(EndianBinaryReader reader, SerializationContext context) {
		// Validate the record boundary before reading any variable-sized configuration.
		var start = reader.BaseStream.Position;
		var length = PrimitiveSerializer<int>.Instance.Deserialize(reader, context);
		if (length < PrimitiveSerializer<int>.Instance.ConstantSize || length > reader.BaseStream.Length - start)
			throw new InvalidDataException("Invalid ObjectStream metadata record length.");

		// Restore the stream settings before allocating its ordered attachment table.
		var item = new ObjectStreamMetaData {
			ItemType = TypeSerializer.Instance.Deserialize(reader, context),
			ItemTypeCode = PrimitiveSerializer<long>.Instance.Deserialize(reader, context),
			PreAllocateOptimization = PrimitiveSerializer<bool>.Instance.Deserialize(reader, context),
			IsRoot = PrimitiveSerializer<bool>.Instance.Deserialize(reader, context),
			ChangeTrackingMemberName = ReadString(reader, context)
		};

		// Each index row has a minimum encoded size, which bounds the allocation on seekable input.
		var count = PrimitiveSerializer<int>.Instance.Deserialize(reader, context);
		if (count < 0 || count > (start + length - reader.BaseStream.Position) / _indexSerializer.MinimumSize)
			throw new InvalidDataException("Invalid ObjectStream metadata index count.");
		item.Indexes = new ObjectStreamMetaData.Index[count];
		for (var index = 0; index < count; index++)
			item.Indexes[index] = _indexSerializer.Deserialize(reader, context);

		// A standalone record contains its one-off factory; child records rely on their ObjectSpace.
		if (PrimitiveSerializer<bool>.Instance.Deserialize(reader, context))
			item.SerializerFactory = _factorySerializer.Deserialize(reader, context);

		// Reject trailing or missing bytes within the record's declared used length.
		if (reader.BaseStream.Position - start != length)
			throw new InvalidDataException("ObjectStream metadata length does not match its record.");
		return item;
	}

	// Identifies attachment kinds whose rows include a concrete B-tree specification.
	private static bool IsTreeIndex(ObjectSpaceDefinition.IndexType kind) {
		return kind is ObjectSpaceDefinition.IndexType.Identifier or ObjectSpaceDefinition.IndexType.UniqueKey or ObjectSpaceDefinition.IndexType.Index;
	}

	// Includes the null-presence byte in the encoded size of an optional name.
	private static long StringSize(SerializationContext context, string value) {
		return PrimitiveSerializer<bool>.Instance.CalculateSize(context, value != null) + (value == null ? 0 : StringSerializer.UTF8.CalculateSize(context, value));
	}

	// Writes an optional name through the primitive and UTF-8 component serializers.
	private static void WriteString(string value, EndianBinaryWriter writer, SerializationContext context) {
		// A presence byte distinguishes null from empty without entering reference-tracking state.
		PrimitiveSerializer<bool>.Instance.Serialize(value != null, writer, context);
		if (value != null)
			StringSerializer.UTF8.Serialize(value, writer, context);
	}

	// Reads the matching presence byte and optional name.
	private static string ReadString(EndianBinaryReader reader, SerializationContext context) {
		return PrimitiveSerializer<bool>.Instance.Deserialize(reader, context) ? StringSerializer.UTF8.Deserialize(reader, context) : null;
	}

	/// <summary>Encodes one ordered attachment row and its kind-specific tree settings.</summary>
	private sealed class IndexSerializer : ItemSerializerBase<ObjectStreamMetaData.Index> {
		private readonly BTreeSpecSerializer _bTreeSpecSerializer;

		/// <summary>Creates the tree component and calculates the minimum encoded row size.</summary>
		public IndexSerializer() {
			// Each index serializer owns its tree component and derives bounds from the primitive encodings.
			_bTreeSpecSerializer = new BTreeSpecSerializer();
			MinimumSize = EnumSerializer<ObjectSpaceDefinition.IndexType>.Instance.ConstantSize + PrimitiveSerializer<bool>.Instance.ConstantSize * 2
				+ EnumSerializer<IndexNullPolicy>.Instance.ConstantSize + EnumSerializer<CHF>.Instance.ConstantSize;
		}

		/// <summary>The smallest possible row size, used to bound decoded index counts.</summary>
		internal long MinimumSize { get; }

		/// <summary>Calculates the row size for this attachment kind and its optional names.</summary>
		public override long CalculateSize(SerializationContext context, ObjectStreamMetaData.Index item) {
			// The attachment kind determines whether its B-tree specification contributes bytes.
			return EnumSerializer<ObjectSpaceDefinition.IndexType>.Instance.CalculateSize(context, item.Kind)
				+ StringSize(context, item.Name) + StringSize(context, item.MemberName)
				+ EnumSerializer<IndexNullPolicy>.Instance.CalculateSize(context, item.NullPolicy)
				+ (IsTreeIndex(item.Kind) ? _bTreeSpecSerializer.CalculateSize(context, item.BTreeSpec) : 0)
				+ EnumSerializer<CHF>.Instance.CalculateSize(context, item.HashFunction);
		}

		/// <summary>Writes attachment identity, key handling and applicable tree settings.</summary>
		public override void Serialize(ObjectStreamMetaData.Index item, EndianBinaryWriter writer, SerializationContext context) {
			// Attachment identity and null handling precede any kind-specific tree configuration.
			EnumSerializer<ObjectSpaceDefinition.IndexType>.Instance.Serialize(item.Kind, writer, context);
			WriteString(item.Name, writer, context);
			WriteString(item.MemberName, writer, context);
			EnumSerializer<IndexNullPolicy>.Instance.Serialize(item.NullPolicy, writer, context);

			// The index kind determines whether a complete B-tree specification is present on the wire.
			if (IsTreeIndex(item.Kind))
				_bTreeSpecSerializer.Serialize(item.BTreeSpec ?? throw new InvalidDataException("Tree index metadata requires a concrete B-tree specification."), writer, context);
			else if (item.BTreeSpec != null)
				throw new InvalidDataException("Non-tree index metadata cannot contain a B-tree specification.");

			// Merkle attachments use this field; other kinds retain their recorded default value.
			EnumSerializer<CHF>.Instance.Serialize(item.HashFunction, writer, context);
		}

		/// <summary>Reads one row using its attachment kind to select the remaining fields.</summary>
		public override ObjectStreamMetaData.Index Deserialize(EndianBinaryReader reader, SerializationContext context) {
			// Read the kind first so the remaining row can be interpreted without an extra presence flag.
			var item = new ObjectStreamMetaData.Index {
				Kind = EnumSerializer<ObjectSpaceDefinition.IndexType>.Instance.Deserialize(reader, context),
				Name = ReadString(reader, context),
				MemberName = ReadString(reader, context),
				NullPolicy = EnumSerializer<IndexNullPolicy>.Instance.Deserialize(reader, context)
			};

			// Non-tree attachments omit the specification bytes but keep the following hash-function field.
			if (IsTreeIndex(item.Kind))
				item.BTreeSpec = _bTreeSpecSerializer.Deserialize(reader, context);
			item.HashFunction = EnumSerializer<CHF>.Instance.Deserialize(reader, context);
			return item;
		}
	}

	/// <summary>Encodes a concrete B-tree specification as its order followed by its type.</summary>
	private sealed class BTreeSpecSerializer : ConstantSizeItemSerializerBase<BTreeSpec> {
		/// <summary>Uses the combined fixed widths of the order and tree-type component serializers.</summary>
		public BTreeSpecSerializer()
			: base(PrimitiveSerializer<int>.Instance.ConstantSize + EnumSerializer<BTreeType>.Instance.ConstantSize, false) {
			// Derive the fixed width from the same primitive encodings used for reading and writing.
		}

		/// <summary>Writes the complete tree order and type.</summary>
		public override void Serialize(BTreeSpec item, EndianBinaryWriter writer, SerializationContext context) {
			// Keep the established order-then-type representation of a complete specification.
			PrimitiveSerializer<int>.Instance.Serialize(item.Order, writer, context);
			EnumSerializer<BTreeType>.Instance.Serialize(item.Type, writer, context);
		}

		/// <summary>Reads and validates a complete tree specification through its constructor.</summary>
		public override BTreeSpec Deserialize(EndianBinaryReader reader, SerializationContext context) {
			// Construction validates both fields before the specification reaches an index factory.
			var order = PrimitiveSerializer<int>.Instance.Deserialize(reader, context);
			var type = EnumSerializer<BTreeType>.Instance.Deserialize(reader, context);
			return new BTreeSpec(type, order);
		}
	}
}
