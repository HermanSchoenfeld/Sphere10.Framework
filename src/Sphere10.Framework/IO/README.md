# IO

File-system utilities, endian-aware binary readers and writers, file content caches, file descriptors for paged and transactional storage, and multi-file transactions.

`Tools.FileSystem` is the general file and directory toolkit. It covers path normalization and validation, case-correct paths, temporary files and folders, synchronous and asynchronous copy and delete, content hashing, file monitoring, part reads and compression with an optional password. `EndianBinaryReader` and `EndianBinaryWriter` are the `BinaryReader` and `BinaryWriter` equivalents over an `EndianBitConverter` from [Conversion](../Conversion/README.md), with variable-length integer support. The reader does not buffer, so the caller may seek the stream between reads. `TextFileCache` and `BinaryFileCache` derive from `FileCacheBase<TContent>`, a [Cache](../Cache/README.md) keyed by path that reloads a file when its write time or length changes.

`PagedFileDescriptor`, `TransactionalFileDescriptor` and `Sphere10FrameworkFileDescriptor` describe a file and its page size, memory budget, page directory, cluster size, container policy and endianness. Transactional collections in [Transactions](../Transactions/README.md) and file object spaces in [ObjectSpaces](../ObjectSpaces/README.md) open files from them. `FileTransaction` enlists transactional file buffers under one header file and commits or rolls them back together. When a header file from an interrupted transaction exists, the constructor completes or rolls back that transaction before starting. `FileTransactionScope` exposes it as an ambient transactional scope from [Scopes](../Scopes/README.md). `ProcessDanglingTransactions` currently does nothing.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BinaryFileCache`](BinaryFileCache.cs) | class | Caches file contents as byte arrays keyed by path and reloads them when the file changes. |
| [`EndianBinaryReader`](EndianBinaryReader.cs) | class | `BinaryReader` equivalent for either byte order, with variable-length integers, two-dimensional arrays and read-or-throw helpers. |
| [`EndianBinaryWriter`](EndianBinaryWriter.cs) | class | `BinaryWriter` equivalent for either byte order, with variable-length integer support. |
| [`FileAccessHelper`](FileAccessHelper.cs) | static class | Opens the file of a `PagedFileDescriptor` for a `FileAccessMode` and reports whether existing content must be loaded and whether to flush on dispose. |
| [`FileAccessMode`](FileAccessMode.cs) | enum | Flags for read, write, truncate, append and automatic load access to a storage file. |
| [`FileAccessModeExtensions`](FileAccessMode.cs) | static class | `IsReadOnly` and `WithoutAutoLoad` for `FileAccessMode`. |
| [`FileCacheBase<TContent>`](FileCacheBase.cs) | abstract class | Cache keyed by file path that treats an entry as stale when the file's UTC write time or length differs. |
| [`FileSystemPathType`](FileSystemPathType.cs) | enum | Whether file-system listings return `Relative` or `Absolute` paths. |
| [`FileTransaction`](FileTransaction.cs) | class | Commits or rolls back a set of enlisted transactional files under one header file, resuming an interrupted transaction on construction. |
| [`FileTransactionScope`](FileTransactionScope.cs) | class | Ambient transactional scope that owns a `FileTransaction` and enlists files in it. |
| [`FileTransactionState`](FileTransactionState.cs) | enum | State of a file transaction: `Unchanged`, `HasChanges`, `Committing` or `RollingBack`. |
| [`PagedFileDescriptor`](PagedFileDescriptor.cs) | record | Path, page size and memory budget of a paged file. |
| [`Sphere10FrameworkFileDescriptor`](Sphere10FileDescriptor.cs) | record | Transactional file descriptor that adds cluster size, clustered-stream container policy and endianness. |
| [`TextFileCache`](TextFileCache.cs) | class | Caches file contents as strings keyed by path and reloads them when the file changes. |
| [`Tools.FileSystem`](FileSystemTool.cs) | tools | Path, file and directory operations, temporary locations, hashing, monitoring and compression. |
| [`TransactionalFileDescriptor`](TransactionalFileDescriptor.cs) | record | Paged file descriptor that adds the directory holding uncommitted pages. |
