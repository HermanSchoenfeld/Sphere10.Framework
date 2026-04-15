namespace Sphere10.Framework;

/// <summary>
/// Implemented by serializers that activate (construct) instances during deserialization
/// (e.g. <see cref="CompositeSerializer{TItem}"/>, <see cref="CollectionSerializerBase{TCollection,TItem}"/>, <see cref="ArraySerializer{T}"/>).
/// When <see cref="ShouldNotifyInstanceActivation"/> is true, the serializer registers the newly-created
/// instance with the <see cref="SerializationContext"/> immediately after construction — before its members
/// are deserialized. This is necessary for cyclic object graphs: a member being deserialized may hold a
/// back-reference to the parent, and the context must already know the parent's identity to resolve it.
/// The flag defaults to false and is set to true by <see cref="ReferenceSerializer{TItem}"/> when it wraps
/// an inner serializer, since only reference-tracked graphs can contain such cycles.
/// </summary>
public interface IValueTypeActivatingSerializer : IItemSerializer {
	bool ShouldNotifyInstanceActivation { get; set; }
}
