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

namespace Sphere10.Framework.Utils.Profiler.Caches;

public sealed class CacheWorkloadMetrics {
	public string Workload { get; set; }
	public string Profile { get; set; }
	public int OperationCount { get; set; }
	public int Capacity { get; set; }
	public double ReapLevel { get; set; }
	public long MinimumBatchRelease { get; set; }
	public int BaselineItemCount { get; set; }
	public long BaselineSize { get; set; }
	public int HotKeyCount { get; set; }
	public int FetchCount { get; set; }
	public long FetchedSize { get; set; }
	public int VictimCount { get; set; }
	public int ReapCount { get; set; }
	public long VictimSize { get; set; }
	public int FinalItemCount { get; set; }
	public long FinalSize { get; set; }
	public long AccessCount { get; set; }
	public long ResultChecksum { get; set; }
	public long MembershipChecksum { get; set; }
	public long FetchWorkChecksum { get; set; }
	public bool Verified { get; set; }
}

public sealed class CacheBenchmarkFixture : IDisposable {
	private readonly CacheProfilerSettings _settings;
	private readonly CacheValue[] _source;
	private readonly int[] _hotKeys;
	private readonly object[] _preboxedHotKeys;
	private readonly int[] _sequentialKeys;
	private readonly int[] _pressureKeys;
	private readonly int[] _fetchedKeys;
	private readonly int[] _removedKeys;
	private readonly int _baselineCount;
	private readonly string _fixtureId;
	private ActionCache<int, CacheValue> _cache;
	private ICache<int, CacheValue> _typedCache;
	private ICache _objectCache;
	private ObjectCacheProjection<int, CacheValue> _objectProjection;
	private int _fetchCount;
	private int _victimCount;
	private int _reapCount;
	private int _lastReapFetchCount;
	private long _fetchedSize;
	private long _victimSize;
	private long _fetchWorkChecksum;
	private long _baselineAccessCount;
	private string _workload;
	private long _result;
	private int _iteration;

	public CacheBenchmarkFixture(string profile)
		: this(CacheProfilerSettings.Current, CacheProfilerSettings.Current.GetProfile(profile)) {
	}

	public CacheBenchmarkFixture(CacheProfilerSettings settings, CacheProfile profile) {
		Guard.ArgumentNotNull(settings, nameof(settings));
		Guard.ArgumentNotNull(profile, nameof(profile));
		settings.Validate();
		profile.Validate();
		_settings = settings;
		Profile = profile;
		_fixtureId = Guid.NewGuid().ToString("N");
		_source = new CacheValue[checked(profile.Capacity + settings.OperationCount)];
		var remaining = profile.Capacity;
		while (remaining > 0) {
			var size = Math.Min(remaining, SizeOf(_baselineCount));
			_source[_baselineCount] = new CacheValue(_baselineCount, size);
			remaining -= size;
			_baselineCount++;
		}
		for (var key = _baselineCount; key < _source.Length; key++)
			_source[key] = new CacheValue(key, SizeOf(key));
		HotKeyCount = Math.Max(1, _baselineCount * settings.HotSetPercent / 100);
		_hotKeys = new int[settings.OperationCount];
		_preboxedHotKeys = new object[settings.OperationCount];
		_sequentialKeys = new int[settings.OperationCount];
		_pressureKeys = new int[settings.OperationCount];
		_fetchedKeys = new int[settings.OperationCount];
		_removedKeys = new int[checked(_baselineCount + settings.OperationCount)];
		var random = new Random(917);
		var nextColdKey = _baselineCount;
		for (var index = 0; index < settings.OperationCount; index++) {
			_hotKeys[index] = random.Next(HotKeyCount);
			_preboxedHotKeys[index] = _hotKeys[index];
			_sequentialKeys[index] = _baselineCount + index;
			_pressureKeys[index] = index * 37 % 100 < settings.HotAccessPercent ? _hotKeys[index] : nextColdKey++;
		}
	}

	public CacheProfile Profile { get; }

	public int HotKeyCount { get; }

	public void PrepareIteration() {
		CloseCache();
		_cache = new ActionCache<int, CacheValue>(Fetch, value => value.Size, reapStrategy: Profile.ReapPolicy,
			maxCapacity: Profile.Capacity, reapLevel: Profile.ReapLevel);
		_typedCache = _cache;
		_objectProjection = _cache.AsObjectCache();
		_objectCache = _objectProjection;
		_cache.ItemRemoved += OnItemRemoved;
		for (var key = 0; key < _baselineCount; key++)
			_cache.Set(key, _source[key]);
		Guard.Ensure(_cache.CurrentSize == Profile.Capacity && _cache.ItemCount == _baselineCount, "Cache preseed did not fill the declared estimator capacity.");
		_baselineAccessCount = _cache.TotalAccesses;
		_fetchCount = _victimCount = _reapCount = _lastReapFetchCount = 0;
		_fetchedSize = _victimSize = _fetchWorkChecksum = 0;
		_workload = null;
		_result = 0;
		_iteration++;
	}

