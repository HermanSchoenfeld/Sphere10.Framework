# Serialization

Binary item serializers for values, collections, tuples, references and polymorphic object graphs, with a type-code registry, a member-by-member builder and item hashers and checksummers.

`IItemSerializer<TItem>` writes an item to an `EndianBinaryWriter` and reads it back from an `EndianBinaryReader`. It extends the `IItemSizer<TItem>` contract from [Sizing](../Sizing/README.md), so every serializer also reports its constant or per-item byte size. Each call takes a `SerializationContext`, which tracks the objects already sized, written or read so that a `ReferenceSerializer<TItem>` can write back-references and cyclic graphs. The `IItemSerializerExtensions` overloads such as `SerializeBytesLE`, `SerializeToBytes`, `DeserializeBytesLE` and `DeserializeBytes` create and dispose a context for one call. Implement a serializer by deriving from `ItemSerializerBase<TItem>`, or from `ConstantSizeItemSerializerBase<TItem>` when every item has the same size. Wrap an existing one with `ItemSerializerDecorator<TItem>`.

`SerializerFactory` maps CLR types to serializers under persisted type codes. `SerializerFactory.Default` holds the built-in registrations and is read-only, so create `new SerializerFactory(SerializerFactory.Default)` to add your own with `Register`, `RegisterEnum` or `RegisterAutoBuild`. `GetSerializer<T>()` returns the registered serializer wrapped for null and reference handling, and `GetPureSerializer<T>()` returns it unwrapped. `RegisterAutoBuild` and `SerializerBuilder.FactoryAssemble` compose `CompositeSerializer<TItem>` instances for public read-write properties and fields, which needs a public parameterless constructor and skips members marked `[Transient]`. `SerializerBuilder.For<TItem>()` lets you choose each member's serializer. `PolymorphicSerializer<TItem>` writes the concrete type's serializer before the item so that derived types round-trip. `BinarySerializer` combines both for arbitrary objects and writes the type registrations it had to create into the stream.

`IItemHasher<TItem>` and `IItemChecksummer<TItem>` turn items into digests and 32-bit checksums. `ItemDigestor<TItem>` implements both by hashing an item's serialized bytes. The [Streams](../Streams/README.md) domain supplies the streams these serializers read and write, and the [Spans](../Spans/README.md) domain reads primitives directly from byte spans.

```csharp
var factory = new SerializerFactory(SerializerFactory.Default);
factory.RegisterAutoBuild<Person>();
var serializer = factory.GetSerializer<Person>();
var bytes = serializer.SerializeBytesLE(person);
var copy = serializer.DeserializeBytesLE(bytes);
```

## BinarySerializer and automatic assembly

`BinarySerializer` takes the place of `BinaryFormatter` for code that saves an object graph and loads it again. Its `Serialize(stream, item)` and `Deserialize(stream)` overloads match the calls being replaced. It has its own wire format, so it cannot read streams that `BinaryFormatter` wrote.

A type without a registered serializer is assembled from its public read-write properties and fields. This is the same rule that `RegisterAutoBuild` and `SerializerBuilder.FactoryAssemble` apply.

- A class needs a public parameterless constructor. A struct does not, provided it has at least one public read-write member.
- Private fields, get-only properties and properties with a non-public setter are not saved and come back with the value the constructor gives them. Members marked `[Transient]` are skipped. A type with a public read-write indexer cannot be serialized.
- Shared references and cycles are preserved, and a member typed as `object`, a base class or an interface gets back the concrete type it held.
- A graph nested more deeply than the thread's stack allows throws `InsufficientExecutionStackException`. A chain of about 1,600 linked objects fits the default 1 MB stack, and a thread with a larger stack reads and writes longer chains.

These .NET types round-trip on their own, as members and inside each other.

