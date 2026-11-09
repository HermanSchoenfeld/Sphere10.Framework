# File Store

Keyed file stores that map application keys to files under a base directory and open them as streams.

An `IFileStore<TFileKey>` owns a set of files identified by keys. Callers create a file with `NewFile`, register keys with `RegisterMany` or `RegisterFile`, resolve the physical path with `GetFilePath`, and open or time-stamp a file by key without handling paths. `FileStoreBase<TFileKey>` implements the stream and file-time members over `GetFilePath`, so a concrete store supplies key management only.

`SimpleFileStore` uses string keys and records the key-to-file map in an `__FileStoreRegistry.xml` registry, an [`XmlFileDictionary`](../XML/README.md) by default. `GuidFileStore` uses `Guid` keys and derives the path from the key, with the first two hex digits as a subfolder. `KeyTransformedFileStore<TFromKey, TToKey>` exposes a store under a different key type, and `GuidStringFileStore` uses it to give a `GuidFileStore` string keys. Stores are disposable. With `FileStorePersistencePolicy.DeleteOnDispose` they delete their files on dispose, and `TempFileStore` always uses that policy in a new folder under the system temp path.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`FileStoreBase<TFileKey>`](FileStoreBase.cs) | abstract class | Implements stream opening and file-time members of `IFileStore<TFileKey>` over the abstract key and path members. |
| [`FileStorePersistencePolicy`](FileStorePersistencePolicy.cs) | enum | Whether a store keeps its files (`Perist`) or deletes them when disposed (`DeleteOnDispose`). |
| [`GuidFileStore`](GuidFileStore.cs) | class | Stores files named by `Guid` under a two-digit subfolder, with an optional default extension. |
| [`GuidStringFileStore`](GuidStringFileStore.cs) | class | `GuidFileStore` addressed by the string form of each `Guid`. |
| [`IFileStore<TFileKeyType>`](IFileStore.cs) | interface | Contract for key-addressed file creation, registration, deletion, stream access and file times. |
| [`IFileStoreExtensions`](IFileStoreExtensions.cs) | static class | Single-key `RegisterFile` and `Delete` helpers over the `RegisterMany` and `DeleteMany` members. |
| [`KeyTransformedFileStore<TFromKey, TToKey>`](KeyTransformedFileStore.cs) | class | Exposes an inner file store under another key type through two conversion functions. |
| [`SimpleFileStore`](SimpleFileStore.cs) | class | String-keyed store that saves files under generated names and tracks them in a persisted registry. |
| [`TempFileStore`](TempFileStore.cs) | class | `SimpleFileStore` in a temp subfolder with an in-memory registry that deletes its files on dispose. |
