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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Sphere10.Framework;

public sealed class StreamMappedClusterMap : ClusterMap, ILoadable {

	public event EventHandlerEx<object> Loading { add => _clusters.InternalCollection.Loading += value; remove => _clusters.InternalCollection.Loading -= value; }
	public event EventHandlerEx<object> Loaded { add => _clusters.InternalCollection.Loaded += value; remove => _clusters.InternalCollection.Loaded -= value; }

	private readonly SynchronizedExtendedList<Cluster, StreamPagedList<Cluster>> _clusters;
	private readonly IDictionary<long, ClusterHeader> _clusterCache;
	private readonly bool _enableCache;

	private record ClusterHeader(ClusterTraits Traits, long Prev , long Next);

	public StreamMappedClusterMap(Stream rootStream, long offset, ClusterSerializer clusterSerializer, bool enableCache, Endianness endianness = Sphere10FrameworkDefaults.Endianness, bool autoLoad = false) {
		_clusters = new StreamPagedList<Cluster>(
			clusterSerializer,
			rootStream
				.AsBounded(offset, long.MaxValue - offset, useRelativeOffset: true, allowInnerResize: true)
				.AsNonClosing(),
			endianness,
			includeListHeader: false,
			autoLoad: autoLoad
		).AsSynchronized<Cluster, StreamPagedList<Cluster>>();
		ClusterSize = clusterSerializer.ClusterDataSize;
		ZeroClusterBytes = new byte[clusterSerializer.ClusterDataSize];
		_enableCache = enableCache;
		_clusterCache = new Dictionary<long, ClusterHeader>();
			
	}

	public bool RequiresLoad => _clusters.InternalCollection.RequiresLoad;

	public void Load() => _clusters.InternalCollection.Load();

	public Task LoadAsync() => _clusters.InternalCollection.LoadAsync();

	public override int ClusterSize { get; }
	
	public override byte[] ZeroClusterBytes { get; }

	public override IReadOnlyExtendedList<Cluster> Clusters => _clusters;


	internal override long ClusterCount => _clusters.Count;

	internal override void AddCluster(Cluster cluster) {
		_clusters.Add(cluster);
		
		if (_enableCache)
			_clusterCache[_clusters.Count - 1] = new ClusterHeader(cluster.Traits, cluster.Prev, cluster.Next);
	}

	internal override Cluster GetCluster(long index) {
		var cluster = _clusters[index];

		if (_enableCache)
			_clusterCache[index] = new ClusterHeader(cluster.Traits, cluster.Prev, cluster.Next);
		return cluster;
	}

	internal override void UpdateCluster(long index, Cluster cluster) {
		if (_enableCache)
			_clusterCache.Remove(index);

		_clusters.Update(index, cluster);

		if (_enableCache)
			_clusterCache[index] = new ClusterHeader(cluster.Traits, cluster.Prev, cluster.Next);
	}

	internal override void RemoveEndClusters(long quantity) {
		if (_enableCache)
			Tools.Collection.RangeL(ClusterCount - quantity, ClusterCount).ForEach(x => _clusterCache.Remove(x));

		_clusters.RemoveRange(ClusterCount - quantity, quantity);  // Migrate included removals in @event
	}

	internal override void ClearClusters() {
		if (_enableCache)
			_clusterCache.Clear();

		_clusters.Clear();
	}

	internal override ClusterTraits ReadClusterTraits(long cluster) {
		if (_enableCache) 
			return GetCachedClusterHeader(cluster).Traits;

		Span<byte> bytes = stackalloc byte[ClusterSerializer.TraitsLength];
		ReadItemBytes(cluster, ClusterSerializer.TraitsOffset, bytes);
		var traits = (ClusterTraits)bytes[0];
		return traits;
	}

	internal override void WriteClusterTraitsInternal(long clusterIndex, ClusterTraits traits) {
		if (_enableCache)
			_clusterCache.Remove(clusterIndex);

		ReadOnlySpan<byte> bytes = stackalloc byte[] { (byte)traits };
		_clusters.InternalCollection.WriteItemBytes(clusterIndex, ClusterSerializer.TraitsOffset, bytes);
	}

