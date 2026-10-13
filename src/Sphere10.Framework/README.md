<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 🧪 Sphere10 Framework

**Developer**: Herman Schoenfeld  
**Copyright**: © 2018-Present Herman Schoenfeld  
**License**: MIT<br>
**Status**: Production-Ready

---

## 📋 Project Overview

Sphere10 Framework is a **low-level, high-performance .NET utility library** providing composable data structures and persistence primitives. It excels at scenarios requiring fine-grained control over memory, serialization, and transactional semantics—think blockchain systems, embedded databases, high-volume analytics, and custom storage layers.

Unlike general-purpose libraries, Sphere10 Framework doesn't provide application frameworks or abstractions. Instead, it offers:

- **50+ collection types** (extended lists, stream-mapped, paged, recyclable, merkle-aware)
- **Advanced serialization framework** (polymorphism, references, versioning, constant-size encoding)
- **Transactional ACID primitives** (scopes, streams, collections with commit/rollback)
- **Merkle-tree implementations** (flat, simple, long, partial) for integrity proofs
- **Cryptographic utilities** (hashing, signatures, key derivation, VRF, post-quantum schemes)
- **Clustered streams** (multi-stream storage, dynamic allocation, attachments)
- **Thread-safe concurrent collections** and producer-consumer patterns
- **50+ string, enumerable, task, stream, and type extension methods**

**Key Attributes**
- **Language**: C# targeting .NET 10
- **Dependencies**: Zero external dependencies for core functionality (optional: BouncyCastle, Newtonsoft.Json)
- **Platform Support**: Windows, Linux, macOS, iOS, Android
- **Philosophy**: Composable, explicit, performance-conscious, extensible, correct
- **Tests**: [Comprehensive test suite](../../tests/Sphere10.Framework.Tests/) with 25+ subsystems and 2000+ tests
- **Maturity**: Production-ready (v3.1.3) with battle-tested core subsystems

## 📦 Installation

```bash
dotnet add package Sphere10.Framework
```

## Typed interface adapters

Generic contracts supply default implementations for their inherited non-generic
members. Implement the typed operations when creating an
`IStreamMappedCollection<T>`, `ISynchronizedObject<TReadScope, TWriteScope>`,
typed protocol handler, `IDigitalSignatureScheme<TPrivateKey, TPublicKey>`,
`IOTSAlgorithm<TConfig>` or `ICloneable<T>`; the interface supplies the conversion
to the base contract. Protocol implementations continue to provide their own
`MessageType`, `RequestType` and `ResponseType` properties. Extended collection,
stack, queue, page and lookup
interfaces also supply their inherited collection adapters. `IExtendedBTreeLookup<K, V>`
provides shared LINQ lookup adapters while existing `IBTreeLookup<K, V>`
implementations retain their original contract.

`IObjectSpaceDimensionBuilder<TBuilder>` preserves fluent return types while
providing the non-generic builder view. `ITransactionalScope<TRootScope>` retains
the root's concrete type. `ICache<TKey, TValue>` can project typed operations over
an implementation of the object-based cache contract, and `IJobSchedule`
supplies its chronological comparison.

`IStreamMappedDictionary<TKey, TValue>` also inherits
`IStreamMappedCollection<TKey, TValue>`, a collection of logical key/value entries.
Its `ObjectStream` remains non-generic because standard dictionaries store pairs
and constant-length-key dictionaries store values with a separate key index.

Existing public class methods and inherited dispatch remain supported.
Compatibility adapters remain where class overrides, interface remapping,
backing storage or event subscriptions affect behavior. Interface bridges are
implemented directly in their `Base Interface Bridges` regions, preserving checked
collection counts and indexes. Class implementations take
precedence over interface defaults, and defaults are called through interface
references.

## 🛠️ Tools.* Namespace — Global Tooling Framework

The **Tools namespace** is a defining feature of Sphere10 Framework, providing a **global, IntelliSense-discoverable collection of static utility methods** organized by domain. This acts as a single point of discovery for developers—instead of searching for the right helper class, simply type `Tools.` and explore available operations across the entire framework.

> 📖 **Getting Started**: See the [Tools Reference](../../docs/tools-reference.md) for the complete catalog and usage patterns.

### Core Tools Classes

**Framework-Wide Utilities**
- **Tools.Array** – Array concatenation, cloning, filling and transposition
- **Tools.Collection** – Generation, repetition, partitioning, searching and array resizing
- **Tools.Crypto** – Secure random bytes, password hashing, AES helpers and secure erase
- **Tools.Time** – Unix-time conversion and date calculations
- **Tools.Enums** – Enumeration utilities, descriptions, conversion
- **Tools.Exceptions** – Exception handling, formatting, analysis
- **Tools.Expressions** – Compiled unary and binary operation factories
- **Tools.FileSystem** – File I/O, directory operations, temp file management
- **Tools.Lambda** – Delegate wrappers and setter expressions
- **Tools.Mail** – Email composition and sending utilities
- **Tools.Maths** / **Tools.MathPlus** – Mathematical operations and numeric calculations
- **Tools.Memory** – Memory metrics, primitive sizing and byte formatting
- **Tools.Network** – Network utilities, IP operations, connectivity
- **Tools.Object** – Object cloning, copying, inspection
- **Tools.OperatorTool** – Generic operator invocation (Add, Subtract, etc.)
- **Tools.Parser** – String parsing, type conversion
- **Tools.Reflection** – Reflection member access and invocation
- **Tools.Runtime** – Build flags, executable paths, entry-assembly access and design-mode checks
- **Tools.Scope** – Cleanup delegates and file/directory lifetime scopes
- **Tools.Spans** – Span and memory utilities
- **Tools.Streams** – Stream operations, routing, reading/writing and transformations
- **Tools.Text** – String manipulation, formatting, generation
- **Tools.Threads** – Threading utilities, synchronization, async helpers
- **Tools.Url** – URL parsing, encoding, manipulation
- **Tools.Values** – Value utilities, comparisons, clipping and futures

### Platform-Specific Tools

**Windows Integration**
- **Tools.Windows** – Registry, services, security, processes, Win32 and shell facade (`Sphere10.Framework.Windows`)
- **Tools.WinForms** – Cursor and window-settings utilities (`Sphere10.Framework.Windows.Forms`)

**Web & Networking**
- **Tools.Web.Html** – HTML helpers (`Sphere10.Framework.Web`)
- **Tools.Web.AspNetCore** – ASP.NET Core helpers (`Sphere10.Framework.Web.AspNetCore`)
- **Tools.Web.Downloader** – HTTP downloads (core `Sphere10.Framework`)

**Database Access**
- **Tools.Data** – Generic database helpers (`Sphere10.Framework.Data`)
- **Tools.Sqlite** – SQLite utilities (`Sphere10.Framework.Data.Sqlite`)
- **Tools.MSSQL** – SQL Server utilities (`Sphere10.Framework.Data.MSSQL`)
- **Tools.NHibernate** – String-mapping compatibility constant (`Sphere10.Framework.Data.NHibernate`)
- **Tools.Xml** – XML data helpers (`Sphere10.Framework.Data`)
- **Tools.Json** – JSON data helpers (`Sphere10.Framework.Data`)

**Mobile Platforms**
- **Tools.iOSTool** – iOS-specific utilities (`Sphere10.Framework.iOS`)

**Application Framework**
- **Tools.Config** – Application-setting reads (`Sphere10.Framework.Application`)
- **Tools.NUnit** – NUnit test helpers (`Sphere10.Framework.NUnit`)
- **Tools.Drawing** – Graphics and drawing helpers (`Sphere10.Framework.Drawing`)
- **Tools.Debugger** – Debug-time utilities (core `Sphere10.Framework`)

### Usage Pattern

```csharp
using System.Linq.Expressions;
using Sphere10.Framework;

public static class ToolsUsageExample {
	public static int[] GenerateAndGrow(int count) {
		Guard.Argument(count >= 0, nameof(count), "Count must be nonnegative.");
		var values = Tools.Collection.GenerateArray(count, index => index);
		Tools.Collection.ResizeArray(ref values, checked((long)count + 1));
		return values;
	}

	public static int Add(int left, int right)
		=> Tools.Expressions.CreateExpression<int, int, int>(Expression.Add)(left, right);

	public static byte[] HashData(byte[] data) {
		Guard.ArgumentNotNull(data, nameof(data));
		return Hashers.Hash(CHF.SHA2_256, data);
	}
}
```

`Tools` is a global namespace. Select the existing facade and its owning project; importing `Sphere10.Framework` supplies contracts such as `Guard` and `Hashers`. Follow the [utility practices](../../docs/guidelines/patterns/framework-utilities.md) and [Tools Reference](../../docs/tools-reference.md) for the concrete API and ownership rules.

### Design Philosophy

The Tools namespace embodies several key principles:

1. **Discovery-First**: Type `Tools.` to explore all available operations in IntelliSense
2. **Consistency**: Related operations grouped under consistent tool names
3. **Extensibility**: Projects extend with their own Tools.* classes (e.g., `Tools.Windows` and `Tools.WinForms`)
4. **Composability**: Tools methods work together seamlessly for complex operations
5. **Performance**: Methods optimized for production use, with zero-allocation variants where possible
6. **Type Safety**: Generic methods preserve type information, avoiding casts

### Extending with Custom Tools

Extend the existing facade when it already owns the operation. A new domain can add a static class in `namespace Tools;`, following the framework's source headers, guards and naming conventions. See [ArrayTool.cs](Collections/Arrays/ArrayTool.cs) for the declaring pattern and the [Tools Reference](../../docs/tools-reference.md) for contributor guidance. The class becomes discoverable through `Tools.` when its owning project or package is referenced.

## 🚀 Quick Navigation

### Core Sphere10 Framework Library Domains

**Data Structures & Collections**
- 📦 [Collections/](Collections/) – Extended lists, stream-mapped, recyclable, paged, observable, synchronized variants
- 🌳 [Merkle/](Merkle/) – Merkle-tree implementations: flat, simple, long, partial
- 🔗 [ClusteredStreams/](ClusteredStreams/) – Multi-stream storage, attachments, dynamic allocation

**Persistence & Serialization**
- 💾 [Serialization/](Serialization/) – Item serializers, polymorphism, references, versioning
- 📊 [ObjectSpaces/](ObjectSpaces/) – Typed dimensions, indexing, integrity proofs, transactions
- 🔄 [ObjectStream/](ObjectStream/) – Stream-backed object storage with indexes

**Transactions & Scoping**
- 🎯 [Scopes/](Scopes/) – Transactional boundaries, commit/rollback, isolation levels
- 💳 [Transactions/](Transactions/) – Transaction management and coordination

**Cryptography & Security**
- 🔐 [Crypto/](Crypto/) – Hashing, signatures, key derivation, VRF algorithms
- 🔑 [Protocol/](Protocol/) – Protocol abstractions and messaging

**Streams & I/O**
- 📝 [Streams/](Streams/) – Stream decorators, bounded, fragmented, extended memory
- 📄 [IO/](IO/) – File I/O operations and utilities

**Utilities & Extensions**
- ✂️ [Text/](Text/) – String extensions, formatting, case operations, validation
- 🔤 [Encoding/](Encoding/) – VarInt, CVarInt, compact integer encoding
- 🧠 [Memory/](Memory/) – Buffer operations, memory allocation, management
- ⚡ [Threading/](Threading/) – Synchronization, producer-consumer patterns
- 💾 [Cache/](Cache/) – Action caches, reaping policies, session management
- 🗄️ [Repository/](Repository/) – Repository pattern implementations
- 🔍 [Mapping/](Mapping/) – Object mapping and transformation
- 🎲 [Conversion/](Conversion/) – Type conversions and parsing
- 📐 [Maths/](Maths/) – Mathematical utilities
- 📅 [Values/](Values/) – Value types and structures
- 🏗️ [Comparers/](Comparers/) – Comparison and equality implementations

