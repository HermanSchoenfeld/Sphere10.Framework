# Object Stream

A list-like store of serialized objects over clustered streams, with observer hooks and projection indexes kept in attachments of the same storage.

`ObjectStream` stores one object per clustered stream of a `ClusteredStreams` container from [ClusteredStreams](../ClusteredStreams/README.md) and is the primitive under the stream-mapped lists, dictionaries and sets in [Collections](../Collections/README.md) and under each dimension of an [ObjectSpace](../ObjectSpaces/README.md). It saves, loads, removes and reaps items by position, raises `PreItemOperation` and `PostItemOperation` around each change, and holds reserved streams for its attachments. `ObjectStream<T>` adds typed `SaveItem` and `LoadItem` and looks up indexes by member expression or name through `GetIndexFor`, `GetUniqueIndexFor`, `TryGetIndex` and `TryGetUniqueIndex`.

The constructor of `ObjectStream<T>` that takes a raw `Stream` creates and owns its clustered container, and can store an `ObjectStreamMetaData` record so that `ObjectStream.TryOpenFromStream` reopens the storage without a supplied schema. The constructor that takes an existing `ClusteredStreams` does not own it unless `OwnsStreams` is set. Disposal disposes the container only when it is owned. Operations take the container's access scope, which callers also enter with `EnterAccessScope` to group several operations.

The `Indexes` subfolder defines the index contracts and bases. The concrete projection, unique, checksum, Merkle and recyclable-slot indexes are internal and are created by the ObjectSpace builders. A unique index rejects a duplicate key before the write, and `IndexNullPolicy` decides whether null keys are skipped, indexed or rejected.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ObjectStream`](ObjectStream.cs) | class | Untyped object store over clustered streams with save, load, remove, reap, clear, item-operation events and optional ownership of its container. |
| [`ObjectStream<T>`](ObjectStreamT.cs) | class | Typed object store that can create its own container over a raw stream, with typed item access and index lookup. |
| [`ObjectStreamMetaData`](ObjectStreamMetaData.cs) | class | Sealed self-describing record of item type, serializer code, preallocation, root flag, change-tracking member, index rows and optional serializer factory. |
| [`ObjectStreamMetaDataSerializer`](ObjectStreamMetaDataSerializer.cs) | class | Sealed serializer for the length-prefixed `ObjectStreamMetaData` record and its index rows. |
| [`ObjectStreamOperationType`](ObjectStreamOperationType.cs) | enum | Operation reported to observers, which is a read, add, insert, update, remove or reap. |

### Indexes

| Type | Kind | Purpose |
|---|---|---|
| [`IndexBase<TStore>`](Indexes/IndexBase.cs) | abstract class | Index that observes an object stream and delegates attachment, flushing and integrity checks to its storage attachment. |
| [`IndexNullPolicy`](Indexes/IndexNullPolicy.cs) | enum | Whether a projection index ignores, indexes or rejects null keys. |
| [`IProjectionIndex<TKey>`](Indexes/IProjectionIndex.cs) | interface | Read-only index exposing an `ILookup` from projected key to item positions. |
| [`IUniqueProjectionIndex<TKey>`](Indexes/IUniqueProjectionIndex.cs) | interface | Read-only unique index exposing a dictionary from projected key to one item position. |
| [`ObjectStreamObserverBase`](Indexes/ObjectStreamObserverBase.cs) | abstract class | Subscribes to an object stream and exposes virtual hooks before and after add, insert, update, remove, reap and clear. |
| [`ProjectionIndexBase<TItem, TProjection, TStore>`](Indexes/ProjectionIndexBase.cs) | abstract class | Typed index base that projects each item to a key, applies the null policy and routes typed hooks. |
