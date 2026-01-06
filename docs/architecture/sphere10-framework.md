# Sphere10 Framework: Architecture & Design

## Table of Contents

1. [Introduction](#introduction)
2. [Design Philosophy](#design-philosophy)
3. [Architectural Layers](#architectural-layers)
4. [The Tools.* Discovery Pattern](#the-tools-discovery-pattern)
5. [Core Subsystems](#core-subsystems)
6. [Data Architecture](#data-architecture)
7. [Platform Strategy](#platform-strategy)
8. [Extension Points](#extension-points)
9. [Related Documentation](#related-documentation)

---

## Introduction

Sphere10 Framework is a **mature, production-ready .NET 8.0+ framework** designed for building high-performance applications across desktop, mobile, and web platforms. It provides low-level primitives and high-level abstractions that work together to solve common development challenges.

### What Makes It Different

Unlike application frameworks that impose structure on your code, Sphere10 Framework provides **composable building blocks**:

- **50+ specialized collection types** — from simple extended lists to stream-mapped transactional collections with merkle-tree integrity
- **Advanced serialization** — polymorphic, versioned, constant-size encoding with reference tracking
- **ACID transactional primitives** — scopes, streams, and collections with commit/rollback semantics
- **Multi-database abstraction** — unified API across SQLite, SQL Server, Firebird, and NHibernate
- **Cryptographic toolkit** — hashing, signatures, key derivation, and post-quantum algorithms
- **Cross-platform support** — Windows, macOS, Linux, iOS, Android from a single codebase

### Target Use Cases

- **Custom storage engines** — when ORM overhead is unacceptable
- **High-volume data processing** — streaming, paging, and memory-efficient operations
- **Distributed systems** — merkle proofs, integrity verification, P2P networking
- **Enterprise applications** — multi-database support, transactional consistency
- **Cross-platform applications** — desktop, mobile, and web from shared code

---

## Design Philosophy

### Principle 1: Composability Over Configuration

Components are designed to work independently or together. There's no "framework lock-in"—use what you need.

```csharp
// Use just the collections
var pagedList = new PagedList<Customer>(pageSize: 100);

// Or combine with transactional semantics
using var scope = new TransactionalScope();
pagedList.Add(customer);
scope.Commit();
```

### Principle 2: Explicit Over Magic

No hidden behavior. Transactions don't auto-commit. Connections aren't silently pooled. You control the lifecycle.

```csharp
// Explicit transaction boundaries
using (var scope = dac.BeginTransactionScope()) {
    dac.Update("Orders", values, "WHERE ID = @id");
    scope.Commit();  // Nothing happens without this
}
```

### Principle 3: Performance-Conscious Defaults

Memory allocation is minimized. Streaming is preferred over buffering. Large datasets use paging.

```csharp
// Stream-mapped collection — data lives on disk, not RAM
var streamMapped = new StreamMappedList<Record>(fileStream, serializer);
streamMapped.Add(record);  // Written directly to stream
```

### Principle 4: Type Safety and Correctness

Generic constraints, nullable annotations, and compile-time checks prevent runtime errors.

```csharp
// Generic operators preserve type safety
T result = Tools.Operator.Add<T>(a, b);  // Compile-time type checking
```

### Principle 5: Discoverable APIs

The Tools.* namespace provides IntelliSense-driven discovery of all utilities.

---

## Architectural Layers

The framework follows a **4-tier architectural model** where each layer has clear responsibilities and minimal coupling to layers above.

```
┌─────────────────────────────────────────────────────────────┐
│  PRESENTATION TIER                                          │
│  Windows Forms, ASP.NET Core, Drawing                       │
│  UI components, data binding, web middleware                │
├─────────────────────────────────────────────────────────────┤
│  DATA TIER                                                  │
│  Sphere10.Framework.Data + Provider Libraries               │
│  Database abstraction, query building, transactions         │
├─────────────────────────────────────────────────────────────┤
│  APPLICATION TIER                                           │
│  Sphere10.Framework.Application                             │
│  DI integration, settings, lifecycle, CLI                   │
├─────────────────────────────────────────────────────────────┤
│  SYSTEM TIER                                                │
│  Sphere10.Framework (Core)                                  │
│  Collections, serialization, crypto, streams, threading     │
└─────────────────────────────────────────────────────────────┘
```

### System Tier (Sphere10.Framework)

The foundation layer with **zero external dependencies** for core functionality. Provides:

- **Collections**: Extended lists, stream-mapped structures, paged collections, merkle trees
- **Serialization**: Binary, JSON, XML with polymorphism and versioning
- **Cryptography**: Hashing (via HashLib4CSharp), signatures, key derivation
- **Streams**: Bit streams, bounded streams, clustered storage
- **Threading**: Synchronization primitives, producer-consumer patterns
- **Extensions**: 200+ extension methods for .NET types

### Application Tier (Sphere10.Framework.Application)

Application lifecycle and configuration:

- **Dependency Injection**: Integration with Microsoft.Extensions.DependencyInjection
- **Settings Management**: Type-safe settings with persistence
- **Command-Line Parsing**: Attribute-based CLI with validation
- **Lifecycle Hooks**: Startup, configuration, and shutdown events

### Data Tier (Sphere10.Framework.Data.*)

Database abstraction with provider-specific implementations:

- **Unified API**: Same code works across SQLite, SQL Server, Firebird
- **Transaction Scopes**: ACID boundaries with auto-rollback
- **Query Building**: Fluent, type-safe query construction
- **Connection Management**: Automatic pooling and reuse

### Presentation Tier

Platform-specific UI frameworks:

- **Windows Forms**: Data binding, validation, component library
- **ASP.NET Core**: Middleware, filters, routing integration
- **Drawing**: Cross-platform graphics and image manipulation

---

## The Tools.* Discovery Pattern

A defining feature is the **Tools namespace**—a global collection of static utility methods organized by domain.

### How It Works

Instead of searching for helper classes, type `Tools.` and IntelliSense shows all available operations:

```csharp
using Sphere10.Framework;

// Cryptography
byte[] hash = Tools.Crypto.SHA256(data);
bool valid = Tools.Crypto.VerifySignature(data, sig, key);

// Text processing  
string clean = Tools.Text.RemoveWhitespace(input);
string truncated = Tools.Text.Truncate(longString, 100);

// File operations
string temp = Tools.FileSystem.GenerateTempFilename();
Tools.FileSystem.WriteAllText(path, content);

// Database access
var dac = Tools.Sqlite.Open("data.db");
var results = Tools.MSSQL.ExecuteQuery(connStr, sql);
```

### Extensibility

Each framework project contributes its own Tools classes:

| Project | Tools Class | Purpose |
|---------|-------------|---------|
| Sphere10.Framework | Tools.Crypto, Tools.Text, Tools.Collection, etc. | Core utilities |
| Sphere10.Framework.Windows | Tools.WinTool | Registry, services, events |
| Sphere10.Framework.Data.Sqlite | Tools.Sqlite | SQLite operations |
| Sphere10.Framework.Web.AspNetCore | Tools.HtmlTool, Tools.XmlTool | Web utilities |
| Sphere10.Framework.Drawing | Tools.Drawing | Graphics operations |

### Design Rationale

1. **Discovery**: One entry point for all utilities
2. **Consistency**: Similar operations have similar signatures
3. **Organization**: Grouped by domain, not by implementation
4. **Extensibility**: New projects add new Tools.* classes

---

## Core Subsystems

### Collections Subsystem

The most extensive subsystem with 50+ collection types:

| Category | Types | Use Case |
|----------|-------|----------|
| **Extended Lists** | `ExtendedList<T>`, `PreAllocatedList<T>` | Enhanced List<T> with batch operations |
| **Stream-Mapped** | `StreamMappedList<T>`, `StreamMappedDictionary<K,V>` | Persistence to streams without loading all data |
| **Paged** | `PagedList<T>`, `PagedDictionary<K,V>` | Handle large datasets in memory-bounded pages |
| **Transactional** | `TransactionalList<T>`, `TransactionalDictionary<K,V>` | ACID semantics with commit/rollback |
| **Merkle** | `MerkleTree`, `FlatMerkleTree`, `LongMerkleTree` | Integrity proofs and change detection |
| **Observable** | `ObservableList<T>`, `ObservableDictionary<K,V>` | Change notifications for data binding |
| **Recyclable** | `RecyclableList<T>` | Object pooling for high-throughput scenarios |

### Serialization Subsystem

Advanced serialization beyond standard .NET:

- **Polymorphic**: Serialize interface references with type resolution
- **Versioned**: Handle schema evolution gracefully
- **Constant-Size**: Fixed-width encoding for random access
- **Reference Tracking**: Circular reference support
- **Factory Pattern**: Custom object instantiation

### Cryptography Subsystem

Built on HashLib4CSharp with extensions:

- **Hashing**: MD5, SHA family, BLAKE2, CRC, checksums
- **Signatures**: ECDSA, Ed25519, Schnorr
- **Key Derivation**: PBKDF2, scrypt, Argon2
- **Post-Quantum**: AMS, WAMS, WOTS# schemes

### Streams Subsystem

Beyond standard .NET streams:

- **BitStream**: Bit-level read/write operations
- **BoundedStream**: Length-limited views
- **BlockingStream**: Producer-consumer patterns
- **ClusteredStream**: Multi-stream storage with dynamic allocation
- **TransactionalStream**: ACID stream operations

---

## Data Architecture

### Database Abstraction

The Data Access Context (DAC) wraps `IDbConnection` with enhanced functionality:

```csharp
// Same code works across all providers
IDAC dac = Tools.Sqlite.Open("local.db");
// or: IDAC dac = Tools.MSSQL.Open(connectionString);
// or: IDAC dac = Tools.Firebird.Open(connectionString);

// Parameterized queries (SQL injection safe)
var count = dac.ExecuteScalar<int>(
    "SELECT COUNT(*) FROM Users WHERE Status = @status",
    new ColumnValue("@status", "active"));
```

### Transaction Management

Explicit transaction scopes with automatic rollback:

```csharp
using (var scope = dac.BeginTransactionScope()) {
    dac.Insert("Orders", orderValues);
    dac.Insert("OrderItems", itemValues);
    
    if (validationFailed)
        return;  // Auto-rollback on scope disposal
    
    scope.Commit();  // Explicit commit required
}
```

### Provider Architecture

```
IDAC (Interface)
├── SqliteDAC
├── MSSqlDAC  
├── FirebirdDAC
└── NHibernateDAC
```

Each provider implements the interface while exposing provider-specific features.

---

## Platform Strategy

### Cross-Platform Core

`Sphere10.Framework` targets .NET 8.0+ and .NET Standard 2.0, running on:

- Windows (x64, x86, ARM64)
- Linux (x64, ARM64)
- macOS (x64, Apple Silicon)

### Platform-Specific Libraries

| Library | Platform | Purpose |
|---------|----------|---------|
| Sphere10.Framework.Windows | Windows | Registry, services, events, LevelDB |
| Sphere10.Framework.Windows.Forms | Windows | Desktop UI components |
| Sphere10.Framework.iOS | iOS | Xamarin.iOS integration |
| Sphere10.Framework.Android | Android | Xamarin.Android integration |
| Sphere10.Framework.macOS | macOS | Xamarin.macOS integration |

### Conditional Compilation

Platform-specific code is isolated in dedicated assemblies—core code remains platform-agnostic.

---

## Extension Points

### Custom Tool Classes

Add your own utilities to the Tools namespace:

```csharp
// In your project
namespace Tools;

public static class MyTool {
    public static void CustomOperation() { /* ... */ }
}

// Usage: Tools.MyTool.CustomOperation()
```

### Custom Serializers

Implement `IItemSerializer<T>` for custom types:

```csharp
public class CustomerSerializer : IItemSerializer<Customer> {
    public void Serialize(Customer item, EndianBinaryWriter writer) {
        writer.Write(item.Id);
        writer.Write(item.Name);
    }
    
    public Customer Deserialize(EndianBinaryReader reader) {
        return new Customer {
            Id = reader.ReadInt32(),
            Name = reader.ReadString()
        };
    }
}
```

### Custom DAC Providers

Extend the data abstraction for new databases by implementing `IDAC`.

---

## Related Documentation

- [Framework Domains](domains.md) — Detailed breakdown of all 40+ domains
- [Tools Reference](../tools-reference.md) — Complete Tools.* namespace catalog
- [Getting Started](../start-here.md) — Quick start guide
- [3-Tier Architecture](../guidelines/3-tier-architecture.md) — Architectural patterns
- [Code Styling Guidelines](../guidelines/code-styling.md) — Coding conventions
