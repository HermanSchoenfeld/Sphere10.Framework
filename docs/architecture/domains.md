# Framework Domains

The Sphere10 Framework comprises a variety of interconnected projects and domains. A **domain** is a collection of code artifacts that are logically related in the abstraction they model. Domains can span multiple architectural tiers (UI, Business Logic, Data) and represent a vertical slice through the architecture.

For complete architecture overview, see [Sphere10 Framework Architecture](sphere10-framework.md).

---

## Sphere10 Framework Domains

The core framework provides **40+ domains** across system, processing, data, and presentation tiers.

### System Tier Domains (Sphere10.Framework)

Core utilities usable across any .NET application (desktop, web, mobile, server, embedded).

| Domain | Purpose | Tools | 
|--------|---------|-------|
| **Collections** | Advanced collection types: B-trees, merkle trees, paged collections, transactional, observable | Tools.Collection |
| **Cryptography** | Hashing, signatures, encryption, key derivation, post-quantum algorithms | Tools.Crypto |
| **Encoding** | Hex, Base64, endian conversion | Tools.Encoding |
| **Extensions** | 200+ extension methods for .NET core types | Tools.Extensions |
| **Exceptions** | Custom exception types and exception handling utilities | Tools.Exceptions |
| **Functional** | Functional programming support: expressions, lambdas, operators | Tools.Functional |
| **Introspection** | Fast reflection library, type discovery, member inspection | Tools.Reflection |
| **IO** | Endian-aware file I/O, stream tools, temporary file management | Tools.FileSystem, Tools.IO |
| **IoC** | Dependency injection container (TinyIoC-based), service registration | Tools.IoC |
| **Logging** | Multi-target logging framework with filtering and formatting | Tools.Logging |
| **Math** | Mathematical utilities: RNG, bloom filters, fixed-point math | Tools.Maths |
| **Memory** | Buffer operations, memory allocation, unit conversion | Tools.Memory |
| **Networking** | URL parsing, MIME types, socket utilities, P2P abstractions | Tools.Network |
| **Objects** | Clone, compare, merge, introspection tools | Tools.Objects |
| **ObjectSpaces** | Stream-mapped persistent collections with merkle tree tracking | Tools.ObjectSpace |
| **Serialization** | Object serialization across binary, JSON, XML formats | Tools.Serialization |
| **Streams** | Bit streams, blocking streams, bounded streams, stream pipelines | Tools.Streams |
| **Text** | String parsers, inflectors, regex builders, HTML utilities | Tools.Text |
| **TextWriters** | Console, debug, file-based, and custom TextWriter implementations | Tools.TextWriters |
| **Threading** | Multi-threading utilities, synchronization primitives | Tools.Threading |
| **Types** | Type activation, resolution, type-based switches | Tools.Types |
| **Values** | Futures, GUIDs, enums, results, value ranges | Tools.Values |
| **XML** | Deep XML serialization, XPath support | Tools.XML |

### Processing Tier Domains (Sphere10.Framework.Application)

Application lifecycle, DI, configuration, and plugin management.

| Domain | Purpose | Tools |
|--------|---------|-------|
| **Component Registry** | IoC container, dependency injection, service resolution | Tools.ComponentRegistry |
| **Configuration** | Settings management, environment-aware configuration | Tools.Configuration |
| **Plugin System** | Dynamic plugin loading, lifecycle management, plugin discovery | Tools.Plugins |
| **Command-Line** | CLI argument parsing, command execution, help text generation | Tools.CommandLine |

### Data Tier Domains (Sphere10.Framework.Data.*)

Multi-database abstraction layer with support for SQL Server, SQLite, Firebird, NHibernate.

| Domain | Purpose | Tools |
|--------|---------|-------|
| **Database Abstraction** | Unified ADO.NET abstraction across multiple databases | Tools.Data |
| **Query Building** | Fluent SQL query construction, parameter binding | Tools.Query |
| **SQL Server Provider** | Microsoft SQL Server-specific implementations | Tools.MSSQL |
| **SQLite Provider** | Embedded SQLite implementations | Tools.Sqlite |
| **Firebird Provider** | Firebird database-specific features | Tools.Firebird |
| **NHibernate Provider** | NHibernate ORM integration layer | Tools.NHibernate |
| **CSV Support** | CSV file reading/writing with type conversion | Tools.CSV |
| **Object Spaces** | Stream-mapped persistent collections with auto-tracking | Tools.ObjectSpace |

### Presentation Tier Domains

UI frameworks for desktop, web, and cross-platform applications.

