# Collections

Provides long-indexed extended lists and collections, B-trees, paged, file-mapped and stream-mapped storage, and observable, synchronized and projected wrappers over lists, dictionaries, sets, queues and stacks.

The central contracts are `IExtendedCollection<T>` and `IExtendedList<T>`. They use 64-bit counts and indices and add batch members such as `ReadRange`, `UpdateRange`, `InsertRange`, `RemoveRange` and `ContainsRange`. A new list derives from `RangedListBase<T>` when its storage works best in batches, or from `SingularListBase<T>` when it works item by item. Each base implements the other half of the contract from the half the subclass supplies. Decorators such as `ExtendedListDecorator<TItem, TConcrete>` add behaviour to an existing list, and the observable, synchronized, bounded, stack and projected types are built that way. Most generic decorators come as a pair, a `TConcrete` form that keeps the inner type and a shortcut form over the interface.

The storage types differ in where items live. `ExtendedList<T>` keeps them in one array. `MemoryPagedList<TItem>` and `MemoryPagedBuffer` keep a bounded number of pages in memory and swap the rest to temporary files, and `FileMappedBuffer` pages over a single file. `StreamPagedList<TItem>` writes pages to a stream and grows or shrinks only at the end. `StreamMappedFactory` creates lists, recyclable lists, dictionaries and hash sets over an object stream on clustered streams, described in [ObjectStream](../ObjectStream/README.md) and [ClusteredStreams](../ClusteredStreams/README.md), with item serializers from [Serialization](../Serialization/README.md). Paged and stream-mapped collections over existing data report `RequiresLoad` and must be loaded before use unless they were created with `autoLoad`. `StreamMappedBTree<K, V>`, `StreamMappedBTreePlus<K, V>` and `StreamMappedBTreeLookup<K, V>` store sorted entries of constant-size keys and values directly in a seekable stream. Reopen an existing tree with its original order and serializers, and dispose it so its header records the final count.

Related domains supply the parts these types depend on. [Threading](../Threading/README.md) provides the locks behind the synchronized types, [Events](../Events/README.md) provides the `EventTraits` filter of the observable types, and [Cache](../Cache/README.md) provides the page cache of the memory-paged lists. [Keys](../Keys/README.md) defines the `CompoundKey` types that B-tree lookups store, and [Transactions](../Transactions/README.md) builds transactional file-mapped lists on `FilePagedListBase<TItem>`.

```csharp
var list = new ExtendedList<int>();
list.AddRange(new[] { 1, 2, 3, 4, 5 });
var middle = list.ReadRange(1, 3).ToArray(); // 2, 3, 4
list.RemoveRange(3, 2);                      // 1, 2, 3
long count = list.Count;                     // 3
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Tools.Collection`](CollectionTool.cs) | tools | Compares keyed snapshots (`CompareByKey`), generates and repeats sequences, partitions counts, binary-searches lists and resizes arrays within the 32-bit addressing limit. |

### Arrays

| Type | Kind | Purpose |
|---|---|---|
| [`ByteArrayBuilder`](Arrays/ByteArrayBuilder.cs) | class | Growable byte buffer that appends single bytes or spans and returns the result with `ToArray()`. |
| [`Tools.Array`](Arrays/ArrayTool.cs) | tools | Array helpers that transpose jagged arrays, concatenate spans into new arrays, fill, clone, reverse, pop the head and pick a random element. |

### Bit

| Type | Kind | Purpose |
|---|---|---|
| [`BitVector`](Bit/BitVector.cs) | class | Stream-backed list of bits that replaces `BitArray`, appends and removes only at the end and offers fast bit-value search helpers. |

### BTree

| Type | Kind | Purpose |
|---|---|---|
| [`BTree<K, V, TNode>`](BTree/BTree.cs) | abstract class | Classic B-tree algorithm (entries in every node) over abstract node-storage hooks, so subclasses choose in-memory or stream storage. |
| [`BTreeBase<K, V>`](BTree/BTreeBase.cs) | abstract class | Shared `IBTree<K, V>` dictionary surface for every tree variant, with order and key comparer validation. |
| [`BTreeLookup<K, V>`](BTree/BTreeLookup.cs) | class | One-to-many lookup over any `IBTree<CompoundKey<K, int>, V>`, storing each value under its key and a group index. |
| [`BTreePlus<K, V, TNode>`](BTree/BTreePlus.cs) | abstract class | B+ tree algorithm with entries only in linked leaves, over abstract node-storage hooks. |
| [`BTreeSpec`](BTree/BTreeSpec.cs) | record | Pairs a `BTreeType` with an order of at least three, with `Default` taken from the framework defaults. |
| [`BTreeType`](BTree/BTreeType.cs) | enum | Selects the `BTree` or `BTreePlus` node layout. |
| [`IBTree<K, V>`](BTree/IBTree.cs) | interface | Sorted dictionary contract with `Set` (optional overwrite), inclusive `FindRange`, `Validate` and a configurable traversal order. |
| [`IBTreeLookup<K, V>`](BTree/IBTreeLookup.cs) | interface | Mutable `ILookup<K, V>` with `Add`, `Remove`, `RemoveAll`, `ContainsEntry`, `Clear` and tree validation. |
| [`IExtendedBTreeLookup<K, V>`](BTree/IExtendedBTreeLookup.cs) | interface | Adds `ContainsKey`, `GetValues`, `EnumerateGroupings` and the distinct key count to `IBTreeLookup<K, V>`. |
| [`InMemoryBTree<K, V>`](BTree/InMemoryBTree.cs) | class | In-memory B-tree whose nodes are objects holding key and child lists. |
| [`InMemoryBTreeLookup<K, V>`](BTree/InMemoryBTreeLookup.cs) | class | In-memory one-to-many lookup built on an in-memory B-tree or B+ tree chosen by a `BTreeSpec`. |
| [`InMemoryBTreePlus<K, V>`](BTree/InMemoryBTreePlus.cs) | class | In-memory B+ tree whose leaves hold entries and link to the next leaf. |

### Buffer

| Type | Kind | Purpose |
|---|---|---|
| [`BufferAdapter`](Buffer/BufferAdapter.cs) | class | Exposes an `IList<byte>` as an `IBuffer`, with span reads and writes copied through the list. |
| [`BufferDecorator<TBuffer>`](Buffer/BufferDecorator.cs) | abstract class | Base decorator that forwards every `IBuffer` member, including span operations, to an inner buffer. |
| [`IBuffer`](Buffer/IBuffer.cs) | interface | Byte `IExtendedList` with span reads, writes and inserts, `AsSpan` and `ExpandTo`/`ExpandBy` growth. |
| [`IBufferExtensions`](Buffer/IBuffer.cs) | static class | Adds `Range` and `Index` overloads of `ReadSpan` and `AsSpan` to `IBuffer`. |
| [`IBufferPage`](Buffer/IBufferPage.cs) | interface | Memory page of bytes with span read, update and append-with-overflow operations, used by paged buffers. |
| [`MemoryBuffer`](Buffer/MemoryBuffer.cs) | class | Byte buffer held in one contiguous array that grows in blocks up to a maximum capacity, for small to medium data. |

### Collections