	public long TypedHotKeys() {
		_workload = nameof(TypedHotKeys);
		var result = 0L;
		foreach (var key in _hotKeys)
			result += _typedCache.Get(key).Value.Checksum;
		return _result = result;
	}

	public long ObjectHotKeys() {
		_workload = nameof(ObjectHotKeys);
		var result = 0L;
		foreach (var key in _hotKeys)
			result += ((CacheValue)_objectCache.Get(key).Value).Checksum;
		return _result = result;
	}

	public long ObjectPreboxedHotKeys() {
		_workload = nameof(ObjectPreboxedHotKeys);
		var result = 0L;
		foreach (var key in _preboxedHotKeys)
			result += ((CacheValue)_objectCache.Get(key).Value).Checksum;
		return _result = result;
	}

	public long SequentialPressure() {
		_workload = nameof(SequentialPressure);
		return _result = AccessTyped(_sequentialKeys);
	}

	public long HotSetPressure() {
		_workload = nameof(HotSetPressure);
		return _result = AccessTyped(_pressureKeys);
	}

	public CacheWorkloadMetrics Verify() {
		Guard.Ensure(_workload != null, "Execute exactly one workload batch after preparing the cache.");
		var queries = _workload switch {
			nameof(SequentialPressure) => _sequentialKeys,
			nameof(HotSetPressure) => _pressureKeys,
			_ => _hotKeys
		};
		var oracle = Replay(queries);
		Guard.Ensure(_result == oracle.Checksum, "The cache batch returned an incorrect value checksum.");
		Guard.Ensure(_fetchCount == oracle.FetchedKeys.Length && _fetchedKeys.AsSpan(0, _fetchCount).SequenceEqual(oracle.FetchedKeys), "The cache fetch sequence differs from the independent oracle.");
		Guard.Ensure(_victimCount == oracle.RemovedKeys.Length && _removedKeys.AsSpan(0, _victimCount).SequenceEqual(oracle.RemovedKeys), "The cache eviction sequence differs from the independent policy oracle.");
		Guard.Ensure(_reapCount == oracle.ReapCount, "The number of admission reaps differs from the oracle.");
		Guard.Ensure(_fetchedSize == oracle.FetchedSize && _victimSize == oracle.VictimSize, "Fetch/eviction estimator sizes differ from the oracle.");
		Guard.Ensure(_fetchWorkChecksum == oracle.FetchWorkChecksum, "Simulated fetch work differs from the oracle.");
		var entries = _cache.CachedItems.ToArray();
		var actualKeys = entries.Select(entry => entry.Value.Key).OrderBy(key => key).ToArray();
		Guard.Ensure(actualKeys.SequenceEqual(oracle.FinalKeys), "Final cache membership differs from the oracle.");
		Guard.Ensure(entries.All(entry => ReferenceEquals(entry.Value, _source[entry.Value.Key]) && entry.Size == entry.Value.Size), "A cached value or its estimator size changed.");
		Guard.Ensure(_cache.CurrentSize == entries.Sum(entry => entry.Size) && _cache.CurrentSize == oracle.FinalSize, "CurrentSize does not reconcile with cached entries and the oracle.");
		Guard.Ensure(_cache.CurrentSize <= Profile.Capacity && _cache.ItemCount == entries.Length, "The final cache exceeds capacity or reports an incorrect item count.");
		Guard.Ensure(_cache.TotalAccesses - _baselineAccessCount == _settings.OperationCount, "The batch did not execute the declared number of cache accesses.");
		return new CacheWorkloadMetrics {
			Workload = _workload, Profile = Profile.Name, OperationCount = _settings.OperationCount,
			Capacity = Profile.Capacity, ReapLevel = Profile.ReapLevel, MinimumBatchRelease = Profile.MinimumBatchRelease,
			BaselineItemCount = _baselineCount, BaselineSize = Profile.Capacity, HotKeyCount = HotKeyCount,
			FetchCount = _fetchCount, FetchedSize = _fetchedSize, VictimCount = _victimCount, ReapCount = _reapCount, VictimSize = _victimSize,
			FinalItemCount = entries.Length, FinalSize = _cache.CurrentSize, AccessCount = _cache.TotalAccesses - _baselineAccessCount,
			ResultChecksum = _result, MembershipChecksum = entries.Sum(entry => entry.Value.Checksum), FetchWorkChecksum = _fetchWorkChecksum, Verified = true
		};
	}

