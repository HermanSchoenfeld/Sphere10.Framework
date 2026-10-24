// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>Typed cache engine with synchronized admission, expiration and capacity management.</summary>
public abstract class CacheBase<TKey, TValue> : SynchronizedObject, ICache<TKey, TValue>, IDisposable {
	public event EventHandlerEx<TKey> ItemFetching {
		add => AddTypedHandler(0, value);
		remove => RemoveTypedHandler(0, value);
	}

	public event EventHandlerEx<TKey, TValue> ItemFetched {
		add => AddTypedHandler(1, value);
		remove => RemoveTypedHandler(1, value);
	}

	public event EventHandlerEx<TKey, CachedItem<TValue>> ItemRemoving {
		add => AddTypedHandler(3, value);
		remove => RemoveTypedHandler(3, value);
	}

	public event EventHandlerEx<TKey, CachedItem<TValue>> ItemRemoved {
		add => AddTypedHandler(2, value);
		remove => RemoveTypedHandler(2, value);
	}

	private EventHandlerEx<TKey> _itemFetching;
	private EventHandlerEx<TKey, TValue> _itemFetched;
	private EventHandlerEx<TKey, CachedItem<TValue>> _itemRemoving;
	private EventHandlerEx<TKey, CachedItem<TValue>> _itemRemoved;
	private readonly Dictionary<(int Event, Delegate Handler), (Delegate Projection, int Count)> _variantHandlers = new();
	private readonly Dictionary<CachedItem<TValue>, int> _protectedItems = new(ReferenceEqualityComparer.Instance);
	private long _order;
	private int _removalDepth;
	private bool _disposed;

	internal readonly IDictionary<TKey, CachedItem<TValue>> InternalStorage;

	protected CacheBase(
		CacheReapPolicy reapStrategy = CacheReapPolicy.None,
		ExpirationPolicy expirationStrategy = ExpirationPolicy.None,
		long maxCapacity = int.MaxValue,
		TimeSpan? expirationDuration = null,
		NullValuePolicy nullValuePolicy = NullValuePolicy.CacheNormally,
		StaleValuePolicy staleValuePolicy = StaleValuePolicy.AssumeNeverStale,
		IEqualityComparer<TKey> keyComparer = null,
		ICacheReaper reaper = null,
		double reapLevel = Sphere10FrameworkDefaults.CacheReapLevel
	) {
		Guard.Argument(maxCapacity >= 0, nameof(maxCapacity), "Capacity must be nonnegative.");
		Guard.Argument(reapLevel > 0 && reapLevel < 1, nameof(reapLevel), "Reap level must be greater than zero and less than one.");
		Guard.Argument(!expirationDuration.HasValue || expirationDuration.Value >= TimeSpan.Zero, nameof(expirationDuration), "Expiration duration must be nonnegative.");
		InternalStorage = new Dictionary<TKey, CachedItem<TValue>>(keyComparer ?? EqualityComparer<TKey>.Default);
		MaxCapacity = maxCapacity;
		ReapLevel = reapLevel;
		ReapPolicy = reapStrategy;
		ExpirationPolicy = expirationStrategy;
		ExpirationDuration = expirationDuration ?? TimeSpan.MaxValue;
		NullValuePolicy = nullValuePolicy;
		StaleValuePolicy = staleValuePolicy;
		LastUpdateOn = LastAccessedOn = DateTime.Now;
		Reaper = reaper ?? new IsolatedCacheReaper();
		Reaper.Register(this);
	}

	public IEnumerable<CachedItem<TValue>> CachedItems {
		get { using (EnterReadScope()) return InternalStorage.Values.ToArray(); }
	}

	public int ItemCount {
		get { using (EnterReadScope()) return InternalStorage.Count; }
	}

	public long CurrentSize { get; internal set; }

	public long MaxCapacity { get; internal set; }

	public double ReapLevel { get; }

	public DateTime LastUpdateOn { get; internal set; }

	public DateTime LastAccessedOn { get; internal set; }

	public long TotalAccesses { get; internal set; }

	public TimeSpan ExpirationDuration { get; internal set; }

	public CacheReapPolicy ReapPolicy { get; internal set; }

	public NullValuePolicy NullValuePolicy { get; internal set; }

	public StaleValuePolicy StaleValuePolicy { get; internal set; }

	public ExpirationPolicy ExpirationPolicy { get; internal set; }

	protected ICacheReaper Reaper { get; }

	public virtual TValue this[TKey key] {
		get => Get(key).Value;
		set => Set(key, value);
	}

