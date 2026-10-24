// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Sphere10.Framework.Tests;

/// <remarks>
/// During dev, bugs seemed to occur when clusters linked in descending order.
/// write unit tests which directly scramble the cluster links, different patterns (descending, random, etc).
/// </remarks>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ClusterMapTests {

	public enum ClusterMapImplementationType {
		InMemory,
		StreamMappedNoCache,
		StreamMappedCached
	}

	private IDisposable Create(ClusterMapImplementationType implementationType, int clusterSize, out ClusterMap clusterMap) {
		switch (implementationType) {
			case ClusterMapImplementationType.InMemory:
				clusterMap = new InMemoryClusterMap(new ExtendedList<Cluster>(), clusterSize);
				return new Disposables();
			case ClusterMapImplementationType.StreamMappedNoCache:
				var memoryStream = new MemoryStream();
				clusterMap = new StreamMappedClusterMap(memoryStream, 0, new ClusterSerializer(clusterSize), false, autoLoad: true);
				return new Disposables(memoryStream);
			case ClusterMapImplementationType.StreamMappedCached:
				memoryStream = new MemoryStream();
				clusterMap = new StreamMappedClusterMap(memoryStream, 0, new ClusterSerializer(clusterSize), true, autoLoad: true);
				return new Disposables(memoryStream);
			default:
				throw new ArgumentOutOfRangeException(nameof(implementationType), implementationType, null);
		}
	}

	[Test]
	public void EmptyOnActivation([Values] ClusterMapImplementationType implementationType) {
		using var _ = Create(implementationType, 1, out var clusterMap);
		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(0));
	}

	[Test]
	public void ClusterDataReadsFillCallerStorageWithoutPayloadAllocations([Values] ClusterMapImplementationType implementationType) {
		const int clusterSize = 4096;
		const int readOffset = 7;
		const int payloadLength = clusterSize - readOffset;
		const int iterations = 1000;
		using var cleanup = Create(implementationType, clusterSize, out var clusterMap);
		clusterMap.NewClusterChain(2, 99);
		var source = new byte[clusterSize];
		for (var index = 0; index < source.Length; index++)
			source[index] = (byte)index;
		clusterMap.WriteClusterData(1, 0, source);
		Span<byte> destination = stackalloc byte[24];
		destination.Fill(255);
		clusterMap.ReadClusterData(1, readOffset, destination.Slice(3, 18));
		Assert.That(destination.Slice(3, 18).ToArray(), Is.EqualTo(source[7..25]));
		Assert.That(destination[..3].ToArray(), Is.All.EqualTo(255));
		Assert.That(destination[21..].ToArray(), Is.All.EqualTo(255));

		var payload = new byte[payloadLength];
		clusterMap.ReadClusterData(1, readOffset, payload.AsSpan());
		Assert.That(clusterMap.ReadClusterData(1, readOffset, payloadLength), Is.EqualTo(source[readOffset..]));
		var destinationChecksum = 0L;
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var iteration = 0; iteration < iterations; iteration++) {
			clusterMap.ReadClusterData(1, readOffset, payload.AsSpan());
			destinationChecksum += payload[0] + payload[^1];
		}
		var destinationAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
		var snapshotChecksum = 0L;
		before = GC.GetAllocatedBytesForCurrentThread();
		for (var iteration = 0; iteration < iterations; iteration++) {
			var snapshot = clusterMap.ReadClusterData(1, readOffset, payloadLength);
			snapshotChecksum += snapshot[0] + snapshot[^1];
		}
		var snapshotAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(destinationAllocated, Is.LessThan(iterations * (long)payloadLength / 2), "Destination reads may allocate backend metadata, but must not allocate a payload snapshot per read, including in a shared helper.");
		TestContext.WriteLine($"{iterations} cluster data reads ({implementationType}, {payloadLength} payload bytes/read): caller storage {destinationAllocated:N0} B, snapshots {snapshotAllocated:N0} B, saved {snapshotAllocated - destinationAllocated:N0} B.");
		Assert.That(payload, Is.EqualTo(source[readOffset..]));
		Assert.That(destinationChecksum, Is.EqualTo(iterations * (long)(source[readOffset] + source[^1])));
		Assert.That(snapshotChecksum, Is.EqualTo(destinationChecksum));
		Assert.That(snapshotAllocated - destinationAllocated, Is.GreaterThanOrEqualTo(iterations * (long)payloadLength),
			"Caller-owned reads must save a payload allocation per read; the same backend's metadata allocations remain visible in both measurements.");
	}

	[Test]
	public void StreamMappedDataSnapshotsRetainShortResultsWhileDestinationReadsRejectTruncation([Values] bool enableCache) {
		using var stream = new TruncatedReadStream();
		var map = new StreamMappedClusterMap(stream, 0, new ClusterSerializer(32), enableCache, autoLoad: true);
		map.NewClusterChain(1, 99);
		map.WriteClusterData(0, 0, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
		stream.ReadLimit = ClusterSerializer.DataOffset + 5;
		Assert.That(map.ReadClusterData(0, 0, 8), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
		var destination = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 };
		Assert.That(() => map.ReadClusterData(0, 0, destination.AsSpan()), Throws.TypeOf<EndOfStreamException>());
		Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 255, 255, 255 }));
	}

	[Test]
	public void TruncatedPrimitiveMetadataReadFailsBeforeUsingIncompleteStackBytes([Values] bool enableCache) {
		using var stream = new TruncatedReadStream();
		var map = new StreamMappedClusterMap(stream, 0, new ClusterSerializer(32), enableCache, autoLoad: true);
		map.NewClusterChain(1, 99);
		var reopened = new StreamMappedClusterMap(stream, 0, new ClusterSerializer(32), enableCache, autoLoad: true);
		stream.ReadLimit = 11;
		Assert.That(() => reopened.ReadClusterNext(0), Throws.TypeOf<EndOfStreamException>());
	}

	[Test]
	public void StreamMappedNextLinksAndTerminalsRetainTheirFullWidth(
		[Values] Endianness endianness,
		[Values] bool enableCache,
		[Values(99L, 0x0000000200000001L, -1L)] long terminal
	) {
		using var stream = new MemoryStream();
		var map = new StreamMappedClusterMap(stream, 0, new ClusterSerializer(32), enableCache, endianness, autoLoad: true);
		map.NewClusterChain(2, terminal);
		map.AppendClustersToEnd(1, 1);
		Assert.That(map.ReadClusterNext(0), Is.EqualTo(1));
		Assert.That(map.ReadClusterNext(1), Is.EqualTo(2));
		Assert.That(map.ReadClusterNext(2), Is.EqualTo(terminal));
		var reopened = new StreamMappedClusterMap(stream, 0, new ClusterSerializer(32), enableCache, endianness, autoLoad: true);
		Assert.That(reopened.ReadClusterNext(0), Is.EqualTo(1));
		Assert.That(reopened.ReadClusterNext(1), Is.EqualTo(2));
		Assert.That(reopened.ReadClusterNext(2), Is.EqualTo(terminal));
		Assert.That(reopened.ReadClusterPrev(2), Is.EqualTo(1));
	}


	[Test]
	public void SingleCluster([Values] ClusterMapImplementationType implementationType) {
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(1, 99);
		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(1));
		var cluster = clusterMap.Clusters[0];
		Assert.That(cluster.Traits, Is.EqualTo(ClusterTraits.Start | ClusterTraits.End));
		Assert.That(cluster.Prev, Is.EqualTo(99));
		Assert.That(cluster.Next, Is.EqualTo(99));
	}

	[Test]
	public void TwoClusters([Values] ClusterMapImplementationType implementationType) {
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(2, 99);
		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(2));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start));
		Assert.That(cluster1.Prev, Is.EqualTo(99));
		Assert.That(cluster1.Next, Is.EqualTo(1));

		var cluster2 = clusterMap.Clusters[1];
		Assert.That(cluster2.Traits, Is.EqualTo(ClusterTraits.End));
		Assert.That(cluster2.Prev, Is.EqualTo(0));
		Assert.That(cluster2.Next, Is.EqualTo(99));

	}

	[Test]
	public void TwoClusters_ViaAppend([Values] ClusterMapImplementationType implementationType) {
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(1, 99);
		clusterMap.AppendClustersToEnd(0, 1);
		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(2));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start));
		Assert.That(cluster1.Prev, Is.EqualTo(99));
		Assert.That(cluster1.Next, Is.EqualTo(1));

		var cluster2 = clusterMap.Clusters[1];
		Assert.That(cluster2.Traits, Is.EqualTo(ClusterTraits.End));
		Assert.That(cluster2.Prev, Is.EqualTo(0));
		Assert.That(cluster2.Next, Is.EqualTo(99));

	}


	[Test]
	public void ThreeClusters_ExpandMiddle([Values] ClusterMapImplementationType implementationType) {
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(1, 99);
		clusterMap.NewClusterChain(1, 88);
		clusterMap.AppendClustersToEnd(0, 1);
		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(3));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start));
		Assert.That(cluster1.Prev, Is.EqualTo(99));
		Assert.That(cluster1.Next, Is.EqualTo(2));

		var cluster2 = clusterMap.Clusters[1];
		Assert.That(cluster2.Traits, Is.EqualTo(ClusterTraits.Start | ClusterTraits.End));
		Assert.That(cluster2.Prev, Is.EqualTo(88));
		Assert.That(cluster2.Next, Is.EqualTo(88));

		var cluster3 = clusterMap.Clusters[2];
		Assert.That(cluster3.Traits, Is.EqualTo(ClusterTraits.End));
		Assert.That(cluster3.Prev, Is.EqualTo(0));
		Assert.That(cluster3.Next, Is.EqualTo(99));

	}

	[Test]
	public void FourClusters_ExpandMiddle([Values] ClusterMapImplementationType implementationType) {
		// Chain 1: [0]		then	[0,2]
		// Chain 2: [1]		then	[1,3]
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(1, 99);
		clusterMap.NewClusterChain(1, 88);
		clusterMap.AppendClustersToEnd(0, 1);
		clusterMap.AppendClustersToEnd(1, 1);
		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(4));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start));
		Assert.That(cluster1.Prev, Is.EqualTo(99));
		Assert.That(cluster1.Next, Is.EqualTo(2));

		var cluster2 = clusterMap.Clusters[1];
		Assert.That(cluster2.Traits, Is.EqualTo(ClusterTraits.Start));
		Assert.That(cluster2.Prev, Is.EqualTo(88));
		Assert.That(cluster2.Next, Is.EqualTo(3));

		var cluster3 = clusterMap.Clusters[2];
		Assert.That(cluster3.Traits, Is.EqualTo(ClusterTraits.End));
		Assert.That(cluster3.Prev, Is.EqualTo(0));
		Assert.That(cluster3.Next, Is.EqualTo(99));

		var cluster4 = clusterMap.Clusters[3];
		Assert.That(cluster4.Traits, Is.EqualTo(ClusterTraits.End));
		Assert.That(cluster4.Prev, Is.EqualTo(1));
		Assert.That(cluster4.Next, Is.EqualTo(88));


	}


	[Test]
	public void RemoveAll_1([Values] ClusterMapImplementationType implementationType) {
		// chain 1 = [0]
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(1, 99);
		clusterMap.RemoveBackwards(0, 1);
		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(0));
	}

	[Test]
	public void RemoveFullChainEnd_1([Values] ClusterMapImplementationType implementationType) {
		// chain 1 = [0,1]
		// chain 2 = [2]
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(2, 99);
		clusterMap.NewClusterChain(1, 88);
		clusterMap.RemoveBackwards(1, 2);
		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(1));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start | ClusterTraits.End));
		Assert.That(cluster1.Prev, Is.EqualTo(88));
		Assert.That(cluster1.Next, Is.EqualTo(88));
	}


	[Test]
	public void RemoveFullChainEnd_2([Values] ClusterMapImplementationType implementationType) {
		// chain 1 = [0,2]
		// chain 2 = [1]
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(1, 99);
		clusterMap.NewClusterChain(1, 88);
		clusterMap.AppendClustersToEnd(0, 1);
		clusterMap.RemoveBackwards(2, 2);
		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(1));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start | ClusterTraits.End));
		Assert.That(cluster1.Prev, Is.EqualTo(88));
		Assert.That(cluster1.Next, Is.EqualTo(88));
	}


	[Test]
	public void RemoveFullChainEnd_2_ExtraQuantityNoEffect([Values] ClusterMapImplementationType implementationType) {
		// chain 1 = [0,2]
		// chain 2 = [1]
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(1, 99);
		clusterMap.NewClusterChain(1, 88);
		clusterMap.AppendClustersToEnd(0, 1);
		clusterMap.RemoveBackwards(2, 2 + 1);   // +1 has no effect
		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(1));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start | ClusterTraits.End));
		Assert.That(cluster1.Prev, Is.EqualTo(88));
		Assert.That(cluster1.Next, Is.EqualTo(88));
	}

	[Test]
	public void RemoveFullChainEnd_3([Values] ClusterMapImplementationType implementationType) {
		// chain 1 = [0]
		// chain 2 = [1,2]
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(1, 99);
		clusterMap.NewClusterChain(2, 88);
		clusterMap.RemoveBackwards(2, 2);
		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(1));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start | ClusterTraits.End));
		Assert.That(cluster1.Prev, Is.EqualTo(99));
		Assert.That(cluster1.Next, Is.EqualTo(99));
	}

	[Test]
	public void RemoveFromMiddle_1([Values] ClusterMapImplementationType implementationType) {
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(2, 99); // chain 1 = [0,1]
		clusterMap.NewClusterChain(1, 88); // chain 2 = [2]
		clusterMap.RemoveNextClusters(1, 1);  // chain 1 = [0], chain 2 = [1]

		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(2));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start | ClusterTraits.End));
		Assert.That(cluster1.Prev, Is.EqualTo(99));
		Assert.That(cluster1.Next, Is.EqualTo(99));

		var cluster2 = clusterMap.Clusters[1];
		Assert.That(cluster2.Traits, Is.EqualTo(ClusterTraits.Start | ClusterTraits.End));
		Assert.That(cluster2.Prev, Is.EqualTo(88));
		Assert.That(cluster2.Next, Is.EqualTo(88));
	}

	[Test]
	public void RemoveFromMiddle_2([Values] ClusterMapImplementationType implementationType) {
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(1, 99); // chain 1 = [0]
		clusterMap.NewClusterChain(2, 88); // chain 2 = [1,2]
		clusterMap.RemoveNextClusters(1, 1);  // chain 1 = [0], chain 2 = [1]

		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(2));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start | ClusterTraits.End));
		Assert.That(cluster1.Prev, Is.EqualTo(99));
		Assert.That(cluster1.Next, Is.EqualTo(99));

		var cluster2 = clusterMap.Clusters[1];
		Assert.That(cluster2.Traits, Is.EqualTo(ClusterTraits.Start | ClusterTraits.End));
		Assert.That(cluster2.Prev, Is.EqualTo(88));
		Assert.That(cluster2.Next, Is.EqualTo(88));
	}

	[Test]
	public void RemoveFromMiddle_3([Values] ClusterMapImplementationType implementationType) {
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(1, 99); // chain 1 = [0]
		clusterMap.NewClusterChain(1, 88); // chain 2 = [1]
		clusterMap.AppendClustersToEnd(0, 1);  // chain 1 = [0, 2], chain 2 = [1]
		clusterMap.RemoveNextClusters(1, 1);  // chain 1 = [0, 1]

		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(2));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start));
		Assert.That(cluster1.Prev, Is.EqualTo(99));
		Assert.That(cluster1.Next, Is.EqualTo(1));

		var cluster2 = clusterMap.Clusters[1];
		Assert.That(cluster2.Traits, Is.EqualTo(ClusterTraits.End));
		Assert.That(cluster2.Prev, Is.EqualTo(0));
		Assert.That(cluster2.Next, Is.EqualTo(99));
	}


	[Test]
	public void RemoveFromMiddle_4([Values] ClusterMapImplementationType implementationType) {
		// chain 1 - [0,1,2]
		// chain 2 - [3,4,5]
		// chain 3 - [6,7,8]

		// delete chain 2

		// chain1 = [0,1,2]
		// chain3 = [3,4,5]
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(3, 99); // chain 1 = [0,1,2]
		clusterMap.NewClusterChain(3, 88); // chain 2 = [3,4,5]
		clusterMap.NewClusterChain(3, 77); // chain 3 = [6,7,8]
		clusterMap.RemoveNextClusters(3, 3);

		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(6));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start));
		Assert.That(cluster1.Prev, Is.EqualTo(99));
		Assert.That(cluster1.Next, Is.EqualTo(1));

		var cluster2 = clusterMap.Clusters[1];
		Assert.That(cluster2.Traits, Is.EqualTo(ClusterTraits.None));
		Assert.That(cluster2.Prev, Is.EqualTo(0));
		Assert.That(cluster2.Next, Is.EqualTo(2));

		var cluster3 = clusterMap.Clusters[2];
		Assert.That(cluster3.Traits, Is.EqualTo(ClusterTraits.End));
		Assert.That(cluster3.Prev, Is.EqualTo(1));
		Assert.That(cluster3.Next, Is.EqualTo(99));

		var cluster4 = clusterMap.Clusters[3];
		Assert.That(cluster4.Traits, Is.EqualTo(ClusterTraits.Start));
		Assert.That(cluster4.Prev, Is.EqualTo(77));
		Assert.That(cluster4.Next, Is.EqualTo(4));

		var cluster5 = clusterMap.Clusters[4];
		Assert.That(cluster5.Traits, Is.EqualTo(ClusterTraits.None));
		Assert.That(cluster5.Prev, Is.EqualTo(3));
		Assert.That(cluster5.Next, Is.EqualTo(5));

		var cluster6 = clusterMap.Clusters[5];
		Assert.That(cluster6.Traits, Is.EqualTo(ClusterTraits.End));
		Assert.That(cluster6.Prev, Is.EqualTo(4));
		Assert.That(cluster6.Next, Is.EqualTo(77));
	}


	[Test]
	public void RemoveInterspersed([Values] ClusterMapImplementationType implementationType) {
		// Chain 1: [0,2]		-> [0, 1]
		// Chain 2: [1,3]		-> removed
		using var _ = Create(implementationType, 1, out var clusterMap);
		clusterMap.NewClusterChain(1, 99);
		clusterMap.NewClusterChain(1, 88);
		clusterMap.AppendClustersToEnd(0, 1);
		clusterMap.AppendClustersToEnd(1, 1);
		clusterMap.RemoveNextClusters(1, 2);

		Assert.That(clusterMap.Clusters.Count, Is.EqualTo(2));
		var cluster1 = clusterMap.Clusters[0];
		Assert.That(cluster1.Traits, Is.EqualTo(ClusterTraits.Start));
		Assert.That(cluster1.Prev, Is.EqualTo(99));
		Assert.That(cluster1.Next, Is.EqualTo(1));

		var cluster2 = clusterMap.Clusters[1];
		Assert.That(cluster2.Traits, Is.EqualTo(ClusterTraits.End));
		Assert.That(cluster2.Prev, Is.EqualTo(0));
		Assert.That(cluster2.Next, Is.EqualTo(99));

	}

	private sealed class TruncatedReadStream : MemoryStream {
		public long ReadLimit { get; set; } = long.MaxValue;

		public override int Read(byte[] buffer, int offset, int count)
			=> base.Read(buffer, offset, (int)Math.Max(0, Math.Min(count, ReadLimit - Position)));

		public override int Read(Span<byte> buffer)
			=> base.Read(buffer[..(int)Math.Max(0, Math.Min(buffer.Length, ReadLimit - Position))]);
	}
}