| Kind | Types |
|---|---|
| Values | `bool`, the integer and floating-point types, `decimal`, `string`, enums, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Nullable<T>`, `KeyValuePair<TKey, TValue>`, `Tuple` up to eight components and `ValueTuple` up to seven |
| Arrays | `byte[]`, `T[]` and jagged arrays |
| Lists and sets | `List<T>`, `Collection<T>`, `ObservableCollection<T>`, `ReadOnlyCollection<T>`, `ArrayList`, `LinkedList<T>`, `Queue<T>`, `Stack<T>`, `HashSet<T>`, `SortedSet<T>` |
| Dictionaries | `Dictionary<TKey, TValue>`, `SortedList<TKey, TValue>`, `SortedDictionary<TKey, TValue>` |
| Concurrent | `ConcurrentDictionary<TKey, TValue>`, `ConcurrentQueue<T>`, `ConcurrentStack<T>`, `ConcurrentBag<T>` |

Collections are rebuilt from their items. A queue keeps its dequeue order and a stack keeps its pop order, while a `ConcurrentBag<T>` has no order to keep. A custom comparer is not saved, so a dictionary, set or sorted collection comes back with the default comparer for its key type. A `ReadOnlyCollection<T>` comes back as a view over its own copy of the items. A class that derives from one of these collections is assembled from its members like any other class, which does not save its items. Multi-dimensional arrays such as `int[,]`, `char` values, delegates, streams and immutable types without public read-write members, such as `DateOnly`, `Uri` and `Version`, are not supported. A `DateTimeOffset` within a day of `DateTimeOffset.MinValue` or `MaxValue` may not come back exactly.

Every type in the table is a registration of `SerializerFactory.Default`, so `SerializerFactory.Default.GetSerializer<Queue<int>>()` resolves directly. Type codes 0 to 99,999 are reserved for these built-in registrations, and the first type you register in a factory copied from `Default` takes `SerializerFactory.UserTypeCodeStart` (100,000). A serializer added to the defaults later therefore never moves the code of one of your types in stored data. Data written before this reservation holds user type codes from 80 and cannot be read by later builds.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BinarySerializer`](BinarySerializer.cs) | class | Replacement for the deprecated .NET `BinaryFormatter` that writes the ad hoc type registrations it needed before the polymorphic, reference-tracked object. It saves public read-write members in its own wire format. |
| [`ConstantSizeItemSerializerBase<TItem>`](ConstantSizeItemSerializerBase.cs) | abstract class | Base for serializers whose items all serialize to the same byte count. |
| [`IItemSerializer`](IItemSerializer.cs) | interface | Untyped serializer contract, an `IItemSizer` that can also be invoked over `object` values. |
| [`IItemSerializer<TItem>`](IItemSerializer.cs) | interface | Typed contract that serializes to an `EndianBinaryWriter` and deserializes from an `EndianBinaryReader` within a `SerializationContext`. |
| [`IItemSerializerDecorator`](ItemSerializerDecorator.cs) | interface | Marks a serializer that wraps an inner serializer. |
| [`IItemSerializerExtensions`](IItemSerializer.cs) | static class | One-call `Serialize`, `SerializeBytesLE`, `SerializeToBytes`, `SerializeReturnSize`, `Deserialize`, `DeserializeBytesLE` and `DeserializeBytes` overloads that create their own context. |
| [`ItemDigestor<TItem>`](ItemDigestor.cs) | class | Serializer decorator that is also an item hasher and checksummer, hashing the serialized bytes with a chosen `CHF`. |
| [`ItemSerializer<TItem>`](ItemSerializer.cs) | static class | `Default` assembles a serializer for `TItem` from `SerializerFactory.Default` on every access. |
| [`ItemSerializerBase<TItem>`](ItemSerializerBase.cs) | abstract class | Base for variable-size serializers that implement `CalculateSize`, `Serialize` and `Deserialize`. |
| [`ItemSerializerDecorator<TItem>`](ItemSerializerDecorator.cs) | class | Decorator over an `IItemSerializer<TItem>` that forwards every member. |
| [`ItemSerializerDecorator<TItem, TSerializer>`](ItemSerializerDecorator.cs) | class | Decorator over a specific serializer type whose members are virtual for subclasses to override. |
| [`SerializationContext`](SerialiationContext.cs) | class | Disposable per-operation state that tracks sized, serialized and deserialized objects, an ephemeral factory and external-reference hooks. |

