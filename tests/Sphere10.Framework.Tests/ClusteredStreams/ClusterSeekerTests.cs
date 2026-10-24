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
public class ClusterSeekerTests {
	[TestCase(0, 3, ClusterTraits.None)]
	[TestCase(1, 3, ClusterTraits.Start)]
	[TestCase(1, 3, ClusterTraits.End)]
	[TestCase(2, 3, ClusterTraits.None)]
	[TestCase(0, 1, ClusterTraits.End)]
	public void PointerRejectsMissingOrMisplacedTerminalTraits(long index, long count, ClusterTraits traits) {
		var pointer = new ClusterSeeker.ClusterPointer {
			Chain = new ClusterSeeker.ClusterChain { TotalClusters = count },
			CurrentCluster = 42,
			CurrentIndex = index,
			CurrentTraits = traits
		};
		Assert.That(pointer.CheckTraits, Throws.InvalidOperationException);
	}

	[TestCase(0, 3, ClusterTraits.Start)]
	[TestCase(1, 3, ClusterTraits.None)]
	[TestCase(2, 3, ClusterTraits.End)]
	[TestCase(0, 1, ClusterTraits.Start | ClusterTraits.End)]
	public void PointerAcceptsConsistentTerminalTraits(long index, long count, ClusterTraits traits) {
		var pointer = new ClusterSeeker.ClusterPointer {
			Chain = new ClusterSeeker.ClusterChain { TotalClusters = count },
			CurrentCluster = 42,
			CurrentIndex = index,
			CurrentTraits = traits
		};
		Assert.That(pointer.CheckTraits, Throws.Nothing);
	}

	[Test]
	public void PointerPreservesExistingAcceptanceOfSingleStartOnlyCluster() {
		// The existing END check accepts START here; integrity behavior is unchanged by the seek optimizations.
		var pointer = new ClusterSeeker.ClusterPointer {
			Chain = new ClusterSeeker.ClusterChain { TotalClusters = 1 },
			CurrentCluster = 42,
			CurrentIndex = 0,
			CurrentTraits = ClusterTraits.Start
		};
		Assert.That(pointer.CheckTraits, Throws.Nothing);
	}

	[TestCase(9, 8, 4, 0, 4, IterateDirection.LeftToRight)]
	[TestCase(11, 4, 2, 0, 2, IterateDirection.LeftToRight)]
	[TestCase(11, 4, 7, 10, 3, IterateDirection.RightToLeft)]
	[TestCase(11, 5, 6, 5, 1, IterateDirection.LeftToRight)]
	public void ShortestPathPreservesStartThenEndThenCurrentTieOrder(int count, int currentIndex, int targetIndex, long expectedCluster, long expectedSteps, IterateDirection expectedDirection) {
		var map = new InMemoryClusterMap(new ExtendedList<Cluster>(), 1);
		var (start, end) = map.NewClusterChain(count, 99);
		var seeker = new PathInspectingClusterSeeker(map, 99, start, end, count);
		seeker.SeekForward(currentIndex);
		var (cluster, steps, direction) = seeker.FindPath(targetIndex);
		Assert.That(cluster, Is.EqualTo(expectedCluster));
		Assert.That(steps, Is.EqualTo(expectedSteps));
		Assert.That(direction, Is.EqualTo(expectedDirection));
	}

	[Test]
	public void TraversalReachesBothBoundariesAcrossScratchThreshold([Values(1, 32, 33, 80)] int steps, [Values] bool integrityChecks) {
		var map = new InMemoryClusterMap(new ExtendedList<Cluster>(), 1);
		var seeker = CreateSeeker(map, steps + 1, 99, integrityChecks);
		seeker.SeekForward(steps);
		Assert.That(seeker.Pointer.CurrentIndex, Is.EqualTo(steps));
		Assert.That(seeker.Pointer.CurrentCluster, Is.EqualTo(seeker.Pointer.Chain.EndCluster));
		Assert.That(seeker.Pointer.CurrentTraits, Is.EqualTo(ClusterTraits.End));
		Assert.That(() => seeker.SeekForward(1), Throws.InvalidOperationException);
		seeker.SeekBackwards(steps);
		Assert.That(seeker.Pointer.CurrentIndex, Is.Zero);
		Assert.That(seeker.Pointer.CurrentCluster, Is.EqualTo(seeker.Pointer.Chain.StartCluster));
		Assert.That(seeker.Pointer.CurrentTraits, Is.EqualTo(ClusterTraits.Start));
		Assert.That(() => seeker.SeekBackwards(1), Throws.InvalidOperationException);
	}

	[Test]
	public void LongStepCountReachesBoundaryWithoutNarrowing([Values] bool backwards, [Values] bool integrityChecks) {
		var map = new InMemoryClusterMap(new ExtendedList<Cluster>(), 1);
		var seeker = CreateSeeker(map, 3, 99, integrityChecks);
		if (backwards) {
			seeker.SeekEnd();
			Assert.That(() => seeker.SeekBackwards(long.MaxValue), Throws.InvalidOperationException.With.Message.Contains("before the first cluster"));
			Assert.That(seeker.Pointer.CurrentIndex, Is.Zero);
		} else {
			Assert.That(() => seeker.SeekForward(long.MaxValue), Throws.InvalidOperationException.With.Message.Contains("past the last cluster"));
			Assert.That(seeker.Pointer.CurrentIndex, Is.EqualTo(2));
		}
	}

	[Test]
	public void RepeatedStationaryAndCheckpointSeeksDoNotAllocate([Values] bool integrityChecks) {
		var map = new InMemoryClusterMap(new ExtendedList<Cluster>(), 1);
		var seeker = CreateSeeker(map, 100, 99, integrityChecks);
		seeker.SeekForward(99);
		seeker.SeekTo(50);
		seeker.SeekTo(51);
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < 1000; i++) {
			seeker.SeekTo(50);
			seeker.SeekTo(50);
			seeker.SeekTo(51);
		}
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		TestContext.Out.WriteLine($"3,000 current/nearby cluster seeks (integrity={integrityChecks}): {allocated:N0} allocated bytes.");
		Assert.That(allocated, Is.LessThan(1024));
		Assert.That(seeker.Pointer.CurrentCluster, Is.EqualTo(51));
	}

	[TestCase(4, false)]
	[TestCase(32, false)]
	[TestCase(33, false)]
	[TestCase(80, false)]
	[TestCase(4, true)]
	[TestCase(32, true)]
	[TestCase(33, true)]
	[TestCase(80, true)]
	public void CyclesAreRejectedWithStackAndHeapTraversalScratch(int steps, bool backwards) {
		var map = new InMemoryClusterMap(new ExtendedList<Cluster>(), 1);
		var seeker = CreateSeeker(map, 100, 99, false);
		if (backwards) {
			seeker.SeekEnd();
			map.WriteClusterPrev(98, 99);
			Assert.That(() => seeker.SeekBackwards(steps), Throws.InvalidOperationException.With.Message.Contains("Cyclic dependency"));
		} else {
			map.WriteClusterNext(1, 0);
			Assert.That(() => seeker.SeekForward(steps), Throws.InvalidOperationException.With.Message.Contains("Cyclic dependency"));
		}
	}

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

	private sealed class PathInspectingClusterSeeker : ClusterSeeker {
		public PathInspectingClusterSeeker(ClusterMap map, long terminal, long start, long end, long count)
			: base(map, terminal, start, end, count, true) {
		}

		public (long Cluster, long Steps, IterateDirection Direction) FindPath(long index) {
			FindShortestPathToLogicalCluster(index, out var cluster, out var steps, out var direction);
			return (cluster, steps, direction);
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
