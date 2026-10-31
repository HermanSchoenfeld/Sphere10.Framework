<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

<p align="center">
  <img  src="resources/branding/sphere-10-framework-logo.jpg" alt="Sphere10 Framework logo">
</p>

![Version](https://img.shields.io/badge/version-3.1.3-blue)
[![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.svg)](https://www.nuget.org/packages/Sphere10.Framework)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

# Sphere10 Framework

Copyright © Herman Schoenfeld 2005 - Present

**Build rich applications, powerful storage engines and cryptographic systems with one modular .NET toolkit.**

Sphere10 Framework brings application shells, data access, embedded object storage, advanced collections, binary serialization and cryptography into a consistent set of C# libraries. Build a complete desktop or web application, compose your own storage engine, or pick the components your existing application needs.

[Explore the features](#what-you-can-build) · [Try the demos](#see-it-in-action) · [Read the docs](docs/README.md) · [Install](#installation)

## What you can build

### Complete desktop and web applications

Compose modular applications from **plugins, application blocks, menus, toolbars, screens and wizards**. WinForms and Blazor adapters share the application model, dependency injection and lifecycle conventions, with platform-specific screen rendering and hosting.

- **Windows Forms:** native application shells, detachable tabs, CRUD grids, data-bound editors, validation, splash screens and chart controls.
- **Blazor:** Interactive Server workspaces with tabbed navigation, typed CRUD columns, buffered editing, reference pickers, custom commands, dialogs, wizards and theme switching.
- **Application infrastructure:** startup and shutdown tasks, persisted settings, command-line parsing, logging, screen lifetime policies and unsaved-change handling.

[Application framework](src/Sphere10.Framework.Application/README.md) · [Windows Forms](src/Sphere10.Framework.Windows.Forms/README.md) · [Blazor](src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md)

### Embedded object databases in ordinary streams

**ObjectSpaces persist typed C# objects directly**, with declared identifier, unique and nonunique member indexes, tracked object identity, recyclable storage slots and optional dirty tracking. Choose memory-backed storage or file-backed transactions with explicit commit and rollback.

Enable self-describing ObjectSpaces and ObjectStreams to carry their construction metadata, including serializer registrations and index settings, so supported stores can be reconstructed from their raw stream. Merkle attachments add cryptographic integrity roots to object stores.

[ObjectSpace guide](src/Sphere10.Framework/ObjectSpaces/README.md) · [Self-describing storage](docs/usage/self-describing-storage.md)

### Collections that fit your data and your storage

Work with **memory, paged and stream-backed lists, dictionaries and sets**, managed and persisted B-trees and B+ trees, grouped lookups, recyclable collections, synchronized wrappers and projected views. Extended collection contracts support long-indexed range operations for batch processing.

Clustered streams multiplex independent logical streams into a single backing stream, with dynamic allocation and pluggable attachments. Page sizes, cache budgets, cluster sizes and index layouts give you direct control over memory and I/O policy.

[Core library](src/Sphere10.Framework/README.md) · [Collections and storage](docs/guidelines/patterns/collections-and-storage.md)

### Binary serialization for complex object graphs

Compose serializers with **explicit member layouts, polymorphic types, shared references, cyclic graphs, size calculation and byte-order control**. Serializer factories and fluent builders connect the same serialization contracts to object stores, collections and typed message protocols.

Use fixed-size records where predictable offsets matter, compact integer encodings where size matters, and stream-based readers and writers throughout.

[Serialization guide](docs/guidelines/patterns/serialization.md)

### Cryptographic integrity and specialized security primitives

Build with **pluggable hashing, digital-signature abstractions, verifiable random functions and dynamic Merkle trees**. Merkle-aware lists, dictionaries, sets and object stores bring integrity roots and proofs into the storage layer.

CryptoEx adds elliptic-curve signing and encryption, Schnorr and MuSig implementations, and additional cryptographic integrations. The core also contains reference implementations of the AMS and Winternitz signature families, alongside the project's published technical papers.

For blockchain applications, compose reversible chain state, chain and branch management, and proof-of-work target and difficulty primitives.

[Core cryptography and Merkle trees](src/Sphere10.Framework/README.md) · [CryptoEx](src/Sphere10.Framework.CryptoEx/README.md) · [Blockchain primitives](src/Sphere10.Framework.Consensus/README.md) · [Research papers](#technical-papers)

### Relational data access across database engines

Use a common **data-access contract, scoped connections and transactions, SQL builders and schema metadata** across SQLite, SQL Server, Firebird, MySQL, PostgreSQL and Oracle. NHibernate integration supports ORM-based applications, while platform-specific controls connect database access to native desktop workflows.

The shared abstractions keep application code consistent; provider guides document their supported SQL and capabilities.

[Data access](src/Sphere10.Framework.Data/README.md) · [NHibernate](src/Sphere10.Framework.Data.NHibernate/README.md)

### Connected services and reusable application infrastructure

Define typed protocols and request/response flows, expose JSON-RPC services, and connect processes through TCP/UDP channels, anonymous pipes and WebSocket implementations. ASP.NET Core and MVC libraries supply web hosting, middleware, controller and form integrations.

Round out your application with scheduled jobs, composable caches and loggers, synchronization scopes, reflection and mapping helpers, source generators, and NUnit utilities for application and database testing.

[Communications](src/Sphere10.Framework.Communications/README.md) · [ASP.NET Core](src/Sphere10.Framework.Web.AspNetCore/README.md) · [Job scheduling](docs/guidelines/patterns/scheduling.md)

## Discover the toolkit with `Tools.*`

Type **`Tools.`** in your IDE to explore utilities grouped by domain: `Tools.Text`, `Tools.Collection`, `Tools.FileSystem`, `Tools.Reflection`, `Tools.Streams`, `Tools.Crypto` and more. The same discovery pattern extends into database, drawing, web and Windows libraries through their owning packages.

[Browse the Tools reference](docs/tools-reference.md).

## See it in action

| Explore | What you can try |
|---|---|
| [Windows Forms tester](utils/Sphere10.Framework.Utils.WinFormsTester/README.md) | Native application screens, ObjectSpaces, CRUD controls, charts and component demonstrations |
| [Blazor tester](utils/Sphere10.Framework.Utils.BlazorTester/README.md) | A modular workspace with tabs, editable grids, menus, toolbars, themes, dialogs and wizards |
| [Blazor CRUD quick start](src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#crud-grid-quick-start) | Build an editable grid with typed columns and validation |
| [Custom editors and reference pickers](src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#custom-editors-reference-pickers-and-actions) | Connect related records and add application-specific actions |
| [MVC tester](utils/Sphere10.Framework.Utils.MvcTester/README.md) | A runnable ASP.NET Core MVC application |
| [ObjectSpace profiler](utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md) | Exercise storage workloads, allocation policies, indexes and file transactions with reproducible measurements |

## Browse the libraries

The libraries are organized under `src/`, with tests under `tests/` and runnable demonstrations and profiling tools under `utils/`. Select the packages that fit your application; the project guides cover their APIs and platform requirements.

<details>
<summary><strong>Library and test project catalog</strong></summary>

## :open_file_folder: Project Structure

> **Note**: The `blackhole/` directory contains archived/experimental projects (DApp framework) that are not part of the active framework. These are retained for reference but are not maintained or included in NuGet packages.

Projects are grouped by their role:

### :gear: Core Framework & Utilities

| Project | Purpose | NuGet |
|---------|---------|-------|
| [**Sphere10.Framework**](src/Sphere10.Framework/README.md) | General-purpose core library with utilities for caching, collections, cryptography, serialization, streaming, and more | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.svg)](https://www.nuget.org/packages/Sphere10.Framework) |
| [**Sphere10.Framework.Application**](src/Sphere10.Framework.Application/README.md) | Application lifecycle, dependency injection, shared application/block hierarchy, startup plugins and wizard orchestration | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Application.svg)](https://www.nuget.org/packages/Sphere10.Framework.Application) |
| [**Sphere10.Framework.Communications**](src/Sphere10.Framework.Communications/README.md) | Multi-protocol networking layer: TCP, UDP, WebSockets, RPC, and pipes | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Communications.svg)](https://www.nuget.org/packages/Sphere10.Framework.Communications) |
| [**Sphere10.Framework.Generators**](src/Sphere10.Framework.Generators/README.md) | C# source generators for compile-time code generation | |
| [**Sphere10.Framework.Runtime**](src/Sphere10.Framework.Runtime/) | Internal runtime/compiler support assembly | |
| [**Sphere10.HashLib4CSharp**](src/Sphere10.HashLib4CSharp/README.md) | Hashing library with support for MD5, SHA, BLAKE2, CRC, checksums, and more | [![NuGet](https://img.shields.io/nuget/v/Sphere10.HashLib4CSharp.svg)](https://www.nuget.org/packages/Sphere10.HashLib4CSharp) |

### :lock: Cryptography & Security

| Project | Purpose | NuGet |
|---------|---------|-------|
| [**Sphere10.Framework.CryptoEx**](src/Sphere10.Framework.CryptoEx/README.md) | Elliptic-curve signing/encryption, Schnorr, MuSig and cryptographic integrations | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.CryptoEx.svg)](https://www.nuget.org/packages/Sphere10.Framework.CryptoEx) |
| [**Sphere10.Framework.Consensus**](src/Sphere10.Framework.Consensus/README.md) | Reversible blockchain state, chain/branch management and proof-of-work target/difficulty primitives | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Consensus.svg)](https://www.nuget.org/packages/Sphere10.Framework.Consensus) |

### :floppy_disk: Data Access & Persistence

| Project | Purpose | NuGet |
|---------|---------|-------|
| [**Sphere10.Framework.Data**](src/Sphere10.Framework.Data/README.md) | Data access abstraction layer with ADO.NET enhancements, SQL query building, CSV support | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Data.svg)](https://www.nuget.org/packages/Sphere10.Framework.Data) |
| [**Sphere10.Framework.Data.Sqlite**](src/Sphere10.Framework.Data.Sqlite/README.md) | SQLite implementation for embedded databases | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Data.Sqlite.svg)](https://www.nuget.org/packages/Sphere10.Framework.Data.Sqlite) |
| [**Sphere10.Framework.Data.Firebird**](src/Sphere10.Framework.Data.Firebird/README.md) | Firebird database implementation | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Data.Firebird.svg)](https://www.nuget.org/packages/Sphere10.Framework.Data.Firebird) |
| [**Sphere10.Framework.Data.MSSQL**](src/Sphere10.Framework.Data.MSSQL/README.md) | Microsoft SQL Server implementation | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Data.MSSQL.svg)](https://www.nuget.org/packages/Sphere10.Framework.Data.MSSQL) |
| [**Sphere10.Framework.Data.NHibernate**](src/Sphere10.Framework.Data.NHibernate/README.md) | NHibernate ORM integration | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Data.NHibernate.svg)](https://www.nuget.org/packages/Sphere10.Framework.Data.NHibernate) |
| [**Sphere10.Framework.Data.MySQL**](src/Sphere10.Framework.Data.MySQL/README.md) | MySQL data-access provider | |
| [**Sphere10.Framework.Data.PostgreSQL**](src/Sphere10.Framework.Data.PostgreSQL/README.md) | PostgreSQL data-access provider | |
| [**Sphere10.Framework.Data.Oracle**](src/Sphere10.Framework.Data.Oracle/README.md) | Oracle data-access provider | |

### :desktop_computer: Desktop & Windows

| Project | Purpose | NuGet |
|---------|---------|-------|
| [**Sphere10.Framework.Windows**](src/Sphere10.Framework.Windows/README.md) | Windows platform integration: registry, services, event logging | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows) |
| [**Sphere10.Framework.Windows.Forms**](src/Sphere10.Framework.Windows.Forms/README.md) | Windows Forms UI framework and components | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.Forms.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows.Forms) |
| [**Sphere10.Framework.Windows.Forms.DataVisualization**](src/Sphere10.Framework.Windows.Forms.DataVisualization/README.md) | Microsoft charting controls adapted to modern .NET, retaining upstream licenses | |
| [**Sphere10.Framework.Windows.Forms.Sqlite**](src/Sphere10.Framework.Windows.Forms.Sqlite/README.md) | SQLite connection editors for Windows Forms | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.Forms.Sqlite.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows.Forms.Sqlite) |
| [**Sphere10.Framework.Windows.Forms.Firebird**](src/Sphere10.Framework.Windows.Forms.Firebird/README.md) | Firebird connection editors for Windows Forms | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.Forms.Firebird.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows.Forms.Firebird) |
| [**Sphere10.Framework.Windows.Forms.MSSQL**](src/Sphere10.Framework.Windows.Forms.MSSQL/README.md) | SQL Server connection editors for Windows Forms | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.Forms.MSSQL.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows.Forms.MSSQL) |
| [**Sphere10.Framework.Windows.Forms.MySQL**](src/Sphere10.Framework.Windows.Forms.MySQL/README.md) | MySQL connection editors for Windows Forms | |
| [**Sphere10.Framework.Windows.Forms.PostgreSQL**](src/Sphere10.Framework.Windows.Forms.PostgreSQL/README.md) | PostgreSQL connection editors for Windows Forms | |
| [**Sphere10.Framework.Windows.Forms.Oracle**](src/Sphere10.Framework.Windows.Forms.Oracle/README.md) | Oracle connection editors for Windows Forms | |
| [**Sphere10.Framework.Windows.LevelDB**](src/Sphere10.Framework.Windows.LevelDB/README.md) | LevelDB integration for fast key-value storage | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.LevelDB.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows.LevelDB) |

