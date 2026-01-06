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
- **Cross-platform** — Windows, macOS, iOS, Android, .NET Core
- **50+ utility tools** — Tools.* namespace for global discovery
- **Complete testing** — 2000+ tests, NUnit utilities, test fixtures

---

## Quick Start

### 1. Explore the Codebase

**Project Structure**:
```
src/                          # 45+ framework projects
├── Sphere10.Framework/       # Core library
├── Sphere10.Framework.Data/  # Database access
├── Sphere10.Framework.Communications/  # Networking
├── Sphere10.Framework.CryptoEx/        # Cryptography
├── Sphere10.Framework.Windows/         # Windows integration
├── Sphere10.Framework.Web.AspNetCore/  # Web framework
└── ... (more platforms and utilities)

tests/                        # 2000+ unit & integration tests
blackhole/                    # Blazor UI components
docs/                         # This documentation
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
var connection = Tools.Sqlite.Create(":memory:");
var adapter = Tools.MSSQL.CreateAdapter(connString);

// JSON/XML
string json = Tools.Json.Serialize(obj);
```

**Complete Reference**: See [Tools Reference](tools-reference.md)

### 3. Pick Your Use Case

**Working with Collections & Data?**
- Read: [src/Sphere10.Framework/README.md](../src/Sphere10.Framework/README.md)
- Key topics: Extended lists, Merkle trees, stream-backed collections, serialization

**Need Database Access?**
- Read: [src/Sphere10.Framework.Data/README.md](../src/Sphere10.Framework.Data/README.md)
- Key topics: Multi-database abstraction, transactions, parameterized queries

**Implementing Cryptography?**
- Read: [src/Sphere10.Framework.CryptoEx/README.md](../src/Sphere10.Framework.CryptoEx/README.md)
- Key topics: Hashing, signatures, key derivation, post-quantum algorithms

**Building Networking/RPC?**
- Read: [src/Sphere10.Framework.Communications/README.md](../src/Sphere10.Framework.Communications/README.md)
- Key topics: TCP, UDP, WebSockets, JSON-RPC, attribute-based service definition

**Windows Desktop Development?**
- Read: [src/Sphere10.Framework.Windows/README.md](../src/Sphere10.Framework.Windows/README.md)
- Key topics: Registry, services, security, event logging

**Web Application (ASP.NET)?**
- Read: [src/Sphere10.Framework.Web.AspNetCore/README.md](../src/Sphere10.Framework.Web.AspNetCore/README.md)
- Key topics: Middleware, controllers, HTML utilities, routing

---

## Key Concepts

### Tools.* Namespace

All framework projects extend the global `Tools` namespace:

```csharp
// Core framework tools
Tools.Text, Tools.Crypto, Tools.Collection, Tools.FileSystem

// Database tools
Tools.Data, Tools.Sqlite, Tools.MSSQL, Tools.Firebird

// Platform tools
Tools.WinTool, Tools.Web.Html, Tools.iOSTool

// Discovery pattern: Type Tools. to see all available operations
```

### Layered Architecture

1. **Core Framework** — Utilities, collections, serialization
2. **Data Access** — Multi-database abstraction layer
3. **Networking** — TCP, UDP, WebSockets, RPC
4. **Cryptography** — Hashing, signatures, key derivation
5. **Platform Integration** — Windows, Web, Mobile
6. **Testing** — NUnit utilities, test fixtures

### Design Patterns

- **Composability** — Small, focused abstractions that compose predictably
- **Explicit Control** — Fine-grained configuration over magic defaults
- **Performance** — Batch operations, memory efficiency, zero-allocation variants
- **Extensibility** — Interface-based design for decoration and adaptation
- **Correctness** — Transaction-aware structures, cryptographic correctness

---

## Common Tasks

### Reading a Database

```csharp
using Sphere10.Framework.Data;

// SQLite
var dac = Tools.Sqlite.Open(":memory:");

// SQL Server
var dac = Tools.MSSQL.CreateAdapter(connectionString);

// Execute query
var results = dac.ExecuteQuery("SELECT * FROM Users");

// With parameters (SQL injection safe)
var count = dac.ExecuteScalar<int>(
    "SELECT COUNT(*) FROM Users WHERE Name = @name",
    new ColumnValue("@name", "Alice")
);
```

### Hashing Data

```csharp
using Sphere10.Framework;

// SHA256
byte[] hash = Tools.Crypto.SHA256(data);

// BLAKE2
byte[] hash256 = Tools.Crypto.BLAKE2B(data);

// Verify signature
bool valid = Tools.Crypto.VerifySignature(publicKey, message, signature);
```

### String Manipulation

```csharp
using Sphere10.Framework;

// Remove whitespace
string clean = Tools.Text.RemoveWhitespace(text);

// Generate random string
string random = Tools.Text.GenerateRandomString(32);

// Safe email validation
bool valid = Tools.Text.IsValidEmail(email);

// Parse safely
int parsed = Tools.Parse.ToInt32(input, defaultValue);
```

### File Operations

```csharp
using Sphere10.Framework;

// Generate temp filename
string temp = Tools.FileSystem.GenerateTempFilename();

// Read/write files
Tools.FileSystem.WriteAllText(path, content);
string content = Tools.FileSystem.ReadAllText(path);

// List files
var files = Tools.FileSystem.GetFiles(directory, "*.txt");
```

---

## Documentation Map

| Document | Purpose |
|----------|---------|
| [index.md](index.md) | Documentation home and topic index |
| [Tools Reference](tools-reference.md) | Complete Tools.* namespace catalog |
| [real-world-usage-examples.md](real-world-usage-examples.md) | Practical examples and patterns |
| [architecture/sphere10-framework.md](architecture/sphere10-framework.md) | Framework architecture and design |
| [architecture/domains.md](architecture/domains.md) | Catalog of 30+ framework domains |
| [guidelines/3-tier-architecture.md](guidelines/3-tier-architecture.md) | Architectural patterns |
| [guidelines/code-styling.md](guidelines/code-styling.md) | Coding standards and conventions |

---

## Next Steps

1. **Explore project README files** in `src/` for component-specific guidance
2. **Review [Tools Reference](tools-reference.md)** for utility catalog
3. **Study [real-world-usage-examples.md](real-world-usage-examples.md)** for practical patterns
4. **Read architecture docs** to understand design decisions
5. **Run the test suite** to see examples in action

---

## Resources

- **[README.md](../README.md)** — Project overview and project list
- **[Tools Reference](tools-reference.md)** — Tools.* namespace guide
- **Individual project READMEs** in `src/` — Component-specific docs

---

## Questions?

- Check the project README in `src/` for your component
- Review [Tools Reference](tools-reference.md) for utility patterns
- See [real-world-usage-examples.md](real-world-usage-examples.md) for practical examples
- Read [architecture/sphere10-framework.md](architecture/sphere10-framework.md) for design context

---

**Version**: 3.0.0  
**Framework**: Sphere10 Framework  
**Target**: .NET 8.0  
**License**: MIT NON-AI

