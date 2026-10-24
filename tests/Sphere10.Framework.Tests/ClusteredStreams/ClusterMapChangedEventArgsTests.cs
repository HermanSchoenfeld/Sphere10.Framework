// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ClusterMapChangedEventArgsTests {

	[Test]
	public void PublicCollectionsAreStableMutableAndOwnedByEachEvent() {
		var changed = new ClusterMapChangedEventArgs();
		Assert.That(changed.AddedClusters, Is.SameAs(changed.AddedClusters));
		Assert.That(changed.ModifiedClusters, Is.SameAs(changed.ModifiedClusters));
		Assert.That(changed.RemovedClusters, Is.SameAs(changed.RemovedClusters));
		Assert.That(changed.MovedClusters, Is.SameAs(changed.MovedClusters));
		Assert.That(changed.MovedTerminals, Is.SameAs(changed.MovedTerminals));
		changed.AddedClusters.Add(1);
		changed.ModifiedClusters.Add(2);
		changed.RemovedClusters.Add(3);
		changed.MovedClusters.Add(4, 5);
		changed.MovedTerminals[6] = (7, 8);
		var next = new ClusterMapChangedEventArgs();
		Assert.That(next.AddedClusters, Is.Empty);
		Assert.That(next.ModifiedClusters, Is.Empty);
		Assert.That(next.RemovedClusters, Is.Empty);
		Assert.That(next.MovedClusters, Is.Empty);
		Assert.That(next.MovedTerminals, Is.Empty);
	}

	[Test]
	public void ExplicitCollectionsReplacePendingDataAndRemainThePublicInstances() {
		var added = new HashSet<long> { 1 };
		var modified = new HashSet<long> { 2 };
		var removed = new HashSet<long> { 3 };
		var moved = new BijectiveDictionary<long, long> { [4] = 5 };
		var terminals = new Dictionary<long, (long? NewStart, long? NewEnd)> { [6] = (7, 8) };
		var changed = new ClusterMapChangedEventArgs();
		changed.AddModifiedCluster(99);
		changed.AddedClusters = added;
		changed.ModifiedClusters = modified;
		changed.RemovedClusters = removed;
		changed.MovedClusters = moved;
		changed.MovedTerminals = terminals;
		changed.AddModifiedCluster(10);
		Assert.That(changed.AddedClusters, Is.SameAs(added));
		Assert.That(changed.ModifiedClusters, Is.SameAs(modified));
		Assert.That(changed.ModifiedClusters, Is.EquivalentTo(new long[] { 2, 10 }));
		Assert.That(changed.RemovedClusters, Is.SameAs(removed));
		Assert.That(changed.MovedClusters, Is.SameAs(moved));
		Assert.That(changed.MovedTerminals, Is.SameAs(terminals));
		Assert.That(changed.TryGetMovedCluster(4, out var cluster), Is.True);
		Assert.That(cluster, Is.EqualTo(5));
		Assert.That(changed.TryGetMovedTerminal(6, out var terminal), Is.True);
		Assert.That(terminal, Is.EqualTo(((long?)7, (long?)8)));
	}

	[Test]
	public void ExplicitNullAssignmentsRetainTheirExistingPropertyContract() {
		var changed = new ClusterMapChangedEventArgs {
			AddedClusters = null,
			ModifiedClusters = null,
			RemovedClusters = null,
			MovedClusters = null,
			MovedTerminals = null
		};
		Assert.That(changed.AddedClusters, Is.Null);
		Assert.That(changed.ModifiedClusters, Is.Null);
		Assert.That(changed.RemovedClusters, Is.Null);
		Assert.That(changed.MovedClusters, Is.Null);
		Assert.That(changed.MovedTerminals, Is.Null);
	}

	[Test]
	public void ModifiedClustersRemainASetBeforeAndAfterItsFirstPublicRead() {
		var changed = new ClusterMapChangedEventArgs();
		changed.AddModifiedCluster(3);
		changed.AddModifiedCluster(3);
		changed.AddModifiedCluster(4);
		changed.AddModifiedCluster(4);
		var modified = changed.ModifiedClusters;
		Assert.That(modified, Is.EquivalentTo(new long[] { 3, 4 }));
		modified.Remove(3);
		changed.AddModifiedCluster(5);
		Assert.That(changed.ModifiedClusters, Is.SameAs(modified));
		Assert.That(modified, Is.EquivalentTo(new long[] { 4, 5 }));
	}

	[Test]
	public void SuccessiveMovesPreserveTheOriginalClusterAndInverseMapping() {
		var changed = new ClusterMapChangedEventArgs();
		changed.InformMovedCluster(5, 2);
		changed.InformMovedCluster(2, 1);
		Assert.That(changed.MovedClusters.Count, Is.EqualTo(1));
		Assert.That(changed.MovedClusters[5], Is.EqualTo(1));
		Assert.That(changed.MovedClusters.TryGetKey(1, out var original), Is.True);
		Assert.That(original, Is.EqualTo(5));
		Assert.That(changed.RemovedClusters, Is.EquivalentTo(new long[] { 2 }));
	}

	[Test]
	public void ReplacingAMovedDestinationRemovesTheEarlierCluster() {
		var changed = new ClusterMapChangedEventArgs();
		changed.InformMovedCluster(5, 1);
		changed.InformMovedCluster(6, 1);
		Assert.That(changed.MovedClusters.Count, Is.EqualTo(1));
		Assert.That(changed.MovedClusters[6], Is.EqualTo(1));
		Assert.That(changed.MovedClusters.TryGetKey(1, out var original), Is.True);
		Assert.That(original, Is.EqualTo(6));
		Assert.That(changed.RemovedClusters, Is.EquivalentTo(new long[] { 5 }));
	}

	[Test]
	public void TerminalUpdatesPreserveBothEndsAndPublicMutations() {
		var changed = new ClusterMapChangedEventArgs();
		changed.InformChainNewStart(1, 2);
		changed.InformChainNewEnd(1, 3);
		Assert.That(changed.TryGetMovedTerminal(1, out var terminal), Is.True);
		Assert.That(terminal, Is.EqualTo(((long?)2, (long?)3)));
		changed.MovedTerminals[1] = (4, null);
		Assert.That(changed.TryGetMovedTerminal(1, out terminal), Is.True);
		Assert.That(terminal, Is.EqualTo(((long?)4, (long?)null)));
	}

	[Test]
	public void MutationsThroughTheInverseBijectionRemainVisibleToInternalLookups() {
		var changed = new ClusterMapChangedEventArgs();
		changed.MovedClusters.Bijection.Add(2, 1);
		Assert.That(changed.HasMovedClusters, Is.True);
		Assert.That(changed.TryGetMovedCluster(1, out var moved), Is.True);
		Assert.That(moved, Is.EqualTo(2));
		changed.MovedClusters.Bijection.Remove(2);
		Assert.That(changed.HasMovedClusters, Is.False);
		Assert.That(changed.TryGetMovedCluster(1, out _), Is.False);
	}

	[Test]
	public void CapturedWriteEventsKeepIndependentPayloadsAfterLaterWrites() {
		var map = new InMemoryClusterMap(new ExtendedList<Cluster>(), 16);
		map.NewClusterChain(2, 7);
		var events = new List<ClusterMapChangedEventArgs>();
		map.Changed += (_, changed) => events.Add(changed);
		map.WriteClusterData(0, 0, new byte[] { 1 });
		map.WriteClusterData(1, 0, new byte[] { 2 });
		Assert.That(events.Count, Is.EqualTo(2));
		Assert.That(events[0], Is.Not.SameAs(events[1]));
		Assert.That(events[0].ModifiedClusters, Is.EquivalentTo(new long[] { 0 }));
		Assert.That(events[1].ModifiedClusters, Is.EquivalentTo(new long[] { 1 }));
	}

	[Test]
	public void EventConstructionDoesNotAllocateUnusedCollections() {
		var events = new ClusterMapChangedEventArgs[1000];
		_ = new ClusterMapChangedEventArgs();
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var index = 0; index < events.Length; index++)
			events[index] = new ClusterMapChangedEventArgs();
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		TestContext.WriteLine($"1000 cluster change event constructors: {allocated:N0} allocated bytes.");
		Assert.That(events[0].ModifiedClusters, Is.Empty);
		Assert.That(allocated, Is.LessThan(256_000), "Unused mutation collections must not be allocated for every cluster write.");
		GC.KeepAlive(events);
	}

	[Test]
	public void SingleClusterWriteKeepsModifiedPayloadWithoutAllocatingASet() {
		var map = new InMemoryClusterMap(new ExtendedList<Cluster>(), 16);
		map.NewClusterChain(1, 7);
		var data = new byte[] { 1 };
		var events = new ClusterMapChangedEventArgs[1000];
		var next = 0;
		map.Changed += (_, changed) => events[next++] = changed;
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var index = 0; index < events.Length; index++)
			map.WriteClusterData(0, 0, data);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		TestContext.WriteLine($"1000 captured single-cluster writes: {allocated:N0} allocated bytes.");
		Assert.That(next, Is.EqualTo(events.Length));
		Assert.That(events[0].ModifiedClusters, Is.EquivalentTo(new long[] { 0 }));
		Assert.That(events[^1].ModifiedClusters, Is.EquivalentTo(new long[] { 0 }));
		Assert.That(allocated, Is.LessThan(256_000), "Single-cluster writes should retain their event without allocating unused collection graphs.");
	}
}