	public virtual bool ContainsCachedItem(TKey key) {
		if (key is null)
			Guard.ArgumentNotNull(key, nameof(key));
		using (EnterReadScope()) {
			CheckNotDisposed();
			return InternalStorage.TryGetValue(key, out var item) && !IsExpired(item, DateTime.Now);
		}
	}

	public virtual CachedItem<TValue> Get(TKey key) {
		if (key is null)
			Guard.ArgumentNotNull(key, nameof(key));
		using (EnterWriteScope()) {
			CheckNotDisposed();
			var now = DateTime.Now;
			var requiresFetch = !InternalStorage.TryGetValue(key, out var item) || !item.IsRemoving && (IsExpired(item, now) || IsStale(key, item));
			CheckNotDisposed();
			if (requiresFetch) {
				if (item != null)
					RemoveItemInternal(key);
				CheckNotDisposed();
				NotifyItemFetching(key);
				CheckNotDisposed();
				var value = Fetch(key);
				CheckNotDisposed();
				now = DateTime.Now;
				Guard.Ensure(value is not null || NullValuePolicy != NullValuePolicy.Throw, "Cache fetched a null value and its NullValuePolicy prohibits null values.");
				if (value is null && NullValuePolicy == NullValuePolicy.ReturnButDontCache) {
					item = NewCachedItem(key, value);
					CheckNotDisposed();
					item.FetchedOn = item.LastAccessedOn = now;
					item.Traits = CachedItemTraits.Invalidated;
				} else {
					item = StoreValue(key, value, now, true);
				}
				NotifyItemFetched(key, value);
				CheckNotDisposed();
				now = DateTime.Now;
			}
			TotalAccesses = TotalAccesses == long.MaxValue ? TotalAccesses : TotalAccesses + 1;
			LastAccessedOn = now;
			item.AccessedCount = item.AccessedCount == uint.MaxValue ? item.AccessedCount : item.AccessedCount + 1;
			item.LastAccessedOn = now;
			item.AccessOrder = NextOrder();
			if (item.Value is null) {
				Guard.Ensure(NullValuePolicy != NullValuePolicy.Throw, "Cache returned a null value and its NullValuePolicy prohibits null values.");
				if (NullValuePolicy == NullValuePolicy.ReturnButDontCache) {
					if (InternalStorage.TryGetValue(key, out var stored) && ReferenceEquals(stored, item))
						RemoveItemInternal(key);
					CheckNotDisposed();
					item.Traits |= CachedItemTraits.Invalidated;
				}
			}
			return item;
		}
	}

	public virtual void Set(TKey key, TValue value) {
		if (key is null)
			Guard.ArgumentNotNull(key, nameof(key));
		using (EnterWriteScope()) {
			CheckNotDisposed();
			var now = DateTime.Now;
			StoreValue(key, value, now, false);
			LastUpdateOn = LastAccessedOn = now;
		}
	}

	public void Invalidate(TKey key) {
		if (key is null)
			Guard.ArgumentNotNull(key, nameof(key));
		using (EnterWriteScope()) {
			CheckNotDisposed();
			if (InternalStorage.TryGetValue(key, out var item))
				item.Traits |= CachedItemTraits.Invalidated;
		}
	}

	public virtual void Remove(TKey key) {
		if (key is null)
			Guard.ArgumentNotNull(key, nameof(key));
		using (EnterWriteScope()) {
			CheckNotDisposed();
			RemoveItemInternal(key);
		}
	}

	public virtual void Purge() {
		using (EnterWriteScope()) {
			CheckNotDisposed();
			foreach (var entry in InternalStorage.ToArray())
				if (InternalStorage.TryGetValue(entry.Key, out var current) && ReferenceEquals(current, entry.Value))
					RemoveItemInternal(entry.Key);
		}
	}

	public virtual void BulkLoad(IEnumerable<KeyValuePair<TKey, TValue>> bulkLoadedValues) {
		Guard.ArgumentNotNull(bulkLoadedValues, nameof(bulkLoadedValues));
		using (EnterWriteScope()) {
			CheckNotDisposed();
			foreach (var pair in bulkLoadedValues)
				Set(pair.Key, pair.Value);
		}
	}

	public bool IsExpired(CachedItem<TValue> item) => IsExpired(item, DateTime.Now);

