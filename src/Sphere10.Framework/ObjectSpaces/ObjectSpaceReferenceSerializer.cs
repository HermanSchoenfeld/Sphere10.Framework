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
/// Serializer decorator for handling reference types within an ObjectSpace. Wraps a standard
/// <see cref="IItemSerializer{TItem}"/> and creates an <see cref="ObjectSpaceSerializationContext"/>
/// for each sizing, serialization, or deserialization operation so that dimension-typed properties
/// are serialized as lightweight external reference pointers instead of inline objects.
/// </summary>
/// <remarks>
/// When a property's CLR type is registered as a dimension in the ObjectSpace, the underlying
/// <see cref="ReferenceSerializer{TItem}"/> will detect it via the <see cref="ObjectSpaceSerializationContext"/>'s
/// <see cref="SerializationContext.TryClassifyAsExternalReference"/> override and write a lightweight
/// <see cref="ObjectSpaceObjectReference"/> pointer instead of serializing inline.
/// For component objects (types NOT registered as dimensions), serialization falls through to the
/// standard inline behavior.
/// </remarks>
/// <typeparam name="TItem">The CLR type being serialized (may or may not be a dimension type).</typeparam>
internal sealed class ObjectSpaceReferenceSerializer<TItem> : ItemSerializerDecorator<TItem> {

	private readonly HashSet<Type> _dimensionTypes;
	private readonly InstanceTracker _instanceTracker;
	private readonly Func<ObjectSpaceObjectReference, object> _resolveReference;

	/// <summary>
	/// Constructs an ObjectSpace-aware reference serializer decorator.
	/// </summary>
	/// <param name="innerSerializer">The inner serializer (typically a <see cref="ReferenceSerializer{TItem}"/> wrapping a CompositeSerializer).</param>
	/// <param name="dimensionTypes">Set of CLR types registered as dimensions in this ObjectSpace. Used to classify external vs component objects.</param>
	/// <param name="instanceTracker">The ObjectSpace's instance tracker, used to look up <see cref="ObjectSpaceObjectReference"/> for tracked objects.</param>
	/// <param name="resolveReference">Callback to resolve an external reference back to a live object (checks cache, then loads from dimension).</param>
	public ObjectSpaceReferenceSerializer(
		IItemSerializer<TItem> innerSerializer,
		HashSet<Type> dimensionTypes,
		InstanceTracker instanceTracker,
		Func<ObjectSpaceObjectReference, object> resolveReference)
		: base(innerSerializer) {
		Guard.ArgumentNotNull(dimensionTypes, nameof(dimensionTypes));
		Guard.ArgumentNotNull(instanceTracker, nameof(instanceTracker));
		Guard.ArgumentNotNull(resolveReference, nameof(resolveReference));
		_dimensionTypes = dimensionTypes;
		_instanceTracker = instanceTracker;
		_resolveReference = resolveReference;
	}

	/// <summary>
	/// Creates an <see cref="ObjectSpaceSerializationContext"/> and delegates to the inner serializer for sizing.
	/// </summary>
	public override long CalculateSize(SerializationContext context, TItem item) {
		using var osContext = CreateObjectSpaceContext();
		return base.CalculateSize(osContext, item);
	}

	/// <summary>
	/// Creates an <see cref="ObjectSpaceSerializationContext"/> and delegates to the inner serializer for serialization.
	/// </summary>
	public override void Serialize(TItem item, EndianBinaryWriter writer, SerializationContext context) {
		using var osContext = CreateObjectSpaceContext();
		base.Serialize(item, writer, osContext);
	}

	/// <summary>
	/// Creates an <see cref="ObjectSpaceSerializationContext"/> and delegates to the inner serializer for deserialization.
	/// </summary>
	public override TItem Deserialize(EndianBinaryReader reader, SerializationContext context) {
		using var osContext = CreateObjectSpaceContext();
		return base.Deserialize(reader, osContext);
	}

	/// <summary>
	/// Creates a fresh <see cref="ObjectSpaceSerializationContext"/> configured with this ObjectSpace's
	/// dimension types, instance tracker, and reference resolution callback.
	/// </summary>
	private ObjectSpaceSerializationContext CreateObjectSpaceContext()
		=> new(_dimensionTypes, _instanceTracker, _resolveReference);
}

