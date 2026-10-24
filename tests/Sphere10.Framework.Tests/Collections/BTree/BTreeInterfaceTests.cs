// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BTreeInterfaceTests {

	[Test]
	public void DictionaryOperationsShareOneContract([Values] BTreeType type, [Values] bool streamMapped) {
		using var stream = new MemoryStream();
		var tree = CreateTree(type, streamMapped, stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<long>.Instance, Comparer<int>.Default);
		using var lifetime = tree as IDisposable;
		var expected = new SortedDictionary<int, long>();
		for (var index = 0; index < 96; index++) {
			var key = index * 37 % 96;
			tree.Add(new KeyValuePair<int, long>(key, key * 10L));
			expected.Add(key, key * 10L);
		}
		Assert.That(tree.Order, Is.EqualTo(4));
		Assert.That(tree.Comparer, Is.SameAs(Comparer<int>.Default));
		Assert.That(tree.TraversalType, Is.EqualTo(TreeTraversalType.InOrder));
		Assert.That(tree.IsReadOnly, Is.False);
		Assert.That(() => tree.Add(17, -1), Throws.ArgumentException);
		tree.Set(17, -17, true);
		expected[17] = -17;
		tree[96] = 960;
		expected[96] = 960;
		Assert.That(tree.Contains(new KeyValuePair<int, long>(17, -17)), Is.True);
		Assert.That(tree.Remove(new KeyValuePair<int, long>(17, 170)), Is.False);
		Assert.That(tree.Remove(new KeyValuePair<int, long>(17, -17)), Is.True);
		expected.Remove(17);
		Assert.That(tree.TryGetValue(17, out _), Is.False);
		Assert.That(tree.Remove(17), Is.False);
		Assert.That(() => { var missing = tree[17]; }, Throws.TypeOf<KeyNotFoundException>());
		Assert.That(tree.Count, Is.EqualTo(expected.Count));
		Assert.That(tree.Keys, Is.EqualTo(expected.Keys));
		Assert.That(tree.Values, Is.EqualTo(expected.Values));
		Assert.That(tree.ToArray(), Is.EqualTo(expected.ToArray()));
		Assert.That(((IEnumerable)tree).Cast<KeyValuePair<int, long>>(), Is.EqualTo(expected));
		var copied = new KeyValuePair<int, long>[expected.Count + 2];
		tree.CopyTo(copied, 1);
		Assert.That(copied.Skip(1).Take(expected.Count), Is.EqualTo(expected));
		Assert.That(copied[0], Is.EqualTo(default(KeyValuePair<int, long>)));
		Assert.That(copied[^1], Is.EqualTo(default(KeyValuePair<int, long>)));
		Assert.That(tree.Validate(out var error), Is.True, error);
		tree.Clear();
		Assert.That(tree.Count, Is.Zero);
		Assert.That(tree, Is.Empty);
		Assert.That(tree.FindRange(0, 100), Is.Empty);
		tree.Set(1, 10, false);
		Assert.That(tree[1], Is.EqualTo(10));
		Assert.That(tree.Validate(out error), Is.True, error);
	}

	[Test]
	public void InclusiveRangeUsesComparerOrderIndependentlyOfTraversal(
		[Values] BTreeType type,
		[Values] bool streamMapped,
		[Values] bool descending,
		[Values] TreeTraversalType traversalType) {
		using var stream = new MemoryStream();
		var comparer = descending ? Comparer<int>.Create((left, right) => right.CompareTo(left)) : Comparer<int>.Default;
		var tree = CreateTree(type, streamMapped, stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<long>.Instance, comparer);
		using var lifetime = tree as IDisposable;
		for (var index = 0; index < 48; index++)
			tree.Add(index * 2, index * 10L);
		tree.TraversalType = traversalType;
		var lower = descending ? 28 : 14;
		var upper = descending ? 14 : 28;
		var expected = Enumerable.Range(7, 8).Select(index => new KeyValuePair<int, long>(index * 2, index * 10L)).OrderBy(entry => entry.Key, comparer).ToArray();
		Assert.That(tree.TraversalType, Is.EqualTo(traversalType));
		Assert.That(tree.FindRange(lower, upper), Is.EqualTo(expected));
		Assert.That(tree.FindRange(14, 14), Is.EqualTo(new[] { new KeyValuePair<int, long>(14, 70) }));
		Assert.That(tree.FindRange(15, 15), Is.Empty);
		Assert.That(tree.FindRange(upper, lower), Is.Empty);
		Assert.That(tree.FindRange(descending ? 27 : 15, descending ? 15 : 27), Is.EqualTo(expected.Skip(1).Take(6)));
		Assert.That(tree.Validate(out var error), Is.True, error);
	}

	[Test]
	public void LookupAcceptsTheTreeInterface([Values] BTreeType type, [Values] bool streamMapped) {
		using var stream = new MemoryStream();
		var serializer = new ConstantSizeCompoundKeySerializer<int, int>(PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance);
		var comparer = new CompoundKeyComparer<int, int>(Comparer<int>.Default, Comparer<int>.Default);
		IBTree<CompoundKey<int, int>, int> tree = CreateTree(type, streamMapped, stream, serializer, PrimitiveSerializer<int>.Instance, comparer);
		using var lifetime = tree as IDisposable;
		tree.Add(CompoundKey.From(7, 0), 70);
		var lookup = new BTreeLookup<int, int>(tree, Comparer<int>.Default);
		lookup.Add(7, 71);
		lookup.Add(7, 72);
		lookup.Add(8, 80);
		Assert.That(lookup.Remove(7, 71), Is.True);
		Assert.That(lookup.GetValues(7), Is.EqualTo(new[] { 70, 72 }));
		Assert.That(tree[CompoundKey.From(7, 1)], Is.EqualTo(72));
		Assert.That(lookup.DistinctKeyCount, Is.EqualTo(2));
		Assert.That(lookup.TotalCount, Is.EqualTo(3));
		Assert.That(lookup.Validate(out var error), Is.True, error);
	}

	[Test]
	public void ThreeComponentKeysSurviveMutationsAndStorageReopen([Values] BTreeType type, [Values] bool streamMapped, [Values] Endianness endianness) {
		using var stream = new MemoryStream();
		var comparer = new CompoundKeyComparer<int, short, byte>(Comparer<int>.Default, Comparer<short>.Default, Comparer<byte>.Default);
		var serializer = new ConstantSizeCompoundKeySerializer<int, short, byte>(PrimitiveSerializer<int>.Instance, PrimitiveSerializer<short>.Instance, PrimitiveSerializer<byte>.Instance);
		var tree = CreateTree(type, streamMapped, stream, serializer, PrimitiveSerializer<long>.Instance, comparer, endianness);
		using var lifetime = tree as IDisposable;
		var expected = new SortedDictionary<CompoundKey<int, short, byte>, long>(comparer);
		for (var index = 0; index < 64; index++) {
			var key = CompoundKey.From(index % 3, (short)(index % 5), (byte)index);
			tree.Add(key, index);
			expected.Add(key, index);
		}
		foreach (var key in expected.Keys.Where(key => key.Key3 % 3 == 0).ToArray()) {
			Assert.That(tree.Remove(key), Is.True);
			expected.Remove(key);
		}
		Assert.That(tree.ToArray(), Is.EqualTo(expected.ToArray()));
		Assert.That(tree.FindRange(expected.Keys.First(), expected.Keys.Last()), Is.EqualTo(expected));
		Assert.That(tree.Validate(out var error), Is.True, error);
		if (!streamMapped)
			return;
		using var snapshot = new MemoryStream(stream.ToArray());
		var reopened = CreateTree(type, true, snapshot, serializer, PrimitiveSerializer<long>.Instance, comparer, endianness);
		using var reopenedLifetime = (IDisposable)reopened;
		Assert.That(reopened.ToArray(), Is.EqualTo(expected.ToArray()));
		Assert.That(reopened.Validate(out error), Is.True, error);
	}

	[Test]
	public void FourComponentKeysSurviveMutationsAndStorageReopen([Values] BTreeType type, [Values] bool streamMapped, [Values] Endianness endianness) {
		using var stream = new MemoryStream();
		var comparer = new CompoundKeyComparer<int, short, byte, long>(Comparer<int>.Default, Comparer<short>.Default, Comparer<byte>.Default, Comparer<long>.Default);
		var serializer = new ConstantSizeCompoundKeySerializer<int, short, byte, long>(PrimitiveSerializer<int>.Instance, PrimitiveSerializer<short>.Instance, PrimitiveSerializer<byte>.Instance, PrimitiveSerializer<long>.Instance);
		var tree = CreateTree(type, streamMapped, stream, serializer, PrimitiveSerializer<long>.Instance, comparer, endianness);
		using var lifetime = tree as IDisposable;
		var expected = new SortedDictionary<CompoundKey<int, short, byte, long>, long>(comparer);
		for (var index = 0; index < 64; index++) {
			var key = CompoundKey.From(index % 3, (short)(index % 5), (byte)(index % 2), index * 100L);
			tree.Add(key, index);
			expected.Add(key, index);
		}
		foreach (var key in expected.Keys.Where(key => key.Key4 % 300 == 0).ToArray()) {
			Assert.That(tree.Remove(key), Is.True);
			expected.Remove(key);
		}
		Assert.That(tree.ToArray(), Is.EqualTo(expected.ToArray()));
		Assert.That(tree.FindRange(expected.Keys.First(), expected.Keys.Last()), Is.EqualTo(expected));
		Assert.That(tree.Validate(out var error), Is.True, error);
		if (!streamMapped)
			return;
		using var snapshot = new MemoryStream(stream.ToArray());
		var reopened = CreateTree(type, true, snapshot, serializer, PrimitiveSerializer<long>.Instance, comparer, endianness);
		using var reopenedLifetime = (IDisposable)reopened;
		Assert.That(reopened.ToArray(), Is.EqualTo(expected.ToArray()));
		Assert.That(reopened.Validate(out error), Is.True, error);
	}

	private static IBTree<K, V> CreateTree<K, V>(BTreeType type, bool streamMapped, Stream stream, IItemSerializer<K> keySerializer, IItemSerializer<V> valueSerializer, IComparer<K> comparer, Endianness endianness = Sphere10FrameworkDefaults.Endianness) {
		if (streamMapped)
			return type == BTreeType.BTree
				? new StreamMappedBTree<K, V>(4, stream, keySerializer, valueSerializer, comparer, endianness)
				: new StreamMappedBTreePlus<K, V>(4, stream, keySerializer, valueSerializer, comparer, endianness);
		return type == BTreeType.BTree ? new InMemoryBTree<K, V>(4, comparer) : new InMemoryBTreePlus<K, V>(4, comparer);
	}
}
