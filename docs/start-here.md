# 🚀 Getting Started with Sphere10 Framework

Welcome! This guide gets you oriented with Sphere10 Framework quickly.

---

## What is Sphere10 Framework?

**Sphere10 Framework** is a comprehensive, production-ready .NET 8.0 framework providing:

- **50+ collection types** — Lists, sets, dictionaries with advanced features (stream-backed, paged, merkle-aware)
- **Advanced serialization** — Binary, JSON, XML with polymorphism and versioning support
- **Data access abstraction** — Multi-database support (SQL Server, SQLite, Firebird, NHibernate)
- **Cryptography** — Hashing, signatures, key derivation, post-quantum algorithms
- **Networking** — TCP, UDP, WebSockets, JSON-RPC frameworks
- **Cross-platform** — Windows, macOS, iOS, Android, .NET 8.0+
- **Tools.* utilities** — Global IntelliSense-discoverable helpers
- **Comprehensive testing** — 2000+ tests, NUnit utilities

---

## Installation

Install via NuGet:

```bash
# Core framework
dotnet add package Sphere10.Framework

# Additional packages as needed
dotnet add package Sphere10.Framework.Data.Sqlite
dotnet add package Sphere10.Framework.CryptoEx
dotnet add package Sphere10.Framework.Communications
```

