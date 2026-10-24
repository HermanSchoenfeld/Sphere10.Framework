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
using System.Text.Json;

namespace Sphere10.Framework.Utils.Profiler.BTrees;

public sealed class BTreeBenchmarkFixture : IDisposable {
	private readonly BTreeProfilerSettings _settings;
	private readonly string _workspaceId;
	private readonly string _workspace;
	private readonly string _path;
	private readonly int[] _insertion;
	private readonly int[] _queries;
	private readonly long[] _values;
	private readonly long _mappedCapacity;
	private readonly object _keys;
	private IBTreeBenchmarkIndex _index;
	private Stream _stream;
	private bool _disposed;
	private bool _mixedPerformed;
	private long _mixedResult;

	public BTreeBenchmarkFixture(string profile)
		: this(BTreeProfilerSettings.Current, BTreeProfilerSettings.Current.GetProfile(profile)) {
	}

	public BTreeBenchmarkFixture(BTreeProfilerSettings settings, BTreeProfile profile) {
		settings.Validate();
		profile.Validate();
		_settings = settings;
		Profile = profile;
		_workspaceId = Guid.NewGuid().ToString("N");
		_workspace = Path.Combine(settings.DataDirectory, settings.RunId, $"{profile.Name}-{_workspaceId}");
		Directory.CreateDirectory(_workspace);
		_path = Path.Combine(_workspace, "tree.bin");
		_insertion = Enumerable.Range(0, settings.RecordCount).ToArray();
		if (settings.ShuffledInsertion)
			Shuffle(_insertion, new Random(713));
		_queries = Enumerable.Range(0, settings.RecordCount).ToArray();
		Shuffle(_queries, new Random(917));
		_queries = _queries.Take(settings.OperationCount).ToArray();
		_values = new long[settings.RecordCount];
		// Bound provisioned capacity by legal minimum occupancy, with headroom for split/recycle work.
		// At large orders this avoids allocating gigabytes merely to benchmark a small tree.
		var minimumEntries = Math.Max(1, (profile.Order + 1) / 2 - 1);
		_mappedCapacity = checked(32L + (settings.RecordCount * 2L / minimumEntries + 32) * profile.NodeRecordBytes);
		var keyCount = checked(settings.RecordCount * 2 + 1);
		_keys = profile.KeyShape switch {
			BTreeKeyShape.Int32 => Enumerable.Range(0, keyCount).ToArray(),
			BTreeKeyShape.Int64 => Enumerable.Range(0, keyCount).Select(key => (long)key).ToArray(),
			_ => Enumerable.Range(0, keyCount).Select(CreateDigest).ToArray()
		};
		try {
			OpenIndex();
		} catch {
			CloseIndex();
			Directory.Delete(_workspace, true);
			throw;
		}
	}

	public BTreeProfile Profile { get; }

	public int Count => _index.Count;

	public long LogicalBytes => _stream?.Length ?? 0;

	public string Workspace => _workspace;

	public bool IsTransactional => Profile.Storage == BTreeStorage.TransactionalFile;

	public void PrepareBaseline(bool warm = false) {
		ResetEmpty();
		Insert();
		Persist();
		Verify();
		WriteMetadata();
		if (warm)
			WarmLookup();
	}

	public void ResetEmpty() {
		CloseIndex();
		if (File.Exists(_path))
			File.Delete(_path);
		Array.Clear(_values);
		_mixedPerformed = false;
		OpenIndex();
	}

	public long Insert() {
		foreach (var identity in _insertion) {
			var value = InitialValue(identity);
			_index.Add(Key(identity), value);
			_values[identity] = value;
		}
		return _index.Count;
	}

	public long WarmLookup() {
		var result = 0L;
		foreach (var identity in _queries) {
			var value = ReadIdentity(identity);
			result += value;
		}
		return result;
	}

	public int MissingLookup() {
		var misses = 0;
		foreach (var identity in _queries)
			if (!_index.TryGet(_settings.RecordCount + identity, out _))
				misses++;
		return misses;
	}

	public long Scan() => _index.Scan();

	public long RangeLookup() {
		var result = 0L;
		foreach (var identity in _queries)
			result += _index.Range(identity, Math.Min(identity + 15, _settings.RecordCount - 1));
		return result;
	}

	public long GroupLookup() {
		var result = 0L;
		foreach (var identity in _queries)
			result += _index.Group(Key(identity));
		return result;
	}

	public long Update() {
		_mixedPerformed = false;
		foreach (var identity in _queries)
			Replace(identity, _values[identity] + 1);
		return _index.Count;
	}

	public long DeleteAndRecycle() {
		_mixedPerformed = false;
		foreach (var identity in _queries)
			_index.Remove(Key(identity), _values[identity]);
		foreach (var identity in _queries)
			_index.Add(Key(identity), _values[identity]);
		return _index.Count;
	}