### Annotations

| Type | Kind | Purpose |
|---|---|---|
| [`KnownSubTypeAttribute`](Annotations/KnownSubTypeAttribute.cs) | class | Declares a sub-type of a non-sealed class or struct so serializer assembly can register it too. |
| [`KnownSubTypeAttribute<T>`](Annotations/KnownSubTypeAttribute.cs) | class | Generic form of `KnownSubTypeAttribute` for sub-type `T`. |
| [`ReferenceModeAttribute`](Annotations/ReferenceModeAttribute.cs) | class | Sets a member's null, context-reference and external-reference handling for automatic member serialization. |
| [`TransientAttribute`](Annotations/TransientAttribute.cs) | class | Excludes a property or field from automatic member serialization. |

### BaseTypes

| Type | Kind | Purpose |
|---|---|---|
| [`AssemblySerializer`](BaseTypes/AssemblySerializer.cs) | class | Serializes an `Assembly` as its full name and reloads it with `Assembly.Load`. |
| [`AssemblyTypeSerializer`](BaseTypes/AssemblyTypeSerializer.cs) | class | Serializes a `Type` from one known assembly as its full name only. |
| [`ConstantSizeArraySerializer<T>`](BaseTypes/ConstantSizeArraySerializer.cs) | class | Fixed-length array of constant-size items, written without a length prefix. |
| [`ConstantSizeByteArraySerializer`](BaseTypes/ConstantSizeByteArraySerializer.cs) | class | Byte array of one fixed size, written without a length prefix. |
| [`ConstantSizeNullableByteArraySerializer`](BaseTypes/ConstantSizeNullableByteArraySerializer.cs) | class | Fixed-size nullable byte array written as a 1-byte null flag followed by the data or padding. |
| [`CVarIntSerializer`](BaseTypes/CVarIntSerializer.cs) | class | Serializes a `CVarInt` compact variable-length integer. |
| [`DateTimeOffsetSerializer`](BaseTypes/DateTimeOffsetSerializer.cs) | class | Constant-size `DateTimeOffset` written as its local date and time followed by its offset. |
| [`DateTimeSerializer`](BaseTypes/DateTimeSerializer.cs) | class | 8-byte `DateTime` written with `ToBinary()`. |
| [`EnumSerializer<T>`](BaseTypes/EnumSerializer.cs) | class | Constant-size enum serializer that writes the value as its underlying integer type. |
| [`GuidSerializer`](BaseTypes/GuidSerializer.cs) | class | 16-byte `Guid` serializer. |
| [`IKeyValuePairSerializer<TKey, TValue>`](BaseTypes/IKeyValuePairSerializer.cs) | interface | Key-value pair serializer that can read only the key, only the value, or their raw bytes. |
| [`KeyValuePairSerializer<TKey, TValue>`](BaseTypes/KeyValuePairSerializer.cs) | class | Writes a `KeyValuePair` as size-prefixed key and value in one shared context. |
| [`ObjectSerializer`](BaseTypes/ObjectSerializer.cs) | class | Reference-tracked polymorphic serializer for `object` values over a given factory. |
| [`PrimitiveSerializer<T>`](BaseTypes/PrimitiveSerializer.cs) | class | Constant-size serializer for the .NET primitive types, chosen by `TypeCode`. |
| [`PureObjectSerializer`](BaseTypes/PureObjectSerializer.cs) | class | Zero-byte serializer for a plain `object`, which deserializes to `new object()`. |
| [`StringSerializer`](BaseTypes/StringSerializer.cs) | class | Length-prefixed string in a chosen `Encoding`, with an optional maximum byte count and shared `UTF8`, `ASCII` and other instances. |
| [`TimeSpanSerializer`](BaseTypes/TimeSpanSerializer.cs) | class | 8-byte `TimeSpan` serializer. |
| [`TypeSerializer`](BaseTypes/TypeSerializer.cs) | class | Serializes a `Type` as its assembly-qualified name and resolves it with `Type.GetType`. |
| [`VarIntSerializer`](BaseTypes/VarIntSerializer.cs) | class | Serializes a `VarInt` variable-length integer. |

