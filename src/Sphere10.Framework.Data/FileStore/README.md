# File Store

Keyed file stores that map application keys to files under a base directory and open them as streams.

An `IFileStore<TFileKey>` owns a set of files identified by keys. Callers create a file with `NewFile`, register keys with `RegisterMany` or `RegisterFile`, resolve the physical path with `GetFilePath`, and open or time-stamp a file by key without handling paths. `FileStoreBase<TFileKey>` implements the stream and file-time members over `GetFilePath`, so a concrete store supplies key management only.

`SimpleFileStore` uses string keys and records the key-to-file map in an `__FileStoreRegistry.xml` registry, an [`XmlFileDictionary`](../XML/README.md) by default. `GuidFileStore` uses `Guid` keys and derives the path from the key, with the first two hex digits as a subfolder. `KeyTransformedFileStore<TFromKey, TToKey>` exposes a store under a different key type, and `GuidStringFileStore` uses it to give a `GuidFileStore` string keys. Stores are disposable. With `FileStorePersistencePolicy.DeleteOnDispose` they delete their files on dispose, and `TempFileStore` always uses that policy in a new folder under the system temp path.

## Usage

`NewFile` creates an empty file, with its folder, and returns its key. The read and write members then address the file by key:

```csharp
using Sphere10.Framework.Data;

using var store = new GuidFileStore(Path.Combine(dataDirectory, "files")) { FileExtension = ".json" };
var key = store.NewFile();                       // {dataDirectory}/files/50/4c143905d1422a906e42fb8552fa5b.json
store.WriteAllText(key, "{\"Name\":\"Ada\"}");
var json = store.ReadAllText(key);
using (var stream = store.OpenRead(key))
	Consume(stream);
store.Delete(key);                               // IFileStoreExtensions; also removes the emptied subfolder
```

`TempFileStore` suits scratch files that must not outlive the process:

```csharp
using var scratch = new TempFileStore();
var key = scratch.NewFile();
scratch.WriteAllBytes(key, payload);
// Disposing the store deletes its folder's files.
```

## Keeping file data outside a database

Large or binary data, such as uploaded documents and images, can live in a `GuidFileStore` while a table row stores only the `Guid` key. The row stays small, the database never holds the bytes, and a backup that copies the data folder carries both. Write the files before the row is committed and delete them if the transaction fails; delete them after the row's deletion commits. When one record owns several files, store one document file under the row's key whose content names the keys of the others, so the row still needs a single column.

## Notes

- `GuidFileStore.Clear` deletes every file under its base directory, so give each store a directory of its own.
- `RegisterMany` throws when a file already exists, and `DeleteMany` throws when one is missing. Check `ContainsFile` first where either can happen.
- The read and write members do not create folders. Create a file with `NewFile` or `RegisterFile` before writing it through the store.
- `GuidFileStore.FileKeys` lists the files that carry its `FileExtension` and whose names parse as a `Guid`; other files in the folder are ignored.

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
