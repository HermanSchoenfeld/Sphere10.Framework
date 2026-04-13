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

/// <summary>
/// Abstract test harness for all B-tree family implementations.
/// Subclasses provide a concrete tree via <see cref="CreateInstance{K,V}"/>.
/// </summary>
public abstract class BTreeBaseTests {
	public const int MaxStringValueLength = 100;
	public const int IntegrationTestItemCount = 1000;
	public const int IntegrationTestIterations = 10;
	protected abstract BTreeBase<K, V> CreateInstance<K, V>(int order, IComparer<K> comparer = null);

	[Test]
	public void IntegrationTest([Range(3, 15)] int order) {
		var KeyGens = 0;
		var Tree = CreateInstance<string, string>(order);
		AssertEx.DictionaryIntegrationTest(
			Tree,
			IntegrationTestItemCount,
			(rng) => ($"{Guid.NewGuid().ToStrictAlphaString()}_{KeyGens++}", $"{rng.NextString(0, MaxStringValueLength)}"),
			iterations: IntegrationTestIterations,
			endOfIterTest: () => {
				IntergrationPerIterationTest(Tree);
			}
		);
	}

	protected virtual void IntergrationPerIterationTest<K,V>(BTreeBase<K, V> tree) {
		var Result = tree.Validate(out var Error);
		Assert.That(Result, Is.True, Error);
	}

	[Test]
	public void Add_SingleItem() {
		var Tree = CreateInstance<int, string>(3);
		Tree.Add(1, "one");
		Assert.That(Tree.Count, Is.EqualTo(1));
		Assert.That(Tree[1], Is.EqualTo("one"));
	}

