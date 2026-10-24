// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sphere10.Framework;

public class SerializationContext : SyncScope {

	private enum SerializationStatus { Sizing, Sized, Serializing, Serialized, Deserializing, Deserialized }

	private BijectiveDictionary<object, long> _processedObjects;
	private Dictionary<long, SerializationStatus> _objectSerializationStatus;
	private List<Action> _onRootContextEndActions;
	private object _firstProcessedObject;
	private SerializationStatus _firstSerializationStatus;
	private bool _hasFirstProcessedObject;
	private bool _hasFirstSerializationStatus;
	private long _currentlyDeserializingIndex;
	// Sequential counter for deserialization indices. Unlike sizing/serialization (which derive indices from
	// _processedObjects.Count), deserialization must use its own counter so that indices align with those
	// assigned during a prior sizing or serialization pass when the same context is reused (mixed-context).
	private long _nextDeserializationIndex;
	private SerializerFactory _serializerFactory;
	private long _lastNonEphemeralTypeCode;
	private long _lastClassifiedExternalReferenceSize;

	public SerializationContext() {
		_currentlyDeserializingIndex = -1;
		_nextDeserializationIndex = 0;
		_serializerFactory = null;
		_lastNonEphemeralTypeCode = 0;
		_lastClassifiedExternalReferenceSize = 0;
	}

	public static SerializationContext New => new();

	/// <summary>
	/// The serialized byte size of the most recently classified external reference.
	/// Set by <see cref="TryClassifyAsExternalReference"/> when it returns true, and consumed
	/// immediately by the subsequent CalculateSize call.
	/// </summary>
	public long LastClassifiedExternalReferenceSize => _lastClassifiedExternalReferenceSize;

	public SerializerFactory EphemeralFactory {
		get  {
			Guard.Ensure(_serializerFactory is not null, "On-the-fly serializer construction is not permitted in this serialization context");
			return _serializerFactory;
		}
	}

	public bool HasEphemeralFactory => _serializerFactory is not null;

	public void SetEphemeralFactory(SerializerFactory factory) {
		Guard.Ensure(factory is not null && _serializerFactory is null, "An ephemeral factory has already been set in this serialization context");
		_serializerFactory = factory;
		_lastNonEphemeralTypeCode = factory.MaxGeneratedTypeCode;
	}

	public IEnumerable<SerializerFactory.Registration> GetEphemeralRegistrations() => _serializerFactory.GetRegistrationsAfterTypeCode(_lastNonEphemeralTypeCode);

