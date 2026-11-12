# 🚀 Getting Started with Sphere10 Framework

Welcome! This guide gets you oriented with Sphere10 Framework quickly.

---

## What is Sphere10 Framework?

**Sphere10 Framework** is a .NET 10 framework providing:

- **Collection types** — Lists, sets, dictionaries with advanced features (stream-backed, paged, merkle-aware)
- **Advanced serialization** — Binary, JSON, XML with polymorphism and versioning support
- **Data access abstraction** — Multi-database support (SQLite, SQL Server, MySQL, PostgreSQL, Oracle, Firebird and NHibernate)
- **Cryptography** — Hashing, signatures, key derivation, post-quantum algorithms
- **Networking** — TCP, UDP, WebSockets, JSON-RPC frameworks
- **Cross-platform** — .NET 10 libraries with platform-specific adapters
- **Tools.* utilities** — Global IntelliSense-discoverable helpers
- **Testing** — NUnit test suites and NUnit utilities

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

See the [main README](../README.md#nuget-packages) for the complete list of available packages.

---

## Quick Start

### 1. Explore the Codebase

**Project Structure**:
```
src/                              # 35 framework projects
├── Sphere10.Framework/           # Core library
├── Sphere10.Framework.Application/  # App lifecycle, DI, settings, shared presentation
├── Sphere10.Framework.Data/      # Database abstraction
├── Sphere10.Framework.Data.Sqlite/  # SQLite provider
├── Sphere10.Framework.Data.MSSQL/   # SQL Server provider
├── Sphere10.Framework.Communications/  # Networking & RPC
├── Sphere10.Framework.CryptoEx/  # Advanced cryptography
├── Sphere10.Framework.Windows/   # Windows integration
├── Sphere10.Framework.Web/             # ASP.NET-independent web utilities
├── Sphere10.Framework.Web.AspNetCore/  # Shared ASP.NET Core integration
├── Sphere10.Framework.Web.AspNetCore.MVC/     # MVC helpers and forms
├── Sphere10.Framework.Web.AspNetCore.Blazor/  # Razor components and application shells
├── Sphere10.Framework.Drawing/   # Graphics utilities
└── ... (more platforms)

tests/                            # NUnit unit & integration tests
docs/                             # This documentation
```

### 2. Learn the Tools.* Namespace

The **Tools namespace** is your gateway to all utilities:

```csharp
using Sphere10.Framework;

// String operations (Tools.Text, plus string extension methods)
string identifier = Tools.Text.ToCasing(TextCasing.PascalCase, "order line item"); // "OrderLineItem"
string token = Tools.Text.GenerateRandomString(16);
string compact = "a b\tc".RemoveWhitespace();                                     // "abc"
string preview = "A long description of the product".Truncate(10);                // "A long des…"

// Collection operations
int[] squares = Tools.Collection.GenerateArray(5, i => i * i);                     // 0, 1, 4, 9, 16
var batchSizes = Tools.Collection.Partition(100, 30).ToArray();                    // 30, 30, 30, 10

// Cryptographic operations (Hashers with a CHF value, random bytes from Tools.Crypto)
byte[] hash = Hashers.Hash(CHF.SHA2_256, data);
byte[] salt = Tools.Crypto.GenerateCryptographicallyRandomBytes(32);

// File I/O
string tempFile = Tools.FileSystem.GenerateTempFilename(".txt");

// Database operations (Sphere10.Framework.Data.Sqlite)
var dac = Tools.Sqlite.Open(":memory:");

// JSON (Sphere10.Framework.Data)
string json = Tools.Json.WriteToString(settings);
var copy = Tools.Json.ReadFromString<AppSettings>(json);
```

`Tools.Text`, `Tools.Collection`, `Tools.Crypto` and `Tools.FileSystem` are in the core package. `Tools.Sqlite` and `Tools.Json` come with the packages named in the comments.

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
| Shared application, plugins and lifecycle | [Sphere10.Framework.Application](../src/Sphere10.Framework.Application/README.md#shared-ui-framework) |
| Blazor application and component demos | [Blazor guide](../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) / [Blazor tester](../utils/Sphere10.Framework.Utils.BlazorTester/README.md) |
| Web (ASP.NET Core) | [Sphere10.Framework.Web.AspNetCore](../src/Sphere10.Framework.Web.AspNetCore/README.md) |
| Graphics & Drawing | [Sphere10.Framework.Drawing](../src/Sphere10.Framework.Drawing/README.md) |
| Legacy Xamarin mobile source (not built) | [Sphere10.Framework.iOS](../src/Sphere10.Framework.iOS/README.md) / [Sphere10.Framework.Android](../src/Sphere10.Framework.Android/README.md) |

---

## Key Concepts

### Application runtime and lifecycle

WinForms and Blazor use `IApplication` as their runtime root. It exposes live `Blocks`, successfully loaded startup `LoadedPlugins`, application commands, `ScreenHost`, `Logger` and `Disposables`. Blocks own menus and screen definitions; the host tracks selected blocks and retained screen instances. Use command objects and their parent links for execution. Route identifiers belong at the Blazor URL boundary.

Plugins declare `WithName(...)` before registering initializers and finalizers. The application runs those tasks through `Initialize` / `InitializeAsync` and `Finalize` / `FinalizeAsync`; `Exit` / `ExitAsync` coordinates confirmation and screen/block permissions before shutdown. Task inventories are protected implementation details. Choose the existing `Sync...Base` or `Async...Base` for a task's implementation direction.

Run the [WinForms tester](../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) to see plugin tasks alongside process tasks in startup and shutdown splashes, with the main form closing last. The [Blazor tester](../utils/Sphere10.Framework.Utils.BlazorTester/README.md#plugin-initialization-and-shutdown) demonstrates tasks once per interactive circuit and cleanup on exit or circuit closure. Its prerender scope does not execute application tasks.

### Tools.* Namespace

All framework projects extend the global `Tools` namespace:

```csharp
using Sphere10.Framework;

// Core framework tools
Tools.Text       // String manipulation
Tools.Crypto     // Random bytes, key derivation helpers
Tools.Collection // Collection generation and partitioning
Tools.FileSystem // File I/O
Tools.Mail       // E-mail address validation

// Database tools
Tools.Sqlite     // SQLite databases (Sphere10.Framework.Data.Sqlite)
Tools.MSSQL      // SQL Server databases (Sphere10.Framework.Data.MSSQL)
Tools.Json       // JSON files and strings (Sphere10.Framework.Data)

// Platform tools
Tools.Windows    // Windows registry, services, processes and more (Sphere10.Framework.Windows)
Tools.Drawing    // Graphics operations (Sphere10.Framework.Drawing)
```

Hashing lives in `Hashers`, which takes a `CHF` value such as `CHF.SHA2_256`.

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
using Sphere10.Framework.Data;

// SQLite. Each connection to ":memory:" has its own database, so keep the work inside one scope.
var dac = Tools.Sqlite.Open(":memory:");
using (var scope = dac.EnterScope()) {
    dac.ExecuteNonQuery("CREATE TABLE Users (ID INTEGER PRIMARY KEY, Name TEXT NOT NULL)");

    // Insert
    dac.Insert("Users", new[] {
        new ColumnValue("ID", 1),
        new ColumnValue("Name", "Alice")
    });

    // Query by value. The SQL builder quotes and escapes the values.
    var matches = dac.Select("Users", columnMatches: new[] { new ColumnValue("Name", "Alice") });
    Console.WriteLine(matches.Rows.Count);                                  // 1
    Console.WriteLine(dac.ExecuteScalar<long>("SELECT COUNT(*) FROM Users")); // 1

    // Transaction on the scope
    scope.BeginTransaction();
    dac.Update("Users",
        new[] { new ColumnValue("Name", "Alicia") },   // set
        new[] { new ColumnValue("ID", 1) });           // where
    scope.Commit();
}
```

`EnterScope()` opens the connection, and nested scopes share it. Without `Commit()`, a transaction begun on the scope rolls back when the scope ends. The [Sqlite README](../src/Sphere10.Framework.Data.Sqlite/README.md) covers bound ADO.NET parameters, file databases and journal modes.

### Hashing Data

```csharp
using Sphere10.Framework;

// SHA-256
byte[] hash = Hashers.Hash(CHF.SHA2_256, data);

// BLAKE2b-256
byte[] blake = Hashers.Hash(CHF.Blake2b_256, data);

// Hex string for display or storage
string hex = hash.ToHexString(); // "0x…"
```

The [CryptoEx README](../src/Sphere10.Framework.CryptoEx/README.md) adds signatures, key derivation and the hash functions that package registers.

### String Manipulation

```csharp
using Sphere10.Framework;

// Remove whitespace (string extension)
string clean = text.RemoveWhitespace();

// Generate random string
string random = Tools.Text.GenerateRandomString(32);

// Validate email
bool valid = Tools.Mail.IsValidEmail(email);

// Shorten for display (string extension)
string shortText = text.Truncate(40);
```

### File Operations

```csharp
using Sphere10.Framework;

// Generate temp filename
string temp = Tools.FileSystem.GenerateTempFilename();

// Create, write and append bytes
Tools.FileSystem.CreateBlankFile(path, createDirectories: true);
Tools.FileSystem.WriteAllBytes(path, header);
Tools.FileSystem.AppendAllBytes(path, body);

// Text files use the BCL
File.WriteAllText(textPath, content);
string loaded = File.ReadAllText(textPath);

// Delete when done
Tools.FileSystem.DeleteFile(path);
```

---

## Documentation Map

Start with the project documentation for the component you use, then select the relevant architecture and engineering practices below.

| Document | Purpose |
|----------|---------|
| [README.md](README.md) | Documentation home |
| [Tools Reference](tools-reference.md) | Complete Tools.* namespace catalog |
| [Real-World Examples](real-world-usage-examples.md) | Practical patterns from tests |
| [Framework Architecture](architecture/sphere10-framework.md) | Design and architecture |
| [Framework Domains](architecture/domains.md) | Catalog of 40+ domains |
| [3-Tier Architecture](guidelines/3-tier-architecture.md) | Architectural patterns |
| [Code Styling](guidelines/code-styling.md) | Coding standards |
| [Patterns and Practices](guidelines/patterns-and-practices.md) | Task index for focused pattern guides |

For structural changes, start with [type hierarchies](guidelines/patterns/type-hierarchies.md), [sync/async operations](guidelines/patterns/sync-async.md), [interface bridges](guidelines/patterns/interface-bridges.md) or [application ownership](guidelines/patterns/application-ownership.md), according to the change. Use the task index for the other pattern topics.

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