### Builder

| Type | Kind | Purpose |
|---|---|---|
| [`CompositeSerializer`](Builder/CompositeSerializer.cs) | static class | Creates and configures `CompositeSerializer<TItem>` instances for a run-time type. |
| [`CompositeSerializer<TItem>`](Builder/CompositeSerializer.cs) | class | Serializes an object as the ordered serializations of its bound members and activates new instances on deserialization. |
| [`MemberSerializationBinding`](Builder/MemberSerializationBinding.cs) | record | Pairs a member with the serializer used for it. |
| [`SerializerBuilder`](Builder/SerializerBuilder.cs) | abstract class | Untyped builder with `For<TItem>()`, `FactoryAssemble` for automatic serializer graphs, and `GetSerializableMembers`. |
| [`SerializerBuilder<TItem>`](Builder/SerializerBuilder.cs) | class | Fluent builder that binds each member of `TItem` to a serializer, ignores members, or fills the rest from a factory, then builds a composite serializer. |

### Checksum

| Type | Kind | Purpose |
|---|---|---|
| [`ActionChecksum<TItem>`](Checksum/ActionChecksum.cs) | class | Checksummer that calls a `Func<TItem, int>` delegate. |
| [`DecoratorExtensions`](Checksum/DecoratorExtensions.cs) | static class | Partial class part that adds `WithSubstitution` for checksummers and `AsProjection` for serializers. |
| [`HashChecksummer`](Checksum/HashChecksummer.cs) | class | Checksum of a hash digest, read as its first four bytes in little-endian order. |
| [`IItemChecksummer<TItem>`](Checksum/IItemChecksummer.cs) | interface | Calculates a 32-bit checksum for an item. |
| [`ItemChecksummerDecorator<TItem>`](Checksum/ItemChecksummerDecorator.cs) | class | Decorator over an `IItemChecksummer<TItem>`. |
| [`ItemChecksummerDecorator<TItem, TInner>`](Checksum/ItemChecksummerDecorator.cs) | class | Decorator over a specific checksummer type with a virtual `CalculateChecksum`. |
| [`ObjectHashCodeChecksummer<T>`](Checksum/ObjectHashCodeChecksummer.cs) | class | Uses the object's identity hash code from `RuntimeHelpers.GetHashCode` as its checksum. |
| [`ProjectionChecksummer<TItem, TMember>`](Checksum/MemberExpressionChecksummer.cs) | class | Checksums a value projected from the item with another checksummer. |
| [`WithSubstitutionChecksummer<TItem>`](Checksum/WithSubstitutionChecksummer.cs) | class | Replaces a reserved checksum value with a substitute so the reserved value never occurs. |

### Collections

