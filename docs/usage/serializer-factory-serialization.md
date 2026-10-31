# Serializing a serializer factory

`SerializerFactorySerializer` persists the registrations needed to reconstruct a
serializer factory. It preserves type codes, the next generated-code boundary,
constructor choices and composite member layouts. It is independent of ObjectSpace
and other storage containers. The existing `SerializerSerializer` continues to
encode a serializer hierarchy against an already available factory; this codec
persists the registry needed to reconstruct that factory.

```csharp
var factory = new SerializerFactory(SerializerFactory.Default)
	.RegisterAutoBuild<StoredItem>();
var serializer = new SerializerFactorySerializer();
var bytes = serializer.SerializeToBytes(factory, Endianness.LittleEndian);
var restored = serializer.DeserializeBytes(bytes, Endianness.LittleEndian);

var itemSerializer = restored.GetSerializer<StoredItem>();
var payload = itemSerializer.SerializeToBytes(new StoredItem { Value = 42 }, Endianness.LittleEndian);
var item = itemSerializer.DeserializeBytes(payload, Endianness.LittleEndian);

public sealed class StoredItem {
	public long Value { get; set; }
}
```

The codec is also registered in `SerializerFactory.Default`. The byte order is
chosen by the caller, as with the other Framework serializers. Binary reader,
writer and serialization context instances are passed through its component
serializers. The codec owns its complex registration/member components and
accesses primitive and other basic singleton components directly. The complex
factory codec has no singleton instance.

This adds a built-in registration to the default factory. Existing persisted
formats must retain their original type/code mappings when automatic application
codes were allocated after the built-ins; this codec does not migrate old stores.

## Supported registrations

| Registration | Preserved behavior |
|---|---|
| Standard registrations copied from `SerializerFactory.Default` | Original type codes and standard construction behavior |
| `Register<TItem, TSerializer>()` | Public parameterless construction, even when an injectable constructor also exists |
| `Register(dataType, serializerType)` | The factory's ordinary serializer-constructor injection |
| Open generic registrations | Generic serializer hierarchy and dependent serializers |
| Enum and nullable enum registrations | Both types and their exact codes |
| Automatically assembled composites | Actual member order, declaring type, field/property identity, polymorphic wrappers and reference modes |
| Automatically assembled polymorphic registrations | Dispatch through the reconstructed factory |

Composite dependencies are reconstructed before their containing composites so
constant-size information remains consistent. Cyclic member graphs retain their
reference handling. The reconstructed factory finishes registration before it is
returned.

## Construction limits

The factory's registered CLR data and serializer types must remain available.
Custom serializer constructors must recreate their deterministic configuration.
State changed on an activated custom serializer after construction is not captured.

Arbitrary registration delegates, action-based behavior, captured closures,
independently configured serializer instances and custom composite activators
cannot be reconstructed. Unsupported registration forms are rejected before
factory bytes are written. A registration owned by another factory is rejected;
polymorphic serializers and member serializers must resolve through the factory
being serialized. Copying a factory does not rebind an already activated
polymorphic serializer to that copy.

The binary record contains the minimum generated type code, registration count,
and registration records. Each registration stores its code, CLR types,
construction choice and ordered member records. The internal registration
serializer delegates member records to its internal member serializer. No
executable delegate or serializer instance state is embedded.

Seekable inputs bound row allocations by the remaining data. Nonseekable inputs
decode rows incrementally rather than allocating from an unchecked count. Normal
stream and deserializer errors propagate.

See [the NUnit fixture](../../tests/Sphere10.Framework.Tests/Serialization/SerializerFactorySerializerTests.cs)
for executable roundtrip, constructor, reference and rejection examples.