### :globe_with_meridians: Web & Cross-Platform

| Project | Purpose | NuGet |
|---------|---------|-------|
| [**Sphere10.Framework.Web**](src/Sphere10.Framework.Web/README.md) | HTML, sitemap, and presentation models independent of ASP.NET Core | |
| [**Sphere10.Framework.Web.AspNetCore**](src/Sphere10.Framework.Web.AspNetCore/README.md) | Shared ASP.NET Core HTTP, middleware, logging, and lifecycle integration | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Web.AspNetCore.svg)](https://www.nuget.org/packages/Sphere10.Framework.Web.AspNetCore) |
| [**Sphere10.Framework.Web.AspNetCore.MVC**](src/Sphere10.Framework.Web.AspNetCore.MVC/README.md) | MVC controllers, forms, filters, routing, and action results | |
| [**Sphere10.Framework.Web.AspNetCore.Blazor**](src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) | Tabbed Blazor applications, extensible identity controls, and CRUD grids with typed columns, validation, reference editors, dialogs and wizards | |
| [**Sphere10.Framework.Drawing**](src/Sphere10.Framework.Drawing/README.md) | Color and geometry helpers, plus Windows image processing and GDI+ rendering | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Drawing.svg)](https://www.nuget.org/packages/Sphere10.Framework.Drawing) |
| [**Sphere10.Framework.NUnit**](src/Sphere10.Framework.NUnit/README.md) | NUnit testing utilities and framework test support | |
| [**Sphere10.Framework.NUnit.DB**](src/Sphere10.Framework.NUnit.DB/README.md) | Database-specific NUnit testing utilities | |
| [**Sphere10.Framework.iOS**](src/Sphere10.Framework.iOS/README.md) | Legacy Xamarin.iOS integration, outside the active .NET 10 solutions | |
| [**Sphere10.Framework.Android**](src/Sphere10.Framework.Android/README.md) | Legacy Xamarin.Android integration, outside the active .NET 10 solutions | |
| [**Sphere10.Framework.macOS**](src/Sphere10.Framework.macOS/README.md) | Legacy macOS integration source, outside the active .NET 10 solutions | |

## :test_tube: Test Projects

The `tests/` directory contains unit and integration tests across framework subsystems:

GitHub Actions builds the cross-platform solution and runs the configured NUnit test assemblies, including the restored Blazor tests. Console output and TRX artifacts include test results and failures. See the [CI test guide](.github/ci.md) for adding tests and reproducing failures locally.

| Test Project | Purpose |
|--------------|---------|
| [**Sphere10.HashLib4CSharp.Tests**](tests/Sphere10.HashLib4CSharp.Tests) | Tests for hashing algorithms |
| [**Sphere10.Framework.Communications.Tests**](tests/Sphere10.Framework.Communications.Tests) | Networking and RPC tests |
| [**Sphere10.Framework.CryptoEx.Tests**](tests/Sphere10.Framework.CryptoEx.Tests) | Cryptography implementation tests |
| [**Sphere10.Framework.Data.Tests**](tests/Sphere10.Framework.Data.Tests) | Database access layer tests |
| [**Sphere10.Framework.Tests**](tests/Sphere10.Framework.Tests) | Core framework tests |
| [**Sphere10.Framework.Application.Tests**](tests/Sphere10.Framework.Application.Tests/README.md) | Shared presentation behavior and UI-independent dependency tests |
| [**Sphere10.Framework.Web.AspNetCore.Tests**](tests/Sphere10.Framework.Web.AspNetCore.Tests) | Shared web and MVC integration regressions |
| [**Sphere10.Framework.Web.AspNetCore.Blazor.Tests**](tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests) | CRUD grid, table, wizard, plugin, ApplicationBlock and lifecycle regression tests |
| [**Sphere10.Framework.Windows.LevelDB.Tests**](tests/Sphere10.Framework.Windows.LevelDB.Tests) | LevelDB integration tests |
| [**Sphere10.Framework.Windows.Tests**](tests/Sphere10.Framework.Windows.Tests) | Windows platform tests |

</details>

## Documentation and learning

### Getting started

- [**Documentation Home**](docs/README.md) — Complete documentation index
- [**Framework To-do**](src/TODO.md) — Running engineering backlog across all Framework subsystems
- [**Getting Started Guide**](docs/start-here.md) — Quick orientation for new developers
- [**Tools Reference**](docs/tools-reference.md) — Complete Tools.* namespace catalog
- [**Real-World Examples**](docs/real-world-usage-examples.md) — Practical patterns from test suite

### Architecture

- [Framework Architecture](docs/architecture/sphere10-framework.md) — Design philosophy, layers, subsystems
- [Framework Domains](docs/architecture/domains.md) — Catalog of 40+ specialized domains

### Engineering guides

- [3-Tier Architecture](docs/guidelines/3-tier-architecture.md) — Architectural patterns
- [Code Styling](docs/guidelines/code-styling.md) — Coding standards and conventions

### Technical papers

- [Dynamic Merkle Trees](https://sphere10.com/tech/dynamic-merkle-trees)
- [Abstract Merkle Signatures (AMS)](https://sphere10.com/tech/ams)
- [Winternitz Abstracted Merkle Signatures (WAMS)](https://sphere10.com/tech/wams)
- [Faster and Smaller Winternitz Signatures](https://sphere10.com/tech/wots-sharp)

## Contributing

Contributions are welcome!

To submit a change:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes
4. Push to your branch
5. Open a Pull Request

Please follow the [Code Styling Guidelines](docs/guidelines/code-styling.md).

## License

Sphere10 Framework is distributed under the **MIT License**.

More information: [MIT License](https://opensource.org/license/mit)

## Installation

### Prerequisites

- **.NET 10 SDK**
- An IDE that supports the **.NET 10 SDK**, such as Visual Studio or JetBrains Rider
- **Windows** required for `Sphere10.Framework.Windows.*` projects
- Two solution files available:
  - `src/Sphere10.Framework (CrossPlatform).sln` — Cross-platform projects only
  - `src/Sphere10.Framework (Win).sln` — All projects including Windows-specific

Both solutions expose architecture guides, engineering practices and reference documents under **Developer Documentation**. The root README appears once in **Misc**, with packaging scripts under **Misc → Scripts**. Project, test and demo READMEs remain in their real projects. Solution items open the original files in Visual Studio.

### NuGet packages

Sphere10 Framework is available as NuGet packages. Install the packages you need:

```bash
# Core framework (required)
dotnet add package Sphere10.Framework

# Additional packages as needed
dotnet add package Sphere10.Framework.Application
dotnet add package Sphere10.Framework.Data
dotnet add package Sphere10.Framework.Data.Sqlite
dotnet add package Sphere10.Framework.CryptoEx
dotnet add package Sphere10.Framework.Communications
```

### Building and publishing packages

Use Windows with the .NET 10 SDK to build the full package set. Set the numeric release version in `VersionPrefix` in [Directory.Build.props](Directory.Build.props); the package and assembly versions derive from it. CI supplies `BuildRevision` from its workflow run number, producing file versions such as `3.1.3.114`. Local builds allocate the next number from `LOCAL-BUILD-NUMBER` in the solution folder (gitignored); override with `-p:BuildRevision=1` for `3.1.3.1`. The NuGet version remains `3.1.3`, and assembly identity remains `3.1.3.0` across builds. For prerelease packages, pass `-p:Version=3.1.3-preview.1` while keeping `VersionPrefix` numeric. Use a new package version for each release; published versions cannot be overwritten.

Before release packaging, run [validate-packages.ps1](scripts/validate-packages.ps1). It packs every active public library under a unique local validation version, checks package metadata and internal dependencies, and restores external consumers from an isolated feed and package cache. It builds core, Application-only, database/testing/generator and Windows consumers, publishes a Blazor ApplicationBlock host, and verifies Razor assets. Files and logs remain in the reported temporary directory; nothing is published. See [package consumer validation](scripts/package-consumers/README.md) for details. `Sphere10.Framework.NUnit`, `Sphere10.Framework.NUnit.DB` and `Sphere10.Framework.Generators` are distributable packages; the runtime/compiler support project remains internal.

From the Framework repository root, run [pack.ps1](scripts/pack.ps1) to clean and pack the Windows solution in Release configuration. It builds into a staging directory, verifies matching package and symbol files, then replaces the framework packages in `nuget-packages`. Failed builds leave the previous packages intact; unrelated files and packages are preserved. Packing does not run tests. Use `-OutputDirectory` to select a different destination.

```powershell
./scripts/validate-packages.ps1
./scripts/pack.ps1
./scripts/publish.ps1 -IncludeSymbols -WhatIf
./scripts/publish.ps1 -IncludeSymbols
```

[publish.ps1](scripts/publish.ps1) validates the entire batch before publishing: each package must have a matching ID and version, duplicate versions are rejected, and requested symbols must match their packages. Only `Sphere10.Framework`, its subpackages, and `Sphere10.HashLib4CSharp` are selected. Independent packages such as `Sphere10.VisualRenderer` are packed separately. Symbols are sent only with `-IncludeSymbols`. `-WhatIf` validates and previews without requesting credentials or pushing anything.

The publisher accepts `-ApiKey`, reads `NUGET_API_KEY`, or prompts with hidden input. Ordinary publication asks for PowerShell confirmation; use `-Confirm:$false` for an automated run with credentials supplied. `-Source`, `-SymbolSource`, and `-PackagesDirectory` override the default feeds and package directory. Both scripts support Windows PowerShell 5.1 and PowerShell 7 and keep their default paths rooted in the Framework repository.
