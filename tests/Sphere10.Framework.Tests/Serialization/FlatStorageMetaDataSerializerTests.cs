// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests;

/// <summary>Verifies metadata record codecs, component serialization and legacy factory compatibility.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FlatStorageMetaDataSerializerTests {
	/// <summary>Verifies that the default factory registers both owner metadata serializers.</summary>
	[Test]
	public void DefaultFactoryIncludesTheOrdinaryMetadataSerializers() {
		Assert.That(SerializerFactory.Default.GetPureSerializer<ObjectSpaceMetaData>(), Is.TypeOf<ObjectSpaceMetaDataSerializer>());
		Assert.That(SerializerFactory.Default.GetPureSerializer<ObjectStreamMetaData>(), Is.TypeOf<ObjectStreamMetaDataSerializer>());
	}

	/// <summary>Verifies that retaining the original factory preserves legacy payload type codes.</summary>
	[Test]
	public void LegacyBytesRemainReadableWithTheirOriginalFactoryAndTypeCodes([Values] Endianness endianness) {
		const long originalTypeCode = 77;
		var originalRows = SerializerFactory.Default.RegisteredTypes.Select(SerializerFactory.Default.FindRegistration)
			.Where(row => row.TypeCode < originalTypeCode).ToArray();
		var originalFactory = new SerializerFactory();
		foreach (var row in originalRows)
			originalFactory.RegisterInternal(row.TypeCode, row.DataType, row.SerializerType, null, row.Factory);
		var serializer = SerializerBuilder.For<WireItem>()
			.Serialize(item => item.First, originalFactory)
			.Serialize(item => item.Second, PrimitiveSerializer<int>.Instance)
			.Build();
		originalFactory.Register(serializer, originalTypeCode);
		Assert.That(SerializerFactory.Default.GetRegistration(originalTypeCode).DataType, Is.Not.EqualTo(typeof(WireItem)));
		using var raw = new MemoryStream();
		using (var created = new ObjectStream<WireItem>(new ClusteredStreams(raw, 64, endianness: endianness), originalFactory.GetSerializer<WireItem>()) { OwnsStreams = true }) {
			created.Load();
			created.SaveItem(0, new WireItem { First = "legacy", Second = 42 }, ObjectStreamOperationType.Add);
		}
		var bytes = raw.ToArray();
		Assert.That(bytes[0], Is.EqualTo(1));
		using (var reopened = new ObjectStream<WireItem>(new ClusteredStreams(raw, 64, endianness: endianness), originalFactory.GetSerializer<WireItem>()) { OwnsStreams = true }) {
			reopened.Load();
			var item = reopened.LoadItem(0);
			Assert.That(item.First, Is.EqualTo("legacy"));
			Assert.That(item.Second, Is.EqualTo(42));
			Assert.That(originalFactory.FindRegistration(typeof(WireItem)).TypeCode, Is.EqualTo(originalTypeCode));
		}
		Assert.That(raw.ToArray(), Is.EqualTo(bytes));
	}

	/// <summary>Verifies ObjectSpace metadata fields, saved factory and calculated record size through a round trip.</summary>
	[Test]
	public void ObjectSpaceRoundTripMatchesItsSize([Values] Endianness endianness) {
		var factory = new SerializerFactory(SerializerFactory.Default).SetMinTypeCode(9000).RegisterAutoBuild<WireItem>();
		var item = new ObjectSpaceMetaData {
			HashFunction = CHF.SHA2_256,
			SerializerFactory = factory
		};
		var serializer = new ObjectSpaceMetaDataSerializer();
		var bytes = serializer.SerializeToBytes(item, endianness);
		Assert.That(bytes.Length, Is.EqualTo(sizeof(int) + sizeof(ushort) + new SerializerFactorySerializer().CalculateSize(factory)));
		Assert.That(serializer.CalculateSize(item), Is.EqualTo(bytes.Length));
		Assert.That(EndianBitConverter.For(endianness).ToInt32(bytes, 0), Is.EqualTo(bytes.Length));
		Assert.That(EndianBitConverter.For(endianness).ToUInt16(bytes, sizeof(int)), Is.EqualTo((ushort)item.HashFunction));
		var restored = serializer.DeserializeBytes(bytes, endianness);
		Assert.That(restored.HashFunction, Is.EqualTo(item.HashFunction));
		Assert.That(restored.SerializerFactory.FindRegistration(typeof(WireItem)).TypeCode, Is.EqualTo(9000));
		var payload = factory.GetSerializer<WireItem>().SerializeToBytes(new WireItem { First = "factory", Second = 42 }, endianness);
		var restoredItem = restored.SerializerFactory.GetSerializer<WireItem>().DeserializeBytes(payload, endianness);
		Assert.That(restoredItem.First, Is.EqualTo("factory"));
		Assert.That(restoredItem.Second, Is.EqualTo(42));
	}

	/// <summary>Verifies dimension settings and ordered index descriptions through the ObjectStream metadata codec.</summary>
	[Test]
	public void ObjectStreamRoundTripMatchesItsSize([Values] Endianness endianness) {
		var item = new ObjectStreamMetaData {
			ItemType = typeof(string),
			ItemTypeCode = SerializerFactory.Default.FindRegistration(typeof(string)).TypeCode,
			PreAllocateOptimization = true,
			IsRoot = true,
			ChangeTrackingMemberName = "HasChanged",
			Indexes = new[] {
				new ObjectStreamMetaData.Index {
					Kind = ObjectSpaceDefinition.IndexType.UniqueKey,
					Name = "ByName",
					MemberName = "Name",
					NullPolicy = IndexNullPolicy.ThrowOnNull,
					BTreeSpec = new BTreeSpec(BTreeType.BTreePlus, 7)
				},
				new ObjectStreamMetaData.Index {
					Kind = ObjectSpaceDefinition.IndexType.MerkleTree,
					Name = "Merkle",
					HashFunction = CHF.SHA2_256
				}
			}
		};
		var serializer = new ObjectStreamMetaDataSerializer();
		var bytes = serializer.SerializeToBytes(item, endianness);
		Assert.That(serializer.CalculateSize(item), Is.EqualTo(bytes.Length));
		Assert.That(EndianBitConverter.For(endianness).ToInt32(bytes, 0), Is.EqualTo(bytes.Length));
		var restored = serializer.DeserializeBytes(bytes, endianness);
		Assert.That(restored.ItemType, Is.EqualTo(typeof(string)));
		Assert.That(restored.ItemTypeCode, Is.EqualTo(item.ItemTypeCode));
		Assert.That(restored.PreAllocateOptimization, Is.True);
		Assert.That(restored.IsRoot, Is.True);
		Assert.That(restored.ChangeTrackingMemberName, Is.EqualTo("HasChanged"));
		Assert.That(restored.Indexes, Has.Length.EqualTo(2));
		Assert.That(restored.Indexes[0].Kind, Is.EqualTo(item.Indexes[0].Kind));
		Assert.That(restored.Indexes[0].Name, Is.EqualTo("ByName"));
		Assert.That(restored.Indexes[0].MemberName, Is.EqualTo("Name"));
		Assert.That(restored.Indexes[0].NullPolicy, Is.EqualTo(IndexNullPolicy.ThrowOnNull));
		Assert.That(restored.Indexes[0].BTreeSpec, Is.EqualTo(new BTreeSpec(BTreeType.BTreePlus, 7)));
		Assert.That(restored.Indexes[1].MemberName, Is.Null);
		Assert.That(restored.Indexes[1].BTreeSpec, Is.Null);
		Assert.That(restored.Indexes[1].HashFunction, Is.EqualTo(CHF.SHA2_256));
		Assert.That(restored.SerializerFactory, Is.Null);
	}

	/// <summary>Verifies that only member-index records persist a complete B-tree specification.</summary>
	[Test]
	public void NonTreeAttachmentsHaveNoSerializedBTreeSpecification(
		[Values] Endianness endianness,
		[Values(ObjectSpaceDefinition.IndexType.MerkleTree, ObjectSpaceDefinition.IndexType.RecyclableIndexStore)] ObjectSpaceDefinition.IndexType kind) {
		var row = new ObjectStreamMetaData.Index { Kind = kind, Name = "attachment", HashFunction = CHF.SHA2_256 };
		var item = new ObjectStreamMetaData { ItemType = typeof(long), Indexes = new[] { row } };
		var serializer = new ObjectStreamMetaDataSerializer();
		var nonTreeBytes = serializer.SerializeToBytes(item, endianness);
		Assert.That(serializer.CalculateSize(item), Is.EqualTo(nonTreeBytes.Length));
		Assert.That(serializer.DeserializeBytes(nonTreeBytes, endianness).Indexes.Single().BTreeSpec, Is.Null);
		row.Kind = ObjectSpaceDefinition.IndexType.Identifier;
		row.BTreeSpec = new BTreeSpec(BTreeType.BTree, 7);
		var treeBytes = serializer.SerializeToBytes(item, endianness);
		Assert.That(treeBytes.Length - nonTreeBytes.Length, Is.EqualTo(8));
		Assert.That(serializer.CalculateSize(item), Is.EqualTo(treeBytes.Length));
		Assert.That(serializer.DeserializeBytes(treeBytes, endianness).Indexes.Single().BTreeSpec, Is.EqualTo(row.BTreeSpec));
	}

	/// <summary>Verifies that metadata components share the caller-provided serialization context.</summary>
	[Test]
	public void ObjectStreamUsesTheProvidedContext([Values] Endianness endianness) {
		var item = new ObjectStreamMetaData {
			ItemType = typeof(long), IsRoot = true,
			ItemTypeCode = SerializerFactory.Default.FindRegistration(typeof(long)).TypeCode
		};
		var serializer = new ObjectStreamMetaDataSerializer();
		using var stream = new MemoryStream();
		using var context = new SerializationContext();
		using (var writer = new EndianBinaryWriter(EndianBitConverter.For(endianness), stream, leaveOpen: true))
			serializer.Serialize(item, writer, context);
		Assert.That(stream.Length, Is.EqualTo(serializer.CalculateSize(context, item)));
		stream.Position = 0;
		using var reader = new EndianBinaryReader(EndianBitConverter.For(endianness), stream, leaveOpen: true);
		var restored = serializer.Deserialize(reader, context);
		Assert.That(restored.ItemType, Is.EqualTo(typeof(long)));
		Assert.That(restored.ItemTypeCode, Is.EqualTo(item.ItemTypeCode));
		Assert.That(restored.IsRoot, Is.True);
		Assert.That(restored.ChangeTrackingMemberName, Is.Null);
		Assert.That(restored.Indexes, Is.Empty);
		Assert.That(stream.Position, Is.EqualTo(stream.Length));
		Assert.That(restored.SerializerFactory, Is.Null);
	}

	/// <summary>Verifies that standalone metadata adds one usable factory while dimension metadata omits it.</summary>
	[Test]
	public void StandaloneStreamIncludesItsFactoryOnce([Values] Endianness endianness) {
		// A dimension omits this factory; a standalone ObjectStream must carry the same reconstruction data itself.
		var factory = new SerializerFactory(SerializerFactory.Default).SetMinTypeCode(9000).RegisterAutoBuild<WireItem>();
		var item = new ObjectStreamMetaData { ItemType = typeof(WireItem), ItemTypeCode = 9000 };
		var serializer = new ObjectStreamMetaDataSerializer();
		var dimensionBytes = serializer.SerializeToBytes(item, endianness);
		item.SerializerFactory = factory;
		var standaloneBytes = serializer.SerializeToBytes(item, endianness);
		Assert.That(standaloneBytes.Length - dimensionBytes.Length, Is.EqualTo(new SerializerFactorySerializer().CalculateSize(factory)));
		Assert.That(serializer.CalculateSize(item), Is.EqualTo(standaloneBytes.Length));

		// The stored factory resolves the original code and reads an actual payload serialized by the original.
		var restored = serializer.DeserializeBytes(standaloneBytes, endianness);
		Assert.That(restored.SerializerFactory.FindRegistration(typeof(WireItem)).TypeCode, Is.EqualTo(item.ItemTypeCode));
		var payload = factory.GetSerializer<WireItem>().SerializeToBytes(new WireItem { First = "standalone", Second = 17 }, endianness);
		var copy = restored.SerializerFactory.GetSerializer<WireItem>().DeserializeBytes(payload, endianness);
		Assert.That(copy.First, Is.EqualTo("standalone"));
		Assert.That(copy.Second, Is.EqualTo(17));
	}

	/// <summary>Verifies rejection when the declared record length disagrees with the consumed metadata bytes.</summary>
	[Test]
	public void IncorrectTotalRecordLengthIsRejected([Values] Endianness endianness) {
		var serializer = new ObjectSpaceMetaDataSerializer();
		var bytes = serializer.SerializeToBytes(new ObjectSpaceMetaData { SerializerFactory = new SerializerFactory() }, endianness);
		EndianBitConverter.For(endianness).WriteTo(bytes.Length + 1, bytes, 0);
		Assert.That(() => serializer.DeserializeBytes(bytes, endianness), Throws.TypeOf<InvalidDataException>());
	}

	/// <summary>Verifies that invalid record bounds fail before reading their component records.</summary>
	[Test]
	public void InvalidTotalRecordLengthsAreRejectedBeforeReadingRows([Values(-1, 0, 3, int.MaxValue)] int length, [Values] Endianness endianness) {
		var serializer = new ObjectSpaceMetaDataSerializer();
		var bytes = serializer.SerializeToBytes(new ObjectSpaceMetaData { SerializerFactory = new SerializerFactory() }, endianness);
		EndianBitConverter.For(endianness).WriteTo(length, bytes, 0);
		Assert.That(() => serializer.DeserializeBytes(bytes, endianness), Throws.TypeOf<InvalidDataException>());
	}

	/// <summary>Provides a simple mixed-size payload for metadata factory and legacy-code round trips.</summary>
	public sealed class WireItem {
		public string First { get; set; }
		public int Second { get; set; }
	}
}
