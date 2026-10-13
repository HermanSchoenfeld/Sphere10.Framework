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
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BTreeDefaultOrderTests {
	[Test]
	public void ManagedDefaults() {
		var tree = new InMemoryBTree<long, long>();
		var plus = new InMemoryBTreePlus<long, long>();
		var lookup = new InMemoryBTreeLookup<long, long>(Comparer<long>.Default);
		Assert.That(tree.Order, Is.EqualTo(Sphere10FrameworkDefaults.BTreeOrder));
		Assert.That(plus.Order, Is.EqualTo(Sphere10FrameworkDefaults.BTreePlusOrder));
		Assert.That(lookup.Order, Is.EqualTo(Sphere10FrameworkDefaults.BTreeLookupOrder));
		tree.Add(7, 11);
		plus.Add(7, 11);
		lookup.Add(7, 11);
		lookup.Add(7, 13);
		Assert.That(tree[7], Is.EqualTo(11));
		Assert.That(plus[7], Is.EqualTo(11));
		Assert.That(lookup.GetValues(7), Is.EqualTo(new long[] { 11, 13 }));
		Assert.That(lookup.Validate(out var error), Is.True, error);
	}

	[TestCase("BTree", false)]
	[TestCase("BTreePlus", false)]
	[TestCase("Lookup", false)]
	[TestCase("BTree", true)]
	[TestCase("BTreePlus", true)]
	[TestCase("Lookup", true)]
	public void StreamDefaultsAndFactories(string family, bool factory) {
		using var stream = new MemoryStream();
		using var disposable = CreateDefault(family, stream, factory);
		var order = family switch {
			"BTree" => ((StreamMappedBTree<long, long>)disposable).Order,
			"BTreePlus" => ((StreamMappedBTreePlus<long, long>)disposable).Order,
			_ => ((StreamMappedBTreeLookup<long, long>)disposable).Order
		};
		var expected = family switch {
			"BTree" => Sphere10FrameworkDefaults.StreamMappedBTreeOrder,
			"BTreePlus" => Sphere10FrameworkDefaults.StreamMappedBTreePlusOrder,
			_ => Sphere10FrameworkDefaults.StreamMappedBTreeLookupOrder
		};
		Assert.That(order, Is.EqualTo(expected));
		AddAndAssert(family, disposable);
	}

	[TestCase("BTree", false)]
	[TestCase("BTreePlus", false)]
	[TestCase("Lookup", false)]
	[TestCase("BTree", true)]
	[TestCase("BTreePlus", true)]
	[TestCase("Lookup", true)]
	public void DefaultStreamRefusesExistingBytesWithoutMutation(string family, bool factory) {
		using var stream = new MemoryStream();
		using (var original = CreateExplicit(family, stream, 64))
			AddAndAssert(family, original);
		var before = stream.ToArray();
		stream.Position = 5;
		Assert.That(() => CreateDefault(family, stream, factory), Throws.InstanceOf<ArgumentException>());
		Assert.That(stream.Position, Is.EqualTo(5));
		Assert.That(stream.ToArray(), Is.EqualTo(before));
		using var reopened = CreateExplicit(family, stream, 64);
		AssertExisting(family, reopened);
	}

	[TestCase("BTree", Endianness.LittleEndian)]
	[TestCase("BTreePlus", Endianness.LittleEndian)]
	[TestCase("Lookup", Endianness.LittleEndian)]
	[TestCase("BTree", Endianness.BigEndian)]
	[TestCase("BTreePlus", Endianness.BigEndian)]
	[TestCase("Lookup", Endianness.BigEndian)]
	public void LegacyExplicitOrderStillReopens(string family, Endianness endianness) {
		using var stream = new MemoryStream();
		using (var original = CreateExplicit(family, stream, 64, endianness))
			AddAndAssert(family, original);
		var before = stream.ToArray();
		using var reopened = CreateExplicit(family, stream, 64, endianness);
		AssertExisting(family, reopened);
		Assert.That(stream.ToArray(), Is.EqualTo(before));
	}

	[TestCase("BTree")]
	[TestCase("BTreePlus")]
	[TestCase("Lookup")]
	public void SplitMergeAndEmptyLegacyReopen(string family) {
		using var stream = new MemoryStream();
		using (var created = CreateExplicit(family, stream, 64)) {
			for (var key = 0L; key < 300; key++) {
				if (family == "Lookup")
					((StreamMappedBTreeLookup<long, long>)created).Add(key, key * 17);
				else
					((BTreeBase<long, long>)created).Add(key, key * 17);
			}
		}
		using (var reopened = CreateExplicit(family, stream, 64)) {
			for (var key = 0L; key < 298; key++)
				Assert.That(family == "Lookup"
					? ((StreamMappedBTreeLookup<long, long>)reopened).Remove(key, key * 17)
					: ((BTreeBase<long, long>)reopened).Remove(key), Is.True);
		}
		using (var merged = CreateExplicit(family, stream, 64)) {
			for (var key = 298L; key < 300; key++)
				Assert.That(family == "Lookup"
					? ((StreamMappedBTreeLookup<long, long>)merged).Remove(key, key * 17)
					: ((BTreeBase<long, long>)merged).Remove(key), Is.True);
		}
		using var empty = CreateExplicit(family, stream, 64);
		if (family == "Lookup") {
			var lookup = (StreamMappedBTreeLookup<long, long>)empty;
			Assert.That(lookup.TotalCount, Is.Zero);
			Assert.That(lookup.Validate(out var error), Is.True, error);
		} else {
			var tree = (BTreeBase<long, long>)empty;
			Assert.That(tree.Count, Is.Zero);
			Assert.That(tree.Validate(out var error), Is.True, error);
		}
	}

	[TestCase("BTree")]
	[TestCase("BTreePlus")]
	[TestCase("Lookup")]
	public void IncorrectOrderOrRemainderRefusedWithoutMutation(string family) {
		using var stream = new MemoryStream();
		using (var original = CreateExplicit(family, stream, 64))
			AddAndAssert(family, original);
		AssertRefused(stream, () => CreateExplicit(family, stream, 2), typeof(ArgumentOutOfRangeException));
		AssertRefused(stream, () => CreateExplicit(family, stream, 128), typeof(InvalidDataFormatException));
		stream.SetLength(stream.Length + 1);
		AssertRefused(stream, () => CreateExplicit(family, stream, 64), typeof(InvalidDataFormatException));
	}

	[TestCase("BTree", "short")]
	[TestCase("BTreePlus", "short")]
	[TestCase("Lookup", "short")]
	[TestCase("BTree", "count")]
	[TestCase("BTreePlus", "count")]
	[TestCase("Lookup", "count")]
	[TestCase("BTree", "root")]
	[TestCase("BTreePlus", "root")]
	[TestCase("Lookup", "root")]
	[TestCase("BTree", "free")]
	[TestCase("BTreePlus", "free")]
	[TestCase("Lookup", "free")]
	[TestCase("BTree", "rootRecord")]
	[TestCase("BTreePlus", "rootRecord")]
	[TestCase("Lookup", "rootRecord")]
	public void InvalidHeadersRefusedWithoutMutation(string family, string corruption) {
		using var stream = new MemoryStream();
		using (var original = CreateExplicit(family, stream, 64))
			AddAndAssert(family, original);
		switch (corruption) {
			case "short": stream.SetLength(20); break;
			case "rootRecord": stream.Position = 32; stream.WriteByte(2); break;
			default:
				stream.Position = corruption == "count" ? 8 : corruption == "free" ? 16 : 0;
				using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)) {
					if (corruption == "free")
						writer.Write(-1);
					else
						writer.Write(long.MaxValue);
				}
				break;
		}
		AssertRefused(stream, () => CreateExplicit(family, stream, 64), typeof(InvalidDataFormatException));
	}

	private static IDisposable CreateDefault(string family, Stream stream, bool factory) {
		var serializer = PrimitiveSerializer<long>.Instance;
		return (family, factory) switch {
			("BTree", false) => new StreamMappedBTree<long, long>(stream, serializer, serializer),
			("BTreePlus", false) => new StreamMappedBTreePlus<long, long>(stream, serializer, serializer),
			("Lookup", false) => new StreamMappedBTreeLookup<long, long>(stream, serializer, serializer, Comparer<long>.Default),
			("BTree", true) => StreamMappedFactory.CreateBTree(stream, serializer, serializer),
			("BTreePlus", true) => StreamMappedFactory.CreateBTreePlus(stream, serializer, serializer),
			_ => StreamMappedFactory.CreateBTreeLookup(stream, serializer, serializer, Comparer<long>.Default)
		};
	}

	private static IDisposable CreateExplicit(string family, Stream stream, int order, Endianness endianness = Endianness.LittleEndian) {
		var serializer = PrimitiveSerializer<long>.Instance;
		return family switch {
			"BTree" => new StreamMappedBTree<long, long>(order, stream, serializer, serializer, endianness: endianness),
			"BTreePlus" => new StreamMappedBTreePlus<long, long>(order, stream, serializer, serializer, endianness: endianness),
			_ => new StreamMappedBTreeLookup<long, long>(order, stream, serializer, serializer, Comparer<long>.Default, endianness)
		};
	}

	private static void AddAndAssert(string family, IDisposable instance) {
		if (family == "Lookup") {
			var lookup = (StreamMappedBTreeLookup<long, long>)instance;
			lookup.Add(7, 11);
			lookup.Add(7, 13);
		} else {
			var tree = (BTreeBase<long, long>)instance;
			tree.Add(7, 11);
		}
		AssertExisting(family, instance);
	}

	private static void AssertExisting(string family, IDisposable instance) {
		if (family == "Lookup") {
			var lookup = (StreamMappedBTreeLookup<long, long>)instance;
			Assert.That(lookup.GetValues(7), Is.EqualTo(new long[] { 11, 13 }));
			Assert.That(lookup.Validate(out var error), Is.True, error);
		} else {
			var tree = (BTreeBase<long, long>)instance;
			Assert.That(tree[7], Is.EqualTo(11));
			Assert.That(tree.Validate(out var error), Is.True, error);
		}
	}

	private static void AssertRefused(MemoryStream stream, Func<IDisposable> create, Type exceptionType) {
		var before = stream.ToArray();
		stream.Position = 5;
		Assert.That(() => create(), Throws.InstanceOf(exceptionType));
		Assert.That(stream.Position, Is.EqualTo(5));
		Assert.That(stream.ToArray(), Is.EqualTo(before));
	}
}
