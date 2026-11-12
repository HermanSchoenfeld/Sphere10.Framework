// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.IO;

using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
public sealed class ClusteredStreamsReadOnlyMetaDataTests {
	[Test]
	public void BorrowedReadOnlyHeaderInspectionPreservesPositionAndMetadata([Values] Endianness endianness, [Values] bool hasMetadata) {
		using var raw = new MemoryStream();
		using (var streams = new ClusteredStreams(raw, 4096, endianness: endianness)) {
			if (hasMetadata)
				streams.ConfigureMetaData(() => StringSerializer.UTF8.SerializeToBytes("manifest", endianness), 128);
			streams.Load();
			streams.AddBytes(new byte[] { 1, 2, 3 });
		}
		using var source = new MemoryStream(raw.ToArray(), writable: false);
		source.Position = 17;
		Assert.That(ClusteredStreams.TryReadMetaData(source, 128, out var metadata, endianness), Is.EqualTo(hasMetadata));
		Assert.That(source.Position, Is.EqualTo(17));
		if (hasMetadata) {
			Assert.That(StringSerializer.UTF8.DeserializeBytes(metadata, endianness), Is.EqualTo("manifest"));
			Assert.That(ClusteredStreams.TryReadMetaData(source, 128, StringSerializer.UTF8, out var typed, endianness), Is.True);
			Assert.That(typed, Is.EqualTo("manifest"));
			Assert.That(source.Position, Is.EqualTo(17));
			Assert.That(() => ClusteredStreams.TryReadMetaData(source, 64, out _, endianness), Throws.InvalidOperationException);
			Assert.That(source.Position, Is.EqualTo(17));
		} else
			Assert.That(metadata, Is.Empty);
	}
}