| Type | Kind | Purpose |
|---|---|---|
| [`CollectionConflictPolicy`](Collections/CollectionConflictPolicy.cs) | enum | Chooses `Throw`, `Override` or `Skip` when a bulk add meets an existing key. |
| [`CollectionDecorator<TItem>`](Collections/CollectionDecorator.cs) | abstract class | Shortcut of `CollectionDecorator<TItem, TConcrete>` for an inner `ICollection<TItem>`. |
| [`CollectionDecorator<TItem, TConcrete>`](Collections/CollectionDecorator.cs) | abstract class | Base decorator that forwards every `ICollection<T>` member to a typed inner collection. |
| [`ConcatenatedCollection<T>`](Collections/ConcatenatedCollection.cs) | class | Read-only view that counts, searches and enumerates two collections as one. |
| [`EnumerableCollectionAdapter<TVal>`](Collections/EnumerableCollectionAdapter.cs) | class | Read-only `ICollection<T>` whose enumeration, count and membership come from delegates. |
| [`ExtendedCollectionAdapter<T>`](Collections/ExtendedCollectionAdapter.cs) | class | Exposes an `ICollection<T>` as an `IExtendedCollection<T>` by looping its range methods over single-item calls. |
| [`ExtendedCollectionBase<T>`](Collections/ExtendedCollectionBase.cs) | abstract class | Abstract `IExtendedCollection<T>` with a version counter that detects changes during enumeration. |
| [`ExtendedCollectionDecorator<TItem>`](Collections/ExtendedCollectionDecorator.cs) | abstract class | Shortcut of `ExtendedCollectionDecorator<TItem, TConcrete>` for an inner `IExtendedCollection<TItem>`. |
| [`ExtendedCollectionDecorator<TItem, TConcrete>`](Collections/ExtendedCollectionDecorator.cs) | abstract class | Base decorator that forwards the long count and range members of an inner `IExtendedCollection<T>`. |
| [`IExtendedCollection<T>`](Collections/IExtendedCollection.cs) | interface | Collection with a 64-bit `Count` that joins the read-only and write-only extended contracts with `ICollection<T>`. |
| [`IReadOnlyExtendedCollection<T>`](Collections/IReadOnlyExtendedCollection.cs) | interface | Read side of an extended collection with `Contains`, batch `ContainsRange` and `CopyTo`. |
| [`IWriteOnlyExtendedCollection<in T>`](Collections/IWriteOnlyExtendedCollection.cs) | interface | Write side of an extended collection with `Add`, `AddRange`, `Remove`, batch `RemoveRange` and `Clear`. |
| [`IWriteOnlyExtendedCollectionExtensions`](Collections/IWriteOnlyExtendedCollection.cs) | static class | Adds a `params` overload of `AddRange` to `IWriteOnlyExtendedCollection<T>`. |
| [`LargeCollection<TItem>`](Collections/LargeCollection.cs) | class | Disposable collection backed by a `MemoryPagedList<TItem>` that keeps a bounded number of pages in memory and raises page events. |
| [`ProjectedCollection<T, TProjection>`](Collections/ProjectedCollection.cs) | class | Presents an `ICollection<T>` as `ICollection<TProjection>` through a projection and its inverse. |
| [`RangedCollectionBase<T>`](Collections/RangedCollectionBase.cs) | abstract class | Extended collection base that implements single-item members through the range members subclasses provide. |
| [`ReadOnlyCollectionBase<T>`](Collections/ReadOnlyCollectionBase.cs) | abstract class | Base for read-only `ICollection<T>` types whose mutators throw `InvalidOperationException`. |
| [`SingularCollectionBase<T>`](Collections/SingularCollectionBase.cs) | abstract class | Extended collection base that implements the range members by looping over single-item members. |

### Columnar

| Type | Kind | Purpose |
|---|---|---|
| [`ColumnarList`](Columnar/ColumnarList.cs) | class | List of `object[]` rows whose fields are stored in separate column lists, one `IExtendedList<object>` per column. |

### Comparers

| Type | Kind | Purpose |
|---|---|---|
| [`CompoundKeyComparer<TKey1, TKey2>`](Comparers/CompoundKeyComparer.cs) | class | Orders two-part `CompoundKey` values by the first component, then the second. |
| [`CompoundKeyComparer<TKey1, TKey2, TKey3>`](Comparers/CompoundKeyComparer.cs) | class | Orders three-part compound keys lexicographically with one comparer per component. |
| [`CompoundKeyComparer<TKey1, TKey2, TKey3, TKey4>`](Comparers/CompoundKeyComparer.cs) | class | Orders four-part compound keys lexicographically with one comparer per component. |
| [`KeyValuePairComparer<TKey, TValue>`](Comparers/KeyValuePairComparer.cs) | class | Orders key/value pairs by key, then by value. |

### Dictionaries