	public long Mixed() {
		var reads = _settings.Mix switch { "read-heavy" => 90, "write-heavy" => 10, _ => 50 };
		var updates = _settings.Mix switch { "read-heavy" => 5, "write-heavy" => 45, _ => 25 };
		var result = 0L;
		for (var index = 0; index < _queries.Length; index++) {
			var identity = _queries[index];
			var kind = index * 37 % 100;
			if (kind < reads) {
				var value = ReadIdentity(identity);
				result += value;
			} else if (kind < reads + updates) {
				Replace(identity, _values[identity] + 1);
			} else {
				_index.Remove(Key(identity), _values[identity]);
				_index.Add(Key(identity), _values[identity]);
			}
		}
		_mixedPerformed = true;
		return _mixedResult = result;
	}

	public long MixedAndPersist() {
		var result = Mixed();
		Persist();
		return result;
	}

	public void Persist() {
		if (_stream is TransactionalStream transaction)
			transaction.Commit();
		else
			_stream?.Flush();
	}

	public void RollbackPreparedChanges() {
		Guard.Ensure(_stream is TransactionalStream, "Rollback requires transactional file storage.");
		// Dispose the tree before rollback: its write-through header cache describes the uncommitted tree.
		_index.Dispose();
		_index = null;
		((TransactionalStream)_stream).Rollback();
		CreateIndex();
		_mixedPerformed = false;
		for (var identity = 0; identity < _values.Length; identity++)
			_values[identity] = InitialValue(identity);
	}

	public long ReopenAndLookup() {
		Guard.Ensure(Profile.Storage is not (BTreeStorage.ManagedNodes or BTreeStorage.StreamMemory), "Reopening requires persisted file storage.");
		CloseIndex();
		OpenIndex();
		return WarmLookup();
	}

	public void Verify() {
		Guard.Ensure(_index.Validate(out var error), error);
		Guard.Ensure(_index.Count == _settings.RecordCount, "Unexpected entry count.");
		foreach (var identity in Enumerable.Range(0, _settings.RecordCount))
			Guard.Ensure(_index.Contains(Key(identity), _values[identity]), $"Missing or incorrect entry {identity}.");
		Guard.Ensure(_index.Scan() == _values.Sum(), "Full scan checksum differs from the oracle.");
		if (_mixedPerformed)
			VerifyMixedResult(_mixedResult);
	}

	public void VerifyMixedResult(long result) {
		var reads = _settings.Mix switch { "read-heavy" => 90, "write-heavy" => 10, _ => 50 };
		var expected = _queries.Where((identity, index) => index * 37 % 100 < reads).Sum(identity => _values[identity]);
		Guard.Ensure(result == expected, $"Mixed read checksum was {result}; expected {expected}.");
	}

	public void VerifyRead(string workload, long result) {
		var expected = workload switch {
			nameof(BTreeReadBenchmarks.MissingLookup) => _settings.OperationCount,
			nameof(BTreeReadBenchmarks.Scan) => _values.Sum(),
			nameof(BTreeReadBenchmarks.RangeLookup) => _queries.Sum(identity => _values.Skip(identity).Take(Math.Min(16, _settings.RecordCount - identity)).Sum()),
			nameof(BTreeLookupReadBenchmarks.GroupLookup) => _queries.Sum(identity => _values.Skip(Key(identity) * _settings.GroupSize).Take(_settings.GroupSize).Sum()),
			_ => _queries.Sum(identity => _values[identity])
		};
		Guard.Ensure(result == expected, $"{workload} returned {result}; expected {expected}.");
	}

	public void Dispose() {
		if (_disposed)
			return;
		CloseIndex();
		// Only this GUID-owned workspace is removed; data-root and sibling runs are untouched.
		Directory.Delete(_workspace, true);
		_disposed = true;
	}

	private int Key(int identity) => Profile.Family == BTreeFamily.Lookup ? identity / _settings.GroupSize : identity;

	private static long InitialValue(int identity) => (identity + 1L) * 17;

	private long ReadIdentity(int identity) {
		if (Profile.Family == BTreeFamily.Lookup)
			return _index.Contains(Key(identity), _values[identity]) ? _values[identity] : 0;
		_index.TryGet(Key(identity), out var value);
		return value;
	}

	private void Replace(int identity, long value) {
		if (Profile.Family == BTreeFamily.Lookup) {
			_index.Remove(Key(identity), _values[identity]);
			_index.Add(Key(identity), value);
		} else {
			_index.Set(Key(identity), value);
		}
		_values[identity] = value;
	}

	private void OpenIndex() {
		if (Profile.Storage != BTreeStorage.ManagedNodes)
			_stream = BTreeStorageStream.Open(Profile, _path, _mappedCapacity);
		CreateIndex();
	}

