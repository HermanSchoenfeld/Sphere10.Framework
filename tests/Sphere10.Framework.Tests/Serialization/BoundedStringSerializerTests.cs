// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.IO;
using System.Text;

using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
public sealed class BoundedStringSerializerTests {
	[Test]
	public void RejectsOversizedLengthPrefixBeforeReadingOrRentingPayload() {
		var serializer = new StringSerializer(Encoding.UTF8, SizeDescriptorStrategy.UseUInt32, 32);
		using var context = SerializationContext.New;
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, new NonClosingStream(stream));
		writer.Write(1000000000U);
		stream.Position = 0;
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, new NonClosingStream(stream));
		Assert.That(() => serializer.Deserialize(reader, context), Throws.InvalidOperationException);
		Assert.That(stream.Position, Is.EqualTo(4), "Only the size prefix may be consumed before rejecting the payload.");
	}

	[Test]
	public void BoundCountsEncodedBytesAndPreservesExistingWireFormatAtTheLimit() {
		var bounded = new StringSerializer(Encoding.UTF8, maximumByteCount: 4);
		var unlimited = StringSerializer.UTF8;
		var bytes = bounded.SerializeBytesLE("éé");
		Assert.That(bytes, Is.EqualTo(unlimited.SerializeBytesLE("éé")));
		Assert.That(bounded.DeserializeBytesLE(bytes), Is.EqualTo("éé"));
		Assert.That(() => bounded.SerializeBytesLE("ééé"), Throws.InvalidOperationException);
	}
}
