// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>
/// ObjectSpace-specific serialization context that provides external reference classification, serialization,
/// and resolution for dimension objects. Overrides the virtual methods on <see cref="SerializationContext"/>
/// to handle <see cref="ObjectSpaceObjectReference"/> pointers transparently.
/// </summary>
/// <remarks>
/// This context is created by <see cref="ObjectSpaceReferenceSerializer{TItem}"/> whenever a serialization
/// operation passes through an ObjectSpace-aware serializer decorator. It encapsulates all ObjectSpace-specific
/// knowledge (dimension types, InstanceTracker, reference resolution) so that the base
/// <see cref="SerializationContext"/> and <see cref="ReferenceSerializer{TItem}"/> remain ObjectSpace-ignorant.
/// </remarks>
internal sealed class ObjectSpaceSerializationContext : SerializationContext {

	private readonly HashSet<Type> _dimensionTypes;
	private readonly InstanceTracker _instanceTracker;
	private readonly Func<ObjectSpaceObjectReference, object> _resolveReference;

	/// <summary>
	/// Accumulated during serialization: every external reference written is recorded here so that
	/// <c>SaveInternal</c> can diff against previous out-refs for GC bookkeeping.
	/// </summary>
	public List<ObjectSpaceObjectReference> CollectedOutRefs { get; } = new();

	public ObjectSpaceSerializationContext(
		HashSet<Type> dimensionTypes,
		InstanceTracker instanceTracker,
		Func<ObjectSpaceObjectReference, object> resolveReference) {
		Guard.ArgumentNotNull(dimensionTypes, nameof(dimensionTypes));
		Guard.ArgumentNotNull(instanceTracker, nameof(instanceTracker));
		Guard.ArgumentNotNull(resolveReference, nameof(resolveReference));
		_dimensionTypes = dimensionTypes;
		_instanceTracker = instanceTracker;
		_resolveReference = resolveReference;
	}

	/// <summary>
	/// Determines whether <paramref name="item"/> is a dimension object that should be serialized as an
	/// external <see cref="ObjectSpaceObjectReference"/> pointer. Returns true if the item's CLR type is
	/// a registered dimension type and the object is tracked in the <see cref="InstanceTracker"/>.
	/// Sets <see cref="SerializationContext.LastClassifiedExternalReferenceSize"/> on success.
	/// </summary>
	protected internal override bool TryClassifyAsExternalReference(object item) {
		var objType = item.GetType();

		// Only objects whose CLR type is registered as a dimension are treated as external references
		if (!_dimensionTypes.Contains(objType))
			return false;

		// Look up the pre-assigned ObjectSpaceObjectReference from the InstanceTracker.
		// This reference was eagerly assigned during New<T>() or Get<T>().
		if (!_instanceTracker.TryGetRef(item, out var objRef))
			throw new InvalidOperationException($"Dimension object of type {objType.ToStringCS()} is not tracked in the InstanceTracker — was it created outside of ObjectSpace.New<T>()?");

		SetLastClassifiedExternalReferenceSize(ObjectSpaceObjectReferenceSerializer.SerializedSize);
		return true;
	}

	/// <summary>
	/// Writes the <see cref="ObjectSpaceObjectReference"/> for the given dimension object to the stream
	/// and records it in <see cref="CollectedOutRefs"/> for post-serialization GC bookkeeping.
	/// </summary>
	protected internal override void SerializeExternalReference(object item, EndianBinaryWriter writer) {
		if (!_instanceTracker.TryGetRef(item, out var objRef))
			throw new InvalidOperationException($"Dimension object of type {item.GetType().ToStringCS()} is not tracked in the InstanceTracker");

		ObjectSpaceObjectReferenceSerializer.Instance.Serialize(objRef, writer, this);
		CollectedOutRefs.Add(objRef);
	}

	/// <summary>
	/// Reads an <see cref="ObjectSpaceObjectReference"/> from the stream and resolves it to a live object
	/// instance via the ObjectSpace's resolution callback (checks InstanceTracker cache first, then loads
	/// from the dimension's stream if needed).
	/// </summary>
	protected internal override object DeserializeAndResolveExternalReference(EndianBinaryReader reader) {
		var externalRef = ObjectSpaceObjectReferenceSerializer.Instance.Deserialize(reader, this);
		return _resolveReference(externalRef);
	}
}
