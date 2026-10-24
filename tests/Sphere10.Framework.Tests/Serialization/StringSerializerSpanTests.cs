// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StringSerializerSpanTests {

	[Test]
	public void StringsRetainTheirPrefixPayloadAndShortReadDecoding([Values(0, 1, 511, 512, 513, 4096)] int length, [Values] Endianness endianness) {
		var text = new string('A', length);
		var serializer = new StringSerializer(Encoding.UTF8, SizeDescriptorStrategy.UseUInt32);
		using var context = SerializationContext.New;
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.For(endianness), new NonClosingStream(stream));
		serializer.Serialize(text, writer, context);
		Assert.That(stream.Length, Is.EqualTo(length + 4));
		stream.Position = 0;
		using var reader = new EndianBinaryReader(EndianBitConverter.For(endianness), new NonClosingStream(stream));
		Assert.That(reader.ReadUInt32(), Is.EqualTo(length));
		Assert.That(reader.ReadBytes(length), Is.EqualTo(Encoding.UTF8.GetBytes(text)));
		stream.Position = 0;
		using var shortReader = new EndianBinaryReader(EndianBitConverter.For(endianness), new ShortReadStream(stream));
		Assert.That(serializer.Deserialize(shortReader, context), Is.EqualTo(text));
	}

	[TestCase(0)]
	[TestCase(7)]
	[TestCase(513)]
	public void TruncatedPayloadIsDecodedWithoutZeroPadding(int available) {
		var serializer = new StringSerializer(Encoding.UTF8, SizeDescriptorStrategy.UseUInt32);
		using var context = SerializationContext.New;
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, new NonClosingStream(stream));
		writer.Write((uint)(available + 3));
		writer.Write(Encoding.UTF8.GetBytes(new string('A', available)));
		stream.Position = 0;
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, new ShortReadStream(stream));
		Assert.That(serializer.Deserialize(reader, context), Is.EqualTo(new string('A', available)));
		Assert.That(stream.Position, Is.EqualTo(stream.Length));
	}

	[Test]
	public void IncompleteUtf8RetainsReplacementOrStrictDecoderBehavior([Values] bool strict) {
		var serializer = new StringSerializer(new UTF8Encoding(false, strict), SizeDescriptorStrategy.UseByte);
		using var context = SerializationContext.New;
		using var stream = new MemoryStream(new byte[] { 2, 0xC3 });
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		if (strict)
			Assert.That(() => serializer.Deserialize(reader, context), Throws.TypeOf<DecoderFallbackException>());
		else
			Assert.That(serializer.Deserialize(reader, context), Is.EqualTo("\uFFFD"));
		Assert.That(stream.Position, Is.EqualTo(2));
	}

	[TestCase(-1L)]
	[TestCase(2147483648L)]
	public void InvalidDeclaredLengthsFailBeforePayloadReads(long size) {
		var serializer = new StringSerializer(Encoding.UTF8, SizeDescriptorStrategy.UseULong);
		using var context = SerializationContext.New;
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, new NonClosingStream(stream));
		writer.Write(size);
		stream.Position = 0;
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, new NonClosingStream(stream));
		if (size < 0)
			Assert.That(() => serializer.Deserialize(reader, context), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("count"));
		else
			Assert.That(() => serializer.Deserialize(reader, context), Throws.TypeOf<NotImplementedException>());
		Assert.That(stream.Position, Is.EqualTo(8));
	}

	[Test]
	public void CustomConversionFailuresRetainPrefixAndPayloadTiming([Values(1, 513)] int length, [Values] bool decoding) {
		var encoding = new FailingEncoding { FailBytes = !decoding, FailString = decoding };
		var serializer = new StringSerializer(encoding, SizeDescriptorStrategy.UseUInt32);
		using var context = SerializationContext.New;
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, new NonClosingStream(stream));
		var text = new string('A', length);
		if (!decoding) {
			Assert.That(() => serializer.Serialize(text, writer, context), Throws.Exception.SameAs(encoding.Error));
			Assert.That(stream.Length, Is.EqualTo(4));
		} else {
			writer.Write((uint)length);
			writer.Write(Encoding.UTF8.GetBytes(text));
			stream.Position = 0;
			using var reader = new EndianBinaryReader(EndianBitConverter.Little, new NonClosingStream(stream));
			Assert.That(() => serializer.Deserialize(reader, context), Throws.Exception.SameAs(encoding.Error));
			Assert.That(stream.Position, Is.EqualTo(length + 4));
		}
	}

	private sealed class ShortReadStream : StreamDecorator {
		public ShortReadStream(Stream stream)
			: base(stream) {
		}

		public override int Read(Span<byte> buffer) => InnerStream.Read(buffer[..Math.Min(3, buffer.Length)]);
	}

	private sealed class FailingEncoding : Encoding {
		public bool FailBytes { get; set; }
		public bool FailString { get; set; }
		public Exception Error { get; } = new InvalidOperationException("Injected encoding failure");
		public override int GetByteCount(char[] chars, int index, int count) => UTF8.GetByteCount(chars, index, count);
		public override int GetByteCount(string value) => UTF8.GetByteCount(value);
		public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
			=> FailBytes ? throw Error : UTF8.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
		public override byte[] GetBytes(string value) => FailBytes ? throw Error : UTF8.GetBytes(value);
		public override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes) => FailBytes ? throw Error : UTF8.GetBytes(chars, bytes);
		public override int GetCharCount(byte[] bytes, int index, int count) => FailString ? throw Error : UTF8.GetCharCount(bytes, index, count);
		public override int GetCharCount(ReadOnlySpan<byte> bytes) => FailString ? throw Error : UTF8.GetCharCount(bytes);
		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
			=> FailString ? throw Error : UTF8.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
		public override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars) => FailString ? throw Error : UTF8.GetChars(bytes, chars);
		public override string GetString(byte[] bytes, int index, int count) => FailString ? throw Error : UTF8.GetString(bytes, index, count);
		public override int GetMaxByteCount(int charCount) => UTF8.GetMaxByteCount(charCount);
		public override int GetMaxCharCount(int byteCount) => UTF8.GetMaxCharCount(byteCount);
	}
}
