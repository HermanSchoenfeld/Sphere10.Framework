<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

<p align="center">
  <img  src="resources/branding/sphere-10-framework-logo.jpg" alt="Sphere10 Framework logo">
</p>

![Version](https://img.shields.io/badge/version-3.0.3-blue)
[![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.svg)](https://www.nuget.org/packages/Sphere10.Framework)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT%20NON--AI-green)

# :rocket: Sphere10 Framework: Comprehensive .NET Application Framework

Copyright © Herman Schoenfeld, Sphere 10 Software 2005 - Present

**A mature, production-ready .NET framework** providing a complete foundation for building full-stack applications across desktop, mobile, and web platforms. Originally designed for blockchain systems, Sphere10 Framework has evolved into a comprehensive general-purpose framework offering robust abstractions, advanced data structures, cryptographic primitives, and utilities for high-performance .NET development.

## :sparkles: What Sphere10 Framework Provides

**Core Foundation**
- **Unified Architecture**: Consistent patterns for application lifecycle, dependency injection, configuration, and component lifecycle across all platforms
- **Enterprise Data Access**: Abstracted data layer with support for multiple database engines (SQL Server, SQLite, Firebird, NHibernate) and advanced query building
- **Advanced Cryptography**: Comprehensive cryptographic implementations including post-quantum algorithms, digital signatures, and multiple hashing algorithms
- **Multi-Protocol Networking**: TCP, UDP, WebSockets, and RPC frameworks for building distributed systems
- **Rich Serialization**: Flexible binary serialization, JSON support, and streaming implementations

**Application Development**
- **Desktop UI Framework**: Full-featured Windows Forms component library with data binding, validation, and plugin support
- **Web Integration**: ASP.NET Core middleware, filters, routing, and form components for server-side web applications
- **Cross-Platform**: Run applications on Windows, macOS, iOS, Android, or .NET Core/5+
- **Plugin Architecture**: Dynamic plugin loading and lifecycle management for extensible applications

**Specialized Features**
- **Memory Efficiency**: Advanced collections, paged data structures, and streaming for handling large datasets
- **Graphics & Drawing**: Cross-platform drawing utilities and image manipulation
- **Performance**: Caching, connection pooling, and optimized algorithms
- **Testing**: Comprehensive testing framework and utilities for unit and integration testing

## :wrench: Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** (17.8+) or **JetBrains Rider** (2023.3+) recommended
- **Windows** required for `Sphere10.Framework.Windows.*` projects
- Two solution files available:
  - `src/Sphere10.Framework (CrossPlatform).sln` — Cross-platform projects only
  - `src/Sphere10.Framework (Win).sln` — All projects including Windows-specific

## :package: Installation

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

## :mag: Tools.* Namespace — Global Utility Discovery

The **Tools namespace** is a defining architectural feature providing a **global, IntelliSense-discoverable collection of static utility methods** across the entire framework. Simply type `Tools.` to explore all available operations:

### Core Utilities
- **Tools.Crypto** — Hashing, signatures, key derivation
- **Tools.Text** — String manipulation, validation, generation
- **Tools.Collection** — Collection operations, filtering, transformation
- **Tools.FileSystem** — File I/O, directory management, temp files
- **Tools.Reflection** — Type inspection, member discovery, attributes
- **Tools.Json** / **Tools.Xml** — Data serialization
- **Tools.Memory** — Buffer operations, memory allocation
- **Tools.Maths** — Mathematical utilities and RNG
- And 30+ more...

### Platform-Specific Tools
- **Tools.WinTool** (Windows) — Registry, services, event logging, privileges
- **Tools.Web.Html** / **Tools.Web.AspNetCore** (Web) — HTML utilities, ASP.NET Core integration
- **Tools.iOSTool** (iOS) — iOS-specific operations
- **Tools.Data** / **Tools.Sqlite** / **Tools.MSSQL** (Database) — Database provider utilities

### Design Pattern
```csharp
using Sphere10.Framework;

// Discovery-first pattern — IntelliSense shows all available tools
byte[] hash = Tools.Crypto.SHA256(data);
string sanitized = Tools.Text.RemoveWhitespace(input);
var connection = Tools.Sqlite.Create(connectionString);
bool running = Tools.WinTool.IsServiceRunning("MyService");
```

For the complete Tools reference, see [docs/tools-reference.md](docs/tools-reference.md).



## :open_file_folder: Project Structure

> **Note**: The `blackhole/` directory contains archived/experimental projects (DApp framework) that are not part of the active framework. These are retained for reference but are not maintained or included in NuGet packages.

The Sphere10 Framework consists of **45+ projects** organized by category within `src/`, `tests/`, and `utils/`:

### :gear: Core Framework & Utilities

| Project | Purpose | NuGet |
|---------|---------|-------|
| [**Sphere10.Framework**](src/Sphere10.Framework/README.md) | General-purpose core library with utilities for caching, collections, cryptography, serialization, streaming, and more | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.svg)](https://www.nuget.org/packages/Sphere10.Framework) |
| [**Sphere10.Framework.Application**](src/Sphere10.Framework.Application/README.md) | Application lifecycle, dependency injection, command-line interface, and presentation framework | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Application.svg)](https://www.nuget.org/packages/Sphere10.Framework.Application) |
| [**Sphere10.Framework.Communications**](src/Sphere10.Framework.Communications/README.md) | Multi-protocol networking layer: TCP, UDP, WebSockets, RPC, and pipes | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Communications.svg)](https://www.nuget.org/packages/Sphere10.Framework.Communications) |
| [**Sphere10.Framework.Generators**](src/Sphere10.Framework.Generators/README.md) | C# source generators for compile-time code generation | |
| [**Sphere10.Framework.Runtime**](src/Sphere10.Framework.Runtime/README.md) | Runtime environment detection, diagnostics, and platform utilities | |
| [**Sphere10.HashLib4CSharp**](src/Sphere10.HashLib4CSharp/README.md) | Hashing library with support for MD5, SHA, BLAKE2, CRC, checksums, and more | [![NuGet](https://img.shields.io/nuget/v/Sphere10.HashLib4CSharp.svg)](https://www.nuget.org/packages/Sphere10.HashLib4CSharp) |

### :lock: Cryptography & Security

| Project | Purpose | NuGet |
|---------|---------|-------|
| [**Sphere10.Framework.CryptoEx**](src/Sphere10.Framework.CryptoEx/README.md) | Extended cryptography: Bitcoin (SECP256k1), elliptic curves, hash functions, post-quantum algorithms | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.CryptoEx.svg)](https://www.nuget.org/packages/Sphere10.Framework.CryptoEx) |
| [**Sphere10.Framework.Consensus**](src/Sphere10.Framework.Consensus/README.md) | Blockchain consensus mechanisms and validation rules framework | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Consensus.svg)](https://www.nuget.org/packages/Sphere10.Framework.Consensus) |

### :floppy_disk: Data Access & Persistence

| Project | Purpose | NuGet |
|---------|---------|-------|
| [**Sphere10.Framework.Data**](src/Sphere10.Framework.Data/README.md) | Data access abstraction layer with ADO.NET enhancements, SQL query building, CSV support | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Data.svg)](https://www.nuget.org/packages/Sphere10.Framework.Data) |
| [**Sphere10.Framework.Data.Sqlite**](src/Sphere10.Framework.Data.Sqlite/README.md) | SQLite implementation for embedded databases | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Data.Sqlite.svg)](https://www.nuget.org/packages/Sphere10.Framework.Data.Sqlite) |
| [**Sphere10.Framework.Data.Firebird**](src/Sphere10.Framework.Data.Firebird/README.md) | Firebird database implementation | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Data.Firebird.svg)](https://www.nuget.org/packages/Sphere10.Framework.Data.Firebird) |
| [**Sphere10.Framework.Data.MSSQL**](src/Sphere10.Framework.Data.MSSQL/README.md) | Microsoft SQL Server implementation | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Data.MSSQL.svg)](https://www.nuget.org/packages/Sphere10.Framework.Data.MSSQL) |
| [**Sphere10.Framework.Data.NHibernate**](src/Sphere10.Framework.Data.NHibernate/README.md) | NHibernate ORM integration | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Data.NHibernate.svg)](https://www.nuget.org/packages/Sphere10.Framework.Data.NHibernate) |

### :desktop_computer: Desktop & Windows

| Project | Purpose | NuGet |
|---------|---------|-------|
| [**Sphere10.Framework.Windows**](src/Sphere10.Framework.Windows/README.md) | Windows platform integration: registry, services, event logging | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows) |
| [**Sphere10.Framework.Windows.Forms**](src/Sphere10.Framework.Windows.Forms/README.md) | Windows Forms UI framework and components | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.Forms.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows.Forms) |
| [**Sphere10.Framework.Windows.Forms.Sqlite**](src/Sphere10.Framework.Windows.Forms.Sqlite/README.md) | Windows Forms with SQLite data binding | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.Forms.Sqlite.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows.Forms.Sqlite) |
| [**Sphere10.Framework.Windows.Forms.Firebird**](src/Sphere10.Framework.Windows.Forms.Firebird/README.md) | Windows Forms with Firebird data binding | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.Forms.Firebird.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows.Forms.Firebird) |
| [**Sphere10.Framework.Windows.Forms.MSSQL**](src/Sphere10.Framework.Windows.Forms.MSSQL/README.md) | Windows Forms with SQL Server data binding | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.Forms.MSSQL.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows.Forms.MSSQL) |
| [**Sphere10.Framework.Windows.LevelDB**](src/Sphere10.Framework.Windows.LevelDB/README.md) | LevelDB integration for fast key-value storage | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Windows.LevelDB.svg)](https://www.nuget.org/packages/Sphere10.Framework.Windows.LevelDB) |

### :globe_with_meridians: Web & Cross-Platform

| Project | Purpose | NuGet |
|---------|---------|-------|
| [**Sphere10.Framework.Web.AspNetCore**](src/Sphere10.Framework.Web.AspNetCore/README.md) | ASP.NET Core integration: middleware, filters, routing, forms | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Web.AspNetCore.svg)](https://www.nuget.org/packages/Sphere10.Framework.Web.AspNetCore) |
| [**Sphere10.Framework.Drawing**](src/Sphere10.Framework.Drawing/README.md) | Cross-platform graphics and drawing utilities | [![NuGet](https://img.shields.io/nuget/v/Sphere10.Framework.Drawing.svg)](https://www.nuget.org/packages/Sphere10.Framework.Drawing) |
| [**Sphere10.Framework.NUnit**](src/Sphere10.Framework.NUnit/README.md) | NUnit testing utilities and framework test support | |
| [**Sphere10.Framework.NUnit.DB**](src/Sphere10.Framework.NUnit.DB/README.md) | Database-specific NUnit testing utilities | |
| [**Sphere10.Framework.iOS**](src/Sphere10.Framework.iOS/README.md) | Xamarin.iOS integration for native iOS apps | |
| [**Sphere10.Framework.Android**](src/Sphere10.Framework.Android/README.md) | Xamarin.Android integration for native Android apps | |
| [**Sphere10.Framework.macOS**](src/Sphere10.Framework.macOS/README.md) | Xamarin.macOS integration for native macOS apps | |

## :test_tube: Test Projects

The `tests/` directory contains **2000+ comprehensive unit and integration tests** covering all framework subsystems:


| Test Project | Purpose |
|--------------|---------|
| [**Sphere10.HashLib4CSharp.Tests**](tests/Sphere10.HashLib4CSharp.Tests) | Tests for hashing algorithms |
| [**Sphere10.Framework.Communications.Tests**](tests/Sphere10.Framework.Communications.Tests) | Networking and RPC tests |
| [**Sphere10.Framework.CryptoEx.Tests**](tests/Sphere10.Framework.CryptoEx.Tests) | Cryptography implementation tests |
| [**Sphere10.Framework.Data.Tests**](tests/Sphere10.Framework.Data.Tests) | Database access layer tests |
| [**Sphere10.Framework.Tests**](tests/Sphere10.Framework.Tests) | Core framework tests |
| [**Sphere10.Framework.Windows.LevelDB.Tests**](tests/Sphere10.Framework.Windows.LevelDB.Tests) | LevelDB integration tests |
| [**Sphere10.Framework.Windows.Tests**](tests/Sphere10.Framework.Windows.Tests) | Windows platform tests |

## :art: Presentation & UI Layer

### Desktop (Windows Forms)
| Project | Purpose |
|---------|---------|
| [**Sphere10.Framework.Windows.Forms**](src/Sphere10.Framework.Windows.Forms/README.md) | Comprehensive Windows Forms UI framework with data binding, validation, and component library |
| [**Sphere10.Framework.Windows.Forms.Sqlite**](src/Sphere10.Framework.Windows.Forms.Sqlite/README.md) | Windows Forms with SQLite data binding and persistence |
| [**Sphere10.Framework.Windows.Forms.MSSQL**](src/Sphere10.Framework.Windows.Forms.MSSQL/README.md) | Windows Forms with SQL Server data binding and persistence |
| [**Sphere10.Framework.Windows.Forms.Firebird**](src/Sphere10.Framework.Windows.Forms.Firebird/README.md) | Windows Forms with Firebird data binding and persistence |

### Web & Cross-Platform UI
| Project | Purpose |
|---------|---------|
| [**Sphere10.Framework.Web.AspNetCore**](src/Sphere10.Framework.Web.AspNetCore/README.md) | ASP.NET Core integration with middleware, filters, routing, and form components |
| [**Sphere10.Framework.Drawing**](src/Sphere10.Framework.Drawing/README.md) | Cross-platform graphics and drawing utilities for all platforms |

## :books: Documentation & Learning

### :open_book: Getting Started

- [**Documentation Home**](docs/README.md) — Complete documentation index
- [**Getting Started Guide**](docs/start-here.md) — Quick orientation for new developers
- [**Tools Reference**](docs/tools-reference.md) — Complete Tools.* namespace catalog
- [**Real-World Examples**](docs/real-world-usage-examples.md) — Practical patterns from test suite

### :triangular_ruler: Architecture

- [Framework Architecture](docs/architecture/sphere10-framework.md) — Design philosophy, layers, subsystems
- [Framework Domains](docs/architecture/domains.md) — Catalog of 40+ specialized domains

### :bookmark_tabs: Guidelines

- [3-Tier Architecture](docs/guidelines/3-tier-architecture.md) — Architectural patterns
- [Code Styling](docs/guidelines/code-styling.md) — Coding standards and conventions

### :chains: Technical Papers

- [Dynamic Merkle Trees](https://sphere10.com/tech/dynamic-merkle-trees)
- [Abstract Merkle Signatures (AMS)](https://sphere10.com/tech/ams)
- [Winternitz Abstracted Merkle Signatures (WAMS)](https://sphere10.com/tech/wams)
- [Faster and Smaller Winternitz Signatures](https://sphere10.com/tech/wots-sharp)

## :handshake: Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes
4. Push to your branch
5. Open a Pull Request

Please follow the [Code Styling Guidelines](docs/guidelines/code-styling.md).

## :balance_scale: License

Sphere10 Framework is distributed under the **MIT NON-AI License** — standard MIT permissions with an additional restriction prohibiting use of the code for training machine learning / AI models.

> :warning: This is **not** a standard MIT license. See [LICENSE](LICENSE) for the full terms.

More information: [Sphere10 NON-AI-MIT License](https://sphere10.com/legal/NON-AI-MIT)

