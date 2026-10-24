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
public class GraphNodeTests {

	[Test]
	public void BuildGraphPreservesSharedNodesCyclesRepeatedEdgesAndDepthFirstOrder() {
		var edges = new[] { new[] { 1, 2, 1, 0 }, new[] { 3 }, new[] { 3 }, new[] { 0 } };
		var expanded = new List<int>();
		var graph = 0.BuildGraph(item => {
			expanded.Add(item);
			return edges[item];
		});
		Assert.That(expanded, Is.EqualTo(new[] { 0, 1, 3, 2 }));
		Assert.That(graph.Entity, Is.Zero);
		Assert.That(graph.Connections.Select(item => item.Entity), Is.EqualTo(new[] { 1, 2, 1, 0 }));
		Assert.That(graph.Connections.ElementAt(2), Is.SameAs(graph.Connections.ElementAt(0)));
		Assert.That(graph.Connections.ElementAt(3), Is.SameAs(graph));
		var shared = graph.Connections.ElementAt(0).Connections.Single();
		Assert.That(graph.Connections.ElementAt(1).Connections.Single(), Is.SameAs(shared));
		Assert.That(shared.Connections.Single(), Is.SameAs(graph));
		Assert.That(graph.Visit().Select(item => item.Entity), Is.EqualTo(expanded));
		Assert.That(graph.Visit().Select(item => item.Entity), Is.EqualTo(expanded));
		edges[0][0] = 99;
		Assert.That(graph.Connections.ElementAt(0).Entity, Is.EqualTo(1), "Connections are a materialized membership snapshot.");
	}

	[Test]
	public void MultipleRootsShareComparerIdentityAndRetainReachableRootsInInputOrder() {
		var roots = new[] { "Root", "SHARED", "root", null, "Other" };
		var graph = roots.BuildGraph(item => item switch {
			"Root" => new[] { "shared", "SHARED" },
			"shared" => new[] { "ROOT" },
			_ => null
		}, comparer: StringComparer.OrdinalIgnoreCase);
		Assert.That(graph.Select(item => item.Entity), Is.EqualTo(new[] { "Root", "shared", "Other" }));
		Assert.That(graph[0].Connections.ElementAt(0), Is.SameAs(graph[1]));
		Assert.That(graph[0].Connections.ElementAt(1), Is.SameAs(graph[1]));
		Assert.That(graph[1].Connections.Single(), Is.SameAs(graph[0]));
		Assert.That(graph[2].Connections, Is.Empty);
	}

	[Test]
	public void BuildGraphPrunesRejectedItemsOncePerIdentityAndSkipsNullSiblings() {
		var filtered = new List<string>();
		var expanded = new List<string>();
		var graph = "root".BuildGraph(item => {
			expanded.Add(item);
			return item == "root" ? new[] { null, "blocked", "leaf", "blocked", null } : null;
		}, item => {
			filtered.Add(item);
			return item != "blocked";
		});
		Assert.That(filtered, Is.EqualTo(new[] { "root", "blocked", "leaf" }));
		Assert.That(expanded, Is.EqualTo(new[] { "root", "leaf" }));
		Assert.That(graph.Visit().Select(item => item.Entity), Is.EqualTo(new[] { "root", "leaf" }));
		Assert.That(graph.Connections.Single().Connections, Is.Empty);
	}

	[Test]
	public void DefaultEqualityMergesValuesWhileReferenceComparerRetainsSeparateInstances([Values] bool referenceIdentity) {
		var first = new string('A', 3);
		var second = new string('A', 3);
		Assert.That(first, Is.Not.SameAs(second));
		var graph = new[] { first, second }.BuildGraph((string item) => Array.Empty<string>(),
			comparer: referenceIdentity ? ReferenceEqualityComparer.Instance : null);
		Assert.That(graph, Has.Length.EqualTo(referenceIdentity ? 2 : 1));
		Assert.That(graph[0].Entity, Is.SameAs(first));
		if (referenceIdentity) {
			Assert.That(graph[1].Entity, Is.SameAs(second));
			graph[0].Connections = new[] { graph[1] };
			Assert.That(graph[0].Visit().Count(), Is.EqualTo(2));
		}
	}

	[Test]
	public void GraphNodeVisitUsesNodeIdentityAndPrunesRejectedConnections() {
		var root = new GraphNode<string>("same");
		var included = new GraphNode<string>("same");
		var blocked = new GraphNode<string>("blocked") { Connections = new[] { new GraphNode<string>("hidden") } };
		root.Connections = new[] { null, blocked, included, root };
		included.Connections = new[] { root };
		Assert.That(root.Visit(item => item != blocked), Is.EqualTo(new[] { root, included }));
		Assert.That(root.Visit().Count(), Is.EqualTo(4));
		included.Connections = null;
		Assert.That(included.Visit(), Is.EqualTo(new[] { included }));
	}

	[Test]
	public void GraphNodeVisitRepeatedAndInterleavedEnumerationsHaveIndependentState() {
		var root = 0.BuildGraph(item => item < 3 ? new[] { item + 1 } : new[] { 0 });
		var traversal = root.Visit();
		Assert.That(traversal.Select(item => item.Entity), Is.EqualTo(new[] { 0, 1, 2, 3 }));
		Assert.That(traversal.Select(item => item.Entity), Is.EqualTo(new[] { 0, 1, 2, 3 }));
		using var first = traversal.GetEnumerator();
		Assert.That(first.MoveNext(), Is.True);
		Assert.That(first.Current, Is.SameAs(root));
		Assert.That(traversal.Select(item => item.Entity), Is.EqualTo(new[] { 0, 1, 2, 3 }));
		var remaining = new List<int>();
		while (first.MoveNext())
			remaining.Add(first.Current.Entity);
		Assert.That(remaining, Is.EqualTo(new[] { 1, 2, 3 }));
	}

