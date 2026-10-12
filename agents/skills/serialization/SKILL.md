---
name: serialization
description: Binary serialization with generic IItemSerializer, SerializerFactory, SerializerBuilder, and endian-aware readers/writers. Trigger when writing serializers or stream I/O.
---

# Serialization Skill

Read [serialization guidance](../../../docs/guidelines/patterns/serialization.md) for endian-aware contracts and existing serializer layers.

Core framework: `IItemSerializer<T>` (extends `IItemSizer<T>`) serializes via `EndianBinaryWriter`/`EndianBinaryReader` with a `SerializationContext`.

## Rules
- **Never** `System.BitConverter` — use `EndianBitConverter.Little` / `.Big`.
- Reuse the supplied `EndianBinaryWriter` / `EndianBinaryReader` and `SerializationContext` for nested members. Endianness comes from the reader/writer's `BitConverter`, not a context property. Pass the chosen `Endianness` explicitly when using byte-array convenience APIs.
- Derive from `ItemSerializerBase<T>` rather than implementing `IItemSerializer<T>` directly.
- Register type→serializer mappings in a `SerializerFactory`; chain custom factories from `SerializerFactory.Default` via `new SerializerFactory(baseFactory)`.

## Building a serializer

`MyType` and `Type1Serializer` below are application types; `MyType` needs a parameterless constructor for `For<MyType>()`, or supply `For(() => CreateMyType())`.

```csharp
using Sphere10.Framework;

var serializer =
	SerializerBuilder
		.For<MyType>()
		.Serialize(x => x.Property1, new Type1Serializer())
		.Ignore(x => x.Transient)
		.Build();

var factory = new SerializerFactory(SerializerFactory.Default);
var auto = SerializerBuilder.FactoryAssemble<MyType>(factory);
```

`FactoryAssemble<T>(factory)` is exercised by `tests/Sphere10.Framework.Tests/Serialization/SerializerBuilderTests.cs`. To retain the generated registration for later factory lookup, use `factory.RegisterAutoBuild<MyType>()`, then `factory.GetSerializer<MyType>()`.

Do not copy `.For<T>().SerializeMembersAutomatically().Build()` as a working automatic path in this checkout: automatic member selection requires a factory, and `SerializerBuilder.UseFactory` currently dereferences its unset `_factory` before assigning the argument. The missing-member path in `SerializeMembers` also ignores its callback's returned serializer. Inspect or repair these paths within the authorized task before recommending them. Explicit member binding and the static factory-assembly path avoid that configuration route.

## Existing pieces
- `CompositeSerializer<T>` serializes its configured member bindings. Automatic assembly selects public instance fields/properties that can be read and written, excludes `[Transient]`, and orders inherited members before derived members; it is not an implicit stable wire contract.
- `PolymorphicSerializer` — dispatches to concrete-type serializers for abstract types.
- `SerializerFactory.Default` — global singleton with primitive/common serializers pre-registered.

Keep custom registrations in an application-owned factory. A copied factory preserves existing registrations; it does not automatically freeze a deterministic protocol schema. Match explicit type codes, member layout, null/reference handling, polymorphic registrations and endianness when bytes cross process/version boundaries.

Implement `CalculateSize(context, item)` consistently with `Serialize`: verify its result equals the written length for the relevant null, reference, collection and polymorphic cases. Use the same context throughout an object graph so shared/cyclic reference tracking remains coherent. Add reference/null support through the existing serializer wrappers rather than assuming every pure serializer accepts null.

## Persistence layers
Serialization feeds into `ClusteredStreams` → `ObjectStream<T>` → `ObjectSpace` and the stream-mapped collections (`StreamMappedList`, `StreamMappedDictionary`, `StreamMappedHashSet`). When changing serializer behavior, verify these consumers still round-trip.
