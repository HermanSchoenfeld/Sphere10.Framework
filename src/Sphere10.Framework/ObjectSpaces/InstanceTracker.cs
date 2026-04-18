// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>
/// Tracks live object instances within an <see cref="ObjectSpace"/>, maintaining:
/// <list type="bullet">
///   <item>A per-type bijective map of row index ↔ object instance (original functionality).</item>
///   <item>A global map of object ↔ <see cref="ObjectSpaceObjectReference"/> for cross-dimension ref tracking.</item>
///   <item>Per-object out-refs and in-refs sets for the in-memory reference cache used by GC.</item>
/// </list>
/// </summary>
/// <remarks>Not thread-safe by design — all access is expected to occur within an ObjectSpace access scope.</remarks>
internal class InstanceTracker {

	private readonly Dictionary<Type, BijectiveDictionary<long, object>> _objectsByType;
	private readonly Dictionary<Type, int> _newInstanceCounts;

	/// <summary>
	/// Maps each tracked object to its globally unique <see cref="ObjectSpaceObjectReference"/>.
	/// Keyed by reference equality so distinct instances with equal values are tracked separately.
	/// </summary>
	private readonly ReferenceDictionary<object, ObjectSpaceObjectReference> _objectRefs;

	/// <summary>
	/// Reverse map: <see cref="ObjectSpaceObjectReference"/> → live object instance.
	/// Used by deserialization to resolve external references back to cached instances.
	/// </summary>
	private readonly Dictionary<ObjectSpaceObjectReference, object> _refToObject;

	/// <summary>
	/// Per-object out-refs: for each tracked object, the set of dimension objects it references.
	/// Updated after serialization when <see cref="ObjectSpaceSerializationContext.CollectedOutRefs"/> is harvested.
	/// </summary>
	private readonly ReferenceDictionary<object, HashSet<ObjectSpaceObjectReference>> _outRefs;

	/// <summary>
	/// Per-object in-refs: for each tracked object, the set of dimension objects that reference it.
	/// This is the inverse of <see cref="_outRefs"/> and is the primary input to GC decisions.
	/// An empty in-refs set on a non-root object means it is garbage.
	/// </summary>
	private readonly ReferenceDictionary<object, HashSet<ObjectSpaceObjectReference>> _inRefs;

	/// <summary>
	/// Set of objects that have been created via <see cref="TrackNew"/> but not yet persisted.
	/// Used by <see cref="IsProvisional"/> to distinguish new objects from persisted ones.
	/// </summary>
	private readonly HashSet<object> _provisionalObjects;

	public InstanceTracker() {
		_objectsByType = new Dictionary<Type, BijectiveDictionary<long, object>>(TypeEquivalenceComparer.Instance);
		_newInstanceCounts = new Dictionary<Type, int>(TypeEquivalenceComparer.Instance);
		_objectRefs = new ReferenceDictionary<object, ObjectSpaceObjectReference>();
		_refToObject = new Dictionary<ObjectSpaceObjectReference, object>();
		_outRefs = new ReferenceDictionary<object, HashSet<ObjectSpaceObjectReference>>();
		_inRefs = new ReferenceDictionary<object, HashSet<ObjectSpaceObjectReference>>();
		_provisionalObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
	}


	public TItem Get<TItem>(long index) {
		if (!TryGet<TItem>(index, out var item))
			throw new InvalidOperationException($"No instance of {typeof(TItem).ToStringCS()} an index {index} was tracked");
		return item;
	}

	public bool TryGet<TItem>(long index, out TItem item) {
		if (TryGet(typeof(TItem), index, out var itemO)) {
			item = (TItem)itemO;
			return true;
		}
		item = default;
		return false;
	}

	public bool TryGet(Type itemType, long index, out object item) {
		if (!_objectsByType.TryGetValue(itemType, out var instances)) {
			item = default;
			return false;
		}

		if (!instances.TryGetValue(index, out item))
			return false;
		
		return true;
	}

	public IEnumerable<object> GetInstances() 
		=> _objectsByType.Values.SelectMany(instances => instances.Values);


	public IEnumerable<object> GetInstances(Type itemType) 
		=> _objectsByType.TryGetValue(itemType, out var instances) ? instances.Values : Array.Empty<object>();