**Type System & Reflection**
- 🔎 [Introspection/](Introspection/) – Reflection and type analysis
- 📦 [Types/](Types/) – Type utilities and checks
- 🏷️ [Attributes/](Attributes/) – Custom attributes
- 📋 [Framework/](Framework/) – Framework abstractions
- 🔌 [Extensions/](Extensions/) – Framework extension methods

**Data & Resources**
- 🌍 [DataSource/](DataSource/) – Data source abstractions
- 🔌 [Peripherals/](Peripherals/) – Hardware peripherals access
- 🌐 [Network/](Network/) – Network utilities
- 🎯 [Environment/](Environment/) – Environment and system information
- ⚙️ [Misc/](Misc/) – Miscellaneous utilities
- 📦 [Objects/](Objects/) – Object utilities
- 🎛️ [Filter/](Filter/) – Filtering abstractions
- 📊 [Functional/](Functional/) – Functional programming utilities
- ♻️ [Disposables/](Disposables/) – Disposable pattern utilities
- 🔗 [Events/](Events/) – Event handling utilities
- ⚠️ [Exceptions/](Exceptions/) – Exception handling
- 📖 [Loadable/](Loadable/) – Loadable resource abstractions
- 📝 [Logging/](Logging/) – Logging utilities
- 💾 [Persistable/](Persistable/) – Persistence abstractions
- 🔧 [Saveable/](Saveable/) – Save/load patterns
- ⏰ [Scheduler/](Scheduler/) – Task scheduling
- 📏 [Sizing/](Sizing/) – Size calculations and measurements
- 🎯 [Spans/](Spans/) – Span and memory utilities
- 📝 [TextWriters/](TextWriters/) – Text writing abstractions

### Related Projects

**Data Access & Persistence**
- [Sphere10.Framework.Data](../Sphere10.Framework.Data/) – Database abstraction layer (SQL Server, SQLite, Firebird)
- [Sphere10.Framework.Data.Sqlite](../Sphere10.Framework.Data.Sqlite/) – SQLite-specific integration
- [Sphere10.Framework.Data.MSSQL](../Sphere10.Framework.Data.MSSQL/) – SQL Server integration
- [Sphere10.Framework.Data.Firebird](../Sphere10.Framework.Data.Firebird/) – Firebird integration
- [Sphere10.Framework.Data.NHibernate](../Sphere10.Framework.Data.NHibernate/) – NHibernate ORM integration
- [Sphere10.Framework.Windows.LevelDB](../Sphere10.Framework.Windows.LevelDB/) – High-performance embedded key-value store

**Networking & Communications**
- [Sphere10.Framework.Communications](../Sphere10.Framework.Communications/) – Multi-protocol networking (RPC, TCP, UDP, WebSockets)
- [Sphere10.Framework.Web.AspNetCore](../Sphere10.Framework.Web.AspNetCore/) – ASP.NET Core integration and middleware

**Cryptography & Consensus**
- [Sphere10.HashLib4CSharp](../Sphere10.HashLib4CSharp/) – Hashes & checksums (SHA-1/2/3, BLAKE2, CRC; stream/file APIs)
- [Sphere10.Framework.CryptoEx](../Sphere10.Framework.CryptoEx/) – Extended cryptography (ECDSA, ECIES, Schnorr, post-quantum)
- [Sphere10.Framework.Consensus](../Sphere10.Framework.Consensus/) – Blockchain consensus mechanisms

**Desktop & Platform Integration**
- [Sphere10.Framework.Windows](../Sphere10.Framework.Windows/) – Windows utilities (registry, services, events, security)
- [Sphere10.Framework.Windows.Forms](../Sphere10.Framework.Windows.Forms/) – WinForms component integration
- [Sphere10.Framework.Windows.Forms.Sqlite](../Sphere10.Framework.Windows.Forms.Sqlite/) – SQLite integration for WinForms
- [Sphere10.Framework.Windows.Forms.MSSQL](../Sphere10.Framework.Windows.Forms.MSSQL/) – SQL Server integration for WinForms
- [Sphere10.Framework.Windows.Forms.Firebird](../Sphere10.Framework.Windows.Forms.Firebird/) – Firebird integration for WinForms
- [Sphere10.Framework.Drawing](../Sphere10.Framework.Drawing/) – Graphics and drawing utilities

**Application Framework**
- [Sphere10.Framework.Application](../Sphere10.Framework.Application/) – Lifecycle management, DI, configuration, settings

**Testing & Quality**
- [Sphere10.Framework.NUnit](../Sphere10.Framework.NUnit/) – NUnit testing utilities
- [Sphere10.Framework.NUnit.DB](../Sphere10.Framework.NUnit.DB/) – Database testing utilities

**Cross-Platform & Generators**
- [Sphere10.Framework.Android](../Sphere10.Framework.Android/) – Android platform integration
- [Sphere10.Framework.iOS](../Sphere10.Framework.iOS/) – iOS platform integration
- [Sphere10.Framework.macOS](../Sphere10.Framework.macOS/) – macOS platform integration
- [Sphere10.Framework.Generators](../Sphere10.Framework.Generators/) – Code generation utilities

### Test Suites

**Core Library Tests**
- [Sphere10.Framework.Tests](../../tests/Sphere10.Framework.Tests/) – Comprehensive test suite for all Sphere10 Framework domains

**Related Project Tests**
- [Sphere10.Framework.Communications.Tests](../../tests/Sphere10.Framework.Communications.Tests/) – Networking and protocol tests
- [Sphere10.Framework.CryptoEx.Tests](../../tests/Sphere10.Framework.CryptoEx.Tests/) – Extended cryptography tests
- [Sphere10.Framework.Data.Tests](../../tests/Sphere10.Framework.Data.Tests/) – Database integration tests
- [Sphere10.Framework.Windows.Tests](../../tests/Sphere10.Framework.Windows.Tests/) – Windows utilities tests
- [Sphere10.Framework.Windows.LevelDB.Tests](../../tests/Sphere10.Framework.Windows.LevelDB.Tests/) – LevelDB integration tests
- [Sphere10.HashLib4CSharp.Tests](../../tests/Sphere10.HashLib4CSharp.Tests/) – Cryptographic hash tests

---

## 🎨 Design Philosophy

### Core Principles

**Composability**: The library is structured around small, focused abstractions that compose predictably. Decorators, adapters, and interfaces allow developers to layer functionality without tight coupling.

**Explicit Control**: Sphere10 Framework favors explicitness over magic. Memory allocation strategies, serialization formats, caching policies, and locking semantics are configurable rather than hidden behind opaque defaults.

**Performance-Conscious**: Many components are optimized for batch operations, memory locality, and reduced allocations. The library provides both in-memory and stream-backed variants of collections to accommodate different performance/capacity tradeoffs.

**Extensibility**: Core abstractions like `IItemSerializer<T>`, `IExtendedList<T>`, and `ITransactionalScope` are designed to be implemented or decorated by user code. The library provides building blocks rather than closed systems.

**Correctness**: Transaction-aware data structures emphasize ACID semantics where applicable. Merkle tree implementations prioritize cryptographic correctness. Thread-safety guarantees are explicit and documented.

### ❌ Non-Goals

- **High-level Application Frameworks**: Sphere10 Framework does not provide MVC frameworks, dependency injection containers, or application scaffolding.
- **Platform Abstractions**: The library does not abstract away platform-specific APIs beyond what .NET Standard requires.
- **Opinionated Workflows**: While the library enables patterns like repositories and transactional scopes, it does not enforce specific architectural patterns.

## 🗂️ Domains Covered

### Collections

Sphere10 Framework provides an extensive suite of collection types that extend beyond the standard .NET collections:

- **Extended Lists and Collections**: Interfaces like `IExtendedList<T>` and `IExtendedCollection<T>` support range-based operations (batch reads, writes, insertions, deletions) for improved performance when working with large datasets.
- **Stream-Mapped Collections**: Collections such as `StreamMappedList<T>`, `StreamMappedDictionary<TKey, TValue>`, and `StreamMappedHashSet<T>` persist their data to streams, enabling collections that exceed available memory while maintaining list/dictionary/set semantics.
- **Recyclable Lists**: `IRecyclableList<T>` and its implementations maintain a pool of reusable indices for deleted items, optimizing scenarios with frequent insertions and deletions.
- **Paged Collections**: Both memory-paged and file-paged lists partition data into pages, supporting arbitrarily large datasets with configurable memory footprints.
- **Observable Collections**: Observable variants of standard collections expose events for monitoring mutations, supporting use cases like change tracking and logging.
- **Synchronized Collections**: Thread-safe variants like `SynchronizedExtendedList<T>`, `SynchronizedDictionary<TKey, TValue>`, and `ProducerConsumerQueue<T>` for concurrent scenarios.
- **Specialized Data Structures**: Bloom filters, binary heaps, circular lists, bounded lists, and bidirectional dictionaries address specific algorithmic needs.

Managed and stream-mapped B-tree, B+ and grouped lookup implementations expose family-specific new-tree defaults through `Sphere10FrameworkDefaults`. Stream constructors/factories that omit order require empty storage; existing trees require their original explicit order, serializer layout and endianness. The [B-tree profiling guide](../../utils/Sphere10.Framework.Utils.Profiler/BTrees/README.md) explains the measured numeric and explicit wide-key settings, backend comparisons and limitations. These defaults do not migrate stored nodes or change generic ObjectStream index layouts.

#### Memorizing an enumerable

`MemorizingIterator<T>` lazily reads its source once and caches the values for repeated or interleaved traversals. Each traversal has its own position; struct copies and interface casts share the same cache. Its read-only collection members support `Count`, `Contains`, `CopyTo`, and LINQ materialization (`ToArray`/`ToList`). `Count` and `CopyTo` finish reading the source, retaining the memorized snapshot even if the original collection later changes.

The source enumerator is disposed when exhausted or when reading fails. A partial traversal keeps the source open so later traversals can continue; finish enumeration when the source owns resources. Source failures are replayed after the same cached prefix. Access from multiple threads requires external synchronization.

### 🔗 Clustered Streams

The `ClusteredStreams` subsystem provides a sophisticated mechanism for managing multiple logical streams within a single underlying stream. This enables:

- **Multi-Stream Storage**: A single file or stream can host multiple independent logical streams, each with its own lifecycle and addressing.
- **Dynamic Allocation**: Streams grow and shrink dynamically, with clusters allocated and linked as needed.
- **Metadata Storage**: Headers and metadata are stored alongside stream data, supporting features like indexing and merkle-tree integration.
- **Attachments**: Pluggable `IClusteredStreamsAttachment` components allow behaviors like indexing, merkle-tree maintenance, and key storage to be composed declaratively.

This architecture underpins the library's stream-mapped collections and object spaces, providing a flexible foundation for custom persistence schemes.

### 🌌 Object Spaces

Object spaces abstract the storage and retrieval of typed objects across multiple "dimensions" (logical tables). Key capabilities include:

- **Typed Dimensions**: Each dimension stores objects of a specific type, with configurable serialization and indexing strategies.
- **Indexing**: Automatic or custom indexes accelerate lookups by projected keys. Both unique and non-unique indexes are supported.
- **Merkle-Tree Integration**: Optional merkle-tree attachments provide cryptographic proofs of data integrity.
- **File Transactions**: `FileObjectSpace` exposes explicit synchronous commit/rollback. Saved changes remain pending until commit; rollback invalidates previously tracked instances.

