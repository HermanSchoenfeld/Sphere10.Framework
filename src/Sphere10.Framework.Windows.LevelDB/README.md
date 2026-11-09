<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Windows.LevelDB

Sphere10.Framework.Windows.LevelDB is a .NET wrapper for Google's LevelDB, an embedded key-value store that keeps its keys in sorted order. Use it on Windows when an application needs a local, persistent map from string or byte-array keys to values, with atomic batches, ordered iteration and consistent snapshots.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Windows.LevelDB
```

The package targets `net10.0`, and also `net11.0` when it is built with a .NET 11 SDK. It belongs to the Windows solution, `src/Sphere10.Framework (Win).sln`, whose projects reference `src/Sphere10.Framework.Windows.LevelDB/Sphere10.Framework.Windows.LevelDB.csproj` directly.

## 📋 Overview

The package embeds 32-bit and 64-bit builds of the native `LevelDB.dll`. On first use, `LevelDBInterop` writes the build that matches the process to `%APPDATA%\Sphere10.Framework\Sphere10.Framework.Windows.LevelDB-<assembly version>\x32` or `\x64`, and then loads it with the Windows `LoadLibrary` function. A file that already exists at that path is reused.

All types are in the `Sphere10.Framework.Windows.LevelDB` namespace.

| Type | Role |
|---|---|
| `DB` | Opens a database directory, reads and writes entries, and creates iterators and snapshots |
| `Options`, `ReadOptions`, `WriteOptions` | Settings for opening a database, for reads and for writes |
| `WriteBatch` | Updates that `DB.Write` applies atomically, in the order they were added |
| `Iterator` | Moves through the entries in key order |
| `SnapShot` | A fixed view of the database for consistent reads |
| `Cache`, `Comparator`, `Env` | An LRU block cache, a custom key order and the default operating-system environment |
| `LevelDBException` | The error that LevelDB reports |

## 🔧 Usage

### Basic CRUD Operations

```csharp
using Sphere10.Framework.Windows.LevelDB;

using var db = new DB(@"C:\Data\cities", new Options { CreateIfMissing = true });

db.Put("Tampa", "green");
db.Put("London", "red");

string color = db.Get("Tampa");   // "green"
db.Delete("London");              // deleting a missing key is not an error
bool removed = db.Get("London") == null;
```

String keys and values are stored as UTF-8. `Put`, `Get` and `Delete` also take `byte[]` keys and values, and an optional `WriteOptions` or `ReadOptions`.

### Batch Writes

```csharp
using (var batch = new WriteBatch()) {
	batch.Delete("London")
		.Put("Paris", "blue")
		.Put("Tokyo", "white");
	db.Write(batch);
}
```

### Iterating Over Data

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

using (var iterator = db.CreateIterator()) {
	for (iterator.Seek("P"); iterator.IsValid(); iterator.Next())
		Console.WriteLine($"{iterator.GetStringKey()}: {iterator.GetStringValue()}");
}

// DB also enumerates every entry in key order, from a snapshot taken when the enumeration starts.
var keys = (db as IEnumerable<KeyValuePair<string, string>>).Select(entry => entry.Key).ToList();
```

A new iterator is invalid until `Seek`, `SeekToFirst` or `SeekToLast` positions it. `Seek` moves to the first key at or after its argument, and `Next` and `Prev` step from there. `DB` implements both `IEnumerable<KeyValuePair<string, string>>` and `IEnumerable<KeyValuePair<byte[], byte[]>>`, so cast it to choose the string view for LINQ.

### Point-in-Time Snapshots

```csharp
db.Put("Tampa", "green");

using (var snapshot = db.CreateSnapshot()) {
	var asOfSnapshot = new ReadOptions { Snapshot = snapshot };
	db.Put("Tampa", "yellow");

	string then = db.Get("Tampa", asOfSnapshot);   // "green"
	string now = db.Get("Tampa");                  // "yellow"
}
```

A snapshot is immutable, so several threads can read through it without locking. Pass it in `ReadOptions.Snapshot` to `Get` or to `CreateIterator`.

### Options

| Setting | Effect |
|---|---|
| `Options.CreateIfMissing` | Creates the database when the directory has none |
| `Options.ErrorIfExists` | Fails when the database already exists |
| `Options.ParanoidChecks` | Checks stored data aggressively and stops at the first error |
| `Options.WriteBufferSize` | Memory to fill before writing a sorted file (default 4 MB) |
| `Options.MaxOpenFiles` | The open-file budget (default 1000) |
| `Options.BlockSize` | Uncompressed data per block (default 4 KB) |
| `Options.RestartInterval` | Keys between delta-encoding restart points (default 16) |
| `Options.CompressionLevel` | `NoCompression`, or `SnappyCompression`, which is LevelDB's default |
| `Options.Cache` | A `Cache(capacity)` LRU block cache, instead of LevelDB's internal 8 MB cache |
| `Options.Comparator` | A `Comparator` that replaces the default key order |
| `Options.Env` | The `Env` that performs file and background operations |
| `ReadOptions.VerifyCheckSums`, `FillCache`, `Snapshot` | Verifies checksums, skips caching for bulk scans, or reads from a snapshot |
| `WriteOptions.Sync` | Flushes each write to disk before it completes, which is slower but keeps recent writes through a machine crash |

