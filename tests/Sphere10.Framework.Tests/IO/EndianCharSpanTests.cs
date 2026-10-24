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
public class EndianCharSpanTests {

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(255)]
	[TestCase(256)]
	[TestCase(257)]
	[TestCase(4096)]
	public void CharacterSpansKeepTheirExistingLengthAndZeroPadding(int length) {
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, stream, Encoding.UTF8);
		var text = new string('A', length);
		writer.Write(text.AsSpan());
		var expected = new byte[2 * length];
		expected.AsSpan(0, length).Fill((byte)'A');
		Assert.That(stream.ToArray(), Is.EqualTo(expected));
	}

	[Test]
	public void MultibyteCharactersRetainTheExistingEncodingAndPadding() {
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Big, stream, Encoding.UTF8);
		writer.Write("éA".AsSpan());
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0xC3, 0xA9, 0x41, 0 }));
	}

	[Test]
	public void SingleCharacterWritesUseTheSameFixedSizeContract() {
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, stream, Encoding.UTF8);
		writer.Write('A');
		writer.Write('é');
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0x41, 0, 0xC3, 0xA9 }));
	}

	[TestCase(1)]
	[TestCase(257)]
	public void EncodingOverflowDoesNotWritePartialBytesAndAllowsAnotherWrite(int length) {
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, stream, Encoding.UTF8);
		var tooWide = new string('漢', length);
		Assert.That(() => writer.Write(tooWide.AsSpan()), Throws.ArgumentException);
		Assert.That(stream.Length, Is.Zero);
		writer.Write(new string('A', length).AsSpan());
		var expected = new byte[2 * length];
		expected.AsSpan(0, length).Fill((byte)'A');
		Assert.That(stream.ToArray(), Is.EqualTo(expected));
	}

	[Test]
	public void StrictEncoderFailureDoesNotWriteOrChangeSubsequentPadding() {
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, stream, new UTF8Encoding(false, true));
		var invalid = new string('\uD800', 257);
		Assert.That(() => writer.Write(invalid.AsSpan()), Throws.TypeOf<EncoderFallbackException>());
		Assert.That(stream.Length, Is.Zero);
		writer.Write("A".AsSpan());
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0x41, 0 }));
	}

	[Test]
	public void DisposedWriterRejectsCharacterSpans() {
		var stream = new MemoryStream();
		var writer = new EndianBinaryWriter(EndianBitConverter.Little, stream);
		writer.Dispose();
		Assert.That(() => writer.Write("A".AsSpan()), Throws.TypeOf<ObjectDisposedException>());
	}
}
