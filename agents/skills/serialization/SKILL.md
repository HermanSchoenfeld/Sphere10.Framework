---
name: serialization
description: Binary serialization with IItemSerializer<T>, SerializerFactory, SerializerBuilder, and endian-aware readers/writers. Trigger when writing serializers or stream I/O.
---

# Serialization Skill

Core framework: `IItemSerializer<T>` (extends `IItemSizer<T>`) serializes via `EndianBinaryWriter`/`EndianBinaryReader` with a `SerializationContext`.

## Rules
- **Never** `System.BitConverter` — use `EndianBitConverter.Little` / `.Big`.
- Always propagate `Endianness` from the parent context in new serializers and stream code.
- Derive from `ItemSerializerBase<T>` rather than implementing `IItemSerializer<T>` directly.
- Register type→serializer mappings in a `SerializerFactory`; chain custom factories from `SerializerFactory.Default` via `new SerializerFactory(baseFactory)`.

## Building a serializer
```csharp
var serializer =
	SerializerBuilder
		.For<MyType>()
		.Serialize(x => x.Property1, new Type1Serializer())
		.Ignore(x => x.Transient)
		.Build();

var auto =
	SerializerBuilder
		.For<MyType>()
		.SerializeMembersAutomatically()
		.Build();
```

## Existing pieces
- `CompositeSerializer` — auto-serializes all readable/writable members.
- `PolymorphicSerializer` — dispatches to concrete-type serializers for abstract types.
- `SerializerFactory.Default` — global singleton with primitive/common serializers pre-registered.

## Persistence layers
Serialization feeds into `ClusteredStreams` → `ObjectStream<T>` → `ObjectSpace` and the stream-mapped collections (`StreamMappedList`, `StreamMappedDictionary`, `StreamMappedHashSet`). When changing serializer behavior, verify these consumers still round-trip.