	[Test]
	public void Add_MultipleItems([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(3, "three");
		Tree.Add(1, "one");
		Tree.Add(2, "two");
		Assert.That(Tree.Count, Is.EqualTo(3));
		Assert.That(Tree[1], Is.EqualTo("one"));
		Assert.That(Tree[2], Is.EqualTo("two"));
		Assert.That(Tree[3], Is.EqualTo("three"));
	}

	[Test]
	public void Add_KVP([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(new KeyValuePair<int, string>(1, "one"));
		Assert.That(Tree.Count, Is.EqualTo(1));
		Assert.That(Tree[1], Is.EqualTo("one"));
	}

	[Test]
	public void Add_ManyItems([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		for (var I = 0; I < 200; I++)
			Tree.Add(I, $"value_{I}");
		Assert.That(Tree.Count, Is.EqualTo(200));
		for (var I = 0; I < 200; I++)
			Assert.That(Tree[I], Is.EqualTo($"value_{I}"));
	}

	[Test]
	public void Set_OverwriteExisting([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Tree.Set(1, "ONE", true);
		Assert.That(Tree.Count, Is.EqualTo(1));
		Assert.That(Tree[1], Is.EqualTo("ONE"));
	}

	[Test]
	public void Indexer_Set_NewKey([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree[1] = "one";
		Assert.That(Tree.Count, Is.EqualTo(1));
		Assert.That(Tree[1], Is.EqualTo("one"));
	}

	[Test]
	public void Indexer_Set_ExistingKey_Updates([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree[1] = "one";
		Tree[1] = "ONE";
		Assert.That(Tree.Count, Is.EqualTo(1));
		Assert.That(Tree[1], Is.EqualTo("ONE"));
	}

	[Test]
	public void Indexer_Get_NonExisting_Throws([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Assert.That(() => { var _ = Tree[999]; }, Throws.InstanceOf<KeyNotFoundException>());
	}

	[Test]
	public void Remove_ExistingKey_ReturnsTrue([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Tree.Add(2, "two");
		var Result = Tree.Remove(1);
		Assert.That(Result, Is.True);
		Assert.That(Tree.Count, Is.EqualTo(1));
		Assert.That(Tree.ContainsKey(1), Is.False);
		Assert.That(Tree.ContainsKey(2), Is.True);
	}

	[Test]
	public void Remove_NonExistingKey_ReturnsFalse([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		var Result = Tree.Remove(999);
		Assert.That(Result, Is.False);
		Assert.That(Tree.Count, Is.EqualTo(1));
	}

	[Test]
	public void Remove_FromEmpty_ReturnsFalse([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Assert.That(Tree.Remove(1), Is.False);
	}

	[Test]
	public void Remove_KVP([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		var Result = Tree.Remove(new KeyValuePair<int, string>(1, "one"));
		Assert.That(Result, Is.True);
		Assert.That(Tree.Count, Is.EqualTo(0));
	}

	[Test]
	public void Remove_AllItems([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		var Count = 50;
		for (var I = 0; I < Count; I++)
			Tree.Add(I, $"value_{I}");
		Assert.That(Tree.Count, Is.EqualTo(Count));

		for (var I = 0; I < Count; I++) {
			var Result = Tree.Remove(I);
			Assert.That(Result, Is.True);
		}
		Assert.That(Tree.Count, Is.EqualTo(0));
	}

	[Test]
	public void Remove_ReverseOrder([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		var Count = 50;
		for (var I = 0; I < Count; I++)
			Tree.Add(I, $"value_{I}");

		for (var I = Count - 1; I >= 0; I--) {
			var Result = Tree.Remove(I);
			Assert.That(Result, Is.True);
		}
		Assert.That(Tree.Count, Is.EqualTo(0));
	}

	[Test]
	public void Clear_ResetsCount([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Tree.Add(2, "two");
		Tree.Add(3, "three");
		Tree.Clear();
		Assert.That(Tree.Count, Is.EqualTo(0));
		Assert.That(Tree.ContainsKey(1), Is.False);
	}

	[Test]
	public void ContainsKey_ExistingKey_ReturnsTrue([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Assert.That(Tree.ContainsKey(1), Is.True);
	}

	[Test]
	public void ContainsKey_NonExistingKey_ReturnsFalse([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Assert.That(Tree.ContainsKey(999), Is.False);
	}

	[Test]
	public void ContainsKey_EmptyTree_ReturnsFalse([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Assert.That(Tree.ContainsKey(1), Is.False);
	}

	[Test]
	public void Contains_MatchingKVP_ReturnsTrue([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Assert.That(Tree.Contains(new KeyValuePair<int, string>(1, "one")), Is.True);
	}

	[Test]
	public void Contains_WrongValue_ReturnsFalse([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Assert.That(Tree.Contains(new KeyValuePair<int, string>(1, "wrong")), Is.False);
	}

	[Test]
	public void Contains_NonExistingKey_ReturnsFalse([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Assert.That(Tree.Contains(new KeyValuePair<int, string>(1, "one")), Is.False);
	}

	[Test]
	public void TryGetValue_ExistingKey_ReturnsTrueWithValue([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		var Found = Tree.TryGetValue(1, out var Value);
		Assert.That(Found, Is.True);
		Assert.That(Value, Is.EqualTo("one"));
	}

	[Test]
	public void TryGetValue_NonExistingKey_ReturnsFalse([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		var Found = Tree.TryGetValue(999, out var Value);
		Assert.That(Found, Is.False);
		Assert.That(Value, Is.Null);
	}

	[Test]
	public void TryGetValue_EmptyTree_ReturnsFalse([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		var Found = Tree.TryGetValue(1, out _);
		Assert.That(Found, Is.False);
	}

	[Test]
	public void Keys_ReturnsAllKeys([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(3, "three");
		Tree.Add(1, "one");
		Tree.Add(2, "two");
		var Keys = Tree.Keys.OrderBy(k => k).ToArray();
		Assert.That(Keys, Is.EqualTo(new[] { 1, 2, 3 }));
	}

	[Test]
	public void Values_ReturnsAllValues([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Tree.Add(2, "two");
		Tree.Add(3, "three");
		var Vals = Tree.Values.OrderBy(v => v).ToArray();
		Assert.That(Vals, Is.EqualTo(new[] { "one", "three", "two" }));
	}

	[Test]
	public void Keys_EmptyTree_ReturnsEmpty([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Assert.That(Tree.Keys, Is.Empty);
	}

	[Test]
	public void Values_EmptyTree_ReturnsEmpty([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Assert.That(Tree.Values, Is.Empty);
	}

	[Test]
	public void IsReadOnly_ReturnsFalse([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Assert.That(Tree.IsReadOnly, Is.False);
	}

	[Test]
	public void CopyTo() {
		var Tree = CreateInstance<int, string>(3);
		Tree.Add(1, "one");
		Tree.Add(2, "two");
		var Array = new KeyValuePair<int, string>[4];
		Tree.CopyTo(Array, 1);
		Assert.That(Array[0], Is.EqualTo(default(KeyValuePair<int, string>)));
		Assert.That(Array[1].Value, Is.Not.Null);
		Assert.That(Array[2].Value, Is.Not.Null);
		Assert.That(Array[3], Is.EqualTo(default(KeyValuePair<int, string>)));
	}

	[Test]
	public void CopyTo_InsufficientSpace_Throws([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Tree.Add(2, "two");
		var Array = new KeyValuePair<int, string>[1];
		Assert.That(() => Tree.CopyTo(Array, 0), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void InOrderTraversal_ReturnsSorted([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.TraversalType = TreeTraversalType.InOrder;
		var Items = new[] { 5, 3, 8, 1, 4, 7, 9, 2, 6 };
		foreach (var Item in Items)
			Tree.Add(Item, $"value_{Item}");

		var Keys = Tree.Select(kvp => kvp.Key).ToArray();
		var Sorted = Keys.OrderBy(k => k).ToArray();
		Assert.That(Keys, Is.EqualTo(Sorted));
	}

	[Test]
	public void InOrderTraversal_LargeDataSet([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.TraversalType = TreeTraversalType.InOrder;
		var Rng = new Random(42);
		var AddedKeys = new HashSet<int>();
		for (var I = 0; I < 200; I++) {
			var Key = Rng.Next(0, 10000);
			if (AddedKeys.Add(Key))
				Tree.Add(Key, $"value_{Key}");
		}

		var Keys = Tree.Select(kvp => kvp.Key).ToArray();
		var Sorted = Keys.OrderBy(k => k).ToArray();
		Assert.That(Keys, Is.EqualTo(Sorted));
	}

	[Test]
	public void PreOrderTraversal_ReturnsAllItems([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.TraversalType = TreeTraversalType.PreOrder;
		for (var I = 0; I < 50; I++)
			Tree.Add(I, $"value_{I}");

		var Keys = Tree.Select(kvp => kvp.Key).OrderBy(k => k).ToArray();
		Assert.That(Keys, Is.EqualTo(Enumerable.Range(0, 50).ToArray()));
	}

	[Test]
	public void PostOrderTraversal_ReturnsAllItems([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.TraversalType = TreeTraversalType.PostOrder;
		for (var I = 0; I < 50; I++)
			Tree.Add(I, $"value_{I}");

		var Keys = Tree.Select(kvp => kvp.Key).OrderBy(k => k).ToArray();
		Assert.That(Keys, Is.EqualTo(Enumerable.Range(0, 50).ToArray()));
	}

	[Test]
	public void LevelOrderTraversal_ReturnsAllItems([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.TraversalType = TreeTraversalType.LevelOrder;
		for (var I = 0; I < 50; I++)
			Tree.Add(I, $"value_{I}");

		var Keys = Tree.Select(kvp => kvp.Key).OrderBy(k => k).ToArray();
		Assert.That(Keys, Is.EqualTo(Enumerable.Range(0, 50).ToArray()));
	}

	[Test]
	public void Enumeration_EmptyTree_YieldsNothing([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		var Items = Tree.ToList();
		Assert.That(Items, Is.Empty);
	}

	[Test]
	public void Constructor_OrderLessThan3_Throws([Range(-1, 2)] int order) {
		Assert.That(() => CreateInstance<int, string>(order), Throws.InstanceOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void Constructor_Order3_Works([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Tree.Add(2, "two");
		Tree.Add(3, "three");
		Assert.That(Tree.Count, Is.EqualTo(3));
	}

	[Test]
	public void Count_TracksAddAndRemove([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Assert.That(Tree.Count, Is.EqualTo(0));
		Tree.Add(1, "one");
		Assert.That(Tree.Count, Is.EqualTo(1));
		Tree.Add(2, "two");
		Assert.That(Tree.Count, Is.EqualTo(2));
		Tree.Remove(1);
		Assert.That(Tree.Count, Is.EqualTo(1));
		Tree.Clear();
		Assert.That(Tree.Count, Is.EqualTo(0));
	}

	[Test]
	public void AddRemoveAdd_SameKey([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "first");
		Assert.That(Tree[1], Is.EqualTo("first"));
		Tree.Remove(1);
		Assert.That(Tree.ContainsKey(1), Is.False);
		Tree.Add(1, "second");
		Assert.That(Tree[1], Is.EqualTo("second"));
	}

	[Test]
	public void StringKeys([Range(3, 15)] int order) {
		var Tree = CreateInstance<string, int>(order);
		Tree.Add("banana", 1);
		Tree.Add("apple", 2);
		Tree.Add("cherry", 3);
		Tree.Add("date", 4);
		Assert.That(Tree.Count, Is.EqualTo(4));
		Assert.That(Tree["apple"], Is.EqualTo(2));
		Assert.That(Tree["banana"], Is.EqualTo(1));
		Assert.That(Tree["cherry"], Is.EqualTo(3));
		Assert.That(Tree["date"], Is.EqualTo(4));
	}

	[Test]
	public void SequentialInsertAndRetrieve([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, int>(order);
		var Count = 500;
		for (var I = 0; I < Count; I++)
			Tree.Add(I, I * 10);
		Assert.That(Tree.Count, Is.EqualTo(Count));
		for (var I = 0; I < Count; I++)
			Assert.That(Tree[I], Is.EqualTo(I * 10));
		Assert.That(Tree.Validate(out var Error), Is.True, Error);
	}

	[Test]
	public void ReverseInsertAndRetrieve([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, int>(order);
		var Count = 100;
		for (var I = Count - 1; I >= 0; I--)
			Tree.Add(I, I * 10);
		Assert.That(Tree.Count, Is.EqualTo(Count));
		for (var I = 0; I < Count; I++)
			Assert.That(Tree[I], Is.EqualTo(I * 10));
	}

	[Test]
	public void RandomInsertAndRetrieve([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, int>(order);
		var Rng = new Random(12345);
		var Expected = new Dictionary<int, int>();
		for (var I = 0; I < 1000; I++) {
			var Key = Rng.Next(0, 100000);
			if (!Expected.ContainsKey(Key)) {
				Expected[Key] = Key * 10;
				Tree.Add(Key, Key * 10);
			}
		}

		Assert.That(Tree.Count, Is.EqualTo(Expected.Count));
		foreach (var Kv in Expected)
			Assert.That(Tree[Kv.Key], Is.EqualTo(Kv.Value));
		Assert.That(Tree.Validate(out var Error), Is.True, Error);
	}

	[Test]
	public void RandomAddRemoveMix([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		var Reference = new Dictionary<int, string>();
		var Rng = new Random(31337);

		for (var I = 0; I < 200; I++) {
			var ToAdd = Rng.Next(1, 12);
			for (var J = 0; J < ToAdd; J++) {
				var Key = Rng.Next(0, 1000);
				if (!Reference.ContainsKey(Key)) {
					var Value = $"v{Key}";
					Reference.Add(Key, Value);
					Tree.Add(Key, Value);
				}
			}

			if (Reference.Count > 0) {
				var ToRemove = Rng.Next(0, Math.Min(8, Reference.Count));
				var KeysToRemove = Reference.Keys.OrderBy(_ => Rng.Next()).Take(ToRemove).ToArray();
				foreach (var Key in KeysToRemove) {
					var RefResult = Reference.Remove(Key);
					var TreeResult = Tree.Remove(Key);
					Assert.That(TreeResult, Is.EqualTo(RefResult));
				}
			}

			Assert.That(Tree.Count, Is.EqualTo(Reference.Count));
			Assert.That(Tree.Validate(out var Error), Is.True, Error);
		}

		foreach (var Kv in Reference)
			Assert.That(Tree[Kv.Key], Is.EqualTo(Kv.Value));
	}

	[Test]
	public void Add_ThenContainsKey([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, int>(order);
		var Rng = new Random(31337);
		var Keys = Tools.Collection.Generate(() => Rng.Next()).Take(IntegrationTestItemCount).Distinct().ToArray();
		for (var I = 0; I < Keys.Length; I++) {
			Tree.Add(Keys[I], I);
			Assert.That(Tree.ContainsKey(Keys[I]), Is.True, $"Missing key {Keys[I]} after inserting (iteration {I})");
			Assert.That(Tree[Keys[I]], Is.EqualTo(I));
		}

		Assert.That(Tree.Validate(out var Error), Is.True, Error);
	}

	
	[Test]
	public void Add_DuplicateKey_Throws([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Assert.That(() => Tree.Add(1, "duplicate"), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void Set_NoOverwrite_Throws([Range(3, 15)] int order) {
		var Tree = CreateInstance<int, string>(order);
		Tree.Add(1, "one");
		Assert.That(() => Tree.Set(1, "ONE", false), Throws.InstanceOf<ArgumentException>());
	}
}
