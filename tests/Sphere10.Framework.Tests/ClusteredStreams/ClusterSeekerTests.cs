// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ClusterSeekerTests {
	[TestCase(100)]
	[TestCase(ClusterSeeker.MaxCheckpoints)]
	[TestCase(8193)]
	public void RepeatedSeeksUseNearbyCheckpoints(int count) {
		var clusters = new CountingClusterList();
		var map = new InMemoryClusterMap(clusters, 1);
		var seeker = CreateSeeker(map, count, 99, true);
		seeker.SeekForward(count - 1);
		var random = new Random(1234567);
		for (var i = 0; i < 100; i++) {
			var index = random.Next(count);
			var uncachedSteps = Math.Min(Math.Abs(index - seeker.Pointer.CurrentIndex), Math.Min(index, count - 1 - index));
			clusters.ReadCount = 0;
			seeker.SeekTo(index);
			Assert.That(seeker.Pointer.CurrentCluster, Is.EqualTo(index));
			if (ClusterSeeker.LogicalClusterMemoizationEnabled)
				Assert.That(clusters.ReadCount, Is.LessThanOrEqualTo((count - 1) / ClusterSeeker.MaxCheckpoints + 1));
			else
				Assert.That(clusters.ReadCount, Is.EqualTo(uncachedSteps));
		}
	}

	[Test]
	public void AppendAndTruncatePreservePositions([Values] bool integrityChecks) {
		var map = new InMemoryClusterMap(new ExtendedList<Cluster>(), 1);
		var seeker = CreateSeeker(map, 128, 99, integrityChecks);
		seeker.SeekForward(127);
		map.NewClusterChain(32, 100);
		map.AppendClustersToEnd(seeker.Pointer.Chain.EndCluster, 2048);
		AssertPositions(map, seeker);
		map.RemoveBackwards(seeker.Pointer.Chain.EndCluster, 2100);
		Assert.That(seeker.Pointer.Chain.TotalClusters, Is.EqualTo(76));
		AssertPositions(map, seeker);
	}

	[Test]
	public void OtherChainDeletionMovesCachedClusters([Values] bool integrityChecks) {
		var clusters = new CountingClusterList();
		var map = new InMemoryClusterMap(clusters, 1);
		var (_, otherEnd) = map.NewClusterChain(32, 100);
		var seeker = CreateSeeker(map, 128, 99, integrityChecks);
		seeker.SeekForward(127);
		map.RemoveBackwards(otherEnd, 32);
		clusters.ReadCount = 0;
		seeker.SeekTo(119);
		Assert.That(seeker.Pointer.CurrentCluster, Is.EqualTo(23));
		Assert.That(clusters.ReadCount, Is.EqualTo(ClusterSeeker.LogicalClusterMemoizationEnabled ? 0 : 8));
		AssertPositions(map, seeker);
	}

	[Test]
	public void ChangingLinksInvalidatesCachedPositions([Values] bool integrityChecks) {
		var map = new InMemoryClusterMap(new ExtendedList<Cluster>(), 1);
		var seeker = CreateSeeker(map, 10, 99, integrityChecks);
		seeker.SeekForward(9);
		seeker.SeekStart();
		// Swap two interior clusters while preserving the chain endpoints and length.
		map.WriteClusterNext(2, 4);
		map.WriteClusterPrev(4, 2);
		map.WriteClusterNext(4, 3);
		map.WriteClusterPrev(3, 4);
		map.WriteClusterNext(3, 5);
		map.WriteClusterPrev(5, 3);
		AssertPositions(map, seeker);
	}

	[Test]
	public void DataWritesPreserveCheckpoints() {
		var clusters = new CountingClusterList();
		var map = new InMemoryClusterMap(clusters, 1);
		var seeker = CreateSeeker(map, 100, 99, true);
		seeker.SeekForward(99);
		map.WriteClusterData(50, 0, new byte[] { 123 });
		clusters.ReadCount = 0;
		seeker.SeekTo(50);
		Assert.That(clusters.ReadCount, Is.EqualTo(ClusterSeeker.LogicalClusterMemoizationEnabled ? 0 : 49));
		Assert.That(map.ReadClusterData(seeker.Pointer.CurrentCluster, 0, 1), Is.EqualTo(new byte[] { 123 }));
	}

	[Test]
	public void RemovedChainCanBeReplaced([Values] bool integrityChecks) {
		var map = new InMemoryClusterMap(new ExtendedList<Cluster>(), 1);
		var seeker = CreateSeeker(map, 128, 99, integrityChecks);
		seeker.SeekForward(127);
		map.RemoveBackwards(seeker.Pointer.Chain.EndCluster, 128);
		Assert.That(() => seeker.SeekTo(0), Throws.Exception);
		map.NewClusterChain(16, 100);
		map.NewClusterChain(64, 99);
		AssertPositions(map, seeker);
		Assert.That(() => seeker.SeekTo(-1), Throws.Exception);
		Assert.That(() => seeker.SeekTo(64), Throws.Exception);
	}

	[Test]
	public void AppendingKeepsExistingCheckpointsWhenSpacingIsUnchanged() {
		var clusters = new CountingClusterList();
		var map = new InMemoryClusterMap(clusters, 1);
		var seeker = CreateSeeker(map, 100, 99, true);
		seeker.SeekForward(99);
		map.NewClusterChain(32, 100);
		map.AppendClustersToEnd(seeker.Pointer.Chain.EndCluster, 100);
		clusters.ReadCount = 0;
		seeker.SeekTo(50);
		Assert.That(seeker.Pointer.CurrentCluster, Is.EqualTo(50));
		Assert.That(clusters.ReadCount, Is.EqualTo(ClusterSeeker.LogicalClusterMemoizationEnabled ? 0 : 49));
		AssertPositions(map, seeker);
	}

	[Test]
	public void RecreatedChainDropsCheckpointsAfterSuppressedClear() {
		var map = new InMemoryClusterMap(new ExtendedList<Cluster>(), 1);
		var seeker = CreateSeeker(map, 100, 99, true);
		seeker.SeekForward(99);
		// ClusteredStreams.Clear suppresses map events while replacing its descriptor chain.
		map.SuppressEvents = true;
		map.Clear();
		map.SuppressEvents = false;
		map.NewClusterChain(16, 100);
		map.NewClusterChain(100, 99);
		AssertPositions(map, seeker);
	}

	private static ClusterSeeker CreateSeeker(ClusterMap map, long count, long terminal, bool integrityChecks) {
		var (start, end) = map.NewClusterChain(count, terminal);
		var seeker = new ClusterSeeker(map, terminal, start, end, count, integrityChecks);
		map.Changed += (_, changedEvent) => seeker.ProcessClusterMapChanged(changedEvent);
		return seeker;
	}

	private static void AssertPositions(ClusterMap map, ClusterSeeker seeker) {
		var expected = new List<long>();
		var physicalCluster = seeker.Pointer.Chain.StartCluster;
		for (var i = 0L; i < seeker.Pointer.Chain.TotalClusters; i++) {
			expected.Add(physicalCluster);
			physicalCluster = map.ReadClusterNext(physicalCluster);
		}
		Assert.That(physicalCluster, Is.EqualTo(seeker.TerminalValue));
		var random = new Random(1234567);
		for (var i = 0; i < 500; i++) {
			var index = random.Next(expected.Count);
			seeker.SeekTo(index);
			Assert.That(seeker.Pointer.CurrentIndex, Is.EqualTo(index));
			Assert.That(seeker.Pointer.CurrentCluster, Is.EqualTo(expected[index]));
		}
	}

	private sealed class CountingClusterList : ExtendedListDecorator<Cluster> {
		public CountingClusterList()
			: base(new ExtendedList<Cluster>()) {
		}

		public int ReadCount { get; set; }

		public override Cluster Read(long index) {
			ReadCount++;
			return base.Read(index);
		}
	}
}
