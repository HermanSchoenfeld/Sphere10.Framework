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
/// <see cref="IItemSerializer{TItem}"/> and installs ObjectSpace-specific external reference callbacks
/// on the <see cref="SerializationContext"/> before each sizing, serialization, or deserialization operation.
/// </summary>
/// <remarks>
/// When a property's CLR type is registered as a dimension in the ObjectSpace, the underlying
/// <see cref="ReferenceSerializer{TItem}"/> will detect it via the installed callbacks and write a lightweight
/// external reference pointer (<see cref="ObjectSpaceObjectReference"/>) instead of serializing inline.
/// For component objects (types NOT registered as dimensions), serialization falls through to the
/// standard inline behavior.
/// <para>
/// This class configures the <see cref="SerializationContext"/> with two callbacks:
/// <list type="bullet">
///   <item><see cref="SerializationContext.ClassifyExternalReference"/>: Checks whether the object's type
///   is a registered dimension type. If so, looks up its <see cref="ObjectSpaceObjectReference"/> from
///   the <see cref="InstanceTracker"/>.</item>
///   <item><see cref="SerializationContext.ResolveExternalReference"/>: Given an <see cref="ObjectSpaceObjectReference"/>,
///   returns the live object instance from the InstanceTracker cache or loads it from the dimension.</item>
/// </list>
/// </para>
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
	/// Configures the serialization context with ObjectSpace-specific external reference callbacks,
	/// then delegates to the inner serializer for actual sizing.
	/// </summary>
	public override long CalculateSize(SerializationContext context, TItem item) {
		// Install the ObjectSpace callbacks on the context before sizing
		ConfigureContext(context);
		return base.CalculateSize(context, item);
	}

	/// <summary>
	/// Configures the serialization context with ObjectSpace-specific external reference callbacks,
	/// then delegates to the inner serializer for actual serialization.
	/// </summary>
	public override void Serialize(TItem item, EndianBinaryWriter writer, SerializationContext context) {
		ConfigureContext(context);
		base.Serialize(item, writer, context);
	}

	/// <summary>
	/// Configures the serialization context with the resolve callback for external references,
	/// then delegates to the inner serializer for deserialization.
	/// </summary>
	public override TItem Deserialize(EndianBinaryReader reader, SerializationContext context) {
		ConfigureContext(context);
		return base.Deserialize(reader, context);
	}

	/// <summary>
	/// Installs the ObjectSpace classification and resolution callbacks on the context if they are not
	/// already set. This is idempotent — safe to call multiple times during the same serialization pass.
	/// </summary>
	private void ConfigureContext(SerializationContext context) {
		// Only install callbacks once per serialization context to avoid overwriting an already-configured context
		if (context.ClassifyExternalReference is null) {
			// Classification: determine if a given object is a dimension object or a component object.
			// Dimension objects → external reference; component objects → inline serialization.
			context.ClassifyExternalReference = obj => {
				var objType = obj.GetType();

				// Only objects whose CLR type is registered as a dimension are treated as external references
				if (!_dimensionTypes.Contains(objType))
					return (IsExternal: false, Reference: default);

				// Look up the pre-assigned ObjectSpaceObjectReference from the InstanceTracker.
				// This reference was eagerly assigned during New<T>() or Get<T>().
				if (!_instanceTracker.TryGetRef(obj, out var objRef))
					throw new InvalidOperationException($"Dimension object of type {objType.ToStringCS()} is not tracked in the InstanceTracker — was it created outside of ObjectSpace.New<T>()?");

				return (IsExternal: true, Reference: objRef);
			};
		}

		if (context.ResolveExternalReference is null) {
			// Resolution: given an ObjectSpaceObjectReference, return the live object instance.
			context.ResolveExternalReference = _resolveReference;
		}
	}
}