	public bool IsStale(TKey key, CachedItem<TValue> item)
		=> StaleValuePolicy switch {
			StaleValuePolicy.AssumeNeverStale => false,
			StaleValuePolicy.CheckStaleOnDemand => CheckStaleness(key, item),
			_ => throw new ArgumentOutOfRangeException()
		};

	public virtual long Reap(long minimumRelease) {
		Guard.Argument(minimumRelease >= 0, nameof(minimumRelease), "The requested release must be nonnegative.");
		using (EnterWriteScope()) {
			CheckNotDisposed();
			if (minimumRelease == 0)
				return 0;
			var before = CurrentSize;
			var now = DateTime.Now;
			var candidates = InternalStorage.Where(entry => IsReapable(entry.Value) && (ReapPolicy != CacheReapPolicy.None || IsExpired(entry.Value, now))).ToArray();
			if (ReapPolicy is not (CacheReapPolicy.None or CacheReapPolicy.ASAP))
				Array.Sort(candidates, (left, right) => CompareVictims(left.Value, right.Value, now));
			else if (ReapPolicy == CacheReapPolicy.ASAP)
				candidates = candidates.Where(entry => IsExpired(entry.Value, now)).Concat(candidates.Where(entry => !IsExpired(entry.Value, now))).ToArray();

			foreach (var candidate in candidates) {
				if (before - CurrentSize >= minimumRelease)
					break;
				if (InternalStorage.TryGetValue(candidate.Key, out var current) && ReferenceEquals(current, candidate.Value) && IsReapable(current))
					RemoveItemInternal(candidate.Key);
			}
			return Math.Max(0, before - CurrentSize);
		}
	}

	public virtual void Dispose() {
		using (EnterWriteScope()) {
			if (_disposed)
				return;
			if (_removalDepth > 0)
				throw new InvalidOperationException("Cannot dispose the cache while preparing an entry for removal.");
			_disposed = true;
			var errors = new List<Exception>();
			try {
				foreach (var entry in InternalStorage.ToArray()) {
					try {
						RemoveItemInternal(entry.Key);
					} catch (Exception error) {
						errors.Add(error);
					}
				}
			} finally {
				// Cached payloads and a supplied reaper are borrowed.
				InternalStorage.Clear();
				CurrentSize = 0;
				_variantHandlers.Clear();
				_itemFetching = null;
				_itemFetched = null;
				_itemRemoving = null;
				_itemRemoved = null;
				try {
					Reaper.Deregister(this);
				} catch (Exception error) {
					errors.Add(error);
				}
			}
			if (errors.Count > 0)
				throw new AggregateException("Cache removal callbacks failed during disposal.", errors);
		}
	}

	protected abstract long EstimateSize(TValue value);

	protected abstract TValue Fetch(TKey key);

	protected abstract bool CheckStaleness(TKey key, CachedItem<TValue> item);

	protected virtual CachedItem<TValue> NewCachedItem(TKey key, TValue value) => new() { Value = value };

	protected virtual void OnItemFetching(TKey key) {
	}

	protected virtual void OnItemFetched(TKey key, TValue value) {
	}

	/// <summary>
	/// Prepares an entry for removal while it remains stored and counted against capacity.
	/// An exception prevents this removal. Recursive removal skips this entry; replacing it or starting disposal is rejected.
	/// Disposal retains its best-effort cleanup contract and clears membership even when preparation fails.
	/// </summary>
	protected virtual void OnItemRemoving(TKey key, CachedItem<TValue> item) {
	}

	protected virtual void OnItemRemoved(TKey key, CachedItem<TValue> item) {
	}

	protected void NotifyItemFetching(TKey key) {
		OnItemFetching(key);
		_itemFetching?.Invoke(key);
	}

	protected void NotifyItemFetched(TKey key, TValue value) {
		OnItemFetched(key, value);
		_itemFetched?.Invoke(key, value);
	}

	protected void NotifyItemRemoving(TKey key, CachedItem<TValue> item) {
		OnItemRemoving(key, item);
		_itemRemoving?.Invoke(key, item);
	}

	protected void NotifyItemRemoved(TKey key, CachedItem<TValue> item) {
		OnItemRemoved(key, item);
		_itemRemoved?.Invoke(key, item);
	}

	internal void RemoveItemInternal(TKey key) {
		if (!InternalStorage.TryGetValue(key, out var item) || item.IsRemoving)
			return;
		item.IsRemoving = true;
		_removalDepth++;
		try {
			NotifyItemRemoving(key, item);
		} finally {
			_removalDepth--;
			item.IsRemoving = false;
		}
		InternalStorage.Remove(key);
		CurrentSize -= item.Size;
		item.Traits = (item.Traits | CachedItemTraits.Purged) & ~CachedItemTraits.CanPurge;
		NotifyItemRemoved(key, item);
	}