	internal override void MaskClusterTraitsInternal(long clusterIndex, ClusterTraits traits, bool on) {
		if (_enableCache)
			_clusterCache.Remove(clusterIndex);

		Span<byte> traitBytes = stackalloc byte[ClusterSerializer.TraitsLength];
		ReadItemBytes(clusterIndex, ClusterSerializer.TraitsOffset, traitBytes);
		var newTraits = ((ClusterTraits)traitBytes[0]).CopyAndSetFlags(traits, on);
		traitBytes[0] = (byte)newTraits;
		_clusters.InternalCollection.WriteItemBytes(clusterIndex, ClusterSerializer.TraitsOffset, traitBytes);
		if (_enableCache)
			_clusterCache.Remove(clusterIndex);
	}

	internal override long ReadClusterPrev(long cluster) {
		if (_enableCache) 
			return GetCachedClusterHeader(cluster).Prev;

		Span<byte> bytes = stackalloc byte[ClusterSerializer.PrevLength];
		ReadItemBytes(cluster, ClusterSerializer.PrevOffset, bytes);
		var prevCluster = _clusters.InternalCollection.Reader.BitConverter.ToInt64(bytes);
		return prevCluster;
	}

	internal override void WriteClusterPrevInternal(long clusterIndex, long prev) {
		if (_enableCache)
			_clusterCache.Remove(clusterIndex);

		Span<byte> bytes = stackalloc byte[sizeof(long)];
		_clusters.InternalCollection.Writer.BitConverter.WriteTo(prev, bytes);
		_clusters.InternalCollection.WriteItemBytes(clusterIndex, ClusterSerializer.PrevOffset, bytes);
	}

	internal override long ReadClusterNext(long cluster) {
		if (_enableCache) 
			return GetCachedClusterHeader(cluster).Next;

		Span<byte> bytes = stackalloc byte[ClusterSerializer.NextLength];
		ReadItemBytes(cluster, ClusterSerializer.NextOffset, bytes);
		var nextValue = _clusters.InternalCollection.Reader.BitConverter.ToInt64(bytes);
		return nextValue;
	}

	internal override void WriteClusterNextInternal(long clusterIndex, long next) {
		if (_enableCache)
			_clusterCache.Remove(clusterIndex);

		Span<byte> bytes = stackalloc byte[sizeof(long)];
		_clusters.InternalCollection.Writer.BitConverter.WriteTo(next, bytes);
		_clusters.InternalCollection.WriteItemBytes(clusterIndex, ClusterSerializer.NextOffset, bytes);
	}

	internal override byte[] ReadClusterData(long clusterIndex, long offset, long size) {
		// cluster data is not cached
		_clusters.InternalCollection.ReadItemBytes(clusterIndex, ClusterSerializer.DataOffset + offset, size, out var bytes);
		return bytes;
	}

	internal override void ReadClusterData(long clusterIndex, long offset, Span<byte> destination)
		=> ReadItemBytes(clusterIndex, ClusterSerializer.DataOffset + offset, destination);

	internal override void WriteClusterDataInternal(long clusterIndex, long offset, ReadOnlySpan<byte> data) {
		// cluster data is not cached

		Guard.Argument(data.Length <= ClusterSize, nameof(data), "Data length exceeds cluster data length");
		_clusters.InternalCollection.WriteItemBytes(clusterIndex, ClusterSerializer.DataOffset + offset, data);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ClusterHeader GetCachedClusterHeader(long index) {
		if (!_clusterCache.TryGetValue(index, out var header)) {
			Span<byte> bytes = stackalloc byte[ClusterSerializer.TraitsLength + ClusterSerializer.PrevLength + ClusterSerializer.NextLength];
			ReadItemBytes(index, 0, bytes);
			header = new ClusterHeader(
				(ClusterTraits)bytes[0],
				_clusters.InternalCollection.Reader.BitConverter.ToInt64(bytes, (int)ClusterSerializer.PrevOffset),
				_clusters.InternalCollection.Reader.BitConverter.ToInt64(bytes, (int)ClusterSerializer.NextOffset)
			);
			_clusterCache[index] = header;
		}
		return header;
	}

	private void ReadItemBytes(long index, long byteOffset, Span<byte> destination) {
		var bytesRead = _clusters.InternalCollection.ReadItemBytes(index, byteOffset, destination);
		if (bytesRead != destination.Length)
			throw new EndOfStreamException("Cluster storage ended before the requested bytes were read.");
	}

	
	
}


