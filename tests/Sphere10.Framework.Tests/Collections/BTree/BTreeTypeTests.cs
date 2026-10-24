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
public class BTreeTypeTests {
	[Test]
	public void LookupDeletionCompactsGroupsAndSurvivesReopen([Values] BTreeType type, [Values] bool streamMapped) {
		using var stream = new MemoryStream();
		IExtendedBTreeLookup<int, int> lookup = streamMapped
			? new StreamMappedBTreeLookup<int, int>(new BTreeSpec(type, 4), stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default)
			: new InMemoryBTreeLookup<int, int>(new BTreeSpec(type, 4), Comparer<int>.Default);
		using var lifetime = lookup as IDisposable;
		var expected = Enumerable.Range(0, 96).Select(value => new KeyValuePair<int, int>(value % 11, value)).ToList();
		foreach (var entry in expected)
			lookup.Add(entry.Key, entry.Value);
		foreach (var entry in expected.Where(entry => entry.Value % 3 == 0).ToArray()) {
			Assert.That(lookup.Remove(entry.Key, entry.Value), Is.True);
			expected.Remove(entry);
			Assert.That(lookup.Validate(out var error), Is.True, error);
		}
		lookup.RemoveAll(4);
		expected.RemoveAll(entry => entry.Key == 4);
		Assert.That(lookup.TotalCount, Is.EqualTo(expected.Count));
		Assert.That(lookup.DistinctKeyCount, Is.EqualTo(expected.Select(entry => entry.Key).Distinct().Count()));
		for (var key = 0; key < 11; key++)
			Assert.That(lookup.GetValues(key), Is.EqualTo(expected.Where(entry => entry.Key == key).Select(entry => entry.Value)));
		if (!streamMapped)
			return;
		using var snapshot = new MemoryStream(stream.ToArray());
		using var reopened = new StreamMappedBTreeLookup<int, int>(new BTreeSpec(type, 4), snapshot, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default);
		Assert.That(reopened.Validate(out var reopenedError), Is.True, reopenedError);
		Assert.That(reopened.TotalCount, Is.EqualTo(expected.Count));
		for (var key = 0; key < 11; key++)
			Assert.That(reopened.GetValues(key), Is.EqualTo(expected.Where(entry => entry.Key == key).Select(entry => entry.Value)));
		reopened.Clear();
		reopened.Add(7, 70);
		Assert.That(reopened.GetValues(7), Is.EqualTo(new[] { 70 }));
	}

	[Test]
	public void ExplicitLookupLeavesReservedHeaderBytesUnused([Values] BTreeType type) {
		using var stream = new MemoryStream();
		using (var lookup = new StreamMappedBTreeLookup<int, int>(new BTreeSpec(type, 4), stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default)) {
			for (var value = 0; value < 32; value++)
				lookup.Add(value % 3, value);
		}
		Assert.That(stream.ToArray().Skip(20).Take(12), Is.All.Zero);
	}

	[Test]
	public void ReservedTreeHeaderBytesAreIgnoredAndPreservedOnReopen([Values] BTreeType type) {
		using var stream = new MemoryStream();
		using (var lookup = new StreamMappedBTreeLookup<int, int>(new BTreeSpec(type, 4), stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default)) {
			for (var value = 0; value < 32; value++)
				lookup.Add(value % 3, value);
		}
		stream.Position = 20;
		stream.Write(new byte[] { (byte)'S', (byte)'1', (byte)'0', (byte)'T', 255, 255, 17, 34, 51, 68, 85, 102 });
		var original = stream.ToArray();
		using (var reopened = new StreamMappedBTreeLookup<int, int>(new BTreeSpec(type, 4), stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default)) {
			Assert.That(reopened.Spec.Type, Is.EqualTo(type));
			Assert.That(reopened.Validate(out var error), Is.True, error);
			for (var key = 0; key < 3; key++)
				Assert.That(reopened.GetValues(key), Is.EqualTo(Enumerable.Range(0, 32).Where(value => value % 3 == key)));
			Assert.That(stream.ToArray(), Is.EqualTo(original), "Opening and reading must not rewrite stored bytes.");
		}
		Assert.That(stream.ToArray().Skip(20).Take(12), Is.EqualTo(original.Skip(20).Take(12)), "Disposal must preserve reserved header data.");
	}

