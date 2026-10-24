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
using MemoryNode = Sphere10.Framework.InMemoryBTreePlus<int, int>.Node;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BTreePlusRemovalRegressionTests {

	[TestCase(RemovalScenario.InternalBorrowLeft)]
	[TestCase(RemovalScenario.InternalBorrowRight)]
	[TestCase(RemovalScenario.InternalMergeLeft)]
	[TestCase(RemovalScenario.InternalMergeRight)]
	[TestCase(RemovalScenario.LeafBorrowLeft)]
	[TestCase(RemovalScenario.LeafBorrowRight)]
	[TestCase(RemovalScenario.LeafMergeLeft)]
	[TestCase(RemovalScenario.LeafMergeRight)]
	[TestCase(RemovalScenario.MinimumWithoutUnderflow)]
	public void RemovalRepairsSeparatorsThroughTheRequiredUnderflowTransition(RemovalScenario scenario) {
		var (root, removedKey) = CreateTopology(scenario);
		var tree = new ObservedMemoryTree(4);
		tree.Seed(root);
		var expected = new SortedDictionary<int, int>(tree.ToDictionary(pair => pair.Key, pair => pair.Value));
		Assert.That(tree.Validate(out var error), Is.True, error);
		Assert.That(expected.Remove(removedKey), Is.True);
		Assert.That(tree.Remove(removedKey), Is.True);
		if (scenario == RemovalScenario.MinimumWithoutUnderflow)
			Assert.That(tree.UnderflowTransitions, Is.Zero);
		else
			Assert.That(tree.TransitionCount(scenario), Is.EqualTo(1), "The fixture must exercise its named merge or rotation.");
		Assert.That(tree.Validate(out error), Is.True, error);
		Assert.That(tree.Count, Is.EqualTo(expected.Count));
		Assert.That(tree.ToArray(), Is.EqualTo(expected.ToArray()));
		Assert.That(tree.TryGetValue(removedKey, out _), Is.False);
		foreach (var entry in expected)
			Assert.That(tree[entry.Key], Is.EqualTo(entry.Value));
		Assert.That(tree.FindRange(20, 65).ToArray(), Is.EqualTo(expected.Where(pair => pair.Key >= 20 && pair.Key <= 65).ToArray()));

		// A later insertion must also follow the repaired separator path.
		tree.Add(removedKey, -removedKey);
		expected.Add(removedKey, -removedKey);
		Assert.That(tree.Validate(out error), Is.True, error);
		Assert.That(tree.ToArray(), Is.EqualTo(expected.ToArray()));
	}

	[Test]
	public void RemovalDoesNotRewriteSeparatorsInAnUnchangedSiblingSubtree() {
		var unchanged = Branch(Leaf(0, 1, 2), Leaf(10, 11, 12), Leaf(20, 21, 22));
		var changed = Branch(Leaf(30, 31, 32), Leaf(40, 41, 42), Leaf(50, 51, 52));
		var tree = new ObservedMemoryTree(4);
		tree.Seed(Branch(unchanged, changed));
		Assert.That(tree.Validate(out var error), Is.True, error);
		Assert.That(tree.Remove(41), Is.True);
		Assert.That(tree.UnderflowTransitions, Is.Zero);
		Assert.That(tree.SeparatorWritesTo(unchanged), Is.Zero, "Deleting elsewhere must not rewrite the unchanged subtree's persisted separators.");
		Assert.That(tree.Validate(out error), Is.True, error);
		Assert.That(tree.Count, Is.EqualTo(17));
		Assert.That(tree.Keys, Is.EqualTo(new[] { 0, 1, 2, 10, 11, 12, 20, 21, 22, 30, 31, 32, 40, 42, 50, 51, 52 }));
	}

	[TestCase(3, Endianness.LittleEndian, false, BTreeType.BTree)]
	[TestCase(3, Endianness.LittleEndian, false, BTreeType.BTreePlus)]
	[TestCase(4, Endianness.BigEndian, false, BTreeType.BTree)]
	[TestCase(4, Endianness.BigEndian, false, BTreeType.BTreePlus)]
	[TestCase(5, Endianness.LittleEndian, true, BTreeType.BTree)]
	[TestCase(5, Endianness.LittleEndian, true, BTreeType.BTreePlus)]
	[TestCase(8, Endianness.BigEndian, true, BTreeType.BTree)]
	[TestCase(8, Endianness.BigEndian, true, BTreeType.BTreePlus)]
	public void StreamMappedDeletionMatchesTheOracleThroughRootCollapseAndReopen(int order, Endianness endianness, bool descending, BTreeType implementation) {
		var comparer = descending ? Comparer<int>.Create((left, right) => right.CompareTo(left)) : Comparer<int>.Default;
		var expected = new SortedDictionary<int, int>(comparer);
		using var stream = new MemoryStream();
		var spec = new BTreeSpec(implementation, order);
		var tree = OpenTree(spec, stream, comparer, endianness);
		using var treeLifetime = (IDisposable)tree;
		var random = new Random(31337);
		var insertionOrder = Enumerable.Range(0, 96).OrderBy(_ => random.Next()).ToArray();
		foreach (var key in insertionOrder) {
			tree.Add(key, key * 10);
			expected.Add(key, key * 10);
		}
		Assert.That(tree.Validate(out var error), Is.True, error);
		var removalOrder = insertionOrder.OrderBy(_ => random.Next()).ToArray();
		for (var index = 0; index < removalOrder.Length; index++) {
			var key = removalOrder[index];
			Assert.That(tree.Remove(key), Is.EqualTo(expected.Remove(key)));
			Assert.That(tree.Validate(out error), Is.True, error);
			Assert.That(tree.Count, Is.EqualTo(expected.Count));
			Assert.That(tree.ToArray(), Is.EqualTo(expected.ToArray()));
			Assert.That(tree.TryGetValue(key, out _), Is.False);
			foreach (var entry in expected)
				Assert.That(tree[entry.Key], Is.EqualTo(entry.Value));
			if (expected.Count > 0)
				Assert.That(tree.FindRange(expected.Keys.First(), expected.Keys.Last()).ToArray(), Is.EqualTo(expected.ToArray()));

			if ((index + 1) % 8 != 0)
				continue;
			using var snapshot = new MemoryStream(stream.ToArray());
			var reopened = OpenTree(spec, snapshot, comparer, endianness);
			using var reopenedLifetime = (IDisposable)reopened;
			Assert.That(reopened.Validate(out error), Is.True, error);
			Assert.That(reopened.Count, Is.EqualTo(expected.Count));
			Assert.That(reopened.ToArray(), Is.EqualTo(expected.ToArray()));
		}
		Assert.That(tree.Count, Is.Zero);
		Assert.That(tree, Is.Empty);
		tree.Add(7, 70);
		Assert.That(tree.Validate(out error), Is.True, error);
		Assert.That(tree[7], Is.EqualTo(70));
	}

	private static IBTree<int, int> OpenTree(BTreeSpec spec, Stream stream, IComparer<int> comparer, Endianness endianness)
		=> spec.Type == BTreeType.BTree
			? new StreamMappedBTree<int, int>(spec.Order, stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, comparer, endianness)
			: new StreamMappedBTreePlus<int, int>(spec.Order, stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, comparer, endianness);

	private static (MemoryNode Root, int RemovedKey) CreateTopology(RemovalScenario scenario) => scenario switch {
		RemovalScenario.InternalBorrowLeft => (
			Branch(Branch(Leaf(10, 15), Leaf(20, 25), Leaf(30, 35)), Branch(Leaf(50, 55), Leaf(60, 65))), 50),
		RemovalScenario.InternalBorrowRight => (
			Branch(Branch(Leaf(10, 15), Leaf(20, 25)), Branch(Leaf(50, 55), Leaf(60, 65), Leaf(70, 75))), 10),
		RemovalScenario.InternalMergeLeft => (
			Branch(Branch(Leaf(10, 15), Leaf(20, 25)), Branch(Leaf(50, 55), Leaf(60, 65))), 50),
		RemovalScenario.InternalMergeRight => (
			Branch(Branch(Leaf(10, 15), Leaf(20, 25)), Branch(Leaf(50, 55), Leaf(60, 65))), 10),
		RemovalScenario.LeafBorrowLeft => (Branch(Leaf(10, 15, 18), Leaf(30, 35)), 30),
		RemovalScenario.LeafBorrowRight => (Branch(Leaf(10, 15), Leaf(30, 35, 38)), 10),
		RemovalScenario.LeafMergeLeft => (Branch(Leaf(10, 15), Leaf(30, 35)), 30),
		RemovalScenario.LeafMergeRight => (Branch(Leaf(10, 15), Leaf(30, 35)), 10),
		_ => (Branch(Leaf(10, 15, 18), Leaf(30, 35, 38)), 30)
	};

	private static MemoryNode Leaf(params int[] keys) {
		var node = new MemoryNode(true, 4);
		foreach (var key in keys)
			node.Entries.Add(new KeyValuePair<int, int>(key, key * 10));
		return node;
	}

	private static MemoryNode Branch(params MemoryNode[] children) {
		var node = new MemoryNode(false, 4);
		for (var index = 0; index < children.Length; index++) {
			node.Children.Add(children[index]);
			if (index > 0) {
				var firstLeaf = children[index];
				while (!firstLeaf.IsLeaf)
					firstLeaf = firstLeaf.Children[0];
				node.Keys.Add(firstLeaf.Entries[0].Key);
			}
		}
		return node;
	}

	public enum RemovalScenario {
		InternalBorrowLeft,
		InternalBorrowRight,
		InternalMergeLeft,
		InternalMergeRight,
		LeafBorrowLeft,
		LeafBorrowRight,
		LeafMergeLeft,
		LeafMergeRight,
		MinimumWithoutUnderflow
	}

	private sealed class ObservedMemoryTree : InMemoryBTreePlus<int, int> {
		private readonly int[] _transitions = new int[9];
		private readonly Dictionary<MemoryNode, int> _separatorWrites = new();

		public ObservedMemoryTree(int order) : base(order) {
		}

		public int UnderflowTransitions => _transitions.Sum();

		public int TransitionCount(RemovalScenario scenario) => _transitions[(int)scenario];

		public int SeparatorWritesTo(MemoryNode node) => _separatorWrites.TryGetValue(node, out var writes) ? writes : 0;

		public void Seed(MemoryNode root) {
			var leaves = new List<MemoryNode>();
			CollectLeaves(root, leaves);
			for (var index = 0; index < leaves.Count; index++)
				leaves[index].NextLeaf = index + 1 < leaves.Count ? leaves[index + 1] : null;
			SetRoot(root);
			Count = leaves.Sum(leaf => leaf.Entries.Count);
		}

		protected override void SetInternalKey(MemoryNode node, int index, int key) {
			_separatorWrites[node] = SeparatorWritesTo(node) + 1;
			base.SetInternalKey(node, index, key);
		}

		protected override void BorrowFromLeft(MemoryNode parent, int childIndex) {
			var scenario = parent.Children[childIndex].IsLeaf ? RemovalScenario.LeafBorrowLeft : RemovalScenario.InternalBorrowLeft;
			_transitions[(int)scenario]++;
			base.BorrowFromLeft(parent, childIndex);
		}

		protected override void BorrowFromRight(MemoryNode parent, int childIndex) {
			var scenario = parent.Children[childIndex].IsLeaf ? RemovalScenario.LeafBorrowRight : RemovalScenario.InternalBorrowRight;
			_transitions[(int)scenario]++;
			base.BorrowFromRight(parent, childIndex);
		}

		protected override void MergeChildren(MemoryNode parent, int leftIndex) {
			var left = parent.Children[leftIndex];
			var leftUnderflow = left.IsLeaf ? left.Entries.Count < MinLeafKeys : left.Keys.Count < MinInternalKeys;
			var scenario = left.IsLeaf
				? (leftUnderflow ? RemovalScenario.LeafMergeRight : RemovalScenario.LeafMergeLeft)
				: (leftUnderflow ? RemovalScenario.InternalMergeRight : RemovalScenario.InternalMergeLeft);
			_transitions[(int)scenario]++;
			base.MergeChildren(parent, leftIndex);
		}

		private static void CollectLeaves(MemoryNode node, ICollection<MemoryNode> leaves) {
			if (node.IsLeaf) {
				leaves.Add(node);
				return;
			}
			foreach (var child in node.Children)
				CollectLeaves(child, leaves);
		}
	}
}