	private CachedItem<TValue> StoreValue(TKey key, TValue value, DateTime now, bool fetched) {
		var size = EstimateSize(value);
		CheckNotDisposed();
		Guard.Ensure(size >= 0, "A cache size estimator returned a negative size.");
		Guard.Ensure(size <= MaxCapacity, $"Cache capacity insufficient for requested space {size}");
		InternalStorage.TryGetValue(key, out var original);
		if (original?.IsRemoving == true)
			throw new InvalidOperationException("Cannot replace an entry while preparing it for removal.");
		Protect(original);
		try {
			EnsureCapacity(size - (original?.Size ?? 0));
			CheckNotDisposed();
			// Callbacks may have replaced the key; credit only the entry still owned by this cache.
			InternalStorage.TryGetValue(key, out var existing);
			var item = existing ?? NewCachedItem(key, value);
			CheckNotDisposed();
			Guard.Ensure(item != null, "The cached-item factory returned null.");
			// The virtual factory may populate this key or other entries before returning.
			if (existing == null && InternalStorage.TryGetValue(key, out var createdDuringFactory)) {
				existing = createdDuringFactory;
				item = existing;
			}
			var delta = size - (existing?.Size ?? 0);
			Guard.Ensure(delta <= Reaper.AvailableSpace(this), "A reentrant callback left insufficient capacity for the requested replacement.");
			var updatedSize = checked(CurrentSize + delta);
			if (existing == null) {
				item.FetchedOn = now;
				item.InsertionOrder = NextOrder();
			}
			if (fetched)
				item.FetchedOn = now;
			item.Value = value;
			item.Size = size;
			item.LastAccessedOn = now;
			item.AccessOrder = NextOrder();
			item.Traits &= ~(CachedItemTraits.Invalidated | CachedItemTraits.Purged);
			InternalStorage[key] = item;
			CurrentSize = updatedSize;
			return item;
		} finally {
			Unprotect(original);
		}
	}

	/// <summary>Attempts to release capacity before a derived cache allocates a value of a known size.</summary>
	protected void EnsureCapacity(long delta) {
		var available = Reaper.AvailableSpace(this);
		if (delta <= available)
			return;
		var deficit = delta - available;
		var scaled = Math.Ceiling(MaxCapacity * ReapLevel);
		var fraction = scaled >= MaxCapacity ? MaxCapacity : Math.Max(1, (long)scaled);
		var target = Math.Max(deficit, fraction);
		Reaper.MakeSpace(this, target);
	}

	private bool IsExpired(CachedItem<TValue> item, DateTime now) {
		Guard.ArgumentNotNull(item, nameof(item));
		if ((item.Traits & CachedItemTraits.Invalidated) != 0)
			return true;
		var from = ExpirationPolicy switch {
			ExpirationPolicy.SinceFetchedTime => item.FetchedOn,
			ExpirationPolicy.SinceLastAccessedTime => item.LastAccessedOn,
			_ => now
		};
		return ExpirationPolicy != ExpirationPolicy.None && now - from > ExpirationDuration;
	}

	private bool IsReapable(CachedItem<TValue> item) => item.CanPurge && !item.IsRemoving && !_protectedItems.ContainsKey(item);

	private int CompareVictims(CachedItem<TValue> left, CachedItem<TValue> right, DateTime now) {
		var expired = IsExpired(right, now).CompareTo(IsExpired(left, now));
		if (expired != 0)
			return expired;
		var result = ReapPolicy switch {
			CacheReapPolicy.LeastUsed => left.AccessedCount.CompareTo(right.AccessedCount),
			CacheReapPolicy.Oldest => left.InsertionOrder.CompareTo(right.InsertionOrder),
			CacheReapPolicy.LongestIdle => left.AccessOrder.CompareTo(right.AccessOrder),
			CacheReapPolicy.Largest => right.Size.CompareTo(left.Size),
			CacheReapPolicy.Smallest => left.Size.CompareTo(right.Size),
			_ => throw new NotSupportedException(ReapPolicy.ToString())
		};
		if (result == 0 && ReapPolicy == CacheReapPolicy.LeastUsed)
			result = right.Size.CompareTo(left.Size);
		return result != 0 ? result : left.InsertionOrder.CompareTo(right.InsertionOrder);
	}