	[Test]
	public void ExplicitLookupReopensUnmarkedIndependentlyCreatedTree([Values] BTreeType type) {
		using var stream = new MemoryStream();
		var keys = new ConstantSizeCompoundKeySerializer<int, int>(PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance);
		var comparer = new CompoundKeyComparer<int, int>(Comparer<int>.Default, Comparer<int>.Default);
		BTreeBase<CompoundKey<int, int>, int> tree = type == BTreeType.BTree
			? new StreamMappedBTree<CompoundKey<int, int>, int>(4, stream, keys, PrimitiveSerializer<int>.Instance, comparer)
			: new StreamMappedBTreePlus<CompoundKey<int, int>, int>(4, stream, keys, PrimitiveSerializer<int>.Instance, comparer);
		using ((IDisposable)tree) {
			var lookup = new BTreeLookup<int, int>(tree, Comparer<int>.Default);
			lookup.Add(7, 70);
			lookup.Add(7, 71);
			lookup.Add(8, 80);
		}
		var original = stream.ToArray();
		Assert.That(original.Skip(20).Take(12), Is.All.Zero);
		using (var reopened = new StreamMappedBTreeLookup<int, int>(new BTreeSpec(type, 4), stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default)) {
			Assert.That(reopened.Validate(out var error), Is.True, error);
			Assert.That(reopened.TotalCount, Is.EqualTo(3));
			Assert.That(reopened.DistinctKeyCount, Is.EqualTo(2));
			Assert.That(reopened.GetValues(7), Is.EqualTo(new[] { 70, 71 }));
			Assert.That(reopened.GetValues(8), Is.EqualTo(new[] { 80 }));
			Assert.That(stream.ToArray(), Is.EqualTo(original), "Opening an existing tree must not add type metadata.");
		}
		Assert.That(stream.ToArray().Skip(20).Take(12), Is.EqualTo(original.Skip(20).Take(12)));
	}

	[TestCase(-1)]
	[TestCase(2)]
	public void InvalidTypeCannotCreateLookupStorage(int type) {
		using var stream = new MemoryStream();
		Assert.That(() => {
			using var refused = new StreamMappedBTreeLookup<int, int>(new BTreeSpec((BTreeType)type, 4), stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default);
		}, Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("type"));
		Assert.That(stream.Length, Is.Zero);
	}

	[Test]
	public void InvalidOrderCannotCreateLookupStorage([Values(0, 2)] int order, [Values] bool streamMapped) {
		using var stream = new MemoryStream();
		Assert.That(() => {
			IExtendedBTreeLookup<int, int> lookup = streamMapped
				? new StreamMappedBTreeLookup<int, int>(new BTreeSpec(BTreeType.BTree, order), stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default)
				: new InMemoryBTreeLookup<int, int>(new BTreeSpec(BTreeType.BTree, order), Comparer<int>.Default);
			using var lifetime = lookup as IDisposable;
		}, Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("order"));
		Assert.That(stream.Length, Is.Zero);
	}

	[Test]
	public void OrderOnlyLookupConstructorsSelectConcreteHistoricalPlusType([Values] bool streamMapped) {
		using var stream = new MemoryStream();
		if (streamMapped) {
			using var lookup = new StreamMappedBTreeLookup<int, int>(7, stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default);
			Assert.That(lookup.Spec, Is.EqualTo(new BTreeSpec(BTreeType.BTreePlus, 7)));
		} else {
			var lookup = new InMemoryBTreeLookup<int, int>(7, Comparer<int>.Default);
			Assert.That(lookup.Spec, Is.EqualTo(new BTreeSpec(BTreeType.BTreePlus, 7)));
		}
	}