Object spaces provide structured embedded persistence. See the [ObjectSpace guide](ObjectSpaces/README.md) for schemas, live-record enumeration, identity and ownership, unique/nonunique queries, autosave and current graph limitations. New stores use `Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder` (256), overridable through `WithIndexBTreeOrder`. Opening existing stores requires their original order or an already bound matching definition; historical default stores used 64. Follow the guide's [storage-opening contract](ObjectSpaces/README.md#new-store-defaults-and-existing-storage) and preserve the original layout. The [WinForms tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) retains its original ObjectSpace screen. The [ObjectSpace suite](../../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md) in the general profiling project compares storage and index settings using reproducible workloads.

### 🌳 Merkle Trees

Sphere10 Framework includes multiple merkle-tree implementations optimized for different use cases:

- **Flat Merkle Trees**: Store all nodes in contiguous memory, optimized for fast random access and proof generation.
- **Simple Merkle Trees**: Lazily compute parent nodes, suitable for smaller trees where memory is less constrained.
- **Long Merkle Trees**: Designed for very large datasets, retaining only sub-root hashes in memory while supporting append and proof operations.
- **Partial Merkle Trees**: Maintain only a subset of nodes, useful for constructing and verifying multi-item proofs.

These implementations integrate with collections, enabling `IMerkleList<T>`, `IMerkleDictionary<TKey, TValue>`, and `IMerkleSet<T>` variants that maintain cryptographic integrity proofs alongside their data.

### 🔐 Cryptography

The library provides cryptographic primitives and utilities:

- **Hashing**: Wrappers for standard hash functions (SHA-256, SHA-512, SHA-3, etc.) plus specialized implementations like BLAKE2b and MurMur3.
- **Digital Signatures**: Abstractions for signature schemes, including stateless schemes (ECDSA, EdDSA, Schnorr) and post-quantum candidates (W-OTS, W-OTS#, AMS).
- **Key Derivation**: PBKDF2 and custom key derivation functions.
- **Verifiable Random Functions (VRF)**: Primitives for generating cryptographically verifiable random outputs.
- **Data Protection**: Secure memory handling and encryption utilities for sensitive data.

### 📦 Serialization

Sphere10 Framework's serialization framework is designed for efficiency, control, and extensibility:

- **Item Serializers**: The `IItemSerializer<T>` abstraction enables custom serialization logic for any type. Serializers can be composed, decorated, and registered in a `SerializerFactory`.
- **Polymorphic Serialization**: Support for serializing and deserializing class hierarchies with type discrimination using `[KnownSubType]` attributes.
- **Reference Handling**: Automatic tracking and resolution of object references and cycles within a serialization context.
- **Constant-Size Serialization**: Specialized serializers for fixed-width data, enabling efficient indexing and random access.
- **Versioning**: Support for versioned serialization strategies, allowing schemas to evolve over time.

The framework integrates deeply with the library's collections and storage primitives, ensuring that persistence strategies are explicit and customizable.

### ⚙️ Transactions

The transactional subsystem provides commit/rollback primitives for configured file-backed collections and custom backends:

- **Transactional Scopes**: `ITransactionalScope` defines begin/commit/rollback operations. Context-aware scopes coordinate nesting and ownership; the concrete backend determines isolation and participation.
- **Transactional Collections**: `TransactionalList<T>`, `TransactionalDictionary<TKey, TValue>`, and `TransactionalHashSet<T>` provide ACID semantics over persistent storage.
- **Transactional Streams**: `TransactionalStream` wraps a stream with commit/rollback capabilities, enabling atomic multi-operation updates.
- **File Transactions**: `FileTransaction` and `FileTransactionScope` coordinate file-system operations within a transactional boundary.

These primitives enable building robust, crash-recoverable data stores without relying on external database engines.

### 💾 Caching

The caching subsystem offers flexible, policy-driven caching mechanisms:

- **Action Caches**: Populate cache entries on-demand using delegates.
- **Bulk-Fetch Caches**: Refresh all entries in a single operation when any entry becomes stale.
- **Session Caches**: Expire entries based on last-access time, suitable for session-style semantics.
- **Reaping Policies**: Both isolated and pooled reaper implementations manage capacity constraints across single or multiple cache instances.

### 📡 Protocol and Communication

The protocol subsystem facilitates structured, bidirectional communication between peers:

- **Protocol Definition**: Define protocols with typed messages, commands, requests, responses, and handshakes.
- **Orchestration**: `ProtocolOrchestrator` manages message dispatch, handshake workflows, and request/response correlation.
- **Extensibility**: Handlers for commands, requests, responses, and handshakes can be implemented via interfaces or delegates.

This framework is suitable for building custom RPC mechanisms, control protocols, or peer-to-peer communication layers.

### 📲 Streams and I/O

Sphere10 Framework extends .NET's stream abstractions with specialized implementations:

- **Bounded Streams**: Restrict read/write operations to a defined segment of an underlying stream.
- **Fragmented Streams**: Compose multiple disparate byte fragments into a single logical stream.
- **Extended Memory Streams**: Use `IBuffer` as the backing store instead of a contiguous byte array, enabling arbitrarily large in-memory streams with paging support.
- **Transactional Streams**: Wrap streams with commit/rollback semantics.
- **Decorator Streams**: Read-only, write-only, non-closing, and profiling stream wrappers.

### 📝 Logging

A flexible, composable logging framework:

- **Logger Abstractions**: `ILogger` defines a simple, level-based logging interface.
- **Decorators**: Prefix, timestamp, thread-ID, and synchronization decorators compose to customize log output.
- **Sinks**: Built-in loggers for console, debug output, files, and custom delegates.
- **Multicast Logging**: Route log messages to multiple sinks simultaneously.

### 🔄 Threading and Concurrency

Utilities for managing concurrency and synchronization:

- **Custom Synchronization Primitives**: `ProducerConsumerLock`, `NonReentrantLock`, `FastLock`, and minimal semaphore implementations.
- **Serial Thread Pool**: Execute actions serially on a background thread, with configurable lifecycle policies.
- **Critical Sections**: `Critical<T>` and `CriticalObject` encapsulate objects with lock-based access.
- **Producer-Consumer Queue**: `ProducerConsumerQueue<T>` provides bounded/unbounded thread-safe queuing with async support.

### ⏰ Scheduling

A job scheduling framework with support for various triggers:

- **Job Definitions**: Wrap actions or async functions as jobs.
- **Schedules**: Interval-based, day-of-week, and day-of-month schedules trigger job execution.
- **Policies**: Control job behavior on failure, completion, and rescheduling.

### :pencil2: Text Extensions & String Utilities

Comprehensive string manipulation and validation helpers:

- **Case Conversion**: Convert between camelCase, PascalCase, snake_case, kebab-case, and UPPERCASE_SNAKE_CASE.
- **Truncation & Padding**: Pad strings to exact lengths or truncate with ellipsis.
- **Validation**: Check for numeric, alphabetic, alphanumeric, hexadecimal, and custom patterns.
- **Parsing**: Safe parsing with tuple results, parse integers, GUIDs, decimals, and custom types.
- **Whitespace Operations**: Split on whitespace, trim variants, normalize line endings.
- **String Replacement**: Pattern-based and regex replacements with proper escaping.

### 🔤 Encoding Utilities

Efficient, space-optimizing encoding schemes:

- **Variable-Length Integer Encoding (VarInt)**: Compress small integers into fewer bytes, with sign-aware variants.
- **Compact Variable-Length (CVarInt)**: Further optimize encoding for extremely small or sparse datasets.
- **Binary Encoding**: Efficient methods for encoding multiple values into contiguous buffers.
- **Bidirectional Encoding**: Reversible encoding schemes that preserve ordering properties for indexed storage.

### 🧠 Memory Management

Utilities for controlling and optimizing memory usage:

- **Buffer Abstractions**: `IBuffer` represents a sequence of bytes that can be memory-resident or memory-mapped.
- **Memory Pools**: Reusable memory pools to reduce allocation pressure in high-throughput scenarios.
- **Buffer Decorators**: Add synchronization, capacity tracking, or transaction support to buffers.
- **Byte Array Utilities**: Comparison, concatenation, and transformation helpers for byte arrays.

### 🔍 Object Mapping & Transformation

Map and transform objects between representations:

- **Property Mapping**: Copy values between objects with matching or custom property mappings.
- **Conversion Chains**: Compose multiple converters to transform between unrelated types.
- **Specialized Mappers**: Collections, dictionary, and array mapping with configurable merge strategies.

### 🎲 Type Conversion & Parsing

Flexible conversion utilities for type coercion:

- **Bidirectional Conversion**: Convert between any two types using registered converters.
- **Numeric Conversions**: Safe conversions between integer types with overflow checking.
- **Enum Conversions**: String to/from enum with case-insensitive options.
- **Collection Conversions**: Convert between array, list, set, and dictionary representations.
- **Custom Converters**: Register converters for application-specific types.

### 📐 Mathematical Utilities

Low-level math helpers and calculations:

- **Bit Manipulation**: Bit-counting, set/clear operations, endian conversions.
- **GCD and LCM**: Greatest common divisor and least common multiple calculations.
- **Prime Number Operations**: Primality testing and prime enumeration.
- **Modular Arithmetic**: Modular exponentiation and inversion.
- **Rounding**: Floor, ceiling, and banker's rounding for financial calculations.

### 📅 Value Types & Structures

Utilities for working with value types and immutable structures:

- **Date/Time Wrappers**: Normalized date/time representations for storage.
- **Numeric Types**: Custom numeric wrappers for specialized domains (e.g., hash values, checksums).
- **Struct Utilities**: Struct cloning, comparison, and hashing helpers.

### 🏗️ Comparison & Equality

Build custom comparison and equality implementations:

- **Comparer Factories**: Create `IComparer<T>` instances with fluent composition (field-by-field, descending, custom).
- **Equality Comparers**: Implement `IEqualityComparer<T>` for custom equality logic.
- **Structural Comparison**: Compare complex objects field-by-field with customizable rules.
- **Tolerance-Based Comparison**: Compare floating-point values with configurable epsilon.

### 🔎 Introspection & Reflection

Reflection utilities and type analysis:

- **Type Discovery**: Find derived types, implementations of interfaces, and generic arguments.
- **Property/Field Analysis**: Discover properties, fields, and their attributes with caching.
- **Method Resolution**: Find methods by name, signature, and attributes.
- **Attribute Retrieval**: Efficiently retrieve custom attributes from types, methods, and properties.
- **Generic Type Analysis**: Decompose and analyze generic type definitions and arguments.

### 📦 Type Utilities

Helpers for type checks and resolution:

- **Type Verification**: Check if a type is numeric, nullable, enumerable, etc.
- **Type Classification**: Categorize types (value, reference, generic, etc.).
- **Type Conversion Checks**: Verify if one type can be converted to another.
- **Default Value Generation**: Create default instances for arbitrary types.

### 🏷️ Attributes & Metadata

Custom attributes for annotating types and members:

- **Serialization Attributes**: Mark fields/properties for serialization with options.
- **Validation Attributes**: Declarative validation rules (required, length, range).
- **Mapping Attributes**: Customize property mapping behavior.
- **Custom Attributes**: Base classes for creating domain-specific attributes.

### 📋 Framework Abstractions

Core abstractions for building extensible frameworks:

- **Loadable**: Abstract base for objects with load/reload semantics.
- **Persistable**: Abstract base for objects that need to save/restore state.
- **Saveable**: Simplified save pattern for simpler scenarios.
- **Disposables**: Helpers for implementing safe disposal patterns.
- **Environment**: Abstract environment information (OS, runtime, configuration).

### 🔌 Extension Methods on Core Types

Fluent extensions for .NET types:

- **Enumerable Extensions**: `Batch`, `Chunk`, `Distinct`, `GroupBy` variants with custom comparers.
- **Dictionary Extensions**: `TryAdd`, `AddOrUpdate`, `GetOrAdd` with custom logic.
- **Task Extensions**: Timeout, retry, async coordination helpers.
- **Functional Extensions**: `Map`, `FlatMap`, `Filter` for more expressive LINQ alternatives.

### 🌍 Data Source Abstractions

Abstractions for querying data from various sources:

- **IDataSource**: Generic interface for querying objects from a source (database, file, API).
- **Caching Data Sources**: Wrap data sources with caching layers.
- **Composed Data Sources**: Combine multiple sources with fallback semantics.
- **Filtering Data Sources**: Add filtering and projection to any data source.

### 🔌 Peripherals & Hardware Access

Utilities for hardware and peripheral interaction:

- **Device Detection**: Discover and enumerate hardware peripherals.
- **Serial Communication**: Serial port abstractions for embedded systems.
- **GPIO Abstractions**: General-purpose I/O operations (where applicable).

### 🌐 Network Utilities

Low-level network helpers:

- **Address Resolution**: DNS and IP address utilities.
- **Port Scanning**: Network connectivity testing.
- **Socket Wrappers**: Extended socket abstractions with timeout support.

### 🎯 Environment & System Information

Query system and runtime information:

- **Runtime Detection**: Detect .NET version, platform, architecture.
- **Resource Availability**: Check available memory, processor count, disk space.
- **Configuration**: Access environment variables, system settings.
- **Process Information**: Query process details, threading, memory usage.

### ⚙️ Miscellaneous Utilities

Catch-all category for specialized helpers:

- **Range Calculations**: Compute overlaps, unions, and intersections of value ranges.
- **Enum Utilities**: Enum iteration, flag manipulation, parsing.
- **Bitfield Helpers**: Work with bitfields and bit arrays.
- **Weak References**: Managed weak reference pools to track objects without preventing GC.

### 📦 Object Utilities

Helpers for object manipulation and introspection:

- **Cloning**: Deep and shallow cloning with customizable strategies.
- **Comparison**: Object equality and comparison without reflection overhead (when possible).
- **Hashing**: Consistent hashing for distributed scenarios.

### 🎛️ Filtering Abstractions

Generic filtering framework:

- **Predicates**: Composable `IFilter<T>` implementations for filtering collections.
- **Composite Filters**: Combine filters with AND, OR, and NOT logic.
- **Range Filters**: Filter by numeric or comparable ranges.

### 📊 Functional Programming Utilities

Support for functional programming patterns:

- **Monads**: Maybe/Option and Result types for error handling.
- **Function Composition**: Compose functions with automatic currying.
- **Lazy Evaluation**: Lazy sequences and deferred computation.
- **Partial Application**: Bind arguments to create specialized functions.

### ♻️ Disposable Pattern Helpers

Simplify implementing proper disposal:

- **Base Classes**: `DisposableBase` and `DisposableObject` handle disposal protocol.
- **Safe Finalization**: Finalization helpers to ensure cleanup occurs.
- **Resource Guards**: RAII-style resource management patterns.

### 🔗 Event Handling Utilities

Framework for event routing and aggregation:

- **Event Aggregators**: Pub/sub patterns for decoupled event routing.
- **Event Priorities**: Route events in priority order.
- **Event Filtering**: Conditionally route events based on predicates.

### ⚠️ Exception Handling

Utilities for robust error handling:

- **Exception Wrapping**: Preserve stack traces while wrapping exceptions.
- **Retry Policies**: Exponential backoff, jitter, and custom retry strategies.
- **Exception Aggregation**: Collect multiple exceptions and report together.
- **Error Context**: Attach context information to exceptions for debugging.

### 📖 Loadable Resources

Framework for loading and caching resources:

- **Resource Loaders**: Abstract protocol for loading resources (files, embedded, remote).
- **Caching Loaders**: Cache loaded resources with expiration policies.
- **Lazy Loading**: Load resources on-demand with automatic caching.

### 📝 Logging Framework

Already covered above in detail.

### 💾 Persistable Objects

Framework for objects that maintain persistent state:

- **State Snapshots**: Capture object state for persistence.
- **Transactional Persistence**: Integrate with transactional scopes.
- **Version Tracking**: Track object versions across saves.

### 🔧 Save/Load Patterns

Simplified persistence for straightforward scenarios:

- **Serialization Delegates**: Custom load/save functions per type.
- **Stream-Based Saving**: Write objects to streams with format flexibility.
- **File-Based Persistence**: High-level API for file-based saves.

### 📏 Sizing Utilities

Calculate sizes and offsets:

- **Byte Size Calculation**: Determine how many bytes a serialized object requires.
- **Alignment Calculation**: Compute aligned offsets for memory layout optimization.
- **Capacity Planning**: Estimate total storage needed for collections.

### 🎯 Span & Memory Utilities

Efficient working with `Span<T>` and `Memory<T>`:

- **Span Slicing**: Safe slicing with bounds checking.
- **Memory Pooling**: Rent and return `Memory<T>` from pools.
- **Span Conversion**: Convert between `Span`, `Memory`, and arrays safely.

### 📝 TextWriter Abstractions

Extensions and helpers for `TextWriter`:

- **Formatted Output**: Structured output writers (JSON, XML, CSV).
- **Indented Writers**: Automatic indentation for hierarchical data.
- **Buffered Writers**: Batch write operations for efficiency.

### :hammer_and_wrench: Extensions & Utilities

**50+ Extension Methods** covering:
- `StringExtensions`: Truncation, case handling, validation, parsing, formatting
- `EnumerableExtensions`: Filtering, grouping, transformation, batching
- `TaskExtensions`: Async utilities, timeout handling, retry logic
- `StreamExtensions`: I/O operations, reading/writing helpers
- `TypeExtensions`: Reflection helpers, type resolution
- And many more...

## 💡 Key Concepts

### Extended Lists

`IExtendedList<T>` extends the standard `IList<T>` interface with range-based operations: `ReadRange`, `UpdateRange`, `InsertRange`, and `RemoveRange`. These methods accept long indices and counts, supporting collections larger than 2GB. Implementations are expected to optimize batch operations rather than iterating element-by-element.

### Stream-Mapped Collections

Stream-mapped collections persist their data to streams using serializers and cluster-based storage. They behave like standard collections but their contents reside on disk (or any stream) rather than entirely in memory. This enables collections to scale beyond available RAM while maintaining familiar APIs.

### Object Streams

`ObjectStream<T>` is a low-level primitive for storing a sequence of serialized objects in a stream, along with metadata and indexes. It underpins stream-mapped collections and object spaces, providing features like:

- Recyclable item slots (deleted items can be reused)
- Pluggable indexes for fast lookups
- Merkle-tree integration for integrity proofs
- Metadata tracking (timestamps, checksums, etc.)

### Serialization Context

`SerializationContext` tracks object references and cycles during serialization/deserialization. When a reference-type object is serialized, the context checks if it has been seen before. If so, a reference marker is emitted rather than re-serializing the object. This enables correct handling of cyclic graphs and repeated references.

### Transactional Scopes

`ITransactionalScope` defines the lifecycle of a backend transaction:

- `BeginTransaction()`: Start a new transaction.
- `Commit()`: Commit through the owning scope, respecting nested rollback votes.
- `Rollback()`: Roll back through the owning scope, or record a nested rollback vote.

The corresponding asynchronous methods are `BeginTransactionAsync()`, `CommitAsync()` and `RollbackAsync()`. `BeginTransaction()` returns `void`; the scope itself is consumed with `using` or `await using`.

`TransactionalScopeBase<TTransaction>` extends `ContextScope` and coordinates transaction ownership and nesting within a logical call context. Isolation and which operations participate are determined by the concrete backend; sharing a context does not automatically enlist every `ITransactionalObject`. Disposal closes an owned transaction, but the common base does not automatically call `Rollback()`. See [resource scopes](../../docs/guidelines/patterns/resource-scopes.md) for ownership and cleanup rules.

### Merkle Coordinates and Proofs

A `MerkleCoordinate` identifies a node within a merkle tree by its level and index. Merkle proofs are represented as sequences of `MerkleNode` instances, which can be verified against a root hash to confirm the presence and position of specific leaves. The library's merkle implementations expose methods for generating proofs and verifying them efficiently.

### Decorators and Adapters

Many components follow the decorator pattern, allowing behavior to be layered:

- `StreamDecorator`: Wrap a stream to add logging, profiling, or transaction support.
- `ListDecorator<T>`: Augment list behavior without reimplementing the entire interface.
- `ItemSerializerDecorator<T>`: Transform serialization logic (e.g., add null-handling or reference-tracking).

Adapters convert between related interfaces (e.g., `IList<T>` to `IExtendedList<T>`) to integrate external code with Sphere10 Framework's abstractions.

## 🎯 Typical Use Cases

### Well-Suited Scenarios

- **Embedded Databases**: Build lightweight, file-based data stores with ACID transactions, indexing, and querying without a full database engine.
- **Blockchain and Distributed Ledgers**: Merkle-tree primitives and cryptographic utilities simplify integrity verification and proof generation.
- **High-Volume Data Processing**: Stream-backed collections and batch-optimized operations enable processing datasets larger than available memory.
- **Custom Persistence Layers**: Fine-grained control over serialization, storage layout, and transactional semantics.
- **Cryptographic Applications**: Post-quantum signature schemes and VRF implementations support advanced security requirements.
- **Protocol Implementations**: Structured communication frameworks for RPC, control protocols, or peer-to-peer messaging.
- **LevelDB Integration**: Native Windows LevelDB wrapper for high-performance key-value storage.

### 📊 Less-Suited Scenarios

- **Simple CRUD Applications**: If standard Entity Framework or Dapper suffice, Sphere10 Framework's low-level primitives may introduce unnecessary complexity.
- **Web APIs with Standard ORMs**: The library does not integrate with ASP.NET or Entity Framework out-of-the-box.
- **UI-Centric Applications**: Sphere10 Framework focuses on data structures and persistence, not UI frameworks or bindings.
- **Rapid Prototyping**: The library's emphasis on explicitness and control trades off against rapid development convenience.

## 🏗️ Architecture Overview

Sphere10 Framework's architecture is organized into largely independent subsystems that compose through well-defined interfaces:

1. **Collections Layer**: Extended list and collection interfaces define the foundation. Implementations range from simple in-memory structures to complex stream-mapped and paged variants.

2. **Storage Layer**: Clustered streams provide the underlying mechanism for multi-stream persistence. Object streams layer serialization, indexing, and metadata on top of clustered streams.

3. **Serialization Framework**: A registry-based system (`SerializerFactory`) maps types to serializers. Serializers compose via decorators for features like null-handling, polymorphism, and reference-tracking.

4. **Transactional Framework**: Transactional scopes coordinate commit/rollback for the transaction they own and its enlisted operations. Transactional collections, streams and object spaces expose `ITransactionalObject`; that contract alone does not enlist them in an unrelated scope.

5. **Merkle Trees**: Separate implementations provide different tradeoffs. Merkle-aware collections integrate tree maintenance into their mutation operations.

6. **Utilities and Extensions**: Comparers, operators, logging, threading, and I/O utilities provide cross-cutting functionality without coupling to core abstractions.

Data typically flows from application code through collections or object spaces, which delegate to object streams for persistence. Object streams use clustered streams for storage and serializers for encoding. Transactional backends coordinate their participating mutations, and merkle trees provide integrity proofs where enabled.

## 🚀 Getting Started

### Installation

**NuGet Packages:**

```bash
# Core library
dotnet add package Sphere10.Framework

# Platform-specific (optional)
dotnet add package Sphere10.Framework.Windows              # Windows utilities
dotnet add package Sphere10.Framework.Windows.Forms        # WinForms integration
dotnet add package Sphere10.Framework.Windows.LevelDB      # Native LevelDB wrapper
dotnet add package Sphere10.Framework.CryptoEx             # Extended cryptography (ECDSA, ECIES)
dotnet add package Sphere10.Framework.Communications       # Networking & protocols
dotnet add package Sphere10.Framework.Web.AspNetCore       # ASP.NET Core integration
```

Or reference compiled assemblies directly in your project.

### 10-Second Example

```csharp
using Sphere10.Framework;
using System.IO;
using System.Text;

// BinarySerializer: Object serialization with type registrations
var serializer = new BinarySerializer();
using var stream = new MemoryStream();
serializer.Serialize(stream, "Hello World");
stream.Position = 0;
var restored = (string)serializer.Deserialize(stream);

// StreamMappedList: List over a caller-owned backing stream
using var fileStream = new FileStream("data.bin", FileMode.Create, FileAccess.ReadWrite);
using var list = StreamMappedFactory.CreateList<string>(fileStream, 4096, StringSerializer.UTF8, autoLoad: true);
list.Add("Persisted Item 1");
list.Add("Persisted Item 2");
fileStream.Flush();

// FlatMerkleTree: Cryptographic proof of integrity
var tree = new FlatMerkleTree(CHF.SHA2_256);
tree.Leafs.AddRange(Encoding.UTF8.GetBytes("Block 1"), Encoding.UTF8.GetBytes("Block 2"));
var root = tree.Root;  // Root hash proves all items

// Synchronized collections: Thread-safe variants
var syncList = new SynchronizedExtendedList<int>();
syncList.Add(42);  // Automatically locked during mutation
```

### 💻 Core Examples

**BinarySerializer and Fixed-Size Item Serialization**

```csharp
using Sphere10.Framework;
using System.IO;

public static class BinarySerializationExamples {
	public static object RoundTripObject(object value) {
		var serializer = new BinarySerializer();
		using var stream = new MemoryStream();
		serializer.Serialize(stream, value);
		stream.Position = 0;
		return serializer.Deserialize(stream);
	}

	public static int ReadIndexedInteger(int[] values, int index) {
		Guard.ArgumentNotNull(values, nameof(values));
		Guard.Argument(index >= 0 && index < values.Length, nameof(index), "Index must identify an input value.");
		var serializer = PrimitiveSerializer<int>.Instance;
		using var stream = new MemoryStream();
		using (var writer = new EndianBinaryWriter(EndianBitConverter.Little, stream.AsNonClosing())) {
			foreach (var value in values)
				serializer.Serialize(value, writer);
		}
		stream.Position = checked(index * serializer.ConstantSize);
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream.AsNonClosing());
		return serializer.Deserialize(reader);
	}
}
```

`BinarySerializer` is non-generic: its stream overloads write and read objects, including type-registration metadata and configured reference handling. It does not perform compression or promise a fixed byte size. The indexed example instead uses the constant-size `PrimitiveSerializer<int>` and an explicit endian reader/writer; `AsNonClosing()` lets those wrappers be disposed while the example retains ownership of its memory stream.

**StreamMappedList: Disk-Backed Collections**

Construct stream-mapped lists through `StreamMappedFactory.CreateList<T>()`. The `Product` model and `CustomProductSerializer` definitions appear below:

```csharp
using System;
using System.Linq;
using Sphere10.Framework;
using System.IO;

static class StreamMappedListExample {
	public static void CreateInventory(string filePath) {
		using var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
		using (var inventory = StreamMappedFactory.CreateList<Product>(stream, 4096, new CustomProductSerializer(), autoLoad: true)) {
			inventory.Add(new Product { Id = 1, Name = "Widget", Price = 9.99m });
			inventory.AddRange(new[] {
				new Product { Id = 2, Name = "Gadget", Price = 19.99m },
				new Product { Id = 3, Name = "Doohickey", Price = 14.99m }
			});
		}
		stream.Flush();
	}

	public static Product[] ReadBatch(string filePath, long index, long count) {
		using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
		using var inventory = StreamMappedFactory.CreateList<Product>(stream, 4096, new CustomProductSerializer(), autoLoad: true);
		return inventory.ReadRange(index, count).ToArray();
	}

	public static long CreateTextLookup(string filePath) {
		using var stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
		var serializer = SerializerFactory.Default.GetSerializer<string>();
		using var list = StreamMappedFactory.CreateList<string>(
			stream,
			4096,
			serializer,
			itemComparer: StringComparer.Ordinal,
			itemChecksummer: new ItemDigestor<string>(CHF.Blake2b_128, serializer, Endianness.LittleEndian),
			reservedStreams: 2,
			optionalItemChecksumIndexName: "TextChecksum",
			autoLoad: true
		);
		list.Add("Important data");
		return list.IndexOfL("Important data");
	}
}
```

`autoLoad: true` initializes or loads the container before use. Collection mutations update the backing stream; there is no list `Save()` method. The factory owns the list's object/cluster containers, while these examples retain ownership of their `FileStream` and dispose it after the list. Reopening must use the same serializers, cluster layout, endianness and attachment configuration. `ReadRange` reads the selected items; materializing the result before disposal keeps the returned values usable. Metadata, indexes, selected items and runtime limits still require memory.

The checksum example registers a named lookup attachment and reserves streams for its forward and reverse stores. Checksums narrow lookup candidates; equality comparisons resolve collisions. This is not automatic payload-corruption verification. See [StreamMappedFactory](Collections/StreamMapped/StreamMappedFactory.cs) and [collections/storage guidance](../../docs/guidelines/patterns/collections-and-storage.md).

**StreamPagedList: Memory-Paged Disk Collections**

```csharp
using Sphere10.Framework;
using System.IO;

// StreamPagedList loads pages into memory as needed (more efficient for sequential access)
using var pagedStream = new FileStream("pages.dat", FileMode.Create, FileAccess.ReadWrite);

var pagedList = new StreamPagedList<string>(
    new StringSerializer(Encoding.UTF8),
    pagedStream,
    pageSize: 4096  // 4KB pages, tuned for your access patterns
);

// Add thousands of items
for (int i = 0; i < 100_000; i++) {
    pagedList.Add($"Item {i}");
}

// Sequential access is fast (page already in memory)
for (int i = 0; i < 10; i++) {
    Console.WriteLine(pagedList[i]);
}

// Random access loads the needed page
var item50000 = pagedList[50000];

// For constant-size items, use constant-size serializer for direct indexing
var constSizeList = new StreamPagedList<string>(
    new StringSerializer(Encoding.UTF8).AsConstantSize(50),  // Fixed 50-byte strings
    pagedStream,
    pageSize: 4096
);
// Now can directly calculate position: position = itemIndex * itemSize
// Without scanning through variable-length items
```

**FlatMerkleTree: Cryptographic Integrity Proofs**

```csharp
using Sphere10.Framework;
using System.Security.Cryptography;

// Create a flat merkle tree (all nodes in memory, optimal for proof generation)
var tree = new FlatMerkleTree(CHF.SHA2_256);

// Add data (hashed immediately)
var data = new[] {
    Encoding.UTF8.GetBytes("Transaction 1"),
    Encoding.UTF8.GetBytes("Transaction 2"),
    Encoding.UTF8.GetBytes("Transaction 3"),
    Encoding.UTF8.GetBytes("Transaction 4")
};
tree.Leafs.AddRange(data);

// Get root hash (proof that all items are included)
var rootHash = tree.Root;
Console.WriteLine($"Root: {Convert.ToHexString(rootHash)}");

// Generate proof for a specific item (prove item 2 is in tree)
var proof = tree.GenerateProof(2);  // Generates merkle path
var leaf = tree.Leafs[2];

// Verify the proof (can be done independently)
bool verified = MerkleTreeUtilities.VerifyProof(
    leaf,
    proof,
    rootHash,
    CHF.SHA2_256
);

// FlatMerkleTree is ideal for:
// - Blockchain blocks (fixed number of transactions)
// - Smaller merkle trees where full tree fits in memory
// - Frequent proof generation

// Compare with LongMerkleTree for massive datasets
var longTree = new LongMerkleTree(CHF.SHA2_256);
// LongMerkleTree only keeps sub-root hashes in memory
// Can handle millions of items with minimal memory overhead
// But proof generation requires computing intermediate hashes
```

**LongMerkleTree: Memory-Efficient Merkle Trees**

```csharp
using Sphere10.Framework;

// LongMerkleTree: For massive datasets (millions of items)
// Only stores sub-root hashes, not all nodes
var tree = new LongMerkleTree(CHF.SHA2_256);

// Append items efficiently
for (int i = 0; i < 1_000_000; i++) {
    var data = Encoding.UTF8.GetBytes($"Item {i}");
    tree.Leafs.AddRange(data);
}

// Root hash proves integrity of all million items
var root = tree.Root;

// Generate proof for an item
var proof = tree.GenerateProof(500_000);

// Verify proof (works same as FlatMerkleTree)
var leaf = tree.Leafs[500_000];
bool verified = MerkleTreeUtilities.VerifyProof(
    leaf,
    proof,
    root,
    CHF.SHA2_256
);

// LongMerkleTree advantages:
// - O(1) memory for append operations
// - Can handle unlimited items
// - Perfect for blockchain, event logs, append-only stores

// Size information
var size = tree.Size;
Console.WriteLine($"Leaf count: {size.LeafCount}");
Console.WriteLine($"Tree depth: {size.Depth}");
```

**Synchronized Collections: Thread-Safe Wrappers**

```csharp
using Sphere10.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

// SynchronizedExtendedList: Thread-safe variant of ExtendedList
var syncList = new SynchronizedExtendedList<int>();

// Safe for concurrent access from multiple threads
var tasks = new List<Task>();
for (int t = 0; t < 10; t++) {
    tasks.Add(Task.Run(() => {
        for (int i = 0; i < 1000; i++) {
            syncList.Add(i);  // Automatically locked
        }
    }));
}
Task.WaitAll(tasks.ToArray());
Console.WriteLine($"Total items: {syncList.Count}");  // 10,000 safely

// SynchronizedDictionary: Thread-safe key-value pairs
var syncDict = new SynchronizedDictionary<string, Account>();

var producer = Task.Run(() => {
    for (int i = 0; i < 100; i++) {
        syncDict[$"account_{i}"] = new Account { Id = i, Balance = 100m };
    }
});

var consumer = Task.Run(() => {
    System.Threading.Thread.Sleep(50);  // Let producer add some
    foreach (var key in syncDict.Keys) {
        var account = syncDict[key];
        Console.WriteLine($"{key}: {account.Balance}");
    }
});

Task.WaitAll(producer, consumer);

// SynchronizedRepository: Cached, thread-safe data access
var syncRepo = new SynchronizedRepository<int, Product>(
    loadFunc: id => FetchProductFromDatabase(id)
);

// Thread-safe get (with automatic caching)
var product1 = syncRepo.Get(1);
var product2 = syncRepo.Get(2);

// Multiple threads can safely access the cache
var readTasks = Enumerable.Range(0, 100)
    .Select(i => Task.Run(() => syncRepo.Get(i % 10)))
    .ToArray();
Task.WaitAll(readTasks);

// Synchronized collection types available:
// - SynchronizedExtendedList<T>
// - SynchronizedDictionary<TKey, TValue>
// - SynchronizedSet<T>
// - SynchronizedQueue<T>
// - SynchronizedHeap<T>
// All use internal locking for thread safety

class Account {
    public int Id { get; set; }
    public decimal Balance { get; set; }
}

class Product {
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

Product FetchProductFromDatabase(int id) {
    return new Product { Id = id, Name = $"Product {id}", Price = 9.99m };
}

class CustomProductSerializer : ItemSerializerBase<Product> {
	private readonly IItemSerializer<int> _idSerializer;
	private readonly IItemSerializer<string> _nameSerializer;
	private readonly IItemSerializer<decimal> _priceSerializer;

	public CustomProductSerializer()
		: this(SerializerFactory.Default) {
	}

	public CustomProductSerializer(SerializerFactory factory) {
		Guard.ArgumentNotNull(factory, nameof(factory));
		_idSerializer = factory.GetSerializer<int>();
		_nameSerializer = factory.GetSerializer<string>();
		_priceSerializer = factory.GetSerializer<decimal>();
	}

	public override long CalculateSize(SerializationContext context, Product item)
		=> _idSerializer.CalculateSize(context, item.Id)
			+ _nameSerializer.CalculateSize(context, item.Name)
			+ _priceSerializer.CalculateSize(context, item.Price);

	public override void Serialize(Product item, EndianBinaryWriter writer, SerializationContext context) {
		_idSerializer.Serialize(item.Id, writer, context);
		_nameSerializer.Serialize(item.Name, writer, context);
		_priceSerializer.Serialize(item.Price, writer, context);
	}

	public override Product Deserialize(EndianBinaryReader reader, SerializationContext context)
		=> new Product {
			Id = _idSerializer.Deserialize(reader, context),
			Name = _nameSerializer.Deserialize(reader, context),
			Price = _priceSerializer.Deserialize(reader, context)
		};
}
```

## 💾 Persistence & Transactions

**Advanced StreamMappedList Usage:**

The StreamMappedList shown above demonstrates the core disk-backed storage pattern. For additional persistence options:

- Use `itemChecksummer` together with `optionalItemChecksumIndexName` for an item lookup index; the checksum alone does not enable an index or verify payload integrity.
- Allocate `reservedStreams` for the configured index/metadata attachments and preserve that layout when reopening.
- Configure `ClusteredStreamsPolicy` to control cluster allocation

**Transactional Scopes:**

`TransactionalDictionary<TKey, TValue>` wraps stream-mapped storage and an `ITransactionalObject`. Construct its file backend with a `Sphere10FrameworkFileDescriptor`, load it, and explicitly call `Commit()` or `Rollback()`. It has no parameterless constructor or `BeginScope()` method:

```csharp
using System;
using Sphere10.Framework;

public static class TransactionalDictionaryExample {
	public static void UpdateBalances(string dataFile, string pagesDirectory) {
		using var dictionary = Open(dataFile, pagesDirectory, FileAccessMode.OpenOrCreate);
		dictionary.Load();
		try {
			dictionary["acc001"] = 1000m;
			dictionary["acc002"] = 500m;
			dictionary.Commit();
		} catch (Exception operationError) {
			try {
				dictionary.Rollback();
			} catch (Exception rollbackError) {
				throw new AggregateException(operationError, rollbackError);
			}
			throw;
		}
	}

	public static decimal ReadBalance(string dataFile, string pagesDirectory, string accountID) {
		using var dictionary = Open(dataFile, pagesDirectory, FileAccessMode.Read);
		dictionary.Load();
		return dictionary[accountID];
	}

	private static TransactionalDictionary<string, decimal> Open(string dataFile, string pagesDirectory, FileAccessMode accessMode)
		=> new(
			Sphere10FrameworkFileDescriptor.From(dataFile, pagesDirectory),
			SerializerFactory.Default.GetSerializer<string>(),
			PrimitiveSerializer<decimal>.Instance,
			keyChecksum: new ItemDigestor<string>(CHF.Blake2b_128, SerializerFactory.Default.GetSerializer<string>(), Endianness.LittleEndian),
			reservedStreamCount: 3,
			accessMode: accessMode
		);
}
```

The caller supplies existing data/page directories; `UpdateBalances` commits before its dictionary is disposed, and `ReadBalance` opens the same backing file to read committed state. The variable-size string keys need the configured key checksummer for automatic dictionary layout selection; both opens use the same configuration. For transaction scopes that coordinate enlisted files, reuse `FileTransactionScope` and call `BeginTransaction()` on the scope itself; see [custom transactional scopes](#custom-transactional-scopes). Scope disposal closes the owned transaction and does not imply automatic rollback by the common base. These are distinct transaction owners: a dictionary is not automatically enlisted in an unrelated scope.

### 📦 Advanced Serialization Patterns

The serialization system supports reference-tracked graphs, polymorphic types, and custom decorators. See the **Core Examples** section above for BinarySerializer patterns. Refer to `IItemSerializer<T>` interface and `SerializerFactory` for custom implementations.

**Merkle List with Integrity Proofs:**

```csharp
using Sphere10.Framework;
using System.Security.Cryptography;

// Create a merkle-aware list with SHA-256
var hasher = new HashAlgorithmAdapter(SHA256.Create());
var merkleList = new FlatMerkleList<string>(
    ItemSerializer<string>.Default,
    hasher
);

merkleList.Add("Block 1");
merkleList.Add("Block 2");
merkleList.Add("Block 3");

// Get root hash (commitment to entire list)
byte[] rootHash = merkleList.MerkleTree.Root;

// Generate proof that \"Block 2\" is at index 1
var proof = merkleList.MerkleTree.GenerateProof(1);

// Verify proof independently
var isValid = merkleList.MerkleTree.VerifyProof(
    merkleList.GetItemHash(1),
    1,
    proof,
    rootHash
);
Console.WriteLine($"Proof valid: {isValid}");  // true
```

**Merkle Dictionary (Multiple Keys):**

```csharp
var merkleDictionary = new MerkleListAdapter<KeyValuePair<string, int>>(
    new ExtendedList<KeyValuePair<string, int>>(),
    hasher
);

merkleDictionary.Add(new KeyValuePair<string, int>("Alice", 100));
merkleDictionary.Add(new KeyValuePair<string, int>("Bob", 50));

// Prove integrity of multi-item state
var multiProof = merkleDictionary.MerkleTree.GenerateMultiProof(new[] { 0, 1 });
```
### 📦 Serialization Examples

**Built-in Serializers (Default):**

```csharp
using Sphere10.Framework;

public static class BuiltInSerializationExamples {
	public static int RoundTripInt(int value) {
		var serializer = SerializerFactory.Default.GetSerializer<int>();
		var bytes = serializer.SerializeToBytes(value, Endianness.LittleEndian);
		return serializer.DeserializeBytes(bytes, Endianness.LittleEndian);
	}

	public static ExtendedList<string> RoundTripList(ExtendedList<string> values) {
		var serializer = SerializerFactory.Default.GetSerializer<ExtendedList<string>>();
		var bytes = serializer.SerializeToBytes(values, Endianness.LittleEndian);
		return serializer.DeserializeBytes(bytes, Endianness.LittleEndian);
	}
}
```

Default registrations include the primitive and `ExtendedList<T>` serializers used here. For application types that need automatic assembly, use `SerializerBuilder.FactoryAssemble<T>(factory)` or retain their registrations with `factory.RegisterAutoBuild<T>()`.

**Custom Serializer Factory with Type Registration:**

This deliberately starts with an empty factory to choose the string encoding and size descriptor. Register each required member serializer before registering the application's pure composite serializer:

```csharp
using System.Text;
using Sphere10.Framework;

public sealed class FactoryMessage {
	public string Text { get; set; }
}

public static class CustomFactoryExample {
	public static FactoryMessage RoundTrip(FactoryMessage message) {
		var factory = new SerializerFactory();
		factory.Register<string>(new StringSerializer(Encoding.UTF8, SizeDescriptorStrategy.UseVarInt));
		var pureSerializer = SerializerBuilder
			.For<FactoryMessage>()
			.Serialize(item => item.Text, factory.GetSerializer<string>())
			.Build();
		factory.Register<FactoryMessage>(pureSerializer);
		var serializer = factory.GetSerializer<FactoryMessage>();
		var bytes = serializer.SerializeToBytes(message, Endianness.LittleEndian);
		return serializer.DeserializeBytes(bytes, Endianness.LittleEndian);
	}
}
```

Usually, use `new SerializerFactory(SerializerFactory.Default)` to copy built-in registrations before adding new application types. Registrations cannot replace an existing type's mapping; do not re-register `string` in that copied factory. Register pure serializers, then obtain configured null/reference handling through `GetSerializer<T>()`. Applications exchanging these bytes must agree on registrations, layout and endianness.

**Polymorphic Serialization (Inheritance Support):**

```csharp
using Sphere10.Framework;

[KnownSubType(typeof(Dog))]
[KnownSubType(typeof(Cat))]
public abstract class Animal {
	public string Name { get; set; }
}

public sealed class Dog : Animal {
	public bool IsWorkingDog { get; set; }
}

public sealed class Cat : Animal {
	public int LivesRemaining { get; set; }
}

public static class PolymorphicSerializationExample {
	public static Animal RoundTrip(Animal animal) {
		var factory = new SerializerFactory(SerializerFactory.Default);
		var serializer = SerializerBuilder.FactoryAssemble<Animal>(factory);
		var bytes = serializer.SerializeToBytes(animal, Endianness.LittleEndian);
		return serializer.DeserializeBytes(bytes, Endianness.LittleEndian);
	}
}
```

The builder scans the `[KnownSubType]` declarations, assembles concrete serializers and uses a polymorphic serializer for the abstract `Animal` contract. Passing a `Dog` or `Cat` preserves its concrete type and public writable properties. Serialize collections with a collection serializer rather than passing a list to an `IItemSerializer<Animal>`.

**Reference-Tracked Serialization (Graph Preservation):**

Automatic assembly supplies composite instance activation and reference handling together, so members can refer back to an object that is still being populated:

```csharp
using System;
using Sphere10.Framework;

public sealed class Node {
	public string Value { get; set; }
	public Node Next { get; set; }
}

public static class GraphSerializationExample {
	public static Node RoundTripCycle() {
		var factory = new SerializerFactory(SerializerFactory.Default);
		factory.RegisterAutoBuild<Node>();
		var serializer = factory.GetSerializer<Node>();
		var first = new Node { Value = "A" };
		var second = new Node { Value = "B", Next = first };
		first.Next = second;
		var bytes = serializer.SerializeToBytes(first, Endianness.LittleEndian);
		var restored = serializer.DeserializeBytes(bytes, Endianness.LittleEndian);
		if (!ReferenceEquals(restored.Next.Next, restored))
			throw new InvalidOperationException("The cycle was not preserved.");
		return restored;
	}
}
```

`AsReferenceSerializer()` is also a public extension for wrapping a pure serializer, but `SerializerFactory` rejects registration of a `ReferenceSerializer<T>` wrapper. Register the pure serializer and let factory lookup apply the wrapper. A custom cyclic graph serializer must implement the early activation contract; the reference wrapper alone cannot supply it.

**Compact Integer Encoding (VarInt/CVarInt):**

`VarInt` and `CVarInt` encode unsigned 64-bit values. `VarInt` uses a size prefix for larger values; `300` occupies three bytes. `CVarInt` uses its compact variable-length encoding, with `10000` occupying two bytes. The custom `int` serializer below accepts non-negative values and rejects encoded values beyond `int.MaxValue` when reading:

```csharp
using System.IO;
using Sphere10.Framework;

public sealed class CompactSerializer : ItemSerializerBase<int> {
	public override long CalculateSize(SerializationContext context, int item)
		=> CVarInt.SizeOf(ToUnsigned(item));

	public override void Serialize(int item, EndianBinaryWriter writer, SerializationContext context)
		=> CVarInt.Write(ToUnsigned(item), writer.BaseStream);

	public override int Deserialize(EndianBinaryReader reader, SerializationContext context)
		=> checked((int)CVarInt.Read(reader.BaseStream));

	private static ulong ToUnsigned(int item) {
		Guard.Argument(item >= 0, nameof(item), "CompactSerializer requires a non-negative integer.");
		return (ulong)item;
	}
}

public static class CompactIntegerExamples {
	public static ulong RoundTripVarInt(ulong value) {
		using var stream = new MemoryStream();
		VarInt.Write(value, stream);
		stream.Position = 0;
		return VarInt.Read(stream);
	}

	public static ulong RoundTripCVarInt(ulong value) {
		var bytes = new CVarInt(value).ToBytes();
		return CVarInt.Read(bytes);
	}

	public static int RoundTripNonNegativeInt(int value) {
		var serializer = new CompactSerializer();
		var bytes = serializer.SerializeToBytes(value, Endianness.LittleEndian);
		return serializer.DeserializeBytes(bytes, Endianness.LittleEndian);
	}
}
```

These compact encodings define their own byte layout; using them does not turn signed values into an unsigned encoding automatically. `CVarIntSerializer.Instance` and `VarIntSerializer.Instance` already serialize the corresponding framework value types.

**Cryptographic Hashing:**

```csharp
using Sphere10.Framework;

var data = "Hello, World!";

// Standard hash functions
byte[] sha256 = Tools.Hashing.SHA256(data);
byte[] sha512 = Tools.Hashing.SHA512(data);
byte[] blake2b = Tools.Hashing.BLAKE2b(data);
byte[] murmurhash = Tools.Hashing.MurmurHash3(data);

// Hash files
byte[] fileHash = Tools.Hashing.SHA256File("path/to/file.bin");

// Compute multiple simultaneously
var hashes = Tools.Hashing.ComputeMultipleHashes(data, CHF.SHA2_256, CHF.SHA3_256);

// Hash files
byte[] fileHash = Tools.Hashing.SHA256File("path/to/file.bin");

// Compute multiple simultaneously
var hashes = Tools.Hashing.ComputeMultipleHashes(data, CHF.SHA2_256, CHF.SHA3_256);
```

### 📝 String Extensions (50+ helpers)

```csharp
using Sphere10.Framework;

var text = "Hello World";

// Formatting & validation
var padded = text.PadToLength(20);                      // Pad or truncate to exact length
var truncated = text.Truncate(5);                       // Truncate with ellipsis
bool isEmpty = text.IsNullOrEmpty();
bool isWhitespace = "   ".IsNullOrWhiteSpace();

// Type checking
bool isNumeric = "12345".IsNumeric();
bool isAlpha = "abc".IsAlpha();
bool isAlphaNumeric = "abc123".IsAlphaNumeric();
bool isHex = "DEADBEEF".IsHex();

// Case conversion
var camelCase = "hello_world".ToCamelCase();           // helloWorld
var pascalCase = "hello_world".ToPascalCase();         // HelloWorld
var snakeCase = "HelloWorld".ToSnakeCase();            // hello_world

// Parsing & extraction
var (success, number) = "42".TryParseInt();
var guid = "550e8400-e29b-41d4-a716-446655440000".TryParseGuid();
var words = "The quick brown fox".SplitOnWhitespace();

// Splitting & joining
var lines = "line1\nline2\nline3".ToLines();
var csv = new[] { "a", "b", "c" }.JoinWith(", ");
```

**LevelDB Integration (High-Performance Key-Value Store):**

```csharp
using Sphere10.Framework.Windows.LevelDB;

// Open database
using var db = new DB("./mydata");

// Basic operations
var key = Encoding.UTF8.GetBytes("user:42");
var value = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(user));
db.Put(key, value);

var retrieved = db.Get(key);
if (retrieved != null) {
    var restored = JsonSerializer.Deserialize<User>(Encoding.UTF8.GetString(retrieved));
}

// Batch operations (atomic)
using (var batch = db.CreateBatch()) {
    for (int i = 0; i < 1000; i++) {
        batch.Put(Encoding.UTF8.GetBytes($"key:{i}"), Encoding.UTF8.GetBytes($"value:{i}"));
    }
    db.Write(batch);
}

// Iteration & range queries
using (var iterator = db.CreateIterator()) {
    iterator.SeekToFirst();
    while (iterator.IsValid()) {
        var k = Encoding.UTF8.GetString(iterator.Key());
        var v = Encoding.UTF8.GetString(iterator.Value());
        Console.WriteLine($"{k} = {v}");
        iterator.Next();
    }
}
```

## 🔧 Extensibility & Customization

The library's design encourages extending core abstractions rather than modifying built-in types. Here are the main extension points:

### Custom Serializers

Derive from `ItemSerializerBase<T>` to implement the `IItemSerializer<T>` contract. Implement sizing alongside serialization, and pass the supplied context and reader/writer to member serializers:

```csharp
using Sphere10.Framework;

public class User {
	public string Name { get; set; }
	public int Age { get; set; }
}

public class UserSerializer : ItemSerializerBase<User> {
	private readonly IItemSerializer<string> _stringSerializer;
	private readonly IItemSerializer<int> _intSerializer;

	public UserSerializer(SerializerFactory factory) {
		Guard.ArgumentNotNull(factory, nameof(factory));
		_stringSerializer = factory.GetSerializer<string>();
		_intSerializer = factory.GetSerializer<int>();
	}

	public override long CalculateSize(SerializationContext context, User item)
		=> _stringSerializer.CalculateSize(context, item.Name) + _intSerializer.CalculateSize(context, item.Age);

	public override void Serialize(User item, EndianBinaryWriter writer, SerializationContext context) {
		_stringSerializer.Serialize(item.Name, writer, context);
		_intSerializer.Serialize(item.Age, writer, context);
	}

	public override User Deserialize(EndianBinaryReader reader, SerializationContext context)
		=> new User {
			Name = _stringSerializer.Deserialize(reader, context),
			Age = _intSerializer.Deserialize(reader, context)
		};
}

public static class UserSerializationExample {
	public static User RoundTrip(User user) {
		var factory = new SerializerFactory(SerializerFactory.Default);
		factory.Register<User>(new UserSerializer(factory));
		var serializer = factory.GetSerializer<User>();
		var bytes = serializer.SerializeToBytes(user, Endianness.LittleEndian);
		return serializer.DeserializeBytes(bytes, Endianness.LittleEndian);
	}
}
```

The copied default factory supplies the string and integer serializers while keeping the custom registration local. This pure serializer expects a non-null `User`; factory lookup supplies the configured null/reference wrapper. Endianness belongs to the reader/writer, and the byte-array helpers accept it explicitly. See [serialization guidance](../../docs/guidelines/patterns/serialization.md) for factory registration, reference handling and stable wire formats.

### Serializer Decorators

Use the public wrappers for nullable values and reference handling. `ItemSerializerDecorator<T>` is the reusable base for custom serializer decorators:

```csharp
using Sphere10.Framework;

public static class SerializerDecoratorExamples {
	public static int? RoundTripNullable(int? value) {
		IItemSerializer<int?> serializer = new NullableSerializer<int>(PrimitiveSerializer<int>.Instance);
		var bytes = serializer.SerializeToBytes(value, Endianness.LittleEndian);
		return serializer.DeserializeBytes(bytes, Endianness.LittleEndian);
	}

	public static string RoundTripString(string value) {
		IItemSerializer<string> serializer = new ReferenceSerializer<string>(
			StringSerializer.UTF8,
			ReferenceSerializerMode.SupportNull | ReferenceSerializerMode.SupportContextReferences
		);
		var bytes = serializer.SerializeToBytes(value, Endianness.LittleEndian);
		return serializer.DeserializeBytes(bytes, Endianness.LittleEndian);
	}
}
```

`SerializerFactory.GetSerializer<T>()` applies its configured reference/null handling; do not wrap an already reference-wrapped serializer in another `ReferenceSerializer<T>`. A graph serializer must support instance activation before reading members if cyclic references need to point back to the instance being constructed; wrapping a serializer alone does not add that activation contract.

Compression and encryption can be composed at the stream layer: supply an `EndianBinaryWriter` over the chosen transforming stream to `Serialize`, and use the matching inverse stream composition with an `EndianBinaryReader` for `Deserialize`. There are no public `EncryptedSerializer<T>` or `CompressedSerializer<T>` wrappers in this library. For `EncryptedStream`, follow its [key, counter, authentication and ownership requirements](#length-preserving-encrypted-streams).

### Custom Indexes on Object Streams

Implement `IProjectionIndex<TItem, TKey>` to add custom indexing strategies:

```csharp
public class LastNameIndex : ProjectionIndexBase<Person, string> {
    public LastNameIndex(ObjectStream<Person> objectStream) 
        : base(objectStream) { }
    
    public override string ProjectKey(Person item) => item.LastName;
    
    protected override void OnIndexAdded(Person item, long index) {
        // Store index mapping
    }
    
    public override long? TryGetIndex(string lastName) {
        // Lookup by last name
        return _indexStore.TryGetValue(lastName, out var idx) ? idx : null;
    }
}

// Attach to ObjectStream
var objectStream = new ObjectStream<Person>(clusteredStreams, serializer);
var index = new LastNameIndex(objectStream);
objectStream.RegisterIndex(index);

// Query via index
var personIndex = index.TryGetIndex("Smith");
var person = objectStream[personIndex.Value];
```

### Custom Transactional Scopes

`TransactionalScopeBase<TTransaction>` coordinates ownership and lifecycle. For a synchronous backend, derive from `SyncTransactionalScope<TTransaction>` and implement its four synchronous transaction hooks. An asynchronous backend uses `AsyncTransactionalScope<TTransaction>` and the corresponding async hooks; concrete types do not implement one public direction by calling the other.

This complete example wraps a provider's `DbTransaction`. The caller supplies an already-open `DbConnection` and a context name; this scope requires that it be the root for that name:

```csharp
using System;
using System.Data.Common;
using Sphere10.Framework;

public sealed class DatabaseTransactionScope : SyncTransactionalScope<DbTransaction> {
	private readonly DbConnection _connection;

	public DatabaseTransactionScope(DbConnection connection, string contextID)
		: base(ContextScopePolicy.MustBeRoot, contextID) {
		Guard.ArgumentNotNull(connection, nameof(connection));
		_connection = connection;
	}

	protected override DbTransaction BeginTransactionInternal() => _connection.BeginTransaction();

	protected override void CloseTransactionInternal(DbTransaction transaction) => transaction.Dispose();

	protected override void CommitInternal(DbTransaction transaction) => transaction.Commit();

	protected override void RollbackInternal(DbTransaction transaction) => transaction.Rollback();
}

public static class DatabaseTransactionExample {
	public static void Execute(DbConnection connection, string contextID, Action<DbTransaction> operation) {
		Guard.ArgumentNotNull(operation, nameof(operation));
		using var scope = new DatabaseTransactionScope(connection, contextID);
		scope.BeginTransaction();
		try {
			operation(scope.Transaction);
			scope.Commit();
		} catch (Exception operationError) {
			if (scope.ParticipatesWithinTransaction) {
				try {
					scope.Rollback();
				} catch (Exception rollbackError) {
					throw new AggregateException(operationError, rollbackError);
				}
			}
			throw;
		}
	}
}
```

The operation must attach its database commands to the supplied transaction. The scope disposes that transaction; the caller retains ownership of the connection. Reuse existing backend scopes such as `FileTransactionScope` before introducing a custom scope. See [directional sync/async hierarchies](../../docs/guidelines/patterns/sync-async.md#synchronous-and-asynchronous-implementation-hierarchies) and [transaction ownership](../../docs/guidelines/patterns/resource-scopes.md).

### Extending Collections

Decorate existing collections to add custom behavior:

```csharp
// Add logging to list operations
public class LoggingList<T> : ExtendedListDecorator<T> {
    private readonly ILogger _logger;
    
    public LoggingList(IExtendedList<T> inner, ILogger logger) : base(inner) {
        _logger = logger;
    }
    
    public override void Add(T item) {
        _logger.Info($"Adding {item}");
        base.Add(item);
    }
    
    public override void InsertRange(long index, T[] items) {
        _logger.Info($"Inserting {items.Length} items at {index}");
        base.InsertRange(index, items);
    }
}

// Use transparently
var baseList = new ExtendedList<int>();
IExtendedList<int> logged = new LoggingList<int>(baseList, logger);
logged.Add(42);  // "Adding 42" logged
```

## ⚠️ Threading / Performance / Safety Notes

### Thread Safety

- **Most collections are NOT thread-safe by default.** Use `SynchronizedList<T>`, `SynchronizedDictionary<TKey, TValue>`, `SynchronizedExtendedList<T>`, or wrap with `ConcurrentStream` where concurrent access is required.
- **Transactional scopes require serialized use.** Nested scopes can share transaction ownership through the logical call context; creating another scope instance does not by itself guarantee an independent transaction. Follow the concrete backend's isolation and concurrency contract.
- **Caches**: Thread-safety depends on implementation. `SynchronizedRepository<T>` and `SynchronizedLogger` provide synchronized wrappers.
- **ProducerConsumerQueue<T>**: Fully thread-safe for concurrent producers and consumers.

### Performance Considerations

- **Range Operations**: Prefer `ReadRange`, `UpdateRange`, and `InsertRange` over element-by-element operations for large datasets.
- **Paging Configuration**: Tune page sizes and in-memory page counts for memory-paged and file-paged collections based on access patterns.
- **Serialization**: Constant-size serializers enable efficient random access. Variable-size serializers require sequential scans.
- **Merkle Trees**: Flat merkle trees optimize for proof generation but consume O(n log n) memory. Long merkle trees trade memory for computation.
- **ClusteredStreams**: Default cluster size is 4KB; adjust based on typical object sizes and access patterns.

### Safety Constraints

- **Stream Position Management**: Many components assume exclusive control over stream position. Concurrent stream access without synchronization is unsafe.
- **Serialization Context**: Contexts are NOT thread-safe. Use separate contexts per thread or synchronize access.
- **Disposal**: Disposable resources (streams, transactions, scopes) must be disposed to release locks and persist changes. Use `using` statements.

## ✅ Status & Maturity

Sphere10 Framework is a mature library that has evolved over multiple years. Core subsystems (collections, serialization, transactions, merkle trees) are stable and production-tested. Some components (post-quantum cryptography, protocol orchestration) may be less battle-tested and should be evaluated carefully for production use.

### Compatibility

- **Target Framework**: .NET 10.0
- **Backward Compatibility**: The library does not guarantee API stability across major versions. Serialization formats may evolve, requiring migration strategies for persistent data.
- **Platform Support**: Windows, Linux, macOS, iOS (via Xamarin/MAUI), Android (via Xamarin/MAUI)

### 🧪 Experimental Components

- Post-quantum signature schemes (W-OTS, AMS) are reference implementations. Audit and validate before use in production cryptographic systems.
- Some advanced merkle-tree variants and indexing strategies are optimized for specific use cases and may have edge-case limitations.

### 📦 Platform-Specific Packages

- `Sphere10.Framework.Windows` - Windows-specific utilities
- `Sphere10.Framework.Windows.Forms` - WinForms integration
- `Sphere10.Framework.Windows.LevelDB` - Native LevelDB wrapper
- `Sphere10.Framework.Application` - Cross-platform application framework
- `Sphere10.Framework.Communications` - Networking and protocol layers
- `Sphere10.Framework.Web.AspNetCore` - ASP.NET Core integration
- `Sphere10.Framework.CryptoEx` - Extended cryptography (ECDSA, ECIES, etc.)

##  Dependencies

- **.NET 10.0** or higher
- **No external dependencies** for core functionality
- **Optional**: BouncyCastle for advanced cryptography
- **Optional**: Newtonsoft.Json for JSON support
- **Optional**: Microsoft.Extensions.DependencyInjection for DI integration

## ⚖️ License

Distributed under the **MIT License**.

Sphere10 Framework is distributed under the [MIT License](../../LICENSE).

More information: [MIT License](https://opensource.org/license/mit)

## 📖 Resources

- **GitHub**: [Herman Schoenfeld](https://github.com/HermanSchoenfeld/Sphere10.Framework)
- **Website**: [sphere10.com](https://sphere10.com/tech/framework)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

## Device hook cleanup

`BaseDeviceHook.Dispose()` retains its public virtual signature and now delegates to `Dispose(true)` before suppressing finalization. The finalizer calls the protected virtual `Dispose(false)` cleanup hook directly, so public disposal overrides and their managed event callbacks are not invoked by finalization. Derived hooks that require cleanup during finalization should override `Dispose(bool disposing)`, restrict managed-resource work to `disposing == true`, and call the base implementation. Base hook cleanup is idempotent.

## Default encryption and network compatibility

Default text/file compression and the non-generic `Tools.Streams.Encrypt` / `Decrypt` overloads use `Aes.Create()`. The generic algorithm overloads remain available. AES key/block sizes, CBC/PKCS7 defaults, existing password derivation and the IV-prefixed encrypted format are retained; historical compressed data remains readable. `SystemCRNG` uses the platform cryptographic random-number factory.

`Tools.Mail.SendEmail` and `SendEmailAsync` enable SMTP TLS when requested and use normal certificate validation. They no longer install a process-wide callback that accepts every certificate. With the default application policy, SMTP servers must present a certificate trusted by the host and valid for the server name. The helpers preserve any certificate policy installed by the application.

`UrlShortner` uses `HttpClient` for its existing sync/async HTTP operations. Provider URLs and response parsing are retained; this transport update does not guarantee that a legacy external provider is still available.

### SMTP and URL provider verification

The SMTP sync and async helpers were exercised against a local SMTP server, including real STARTTLS negotiation, synthetic message delivery under an explicitly pinned test certificate, rejection of an untrusted certificate under the default policy, and rejection when a server does not offer STARTTLS. The test certificate is trusted only inside the isolated test process; no operating-system trust store is changed. This does not verify delivery through a particular external mail provider or its authentication requirements.

`requiresSSL: true` uses STARTTLS, not implicit TLS. Supply the provider's STARTTLS port explicitly (typically `port: Tools.Mail.SMTPSubmissionPort`, 587). The existing omitted-port default remains 465, which is normally used for implicit TLS and is incompatible with `System.Net.Mail.SmtpClient`. See [Microsoft's supported SMTP TLS modes](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient.enablessl?view=net-10.0).

`UrlShortner.Google` and `GoogleAsync` target an API that [Google discontinued on March 30, 2019](https://developers.googleblog.com/transitioning-google-url-shortener-to-firebase-dynamic-links/). Updating the HTTP client cannot restore that service. `TinyUrl` and `TinyUrlAsync` target the third-party `tiny-url.info` service, not `tinyurl.com`; live shortening remains unverified without a supported service and credentials.
## Length-preserving encrypted streams

`EncryptedStream` (and `EncryptedStream<TStream>`) uses AES-CTR. It takes the backing stream, key, initial counter and optional `leaveOpen` flag. Writes encrypt immediately and reads decrypt; 10 plaintext bytes produce exactly 10 ciphertext bytes. The mode is fixed to preserve arbitrary byte lengths and independent random access, including overwrites that touch only the requested bytes. There is no separate cipher interface, factory or inheritance hierarchy.

```csharp
var key = Tools.Crypto.GenerateCryptographicallyRandomBytes(32);
var initialCounter = Tools.Crypto.GenerateCryptographicallyRandomBytes(16);
using var storage = new MemoryStream();
using var encryptedStream = new EncryptedStream(storage, key, initialCounter, leaveOpen: true);
encryptedStream.Write(new byte[10]);
// storage.Length == 10
encryptedStream.Position = 3;
var plaintext = new byte[7];
encryptedStream.ReadExactly(plaintext);
```

Use a 32-byte key for AES-256; AES-128 and AES-192 keys (16 and 24 bytes) are also accepted. The initial counter is always 16 bytes. Both arrays are copied. The stream owns its internal AES instance; `leaveOpen` controls only the backing stream. Counter blocks increment as big-endian integers, following NIST SP 800-38A section 6.5; AES vectors from appendix F.5 are covered by the tests.

[.NET's `CipherMode` enum](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.ciphermode) has no CTR member. The small counter-mode loop therefore lives directly inside `EncryptedStream` and uses .NET AES to encrypt counter blocks without padding. It does not expose ECB as though it were the stream mode.

Keep the key secret and store the initial counter separately. Never reuse the same key/counter blocks for different plaintext, including revisions of overwritten data. CTR supplies confidentiality; authenticate ciphertext separately before decrypting untrusted input. `Encrypted<T>` below supplies that authentication for stored objects.

The stream supports array/span, async array/memory, byte-at-a-time, copy and legacy Begin/End operations. Seekable streams use absolute positions from the underlying stream's origin; reads and writes can start within a cipher block. Non-seekable streams process bytes sequentially from their position at wrapping. Resize and writes that create gaps are rejected, since underlying zero-filled gaps are not encrypted zeroes. Callers must serialize access to an instance and discard it after an underlying I/O failure. Closing/disposal adds no bytes.

### Encrypted objects

`Encrypted.For(item, secret)` and `new Encrypted<T>(item, secret)` use the framework's `BinarySerializer` directly through `EncryptedStream`, including its type information, reference handling and null support. Callers no longer supply an item serializer.

```csharp
var secret = Encoding.UTF8.GetBytes("application secret");
var encrypted = Encrypted.For("example", secret);
if (encrypted.TryDecrypt(secret, out var value)) {
    // value == "example"
}
```

`EncryptedBytes.Length` equals the **BinarySerializer output length**, including serialization metadata. Salt, initial counter and a SHA-256 HMAC tag are held separately on the object, keeping ciphertext length unchanged. The object retains these values for `TryDecrypt`; `EncryptedBytes` alone is not a self-contained persistence format. The metadata accessors return defensive copies. Each `SetItem` generates fresh salt and counter values. PBKDF2-HMAC-SHA256 derives independent AES-256 and authentication keys; the configurable iteration count defaults to 600,000. HMAC covers the version domain, iteration count, salt, counter and ciphertext and is checked in constant time before deserialization. Wrong secrets or modified ciphertext return `false`; string secrets use UTF-8.

The new binary-secret `PBKDF2.DeriveKey` overload requires an explicit hash algorithm. The existing string overload keeps its legacy SHA-1 behavior for compatibility.
