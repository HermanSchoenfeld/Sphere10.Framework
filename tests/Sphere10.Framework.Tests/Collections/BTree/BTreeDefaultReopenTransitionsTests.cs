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
public class BTreeDefaultReopenTransitionsTests {
	[TestCase("BTree", false, Endianness.LittleEndian)]
	[TestCase("BTree", true, Endianness.LittleEndian)]
	[TestCase("BTree", false, Endianness.BigEndian)]
	[TestCase("BTree", true, Endianness.BigEndian)]
	[TestCase("BTreePlus", false, Endianness.LittleEndian)]
	[TestCase("BTreePlus", true, Endianness.LittleEndian)]
	[TestCase("BTreePlus", false, Endianness.BigEndian)]
	[TestCase("BTreePlus", true, Endianness.BigEndian)]
	[TestCase("Lookup", false, Endianness.LittleEndian)]
	[TestCase("Lookup", true, Endianness.LittleEndian)]
	[TestCase("Lookup", false, Endianness.BigEndian)]
	[TestCase("Lookup", true, Endianness.BigEndian)]
	public void ExplicitReopenAcceptsSplitMergedEmptyAndReinsertedRoots(string family, bool useFrameworkDefault, Endianness endianness) {
		var order = useFrameworkDefault ? DefaultOrder(family) : 64;
		var entries = Enumerable.Range(0, checked(2 * order + 17)).Select(index => Entry(family, index)).ToArray();
		using var stream = new MemoryStream();
		using (var created = Create(family, stream, useFrameworkDefault ? null : order, endianness)) {
			Assert.That(OrderOf(created), Is.EqualTo(order));
			// More than two node capacities force splits and an internal root; the odd/even order also exercises insertion within nodes.
			foreach (var entry in entries.Where((_, index) => index % 2 == 1).Concat(entries.Where((_, index) => index % 2 == 0)))
				Add(created, entry);
			AssertEntries(created, entries);
		}

		var survivors = entries.TakeLast(3).ToArray();
		using (var reopened = ReopenWithoutChangingBytes(family, stream, order, endianness)) {
			AssertEntries(reopened, entries);
			// Only three entries remain, forcing merges and collapse back to a leaf root at either tested order.
			foreach (var entry in entries.Take(entries.Length - survivors.Length))
				Assert.That(Remove(reopened, entry), Is.True);
			AssertEntries(reopened, survivors);
		}

		using (var reopened = ReopenWithoutChangingBytes(family, stream, order, endianness)) {
			AssertEntries(reopened, survivors);
			foreach (var entry in survivors)
				Assert.That(Remove(reopened, entry), Is.True);
			AssertEntries(reopened, Array.Empty<KeyValuePair<long, long>>());
		}
		AssertEmptyHeader(family, stream, endianness);

		var reinserted = entries.Take(7).ToArray();
		using (var reopened = ReopenWithoutChangingBytes(family, stream, order, endianness)) {
			AssertEntries(reopened, Array.Empty<KeyValuePair<long, long>>());
			foreach (var entry in reinserted)
				Add(reopened, entry);
			AssertEntries(reopened, reinserted);
		}
		using var finalReopen = ReopenWithoutChangingBytes(family, stream, order, endianness);
		AssertEntries(finalReopen, reinserted);
	}

	private static int DefaultOrder(string family) => family switch {
		"BTree" => Sphere10FrameworkDefaults.StreamMappedBTreeOrder,
		"BTreePlus" => Sphere10FrameworkDefaults.StreamMappedBTreePlusOrder,
		_ => Sphere10FrameworkDefaults.StreamMappedBTreeLookupOrder
	};

	private static int OrderOf(IDisposable instance)
		=> instance is StreamMappedBTreeLookup<long, long> lookup ? lookup.Order : ((BTreeBase<long, long>)instance).Order;

	private static KeyValuePair<long, long> Entry(string family, int index)
		=> new(family == "Lookup" ? index / 3 : index, index);

	private static IDisposable Create(string family, Stream stream, int? order, Endianness endianness) {
		var serializer = PrimitiveSerializer<long>.Instance;
		return (family, order.HasValue) switch {
			("BTree", false) => new StreamMappedBTree<long, long>(stream, serializer, serializer, endianness: endianness),
			("BTreePlus", false) => new StreamMappedBTreePlus<long, long>(stream, serializer, serializer, endianness: endianness),
			("Lookup", false) => new StreamMappedBTreeLookup<long, long>(stream, serializer, serializer, Comparer<long>.Default, endianness),
			("BTree", true) => new StreamMappedBTree<long, long>(order.Value, stream, serializer, serializer, endianness: endianness),
			("BTreePlus", true) => new StreamMappedBTreePlus<long, long>(order.Value, stream, serializer, serializer, endianness: endianness),
			_ => new StreamMappedBTreeLookup<long, long>(order.Value, stream, serializer, serializer, Comparer<long>.Default, endianness)
		};
	}

	private static IDisposable ReopenWithoutChangingBytes(string family, MemoryStream stream, int order, Endianness endianness) {
		var before = stream.ToArray();
		var reopened = Create(family, stream, order, endianness);
		Assert.That(stream.ToArray(), Is.EqualTo(before), "Opening with the original explicit layout must not rewrite stored bytes.");
		return reopened;
	}

	private static void Add(IDisposable instance, KeyValuePair<long, long> entry) {
		if (instance is StreamMappedBTreeLookup<long, long> lookup)
			lookup.Add(entry.Key, entry.Value);
		else
			((BTreeBase<long, long>)instance).Add(entry);
	}

	private static bool Remove(IDisposable instance, KeyValuePair<long, long> entry)
		=> instance is StreamMappedBTreeLookup<long, long> lookup ? lookup.Remove(entry.Key, entry.Value) : ((BTreeBase<long, long>)instance).Remove(entry.Key);

	private static void AssertEntries(IDisposable instance, KeyValuePair<long, long>[] expected) {
		if (instance is StreamMappedBTreeLookup<long, long> lookup) {
			Assert.That(lookup.Validate(out var error), Is.True, error);
			Assert.That(lookup.TotalCount, Is.EqualTo(expected.Length));
			Assert.That(lookup.DistinctKeyCount, Is.EqualTo(expected.Select(entry => entry.Key).Distinct().Count()));
			var actual = lookup.EnumerateGroupings().SelectMany(group => group.Select(value => new KeyValuePair<long, long>(group.Key, value))).ToArray();
			Assert.That(actual, Is.EquivalentTo(expected));
			foreach (var entry in expected)
				Assert.That(lookup.ContainsEntry(entry.Key, entry.Value), Is.True);
		} else {
			var tree = (BTreeBase<long, long>)instance;
			Assert.That(tree.Validate(out var error), Is.True, error);
			Assert.That(tree.Count, Is.EqualTo(expected.Length));
			Assert.That(tree.ToArray(), Is.EquivalentTo(expected));
			foreach (var entry in expected)
				Assert.That(tree[entry.Key], Is.EqualTo(entry.Value));
		}
	}

	private static void AssertEmptyHeader(string family, Stream stream, Endianness endianness) {
		var position = stream.Position;
		try {
			stream.Position = 0;
			var reader = new EndianBinaryReader(EndianBitConverter.For(endianness), stream);
			Assert.That(reader.ReadInt64(), Is.EqualTo(family == "BTree" ? StreamMappedBTree<long, long>.NoNode : StreamMappedBTreePlus<long, long>.NoNode));
			Assert.That(reader.ReadInt64(), Is.Zero, "Deletion to empty must persist zero entries alongside the family's null-root sentinel.");
		} finally {
			stream.Position = position;
		}
	}
}