| Type | Kind | Purpose |
|---|---|---|
| [`ArrayListSerializer`](Collections/ArrayListSerializer.cs) | class | Count-prefixed `ArrayList` whose elements use a factory's object serializer. |
| [`ArraySerializer<T>`](Collections/ArraySerializer.cs) | class | Count-prefixed `T[]` serializer that can register the array before its elements for cyclic references. |
| [`ByteArraySerializer`](Collections/ByteArraySerializer.cs) | class | Length-prefixed `byte[]` serializer. |
| [`CollectionSerializer<T>`](Collections/CollectionSerializer.cs) | class | Count-prefixed `Collection<T>` serializer. |
| [`CollectionSerializerBase<TCollection, TItem>`](Collections/CollectionSerializerBase.cs) | abstract class | Base for count-prefixed collection serializers that implement `GetLength`, `Activate` and `SetItem`, with an optional `OnItemsSet` step after the last item. |
| [`ConcurrentBagSerializer<T>`](Collections/ConcurrentBagSerializer.cs) | class | Count-prefixed `ConcurrentBag<T>` serializer, which does not preserve item order. |
| [`ConcurrentDictionarySerializer<TKey, TValue>`](Collections/DictionarySerializers.cs) | class | `ConcurrentDictionary<TKey, TValue>` written as a count followed by key-value pairs. |
| [`ConcurrentQueueSerializer<T>`](Collections/ConcurrentQueueSerializer.cs) | class | Count-prefixed `ConcurrentQueue<T>` serializer that preserves dequeue order. |
| [`ConcurrentStackSerializer<T>`](Collections/ConcurrentStackSerializer.cs) | class | Count-prefixed `ConcurrentStack<T>` serializer that preserves pop order. |
| [`DictionarySerializer<TKey, TValue>`](Collections/DictionarySerializers.cs) | class | `Dictionary<TKey, TValue>` written as a count followed by key-value pairs. |
| [`EnumerableSerializer<T>`](Collections/EnumerableSerializer.cs) | class | Writes any `IEnumerable<T>` with its count and reads it back as an array. |
| [`ExtendedListSerializer<T>`](Collections/ExtendedListSerializers.cs) | class | Count-prefixed `ExtendedList<T>` serializer. |
| [`HashSetSerializer<TItem>`](Collections/HashSetSerializer.cs) | class | Count-prefixed `HashSet<TItem>` serializer. |
| [`LinkedListSerializer<T>`](Collections/LinkedListSerializer.cs) | class | Count-prefixed `LinkedList<T>` serializer that preserves node order. |
| [`ListSerializer<T>`](Collections/ListSerializer.cs) | class | Count-prefixed `List<T>` serializer. |
| [`ObservableCollectionSerializer<T>`](Collections/ObservableCollectionSerializer.cs) | class | Count-prefixed serializer for the .NET `System.Collections.ObjectModel.ObservableCollection<T>`. |
| [`QueueSerializer<T>`](Collections/QueueSerializer.cs) | class | Count-prefixed `Queue<T>` serializer that preserves dequeue order. |
| [`ReadOnlyCollectionSerializer<T>`](Collections/ReadOnlyCollectionSerializer.cs) | class | Writes a `ReadOnlyCollection<T>` as an array of its items and reads it back as a read-only view of that array. |
| [`SortedDictionarySerializer<TKey, TValue>`](Collections/DictionarySerializers.cs) | class | `SortedDictionary<TKey, TValue>` written as a count followed by key-value pairs. |
| [`SortedListSerializer<TKey, TValue>`](Collections/DictionarySerializers.cs) | class | `SortedList<TKey, TValue>` written as a count followed by key-value pairs. |
| [`SortedSetSerializer<TItem>`](Collections/SortedSetSerializer.cs) | class | Count-prefixed `SortedSet<TItem>` serializer. |
| [`StackSerializer<T>`](Collections/StackSerializer.cs) | class | Count-prefixed `Stack<T>` serializer that preserves pop order. |

### Factory

| Type | Kind | Purpose |
|---|---|---|
| [`SerializerFactory`](Factory/SerializerFactory.cs) | class | Registry that maps types to serializers under persisted type codes, resolves generic and nested serializers, and provides the read-only `Default` factory. |
| [`SerializerFactorySerializer`](Factory/SerializerFactorySerializer.cs) | class | Serializes a factory's type codes, registrations and serializer construction settings so it can be rebuilt. |
| [`VersionedSerializers`](Factory/VersionedSerializers.cs) | class | Maps version numbers to `SerializerFactory` instances for versioned formats. |

### Hashers

