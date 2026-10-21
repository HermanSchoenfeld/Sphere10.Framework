# Binary Serialization

The [core project guide](../../../src/Sphere10.Framework/README.md) describes the persistence consumers.

## Serialization

The serialization framework provides binary serializers for efficient stream-based I/O. Core types:

- **[IItemSerializer<T>](../../../src/Sphere10.Framework/Serialization/IItemSerializer.cs)** — serializes/deserializes `T` via `EndianBinaryWriter`/`EndianBinaryReader` with a `SerializationContext`. Extends `IItemSizer<T>` for size calculation.
- **[ItemSerializerBase<T>](../../../src/Sphere10.Framework/Serialization/ItemSerializerBase.cs)** — abstract base implementing the typed serialization primitives.
- **[SerializerFactory](../../../src/Sphere10.Framework/Serialization/Factory/SerializerFactory.cs)** — registry of type→serializer mappings and generic serializer definitions. `SerializerFactory.Default` has the built-in primitive/common registrations and finishes registration during static initialization. `new SerializerFactory(baseFactory)` copies registrations into a separately configurable factory; it does not establish a live fallback to future registrations in the source factory.
- **[SerializerBuilder](../../../src/Sphere10.Framework/Serialization/Builder/SerializerBuilder.cs)** — fluent configuration of composite member bindings; see [fluent builders](fluent-builders.md).
- **[CompositeSerializer<T>](../../../src/Sphere10.Framework/Serialization/Builder/CompositeSerializer.cs)** — serializes configured member bindings. Automatic assembly is a separate factory path.
- **[PolymorphicSerializer<T>](../../../src/Sphere10.Framework/Serialization/Special/PolymorphicSerializer.cs)** — resolves the concrete runtime type's serializer through a factory. The non-generic `PolymorphicSerializer.Create(factory, itemType)` is its construction helper.
- Serializers are endian-aware (`EndianBitConverter`, `Endianness`).

## Endian-Aware Bit Conversion

**Never** use `System.BitConverter`. Always use the framework's endian-aware equivalents:

- **[EndianBitConverter](../../../src/Sphere10.Framework/Conversion/EndianBitConverter.cs)** — abstract base with `LittleEndianBitConverter` and `BigEndianBitConverter` implementations. Use `EndianBitConverter.Little` / `EndianBitConverter.Big` static singletons.
- **[EndianBinaryWriter](../../../src/Sphere10.Framework/IO/EndianBinaryWriter.cs)** / **[EndianBinaryReader](../../../src/Sphere10.Framework/IO/EndianBinaryReader.cs)** — endian-aware stream readers/writers used throughout serialization.
- All serialization infrastructure (`IItemSerializer<T>`, `ClusteredStreams`, `ObjectStream`) is endianness-aware. When writing new serializers or stream code, reuse the supplied reader/writer and context. Endianness comes from the reader/writer's `BitConverter`; [SerializationContext](../../../src/Sphere10.Framework/Serialization/SerialiationContext.cs) does not expose an `Endianness` property.

## Automatic assembly and reflection

Use `SerializerBuilder.FactoryAssemble<T>(factory)` for the existing factory-based automatic path. Its non-generic convenience overload is `FactoryAssemble(Type itemType, SerializerFactory factory)`; the overload accepting retention explicitly is `FactoryAssemble(SerializerFactory factory, Type itemType, bool retainAssembledTypeSerializersInFactory, long? typeCodeStart = null)`. The generic convenience overload uses a temporary copied factory. To retain assembled registrations in your factory, call `factory.RegisterAutoBuild<T>()` and then `factory.GetSerializer<T>()`.

The current `GetSerializableMembers` scans public instance fields/properties, uses the mapping member's `CanRead`/`CanWrite` flags, excludes `[Transient]`, and orders base members before derived members. Properties require public getters and setters. This scan does not filter indexers or distinguish readonly fields through `FieldMember.CanWrite`; reflected member access may therefore be unsuitable for a particular type. The automatic composite path also requires a compatible public parameterless constructor. Use explicit bindings and an activator where the reflected shape is unsuitable. See [PropertyMember](../../../src/Sphere10.Framework/Mapping/PropertyMember.cs), [FieldMember](../../../src/Sphere10.Framework/Mapping/FieldMember.cs) and the constructor configuration in `CompositeSerializer`.

Existing builder limitations remain explicit: `UseFactory` reads its unset `_factory` before assigning the argument, and `SerializeMembers` ignores the serializer returned by its missing-member callback. Automatic member selection also requires `_factory`. Explicit `Serialize(Member, serializer)` requires the member's declaring type to equal the configured item type, so it does not universally accept inherited member bindings. Do not recommend a `For<T>().UseFactory(factory).SerializeMembersAutomatically()` chain as a working alternative until these paths are repaired within an authorized task.

## Wire contracts and sizing

Reflected automatic assembly does not establish a stable wire layout. Custom factory registrations, type codes, member layout, null/reference handling and endianness must match the storage or protocol contract. `CalculateSize(context, item)` must agree with the bytes written by `Serialize`. Reuse one context throughout the graph; convenience overloads without a context create a new one. Pure composite and polymorphic serializers report `SupportsNull=false`; use the existing null/reference wrappers when the contract requires them.

[SerializerBuilderTests](../../../tests/Sphere10.Framework.Tests/Serialization/SerializerBuilderTests.cs) provide explicit-binding, automatic-assembly, base-member ordering, constructor, null and cyclic-reference examples. They are references for the relevant behavior, not a promise that every reflected type can be serialized automatically.

See [fluent builders](fluent-builders.md) and [collections and storage](collections-and-storage.md) for the surrounding configuration and consumers.