	/// <summary>
	/// Tracks a newly created object with a provisional positive index computed as
	/// <paramref name="dimensionCount"/> + the number of new instances already created for that type.
	/// This avoids negative provisional indices and produces indices that are safe for use as
	/// <see cref="ObjectSpaceObjectReference.ObjectIndex"/> values.
	/// </summary>
	public long TrackNew(object item, long dimensionCount) {
		var itemType = item.GetType();
		if (!_newInstanceCounts.TryGetValue(itemType, out var typeNewCount))
			typeNewCount = 0;
		typeNewCount++;
		_newInstanceCounts[itemType] = typeNewCount;
		var provisionalIndex = dimensionCount + typeNewCount - 1;
		Track(item, provisionalIndex);
		_provisionalObjects.Add(item);
		AssertConsistency();
		return provisionalIndex;
	}

	/// <summary>
	/// Returns true if the object was created via <see cref="TrackNew"/> and has not yet been
	/// marked as persisted via <see cref="MarkPersisted"/>.
	/// </summary>
	public bool IsProvisional(object item) => _provisionalObjects.Contains(item);

	/// <summary>
	/// Marks a provisional object as persisted, removing it from the provisional set.
	/// Called after <c>SaveInternal</c> persists the object and updates its tracked index.
	/// </summary>
	public void MarkPersisted(object item) => _provisionalObjects.Remove(item);

	public void Track(object item, long index) {
		var itemType = item.GetType();
		
		if (!_objectsByType.TryGetValue(itemType, out var instances)) {
			instances = CreateInstanceDictionary();
			_objectsByType.Add(itemType, instances);
		}

		// check if the index is already in use
		if (instances.TryGetValue(index, out var _)) 
			throw new InvalidOperationException($"An instance of {itemType.ToStringCS()} with index {index} has already been tracked");

		// if the item is already tracked, update the index
		if (instances.Bijection.ContainsKey(item))
			instances.Bijection[item] = index;
		else
			// otherwise, add the item
			instances.Add(index, item);
		AssertConsistency();
	}

	public void Untrack(object item) {
		var itemType = item.GetType();
		
		if (!_objectsByType.TryGetValue(itemType, out var instances) || !instances.TryGetKey(item, out var index)) 
			throw new InvalidOperationException("Object instance was not tracked");

		instances.Remove(index);
		if (instances.Count == 0)
			_objectsByType.Remove(itemType);

		// Also clean up the ObjectSpaceObjectReference tracking and reference caches
		if (_objectRefs.TryGetValue(item, out var objRef)) {
			_objectRefs.Remove(item);
			_refToObject.Remove(objRef);
		}
		_outRefs.Remove(item);
		_inRefs.Remove(item);
		_provisionalObjects.Remove(item);
		AssertConsistency();
	}

	public long GetIndexOf(object item) {
		if (!TryGetIndexOf(item, out var index))
			throw new InvalidOperationException($"Instance of {item.GetType().ToStringCS()} was not tracked");
		return index;
	}

	public bool TryGetIndexOf(object item, out long index) {
		var itemType = item.GetType();

		if (!_objectsByType.TryGetValue(itemType, out var instances)) {
			index = default;
			return false;
		}

		if (!instances.TryGetKey(item, out index)) 
			return false;

		return true;
	}

	// --- ObjectSpaceObjectReference tracking (Task 6) ---

	/// <summary>
	/// Associates the given object with its <see cref="ObjectSpaceObjectReference"/>. Called eagerly
	/// during <c>New&lt;T&gt;</c> (with a provisional negative ObjectIndex) and again during <c>Save</c>
	/// (with the real persisted ObjectIndex). Replaces any previous association for the same object.
	/// </summary>
	public void TrackRef(object item, ObjectSpaceObjectReference objRef) {
		// Remove any previous ref mapping for this object (e.g. replacing provisional with real index)
		if (_objectRefs.TryGetValue(item, out var oldRef))
			_refToObject.Remove(oldRef);

		_objectRefs[item] = objRef;
		_refToObject[objRef] = item;
		AssertConsistency();
	}

