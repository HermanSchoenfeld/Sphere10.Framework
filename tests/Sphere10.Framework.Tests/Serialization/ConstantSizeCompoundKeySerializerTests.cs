// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ConstantSizeCompoundKeySerializerTests {

	[Test]
	public void TwoComponentStorageLayoutIsUnchanged([Values] Endianness endianness) {
		var serializer = new ConstantSizeCompoundKeySerializer<int, short>(PrimitiveSerializer<int>.Instance, PrimitiveSerializer<short>.Instance);
		var key = CompoundKey.From(0x01020304, (short)0x0506);
		var bytes = serializer.SerializeToBytes(key, endianness);
		var expected = endianness == Endianness.LittleEndian ? new byte[] { 4, 3, 2, 1, 6, 5 } : new byte[] { 1, 2, 3, 4, 5, 6 };
		Assert.That(bytes, Is.EqualTo(expected));
		Assert.That(serializer.ConstantSize, Is.EqualTo(6));
		Assert.That(serializer.CalculateSize(key), Is.EqualTo(bytes.Length));
		Assert.That(serializer.IsConstantSize, Is.True);
		Assert.That(serializer.SupportsNull, Is.False);
		Assert.That(serializer.DeserializeBytes(bytes, endianness), Is.EqualTo(key));
		Assert.That(() => serializer.DeserializeBytes(bytes.AsSpan(0, bytes.Length - 1), endianness), Throws.TypeOf<EndOfStreamException>());
	}

	[Test]
	public void ThreeComponentsHaveConcatenatedFixedLayout([Values] Endianness endianness) {
		var serializer = new ConstantSizeCompoundKeySerializer<int, short, byte>(PrimitiveSerializer<int>.Instance, PrimitiveSerializer<short>.Instance, PrimitiveSerializer<byte>.Instance);
		var key = CompoundKey.From(0x01020304, (short)0x0506, (byte)7);
		var bytes = serializer.SerializeToBytes(key, endianness);
		var expected = endianness == Endianness.LittleEndian ? new byte[] { 4, 3, 2, 1, 6, 5, 7 } : new byte[] { 1, 2, 3, 4, 5, 6, 7 };
		Assert.That(bytes, Is.EqualTo(expected));
		Assert.That(serializer.ConstantSize, Is.EqualTo(7));
		Assert.That(serializer.CalculateSize(key), Is.EqualTo(bytes.Length));
		Assert.That(serializer.IsConstantSize, Is.True);
		Assert.That(serializer.SupportsNull, Is.False);
		Assert.That(serializer.DeserializeBytes(bytes, endianness), Is.EqualTo(key));
		Assert.That(() => serializer.DeserializeBytes(bytes.AsSpan(0, bytes.Length - 1), endianness), Throws.TypeOf<EndOfStreamException>());
	}

	[Test]
	public void FourComponentsHaveConcatenatedFixedLayout([Values] Endianness endianness) {
		var serializer = new ConstantSizeCompoundKeySerializer<int, short, byte, long>(PrimitiveSerializer<int>.Instance, PrimitiveSerializer<short>.Instance, PrimitiveSerializer<byte>.Instance, PrimitiveSerializer<long>.Instance);
		var key = CompoundKey.From(0x01020304, (short)0x0506, (byte)7, 0x08090A0B0C0D0E0FL);
		var bytes = serializer.SerializeToBytes(key, endianness);
		var expected = endianness == Endianness.LittleEndian
			? new byte[] { 4, 3, 2, 1, 6, 5, 7, 15, 14, 13, 12, 11, 10, 9, 8 }
			: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
		Assert.That(bytes, Is.EqualTo(expected));
		Assert.That(serializer.ConstantSize, Is.EqualTo(15));
		Assert.That(serializer.CalculateSize(key), Is.EqualTo(bytes.Length));
		Assert.That(serializer.IsConstantSize, Is.True);
		Assert.That(serializer.SupportsNull, Is.False);
		Assert.That(serializer.DeserializeBytes(bytes, endianness), Is.EqualTo(key));
		Assert.That(() => serializer.DeserializeBytes(bytes.AsSpan(0, bytes.Length - 1), endianness), Throws.TypeOf<EndOfStreamException>());
	}

	[TestCase(2, 0)]
	[TestCase(2, 1)]
	[TestCase(3, 0)]
	[TestCase(3, 1)]
	[TestCase(3, 2)]
	[TestCase(4, 0)]
	[TestCase(4, 1)]
	[TestCase(4, 2)]
	[TestCase(4, 3)]
	public void MissingComponentSerializerReportsArgumentBeforeDereferencing(int arity, int component) {
		var serializers = new IItemSerializer<int>[] { PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance };
		serializers[component] = null;
		Assert.That(() => CreateSerializer(arity, serializers), Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo($"key{component + 1}Serializer"));
	}

	[TestCase(2, 0)]
	[TestCase(2, 1)]
	[TestCase(3, 0)]
	[TestCase(3, 1)]
	[TestCase(3, 2)]
	[TestCase(4, 0)]
	[TestCase(4, 1)]
	[TestCase(4, 2)]
	[TestCase(4, 3)]
	public void VariableComponentSerializerIsRejectedBeforeReadingConstantSize(int arity, int component) {
		var serializers = new IItemSerializer<int>[] { PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance };
		serializers[component] = new VariableSizeSerializer();
		Assert.That(() => CreateSerializer(arity, serializers), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo($"key{component + 1}Serializer"));
	}

	[Test]
	public void ComponentSizeSumCannotOverflow([Values(2, 3, 4)] int arity) {
		var serializers = new IItemSerializer<int>[] { new DeclaredSizeSerializer(long.MaxValue), PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance };
		Assert.That(() => CreateSerializer(arity, serializers), Throws.TypeOf<OverflowException>());
	}

	[Test]
	public void ComponentsShareTheProvidedContextAndBinaryStreams([Values(2, 3, 4)] int arity) {
		var calls = new List<string>();
		var key1 = new RecordingSerializer("1", calls);
		var key2 = new RecordingSerializer("2", calls);
		var key3 = new RecordingSerializer("3", calls);
		var key4 = new RecordingSerializer("4", calls);
		using var stream = new MemoryStream();
		using var context = new SerializationContext();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, stream);
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		switch (arity) {
			case 2: {
				var serializer = new ConstantSizeCompoundKeySerializer<int, int>(key1, key2);
				serializer.Serialize(CompoundKey.From(1, 2), writer, context);
				stream.Position = 0;
				Assert.That(serializer.Deserialize(reader, context), Is.EqualTo(CompoundKey.From(1, 2)));
				break;
			}
			case 3: {
				var serializer = new ConstantSizeCompoundKeySerializer<int, int, int>(key1, key2, key3);
				serializer.Serialize(CompoundKey.From(1, 2, 3), writer, context);
				stream.Position = 0;
				Assert.That(serializer.Deserialize(reader, context), Is.EqualTo(CompoundKey.From(1, 2, 3)));
				break;
			}
			default: {
				var serializer = new ConstantSizeCompoundKeySerializer<int, int, int, int>(key1, key2, key3, key4);
				serializer.Serialize(CompoundKey.From(1, 2, 3, 4), writer, context);
				stream.Position = 0;
				Assert.That(serializer.Deserialize(reader, context), Is.EqualTo(CompoundKey.From(1, 2, 3, 4)));
				break;
			}
		}
		var expectedCalls = arity == 2 ? new[] { "S1", "S2", "D1", "D2" }
			: arity == 3 ? new[] { "S1", "S2", "S3", "D1", "D2", "D3" }
			: new[] { "S1", "S2", "S3", "S4", "D1", "D2", "D3", "D4" };
		Assert.That(calls, Is.EqualTo(expectedCalls));
		var components = new[] { key1, key2, key3, key4 };
		for (var index = 0; index < arity; index++) {
			Assert.That(components[index].WriteContext, Is.SameAs(context));
			Assert.That(components[index].ReadContext, Is.SameAs(context));
			Assert.That(components[index].Writer, Is.SameAs(writer));
			Assert.That(components[index].Reader, Is.SameAs(reader));
		}
		Assert.That(stream.Position, Is.EqualTo(arity * sizeof(int)));
	}

	private static object CreateSerializer(int arity, IItemSerializer<int>[] serializers) => arity switch {
		2 => new ConstantSizeCompoundKeySerializer<int, int>(serializers[0], serializers[1]),
		3 => new ConstantSizeCompoundKeySerializer<int, int, int>(serializers[0], serializers[1], serializers[2]),
		_ => new ConstantSizeCompoundKeySerializer<int, int, int, int>(serializers[0], serializers[1], serializers[2], serializers[3])
	};

	private sealed class VariableSizeSerializer : ItemSerializerDecorator<int> {
		public VariableSizeSerializer() : base(PrimitiveSerializer<int>.Instance) {
		}

		public override bool IsConstantSize => false;

		public override long ConstantSize => throw new InvalidOperationException("A variable-size serializer has no constant size.");
	}

	private sealed class DeclaredSizeSerializer : ItemSerializerDecorator<int> {
		private readonly long _size;

		public DeclaredSizeSerializer(long size) : base(PrimitiveSerializer<int>.Instance) {
			_size = size;
		}

		public override long ConstantSize => _size;
	}

	private sealed class RecordingSerializer : ItemSerializerDecorator<int> {
		private readonly string _name;
		private readonly List<string> _calls;

		public RecordingSerializer(string name, List<string> calls) : base(PrimitiveSerializer<int>.Instance) {
			_name = name;
			_calls = calls;
		}

		public SerializationContext WriteContext { get; private set; }
		public SerializationContext ReadContext { get; private set; }
		public EndianBinaryWriter Writer { get; private set; }
		public EndianBinaryReader Reader { get; private set; }

		public override void Serialize(int item, EndianBinaryWriter writer, SerializationContext context) {
			WriteContext = context;
			Writer = writer;
			_calls.Add("S" + _name);
			base.Serialize(item, writer, context);
		}

		public override int Deserialize(EndianBinaryReader reader, SerializationContext context) {
			ReadContext = context;
			Reader = reader;
			_calls.Add("D" + _name);
			return base.Deserialize(reader, context);
		}
	}
}
