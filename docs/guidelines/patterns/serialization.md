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

## Component serializers

Compose complex serializers from serializers for their fields. Use
`PrimitiveSerializer<T>.Instance`, `EnumSerializer<T>.Instance` and existing type,
string or other component serializers consistently for sizing, serialization and
deserialization. Derive fixed widths from a component's `ConstantSize`; calculate
variable fields through that component's `CalculateSize(context, value)`.

Access existing primitive and basic singleton components such as
`PrimitiveSerializer<T>.Instance`, `EnumSerializer<T>.Instance` and
`StringSerializer.UTF8` directly where they are used. Do not cache their
singleton references in fields or local aliases.

Instantiate complex serializers when needed rather than exposing them as
singletons. A containing serializer can construct and own its complex component
serializers in readonly fields, preserving their registration/member hierarchy.
Pass the supplied context and reader/writer through to every component. Preserve
the agreed field order, enum representation, length framing and null/reference
semantics when replacing direct primitive IO.

Use nested component serializers for subordinate records instead of duplicating
their field encodings in each owning serializer. The factory and metadata codecs
construct their complex components and use the basic singleton components directly.

## Endian-Aware Bit Conversion

**Never** use `System.BitConverter`. Always use the framework's endian-aware equivalents:

- **[EndianBitConverter](../../../src/Sphere10.Framework/Conversion/EndianBitConverter.cs)** — abstract base with `LittleEndianBitConverter` and `BigEndianBitConverter` implementations. Select configured byte order through `EndianBitConverter.For(endianness)`. Use explicit `.Little` / `.Big` only when the byte contract fixes that order.
- **[EndianBinaryWriter](../../../src/Sphere10.Framework/IO/EndianBinaryWriter.cs)** / **[EndianBinaryReader](../../../src/Sphere10.Framework/IO/EndianBinaryReader.cs)** — endian-aware stream readers/writers used throughout serialization.
- All serialization infrastructure (`IItemSerializer<T>`, `ClusteredStreams`, `ObjectStream`) is endianness-aware. When writing new serializers or stream code, reuse the supplied reader/writer and context. Endianness comes from the reader/writer's `BitConverter`; [SerializationContext](../../../src/Sphere10.Framework/Serialization/SerialiationContext.cs) does not expose an `Endianness` property.

Storage openers receive byte order from their caller and pass it through to readers and component serializers. The ObjectSpace/ObjectStream helpers validate the recorded endian trait against that supplied order; they do not infer endianness by probing the magic value.

`EndianBinaryReader`, `EndianBinaryWriter` and `EncryptedStream` mirror the .NET `leaveOpen` convention. They close their underlying stream by default (`leaveOpen: false`); pass `leaveOpen: true` when a temporary helper borrows a stream whose lifetime belongs to its caller. Disposing a borrowed writer still flushes it. Framework containers and other owned abstract data types retain their positive `ownsStream` convention; pass `leaveOpen: !ownsStream` when adapting that choice to a .NET-style helper.

## Automatic assembly and reflection

Use `SerializerBuilder.FactoryAssemble<T>(factory)` for the existing factory-based automatic path. Its non-generic convenience overload is `FactoryAssemble(Type itemType, SerializerFactory factory)`; the overload accepting retention explicitly is `FactoryAssemble(SerializerFactory factory, Type itemType, bool retainAssembledTypeSerializersInFactory, long? typeCodeStart = null)`. The generic convenience overload uses a temporary copied factory. To retain assembled registrations in your factory, call `factory.RegisterAutoBuild<T>()` and then `factory.GetSerializer<T>()`.

The current `GetSerializableMembers` scans public instance fields/properties, uses the mapping member's `CanRead`/`CanWrite` flags, excludes `[Transient]`, and orders base members before derived members. Properties require public getters and setters. This scan does not filter indexers or distinguish readonly fields through `FieldMember.CanWrite`; reflected member access may therefore be unsuitable for a particular type. The automatic composite path also requires a compatible public parameterless constructor. Use explicit bindings and an activator where the reflected shape is unsuitable. See [PropertyMember](../../../src/Sphere10.Framework/Mapping/PropertyMember.cs), [FieldMember](../../../src/Sphere10.Framework/Mapping/FieldMember.cs) and the constructor configuration in `CompositeSerializer`.

Existing builder limitations remain explicit: `UseFactory` reads its unset `_factory` before assigning the argument, and `SerializeMembers` ignores the serializer returned by its missing-member callback. Automatic member selection also requires `_factory`. Explicit `Serialize(Member, serializer)` requires the member's declaring type to equal the configured item type, so it does not universally accept inherited member bindings. Do not recommend a `For<T>().UseFactory(factory).SerializeMembersAutomatically()` chain as a working alternative until these paths are repaired within an authorized task.

## Wire contracts and sizing

Use [`SerializerFactorySerializer`](../../usage/serializer-factory-serialization.md)
to persist a reconstructible factory's registrations, exact type codes and composite
member configuration. It supports ordinary registered constructors and automatic
composite/polymorphic assembly; arbitrary delegates and independently configured
serializer state require application-supplied configuration. The factory codec is
independent of the storage container using it.

Reflected automatic assembly does not establish a stable wire layout. Custom factory registrations, type codes, member layout, null/reference handling and endianness must match the storage or protocol contract. `CalculateSize(context, item)` must agree with the bytes written by `Serialize`. Reuse one context throughout the graph; convenience overloads without a context create a new one. Pure composite and polymorphic serializers report `SupportsNull=false`; use the existing null/reference wrappers when the contract requires them.

[SerializerBuilderTests](../../../tests/Sphere10.Framework.Tests/Serialization/SerializerBuilderTests.cs) provide explicit-binding, automatic-assembly, base-member ordering, constructor, null and cyclic-reference examples. They are references for the relevant behavior, not a promise that every reflected type can be serialized automatically.

See [fluent builders](fluent-builders.md) and [collections and storage](collections-and-storage.md) for the surrounding configuration and consumers.