	private void Protect(CachedItem<TValue> item) {
		if (item != null)
			_protectedItems[item] = _protectedItems.TryGetValue(item, out var count) ? count + 1 : 1;
	}

	private void Unprotect(CachedItem<TValue> item) {
		if (item == null)
			return;
		var count = _protectedItems[item];
		if (count == 1)
			_protectedItems.Remove(item);
		else
			_protectedItems[item] = count - 1;
	}

	private long NextOrder() => _order = checked(_order + 1);

	private void CheckNotDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

	private void AddTypedHandler(int eventIndex, Delegate handler) {
		if (handler == null)
			return;
		if (handler.GetType() != EventType(eventIndex)) {
			AddVariantHandler(eventIndex, handler);
			return;
		}
		using (EnterWriteScope())
			SetEvent(eventIndex, Delegate.Combine(GetEvent(eventIndex), handler));
	}

	private void RemoveTypedHandler(int eventIndex, Delegate handler) {
		if (handler == null)
			return;
		if (handler.GetType() != EventType(eventIndex)) {
			RemoveVariantHandler(eventIndex, handler);
			return;
		}
		using (EnterWriteScope())
			SetEvent(eventIndex, Delegate.Remove(GetEvent(eventIndex), handler));
	}

	private static Type EventType(int eventIndex)
		=> eventIndex switch { 0 => typeof(EventHandlerEx<TKey>), 1 => typeof(EventHandlerEx<TKey, TValue>), _ => typeof(EventHandlerEx<TKey, CachedItem<TValue>>) };

	private void AddVariantHandler(int eventIndex, Delegate handler) {
		if (handler == null)
			return;
		if (handler.GetType() == EventType(eventIndex)) {
			AddTypedHandler(eventIndex, handler);
			return;
		}
		using (EnterWriteScope()) {
			foreach (var component in handler.GetInvocationList()) {
				var key = (eventIndex, component);
				if (!_variantHandlers.TryGetValue(key, out var projection))
					projection = (CreateHandlerProjection(eventIndex, component), 0);
				_variantHandlers[key] = (projection.Projection, projection.Count + 1);
				SetEvent(eventIndex, Delegate.Combine(GetEvent(eventIndex), projection.Projection));
			}
		}
	}

	private void RemoveVariantHandler(int eventIndex, Delegate handler) {
		if (handler == null)
			return;
		if (handler.GetType() == EventType(eventIndex)) {
			RemoveTypedHandler(eventIndex, handler);
			return;
		}
		using (EnterWriteScope()) {
			Delegate combined = null;
			foreach (var component in handler.GetInvocationList()) {
				if (!_variantHandlers.TryGetValue((eventIndex, component), out var projection))
					return;
				combined = Delegate.Combine(combined, projection.Projection);
			}
			var before = GetEvent(eventIndex);
			var after = Delegate.Remove(before, combined);
			if (Equals(before, after))
				return;
			SetEvent(eventIndex, after);
			foreach (var component in handler.GetInvocationList()) {
				var key = (eventIndex, component);
				var projection = _variantHandlers[key];
				if (projection.Count == 1)
					_variantHandlers.Remove(key);
				else
					_variantHandlers[key] = (projection.Projection, projection.Count - 1);
			}
		}
	}

	private Delegate CreateHandlerProjection(int eventIndex, Delegate handler)
		=> eventIndex switch {
			0 => new EventHandlerEx<TKey>(key => ((EventHandlerEx<TKey>)handler)(key)),
			1 => new EventHandlerEx<TKey, TValue>((key, value) => ((EventHandlerEx<TKey, TValue>)handler)(key, value)),
			_ => new EventHandlerEx<TKey, CachedItem<TValue>>((key, item) => ((EventHandlerEx<TKey, CachedItem<TValue>>)handler)(key, item))
		};

	private Delegate GetEvent(int eventIndex) => eventIndex switch { 0 => _itemFetching, 1 => _itemFetched, 2 => _itemRemoved, _ => _itemRemoving };

	private void SetEvent(int eventIndex, Delegate value) {
		switch (eventIndex) {
			case 0: _itemFetching = (EventHandlerEx<TKey>)value; break;
			case 1: _itemFetched = (EventHandlerEx<TKey, TValue>)value; break;
			case 2: _itemRemoved = (EventHandlerEx<TKey, CachedItem<TValue>>)value; break;
			default: _itemRemoving = (EventHandlerEx<TKey, CachedItem<TValue>>)value; break;
		}
	}
}