| Type | Kind | Purpose |
|---|---|---|
| [`ActionDictionaryDecorator<TKey, TValue, TConcrete>`](Dictionaries/ActionDictionaryDecorator.cs) | class | Dictionary decorator that runs an enter action before and an exit action after every member call. |
| [`BijectiveDictionary<U, V>`](Dictionaries/BijectiveDictionary.cs) | class | One-to-one dictionary that keeps an inverse dictionary in step, exposed through `Bijection`, `ContainsValue` and `TryGetKey`. |
| [`DictionaryBase<TKey, TValue>`](Dictionaries/DictionaryBase.cs) | abstract class | Extended-collection base for dictionaries that supplies the indexer, `Keys`, `Values` and batch members from a few abstract operations. |
| [`DictionaryChain<TKey, TValue>`](Dictionaries/DictionaryChain.cs) | class | Chain of dictionaries where lookups fall through from head to tail and heads can be attached or detached. |
| [`DictionaryDecorator<TKey, TValue>`](Dictionaries/DictionaryDecorator.cs) | abstract class | Shortcut of `DictionaryDecorator<TKey, TValue, TDictionary>` for an inner `IDictionary<TKey, TValue>`. |
| [`DictionaryDecorator<TKey, TValue, TDictionary>`](Dictionaries/DictionaryDecorator.cs) | abstract class | Base decorator that forwards every `IDictionary<TKey, TValue>` member to a typed inner dictionary. |
| [`DictionaryList<TKey, TVal>`](Dictionaries/DictionaryList.cs) | class | Dictionary that also stores values in insertion order, so they can be read by position and keys mapped to an index. |
| [`EnumerableKeyDictionary<K, V>`](Dictionaries/EnumerableKeyDictionary.cs) | class | Dictionary keyed by sequences of `K`, stored as a tree of nested nodes with `params` key overloads. |
| [`IBijectiveDictionary<U, V>`](Dictionaries/IBijectiveDictionary.cs) | interface | Dictionary contract with an inverse view, `ContainsValue` and `TryGetKey`. |
| [`IPersistedDictionary<T1, T2>`](Dictionaries/IPersistedDictionary.cs) | interface | Dictionary that can `Load`, `Save` and `Delete` its backing store. |
| [`IReadOnlyDictionaryList<TKey, TValue>`](Dictionaries/IReadOnlyDictionaryList.cs) | interface | Read-only dictionary that is also a positional list of values with `IndexOf(key)`. |
| [`MultiKeyDictionary<K1, K2, V>`](Dictionaries/MultiKeyDictionary.cs) | class | Nested dictionary addressed by 2 keys, with a 2-key indexer, `Add`, `ContainsKey`, `TryGetValue` and tuple `Keys`. |
| [`MultiKeyDictionary<K1, K2, K3, V>`](Dictionaries/MultiKeyDictionary.cs) | class | Nested dictionary addressed by 3 keys, with a 3-key indexer, `Add`, `ContainsKey`, `TryGetValue` and tuple `Keys`. |
| [`MultiKeyDictionary<K1, K2, K3, K4, V>`](Dictionaries/MultiKeyDictionary.cs) | class | Nested dictionary addressed by 4 keys, with a 4-key indexer, `Add`, `ContainsKey`, `TryGetValue` and tuple `Keys`. |
| [`MultiKeyDictionary<K1, K2, K3, K4, K5, V>`](Dictionaries/MultiKeyDictionary.cs) | class | Nested dictionary addressed by 5 keys, with a 5-key indexer, `Add`, `ContainsKey`, `TryGetValue` and tuple `Keys`. |
| [`MultiKeyDictionary<K1, K2, K3, K4, K5, K6, V>`](Dictionaries/MultiKeyDictionary.cs) | class | Nested dictionary addressed by 6 keys, with a 6-key indexer, `Add`, `ContainsKey`, `TryGetValue` and tuple `Keys`. |
| [`MultiKeyDictionary<K1, K2, K3, K4, K5, K6, K7, V>`](Dictionaries/MultiKeyDictionary.cs) | class | Nested dictionary addressed by 7 keys, with a 7-key indexer, `Add`, `ContainsKey`, `TryGetValue` and tuple `Keys`. |
| [`MultiKeyDictionary<K1, K2, K3, K4, K5, K6, K7, K8, V>`](Dictionaries/MultiKeyDictionary.cs) | class | Nested dictionary addressed by 8 keys, with a 8-key indexer, `Add`, `ContainsKey`, `TryGetValue` and tuple `Keys`. |
| [`MultiKeyDictionary<K1, K2, K3, K4, K5, K6, K7, K8, K9, V>`](Dictionaries/MultiKeyDictionary.cs) | class | Nested dictionary addressed by 9 keys, with a 9-key indexer, `Add`, `ContainsKey`, `TryGetValue` and tuple `Keys`. |
| [`MultiKeyDictionary<K1, K2, K3, K4, K5, K6, K7, K8, K9, K10, V>`](Dictionaries/MultiKeyDictionary.cs) | class | Nested dictionary addressed by 10 keys, with a 10-key indexer, `Add`, `ContainsKey`, `TryGetValue` and tuple `Keys`. |
| [`MultiKeyDictionary<K1, K2, K3, K4, K5, K6, K7, K8, K9, K10, K11, V>`](Dictionaries/MultiKeyDictionary.cs) | class | Nested dictionary addressed by 11 keys, with a 11-key indexer, `Add`, `ContainsKey`, `TryGetValue` and tuple `Keys`. |
| [`MultiKeyMap<K, V>`](Dictionaries/MultiKeyMap.cs) | class | Shortcut of `MultiKeyMap<K, V, C>` that stores values in a `List<V>`. |
| [`MultiKeyMap<K, V, C>`](Dictionaries/MultiKeyMap.cs) | class | Sequence-keyed dictionary whose entries are collections of type `C`, with `Add` and `Remove` of single values. |
| [`NotPersistedDictionary<T1, T2>`](Dictionaries/NotPersistedDictionary.cs) | class | In-memory `IPersistedDictionary` whose `Load`, `Save` and `Delete` do nothing. |
| [`ProjectedDictionary<TKey, TValue, TProjectedKey, TProjectedValue>`](Dictionaries/ProjectedDictionary.cs) | class | Presents a dictionary with converted key and value types through four projection functions. |
| [`ProjectedReadOnlyDictionary<TKey, TValue, TProjectedKey, TProjectedValue>`](Dictionaries/ProjectedReadOnlyDictionary.cs) | class | Read-only version of `ProjectedDictionary` over an `IReadOnlyDictionary<TKey, TValue>`. |
| [`PropertyBag`](Dictionaries/PropertyBag.cs) | class | String-keyed bag of typed values that serializes to XML with one element per key and a `type` attribute. |
| [`ReadOnlyDictionaryAdapter<TKey, TValue>`](Dictionaries/ReadOnlyDictionaryAdapter.cs) | class | Exposes an `IDictionary<TKey, TValue>` as an `IReadOnlyDictionary<TKey, TValue>`. |
| [`ReadOnlyDictionaryDecorator<TKey, TValue, TReadOnlyDictionary>`](Dictionaries/ReadOnlyDictionaryDecorator.cs) | class | Virtual forwarding decorator over an `IReadOnlyDictionary<TKey, TValue>`. |
| [`ReferenceDictionary<TKey, TVal>`](Dictionaries/ReferenceDictionary.cs) | class | `Dictionary` that compares class keys by reference identity. |
| [`SerializableDictionarySurrogate<TKey, TValue>`](Dictionaries/SerializableDictionarySurrogate.cs) | class | XML-serializable stand-in for a dictionary that stores parallel `Keys` and `Values` lists. |
| [`SerializableDictionarySurrogate2<TKey, TValue>`](Dictionaries/SerializableDictionarySurrogate2.cs) | class | XML-serializable stand-in for a dictionary that stores an array of `SerializableKeyValuePair` items. |
| [`SerializableKeyValuePair<TKey, TValue>`](Dictionaries/SerializableKeyValuePair.cs) | struct | Serializable key/value pair with settable `Key` and `Value`, used where `KeyValuePair` cannot be serialized. |
| [`ValueReferenceInverseDictionary<TKey, TValue>`](Dictionaries/ValueReferenceInverseDictionary.cs) | class | Lookup from each value object, compared by reference, to the keys that held it when the lookup was built. |

### FileMapped

| Type | Kind | Purpose |
|---|---|---|
| [`FileMappedBuffer`](FileMapped/FileMappedBuffer.cs) | class | Byte buffer mapped onto one file in pages, with a bounded number of pages cached in memory. |
| [`FilePageBase<TItem>`](FileMapped/FilePageBase.cs) | abstract class | Memory page that loads from and saves to its own region of a shared file stream. |
| [`FilePagedListBase<TItem>`](FileMapped/FilePagedListBase.cs) | abstract class | Memory-paged list whose pages live in one file opened from a `PagedFileDescriptor` and access mode. |
| [`IFilePage<TItem>`](FileMapped/IFilePage.cs) | interface | Memory page with the start and end positions of its region in the file. |
| [`IFilePagedList<TItem>`](FileMapped/IFilePagedList.cs) | interface | Memory-paged list that exposes its file descriptor and access mode. |

### Graph