	[Test]
	public void GraphNodeVisitIsLazyAndEarlyDisposalClosesActiveConnections() {
		var expanded = new List<int>();
		var closed = new List<int>();
		IEnumerable<GraphNode<int>> Connections(int entity) {
			expanded.Add(entity);
			try {
				yield return new GraphNode<int>(entity + 1) { Connections = Connections(entity + 1) };
			} finally {
				closed.Add(entity);
			}
		}
		var root = new GraphNode<int>(0) { Connections = Connections(0) };
		var traversal = root.Visit();
		Assert.That(expanded, Is.Empty);
		using (var enumerator = traversal.GetEnumerator()) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.SameAs(root));
			Assert.That(expanded, Is.Empty);
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current.Entity, Is.EqualTo(1));
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current.Entity, Is.EqualTo(2));
		}
		Assert.That(expanded, Is.EqualTo(new[] { 0, 1 }));
		Assert.That(closed, Is.EqualTo(new[] { 1, 0 }));
	}

	[Test]
	public void GraphNodeVisitFailureClosesActiveConnectionsAndPreservesTheException() {
		var error = new IOException("Injected graph traversal failure");
		var closed = new List<int>();
		IEnumerable<GraphNode<int>> Connections(int entity) {
			try {
				if (entity == 1)
					throw error;
				yield return new GraphNode<int>(entity + 1) { Connections = Connections(entity + 1) };
			} finally {
				closed.Add(entity);
			}
		}
		var root = new GraphNode<int>(0) { Connections = Connections(0) };
		Assert.That(() => root.Visit().ToArray(), Throws.Exception.SameAs(error));
		Assert.That(closed, Is.EqualTo(new[] { 1, 0 }));
	}

	[Test]
	public void SimpleGraphBridgeMaterializesConnectionsAndSupportsCycles() {
		var root = new GraphNode<int>(0);
		var child = new GraphNode<int>(1) { Connections = new[] { root } };
		var connections = new List<GraphNode<int>> { child };
		ISimpleGraph<int, GraphNode<int>> simple = root;
		simple.Connections = connections;
		connections.Clear();
		Assert.That(root.Connections, Is.EqualTo(new[] { child }));
		Assert.That(root.Visit(), Is.EqualTo(new[] { root, child }));
		simple.Connections = null;
		Assert.That(root.Connections, Is.Empty);
	}

	[Test]
	public void BuildGraphEnumeratesRootsAndConnectionsOnceAndClosesThem() {
		var rootReads = 0;
		var edgeReads = new int[3];
		var closed = new List<int>();
		IEnumerable<int> Roots() {
			Assert.That(++rootReads, Is.EqualTo(1));
			try {
				yield return 0;
				yield return 2;
			} finally {
				closed.Add(-1);
			}
		}
		IEnumerable<int> Edges(int item) {
			Assert.That(++edgeReads[item], Is.EqualTo(1));
			try {
				yield return (item + 1) % 3;
			} finally {
				closed.Add(item);
			}
		}
		var graph = Roots().BuildGraph(Edges);
		Assert.That(graph.Select(item => item.Entity), Is.EqualTo(new[] { 0, 2 }));
		Assert.That(graph[0].Visit().Select(item => item.Entity), Is.EqualTo(new[] { 0, 1, 2 }));
		Assert.That(graph[1], Is.SameAs(graph[0].Connections.ElementAt(0).Connections.ElementAt(0)));
		Assert.That(edgeReads, Is.EqualTo(new[] { 1, 1, 1 }));
		Assert.That(closed, Is.EquivalentTo(new[] { -1, 0, 1, 2 }));
	}

	[Test]
	public void BuildFailureClosesActiveEnumeratorsAndPreservesTheException([Values] bool failDuringMove) {
		var error = new IOException("Injected graph construction failure");
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
		Assert.That(() => Roots().BuildGraph(SelectEdges), Throws.Exception.SameAs(error));
		Assert.That(closed, Is.EqualTo(failDuringMove ? new[] { 1, 0, -1 } : new[] { 0, -1 }));
	}

	[Test]
	public void NullAndRejectedRootsProduceNoNodesAndAncestorOverloadPreservesCycles() {
		Func<string, IEnumerable<string>> noEdges = _ => null;
		Assert.That(((string)null).BuildGraph(noEdges), Is.Null);
		Assert.That(((IEnumerable<string>)null).BuildGraph(noEdges), Is.Empty);
		Assert.That("excluded".BuildGraph(noEdges, _ => false), Is.Null);
		var parents = new Dictionary<string, string> { ["child"] = "parent", ["parent"] = "child" };
		var graph = "child".BuildGraph(item => parents[item]);
		Assert.That(graph.Connections.Single().Connections.Single(), Is.SameAs(graph));
		Assert.That(graph.Visit().Select(item => item.Entity), Is.EqualTo(new[] { "child", "parent" }));
	}

	[Test]
	public void DeepGraphConstructionAndVisitDoNotRequireRecursiveCalls() {
		const int count = 20000;
		var graph = 0.BuildGraph(item => item + 1 < count ? new[] { item + 1 } : Array.Empty<int>());
		Assert.That(graph.Visit().Select(item => item.Entity), Is.EqualTo(Enumerable.Range(0, count)));
	}
}