	[Test]
	public void HistoricalLookupRetainsOriginalBytesAndImplementation() {
		using var stream = new MemoryStream();
		using (var lookup = new StreamMappedBTreeLookup<int, int>(4, stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default))
			lookup.Add(1, 2);
		var original = stream.ToArray();
		Assert.That(original.Skip(20).Take(12), Is.All.Zero);
		using (var reopened = new StreamMappedBTreeLookup<int, int>(new BTreeSpec(BTreeType.BTreePlus, 4), stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default))
			Assert.That(reopened.GetValues(1), Is.EqualTo(new[] { 2 }));
		Assert.That(stream.ToArray(), Is.EqualTo(original));
	}

	[Test]
	public void BTreeSearchDoesNotDeserializeValuesForComparisonProbes([Values] bool inherited) {
		using var stream = new MemoryStream();
		var values = new CountingValueSerializer();
		using var tree = inherited ? new InheritedTree(stream, values) : new StreamMappedBTree<int, int>(32, stream, PrimitiveSerializer<int>.Instance, values);
		for (var key = 0; key < 256; key++)
			tree.Add(key * 2, key);
		values.Reads = 0;
		Assert.That(tree.TryGetValue(257, out _), Is.False);
		Assert.That(values.Reads, Is.Zero, "A missing-key binary search only needs keys.");
		Assert.That(tree.TryGetValue(258, out var value), Is.True);
		Assert.That(value, Is.EqualTo(129));
		Assert.That(values.Reads, Is.EqualTo(1), "Only the matching entry needs its value deserialized.");
	}

	[Test]
	public void DerivedBTreeEntryReadingControlsReturnedValuesWithoutComparisonReads() {
		using var stream = new MemoryStream();
		using var tree = new DerivedEntryTree(stream);
		tree.Add(1, 2);
		Assert.That(tree.TryGetValue(1, out var value), Is.True);
		Assert.That(value, Is.EqualTo(1002));
		Assert.That(tree.EntryReads, Is.EqualTo(1));
	}

	[Test]
	public void DerivedBTreeComparisonHookControlsProjectedKeys() {
		using var stream = new MemoryStream();
		using var tree = new ProjectedKeyTree(stream);
		tree.Add(1, 2);
		Assert.That(tree.TryGetValue(1001, out var value), Is.True);
		Assert.That(value, Is.EqualTo(2));
		Assert.That(tree.ComparisonReads, Is.GreaterThan(0));
		Assert.That(tree.TryGetValue(1, out _), Is.False);
	}

	private sealed class CountingValueSerializer : ItemSerializerDecorator<int> {
		public CountingValueSerializer() : base(PrimitiveSerializer<int>.Instance) {
		}

		public int Reads { get; set; }

		public override int Deserialize(EndianBinaryReader reader, SerializationContext context) {
			Reads++;
			return base.Deserialize(reader, context);
		}
	}

	private sealed class DerivedEntryTree : StreamMappedBTree<int, int> {
		public DerivedEntryTree(Stream stream) : base(4, stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance) {
		}

		public int EntryReads { get; private set; }

		protected override KeyValuePair<int, int> GetKey(long node, int index) {
			EntryReads++;
			var entry = base.GetKey(node, index);
			return new KeyValuePair<int, int>(entry.Key, entry.Value + 1000);
		}
	}

	private sealed class InheritedTree : StreamMappedBTree<int, int> {
		public InheritedTree(Stream stream, IItemSerializer<int> valueSerializer)
			: base(32, stream, PrimitiveSerializer<int>.Instance, valueSerializer) {
		}
	}

	private sealed class ProjectedKeyTree : StreamMappedBTree<int, int> {
		public ProjectedKeyTree(Stream stream)
			: base(4, stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance) {
		}

		public int ComparisonReads { get; private set; }

		protected override int GetKeyValue(long node, int index) {
			ComparisonReads++;
			return base.GetKeyValue(node, index) + 1000;
		}
	}
}
