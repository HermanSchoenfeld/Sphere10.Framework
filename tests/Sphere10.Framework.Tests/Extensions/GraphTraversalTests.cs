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
public class GraphTraversalTests {

	[Test]
	public void VisitUsesDepthFirstPreorderAcrossCyclesAndSharedNodes() {
		var edges = new Dictionary<string, string[]> {
			["root"] = new[] { "left", null, "right", "left" },
			["left"] = new[] { "shared" },
			["right"] = new[] { "shared", "root" },
			["shared"] = new[] { "root" },
			["last"] = null
		};
		var roots = new[] { "root", "last", "right" };
		Assert.That(roots.Visit(item => edges[item]), Is.EqualTo(new[] { "root", "left", "shared", "last", "right" }));
	}

	[Test]
	public void RepeatedEnumerationsShareVisitedState() {
		var traversal = 0.Visit(item => item < 3 ? new[] { item + 1 } : new[] { 0 });
		Assert.That(traversal.ToArray(), Is.EqualTo(new[] { 0, 1, 2, 3 }));
		Assert.That(traversal, Is.Empty);
	}

	[Test]
	public void InterleavedEnumerationsShareCompletedVisits() {
		var traversal = 0.Visit(item => item < 3 ? new[] { item + 1 } : new[] { 0 });
		using var first = traversal.GetEnumerator();
		Assert.That(first.MoveNext(), Is.True);
		Assert.That(first.Current, Is.Zero);
		Assert.That(first.MoveNext(), Is.True);
		Assert.That(first.Current, Is.EqualTo(1));
		Assert.That(traversal, Is.Empty);
		var remaining = new List<int>();
		while (first.MoveNext())
			remaining.Add(first.Current);
		Assert.That(remaining, Is.EqualTo(new[] { 2, 3 }));
	}

	[Test]
	public void NullItemsEndTheirSiblingSequenceWithoutEndingParentSiblings() {
		var roots = new[] { "root", "last" };
		Func<string, IEnumerable<string>> noEdges = _ => Array.Empty<string>();
		IEnumerable<string> Edges(string item) => item == "root" ? new[] { "first", null, "skipped" } : null;
		Assert.That(roots.Visit(Edges), Is.EqualTo(new[] { "root", "first", "last" }));
		Assert.That(new[] { "root", null, "skipped-root" }.Visit(noEdges), Is.EqualTo(new[] { "root" }));
		Assert.That(new[] { null, "skipped-root" }.Visit(noEdges), Is.Empty);
	}

	[Test]
	public void ComparerMergesAliasesAndRejectedItemsPruneTheirDescendants() {
		var expanded = new List<string>();
		IEnumerable<string> Edges(string item) {
			expanded.Add(item);
			return item switch {
				"root" => new[] { "ROOT", "blocked", "leaf", "LEAF" },
				"blocked" => new[] { "hidden" },
				_ => null
			};
		}
		var visited = "root".Visit(Edges, item => item != "blocked", StringComparer.OrdinalIgnoreCase).ToArray();
		Assert.That(visited, Is.EqualTo(new[] { "root", "leaf" }));
		Assert.That(expanded, Is.EqualTo(visited));
	}

	[Test]
	public void VisitIsLazyAndEarlyDisposalClosesEveryActiveEnumerator() {
		var closed = new List<int>();
		var expanded = new List<int>();
		IEnumerable<int> Roots() {
			try {
				yield return 0;
			} finally {
				closed.Add(-1);
			}
		}
		IEnumerable<int> Edges(int item) {
			expanded.Add(item);
			try {
				yield return item + 1;
			} finally {
				closed.Add(item);
			}
		}
		var traversal = Roots().Visit(Edges);
		Assert.That(expanded, Is.Empty);
		using (var enumerator = traversal.GetEnumerator()) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.Zero);
			Assert.That(expanded, Is.Empty);
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(1));
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(2));
		}
		Assert.That(expanded, Is.EqualTo(new[] { 0, 1 }));
		Assert.That(closed, Is.EqualTo(new[] { 1, 0, -1 }));
	}

	[Test]
	public void FailureClosesActiveEnumeratorsAndPreservesTheException([Values] bool failDuringMove) {
		var error = new IOException("Injected traversal failure");
		var closed = new List<int>();
		IEnumerable<int> Roots() {
			try {
				yield return 0;
			} finally {
				closed.Add(-1);
			}
		}
		IEnumerable<int> Edges(int item) {
			try {
				if (item == 1)
					throw error;
				yield return item + 1;
			} finally {
				closed.Add(item);
			}
		}
		IEnumerable<int> SelectEdges(int item) => item == 1 && !failDuringMove ? throw error : Edges(item);
		Assert.That(() => Roots().Visit(SelectEdges).ToArray(), Throws.Exception.SameAs(error));
		Assert.That(closed, Is.EquivalentTo(failDuringMove ? new[] { -1, 0, 1 } : new[] { -1, 0 }));
	}

	[Test]
	public void AncestorOverloadStopsAtNullOrAnAlreadyVisitedAncestor() {
		var parents = new Dictionary<string, string> { ["child"] = "parent", ["parent"] = "child", ["single"] = null };
		Assert.That("child".Visit(item => parents[item]), Is.EqualTo(new[] { "child", "parent" }));
		Assert.That("single".Visit(item => parents[item]), Is.EqualTo(new[] { "single" }));
		Assert.That("child".Visit(item => parents[item], item => item != "parent"), Is.EqualTo(new[] { "child" }));
	}

	[Test]
	public void NullRootsAreEmptyAndAnEmptyConnectionSequenceEndsOnlyItsOwnBranch() {
		IEnumerable<string> roots = null;
		Func<string, IEnumerable<string>> edges = _ => null;
		Assert.That(roots.Visit(edges), Is.Empty);
		Assert.That(((string)null).Visit(edges), Is.Empty);
		Assert.That(new[] { "first", "last" }.Visit(edges), Is.EqualTo(new[] { "first", "last" }));
	}

	[Test]
	public void NullSelectorsFailLazilyAfterYieldingTheRoot([Values] bool ancestorOverload) {
		var traversal = ancestorOverload
			? "root".Visit((Func<string, string>)null)
			: "root".Visit((Func<string, IEnumerable<string>>)null);
		using var enumerator = traversal.GetEnumerator();
		Assert.That(enumerator.MoveNext(), Is.True);
		Assert.That(enumerator.Current, Is.EqualTo("root"));
		Assert.That(() => enumerator.MoveNext(), Throws.TypeOf<NullReferenceException>());

		var empty = ancestorOverload
			? ((string)null).Visit((Func<string, string>)null)
			: ((string)null).Visit((Func<string, IEnumerable<string>>)null);
		Assert.That(empty, Is.Empty);
	}
}
