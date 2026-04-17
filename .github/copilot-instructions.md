# GitHub Copilot Instructions

These are project-level instructions for GitHub Copilot when working in the Sphere10 Framework codebase.

## Workflow
- **Never commit** unless explicitly asked to. See [Committing](#committing) for commit rules.
- When writing code, understand the ambient code pattern at the method, class, and library level, then re-evaluate your intentions to ensure you achieve your goals and blend perfectly into the existing codebase.

## Project Overview

Sphere10 Framework is a .NET framework targeting **.NET 8** and **.NET Standard 2.1**. It provides foundational libraries for application development including data access, cryptography, communications, Windows utilities, and WinForms UI components.

## Code Style

### Braces & Formatting
- **Opening braces** go at the **end of the line** (K&R / Egyptian style), not on a new line.
- Use **tabs** for code indentation (tab width 4). Use **spaces** for aligning comments.
- Prefer `var` over explicit type declarations wherever possible.
- Omit braces for **single-line scopes** (single-statement `if`, `foreach`, etc.).
- Remove **redundant `else`** blocks after `return` or `throw`.
- Do **not** wrap lines unless they exceed ~170 characters or wrapping significantly improves readability.
- When wrapping after `(`, place the closing `)` on its own line at the indentation of the originating line.

### Naming Conventions
- **PascalCase** for all types, methods, properties, and non-private fields.
- **`_camelCase`** (underscore prefix) for private fields.
- **PascalCase** for local variables and parameters (this codebase uses Pascal-cased locals).
- Use **self-describing names**; avoid short, cryptic, or abbreviated names.

### Namespaces
- Use **file-scoped namespace declarations** (`namespace X;`).
- Namespaces are **not** strictly tied to folder structure. They follow a logical `CompanyName.ProductName.Tier.Domain` pattern and should not be overly granular.

### Member Ordering
Inside a class, order members as:
1. Events
2. Private fields
3. Protected fields
4. Constructors (simple first, complex last), then finalizers
5. Public properties
6. Internal/protected/private properties
7. Public methods
8. Internal/protected/private methods
9. Inner types

### Comments & Vertical Spacing
- Use code comments to describe **logical segments** within method bodies.
- Use a **single blank line** to separate logical segments; no blank lines within a segment.
- One blank line between members in a class.

### Constructor Chaining
- Base/sibling constructor calls (`: base(...)`, `: this(...)`) go on the **next line**, indented with a tab.

## Build Settings
- `LangVersion` is set to `latest`.
- `Nullable` is enabled globally.
- `ImplicitUsings` is **disabled** — always add explicit `using` directives.

## Architecture Patterns

### Tools Namespace (Maximal Use)
Static utility/tool classes live in the **`Tools`** global namespace so consumers can discover them via `Tools.` intellisense. Always prefer `Tools.*` over raw BCL calls when a tool exists. Key tools include:
- **`Tools.Array`** — array manipulation, sub-arrays, resizing.
- **`Tools.Collection`** — filtering, mapping, flattening, `ResizeArray`.
- **`Tools.Crypto`** — hashing (SHA-256, BLAKE2B), signing, key derivation.
- **`Tools.Text`** — string manipulation, formatting, generation.
- **`Tools.Enum`** — enum descriptions, values, parsing.
- **`Tools.Values`** — futures (`Tools.Values.Future.Explicit(...)`, `Tools.Values.Future.Reloadable(...)`), clipping, comparison.
- **`Tools.Lambda`** / **`Tools.Expression`** — expression tree helpers.
- **`Tools.Reflection`** — type inspection, property access, activation.
- **`Tools.Runtime`** — `IsDebugBuild`, framework version, OS detection.
- **`Tools.FileSystem`** — temp files, read/write, directory management.
- **`Tools.Stream`** — stream decorators, bounded streams, read/write all bytes.
- **`Tools.Scope`** — transactional scope management.
- **`Tools.Sqlite`** / **`Tools.MSSQL`** / **`Tools.Firebird`** — database helpers.
- **`Tools.NUnit`** — test utilities (e.g., 2D array formatting).

`WinTool` is a **static facade** with `[ThreadStatic]` singleton properties (`Registry`, `Services`, `Security`, `Processes`, `Win32`) that return instance-based utility classes (e.g., `RegistryUtil`, `ServicesUtil`).

To add a new tool, create a `static class` in the `Tools` namespace anywhere in the codebase — it becomes discoverable via `Tools.` intellisense automatically.

### Builder Facade Pattern
The codebase favors **fluent builder classes** for configuring complex objects. Builders accumulate state via chainable `With*`/`Add*`/`Configure*` methods and finalize with `.Build()`. Key examples:
- **`SerializerBuilder`** — builds `IItemSerializer<T>` by specifying member serializers: `SerializerBuilder.For<T>().Serialize(x => x.Prop, serializer).Build()`. Also supports `.SerializeMembersAutomatically()` for convention-based assembly.
- **`ProtocolBuilder`** — builds communication `Protocol` objects with handshake, request/response, and command handlers per mode.
- **`ApplicationBlockBuilder`** — builds WinForms `ApplicationBlock` with screens and menus: `.WithName().WithDefaultScreen<T>().AddMenu(mb => ...).Build()`.
- **`WizardBuilder<T>`** — builds multi-step wizard dialogs: `.WithTitle().WithModel().AddScreen().OnFinished().Build()`.

When creating new complex configuration APIs, follow this pattern: create a `FooBuilder` class with chainable methods and a terminal `.Build()`.

### Serialization
The serialization framework provides binary serializers for efficient stream-based I/O. Core types:
- **`IItemSerializer<T>`** — serializes/deserializes `T` via `EndianBinaryWriter`/`EndianBinaryReader` with a `SerializationContext`. Extends `IItemSizer<T>` for size calculation.
- **`ItemSerializerBase<T>`** — abstract base implementing `IItemSerializer<T>`.
- **`SerializerFactory`** — registry of type→serializer mappings with auto-resolution for generics, enums, and composite types. `SerializerFactory.Default` is the global singleton with all primitive and common-type serializers pre-registered. Custom factories chain from it via `new SerializerFactory(baseFactory)`.
- **`SerializerBuilder`** — fluent builder for composite serializers (see Builder pattern above).
- **`CompositeSerializer`** — auto-serializes all readable/writable members of a type.
- **`PolymorphicSerializer`** — dispatches to concrete-type serializers for abstract types.
- Serializers are endian-aware (`EndianBitConverter`, `Endianness`).

### ClusteredStreams
`ClusteredStreams` is a virtual file-system that multiplexes many logical `Stream`s onto a single backing stream using a cluster-based allocation model (similar to FAT). Core types:
- **`ClusteredStreams`** — manages a `ClusterMap` of fixed-size clusters, a stream descriptor list, and reserved streams. Supports `Add`, `OpenRead`, `OpenWrite`, `Remove`, `Insert`, `Swap`, `Clear`. Provides pluggable `IClusteredStreamsAttachment` for indexes and merkle trees.
- **`ClusteredStream`** — a `StreamDecorator` representing a single logical stream backed by a cluster chain. Opened via `ClusteredStreams.Open*`.
- **`ObjectStream<T>`** — stores typed objects in a `ClusteredStreams` using an `IItemSerializer<T>`. Acts like a "database table" with per-item stream storage.
- **`ObjectSpace`** — an ORM-like layer built on `ClusteredStreams` + `ObjectStream` + `SerializerFactory` for persisting object graphs with indexes, merkle trees, and change tracking. Provides `Get<T>`, `Save<T>`, `Delete<T>`, `New<T>`.
- **Stream-mapped collections** — `IStreamMappedList<T>`, `IStreamMappedDictionary<TKey, TValue>`, `IStreamMappedHashSet<T>` all build on `ObjectStream` for persistent, list/dict/set semantics with full CRUD, serialization, and optional merkle-tree integrity.

### DataSource Pattern (Sync/Async Hierarchy)
`IDataSource<T>` defines a **full dual sync/async interface** with both item-level and batch-level CRUD. The abstract implementation hierarchy avoids duplication:
- **`IDataSource<T>`** — declares all methods: `Create`, `Read`, `Update`, `Delete`, `Validate` (item), `CreateRange`, `ReadRange`, `UpdateRange`, `DeleteRange`, `ValidateRange` (batch), plus `*Async` counterparts. **Batch methods use the `Range` suffix** before `Async` (e.g., `CreateRange`, `CreateRangeAsync`).
- **`DataSourceBase<T>`** — abstract class declaring all methods as `abstract`.
- **`SyncBatchDataSourceBase<T>`** — for sync-first implementations. Override only the sync batch methods (`CreateRange`, `ReadRange`, etc.). Item methods delegate to batch, async methods wrap sync via `Task.Run`.
- **`AsyncBatchDataSourceBase<T>`** — for async-first implementations. Override only the async batch methods. Item methods delegate to batch, sync methods wrap async via `.ResultSafe()`/`.WaitSafe()`.
- **`FutureListDataSource<T>`** — extends `SyncBatchDataSourceBase<T>`, backed by an `IFuture<IExtendedList<T>>`. Provides default CRUD over the future list.
- **`BulkFetchDataSource<T>`** — extends `FutureListDataSource<T>` with a `Reloadable` future. Call `Invalidate()` to force re-fetch on next access. Use for lazy/invalidatable data fetching.
- **`ListDataSource<T>`** — wraps an existing `IExtendedList<T>` as a data source.
- **`ProjectedDataSource<TFrom, TTo>`** — decorator that projects entities between types via `Func<TFrom, TTo>` / `Func<TTo, TFrom>` mappings.

### Collections (ExtendedList & Stream-Mapped)
The framework provides `long`-indexed collection interfaces and implementations:
- **`IExtendedCollection<T>`** — extends `ICollection<T>` with `long Count` and range operations.
- **`IExtendedList<T>`** — extends `IExtendedCollection<T>` with `long` indexing, `InsertRange`, `RemoveRange`, `UpdateRange`, `ReadRange`, `AddRange`, `IndexOfRange`.
- **`RangedListBase<T>`** — abstract base where single-item operations (`Add`, `Read`, `Update`, `Remove`) delegate to their `*Range` counterparts. Subclass and override only the range methods.
- **`ExtendedList<T>`** — resizable array-backed list with configurable capacity, growth size, and max capacity.
- **`StreamMappedList<T>`** / **`StreamMappedRecyclableList<T>`** — persistent lists backed by `ObjectStream<T>` and `ClusteredStreams`. Support recyclable (soft-delete/reuse) semantics.
- **`StreamMappedDictionary<TKey, TValue>`** / **`StreamMappedHashSet<T>`** — persistent dictionary/set backed by clustered streams with index lookups.
- **Observable**, **Synchronized**, **Transactional**, and **Merkle** decorator variants exist for most collection types.

### Logging
Logging uses a simple `ILogger` interface with `Debug`, `Info`, `Warning`, `Error`, `Exception` methods controlled by `LogOptions` flags. Core types:
- **`ILogger`** — interface with level methods and `Options` property.
- **`LoggerBase`** — abstract base that filters by `LogOptions` and delegates to `Log(LogLevel, string)`.
- **`SystemLog`** — static application-wide logger backed by a `MulticastLogger`. Call `SystemLog.Info(...)`, `SystemLog.Error(...)`, etc. Register sinks via `SystemLog.RegisterLogger(logger)`.
- **Decorator loggers** — `TimestampLogger`, `ThreadIdLogger`, `PrefixLogger`, `SynchronizedLogger`, `AsyncLogger` wrap an inner `ILogger` to add behavior.
- **Sink loggers** — `ConsoleLogger`, `DebugLogger`, `FileAppendLogger`, `RollingFileLogger`, `TextWriterLogger`, `EventLogLogger` (Windows), `TextBoxLogger` (WinForms).
- Default options: `VerboseProfile` in debug builds, `StandardProfile` otherwise (set via `Tools.Runtime.IsDebugBuild`).

### WinForms UI Pattern
- `ApplicationBlock` + `ApplicationBlockBuilder` for navigation registration (`.WithDefaultScreen<T>()`, `.AddMenu(mb => mb.AddScreenItem<T>())`).
- `ApplicationScreen` base class for screens within a block.
- `CrudGrid` for data-bound grid screens backed by `IDataSource<T>`.

## License Header

All new source files must include the copyright header:
```csharp
// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.
```

## Unit Testing

### Framework & Conventions
- Use **NUnit** as the test framework.
- **Do not** use `ClassicAssert` or any legacy NUnit assertion style (`Assert.IsTrue`, `Assert.AreEqual`, `ClassicAssert.IsTrue`, etc.).
- **Always** use the modern **constraint-model assertions** via `Assert.That(...)`:
  ```csharp
  // ✅ Correct
  Assert.That(result, Is.EqualTo(expected));
  Assert.That(flag, Is.True);
  Assert.That(collection, Is.Not.Empty);
  Assert.That(() => Foo(), Throws.InstanceOf<InvalidOperationException>());

  // ❌ Wrong — do not use
  ClassicAssert.AreEqual(expected, result);
  ClassicAssert.IsTrue(flag);
  Assert.IsTrue(flag);
  ```
- Include a descriptive failure message where it aids diagnosis: `Assert.That(result, Is.True, "Signature must verify against the correct public key");`
- Use `[TestFixture]`, `[Test]`, `[TestCase(...)]`, `[Values(...)]`, `[Repeat(n)]` attributes as appropriate.
- Use `[Parallelizable(ParallelScope.Children)]` on test fixtures unless tests share mutable state.

## Committing
- Prefer **granular commits per logical task** rather than bulk commits. This does not mean one commit per file — group related changes across files into a single commit when they serve the same logical purpose.
- Write commit messages in **imperative mood** using a **title + detail** format:
  ```
  Short summarative title of overall change

  - specific logical change 1
  - specific logical change 2
  - specific logical change N
  ```
  The first line is the title shown in log views; the body (after a blank line) lists individual logical changes. Example:
  ```
  Harden RNG and fix deterministic key generation

  - Remove public Seed property from HashRandom
  - Fix GeneratePrivateKey to use seeded DigestRandomGenerator
  - Add SeedGenerator with environment entropy collection
  ```
- Do **not** amend or force-push unless explicitly asked to.
- Stage only the files relevant to the logical change — do not include unrelated modifications.
- When multiple logical changes are ready, commit them **separately** in dependency order.
