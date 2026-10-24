// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class PaddingDiscardSpanTests {

	[Test]
	public void DiscardMatchesReadBytesConsumptionAcrossShortReads([Values(0, 1, 511, 512, 513, 4096)] int count, [Values(0, 17, 4100)] int available) {
		using var stream = new LimitedReadStream(new byte[available]);
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		reader.DiscardBytes(count);
		Assert.That(stream.Position, Is.EqualTo(Math.Min(count, available)));
	}

	[Test]
	public void DiscardPreservesLengthValidationAndDisposedPrecedence() {
		var stream = new MemoryStream();
		var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		Assert.That(() => reader.DiscardBytes(-1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("count"));
		Assert.That(() => reader.DiscardBytes((long)int.MaxValue + 1), Throws.TypeOf<NotImplementedException>());
		reader.Dispose();
		Assert.That(() => reader.DiscardBytes(-1), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => reader.DiscardBytes(0), Throws.TypeOf<ObjectDisposedException>());
	}

	[TestCase(16)]
	[TestCase(2049)]
	[TestCase(8192)]
	public void PaddedWritesKeepZeroBytesAndReadsStopBeforeTheNextItem(int size) {
		var serializer = PrimitiveSerializer<byte>.Instance.AsConstantSize(size);
		using var context = SerializationContext.New;
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, new NonClosingStream(stream));
		serializer.Serialize(42, writer, context);
		writer.Write((byte)99);
		var expected = new byte[size + 1];
		expected[0] = 42;
		expected[size] = 99;
		Assert.That(stream.ToArray(), Is.EqualTo(expected));
		stream.Position = 0;
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, new NonClosingStream(stream));
		Assert.That(serializer.Deserialize(reader, context), Is.EqualTo(42));
		Assert.That(reader.ReadByte(), Is.EqualTo(99));
	}

	[Test]
	public void MissingPaddingRetainsItsExistingEofBehavior() {
		var serializer = PrimitiveSerializer<byte>.Instance.AsConstantSize(8192);
		using var context = SerializationContext.New;
		using var stream = new MemoryStream(new byte[] { 42, 0, 0 });
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		Assert.That(serializer.Deserialize(reader, context), Is.EqualTo(42));
		Assert.That(stream.Position, Is.EqualTo(3));
	}

	[Test]
	public void NullablePreservesItsExistingUnconditionalConstantSizeSkip([Values] bool hasValue, [Values] bool truncated) {
		var serializer = new NullableSerializer<int>(PrimitiveSerializer<int>.Instance, preserveConstantSize: true);
		using var context = SerializationContext.New;
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, new NonClosingStream(stream));
		writer.Write(hasValue);
		if (hasValue)
			writer.Write(42);
		writer.Write(new byte[truncated ? 2 : 4]);
		if (!truncated)
			writer.Write((byte)99);
		stream.Position = 0;
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, new NonClosingStream(stream));
		Assert.That(serializer.Deserialize(reader, context), Is.EqualTo(hasValue ? (int?)42 : null));
		if (truncated)
			Assert.That(stream.Position, Is.EqualTo(stream.Length));
		else
			Assert.That(reader.ReadByte(), Is.EqualTo(99));
	}

	[Test]
	public void NullReferencePaddingRetainsItsDiscriminatorAndEofBehavior([Values] bool truncated) {
		var serializer = new ConstantSizeReferenceTypeSerializer<byte[]>(new ReferenceSerializer<byte[]>(new ConstantSizeByteArraySerializer(1024), ReferenceSerializerMode.SupportNull));
		using var writeContext = SerializationContext.New;
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, new NonClosingStream(stream));
		serializer.Serialize(null, writer, writeContext);
		if (truncated)
			stream.SetLength(3);
		else
			writer.Write((byte)99);
		stream.Position = 0;
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, new NonClosingStream(stream));
		using var readContext = SerializationContext.New;
		Assert.That(serializer.Deserialize(reader, readContext), Is.Null);
		if (truncated)
			Assert.That(stream.Position, Is.EqualTo(3));
		else
			Assert.That(reader.ReadByte(), Is.EqualTo(99));
	}

	private sealed class LimitedReadStream : MemoryStream {
		public LimitedReadStream(byte[] data)
			: base(data) {
		}

		public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(3, buffer.Length)]);
	}
}