	public void WriteMetrics(CacheWorkloadMetrics metrics) {
		Guard.ArgumentNotNull(metrics, nameof(metrics));
		Guard.Argument(metrics.Verified && metrics.Profile == Profile.Name && metrics.Workload == _workload, nameof(metrics), "Retain only the verified current workload metrics.");
		var directory = Path.Combine(_settings.OutputDirectory, "cache-metrics", _settings.RunId);
		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, $"{_fixtureId}-{_iteration:D5}.json");
		var metadata = new { RecordedAtUtc = DateTime.UtcNow, ProcessId = Environment.ProcessId, FixtureId = _fixtureId, Iteration = _iteration,
			Profile, Settings = new { _settings.OperationCount, _settings.HotSetPercent, _settings.HotAccessPercent, _settings.FetchWork }, Metrics = metrics };
		File.WriteAllText(path, JsonSerializer.Serialize(metadata, CacheProfilerSettings.JsonOptions));
	}

	public void Dispose() => CloseCache();

	private int SizeOf(int key) => Profile.SizePattern == CacheSizePattern.Unit ? 1 : key % 4 + 1;

	private long AccessTyped(int[] queries) {
		var result = 0L;
		foreach (var key in queries)
			result += _typedCache.Get(key).Value.Checksum;
		return result;
	}

	private CacheValue Fetch(int key) {
		_fetchedKeys[_fetchCount++] = key;
		_fetchedSize += _source[key].Size;
		_fetchWorkChecksum = unchecked(_fetchWorkChecksum + ComputeFetchWork(key));
		return _source[key];
	}

	private void OnItemRemoved(int key, CachedItem<CacheValue> item) {
		if (_lastReapFetchCount != _fetchCount) {
			_reapCount++;
			_lastReapFetchCount = _fetchCount;
		}
		_removedKeys[_victimCount++] = key;
		_victimSize += item.Size;
	}

	private long ComputeFetchWork(int key) {
		var result = (long)key;
		for (var index = 0; index < _settings.FetchWork; index++)
			result = unchecked(result * 1664525 + 1013904223);
		return result;
	}

	private OracleResult Replay(int[] queries) {
		// A linked-list reference model is deliberately independent of the cache's candidate selection implementation.
		var order = new LinkedList<int>();
		var nodes = new Dictionary<int, LinkedListNode<int>>();
		var fetched = new List<int>();
		var removed = new List<int>();
		var currentSize = (long)Profile.Capacity;
		var result = new OracleResult();
		for (var key = 0; key < _baselineCount; key++)
			nodes.Add(key, order.AddLast(key));
		foreach (var key in queries) {
			if (!nodes.TryGetValue(key, out var node)) {
				var incoming = _source[key].Size;
				if (currentSize + incoming > Profile.Capacity) {
					result.ReapCount++;
					var release = Math.Max(currentSize + incoming - Profile.Capacity, Profile.MinimumBatchRelease);
					var released = 0L;
					while (released < release) {
						var victim = order.First.Value;
						order.RemoveFirst();
						nodes.Remove(victim);
						removed.Add(victim);
						var size = _source[victim].Size;
						released += size;
						currentSize -= size;
						result.VictimSize += size;
					}
				}
				fetched.Add(key);
				result.FetchedSize += incoming;
				result.FetchWorkChecksum = unchecked(result.FetchWorkChecksum + ComputeFetchWork(key));
				currentSize += incoming;
				nodes.Add(key, node = order.AddLast(key));
			} else if (Profile.ReapPolicy == CacheReapPolicy.LongestIdle) {
				order.Remove(node);
				order.AddLast(node);
			}
			result.Checksum += _source[key].Checksum;
		}
		result.FetchedKeys = fetched.ToArray();
		result.RemovedKeys = removed.ToArray();
		result.FinalKeys = nodes.Keys.OrderBy(key => key).ToArray();
		result.FinalSize = currentSize;
		return result;
	}

	private void CloseCache() {
		if (_cache == null)
			return;
		_objectProjection.Dispose();
		_objectProjection = null;
		_cache.ItemRemoved -= OnItemRemoved;
		_cache.Dispose();
		_cache = null;
		_typedCache = null;
		_objectCache = null;
	}

	private sealed class CacheValue {
		public CacheValue(int key, int size) {
			Key = key;
			Size = size;
			Checksum = key * 17L + 3;
		}

		public int Key { get; }
		public int Size { get; }
		public long Checksum { get; }
	}

	private sealed class OracleResult {
		public long Checksum;
		public long FinalSize;
		public long FetchedSize;
		public long VictimSize;
		public long FetchWorkChecksum;
		public int ReapCount;
		public int[] FetchedKeys;
		public int[] RemovedKeys;
		public int[] FinalKeys;
	}
}