These properties have setters only, except `Env`, `Cache` and `Comparator`. Do not change an `Options` object after a `DB` has opened with it.

### Database Maintenance

```csharp
string stats = db.PropertyValue("leveldb.stats");   // also "leveldb.num-files-at-level<N>"

// With the database closed:
DB.Repair(@"C:\Data\cities");    // recovers what it can from a database that no longer opens, and can lose data
DB.Destroy(@"C:\Data\cities");   // deletes the database
```

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Windows.LevelDB` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [Resources](Resources/README.md) | Holds the 32-bit and 64-bit native `LevelDB.dll` builds that the project embeds as resources. | [source](Resources/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`Cache`](Cache.cs) | class | Native LRU block cache of a fixed byte capacity, assigned through `Options.Cache` |
| [`Comparator`](Comparator.cs) | class | Named custom key order built from an `IComparer<NativeArray>` or `Comparison<NativeArray>`, assigned through `Options.Comparator` |
| [`CompressionLevel`](CompressionLevel.cs) | enum | Block compression choice: `NoCompression` or `SnappyCompression` |
| [`DB`](DB.cs) | class | Open LevelDB database with string and byte-array `Put`, `Get`, `Delete`, batch `Write`, iterators, snapshots, properties and static `Repair` and `Destroy` |
| [`Env`](Env.cs) | class | Handle to LevelDB's default operating-system environment for file and background work, assigned through `Options.Env` |
| [`Iterator`](Iterator.cs) | class | Cursor over entries in key order with `Seek`, `SeekToFirst`, `SeekToLast`, `Next`, `Prev` and key and value accessors |
| [`LevelDBException`](LevelDBException.cs) | class | Error reported by the native library, raised by `Check` when a call returns an error pointer |
| [`LevelDBHandle`](LevelDBHandle.cs) | abstract class | Disposable base for every wrapper that owns a native handle and frees it in `FreeUnManagedObjects` |
| [`LevelDBInterop`](LevelDBInterop.cs) | static class | P/Invoke declarations for the LevelDB C API, with a static constructor that extracts and loads the embedded x86 or x64 `LevelDB.dll` |
| [`Options`](Options.cs) | class | Database open settings such as `CreateIfMissing`, buffer and block sizes, compression, cache, comparator and environment |
| [`ReadOptions`](ReadOptions.cs) | class | Read settings for checksum verification, cache filling and reading from a `SnapShot` |
| [`SnapShot`](SnapShot.cs) | class | Immutable point-in-time view of a `DB`, released through its parent database when disposed |
| [`WriteBatch`](WriteBatch.cs) | class | Chainable list of puts and deletes that `DB.Write` applies atomically in order, with an `Iterate` callback walk |
| [`WriteOptions`](WriteOptions.cs) | class | Write settings, currently `Sync`, which flushes each write to disk before it completes |
<!-- domains:end -->

## ⚠️ Important Notes

- Keys stay sorted. LevelDB's default comparator orders them byte by byte, and `Options.Comparator` can supply another order.
- A `DB` is safe to use from several threads without external locking. LevelDB lets only one process open a database directory at a time.
- Each wrapper owns a native handle. Dispose the `DB`, its iterators and snapshots, batches and option objects, or their finalizers release the handles later. Dispose iterators and snapshots before the `DB` that created them.
- The native library loads only on Windows, and the package includes builds for x86 and x64 processes. In any other process architecture, such as ARM64, the first LevelDB call fails with a `TypeInitializationException` that wraps a `PlatformNotSupportedException`.
- `WriteBatch.Iterate` reports each buffered put and delete to `WriteBatch.PutCallback` and `WriteBatch.DeleteCallback` delegates. Their key and value pointers are valid only during the callback.
- **LevelDB License Compliance**: This package embeds LevelDB native binaries, which are distributed under LevelDB's BSD 3-Clause License - see the License Attribution section below.

## 📦 Dependencies

- **Sphere10 Framework**: Core framework library
- **LevelDB**: Native LevelDB database engine (BSD License), embedded as the `LevelDB32.dll` and `LevelDB64.dll` resources

## 📜 License Attribution

This package includes or uses the following third-party components:

### LevelDB
- **License**: BSD 3-Clause License
- **Source**: https://github.com/google/leveldb
- **Notice**: LevelDB is distributed under the BSD 3-Clause License, which permits redistribution in source and binary forms under the conditions in its LICENSE file.

For complete license details, see the [LevelDB LICENSE file](https://github.com/google/leveldb/blob/master/LICENSE).

## 📄 Related Projects

- [Sphere10.Framework.Windows.LevelDB.Tests](../../tests/Sphere10.Framework.Windows.LevelDB.Tests) - NUnit tests for opening, CRUD, iteration, enumeration, snapshots, batches, repair and database properties. Run them with `dotnet test tests/Sphere10.Framework.Windows.LevelDB.Tests/Sphere10.Framework.Windows.LevelDB.Tests.csproj`. Each test opens its database in a new folder under `Sphere10.Framework.Windows.LevelDB.Tests` in the user's temporary folder and deletes it afterwards.
- [Sphere10.Framework.Windows](../Sphere10.Framework.Windows/README.md) - Windows platform integration