| Type | Kind | Purpose |
|---|---|---|
| [`ActionHasher<T>`](Hashers/ActionHasher.cs) | class | Hasher that calls a `Func<T, byte[]>` delegate with a declared digest length. |
| [`IItemHasher<TItem>`](Hashers/IItemHasher.cs) | interface | Hashes an item to a digest of a fixed `DigestLength`. |
| [`ItemHasherDecorator<TItem>`](Hashers/ItemHasherDecorator.cs) | class | Decorator over an `IItemHasher<TItem>`. |
| [`ItemHasherDecorator<TItem, TItemHasher>`](Hashers/ItemHasherDecorator.cs) | class | Decorator over a specific hasher type with a virtual `Hash`. |
| [`ItemHasherExtensions`](Hashers/ItemHasherExtensions.cs) | static class | `WithNullHash` overloads that make a hasher return a fixed digest for `null` items. |
| [`ProjectionHasher<TItem, TProjection>`](Hashers/ProjectionHasher.cs) | class | Hashes a value projected from the item with another hasher. |

### Nullable

| Type | Kind | Purpose |
|---|---|---|
| [`NullableSerializer<T>`](Nullable/NullableSerializer.cs) | class | Serializes `T?` as a presence flag followed by the value, optionally padded to a constant size. |

### Special

