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
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MerkleRootPublicationTests {

	[Test]
	public void SettledRootsDoNotWriteOrPublishAgain([Values(CHF.SHA2_256, CHF.Blake2b_128)] CHF chf, [Values(1, 3)] int leafCount) {
		using var stream = new WriteCountingStream();
		using var list = CreateList(stream, chf);
		list.AddRange(CreateValues(leafCount));
		var storage = (MerkleTreeStorageAttachment)list.MerkleTree;
		storage.Flush();
		var expectedRoot = storage.Root;
		var notifications = 0;
		storage.RootChanged += (_, _) => notifications++;
		stream.ResetWriteCount();

		for (var i = 0; i < 10; i++) {
			storage.Flush();
			Assert.That(storage.Root, Is.EqualTo(expectedRoot));
		}

		Assert.That(notifications, Is.Zero);
		Assert.That(stream.WriteCount, Is.Zero);
	}

	[Test]
	public void SameContentUpdatesDoNotPublishRootChanges([Values(CHF.SHA2_256, CHF.Blake2b_128)] CHF chf, [Values(1, 3)] int leafCount) {
		using var stream = new MemoryStream();
		using var list = CreateList(stream, chf);
		var values = CreateValues(leafCount);
		list.AddRange(values);
		var storage = (MerkleTreeStorageAttachment)list.MerkleTree;
		var expectedRoot = storage.Root;
		var notifications = 0;
		storage.RootChanged += (_, _) => notifications++;

		list.Update(leafCount - 1, values[^1]);
		Assert.That(notifications, Is.Zero);
		storage.Flush();
		Assert.That(storage.Root, Is.EqualTo(expectedRoot));
		Assert.That(notifications, Is.Zero);
	}

	[Test]
	public void ChangedRootsPublishOnceWhenCalculated([Values(CHF.SHA2_256, CHF.Blake2b_128)] CHF chf, [Values(1, 3)] int leafCount) {
		using var stream = new MemoryStream();
		using var list = CreateList(stream, chf);
		var values = CreateValues(leafCount);
		list.AddRange(values);
		var storage = (MerkleTreeStorageAttachment)list.MerkleTree;
		var oldRoot = storage.Root;
		var notifications = new List<(byte[] Old, byte[] New)>();
		storage.RootChanged += (oldValue, newValue) => notifications.Add((oldValue?.ToArray(), newValue?.ToArray()));

		values[^1] = "changed";
		list.Update(leafCount - 1, values[^1]);
		Assert.That(notifications, Is.Empty);
		storage.Flush();
		Assert.That(notifications, Has.Count.EqualTo(1));
		Assert.That(notifications[0].Old, Is.EqualTo(oldRoot));
		Assert.That(notifications[0].New, Is.EqualTo(CalculateRoot(list, chf, values)));
		Assert.That(storage.Root, Is.EqualTo(notifications[0].New));
		storage.Flush();
		Assert.That(notifications, Has.Count.EqualTo(1));
	}

	[Test]
	public void ReturnedRootsDoNotAliasTheMappedHeaderCache([Values(CHF.SHA2_256, CHF.Blake2b_128)] CHF chf, [Values(1, 3)] int leafCount, [Values] bool dirty) {
		using var stream = new MemoryStream();
		using var list = CreateList(stream, chf);
		var values = CreateValues(leafCount);
		list.AddRange(values);
		var storage = (MerkleTreeStorageAttachment)list.MerkleTree;
		storage.Flush();
		if (dirty) {
			values[^1] = "changed";
			list.Update(leafCount - 1, values[^1]);
		}

		var expectedRoot = CalculateRoot(list, chf, values);
		var returnedRoot = storage.Root;
		Assert.That(returnedRoot, Is.EqualTo(expectedRoot));
		returnedRoot[0] ^= 1;

		// Verify before reading Root again: another root read could repair an aliased cached value.
		using var access = list.ObjectStream.Streams.EnterAccessScope();
		Assert.That(() => storage.VerifyIntegrity(), Throws.Nothing);
		Assert.That(storage.Root, Is.EqualTo(expectedRoot));
		Assert.That(ReadHeaderRoot(list, chf).Value, Is.EqualTo(expectedRoot));
	}

	[Test]
	public void HeaderCacheInvalidationPreservesCorruptionDetectionAndRootPublication([Values(CHF.SHA2_256, CHF.Blake2b_128)] CHF chf) {
		using var stream = new MemoryStream();
		using var list = CreateList(stream, chf);
		list.AddRange(CreateValues(3));
		var storage = (MerkleTreeStorageAttachment)list.MerkleTree;
		var expectedRoot = storage.Root;
		using var access = list.ObjectStream.Streams.EnterAccessScope();
		var headerRoot = ReadHeaderRoot(list, chf);
		using var restore = Tools.Scope.ExecuteOnDispose(() => {
			headerRoot.Value = expectedRoot;
			list.ObjectStream.Streams.Header.FlushCache();
		});
		var corruptRoot = expectedRoot.ToArray();
		corruptRoot[0] ^= 1;
		headerRoot.Value = corruptRoot;
		list.ObjectStream.Streams.Header.FlushCache();

		Assert.That(() => storage.VerifyIntegrity(), Throws.TypeOf<InvalidOperationException>().With.Message.Contains("header root"));
		Assert.That(headerRoot.Value, Is.EqualTo(corruptRoot));

		// Root historically publishes the calculated value; a cached "clean" shortcut must not hide invalidation.
		Assert.That(storage.Root, Is.EqualTo(expectedRoot));
		list.ObjectStream.Streams.Header.FlushCache();
		Assert.That(headerRoot.Value, Is.EqualTo(expectedRoot));
		Assert.That(() => storage.VerifyIntegrity(), Throws.Nothing);
	}

	[Test]
	public void ReapedZeroRootsAndEmptyNullRootsRemainDistinctAfterReopening([Values(CHF.SHA2_256, CHF.Blake2b_128)] CHF chf) {
		byte[] reapedBytes;
		using (var stream = new MemoryStream())
		using (var list = CreateList(stream, chf)) {
			list.Add("alpha");
			list.RemoveAt(0);
			var storage = (MerkleTreeStorageAttachment)list.MerkleTree;
			storage.Flush();
			Assert.That(storage.Root, Is.EqualTo(Hashers.ZeroHash(chf)));
			Assert.That(storage.Size.LeafCount, Is.EqualTo(1));
			reapedBytes = stream.ToArray();
		}

		byte[] emptyBytes;
		using (var stream = CopyToExpandableStream(reapedBytes))
		using (var list = CreateList(stream, chf)) {
			var storage = (MerkleTreeStorageAttachment)list.MerkleTree;
			Assert.That(storage.Root, Is.EqualTo(Hashers.ZeroHash(chf)));
			Assert.That(list.RecycledCount, Is.EqualTo(1));
			Assert.That(list.Count, Is.Zero);
			list.Add("beta");
			Assert.That(list.ListCount, Is.EqualTo(1));
			Assert.That(storage.Root, Is.EqualTo(CalculateRoot(list, chf, new[] { "beta" })));
			list.Clear();
			storage.Flush();
			Assert.That(storage.Root, Is.Null);
			Assert.That(storage.Size.LeafCount, Is.Zero);
			emptyBytes = stream.ToArray();
		}

		using (var stream = CopyToExpandableStream(emptyBytes))
		using (var list = CreateList(stream, chf)) {
			Assert.That(list.MerkleTree.Root, Is.Null);
			Assert.That(list.ListCount, Is.Zero);
		}
	}

	private static StreamMappedMerkleRecyclableList<string> CreateList(MemoryStream stream, CHF chf)
		=> new(stream, chf, 32, new StringSerializer(), autoLoad: true);

	private static string[] CreateValues(int count) => Enumerable.Range(0, count).Select(i => $"value-{i}").ToArray();

	private static byte[] CalculateRoot(StreamMappedMerkleRecyclableList<string> list, CHF chf, IEnumerable<string> values)
		=> MerkleTree.ComputeMerkleRoot(values.Select(value => Hashers.Hash(chf, list.ItemSerializer.SerializeBytesLE(value))), chf);

	private static StreamMappedProperty<byte[]> ReadHeaderRoot(StreamMappedMerkleRecyclableList<string> list, CHF chf) {
		var digestSize = Hashers.GetDigestSizeBytes(chf);
		return list.ObjectStream.Streams.Header.MapExtensionProperty(0, sizeof(bool) + digestSize, new ConstantSizeNullableByteArraySerializer(digestSize));
	}

	private static MemoryStream CopyToExpandableStream(byte[] bytes) {
		var stream = new MemoryStream();
		stream.Write(bytes);
		stream.Position = 0;
		return stream;
	}

	private sealed class WriteCountingStream : MemoryStream {
		public int WriteCount { get; private set; }

		public void ResetWriteCount() => WriteCount = 0;

		public override void Write(byte[] buffer, int offset, int count) {
			WriteCount++;
			base.Write(buffer, offset, count);
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			WriteCount++;
			base.Write(buffer);
		}

		public override void WriteByte(byte value) {
			WriteCount++;
			base.WriteByte(value);
		}
	}
}