	private void CreateIndex() {
		_index = Profile.KeyShape switch {
			BTreeKeyShape.Int32 => new BTreeIndex<int>(Profile, (int[])_keys, PrimitiveSerializer<int>.Instance, Comparer<int>.Default, _stream),
			BTreeKeyShape.Int64 => new BTreeIndex<long>(Profile, (long[])_keys, PrimitiveSerializer<long>.Instance, Comparer<long>.Default, _stream),
			_ => new BTreeIndex<byte[]>(Profile, (byte[][])_keys, new ConstantSizeByteArraySerializer(32), ByteArrayComparer.Instance, _stream)
		};
	}

	private void CloseIndex() {
		_index?.Dispose();
		_index = null;
		_stream?.Dispose();
		_stream = null;
	}

	public void WriteMetadata() {
		var directory = Path.Combine(_settings.OutputDirectory, "storage", _settings.RunId);
		Directory.CreateDirectory(directory);
		var metadata = new {
			Profile = Profile.Name,
			RunId = _settings.RunId,
			Profile.Storage,
			Profile.Family,
			Profile.Spec,
			Profile.EffectiveType,
			Profile.KeyShape,
			MaximumLeafEntries = Profile.Order - 1,
			Profile.KeyBytes,
			Profile.StoredKeyBytes,
			NodeRecordBytes = Profile.Storage == BTreeStorage.ManagedNodes ? (int?)null : Profile.NodeRecordBytes,
			LogicalTreeBytes = LogicalBytes,
			ProvisionedMappedCapacity = Profile.Storage == BTreeStorage.OSMemoryMapped ? (long?)_mappedCapacity : null,
			EntryCount = Count,
			PhysicalStorageBytes = Directory.EnumerateFiles(_workspace, "*", SearchOption.AllDirectories).Sum(path => new FileInfo(path).Length),
			Geometry = ReadGeometry(),
			AppliedPageSize = Profile.Storage is BTreeStorage.FilePaged or BTreeStorage.TransactionalFile ? (long?)Profile.PageSize : null,
			AppliedCacheBytes = Profile.Storage is BTreeStorage.FilePaged or BTreeStorage.TransactionalFile ? (long?)Profile.CacheBytes : null,
			AppliedFileBufferBytes = Profile.Storage == BTreeStorage.FileStream ? (int?)Profile.FileBufferBytes : null
		};
		File.WriteAllText(Path.Combine(directory, _workspaceId + ".json"), JsonSerializer.Serialize(metadata, BTreeProfilerSettings.JsonOptions));
	}

	private object ReadGeometry() {
		if (_stream == null)
			return null;
		// Read only reachable nodes from the published stream layout; recycled physical slots are not active leaves.
		using var position = _stream.EnterRestoreSeekPositionScope();
		using var reader = new EndianBinaryReader(EndianBitConverter.For(Profile.Endianness), new NonClosingStream(_stream));
		_stream.Position = 0;
		var root = reader.ReadInt64();
		var plus = Profile.EffectiveType == BTreeType.BTreePlus;
		var empty = plus ? 0L : -1L;
		var queue = new Queue<(long Handle, int Depth)>();
		if (root != empty)
			queue.Enqueue((root, 1));
		var seen = new HashSet<long>();
		var leaves = 0;
		var nodes = 0;
		var leafEntries = 0L;
		var height = 0;
		while (queue.TryDequeue(out var current)) {
			Guard.Ensure(seen.Add(current.Handle), "Cycle detected in tree geometry.");
			var slot = plus ? current.Handle - 1 : current.Handle;
			var offset = 32L + slot * Profile.NodeRecordBytes;
			_stream.Position = offset;
			var isLeaf = reader.ReadByte() != 0;
			var keys = reader.ReadInt32();
			var children = reader.ReadInt32();
			nodes++;
			height = Math.Max(height, current.Depth);
			if (isLeaf) {
				leaves++;
				leafEntries += keys;
			} else {
				_stream.Position = offset + (plus ? 17 : 9) + Profile.Order * (Profile.StoredKeyBytes + sizeof(long));
				for (var index = 0; index < children; index++)
					queue.Enqueue((reader.ReadInt64(), current.Depth + 1));
			}
		}
		return new { Height = height, ActiveNodes = nodes, TotalLeaves = leaves, LeafEntries = leafEntries,
			MeanLeafOccupancy = leaves == 0 ? 0 : (double)leafEntries / (leaves * (Profile.Order - 1)), PhysicalSlots = (LogicalBytes - 32) / Profile.NodeRecordBytes };
	}

	private static byte[] CreateDigest(int identity) {
		var bytes = new byte[32];
		// Fixed-width checksum-shaped keys preserve numeric ordering for verifiable inclusive ranges.
		EndianBitConverter.Big.GetBytes((long)identity).CopyTo(bytes, 0);
		for (var index = 8; index < bytes.Length; index++)
			bytes[index] = (byte)((identity * 31L + index * 17L) % 251);
		return bytes;
	}

	private static void Shuffle(int[] values, Random random) {
		for (var index = values.Length - 1; index > 0; index--) {
			var other = random.Next(index + 1);
			(values[index], values[other]) = (values[other], values[index]);
		}
	}
}