| Type | Kind | Purpose |
|---|---|---|
| [`Graph`](Graph/Graph.cs) | class | Non-generic graph node whose edges point to other `Graph` nodes with integer weights. |
| [`Graph<TNode>`](Graph/Graph.cs) | class | Graph node type with `GraphEdge<TNode>` edges and integer weights. |
| [`Graph<TNode, TEdge, TWeight>`](Graph/Graph.cs) | class | Self-referencing graph node with a settable edge list and a `ToSimpleGraph` adapter. |
| [`GraphEdge`](Graph/GraphEdge.cs) | class | Integer-weighted edge between two `Graph` nodes. |
| [`GraphEdge<TNode>`](Graph/GraphEdge.cs) | class | Integer-weighted edge between two nodes of type `TNode`. |
| [`GraphEdge<TNode, TEdge, TWeight>`](Graph/GraphEdge.cs) | class | Edge with `Source`, `Destination` and `Weight`. |
| [`GraphNode<TEntity>`](Graph/GraphNode.cs) | class | Directed node holding an entity and ordered connections, with a cycle-safe depth-first `Visit` that can prune branches. |
| [`HierarchyNode<T>`](Graph/HierarchyNode.cs) | class | Tree node with an entity, `Parent`, `Depth` and `ChildNodes`. |
| [`IGraph`](Graph/IGraph.cs) | interface | Non-generic contract implemented by `Graph`. |
| [`IGraph<TNode>`](Graph/IGraph.cs) | interface | Graph contract with `GraphEdge<TNode>` edges and integer weights. |
| [`IGraph<TNode, TEdge, TWeight>`](Graph/IGraph.cs) | interface | Graph node contract with an edge list and conversion to `ISimpleGraph`. |
| [`IGraphEdge`](Graph/IGraphEdge.cs) | interface | Non-generic edge contract between `Graph` nodes. |
| [`IGraphEdge<TNode>`](Graph/IGraphEdge.cs) | interface | Edge contract with integer weights between `TNode` nodes. |
| [`IGraphEdge<TNode, TEdge, TWeight>`](Graph/IGraphEdge.cs) | interface | Edge contract with `Source`, `Destination` and `Weight`. |
| [`ISimpleGraph`](Graph/ISimpleGraph.cs) | interface | Non-generic unweighted node whose connections are other `ISimpleGraph` nodes. |
| [`ISimpleGraph<TNode>`](Graph/ISimpleGraph.cs) | interface | Unweighted node contract with a settable `Connections` sequence. |
| [`ISimpleGraph<TEntity, TNode>`](Graph/ISimpleGraph.cs) | interface | Unweighted node contract that also carries an `Entity`. |
| [`SimpleGraph`](Graph/SimpleGraph.cs) | class | Non-generic unweighted node implementation. |
| [`SimpleGraph<TNode>`](Graph/SimpleGraph.cs) | class | Unweighted node implementation with virtual `Connections`. |
| [`SimpleGraph<TEntity, TNode>`](Graph/SimpleGraph.cs) | class | Unweighted node implementation that carries an `Entity`. |

### Iterators

| Type | Kind | Purpose |
|---|---|---|
| [`BatchFetchIterator<T>`](Iterators/BatchFetchIterator.cs) | class | Enumerates items fetched lazily in batches by index and count, stopping at the first empty batch. |
| [`BoundedEnumerator<T>`](Iterators/BoundedEnumerator.cs) | class | Enumerator decorator that stops after a maximum number of items. |
| [`EnumeratedItem<T>`](Iterators/EnumeratedItem.cs) | class | Item yielded by `WithDescriptions()` with its index and first, last, interior, even or odd flags. |
| [`EnumeratedItemDescription`](Iterators/EnumeratedItemDescription.cs) | enum | Flags that describe an item position as `First`, `Last`, `Interior`, `Even` or `Odd`. |
| [`EnumeratorDecorator<T>`](Iterators/EnumeratorDecorator.cs) | class | Base enumerator decorator that forwards to an inner enumerator. |
| [`IEnumerableEx<out T>`](Iterators/IEnumerableEx.cs) | interface | Marker for a covariant sequence that also knows its count. |
| [`MemorizingEnumerator<T>`](Iterators/MemorizingEnumerator.cs) | class | Enumerator decorator that records items as they are read so they can be replayed after `Reset`. The source marks it as untested. |
| [`MemorizingIterator<T>`](Iterators/MemorizingIterator.cs) | struct | Reads a source once and caches it for repeated or interleaved traversals, each with its own position. |
| [`OnDisposeEnumerator<T>`](Iterators/OnDisposeEnumerator.cs) | class | Enumerator decorator that runs an action when it is disposed. |
| [`OnMoveNextEnumerator<T>`](Iterators/OnMoveNextEnumerator.cs) | class | Enumerator decorator that runs callbacks before and after `MoveNext` and can override its result. |
| [`ProjectedEnumerator<TFrom, TTo>`](Iterators/ProjectedEnumerator.cs) | class | Enumerator that converts each item of an inner enumerator with a projection. |
| [`ValueRangeIterator<T>`](Iterators/ValueRangeIterator.cs) | struct | Enumerates a `ValueRange<T>` ascending or descending with a caller-supplied step function. |

### Lists

