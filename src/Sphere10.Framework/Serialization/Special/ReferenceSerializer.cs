using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// Serializer wrapper for handling reference types. This serializes a prefix byte that indicates whether the item is null, not null, or a reference to an object
/// that was previously serialized in the serialization context. It is used to support nullability and cyclic/repeating references in a serialization context.
/// </summary>
public sealed class ReferenceSerializer<TItem> : ItemSerializerDecorator<TItem> {
	private static string ErrMsg_NullValuesNotEnabled = $"Null value for '{typeof(TItem).ToStringCS()}' is not permitted";

	private readonly bool _supportsNull;
	private readonly bool _supportsContextReferences;
	private readonly bool _supportsExternalReferences;
	private readonly bool _supportsReferences; 

	public ReferenceSerializer(IItemSerializer<TItem> valueSerializer) 
		: this(valueSerializer, ReferenceSerializerMode.Default) {
	}

	public ReferenceSerializer(IItemSerializer<TItem> valueSerializer, ReferenceSerializerMode mode) 
		: base(valueSerializer) {
		Guard.ArgumentNotNull(valueSerializer, nameof(valueSerializer));
		Guard.Argument(!valueSerializer.GetType().IsSubtypeOfGenericType(typeof(ReferenceSerializer<>), out _), nameof(valueSerializer), "Value serializer cannot be a reference serializer");
		Guard.Ensure(!typeof(TItem).IsValueType, $"{nameof(TItem)} can only be used with reference types");
		_supportsNull = mode.HasFlag(ReferenceSerializerMode.SupportNull);
		_supportsContextReferences = mode.HasFlag(ReferenceSerializerMode.SupportContextReferences);
		_supportsExternalReferences = mode.HasFlag(ReferenceSerializerMode.SupportExternalReferences);
		_supportsReferences = _supportsContextReferences || _supportsExternalReferences;
		// Enable early instance registration on the inner serializer so that cyclic back-references
		// encountered during deserialization can resolve to the parent object before it is fully populated.
		if (valueSerializer is IValueTypeActivatingSerializer activatingSerializer)
			activatingSerializer.ShouldNotifyInstanceActivation = true;

	}

	public override bool SupportsNull => true;

	public override long CalculateTotalSize(SerializationContext context, IEnumerable<TItem> items, bool calculateIndividualItems, out long[] itemSizes) {
		// We need to calculate the size of each item individually, since some may be references and some may not be.
		var sizes = items.Select(item => CalculateSize(context, item)).ToArray();
		itemSizes = calculateIndividualItems ? sizes.ToArray() : null;
		return sizes.Sum();
	}
		