	public void ClearEphemeralFactory() {
		Guard.Ensure(_serializerFactory is not null , "No ephemeral factory was set in this context");
		_serializerFactory = null;
		_lastNonEphemeralTypeCode = 0;
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public bool IsSizingOrSerializingObject(object obj, out long index) {
		if (obj is null) {
			index = -1;
			return false;
		}

		return TryGetProcessedIndex(obj, out index) && GetObjectStatus(index) is SerializationStatus.Sizing or SerializationStatus.Serializing;
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public bool HasSizedOrSerializedObject(object obj, out long index) {
		if (obj is null) {
			index = -1;
			return false;
		}

		// Optimized form of:
		// return _processedObjects.TryGetValue(obj, out index) && _objectSerializationStatus[index].IsIn(SerializationStatus.Sizing, SerializationStatus.Sized, SerializationStatus.Serializing, SerializationStatus.Serialized);
		if (!TryGetProcessedIndex(obj, out index))
			return false;
		var status = GetObjectStatus(index);
		return (int)status is >= (int)SerializationStatus.Sizing and <= (int)SerializationStatus.Serialized;
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public bool IsSerializingObject(object obj, out long index) {
		if (obj is null) {
			index = -1;
			return false;
		}

		return TryGetProcessedIndex(obj, out index) && GetObjectStatus(index) == SerializationStatus.Serializing;
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public bool IsSerializingOrHasSerializedObject(object obj, out long index) {
		if (obj is null) {
			index = -1;
			return false;
		}

		// Optimized form of:
		// return _processedObjects.TryGetValue(obj, out index) && _objectSerializationStatus[index].IsIn(SerializationStatus.Serializing,  SerializationStatus.Serialized);
		if (!TryGetProcessedIndex(obj, out index))
			return false;
		var status = GetObjectStatus(index);
		return (int)status is >= (int)SerializationStatus.Serializing and <= (int)SerializationStatus.Serialized;
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public object GetSizedOrSerializedObject(long index) {
		Debug.Assert(TryGetObjectStatus(index, out var status) && (int)status is >= (int)SerializationStatus.Sizing and <= (int)SerializationStatus.Serialized, $"No object was sized/serialized in this serialization context at index {index}");
		return GetProcessedObject(index);
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public object GetSerializedObject(long index) {
		Debug.Assert(TryGetObjectStatus(index, out var status) && status == SerializationStatus.Serialized, $"No object was serialized in this serialization context at index {index}");
		return GetProcessedObject(index);
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public object GetDeserializedObject(long index) {
		if(!TryGetObjectStatus(index, out var status))
			throw new InvalidOperationException($"No object at deserialization context index {index} exists");

		if (status != SerializationStatus.Deserialized)
			throw new InvalidOperationException($"Object at deserialization context index {index} was in status {status}");

		var obj = GetProcessedObject(index);
		// NullPlaceHolder was substituted during serialization to give null a distinct identity in the bijection.
		// Unwrap it here so callers see the original null value.
		return obj is NullPlaceHolder ? null : obj;
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public void NotifySizing(object obj, out long index) {
		obj ??= new NullPlaceHolder();
		index = ProcessedObjectCount;
		SetProcessedIndex(obj, index);
		SetObjectStatus(index, SerializationStatus.Sizing);
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public void NotifySized(long index) {
		SetObjectStatus(index, SerializationStatus.Sized);
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public void NotifySerializingObject(object obj, out long index) {
		obj ??= new NullPlaceHolder();

		if (TryGetProcessedIndex(obj, out index)) {
			// Some serializers may size objects before serializing them, and sizing may notify an object of serializtion for sizing purposes only
			// so we need to make sure we re-use the index established during sizing, and unmark this object as "sizing only"
			//if (_objectSerializationStatus[index] != SerializationStatus.Sized)
				//throw new InvalidOperationException("Object was already serialized in this serialization context");
			SetObjectStatus(index, SerializationStatus.Serializing);
			return;
		}

		index = ProcessedObjectCount;
		SetProcessedIndex(obj, index);
		SetObjectStatus(index, SerializationStatus.Serializing);
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public void NotifySerializedObject(long index) {
		SetObjectStatus(index, SerializationStatus.Serialized);
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public void NotifyDeserializingObject(out long serializationContextIndex) {
		serializationContextIndex = _nextDeserializationIndex++;
		var placeHolder = new PlaceHolder(this, serializationContextIndex);
		if (TryGetObjectStatus(serializationContextIndex, out var existingStatus) &&
			existingStatus is SerializationStatus.Sized or SerializationStatus.Serialized) {
			// Mixed-context: reuse the index from a prior sizing/serialization pass.
			// Replace the old object with a placeholder so the bijection stays valid.
			SetProcessedObject(serializationContextIndex, placeHolder);
			SetObjectStatus(serializationContextIndex, SerializationStatus.Deserializing);
		} else {
			AddProcessedObject(placeHolder, serializationContextIndex);
			AddObjectStatus(serializationContextIndex, SerializationStatus.Deserializing);
		}
		_currentlyDeserializingIndex = serializationContextIndex;
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	/// <summary>
	/// Called by serializers implementing <see cref="IValueTypeActivatingSerializer"/> immediately after
	/// constructing the target instance. Replaces the placeholder in the context with the real instance
	/// so that any cyclic back-references encountered while deserializing the instance's members can
	/// resolve to it. Resets <c>_currentlyDeserializingIndex</c> to -1 to prevent double-registration.
	/// </summary>
	public void SetDeserializingItem(object item) {
		if (_currentlyDeserializingIndex < 0)
			return;

		// this is a special case where we are deserializing an object that was not notified as being deserialized
		if (_currentlyDeserializingIndex == -1)
			NotifyDeserializingObject(out _);

		Debug.Assert(GetProcessedObject(_currentlyDeserializingIndex) is PlaceHolder, "Expected PlaceHolder but was an instance. Logic error in serialization flow.");
		SetProcessedObject(_currentlyDeserializingIndex, item);
		SetObjectStatus(_currentlyDeserializingIndex, SerializationStatus.Deserialized);
		_currentlyDeserializingIndex = -1;
	}
	
#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public void NotifyDeserializedObject(object obj, long serializationContextIndex) {
		obj ??= new NullPlaceHolder();
		
		if (TryGetProcessedObject(serializationContextIndex, out var item))
			Debug.Assert(item is PlaceHolder || ReferenceEquals(item, obj), "Cannot change a deserialized instance in context. Logic error in serialization flow.");
		
		SetProcessedObject(serializationContextIndex, obj); // setting it this way updates the dummy object put in NotifyDeserializingObject
		SetObjectStatus(serializationContextIndex, SerializationStatus.Deserialized);
		_currentlyDeserializingIndex = -1;
	}

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	public void RegisterFinalizationAction(Action action) => (_onRootContextEndActions ??= new List<Action>()).Add(action);

#if !DEBUG
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
	protected override void OnScopeEnd() {
		if (_onRootContextEndActions is not null)
			foreach (var action in _onRootContextEndActions)
				action();
		_processedObjects?.Clear();
		_firstProcessedObject = null;
		_hasFirstProcessedObject = false;

	}

	// --- External reference support (virtual methods for subclass override) ---

	/// <summary>
	/// Determines whether <paramref name="item"/> is an external reference (e.g. a dimension object
	/// in an ObjectSpace) that should be serialized as a lightweight pointer instead of inline.
	/// When true, <see cref="LastClassifiedExternalReferenceSize"/> is set to the byte size of the
	/// serialized reference.
	/// The base implementation always returns false (no external references).
	/// </summary>
	protected internal virtual bool TryClassifyAsExternalReference(object item) {
		_lastClassifiedExternalReferenceSize = 0;
		return false;
	}

	/// <summary>
	/// Writes the external reference for <paramref name="item"/> to the output stream.
	/// Called by <see cref="ReferenceSerializer{TItem}"/> when <see cref="TryClassifyAsExternalReference"/>
	/// returned true. Also performs any post-serialization bookkeeping (e.g. collecting out-refs).
	/// The base implementation throws — subclasses that support external references must override.
	/// </summary>
	protected internal virtual void SerializeExternalReference(object item, EndianBinaryWriter writer) {
		throw new NotSupportedException("External references are not supported by this serialization context");
	}

	/// <summary>
	/// Reads an external reference from the input stream and resolves it to a live object instance.
	/// Called by <see cref="ReferenceSerializer{TItem}"/> when it encounters an external reference discriminator.
	/// The base implementation throws — subclasses that support external references must override.
	/// </summary>
	protected internal virtual object DeserializeAndResolveExternalReference(EndianBinaryReader reader) {
		throw new NotSupportedException("External references are not supported by this serialization context");
	}

	/// <summary>
	/// Stores the serialized size from the most recent successful external reference classification.
	/// Called by <see cref="TryClassifyAsExternalReference"/> overrides to communicate the size
	/// to the <see cref="ReferenceSerializer{TItem}"/> CalculateSize path.
	/// </summary>
	protected void SetLastClassifiedExternalReferenceSize(long size) {
		_lastClassifiedExternalReferenceSize = size;
	}

	// Scalar reference serializers usually register only one object. Keep that entry inline so
	// transient placeholders and strings do not need identity hashes or dictionary storage.
	private long ProcessedObjectCount => _processedObjects?.Count ?? (_hasFirstProcessedObject ? 1 : 0);

	private BijectiveDictionary<object, long> ProcessedObjects {
		get {
			if (_processedObjects is null) {
				_processedObjects = new BijectiveDictionary<object, long>(ReferenceEqualityComparer.Instance, EqualityComparer<long>.Default);
				if (_hasFirstProcessedObject) {
					_processedObjects.Add(_firstProcessedObject, 0);
					_firstProcessedObject = null;
					_hasFirstProcessedObject = false;
				}
			}
			return _processedObjects;
		}
	}

	private Dictionary<long, SerializationStatus> ObjectSerializationStatus {
		get {
			if (_objectSerializationStatus is null) {
				_objectSerializationStatus = new Dictionary<long, SerializationStatus>();
				if (_hasFirstSerializationStatus) {
					_objectSerializationStatus.Add(0, _firstSerializationStatus);
					_hasFirstSerializationStatus = false;
				}
			}
			return _objectSerializationStatus;
		}
	}

	private bool TryGetProcessedIndex(object obj, out long index) {
		if (_processedObjects is not null)
			return _processedObjects.TryGetValue(obj, out index);
		index = 0;
		return _hasFirstProcessedObject && ReferenceEquals(_firstProcessedObject, obj);
	}

	private bool TryGetProcessedObject(long index, out object obj) {
		if (_processedObjects is not null)
			return _processedObjects.Bijection.TryGetValue(index, out obj);
		obj = _hasFirstProcessedObject && index == 0 ? _firstProcessedObject : null;
		return _hasFirstProcessedObject && index == 0;
	}

	private object GetProcessedObject(long index)
		=> TryGetProcessedObject(index, out var obj) ? obj : ProcessedObjects.Bijection[index];

	private void SetProcessedIndex(object obj, long index) {
		if (_processedObjects is null && index == 0 && obj is not null && (!_hasFirstProcessedObject || ReferenceEquals(_firstProcessedObject, obj))) {
			_firstProcessedObject = obj;
			_hasFirstProcessedObject = true;
			return;
		}
		ProcessedObjects[obj] = index;
	}

	private void SetProcessedObject(long index, object obj) {
		if (_processedObjects is null && index == 0 && obj is not null) {
			_firstProcessedObject = obj;
			_hasFirstProcessedObject = true;
			return;
		}
		ProcessedObjects.Bijection[index] = obj;
	}

	private void AddProcessedObject(object obj, long index) {
		if (_processedObjects is null && index == 0 && obj is not null && !_hasFirstProcessedObject) {
			_firstProcessedObject = obj;
			_hasFirstProcessedObject = true;
			return;
		}
		ProcessedObjects.Add(obj, index);
	}

	private bool TryGetObjectStatus(long index, out SerializationStatus status) {
		if (_objectSerializationStatus is not null)
			return _objectSerializationStatus.TryGetValue(index, out status);
		status = _firstSerializationStatus;
		return _hasFirstSerializationStatus && index == 0;
	}

	private SerializationStatus GetObjectStatus(long index)
		=> TryGetObjectStatus(index, out var status) ? status : ObjectSerializationStatus[index];

	private void SetObjectStatus(long index, SerializationStatus status) {
		if (_objectSerializationStatus is null && index == 0) {
			_firstSerializationStatus = status;
			_hasFirstSerializationStatus = true;
			return;
		}
		ObjectSerializationStatus[index] = status;
	}

	private void AddObjectStatus(long index, SerializationStatus status) {
		if (_objectSerializationStatus is null && index == 0 && !_hasFirstSerializationStatus) {
			_firstSerializationStatus = status;
			_hasFirstSerializationStatus = true;
			return;
		}
		ObjectSerializationStatus.Add(index, status);
	}

	#region Inner Types

	internal sealed class PlaceHolder {
		private readonly SerializationContext _owner;
		private readonly long _serializationContextIndex;

		public PlaceHolder(SerializationContext owner, long serializationContextIndex) {
			_owner = owner;
			_serializationContextIndex = serializationContextIndex;
		}

#if !DEBUG
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public object GetValue() {
			var value = _owner.GetDeserializedObject(_serializationContextIndex);
			if (value is NullPlaceHolder)
				value = null;
			return value;
		}
	}

	internal class NullPlaceHolder {
		public override string ToString() => "NULL PLACEHOLDER";
	}

	#endregion

}

