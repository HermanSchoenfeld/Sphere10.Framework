// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BTreeLookupTests {

	#region InMemoryBTreeLookup Tests

	[Test]
	public void InMemory_Add_SingleEntry() {
		var Lookup = CreateInMemoryLookup();
		Lookup.Add("key1", 100L);
		Assert.That(Lookup.TotalCount, Is.EqualTo(1));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(1));
		Assert.That(Lookup.ContainsKey("key1"), Is.True);
		Assert.That(Lookup.GetValues("key1").ToArray(), Is.EqualTo(new[] { 100L }));
	}

	[Test]
	public void InMemory_Add_MultipleValuesPerKey() {
		var Lookup = CreateInMemoryLookup();
		Lookup.Add("key1", 100L);
		Lookup.Add("key1", 200L);
		Lookup.Add("key1", 300L);
		Assert.That(Lookup.TotalCount, Is.EqualTo(3));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(1));
		Assert.That(Lookup.GetValues("key1").OrderBy(X => X).ToArray(), Is.EqualTo(new[] { 100L, 200L, 300L }));
	}

	[Test]
	public void InMemory_Add_MultipleKeys() {
		var Lookup = CreateInMemoryLookup();
		Lookup.Add("key1", 100L);
		Lookup.Add("key2", 200L);
		Lookup.Add("key3", 300L);
		Assert.That(Lookup.TotalCount, Is.EqualTo(3));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(3));
		Assert.That(Lookup.GetValues("key1").ToArray(), Is.EqualTo(new[] { 100L }));
		Assert.That(Lookup.GetValues("key2").ToArray(), Is.EqualTo(new[] { 200L }));
		Assert.That(Lookup.GetValues("key3").ToArray(), Is.EqualTo(new[] { 300L }));
	}

	[Test]
	public void InMemory_Remove_SpecificEntry() {
		var Lookup = CreateInMemoryLookup();
		Lookup.Add("key1", 100L);
		Lookup.Add("key1", 200L);
		var Removed = Lookup.Remove("key1", 100L);
		Assert.That(Removed, Is.True);
		Assert.That(Lookup.TotalCount, Is.EqualTo(1));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(1));
		Assert.That(Lookup.GetValues("key1").ToArray(), Is.EqualTo(new[] { 200L }));
	}

	[Test]
	public void InMemory_Remove_LastEntry_UpdatesDistinctCount() {
		var Lookup = CreateInMemoryLookup();
		Lookup.Add("key1", 100L);
		Lookup.Remove("key1", 100L);
		Assert.That(Lookup.TotalCount, Is.EqualTo(0));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(0));
		Assert.That(Lookup.ContainsKey("key1"), Is.False);
	}

	[Test]
	public void InMemory_Remove_NonExistent_ReturnsFalse() {
		var Lookup = CreateInMemoryLookup();
		Lookup.Add("key1", 100L);
		var Removed = Lookup.Remove("key1", 999L);
		Assert.That(Removed, Is.False);
		Assert.That(Lookup.TotalCount, Is.EqualTo(1));
	}

	[Test]
	public void InMemory_ContainsKey_NotFound() {
		var Lookup = CreateInMemoryLookup();
		Assert.That(Lookup.ContainsKey("nonexistent"), Is.False);
	}

	[Test]
	public void InMemory_GetValues_EmptyForMissingKey() {
		var Lookup = CreateInMemoryLookup();
		var Values = Lookup.GetValues("nonexistent").ToList();
		Assert.That(Values, Is.Empty);
	}

	[Test]
	public void InMemory_Clear() {
		var Lookup = CreateInMemoryLookup();
		Lookup.Add("key1", 100L);
		Lookup.Add("key2", 200L);
		Lookup.Clear();
		Assert.That(Lookup.TotalCount, Is.EqualTo(0));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(0));
	}

	[Test]
	public void InMemory_EnumerateGroupings() {
		var Lookup = CreateInMemoryLookup();
		Lookup.Add("alpha", 1L);
		Lookup.Add("alpha", 2L);
		Lookup.Add("beta", 3L);
		var Groups = Lookup.EnumerateGroupings().ToList();
		Assert.That(Groups.Count, Is.EqualTo(2));
		var Alpha = Groups.First(G => G.Key == "alpha");
		var Beta = Groups.First(G => G.Key == "beta");
		Assert.That(Alpha.OrderBy(X => X).ToArray(), Is.EqualTo(new[] { 1L, 2L }));
		Assert.That(Beta.ToArray(), Is.EqualTo(new[] { 3L }));
	}

	#endregion

	#region StreamMappedBTreeLookup Tests

	[Test]
	public void StreamMapped_Add_MultipleValuesPerKey() {
		using var Lookup = CreateStreamMappedLookup();
		Lookup.Add(42, 100L);
		Lookup.Add(42, 200L);
		Lookup.Add(42, 300L);
		Assert.That(Lookup.TotalCount, Is.EqualTo(3));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(1));
		Assert.That(Lookup.GetValues(42).OrderBy(X => X).ToArray(), Is.EqualTo(new[] { 100L, 200L, 300L }));
	}

	[Test]
	public void StreamMapped_Add_MultipleKeys() {
		using var Lookup = CreateStreamMappedLookup();
		Lookup.Add(1, 100L);
		Lookup.Add(2, 200L);
		Lookup.Add(3, 300L);
		Assert.That(Lookup.TotalCount, Is.EqualTo(3));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(3));
	}

	[Test]
	public void StreamMapped_Remove_SpecificEntry() {
		using var Lookup = CreateStreamMappedLookup();
		Lookup.Add(42, 100L);
		Lookup.Add(42, 200L);
		var Removed = Lookup.Remove(42, 100L);
		Assert.That(Removed, Is.True);
		Assert.That(Lookup.TotalCount, Is.EqualTo(1));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(1));
		Assert.That(Lookup.GetValues(42).ToArray(), Is.EqualTo(new[] { 200L }));
	}

	[Test]
	public void StreamMapped_Clear() {
		using var Lookup = CreateStreamMappedLookup();
		Lookup.Add(1, 100L);
		Lookup.Add(2, 200L);
		Lookup.Clear();
		Assert.That(Lookup.TotalCount, Is.EqualTo(0));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(0));
	}

	[Test]
	public void StreamMapped_ContainsEntry() {
		using var Lookup = CreateStreamMappedLookup();
		Lookup.Add(42, 100L);
		Assert.That(Lookup.ContainsEntry(42, 100L), Is.True);
		Assert.That(Lookup.ContainsEntry(42, 999L), Is.False);
	}

	[Test]
	public void StreamMapped_RemoveAll() {
		using var Lookup = CreateStreamMappedLookup();
		Lookup.Add(42, 100L);
		Lookup.Add(42, 200L);
		Lookup.Add(99, 300L);
		Lookup.RemoveAll(42);
		Assert.That(Lookup.TotalCount, Is.EqualTo(1));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(1));
		Assert.That(Lookup.ContainsKey(42), Is.False);
		Assert.That(Lookup.ContainsKey(99), Is.True);
	}

	[Test]
	public void StreamMapped_Persistence_DataSurvivesReopen() {
		using var BackingStream = new MemoryStream();
		// Create and populate
		using (var Lookup = new StreamMappedBTreeLookup<int, long>(
			5, BackingStream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<long>.Instance,
			Comparer<int>.Default)) {
			Lookup.Add(1, 10L);
			Lookup.Add(1, 20L);
			Lookup.Add(2, 30L);
		}

		// Reopen from same stream
		BackingStream.Position = 0;
		using (var Lookup = new StreamMappedBTreeLookup<int, long>(
			5, BackingStream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<long>.Instance,
			Comparer<int>.Default)) {
			Assert.That(Lookup.TotalCount, Is.EqualTo(3));
			Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(2));
			Assert.That(Lookup.GetValues(1).OrderBy(X => X).ToArray(), Is.EqualTo(new[] { 10L, 20L }));
			Assert.That(Lookup.GetValues(2).ToArray(), Is.EqualTo(new[] { 30L }));
		}
	}

	[Test]
	public void StreamMapped_ManyEntries([Values(5, 10, 64)] int order) {
		using var Lookup = CreateStreamMappedLookup(order);
		// Add 100 keys with 3 values each
		for (var I = 0; I < 100; I++)
			for (var J = 0; J < 3; J++)
				Lookup.Add(I, I * 1000L + J);
		Assert.That(Lookup.TotalCount, Is.EqualTo(300));
		Assert.That(Lookup.DistinctKeyCount, Is.EqualTo(100));
		for (var I = 0; I < 100; I++) {
			var Values = Lookup.GetValues(I).OrderBy(X => X).ToArray();
			Assert.That(Values.Length, Is.EqualTo(3));
			Assert.That(Values, Is.EqualTo(new[] { I * 1000L, I * 1000L + 1, I * 1000L + 2 }));
		}
	}

	#endregion

	#region Helper Methods

	private static InMemoryBTreeLookup<string, long> CreateInMemoryLookup(int order = 5) {
		return new InMemoryBTreeLookup<string, long>(
			order,
			Comparer<string>.Default
		);
	}

	private static StreamMappedBTreeLookup<int, long> CreateStreamMappedLookup(int order = 5) {
		return new StreamMappedBTreeLookup<int, long>(
			order,
			new MemoryStream(),
			PrimitiveSerializer<int>.Instance,
			PrimitiveSerializer<long>.Instance,
			Comparer<int>.Default
		);
	}

	#endregion
}