| Domain | Purpose | Tools |
|--------|---------|-------|
| **Windows Forms** | Data binding, validation, component library | Tools.WinForms |
| **Windows Forms + SQLite** | Windows Forms with SQLite data binding | Tools.WinForms.Sqlite |
| **Windows Forms + SQL Server** | Windows Forms with SQL Server data binding | Tools.WinForms.MSSQL |
| **Windows Forms + Firebird** | Windows Forms with Firebird data binding | Tools.WinForms.Firebird |
| **Web / ASP.NET Core** | ASP.NET Core middleware, routing, form components | Tools.Web.AspNetCore |
| **Blazor Components** | Web UI components (modals, grids, wizards, etc.) | Tools.Blazor |
| **Drawing** | Cross-platform graphics, image manipulation | Tools.Drawing |

### Windows Integration Domains (Sphere10.Framework.Windows.*)

Windows platform-specific functionality.

| Domain | Purpose | Tools |
|--------|---------|-------|
| **Registry** | Windows registry access and manipulation | Tools.WinTool |
| **Services** | Windows service management and queries | Tools.WinTool |
| **Event Logging** | Windows event log reading/writing | Tools.WinTool |
| **Privileges** | User privilege elevation and checking | Tools.WinTool |
| **LevelDB** | LevelDB key-value storage integration | Tools.LevelDB |

### Platform-Specific Domains

Native integration for mobile and alternative platforms.

| Domain | Purpose | Tools |
|--------|---------|-------|
| **iOS Integration** | Xamarin.iOS utilities and native API wrappers | Tools.iOSTool |
| **Android Integration** | Xamarin.Android utilities and native API wrappers | Tools.AndroidTool |
| **macOS Integration** | Xamarin.macOS utilities and native API wrappers | Tools.macOSTool |
| **.NET Framework** | .NET Framework 4.8 specific utilities | Tools.NETFramework |
| **.NET Core / Modern .NET** | .NET 5.0+ specific utilities | Tools.NETCore |

## Domain Relationships

### Dependency Hierarchy

```
System Tier (Sphere10.Framework)
├── Collections, Crypto, Serialization, Networking, etc.
│   ↑ (all depend on core utilities)
│
Processing Tier (Sphere10.Framework.Application)
├── Component Registry, Configuration, Plugins
│   ↑ (depends on System Tier)
│
Data Tier (Sphere10.Framework.Data.*)
├── Database Abstraction, Providers (SQLite, MSSQL, etc.)
│   ↑ (depends on System Tier)
│
Presentation Tier
├── Windows Forms, Web/Blazor, Drawing
│   ↑ (depends on System + Data Tiers)
│
Consensus Tier (Sphere10.Framework.Consensus)
├── Consensus rules, validation primitives
│   ↑ (depends on System + Data + Communications Tiers)
```

### Cross-Cutting Concerns

| Concern | Implemented Via | Tools |
|---------|-----------------|-------|
| **Logging** | System.Logging domain | Tools.Logging |
| **Caching** | System.Cache domain | Tools.Cache |
| **Configuration** | Application.Configuration domain | Tools.Configuration |
| **Dependency Injection** | Application.ComponentRegistry domain | Tools.ComponentRegistry |
| **Persistence** | Data.ObjectSpaces domain | Tools.ObjectSpace |
| **Cryptography** | System.Cryptography domain | Tools.Crypto |

---

## When to Use Each Domain

### Building a Desktop Application (Windows Forms)

1. **System Tier**: Collections, Serialization, Logging, Threading
2. **Processing Tier**: Component Registry, Configuration
3. **Data Tier**: Database Abstraction + Provider (SQLite/MSSQL)
4. **Presentation Tier**: Windows Forms + Windows (for registry/services)

### Building a Web Application (ASP.NET Core + Blazor)

1. **System Tier**: All core utilities as needed
2. **Processing Tier**: Component Registry, Configuration, Plugins
3. **Data Tier**: Database Abstraction + Provider
4. **Presentation Tier**: Web/Blazor, Drawing (for charts/graphics)

### Building a Blockchain DApp

1. **System Tier**: All core utilities (Collections, Crypto, Networking, Serialization)
2. **Processing Tier**: Component Registry, Configuration, Plugins
3. **Data Tier**: Database Abstraction, ObjectSpaces
4. **Presentation Tier**: Blazor components, Drawing
5. **DApp Tier**: Blocks, Wallets, Consensus, Node

### Building a Mobile App (iOS/Android)

1. **System Tier**: Core utilities (Collections, Crypto, Serialization)
2. **Processing Tier**: Component Registry, Configuration
3. **Data Tier**: Database Abstraction + Provider (SQLite)
4. **Platform Tier**: iOS/Android specific tools
5. **Communications**: Networking for remote APIs

---

## Related Documentation

- [Sphere10 Framework Architecture](sphere10-framework.md) — Complete architecture overview
- [Tools Reference](../tools-reference.md) — Complete Tools.* namespace catalog
- [3-Tier Architecture](../guidelines/3-tier-architecture.md) — Architecture pattern
- [Code Styling Guidelines](../guidelines/code-styling.md) — Coding conventions

