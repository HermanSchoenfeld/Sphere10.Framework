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
using System.Runtime.CompilerServices;
using System.Threading;

namespace Sphere10.Framework;

/// <summary>Exposes another key/value representation without replacing the source cache's typed storage or behavior.</summary>
public class ProjectedCache<TKey, TValue, TProjectedKey, TProjectedValue> : ICache<TProjectedKey, TProjectedValue>, IDisposable {

	public event EventHandlerEx<TProjectedKey> ItemFetching {
		add => AddHandler(0, value);
		remove => RemoveHandler(0, value);
	}

	public event EventHandlerEx<TProjectedKey, TProjectedValue> ItemFetched {
		add => AddHandler(1, value);
		remove => RemoveHandler(1, value);
	}

	public event EventHandlerEx<TProjectedKey, CachedItem<TProjectedValue>> ItemRemoving {
		add => AddHandler(3, value);
		remove => RemoveHandler(3, value);
	}

	public event EventHandlerEx<TProjectedKey, CachedItem<TProjectedValue>> ItemRemoved {
		add => AddHandler(2, value);
		remove => RemoveHandler(2, value);
	}

	private readonly ICache<TKey, TValue> _source;
	private readonly Func<TKey, TProjectedKey> _keyProjection;
	private readonly Func<TProjectedKey, TKey> _inverseKeyProjection;
	private readonly Func<TValue, TProjectedValue> _valueProjection;
	private readonly Func<TProjectedValue, TValue> _inverseValueProjection;
	private readonly ConditionalWeakTable<CachedItem<TValue>, ProjectedCachedItem<TValue, TProjectedValue>> _entryViews;
	private readonly ConditionalWeakTable<CachedItem<TValue>, ProjectedCachedItem<TValue, TProjectedValue>>.CreateValueCallback _entryViewFactory;
	private readonly Dictionary<(int Event, Delegate Handler), (Delegate Normalized, int Count)> _handlerProjections;
	private readonly bool[] _forwardersAttached = new bool[4];
	private volatile EventHandlerEx<TProjectedKey> _itemFetching;
	private volatile EventHandlerEx<TProjectedKey, TProjectedValue> _itemFetched;
	private volatile EventHandlerEx<TProjectedKey, CachedItem<TProjectedValue>> _itemRemoving;
	private volatile EventHandlerEx<TProjectedKey, CachedItem<TProjectedValue>> _itemRemoved;
	private volatile bool _disposed;

	public ProjectedCache(
		ICache<TKey, TValue> source,
		Func<TKey, TProjectedKey> keyProjection,
		Func<TProjectedKey, TKey> inverseKeyProjection,
		Func<TValue, TProjectedValue> valueProjection,
		Func<TProjectedValue, TValue> inverseValueProjection
	) {
		Guard.ArgumentNotNull(source, nameof(source));
		Guard.ArgumentNotNull(keyProjection, nameof(keyProjection));
		Guard.ArgumentNotNull(inverseKeyProjection, nameof(inverseKeyProjection));
		Guard.ArgumentNotNull(valueProjection, nameof(valueProjection));
		Guard.ArgumentNotNull(inverseValueProjection, nameof(inverseValueProjection));
		_source = source;
		_keyProjection = keyProjection;
		_inverseKeyProjection = inverseKeyProjection;
		_valueProjection = valueProjection;
		_inverseValueProjection = inverseValueProjection;
		_entryViews = new ConditionalWeakTable<CachedItem<TValue>, ProjectedCachedItem<TValue, TProjectedValue>>();
		_entryViewFactory = item => new ProjectedCachedItem<TValue, TProjectedValue>(item, _valueProjection, _inverseValueProjection);
		_handlerProjections = new Dictionary<(int Event, Delegate Handler), (Delegate Normalized, int Count)>();
	}

	public IEnumerable<CachedItem<TProjectedValue>> CachedItems {
		get {
			using (EnterReadScope())
				return Source.CachedItems.Select(ProjectEntry).ToArray();
		}
	}

	public int ItemCount => Source.ItemCount;

	public long CurrentSize => Source.CurrentSize;

	public long MaxCapacity => Source.MaxCapacity;

	public double ReapLevel => Source.ReapLevel;

	public DateTime LastUpdateOn => Source.LastUpdateOn;

	public DateTime LastAccessedOn => Source.LastAccessedOn;

	public long TotalAccesses => Source.TotalAccesses;

