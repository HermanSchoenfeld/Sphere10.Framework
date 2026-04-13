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
public class BTreeFindRangeTests {

	#region InMemoryBTree FindRange Tests

	[Test]
	public void FindRange_EmptyTree_ReturnsEmpty() {
		var Tree = new InMemoryBTree<int, string>(5);
		var Result = Tree.FindRange(0, 100).ToList();
		Assert.That(Result, Is.Empty);
	}

	[Test]
	public void FindRange_AllInRange([Range(3, 10)] int order) {
		var Tree = new InMemoryBTree<int, string>(order);
		for (var I = 1; I <= 20; I++)
			Tree.Add(I, $"v{I}");
		var Result = Tree.FindRange(1, 20).ToList();
		Assert.That(Result.Count, Is.EqualTo(20));
		for (var I = 0; I < 20; I++)
			Assert.That(Result[I].Key, Is.EqualTo(I + 1));
	}

	[Test]
	public void FindRange_SubRange([Range(3, 10)] int order) {
		var Tree = new InMemoryBTree<int, string>(order);
		for (var I = 1; I <= 100; I++)
			Tree.Add(I, $"v{I}");
		var Result = Tree.FindRange(25, 75).ToList();
		Assert.That(Result.Count, Is.EqualTo(51));
		Assert.That(Result.First().Key, Is.EqualTo(25));
		Assert.That(Result.Last().Key, Is.EqualTo(75));
	}

	[Test]
	public void FindRange_NoMatches([Range(3, 10)] int order) {
		var Tree = new InMemoryBTree<int, string>(order);
		for (var I = 1; I <= 10; I++)
			Tree.Add(I, $"v{I}");
		var Result = Tree.FindRange(11, 20).ToList();
		Assert.That(Result, Is.Empty);
	}

	[Test]
	public void FindRange_SingleMatch([Range(3, 10)] int order) {
		var Tree = new InMemoryBTree<int, string>(order);
		for (var I = 1; I <= 10; I++)
			Tree.Add(I, $"v{I}");
		var Result = Tree.FindRange(5, 5).ToList();
		Assert.That(Result.Count, Is.EqualTo(1));
		Assert.That(Result[0].Key, Is.EqualTo(5));
	}

	[Test]
	public void FindRange_PartialOverlap_LowEnd([Range(3, 10)] int order) {
		var Tree = new InMemoryBTree<int, string>(order);
		for (var I = 10; I <= 20; I++)
			Tree.Add(I, $"v{I}");
		var Result = Tree.FindRange(5, 15).ToList();
		Assert.That(Result.Count, Is.EqualTo(6));
		Assert.That(Result.First().Key, Is.EqualTo(10));
		Assert.That(Result.Last().Key, Is.EqualTo(15));
	}

	[Test]
	public void FindRange_PartialOverlap_HighEnd([Range(3, 10)] int order) {
		var Tree = new InMemoryBTree<int, string>(order);
		for (var I = 10; I <= 20; I++)
			Tree.Add(I, $"v{I}");
		var Result = Tree.FindRange(15, 25).ToList();
		Assert.That(Result.Count, Is.EqualTo(6));
		Assert.That(Result.First().Key, Is.EqualTo(15));
		Assert.That(Result.Last().Key, Is.EqualTo(20));
	}

	[Test]
	public void FindRange_ResultsAreSorted([Range(3, 10)] int order) {
		var Tree = new InMemoryBTree<int, string>(order);
		var Rng = new System.Random(42);
		var Keys = Enumerable.Range(1, 200).OrderBy(_ => Rng.Next()).ToList();
		foreach (var Key in Keys)
			Tree.Add(Key, $"v{Key}");
		var Result = Tree.FindRange(50, 150).ToList();
		for (var I = 1; I < Result.Count; I++)
			Assert.That(Result[I].Key, Is.GreaterThan(Result[I - 1].Key));
	}

	#endregion

	#region StreamMappedBTree FindRange Tests

	[Test]
	public void StreamMapped_FindRange_SubRange([Range(3, 10)] int order) {
		using var Stream = new MemoryStream();
		var Tree = new StreamMappedBTree<int, int>(order, Stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance);
		for (var I = 1; I <= 100; I++)
			Tree.Add(I, I * 10);
		var Result = Tree.FindRange(25, 75).ToList();
		Assert.That(Result.Count, Is.EqualTo(51));
		Assert.That(Result.First().Key, Is.EqualTo(25));
		Assert.That(Result.Last().Key, Is.EqualTo(75));
		foreach (var Entry in Result)
			Assert.That(Entry.Value, Is.EqualTo(Entry.Key * 10));
	}

	[Test]
	public void StreamMapped_FindRange_Empty() {
		using var Stream = new MemoryStream();
		var Tree = new StreamMappedBTree<int, int>(5, Stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance);
		var Result = Tree.FindRange(0, 100).ToList();
		Assert.That(Result, Is.Empty);
	}

	#endregion
}