| Type | Kind | Purpose |
|---|---|---|
| [`BoundedList<T>`](Lists/BoundedList.cs) | class | Shortcut of `BoundedList<T, TInner>` over an `IExtendedList<T>`. |
| [`BoundedList<T, TInner>`](Lists/BoundedList.cs) | class | List decorator whose indices start at `FirstIndex` instead of zero. |
| [`CircularList<TItem>`](Lists/CircularList.cs) | class | Fixed-capacity list that overwrites its oldest item once full and does not support insert or remove. |
| [`ExtendedList<T>`](Lists/ExtendedList.cs) | class | Array-backed `IExtendedList<T>` with long indexing, block growth, a maximum capacity and span overloads. |
| [`ExtendedListAdapter<TItem>`](Lists/ExtendedListAdapter.cs) | class | Exposes an `IList<T>` (a new `List<T>` by default) as an `IExtendedList<T>`. |
| [`ExtendedListBase<T>`](Lists/ExtendedListBase.cs) | abstract class | Abstract `IExtendedList<T>` that maps the 32-bit `IList<T>` members to long-indexed ones and supplies index and range checks. |
| [`ExtendedListDecorator<TItem>`](Lists/ExtendedListDecorator.cs) | abstract class | Shortcut of `ExtendedListDecorator<TItem, TConcrete>` for an inner `IExtendedList<TItem>`. |
| [`ExtendedListDecorator<TItem, TConcrete>`](Lists/ExtendedListDecorator.cs) | abstract class | Base decorator for an `IExtendedList<T>` that routes single-item calls to the inner range members. |
| [`GenericListAdapter<T>`](Lists/GenericListAdapter.cs) | class | Exposes a non-generic `IList` as an `IList<T>`. |
| [`IBoundedList<T>`](Lists/IBoundedList.cs) | interface | Mutable extended list whose first index is `FirstIndex`. |
| [`IExtendedList<T>`](Lists/IExtendedList.cs) | interface | Long-indexed list with batch `ReadRange`, `UpdateRange`, `InsertRange` and `RemoveRange` that also implements `IList<T>`. |
| [`IExtendedListExtensions`](Lists/IExtendedList.cs) | static class | Adds `RemoveRange(Range)` and a comparer-based `BinarySearch` to `IExtendedList<T>`. |
| [`IReadOnlyBoundedList<T>`](Lists/IReadOnlyBoundedList.cs) | interface | Read-only extended list that reports its `FirstIndex`. |
| [`IReadOnlyExtendedList<T>`](Lists/IReadOnlyExtendedList.cs) | interface | Read side of an extended list with `IndexOfL`, `IndexOfRange`, `Read` and `ReadRange`. |
| [`IReadOnlyExtendedListExtensions`](Lists/IReadOnlyExtendedList.cs) | static class | Adds a `ReadRange(Range)` overload to `IReadOnlyExtendedList<T>`. |
| [`IRecyclableList`](Lists/IRecyclableList.cs) | interface | Untyped recyclable list members `Recycle`, `IsRecycled`, `ListCount`, `RecycledCount` and object `Add`/`Update`. |
| [`IRecyclableList<T>`](Lists/IRecyclableList.cs) | interface | List whose removed slots are marked free and reused by later adds, with `Add(item, out index)`. |
| [`IWriteOnlyExtendedList<in T>`](Lists/IWriteOnlyExtendedList.cs) | interface | Write side of an extended list with long-indexed update, insert and remove members. |
| [`LegacyListAdapter<T>`](Lists/LegacyListAdapter.cs) | class | Exposes an `IList<T>` as a non-generic `IList`. |
| [`ListDecorator<TItem>`](Lists/ListDecorator.cs) | abstract class | Shortcut of `ListDecorator<TItem, TList>` for an inner `IList<TItem>`. |
| [`ListDecorator<TItem, TList>`](Lists/ListDecorator.cs) | abstract class | Base decorator that forwards every `IList<T>` member to an inner list. |
| [`PreAllocationPolicy`](Lists/PreAllocationPolicy.cs) | enum | Chooses how `UpdateOnlyList` grows its pre-allocated store, as `Fixed`, `ByBlock` or `MinimumRequired`. |
| [`ProjectedExtendedList<TFrom, TTo>`](Lists/ProjectedExtendedList.cs) | class | Presents an `IExtendedList<TFrom>` as `IExtendedList<TTo>` through a projection and its inverse. Its enumerator casts source items instead of projecting them. |
| [`RangedListBase<T>`](Lists/RangedListBase.cs) | abstract class | Extended list base for batch-optimized storage that implements single-item members through range members. |
| [`ReadOnlyListAdapter<TItem>`](Lists/ReadOnlyListAdapter.cs) | class | Exposes an `IList<T>` as an `IReadOnlyList<T>`. |
| [`ReadOnlyListProjection<TFrom, TTo>`](Lists/ReadOnlyListProjection.cs) | class | Read-only list that converts each item of an inner `IReadOnlyList<TFrom>` on access. |
| [`RecyclableListAdapter<T>`](Lists/RecyclableListAdapter.cs) | class | Recyclable list built from an `IExtendedList<T>` for items and an `IStack<long>` for free indices. |
| [`RecyclableListBase<T>`](Lists/RecyclableListBase.cs) | abstract class | Base `IRecyclableList<T>` that recycles on remove, reuses free slots on add, skips them on enumeration and rejects insert. |
| [`RecyclableListDecorator<TItem>`](Lists/RecyclableListDecorator.cs) | abstract class | Shortcut of `RecyclableListDecorator<TItem, TConcrete>` for an inner `IRecyclableList<TItem>`. |
| [`RecyclableListDecorator<TItem, TConcrete>`](Lists/RecyclableListDecorator.cs) | abstract class | Base decorator that forwards the recyclable members of an inner `IRecyclableList<T>`. |
| [`SingularListBase<T>`](Lists/SingularListBase.cs) | abstract class | Extended list base for item-by-item storage that implements range members by looping over single-item members. |
| [`SingularListDecorator<TItem>`](Lists/SingularListDecorator.cs) | abstract class | Shortcut of `SingularListDecorator<TItem, TConcrete>` for an inner `IExtendedList<TItem>`. |
| [`SingularListDecorator<TItem, TConcrete>`](Lists/SingularListDecorator.cs) | abstract class | List decorator that seals the range members and implements them through single-item calls. |
| [`UpdateOnlyList<TItem>`](Lists/UpdateOnlyList.cs) | class | Shortcut of `UpdateOnlyList<TItem, TInner>` over an `IExtendedList<TItem>`, by default a new `ExtendedList<TItem>`. |
| [`UpdateOnlyList<TItem, TInner>`](Lists/UpdateOnlyList.cs) | class | Turns add, insert and remove into updates over pre-allocated items, so stores that only support update can act as full lists. |

### Lookup

| Type | Kind | Purpose |
|---|---|---|
| [`CollectionGroup<TKey, TElement>`](Lookup/CollectionGroup.cs) | class | Shortcut of `CollectionGroup<TKey, TElement, TCollection>` that stores elements in a `List<TElement>`. |
| [`CollectionGroup<TKey, TElement, TCollection>`](Lookup/CollectionGroup.cs) | class | `IGrouping` that copies its elements into a new collection of type `TCollection`, exposed as `Elements`. |
| [`ExtendedLookup<TKey, TValue>`](Lookup/ExtendedLookup.cs) | class | Mutable `ILookup` with `Add`, `AddRange`, `Remove`, per-key counts and a configurable value collection factory. |
| [`Grouping<TKey, TElement>`](Lookup/Grouping.cs) | class | Minimal `IGrouping` over a key and an element sequence. |
| [`GroupOfAdjacent<TSource, TKey>`](Lookup/GroupOfAdjacent.cs) | class | Group of consecutive items with the same key, produced by `GroupAdjacentBy`. |
| [`LookupDecorator<TKey, TValue>`](Lookup/LookupDecorator.cs) | class | Shortcut of `LookupDecorator<TKey, TValue, TInner>` for an inner `ILookup<TKey, TValue>`. |
| [`LookupDecorator<TKey, TValue, TInner>`](Lookup/LookupDecorator.cs) | class | Forwarding decorator over a typed inner `ILookup<TKey, TValue>`. |
| [`MultiKeyLookup<K, V>`](Lookup/MultiKeyLookup.cs) | class | Lookup keyed by sequences of `K`, created by the `ToMultiKeyLookup` extension. |
| [`ProjectedLookup<TKey, TValue, TProjectedKey, TProjectedValue>`](Lookup/ProjectedLookup.cs) | class | Presents a lookup with converted key and value types. Its indexer throws `NotImplementedException`. |

### MemoryPaged

| Type | Kind | Purpose |
|---|---|---|
| [`FileSwappedMemoryPage<TItem>`](MemoryPaged/FileSwappedMemoryPage.cs) | abstract class | Memory page that swaps its contents to its own file, a temporary file by default, and deletes it on dispose. |
| [`IMemoryPage<TItem>`](MemoryPaged/IMemoryPage.cs) | interface | Disposable page with a maximum byte size that can `Save`, `Load` and `Unload`. |
| [`IMemoryPagedBuffer`](MemoryPaged/IMemoryPagedBuffer.cs) | interface | Byte buffer that is also a memory-paged list. |
| [`IMemoryPagedList<TItem>`](MemoryPaged/IMemoryPagedList.cs) | interface | Disposable paged list that loads, saves and unloads pages under a memory limit and raises an event for each step. |
| [`MemoryPageBase<TItem>`](MemoryPaged/MemoryPageBase.cs) | abstract class | Page that holds items in an in-memory store, measures them with an `IItemSizer` and saves or loads them through streams. |
| [`MemoryPagedBuffer`](MemoryPaged/MemoryPagedBuffer.cs) | class | Byte buffer split into pages that swap to temporary files when the memory limit is reached. |
| [`MemoryPagedBufferDecorator`](MemoryPaged/MemoryPagedBufferDecorator.cs) | abstract class | Shortcut of `MemoryPagedBufferDecorator<TMemoryPagedBuffer>` for an inner `IMemoryPagedBuffer`. |
| [`MemoryPagedBufferDecorator<TMemoryPagedBuffer>`](MemoryPaged/MemoryPagedBufferDecorator.cs) | abstract class | Base decorator that forwards the buffer and paging members of an inner memory-paged buffer. |
| [`MemoryPagedList<TItem>`](MemoryPaged/MemoryPagedList.cs) | class | Paged list of serialized items that keeps a bounded number of pages in memory and swaps the rest to temporary files. |
| [`MemoryPagedListBase<TItem>`](MemoryPaged/MemoryPagedListBase.cs) | abstract class | Paged list base with a page cache bounded by `MaxMemory`, page lifecycle events, `Flush` and `FlushOnDispose`. |
| [`MemoryPagedListDecorator<TItem>`](MemoryPaged/MemoryPagedListDecorator.cs) | abstract class | Shortcut of `MemoryPagedListDecorator<TItem, TMemoryPagedList>` for an inner `IMemoryPagedList<TItem>`. |
| [`MemoryPagedListDecorator<TItem, TMemoryPagedList>`](MemoryPaged/MemoryPagedListDecorator.cs) | class | Decorator that forwards an inner memory-paged list and relays its page events through virtual hooks. |