| Type | Kind | Purpose |
|---|---|---|
| [`ActionSerializer<T>`](Special/ActionSerializer.cs) | class | Serializer built from sizing, writing and reading delegates. |
| [`CastedSerializer<TFrom, TTo>`](Special/CastedSerializer.cs) | class | Exposes a `TFrom` serializer as a `TTo` serializer by casting items. |
| [`ConstantSizeCompoundKeySerializer<TKey1, TKey2>`](Special/ConstantSizeCompoundKeySerializer.cs) | class | Constant-size `CompoundKey<TKey1, TKey2>` written as its concatenated component serializations. |
| [`ConstantSizeCompoundKeySerializer<TKey1, TKey2, TKey3>`](Special/ConstantSizeCompoundKeySerializer.cs) | class | Three-component form of the constant-size compound key serializer. |
| [`ConstantSizeCompoundKeySerializer<TKey1, TKey2, TKey3, TKey4>`](Special/ConstantSizeCompoundKeySerializer.cs) | class | Four-component form of the constant-size compound key serializer. |
| [`ConstantSizeKeyValuePairSerializer<TKey, TValue>`](Special/ConstantSizeKeyValuePairSerializer.cs) | class | Constant-size `KeyValuePair` written as concatenated constant-size key and value. |
| [`ConstantSizeReferenceTypeSerializer<TItem>`](Special/ConstantSizeReferenceTypeSerializer.cs) | class | Pads a `ReferenceSerializer<TItem>` over a constant-size serializer so that null items take the same space. |
| [`ContextSegregatedKeyValuePairSerializer<TKey, TValue>`](Special/ContextSegregatedKeyValuePairSerializer.cs) | class | Size-prefixed key and value serialized in separate contexts so that each can be read on its own. |
| [`FastMarshalStructSerializer<T>`](Special/FastMarshalStructSerializer.cs) | class | Copies a blittable struct's unmanaged memory with `Marshal`, unsuitable for structs with reference fields. |
| [`IValueTypeActivatingSerializer`](Special/IValueTypeActivatingSerializer.cs) | interface | Serializer that constructs instances while deserializing and can register them early for cyclic references. |
| [`PolymorphicSerializer`](Special/PolymorphicSerializer.cs) | static class | Creates a `PolymorphicSerializer<TItem>` for a run-time type. |
| [`PolymorphicSerializer<TItem>`](Special/PolymorphicSerializer.cs) | class | Writes a reference to the concrete type's serializer before the item so that sub-types round-trip. |
| [`ProjectedSerializer<TFrom, TTo>`](Special/ProjectedSerializer.cs) | class | Serializes `TTo` values through a `TFrom` serializer with a projection and its inverse. |
| [`ReferenceSerializer<TItem>`](Special/ReferenceSerializer.cs) | class | Prefixes each item with a byte that marks null, a new value, a back-reference to an object in the context, or an external reference. |
| [`ReferenceSerializerMode`](Special/ReferenceSerializerMode.cs) | enum | Flags that enable null values, context references and external references, all enabled by `Default`. |
| [`SizeDescriptorSerializer`](Special/SizeDescriptorSerializer.cs) | class | Writes and reads length prefixes with a `SizeDescriptorStrategy`, CVarInt by default. |
| [`TaggedTypeCollectionSerializer<TTag>`](Special/TaggedTypeCollectionSerializer.cs) | class | Serializes (type, tag) pairs grouped by assembly, without preserving their order. |
| [`TupleSerializer<T1>`](Special/TupleSerializer.cs) | class | Serializes `Tuple<T1>` with one component serializer. |
| [`TupleSerializer<T1, T2>`](Special/TupleSerializer.cs) | class | Serializes `Tuple<T1, T2>` component by component. |
| [`TupleSerializer<T1, T2, T3>`](Special/TupleSerializer.cs) | class | Serializes `Tuple<T1, T2, T3>` component by component. |
| [`TupleSerializer<T1, T2, T3, T4>`](Special/TupleSerializer.cs) | class | Serializes `Tuple<T1, T2, T3, T4>` component by component. |
| [`TupleSerializer<T1, T2, T3, T4, T5>`](Special/TupleSerializer.cs) | class | Serializes `Tuple<T1, T2, T3, T4, T5>` component by component. |
| [`TupleSerializer<T1, T2, T3, T4, T5, T6>`](Special/TupleSerializer.cs) | class | Serializes `Tuple<T1, T2, T3, T4, T5, T6>` component by component. |
| [`TupleSerializer<T1, T2, T3, T4, T5, T6, T7>`](Special/TupleSerializer.cs) | class | Serializes `Tuple<T1, T2, T3, T4, T5, T6, T7>` component by component. |
| [`TupleSerializer<T1, T2, T3, T4, T5, T6, T7, TRest>`](Special/TupleSerializer.cs) | class | Serializes an eight-component `Tuple` with its `TRest` tail. |
| [`TypeCollectionSerializer`](Special/TypeCollectionSerializer.cs) | class | Serializes a collection of types grouped by assembly. |
| [`ValueTupleSerializer<T1>`](Special/ValueTupleSerializer.cs) | class | Serializes `ValueTuple<T1>` with one component serializer. |
| [`ValueTupleSerializer<T1, T2>`](Special/ValueTupleSerializer.cs) | class | Serializes `(T1, T2)` component by component. |
| [`ValueTupleSerializer<T1, T2, T3>`](Special/ValueTupleSerializer.cs) | class | Serializes `(T1, T2, T3)` component by component. |
| [`ValueTupleSerializer<T1, T2, T3, T4>`](Special/ValueTupleSerializer.cs) | class | Serializes `(T1, T2, T3, T4)` component by component. |
| [`ValueTupleSerializer<T1, T2, T3, T4, T5>`](Special/ValueTupleSerializer.cs) | class | Serializes `(T1, T2, T3, T4, T5)` component by component. |
| [`ValueTupleSerializer<T1, T2, T3, T4, T5, T6>`](Special/ValueTupleSerializer.cs) | class | Serializes `(T1, T2, T3, T4, T5, T6)` component by component. |
| [`ValueTupleSerializer<T1, T2, T3, T4, T5, T6, T7>`](Special/ValueTupleSerializer.cs) | class | Serializes `(T1, T2, T3, T4, T5, T6, T7)` component by component. |
| [`ValueTupleSerializer<T1, T2, T3, T4, T5, T6, T7, TRest>`](Special/ValueTupleSerializer.cs) | class | Serializes an eight-component `ValueTuple` with its `TRest` tail. |
| [`VoidSerializer`](Special/VoidSerializer.cs) | class | Zero-byte serializer for the `Void` placeholder type. |