	/// <summary>
	/// Looks up the <see cref="ObjectSpaceObjectReference"/> for a tracked object.
	/// Returns true if the object has a tracked ref, false otherwise.
	/// </summary>
	public bool TryGetRef(object item, out ObjectSpaceObjectReference objRef) 
		=> _objectRefs.TryGetValue(item, out objRef);

	/// <summary>
	/// Resolves an <see cref="ObjectSpaceObjectReference"/> to its live object instance.
	/// Returns true if the ref is currently tracked (the object is in the cache), false otherwise.
	/// </summary>
	public bool TryResolveRef(ObjectSpaceObjectReference objRef, out object item) 
		=> _refToObject.TryGetValue(objRef, out item);

	// --- In-memory reference cache (Task 7) ---

	/// <summary>
	/// Returns the set of outgoing references for the given object (dimension objects it references).
	/// Creates an empty set if none exists yet. This set is updated after each serialization pass.
	/// </summary>
	public HashSet<ObjectSpaceObjectReference> GetOrCreateOutRefs(object item) {
		if (!_outRefs.TryGetValue(item, out var refs)) {
			refs = new HashSet<ObjectSpaceObjectReference>();
			_outRefs[item] = refs;
		}
		return refs;
	}

	/// <summary>
	/// Returns the set of incoming references for the given object (dimension objects that reference it).
	/// Creates an empty set if none exists yet. An empty in-refs set on a non-root object = garbage.
	/// </summary>
	public HashSet<ObjectSpaceObjectReference> GetOrCreateInRefs(object item) {
		if (!_inRefs.TryGetValue(item, out var refs)) {
			refs = new HashSet<ObjectSpaceObjectReference>();
			_inRefs[item] = refs;
		}
		return refs;
	}

	/// <summary>
	/// Tries to get the in-refs set for the object identified by <paramref name="objRef"/>.
	/// Returns false if the object is not currently tracked in memory.
	/// </summary>
	public bool TryGetInRefs(ObjectSpaceObjectReference objRef, out HashSet<ObjectSpaceObjectReference> inRefs) {
		if (_refToObject.TryGetValue(objRef, out var targetObj)) {
			inRefs = GetOrCreateInRefs(targetObj);
			return true;
		}
		inRefs = null;
		return false;
	}

	public void Clear() {
		_objectsByType.Clear();
		_newInstanceCounts.Clear();
		_objectRefs.Clear();
		_refToObject.Clear();
		_outRefs.Clear();
		_inRefs.Clear();
		_provisionalObjects.Clear();
	}

	/// <summary>
	/// DEBUG-only consistency assertion verifying that <see cref="_objectsByType"/>,
	/// <see cref="_objectRefs"/>, and <see cref="_refToObject"/> are mutually consistent.
	/// </summary>
	[Conditional("DEBUG")]
	internal void AssertConsistency() {
		// Every object that has a ref mapping must also be present in _objectsByType
		foreach (var kvp in _objectRefs) {
			var item = kvp.Key;
			var objRef = kvp.Value;
			var itemType = item.GetType();
			Debug.Assert(
				_objectsByType.TryGetValue(itemType, out var instances) && instances.Bijection.ContainsKey(item),
				$"InstanceTracker inconsistency: object of type {itemType.Name} has an ObjectRef but is not in _objectsByType"
			);
			Debug.Assert(
				_refToObject.TryGetValue(objRef, out var reverseItem) && ReferenceEquals(reverseItem, item),
				$"InstanceTracker inconsistency: _refToObject does not map back to the same object for ref ({objRef.DimensionIndex},{objRef.ObjectIndex})"
			);
		}

		// Every entry in _refToObject must have a corresponding _objectRefs entry
		foreach (var kvp in _refToObject) {
			var objRef = kvp.Key;
			var item = kvp.Value;
			Debug.Assert(
				_objectRefs.TryGetValue(item, out var forwardRef) && forwardRef.Equals(objRef),
				$"InstanceTracker inconsistency: _refToObject contains ref ({objRef.DimensionIndex},{objRef.ObjectIndex}) with no matching _objectRefs entry"
			);
		}
	}

	private BijectiveDictionary<long, object> CreateInstanceDictionary() => new(EqualityComparer<long>.Default, ReferenceEqualityComparer.Instance);

}