### Observable

| Type | Kind | Purpose |
|---|---|---|
| [`AddedEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments for an add, carrying the added items. |
| [`AddingEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments for an add, carrying the items to add. |
| [`ClearedItemsEventArgs`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments raised after a clear. |
| [`ClearingItemsEventArgs`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments raised before a clear. |
| [`CountedEventArgs`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments whose result is the count that was read. |
| [`CountingEventArgs`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments raised before `Count` is read. |
| [`EnumeratedEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments whose result is the fetched key or value collection. |
| [`EnumeratedItemEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments whose result is the item an enumerator moved to. |
| [`EnumeratingEventArgs`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments raised before a key or value collection is fetched. |
| [`EnumeratingItemEventArgs`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments raised before an enumerator moves to the next item. |
| [`FetchedByItemsEventArgs<TKey, TValue>`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments for a keyed fetch, carrying the keys and fetched values. |
| [`FetchedByRangeEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments for an indexed read, carrying the index, count and items read. |
| [`FetchingByItemsEventArgs<TKey>`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments for a keyed fetch, carrying the keys. |
| [`FetchingByRangeEventArgs`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments for an indexed read, carrying the index and count. |
| [`InsertedEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments for an insert, carrying the index and items. |
| [`InsertingEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments for an insert, carrying the index and items. |
| [`IObservableCollection`](Observable/IObservableCollection.cs) | interface | Access, read and mutate events with an `EventTraits` filter and suppressible notifications. |
| [`ObservableCollection<TItem>`](Observable/ObservableCollection.cs) | class | Observable collection over an `ICollection<TItem>`, by default a new `ExtendedList<TItem>`. |
| [`ObservableCollection<TItem, TConcrete>`](Observable/ObservableCollection.cs) | class | Collection decorator that raises pre and post events around every operation, filtered by `EventFilter` and bypassed by `SuppressNotifications`. |
| [`ObservableDictionary<TKey, TValue>`](Observable/ObservableDictionary.cs) | class | Observable dictionary over an `IDictionary<TKey, TValue>`, by default a new `Dictionary`. |
| [`ObservableDictionary<TKey, TValue, TConcrete>`](Observable/ObservableDictionary.cs) | class | Observable dictionary decorator that adds key search, fetch, update and key removal events. |
| [`ObservableExtendedCollection<TItem>`](Observable/ObservableExtendedCollection.cs) | class | Observable extended collection over an `IExtendedCollection<TItem>`. |
| [`ObservableExtendedCollection<TItem, TConcrete>`](Observable/ObservableExtendedCollection.cs) | class | Observable decorator for an `IExtendedCollection<T>` that also reports the range members. |
| [`ObservableExtendedList<TItem>`](Observable/ObservableExtendedList.cs) | class | Observable extended list over an `IExtendedList<TItem>`. |
| [`ObservableExtendedList<TItem, TConcrete>`](Observable/ObservableExtendedList.cs) | class | Observable decorator for an `IExtendedList<T>` with search, fetch, insert, update and range removal events. |
| [`ObservableList<TItem>`](Observable/ObservableList.cs) | class | Observable list over an `IList<TItem>`, by default a new `List<TItem>`. |
| [`ObservableList<TItem, TConcrete>`](Observable/ObservableList.cs) | class | Observable decorator for an `IList<T>` with index search, fetch, insert, update and removal events. |
| [`RemovedItemsEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments for removal by item, with one success flag per item. |
| [`RemovedRangeEventArgs`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments for removal by index and count. |
| [`RemovingItemsEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments for removal by item, carrying the items. |
| [`RemovingRangeEventArgs`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments for removal by index and count. |
| [`SearchedLocationEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments for an index search, with the index found for each item. |
| [`SearchedMembershipEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments for a membership search, with one flag per item. |
| [`SearchingLocationEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments for an index search, carrying the items. |
| [`SearchingMembershipEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments for a membership search, carrying the items. |
| [`UpdatedByItemsEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments for a keyed update, carrying the updated pairs. |
| [`UpdatedByRangeEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Post-operation arguments for an indexed update, carrying the index and items. |
| [`UpdatingByItemsEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments for a keyed update, carrying the pairs to write. |
| [`UpdatingByRangeEventArgs<T>`](Observable/ObservableEventArgs.cs) | class | Pre-operation arguments for an indexed update, carrying the index and items. |

### Paged

| Type | Kind | Purpose |
|---|---|---|
| [`BinarySerializedPage<TItem>`](Paged/BinarySerializedPage.cs) | class | File-swapped memory page that saves and loads its items as an array with `BinarySerializer`. |
| [`IPage<TItem>`](Paged/IPage.cs) | interface | Indexed slice of a paged list with number, index range, size, dirty flag, `PageState`, `Read`, `Write` with overflow and `EraseFromEnd`. |
| [`IPagedList<TItem>`](Paged/IPagedList.cs) | interface | Loadable extended list split into pages, with page events, `Pages` and `EnterOpenPageScope` to pin a page during work. |
| [`PageBase<TItem>`](Paged/PageBase.cs) | abstract class | Page base that tracks indices, size and dirty state, splits writes into update, append and overflow, and rejects updates that change item size. |
| [`PagedListBase<TItem>`](Paged/PagedListBase.cs) | abstract class | Paged list base that maps ranges to pages, raises page events and only inserts or removes at the end. |
| [`PagedListDecorator<TItem>`](Paged/PagedListDecorator.cs) | abstract class | Shortcut of `PagedListDecorator<TItem, TPagedList>` for an inner `IPagedList<TItem>`. |
| [`PagedListDecorator<TItem, TPagedList>`](Paged/PagedListDecorator.cs) | abstract class | Base decorator that forwards the paging members and events of an inner `IPagedList<T>`. |
| [`PageState`](Paged/PageState.cs) | enum | Page lifecycle states from `Loading` to `Deleted`. |

### Queues

| Type | Kind | Purpose |
|---|---|---|
| [`BinaryHeap<T>`](Queues/BinaryHeap.cs) | class | Array-backed binary min-heap of `IComparable<T>` items with `Peek`, `Pop` and `Copy`. |
| [`IHeap<T>`](Queues/IHeap.cs) | interface | Collection contract with `Peek` and `Pop` of the top item. |
| [`IQueue<T>`](Queues/IQueue.cs) | interface | Collection contract with `Enqueue`, `TryPeek` and `TryDequeue`. |
| [`IQueueExtensions`](Queues/IQueue.cs) | static class | Adds throwing `Dequeue` and `Peek` to `IQueue<T>`. |
| [`ProcessingQueue<T>`](Queues/ProcessingQueue.cs) | class | Accepts items from many threads and processes them one at a time on a pool thread once `Enabled` is set, raising success, failure and error events. |
| [`ProducerConsumerQueue<T>`](Queues/ProducerConsumerQueue.cs) | class | Producer/consumer queue bounded by a size estimate, whose puts wait for capacity, with batch put and take and completion signals. |
| [`QueueAdapter<T>`](Queues/QueueAdapter.cs) | class | Exposes a `Queue<T>` as an `IQueue<T>`. |
| [`QueueListAdapter<T>`](Queues/QueueListAdapter.cs) | class | Exposes an `IList<T>` as an `IQueue<T>` that enqueues at the end. `TryPeek` removes the last item and `TryDequeue` reads it without removing. |

### Sets

| Type | Kind | Purpose |
|---|---|---|
| [`BloomFilterBase<TItem>`](Sets/BloomFilterBase.cs) | abstract class | Bloom filter over a bit array, sized by length or by target error and capacity, with subclasses supplying the hash function. |
| [`HashedBloomFilter<TItem>`](Sets/HashedBloomFilter.cs) | class | Bloom filter that derives its bit positions from a cryptographic hash (`CHF`) of each serialized item. |
| [`IBloomFilter<in TItem>`](Sets/IBloomFilter.cs) | interface | Probabilistic set contract with `Add`, `Contains`, `UnionWith`, `Clear`, size and estimated `Error`. |
| [`MurMur3BloomFilter<TItem>`](Sets/MurMur3BloomFilter.cs) | class | Bloom filter that hashes each serialized item with MurMur3 under a different seed per round. |
| [`ReferenceHashSet<TItem>`](Sets/ReferenceHashSet.cs) | class | `HashSet` that compares class items by reference identity. |
| [`SetBase<TItem>`](Sets/SetBase.cs) | abstract class | Base `ISet<T>` that implements the set algebra members over abstract `Add`, `Contains`, `Remove` and `Clear`. |
| [`SetDecorator<TItem>`](Sets/SetDecorator.cs) | class | Forwarding decorator over an `ISet<TItem>`. |
| [`SetDecorator<TItem, TSet>`](Sets/SetDecorator.cs) | abstract class | Base decorator that forwards every `ISet<T>` member to a typed inner set. |

### Sorted

| Type | Kind | Purpose |
|---|---|---|
| [`ISortedList<T>`](Sorted/ISortedList.cs) | interface | Extended collection that is also a read-only extended list kept in sort order. |
| [`SortedList<T>`](Sorted/SortedList.cs) | class | Keeps an inner `IExtendedList<T>` sorted ascending or descending and finds items by binary search. |

### Sorting

| Type | Kind | Purpose |
|---|---|---|
| [`BubbleSorter`](Sorting/BubbleSorter.cs) | static class | Static entry point that bubble-sorts an `IExtendedList<T>` in place. |
| [`BubbleSorter<T>`](Sorting/BubbleSorter.cs) | class | Bubble sort over an `IExtendedList<T>`. |
| [`InsertionSorter`](Sorting/InsertionSorter.cs) | static class | Static entry point that insertion-sorts an `IExtendedList<T>` in place. |
| [`InsertionSorter<T>`](Sorting/InsertionSorter.cs) | class | Insertion sort over an `IExtendedList<T>`. |
| [`QuickSorter`](Sorting/QuickSorter.cs) | static class | Static entry point that quick-sorts an `IExtendedList<T>` in place. |
| [`QuickSorter<T>`](Sorting/QuickSorter.cs) | class | Quick sort over an `IExtendedList<T>`. |
| [`SelectionSorter`](Sorting/SelectionSorter.cs) | static class | Static entry point that selection-sorts an `IExtendedList<T>` in place. |
| [`SelectionSorter<T>`](Sorting/SelectionSorter.cs) | class | Selection sort over an `IExtendedList<T>`. |
| [`SortAlgorithm<T>`](Sorting/SortAlgorithm.cs) | abstract class | Base for in-place sorts of an `IExtendedList<T>` with a comparer and a `Swap` helper. |
| [`TopologicalSorter`](Sorting/TopologicalSorter.cs) | static class | Topologically sorts items from edge lists or edge functions and finds strongly connected components with Tarjan's algorithm. |

### Spatial

| Type | Kind | Purpose |
|---|---|---|
| [`HalfSizeNodeDivider<TBounds>`](Spatial/QuadTreeNodeDividers.cs) | class | Quad tree divider that always splits a node into four equal quadrants. |
| [`IIntegerRectangle<TSelf>`](Spatial/IIntegerRectangle.cs) | interface | Integer row and column rectangle with containment, intersection and a static `Create`, used as the quad tree bounds constraint. |
| [`IntegerRectangle`](Spatial/IntegerRectangle.cs) | struct | Default immutable `IIntegerRectangle` with inclusive start and end rows and columns. |
| [`IQuadTreeNodeDivider<TBounds>`](Spatial/IQuadTreeNodeDivider.cs) | interface | Strategy that creates child nodes and grows a new root for a quad tree. |
| [`ProportionateSizeNodeDivider<TBounds>`](Spatial/QuadTreeNodeDividers.cs) | class | Quad tree divider that splits only along rows when a node is much taller than it is wide. |
| [`QuadTree<TBounds>`](Spatial/QuadTree.cs) | class | Region quad tree over integer rectangles with insert, remove, area and point queries and `Grow`. |
| [`QuadTreeNode<TBounds>`](Spatial/QuadTreeNode.cs) | class | Quad tree node that holds the items not contained by any single child quadrant. |

### Stacks

| Type | Kind | Purpose |
|---|---|---|
| [`IStack<T>`](Stacks/IStack.cs) | interface | Collection contract with `Push`, `TryPeek` and `TryPop`. |
| [`IStackExtensions`](Stacks/IStack.cs) | static class | Adds throwing `Pop` and `Peek` to `IStack<T>`. |
| [`StackAdapter<T>`](Stacks/StackAdapter.cs) | class | Exposes an `IExtendedList<T>` as an `IStack<T>` whose top is the last item. |
| [`StackList<T>`](Stacks/StackList.cs) | class | Shortcut of `StackList<T, TInner>` over an `IExtendedList<T>`, by default a new `ExtendedList<T>`. |
| [`StackList<T, TInner>`](Stacks/StackList.cs) | class | Extended list decorator that is also a stack, with `Peek(depth)` below the top. |

### StreamMapped

| Type | Kind | Purpose |
|---|---|---|
| [`IStreamMappedCollection`](StreamMapped/IStreamMappedCollection.cs) | interface | Collection stored in an `ObjectStream`, exposing the stream and `Clear`. |
| [`IStreamMappedCollection<TItem>`](StreamMapped/IStreamMappedCollection.cs) | interface | Stream-mapped collection with a typed `ObjectStream<TItem>`. |
| [`IStreamMappedCollection<TKey, TValue>`](StreamMapped/IStreamMappedCollection.cs) | interface | Stream-mapped collection of key/value pairs that does not fix the physical layout. |
| [`IStreamMappedDictionary`](StreamMapped/IStreamMappedDictionary.cs) | interface | Untyped stream-mapped dictionary marker whose raw key and value reads are internal. |
| [`IStreamMappedDictionary<TKey, TValue>`](StreamMapped/IStreamMappedDictionary.cs) | interface | Loadable, disposable dictionary over an object stream with `ReadKey`, `ReadValue`, `TryFindKey`, `TryFindValue` and `RemoveAt` by slot index. |
| [`IStreamMappedHashSet<TItem>`](StreamMapped/IStreamMappedHashSet.cs) | interface | Loadable, disposable `ISet<T>` stored in an object stream. |
| [`IStreamMappedList<TItem>`](StreamMapped/IStreamMappedList.cs) | interface | Loadable, disposable extended list stored in an object stream with its serializer and comparer. |
| [`IStreamMappedRecyclableList<TItem>`](StreamMapped/IStreamMappedRecyclableList.cs) | interface | Loadable, disposable recyclable list stored in an object stream. |
| [`IStreamMappedRecylableList`](StreamMapped/IStreamMappedRecylableList.cs) | interface | Untyped recyclable stream-mapped list with a long `Count`, declared in the global namespace. |
| [`StreamMappedBTree<K, V>`](StreamMapped/StreamMappedBTree.cs) | class | Disposable B-tree whose fixed-size node records are written directly to a seekable stream, for constant-size keys and values. |
| [`StreamMappedBTreeLookup<K, V>`](StreamMapped/StreamMappedBTreeLookup.cs) | class | Disposable one-to-many lookup stored in a stream-mapped B-tree or B+ tree chosen by a `BTreeSpec`. |
| [`StreamMappedBTreePlus<K, V>`](StreamMapped/StreamMappedBTreePlus.cs) | class | Disposable B+ tree whose fixed-size node records are written directly to a seekable stream, for constant-size keys and values. |
| [`StreamMappedBuffer`](StreamMapped/StreamMappedBuffer.cs) | class | Byte buffer whose contents are the bytes of a stream, shifting bytes on insert and remove. |
| [`StreamMappedDictionary<TKey, TValue>`](StreamMapped/StreamMappedDictionary.cs) | class | Dictionary for keys of any length that stores key/value pairs as object stream items and finds keys through a key checksum index. Removed slots are reused. |
| [`StreamMappedDictionaryCLK<TKey, TValue>`](StreamMapped/StreamMappedDictionaryCLK.cs) | class | Dictionary for constant-length keys that stores only values as items and keeps the keys in a reserved stream. Removed slots are reused. |
| [`StreamMappedFactory`](StreamMapped/StreamMappedFactory.cs) | static class | Creates stream-mapped lists, recyclable lists, dictionaries, hash sets, B-trees and lookups over a stream or `ClusteredStreams`. |
| [`StreamMappedHashSet<TItem>`](StreamMapped/StreamMappedHashSet.cs) | class | Set stored in a constant-length-key dictionary keyed by each item's digest. |
| [`StreamMappedList<TItem>`](StreamMapped/StreamMappedList.cs) | class | Extended list whose items are stored in an object stream, with an optional checksum index for fast `IndexOf`. |
| [`StreamMappedRecyclableList<TItem>`](StreamMapped/StreamMappedRecyclableList.cs) | class | Recyclable list stored in an object stream that tracks free slots in a recyclable index attachment. |

### StreamPaged

| Type | Kind | Purpose |
|---|---|---|
| [`StreamPagedList<TItem>`](StreamPaged/StreamPagedList.cs) | class | Paged list written to a stream as contiguous pages with an optional 256-byte header, appended and removed only at the end. |
| [`StreamPagedListType`](StreamPaged/StreamPagedListType.cs) | enum | Chooses `Static` pages of constant-size items or `Dynamic` pages that record each item size. |

### Synchronized

| Type | Kind | Purpose |
|---|---|---|
| [`ISynchronizedExtendedList<T>`](Synchronized/ISynchronizedExtendedList.cs) | interface | Extended list that is also a synchronized list and read-only list. |
| [`ISynchronizedList<T>`](Synchronized/ISynchronizedList.cs) | interface | `IList<T>` that exposes its read/write lock through `ISynchronizedObject`. |
| [`ISynchronizedReadOnlyExtendedList<T>`](Synchronized/ISynchronizedReadOnlyExtendedList.cs) | interface | Read-only extended list that exposes its lock. |
| [`ISynchronizedReadOnlyList<T>`](Synchronized/ISynchronizedReadOnlyList.cs) | interface | `IReadOnlyList<T>` that exposes its read/write lock through `ISynchronizedObject`. |
| [`SynchronizedCollection<TItem>`](Synchronized/SynchronizedCollection.cs) | class | Synchronized collection over an `ICollection<TItem>`. |
| [`SynchronizedCollection<TItem, TConcrete>`](Synchronized/SynchronizedCollection.cs) | class | Collection decorator that takes a read or write lock around every member and holds the read lock while an enumerator is alive. |
| [`SynchronizedDictionary<TKey, TValue>`](Synchronized/SynchronizedDictionary.cs) | class | Synchronized dictionary over an `IDictionary<TKey, TValue>`, by default a new `Dictionary`. |
| [`SynchronizedDictionary<TKey, TValue, TInner>`](Synchronized/SynchronizedDictionary.cs) | class | Dictionary decorator that locks every member, optionally sharing another object's lock. |
| [`SynchronizedExtendedList<TItem>`](Synchronized/SynchronizedExtendedList.cs) | class | Synchronized extended list over an `IExtendedList<TItem>`, by default a new `ExtendedList<TItem>`. |
| [`SynchronizedExtendedList<TItem, TInternalList>`](Synchronized/SynchronizedExtendedList.cs) | class | Extended list decorator that locks every member, including the range members. |
| [`SynchronizedHeap<T>`](Synchronized/SynchronizedHeap.cs) | class | Synchronized `IHeap<T>`, by default over a `BinaryHeap<T>`. |
| [`SynchronizedList<TItem>`](Synchronized/SynchronizedList.cs) | class | Synchronized list over an `IList<TItem>`, by default a new `List<TItem>`. |
| [`SynchronizedList<TItem, TList>`](Synchronized/SynchronizedList.cs) | class | `IList<T>` decorator that locks each member. Check-then-act sequences still race unless the caller holds a scope. |
| [`SynchronizedQueue<T>`](Synchronized/SynchronizedQueue.cs) | class | Synchronized `IQueue<T>` with `Enqueue`, `Dequeue` and `Peek`, by default over a `Queue<T>`. |
| [`SynchronizedSet<TItem>`](Synchronized/SynchronizedSet.cs) | class | Synchronized set over an `ISet<TItem>`, by default a new `HashSet<TItem>`. |
| [`SynchronizedSet<TItem, TSet>`](Synchronized/SynchronizedSet.cs) | class | Set decorator that locks every member. |

### Trees

| Type | Kind | Purpose |
|---|---|---|
