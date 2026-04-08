// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.NUnit;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BTreeTests {

	[Test]
	public void IntegrationTest([Range(3, 15)] int order) {
		var keyGens = 0;
		var tree = new MemoryBTree<string, TestObject>(order);
		AssertEx.DictionaryIntegrationTest(
			tree,
			500,
			(rng) => ($"{keyGens++}_{rng.NextString(0, 100)}", new TestObject(rng)),
			iterations: 500,
			valueComparer: new TestObjectEqualityComparer(),
			endOfIterTest: () => {
				// Verify in-order traversal yields sorted keys
				var result = tree.Validate(out var error);
				Assert.That(result, Is.True, error);
			}
		);
	}

	[Test]
	public void Add_SingleItem() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		Assert.That(tree.Count, Is.EqualTo(1));
		Assert.That(tree[1], Is.EqualTo("one"));
	}

	[Test]
	public void Add_MultipleItems([Range(3, 15)] int order) {
		var tree = new MemoryBTree<int, string>(order);
		tree.Add(3, "three");
		tree.Add(1, "one");
		tree.Add(2, "two");
		Assert.That(tree.Count, Is.EqualTo(3));
		Assert.That(tree[1], Is.EqualTo("one"));
		Assert.That(tree[2], Is.EqualTo("two"));
		Assert.That(tree[3], Is.EqualTo("three"));
	}

	[Test]
	public void Add_DuplicateKey_Throws() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		Assert.That(() => tree.Add(1, "duplicate"), Throws.InstanceOf<InvalidOperationException>());
	}

	[Test]
	public void Add_KVP() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(new KeyValuePair<int, string>(1, "one"));
		Assert.That(tree.Count, Is.EqualTo(1));
		Assert.That(tree[1], Is.EqualTo("one"));
	}

	[Test]
	public void Add_ManyItems([Range(3, 15)] int order) {
		var tree = new MemoryBTree<int, string>(order);
		for (var i = 0; i < 200; i++)
			tree.Add(i, $"value_{i}");
		Assert.That(tree.Count, Is.EqualTo(200));
		for (var i = 0; i < 200; i++)
			Assert.That(tree[i], Is.EqualTo($"value_{i}"));
	}

	[Test]
	public void Set_OverwriteExisting() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		tree.Set(1, "ONE", true);
		Assert.That(tree.Count, Is.EqualTo(1));
		Assert.That(tree[1], Is.EqualTo("ONE"));
	}

	[Test]
	public void Set_NoOverwrite_Throws() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		Assert.That(() => tree.Set(1, "ONE", false), Throws.InstanceOf<InvalidOperationException>());
	}

	[Test]
	public void Indexer_Set_NewKey() {
		var tree = new MemoryBTree<int, string>(3);
		tree[1] = "one";
		Assert.That(tree.Count, Is.EqualTo(1));
		Assert.That(tree[1], Is.EqualTo("one"));
	}

	[Test]
	public void Indexer_Set_ExistingKey_Updates() {
		var tree = new MemoryBTree<int, string>(3);
		tree[1] = "one";
		tree[1] = "ONE";
		Assert.That(tree.Count, Is.EqualTo(1));
		Assert.That(tree[1], Is.EqualTo("ONE"));
	}

	[Test]
	public void Indexer_Get_NonExisting_Throws() {
		var tree = new MemoryBTree<int, string>(3);
		Assert.That(() => { var _ = tree[999]; }, Throws.InstanceOf<KeyNotFoundException>());
	}

	[Test]
	public void Remove_ExistingKey_ReturnsTrue() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		tree.Add(2, "two");
		var result = tree.Remove(1);
		Assert.That(result, Is.True);
		Assert.That(tree.Count, Is.EqualTo(1));
		Assert.That(tree.ContainsKey(1), Is.False);
		Assert.That(tree.ContainsKey(2), Is.True);
	}

	[Test]
	public void Remove_NonExistingKey_ReturnsFalse() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		var result = tree.Remove(999);
		Assert.That(result, Is.False);
		Assert.That(tree.Count, Is.EqualTo(1));
	}

	[Test]
	public void Remove_FromEmpty_ReturnsFalse() {
		var tree = new MemoryBTree<int, string>(3);
		Assert.That(tree.Remove(1), Is.False);
	}

	[Test]
	public void Remove_KVP() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		var result = tree.Remove(new KeyValuePair<int, string>(1, "one"));
		Assert.That(result, Is.True);
		Assert.That(tree.Count, Is.EqualTo(0));
	}

	[Test]
	public void Remove_AllItems([Range(3, 15)] int order) {
		var tree = new MemoryBTree<int, string>(order);
		var count = 50;
		for (var i = 0; i < count; i++)
			tree.Add(i, $"value_{i}");
		Assert.That(tree.Count, Is.EqualTo(count));

		for (var i = 0; i < count; i++) {
			var result = tree.Remove(i);
			Assert.That(result, Is.True);
		}
		Assert.That(tree.Count, Is.EqualTo(0));
	}

	[Test]
	public void Remove_ReverseOrder([Range(3, 15)] int order) {
		var tree = new MemoryBTree<int, string>(order);
		var count = 50;
		for (var i = 0; i < count; i++)
			tree.Add(i, $"value_{i}");

		for (var i = count - 1; i >= 0; i--) {
			var result = tree.Remove(i);
			Assert.That(result, Is.True);
		}
		Assert.That(tree.Count, Is.EqualTo(0));
	}

	[Test]
	public void Clear_ResetsCount() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		tree.Add(2, "two");
		tree.Add(3, "three");
		tree.Clear();
		Assert.That(tree.Count, Is.EqualTo(0));
		Assert.That(tree.ContainsKey(1), Is.False);
	}

	[Test]
	public void ContainsKey_ExistingKey_ReturnsTrue() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		Assert.That(tree.ContainsKey(1), Is.True);
	}

	[Test]
	public void ContainsKey_NonExistingKey_ReturnsFalse() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		Assert.That(tree.ContainsKey(999), Is.False);
	}

	[Test]
	public void ContainsKey_EmptyTree_ReturnsFalse() {
		var tree = new MemoryBTree<int, string>(3);
		Assert.That(tree.ContainsKey(1), Is.False);
	}

	[Test]
	public void Contains_MatchingKVP_ReturnsTrue() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		Assert.That(tree.Contains(new KeyValuePair<int, string>(1, "one")), Is.True);
	}

	[Test]
	public void Contains_WrongValue_ReturnsFalse() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		Assert.That(tree.Contains(new KeyValuePair<int, string>(1, "wrong")), Is.False);
	}

	[Test]
	public void Contains_NonExistingKey_ReturnsFalse() {
		var tree = new MemoryBTree<int, string>(3);
		Assert.That(tree.Contains(new KeyValuePair<int, string>(1, "one")), Is.False);
	}

	[Test]
	public void TryGetValue_ExistingKey_ReturnsTrueWithValue() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		var found = tree.TryGetValue(1, out var value);
		Assert.That(found, Is.True);
		Assert.That(value, Is.EqualTo("one"));
	}

	[Test]
	public void TryGetValue_NonExistingKey_ReturnsFalse() {
		var tree = new MemoryBTree<int, string>(3);
		var found = tree.TryGetValue(999, out var value);
		Assert.That(found, Is.False);
		Assert.That(value, Is.Null);
	}

	[Test]
	public void TryGetValue_EmptyTree_ReturnsFalse() {
		var tree = new MemoryBTree<int, string>(3);
		var found = tree.TryGetValue(1, out _);
		Assert.That(found, Is.False);
	}

	[Test]
	public void Keys_ReturnsAllKeys() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(3, "three");
		tree.Add(1, "one");
		tree.Add(2, "two");
		var keys = tree.Keys.OrderBy(k => k).ToArray();
		Assert.That(keys, Is.EqualTo(new[] { 1, 2, 3 }));
	}

	[Test]
	public void Values_ReturnsAllValues() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		tree.Add(2, "two");
		tree.Add(3, "three");
		var values = tree.Values.OrderBy(v => v).ToArray();
		Assert.That(values, Is.EqualTo(new[] { "one", "three", "two" }));
	}

	[Test]
	public void Keys_EmptyTree_ReturnsEmpty() {
		var tree = new MemoryBTree<int, string>(3);
		Assert.That(tree.Keys, Is.Empty);
	}

	[Test]
	public void Values_EmptyTree_ReturnsEmpty() {
		var tree = new MemoryBTree<int, string>(3);
		Assert.That(tree.Values, Is.Empty);
	}

	[Test]
	public void IsReadOnly_ReturnsFalse() {
		var tree = new MemoryBTree<int, string>(3);
		Assert.That(tree.IsReadOnly, Is.False);
	}

	[Test]
	public void CopyTo() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		tree.Add(2, "two");
		var array = new KeyValuePair<int, string>[4];
		tree.CopyTo(array, 1);
		Assert.That(array[0], Is.EqualTo(default(KeyValuePair<int, string>)));
		Assert.That(array[1].Value, Is.Not.Null);
		Assert.That(array[2].Value, Is.Not.Null);
		Assert.That(array[3], Is.EqualTo(default(KeyValuePair<int, string>)));
	}

	[Test]
	public void CopyTo_InsufficientSpace_Throws() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		tree.Add(2, "two");
		var array = new KeyValuePair<int, string>[1];
		Assert.That(() => tree.CopyTo(array, 0), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void InOrderTraversal_ReturnsSorted() {
		var tree = new MemoryBTree<int, string>(3) { TraversalType = TreeTraversalType.InOrder };
		var items = new[] { 5, 3, 8, 1, 4, 7, 9, 2, 6 };
		foreach (var item in items)
			tree.Add(item, $"value_{item}");

		var keys = tree.Select(kvp => kvp.Key).ToArray();
		var sorted = keys.OrderBy(k => k).ToArray();
		Assert.That(keys, Is.EqualTo(sorted));
	}

	[Test]
	public void InOrderTraversal_LargeDataSet([Range(3, 15)] int order) {
		var tree = new MemoryBTree<int, string>(order) { TraversalType = TreeTraversalType.InOrder };
		var rng = new Random(42);
		var addedKeys = new HashSet<int>();
		for (var i = 0; i < 200; i++) {
			var key = rng.Next(0, 10000);
			if (addedKeys.Add(key))
				tree.Add(key, $"value_{key}");
		}

		var keys = tree.Select(kvp => kvp.Key).ToArray();
		var sorted = keys.OrderBy(k => k).ToArray();
		Assert.That(keys, Is.EqualTo(sorted));
	}

	[Test]
	public void PreOrderTraversal_ReturnsAllItems() {
		var tree = new MemoryBTree<int, string>(3) { TraversalType = TreeTraversalType.PreOrder };
		tree.Add(2, "two");
		tree.Add(1, "one");
		tree.Add(3, "three");
		var keys = tree.Select(kvp => kvp.Key).OrderBy(k => k).ToArray();
		Assert.That(keys, Is.EqualTo(new[] { 1, 2, 3 }));
	}

	[Test]
	public void PostOrderTraversal_ReturnsAllItems() {
		var tree = new MemoryBTree<int, string>(3) { TraversalType = TreeTraversalType.PostOrder };
		tree.Add(2, "two");
		tree.Add(1, "one");
		tree.Add(3, "three");
		var keys = tree.Select(kvp => kvp.Key).OrderBy(k => k).ToArray();
		Assert.That(keys, Is.EqualTo(new[] { 1, 2, 3 }));
	}

	[Test]
	public void LevelOrderTraversal_ReturnsAllItems() {
		var tree = new MemoryBTree<int, string>(3) { TraversalType = TreeTraversalType.LevelOrder };
		tree.Add(2, "two");
		tree.Add(1, "one");
		tree.Add(3, "three");
		var keys = tree.Select(kvp => kvp.Key).OrderBy(k => k).ToArray();
		Assert.That(keys, Is.EqualTo(new[] { 1, 2, 3 }));
	}

	[Test]
	public void Enumeration_EmptyTree_YieldsNothing() {
		var tree = new MemoryBTree<int, string>(3);
		var items = tree.ToList();
		Assert.That(items, Is.Empty);
	}

	[Test]
	public void Constructor_OrderLessThan3_Throws() {
		Assert.That(() => new MemoryBTree<int, string>(2), Throws.InstanceOf<ArgumentOutOfRangeException>());
		Assert.That(() => new MemoryBTree<int, string>(1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		Assert.That(() => new MemoryBTree<int, string>(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
		Assert.That(() => new MemoryBTree<int, string>(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void Constructor_Order3_Works() {
		var tree = new MemoryBTree<int, string>(3);
		tree.Add(1, "one");
		tree.Add(2, "two");
		tree.Add(3, "three");
		Assert.That(tree.Count, Is.EqualTo(3));
	}

	[Test]
	public void Count_TracksAddAndRemove() {
		var tree = new MemoryBTree<int, string>(3);
		Assert.That(tree.Count, Is.EqualTo(0));
		tree.Add(1, "one");
		Assert.That(tree.Count, Is.EqualTo(1));
		tree.Add(2, "two");
		Assert.That(tree.Count, Is.EqualTo(2));
		tree.Remove(1);
		Assert.That(tree.Count, Is.EqualTo(1));
		tree.Clear();
		Assert.That(tree.Count, Is.EqualTo(0));
	}

	[Test]
	public void AddRemoveAdd_SameKey([Range(3, 15)] int order) {
		var tree = new MemoryBTree<int, string>(order);
		tree.Add(1, "first");
		Assert.That(tree[1], Is.EqualTo("first"));
		tree.Remove(1);
		Assert.That(tree.ContainsKey(1), Is.False);
		tree.Add(1, "second");
		Assert.That(tree[1], Is.EqualTo("second"));
	}

	[Test]
	public void StringKeys([Range(3, 15)] int order) {
		var tree = new MemoryBTree<string, int>(order);
		tree.Add("banana", 1);
		tree.Add("apple", 2);
		tree.Add("cherry", 3);
		tree.Add("date", 4);
		Assert.That(tree.Count, Is.EqualTo(4));
		Assert.That(tree["apple"], Is.EqualTo(2));
		Assert.That(tree["banana"], Is.EqualTo(1));
		Assert.That(tree["cherry"], Is.EqualTo(3));
		Assert.That(tree["date"], Is.EqualTo(4));
	}

	[Test]
	public void SequentialInsertAndRetrieve([Range(3, 15)] int order) {
		var tree = new MemoryBTree<int, int>(order);
		var count = 100;
		for (var i = 0; i < count; i++)
			tree.Add(i, i * 10);
		Assert.That(tree.Count, Is.EqualTo(count));
		for (var i = 0; i < count; i++)
			Assert.That(tree[i], Is.EqualTo(i * 10));
	}

	[Test]
	public void ReverseInsertAndRetrieve([Range(3, 15)] int order) {
		var tree = new MemoryBTree<int, int>(order);
		var count = 100;
		for (var i = count - 1; i >= 0; i--)
			tree.Add(i, i * 10);
		Assert.That(tree.Count, Is.EqualTo(count));
		for (var i = 0; i < count; i++)
			Assert.That(tree[i], Is.EqualTo(i * 10));
	}

	[Test]
	public void RandomInsertAndRetrieve([Range(3, 15)] int order) {
		var tree = new MemoryBTree<int, int>(order);
		var rng = new Random(12345);
		var expected = new Dictionary<int, int>();
		for (var i = 0; i < 200; i++) {
			var key = rng.Next(0, 10000);
			if (!expected.ContainsKey(key)) {
				expected[key] = key * 10;
				tree.Add(key, key * 10);
			}
		}
		Assert.That(tree.Count, Is.EqualTo(expected.Count));
		foreach (var kvp in expected)
			Assert.That(tree[kvp.Key], Is.EqualTo(kvp.Value));
	}

	[Test]
	public void RandomAddRemoveMix([Range(3, 15)] int order) {
		var tree = new MemoryBTree<int, string>(order);
		var reference = new Dictionary<int, string>();
		var rng = new Random(31337);

		for (var i = 0; i < 100; i++) {
			// Add some items
			var toAdd = rng.Next(1, 10);
			for (var j = 0; j < toAdd; j++) {
				var key = rng.Next(0, 500);
				if (!reference.ContainsKey(key)) {
					var value = $"v{key}";
					reference.Add(key, value);
					tree.Add(key, value);
				}
			}

			// Remove some items
			if (reference.Count > 0) {
				var toRemove = rng.Next(0, Math.Min(5, reference.Count));
				var keysToRemove = reference.Keys.OrderBy(_ => rng.Next()).Take(toRemove).ToArray();
				foreach (var key in keysToRemove) {
					var refResult = reference.Remove(key);
					var treeResult = tree.Remove(key);
					Assert.That(treeResult, Is.EqualTo(refResult));
				}
			}

			Assert.That(tree.Count, Is.EqualTo(reference.Count));
		}

		// Verify all remaining items
		foreach (var kvp in reference)
			Assert.That(tree[kvp.Key], Is.EqualTo(kvp.Value));
	}

	[Test]
	public void Add_ThenContainsKey([Range(3, 15)] int order) {
		var tree = new MemoryBTree<int, int>(order);
		var rng = new Random(31337);
		var keys = Tools.Collection.Generate(() => rng.Next()).Take(100000).Distinct().ToArray();
		for (var i = 0; i < keys.Length; i++) {
			tree.Add(keys[i],i);
			Assert.That(tree.ContainsKey(keys[i]), Is.True, $"Missing key {keys[i]} after inserting (iteration {i})");
			Assert.That(tree[keys[i]], Is.EqualTo(i));
		}
	}

}