See the [main README](../README.md#package-installation) for the complete list of available packages.

---

## Quick Start

### 1. Explore the Codebase

**Project Structure**:
```
src/                              # 45+ framework projects
├── Sphere10.Framework/           # Core library
├── Sphere10.Framework.Application/  # App lifecycle, DI, settings
├── Sphere10.Framework.Data/      # Database abstraction
├── Sphere10.Framework.Data.Sqlite/  # SQLite provider
├── Sphere10.Framework.Data.MSSQL/   # SQL Server provider
├── Sphere10.Framework.Communications/  # Networking & RPC
├── Sphere10.Framework.CryptoEx/  # Advanced cryptography
├── Sphere10.Framework.Windows/   # Windows integration
├── Sphere10.Framework.Web.AspNetCore/  # Web framework
├── Sphere10.Framework.Drawing/   # Graphics utilities
└── ... (more platforms)

tests/                            # 2000+ unit & integration tests
docs/                             # This documentation
```

### 2. Learn the Tools.* Namespace

The **Tools namespace** is your gateway to all utilities:

```csharp
using Sphere10.Framework;

// String operations
string sanitized = Tools.Text.RemoveWhitespace(input);
string truncated = Tools.Text.Truncate(text, 100);

// Collection operations
var filtered = Tools.Collection.Where(items, predicate);
var flattened = Tools.Collection.Flatten(nested);

// Cryptographic operations
byte[] hash = Tools.Crypto.SHA256(data);

// File I/O
string tempFile = Tools.FileSystem.GenerateTempFilename();

// Database operations
var dac = Tools.Sqlite.Open(":memory:");

// JSON/XML
string json = Tools.Json.Serialize(obj);
```

**Complete Reference**: See [Tools Reference](tools-reference.md)

### 3. Pick Your Use Case

| Use Case | Documentation |
|----------|---------------|
| Collections & Data Structures | [Sphere10.Framework](../src/Sphere10.Framework/README.md) |
| Database Access | [Sphere10.Framework.Data](../src/Sphere10.Framework.Data/README.md) |
| Cryptography | [Sphere10.Framework.CryptoEx](../src/Sphere10.Framework.CryptoEx/README.md) |
| Networking & RPC | [Sphere10.Framework.Communications](../src/Sphere10.Framework.Communications/README.md) |
| Windows Desktop | [Sphere10.Framework.Windows](../src/Sphere10.Framework.Windows/README.md) |
| Windows Forms UI | [Sphere10.Framework.Windows.Forms](../src/Sphere10.Framework.Windows.Forms/README.md) |
| Web (ASP.NET Core) | [Sphere10.Framework.Web.AspNetCore](../src/Sphere10.Framework.Web.AspNetCore/README.md) |
| Graphics & Drawing | [Sphere10.Framework.Drawing](../src/Sphere10.Framework.Drawing/README.md) |
| iOS Development | [Sphere10.Framework.iOS](../src/Sphere10.Framework.iOS/README.md) |
| Android Development | [Sphere10.Framework.Android](../src/Sphere10.Framework.Android/README.md) |

---

## Key Concepts

### Tools.* Namespace

All framework projects extend the global `Tools` namespace:

```csharp
using Sphere10.Framework;

// Core framework tools
Tools.Text       // String manipulation
Tools.Crypto     // Cryptography
Tools.Collection // Collection operations
Tools.FileSystem // File I/O

// Database tools  
Tools.Sqlite     // SQLite operations
Tools.MSSQL      // SQL Server operations

// Platform tools
Tools.WinTool    // Windows registry, services
Tools.Drawing    // Graphics operations
```

### Layered Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  PRESENTATION — Windows Forms, ASP.NET Core, Drawing        │
├─────────────────────────────────────────────────────────────┤
│  DATA — Database abstraction, SQLite, MSSQL, Firebird       │
├─────────────────────────────────────────────────────────────┤
│  APPLICATION — DI, settings, lifecycle, CLI                 │
├─────────────────────────────────────────────────────────────┤
│  CORE — Collections, serialization, crypto, streams         │
└─────────────────────────────────────────────────────────────┘
```

### Design Principles

- **Composability** — Small, focused components that work together
- **Explicit Control** — No magic defaults; you control the lifecycle
- **Performance** — Memory-efficient, zero-allocation where possible
- **Extensibility** — Interface-based design for customization
- **Correctness** — ACID transactions, cryptographic correctness

---

## Common Tasks

### Database Access

```csharp
using Sphere10.Framework;

// SQLite
var dac = Tools.Sqlite.Open(":memory:");

// Insert
dac.Insert("Users", new[] {
    new ColumnValue("ID", 1),
    new ColumnValue("Name", "Alice")
});

// Query with parameters (SQL injection safe)
var count = dac.ExecuteScalar<int>(
    "SELECT COUNT(*) FROM Users WHERE Name = @name",
    new ColumnValue("@name", "Alice")
);

// Transaction
using (var scope = dac.BeginTransactionScope()) {
    dac.Update("Users", values, "WHERE ID = 1");
    scope.Commit();
}
```

### Hashing Data

```csharp
using Sphere10.Framework;

// SHA256
byte[] hash = Tools.Crypto.SHA256(data);

// BLAKE2
byte[] blake = Tools.Crypto.BLAKE2B(data);
```

### String Manipulation

```csharp
using Sphere10.Framework;

// Remove whitespace
string clean = Tools.Text.RemoveWhitespace(text);

// Generate random string
string random = Tools.Text.GenerateRandomString(32);

// Validate email
bool valid = Tools.Text.IsValidEmail(email);
```

### File Operations

```csharp
using Sphere10.Framework;

// Generate temp filename
string temp = Tools.FileSystem.GenerateTempFilename();

// Read/write files
Tools.FileSystem.WriteAllText(path, content);
string content = Tools.FileSystem.ReadAllText(path);
```

---

## Documentation Map

| Document | Purpose |
|----------|---------|
| [README.md](README.md) | Documentation home |
| [Tools Reference](tools-reference.md) | Complete Tools.* namespace catalog |
| [Real-World Examples](real-world-usage-examples.md) | Practical patterns from tests |
| [Framework Architecture](architecture/sphere10-framework.md) | Design and architecture |
| [Framework Domains](architecture/domains.md) | Catalog of 40+ domains |
| [3-Tier Architecture](guidelines/3-tier-architecture.md) | Architectural patterns |
| [Code Styling](guidelines/code-styling.md) | Coding standards |

---

## Next Steps

1. **Explore project READMEs** in `src/` for component-specific guidance
2. **Review [Tools Reference](tools-reference.md)** for utility catalog
3. **Study [Real-World Examples](real-world-usage-examples.md)** for practical patterns
4. **Read [Framework Architecture](architecture/sphere10-framework.md)** to understand design decisions
5. **Run the test suite** to see examples in action

---

## Quick Links

- [Main README](../README.md) — Project overview
- [Documentation Home](README.md) — Full documentation index
- [Tools Reference](tools-reference.md) — Tools.* namespace guide