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
using Sphere10.Framework.Collections;

namespace Sphere10.Framework.Tests.Merkle;

[TestFixture]
[NonParallelizable]
public class FlatMerkleTreeRegressionTests {

	[Test]
	public void MutationShapesPreserveTheIndependentRoot(
		[Values(false, true)] bool streamBacked,
		[Values(CHF.SHA2_256, CHF.Blake2b_128, CHF.Blake2b_512)] CHF chf) {
		using var stream = new MemoryStream();
		var tree = CreateTree(streamBacked, chf, stream);
		var leaves = new List<byte[]>();
		AssertTree(tree, leaves);

		// Cross both perfect and imperfect roots without duplicating the broad random suites.
		foreach (var count in new[] { 1, 2, 3, 7, 8, 9 }) {
			var additions = Enumerable.Range(leaves.Count, count - leaves.Count).Select(i => CreateDigest(chf, i)).ToArray();
			leaves.AddRange(additions);
			tree.Leafs.AddRange(additions);
			AssertTree(tree, leaves);
		}

		leaves[0] = CreateDigest(chf, 41);
		tree.Leafs.UpdateRange(0, new[] { leaves[0] });
		AssertTree(tree, leaves);

		var inserted = CreateDigest(chf, 42);
		leaves.Insert(4, inserted);
		tree.Leafs.InsertRange(4, new[] { inserted });
		AssertTree(tree, leaves);

		leaves.RemoveRange(2, 3);
		tree.Leafs.RemoveRange(2, 3);
		AssertTree(tree, leaves);

		tree.Leafs.RemoveRange(tree.Leafs.Count, 0);
		AssertTree(tree, leaves);
		leaves.Clear();
		tree.Leafs.Clear();
		AssertTree(tree, leaves);
	}

	[Test]
	public void ReturnedRootsAreDetachedFromTheTreeAndLaterRebuilds(
		[Values(false, true)] bool streamBacked,
		[Values(CHF.SHA2_256, CHF.Blake2b_128, CHF.Blake2b_512)] CHF chf) {
		foreach (var count in new[] { 1, 3, 8 }) {
			using var stream = new MemoryStream();
			var tree = CreateTree(streamBacked, chf, stream);
			var leaves = Enumerable.Range(0, count).Select(i => CreateDigest(chf, i)).ToArray();
			tree.Leafs.AddRange(leaves);
			var expectedRoot = ComputeRoot(chf, leaves);
			var mutableRoot = tree.Root;
			mutableRoot[0] ^= 0xff;
			Assert.That(tree.Root, Is.EqualTo(expectedRoot));

			var retainedRoot = tree.Root;
			leaves[0] = CreateDigest(chf, 43);
			tree.Leafs.UpdateRange(0, new[] { leaves[0] });
			Assert.That(tree.Root, Is.EqualTo(ComputeRoot(chf, leaves)));
			Assert.That(retainedRoot, Is.EqualTo(expectedRoot));
		}
	}

	[Test]
	public void FailedParentWriteCanBeRetried(
		[Values(false, true)] bool streamBacked,
		[Values(CHF.SHA2_256, CHF.Blake2b_128)] CHF chf) {
		using var stream = new MemoryStream();
		IBuffer backing = streamBacked ? new StreamMappedBuffer(stream) : new MemoryBuffer();
		var buffer = new FailingWriteBuffer(backing);
		var tree = new FlatMerkleTree(chf, buffer);
		var leaves = Enumerable.Range(0, 8).Select(i => CreateDigest(chf, i)).ToArray();
		tree.Leafs.AddRange(leaves);
		tree.EnsureComputed();
		leaves[6] = CreateDigest(chf, 44);
		tree.Leafs.UpdateRange(6, new[] { leaves[6] });
		buffer.FailNextWrite = true;

		Assert.That(() => tree.EnsureComputed(), Throws.TypeOf<IOException>());
		Assert.That(buffer.FailNextWrite, Is.False);
		AssertTree(tree, leaves);
	}

	private static FlatMerkleTree CreateTree(bool streamBacked, CHF chf, MemoryStream stream) {
		IBuffer buffer = streamBacked ? new StreamMappedBuffer(stream) : new MemoryBuffer();
		return new FlatMerkleTree(chf, buffer);
	}

	private static byte[] CreateDigest(CHF chf, int seed) {
		var result = new byte[Hashers.GetDigestSizeBytes(chf)];
		for (var i = 0; i < result.Length; i++)
			result[i] = unchecked((byte)(17 * seed + i));
		return result;
	}

	private static void AssertTree(FlatMerkleTree tree, IReadOnlyList<byte[]> leaves) {
		var expectedRoot = ComputeRoot(tree.HashAlgorithm, leaves);
		Assert.That(tree.Size.LeafCount, Is.EqualTo(leaves.Count));
		Assert.That(tree.Root, Is.EqualTo(expectedRoot));
		Assert.That(tree.Leafs.ToArray(), Is.EqualTo(leaves));
		var bytes = tree.ToBytes();
		var restored = new FlatMerkleTree(tree.HashAlgorithm, new MemoryBuffer(bytes), leaves.Count);
		Assert.That(restored.Root, Is.EqualTo(expectedRoot));
		if (leaves.Count == 0)
			Assert.That(bytes, Is.Empty);
	}

	private static byte[] ComputeRoot(CHF chf, IReadOnlyList<byte[]> leaves) {
		if (leaves.Count == 0)
			return null;
		var level = leaves.Select(x => (byte[])x.Clone()).ToList();
		while (level.Count > 1) {
			var parents = new List<byte[]>((level.Count + 1) / 2);
			for (var i = 0; i < level.Count; i += 2)
				parents.Add(i + 1 < level.Count ? Hashers.JoinHash(chf, level[i], level[i + 1]) : level[i]);
			level = parents;
		}
		return level[0];
	}

	private sealed class FailingWriteBuffer : BufferDecorator<IBuffer> {
		public FailingWriteBuffer(IBuffer buffer) : base(buffer) {
		}

		public bool FailNextWrite { get; set; }

		public override void UpdateRange(long index, ReadOnlySpan<byte> items) {
			CheckWrite();
			base.UpdateRange(index, items);
		}

		public override void UpdateRange(long index, IEnumerable<byte> items) {
			CheckWrite();
			base.UpdateRange(index, items);
		}

		private void CheckWrite() {
			if (!FailNextWrite)
				return;
			FailNextWrite = false;
			throw new IOException("Injected parent-write failure");
		}
	}
}