	public override long CalculateSize(SerializationContext context, TItem item) {
		var referenceType = ClassifyReferenceType(item, context, true, out var contextIndex);
		long size;
		switch(referenceType) {
			case ReferenceType.IsNull:
				context.NotifySizing(item, out contextIndex);
				size = sizeof(byte); // only the discriminator byte is present (ReferenceType.IsNull)
				context.NotifySized(contextIndex);
				break;
			case ReferenceType.IsNotNull:
				if (!_supportsReferences)
					if (context.IsSizingOrSerializingObject(item, out _))
						throw new InvalidOperationException($"Cyclic-reference was encountered when sizing item  '{item}'. Please ensure context references are enabled sizing cyclic-referencing object graphs or ensure no cyclic references exist.");
				context.NotifySizing(item, out contextIndex);
				size = sizeof(byte) + Internal.CalculateSize(context, item);
				context.NotifySized(contextIndex);
				break;
			case ReferenceType.IsContextReference:
				size = sizeof(byte) + CVarIntSerializer.Instance.CalculateSize(context, unchecked((ulong)contextIndex));
				break;
			case ReferenceType.IsExternalReference:
				// External references: use the serialized size from the context's classification
				size = sizeof(byte) + context.LastClassifiedExternalReferenceSize;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(referenceType), referenceType, null);
		}
		return size;
	}

	public override void Serialize(TItem item, EndianBinaryWriter writer, SerializationContext context) {
		var referenceType = ClassifyReferenceType(item, context, false, out var contextIndex);
		PrimitiveSerializer<byte>.Instance.Serialize((byte)referenceType, writer, context);
		switch (referenceType) {
			case ReferenceType.IsNull:
				context.NotifySerializingObject(item, out contextIndex);
				context.NotifySerializedObject(contextIndex);
				break;
			case ReferenceType.IsNotNull:
				if (!_supportsReferences)
					if (context.IsSerializingObject(item, out _))
						throw new InvalidOperationException($"Cyclic-reference was encountered when serializing item '{item}'. Please ensure context references are enabled serializing cyclic-referencing object graphs or ensure no cyclic references exist.");
				context.NotifySerializingObject(item, out contextIndex);
				Internal.Serialize(item, writer, context);
				context.NotifySerializedObject(contextIndex);
				break;
			case ReferenceType.IsContextReference:
				CVarIntSerializer.Instance.Serialize(unchecked((ulong)contextIndex), writer, context);
				break;
			case ReferenceType.IsExternalReference:
				// Delegate external reference serialization entirely to the context
				context.SerializeExternalReference(item, writer);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(referenceType), referenceType, null);
		}
	}

	public override TItem Deserialize(EndianBinaryReader reader, SerializationContext context) {
		var referenceType = (ReferenceType)PrimitiveSerializer<byte>.Instance.Deserialize(reader, context);
		switch (referenceType) {
			case ReferenceType.IsNull:
					Guard.Ensure(_supportsNull, ErrMsg_NullValuesNotEnabled);
					// Null values must still be registered in the context so that deserialization indices
					// stay aligned with those assigned during sizing/serialization. Without this, any
					// subsequent IsContextReference would resolve to the wrong object.
					context.NotifyDeserializingObject(out var index);
					context.NotifyDeserializedObject(null, index);
					return default;
				case ReferenceType.IsNotNull:
					context.NotifyDeserializingObject(out index);
					var item = Internal.Deserialize(reader, context);
					context.NotifyDeserializedObject(item, index);
					return item;
				case ReferenceType.IsContextReference:
					var contextIndex = CVarIntSerializer.Instance.Deserialize(reader, context);
					return (TItem)context.GetDeserializedObject(unchecked((long)(ulong)contextIndex));
				case ReferenceType.IsExternalReference:
					// Delegate external reference deserialization and resolution entirely to the context
					return (TItem)context.DeserializeAndResolveExternalReference(reader);
			default:
				throw new ArgumentOutOfRangeException(nameof(referenceType), referenceType, null);
		}
	}

	/// <summary>
	/// Determines whether <paramref name="item"/> should be serialized as null, a full value, a context reference,
	/// or an external reference.
	/// The <paramref name="sizeOnly"/> flag selects which context query to use:
	///   - Sizing (sizeOnly=true) uses <see cref="SerializationContext.HasSizedOrSerializedObject"/> because an object
	///     that has been sized (or serialized) in any prior pass already has a stable context index.
	///   - Serializing (sizeOnly=false) uses <see cref="SerializationContext.IsSerializingOrHasSerializedObject"/>,
	///     which excludes the "Sized" status. This prevents treating an object that was only sized (not yet serialized)
	///     as a context reference during the serialization pass, which would produce an invalid reference.
	/// External references are only produced when <see cref="_supportsExternalReferences"/> is true AND the
	/// serialization context's <see cref="SerializationContext.TryClassifyAsExternalReference"/> identifies the item
	/// as an external object. Component objects fall through to <see cref="ReferenceType.IsNotNull"/>.
	/// </summary>
	private ReferenceType ClassifyReferenceType(TItem item, SerializationContext context, bool sizeOnly, out long index) {
		index = -1;

		// Null check — return IsNull if nulls are supported, otherwise throw
		if (item == null) 
			return _supportsNull ? ReferenceType.IsNull : throw new InvalidOperationException(ErrMsg_NullValuesNotEnabled);

		// Context reference check — has this exact object instance already been processed in this context?
		if (_supportsContextReferences && (sizeOnly ? context.HasSizedOrSerializedObject(item, out index) : context.IsSerializingOrHasSerializedObject(item, out index)))
			return ReferenceType.IsContextReference;

		// External reference check — delegate to the context's virtual method
		if (_supportsExternalReferences && context.TryClassifyAsExternalReference(item, out _))
			return ReferenceType.IsExternalReference;

		return ReferenceType.IsNotNull;
	}

	/// <summary>
	/// Discriminates how a reference-type value is serialized in the stream.
	/// </summary>
	public enum ReferenceType : byte {
		IsNull = 0,             // The value is null — only the discriminator byte is written
		IsNotNull = 1,          // The value is a full inline object — discriminator byte followed by the serialized object
		IsContextReference = 2, // The value was already seen in this serialization context — discriminator byte followed by a CVarInt context index
		IsExternalReference = 3 // The value is an external reference — discriminator byte followed by subclass-defined reference data
	}
}