	public TimeSpan ExpirationDuration => Source.ExpirationDuration;

	public ExpirationPolicy ExpirationPolicy => Source.ExpirationPolicy;

	public CacheReapPolicy ReapPolicy => Source.ReapPolicy;

	public NullValuePolicy NullValuePolicy => Source.NullValuePolicy;

	public ISynchronizedObject ParentSyncObject {
		get => Source.ParentSyncObject;
		set => Source.ParentSyncObject = value;
	}

	public ReaderWriterLockSlim ThreadLock => Source.ThreadLock;

	public TProjectedValue this[TProjectedKey key] {
		get {
			CheckNotDisposed();
			var sourceKey = _inverseKeyProjection(key);
			var sourceValue = Source[sourceKey];
			CheckNotDisposed();
			var value = _valueProjection(sourceValue);
			CheckNotDisposed();
			return value;
		}
		set => Source[_inverseKeyProjection(key)] = _inverseValueProjection(value);
	}

	private ICache<TKey, TValue> Source {
		get {
			CheckNotDisposed();
			return _source;
		}
	}

	public bool ContainsCachedItem(TProjectedKey key) => Source.ContainsCachedItem(_inverseKeyProjection(key));

	public CachedItem<TProjectedValue> Get(TProjectedKey key) {
		CheckNotDisposed();
		var sourceKey = _inverseKeyProjection(key);
		var sourceItem = Source.Get(sourceKey);
		CheckNotDisposed();
		return ProjectEntry(sourceItem);
	}

	public void Set(TProjectedKey key, TProjectedValue value) => Source.Set(_inverseKeyProjection(key), _inverseValueProjection(value));

	public void BulkLoad(IEnumerable<KeyValuePair<TProjectedKey, TProjectedValue>> bulkLoadedValues) {
		Guard.ArgumentNotNull(bulkLoadedValues, nameof(bulkLoadedValues));
		Source.BulkLoad(bulkLoadedValues.Select(item => new KeyValuePair<TKey, TValue>(_inverseKeyProjection(item.Key), _inverseValueProjection(item.Value))));
	}

	public void Invalidate(TProjectedKey key) => Source.Invalidate(_inverseKeyProjection(key));

	public void Remove(TProjectedKey key) => Source.Remove(_inverseKeyProjection(key));

	public long Reap(long minimumRelease) => Source.Reap(minimumRelease);

	public void Purge() => Source.Purge();

	public ReadScope EnterReadScope() => Source.EnterReadScope();

	public WriteScope EnterWriteScope() => Source.EnterWriteScope();


	/// <summary>Detaches this adapter's subscriptions. Neither the source cache nor its cached values are disposed.</summary>
	public void Dispose() {
		using (_source.EnterWriteScope()) {
			if (_disposed)
				return;
			_disposed = true;
			var errors = new List<Exception>();
			try {
				for (var eventIndex = 0; eventIndex < _forwardersAttached.Length; eventIndex++) {
					try {
						DetachForwarder(eventIndex);
					} catch (Exception error) {
						errors.Add(error);
					}
				}
			} finally {
				_itemFetching = null;
				_itemFetched = null;
				_itemRemoving = null;
				_itemRemoved = null;
				_handlerProjections.Clear();
				_entryViews.Clear();
				Array.Clear(_forwardersAttached);
			}
			if (errors.Count > 0)
				throw new AggregateException("Cache projection event detachment failed during disposal.", errors);
		}
	}

	private ProjectedCachedItem<TValue, TProjectedValue> ProjectEntry(CachedItem<TValue> item)
		=> item == null ? null : _entryViews.GetValue(item, _entryViewFactory);

	private void AddHandler(int eventIndex, Delegate handler) {
		using (EnterWriteScope()) {
			CheckNotDisposed();
			if (handler == null)
				return;
			var before = GetEvent(eventIndex);
			Delegate normalized = null;
			foreach (var component in handler.GetInvocationList()) {
				var key = (eventIndex, component);
				if (!_handlerProjections.TryGetValue(key, out var projection))
					projection = (NormalizeHandler(eventIndex, component), 0);
				_handlerProjections[key] = (projection.Normalized, projection.Count + 1);
				normalized = Delegate.Combine(normalized, projection.Normalized);
			}
			if (before == null)
				AttachForwarder(eventIndex);
			SetEvent(eventIndex, Delegate.Combine(before, normalized));
		}
	}

	private void RemoveHandler(int eventIndex, Delegate handler) {
		using (_source.EnterWriteScope()) {
			if (_disposed || handler == null)
				return;
			Delegate normalized = null;
			foreach (var component in handler.GetInvocationList()) {
				if (!_handlerProjections.TryGetValue((eventIndex, component), out var projection))
					return;
				normalized = Delegate.Combine(normalized, projection.Normalized);
			}
			var before = GetEvent(eventIndex);
			var after = Delegate.Remove(before, normalized);
			if (Equals(before, after))
				return;
			SetEvent(eventIndex, after);
			foreach (var component in handler.GetInvocationList()) {
				var key = (eventIndex, component);
				var projection = _handlerProjections[key];
				if (projection.Count == 1)
					_handlerProjections.Remove(key);
				else
					_handlerProjections[key] = (projection.Normalized, projection.Count - 1);
			}
			if (after == null)
				DetachForwarder(eventIndex);
		}
	}

	private Delegate NormalizeHandler(int eventIndex, Delegate handler) {
		var eventType = eventIndex switch {
			0 => typeof(EventHandlerEx<TProjectedKey>),
			1 => typeof(EventHandlerEx<TProjectedKey, TProjectedValue>),
			_ => typeof(EventHandlerEx<TProjectedKey, CachedItem<TProjectedValue>>)
		};
		if (handler.GetType() == eventType)
			return handler;
		// A contravariant handler retains its original runtime delegate type, so normalize it before combining delegates.
		return eventIndex switch {
			0 => new EventHandlerEx<TProjectedKey>(key => ((EventHandlerEx<TProjectedKey>)handler)(key)),
			1 => new EventHandlerEx<TProjectedKey, TProjectedValue>((key, value) => ((EventHandlerEx<TProjectedKey, TProjectedValue>)handler)(key, value)),
			_ => new EventHandlerEx<TProjectedKey, CachedItem<TProjectedValue>>((key, item) => ((EventHandlerEx<TProjectedKey, CachedItem<TProjectedValue>>)handler)(key, item))
		};
	}

	private Delegate GetEvent(int eventIndex) => eventIndex switch { 0 => _itemFetching, 1 => _itemFetched, 2 => _itemRemoved, _ => _itemRemoving };

	private void SetEvent(int eventIndex, Delegate value) {
		switch (eventIndex) {
			case 0: _itemFetching = (EventHandlerEx<TProjectedKey>)value; break;
			case 1: _itemFetched = (EventHandlerEx<TProjectedKey, TProjectedValue>)value; break;
			case 2: _itemRemoved = (EventHandlerEx<TProjectedKey, CachedItem<TProjectedValue>>)value; break;
			default: _itemRemoving = (EventHandlerEx<TProjectedKey, CachedItem<TProjectedValue>>)value; break;
		}
	}

	private void AttachForwarder(int eventIndex) {
		if (_forwardersAttached[eventIndex])
			return;
		switch (eventIndex) {
			case 0: _source.ItemFetching += OnItemFetching; break;
			case 1: _source.ItemFetched += OnItemFetched; break;
			case 2: _source.ItemRemoved += OnItemRemoved; break;
			default: _source.ItemRemoving += OnItemRemoving; break;
		}
		_forwardersAttached[eventIndex] = true;
	}

	private void DetachForwarder(int eventIndex) {
		if (!_forwardersAttached[eventIndex])
			return;
		switch (eventIndex) {
			case 0: _source.ItemFetching -= OnItemFetching; break;
			case 1: _source.ItemFetched -= OnItemFetched; break;
			case 2: _source.ItemRemoved -= OnItemRemoved; break;
			default: _source.ItemRemoving -= OnItemRemoving; break;
		}
		_forwardersAttached[eventIndex] = false;
	}

	private void OnItemFetching(TKey key) {
		if (!_disposed)
			_itemFetching?.Invoke(_keyProjection(key));
	}

	private void OnItemFetched(TKey key, TValue value) {
		if (!_disposed)
			_itemFetched?.Invoke(_keyProjection(key), _valueProjection(value));
	}

	private void OnItemRemoving(TKey key, CachedItem<TValue> item) {
		if (!_disposed)
			_itemRemoving?.Invoke(_keyProjection(key), ProjectEntry(item));
	}

	private void OnItemRemoved(TKey key, CachedItem<TValue> item) {
		if (!_disposed)
			_itemRemoved?.Invoke(_keyProjection(key), ProjectEntry(item));
	}

	private void CheckNotDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
