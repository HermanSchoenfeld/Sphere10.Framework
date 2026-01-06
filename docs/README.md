# Sphere10 Framework Documentation

**Copyright © 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved.**

Welcome to the documentation for **Sphere10 Framework** — a comprehensive .NET 8.0 framework for building high-performance applications across desktop, mobile, and web platforms.

---

## 📖 Start Here

| Resource | Description |
|----------|-------------|
| [**Getting Started**](start-here.md) | Quick orientation for new developers |
| [**Tools Reference**](tools-reference.md) | Complete catalog of `Tools.*` namespace utilities |
| [**Real-World Examples**](real-world-usage-examples.md) | Practical patterns from the test suite |

---

## 🏗️ Architecture & Design

| Document | Description |
|----------|-------------|
| [**Framework Architecture**](architecture/sphere10-framework.md) | Design philosophy, layers, and core subsystems |
| [**Framework Domains**](architecture/domains.md) | Catalog of 40+ specialized domains |
| [**3-Tier Architecture**](guidelines/3-tier-architecture.md) | Architectural patterns and principles |
| [**Code Styling**](guidelines/code-styling.md) | Coding standards and conventions |

---

## 📦 Project Documentation

### Core Framework

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework** | Collections, serialization, cryptography, streams, extensions | [View](../src/Sphere10.Framework/README.md) |
| **Sphere10.Framework.Application** | DI integration, settings, lifecycle, CLI | [View](../src/Sphere10.Framework.Application/README.md) |
| **Sphere10.HashLib4CSharp** | Hashing algorithms (MD5, SHA, BLAKE2, CRC) | [View](../src/Sphere10.HashLib4CSharp/README.md) |

### Data Access

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.Data** | Database abstraction, transactions, query building | [View](../src/Sphere10.Framework.Data/README.md) |
| **Sphere10.Framework.Data.Sqlite** | SQLite provider implementation | [View](../src/Sphere10.Framework.Data.Sqlite/README.md) |
| **Sphere10.Framework.Data.MSSQL** | SQL Server provider implementation | [View](../src/Sphere10.Framework.Data.MSSQL/README.md) |
| **Sphere10.Framework.Data.Firebird** | Firebird provider implementation | [View](../src/Sphere10.Framework.Data.Firebird/README.md) |
| **Sphere10.Framework.Data.NHibernate** | NHibernate ORM integration | [View](../src/Sphere10.Framework.Data.NHibernate/README.md) |

### Networking & Communications

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.Communications** | TCP, UDP, WebSockets, JSON-RPC | [View](../src/Sphere10.Framework.Communications/README.md) |

### Cryptography

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.CryptoEx** | ECDSA, post-quantum algorithms, signatures | [View](../src/Sphere10.Framework.CryptoEx/README.md) |

### Desktop & Windows

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.Windows** | Registry, services, event logging | [View](../src/Sphere10.Framework.Windows/README.md) |
| **Sphere10.Framework.Windows.Forms** | UI components, data binding | [View](../src/Sphere10.Framework.Windows.Forms/README.md) |
| **Sphere10.Framework.Windows.LevelDB** | LevelDB key-value storage | [View](../src/Sphere10.Framework.Windows.LevelDB/README.md) |

### Web & Cross-Platform

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.Web.AspNetCore** | Middleware, filters, HTML utilities | [View](../src/Sphere10.Framework.Web.AspNetCore/README.md) |
| **Sphere10.Framework.Drawing** | Graphics and image manipulation | [View](../src/Sphere10.Framework.Drawing/README.md) |
| **Sphere10.Framework.iOS** | Xamarin.iOS integration | [View](../src/Sphere10.Framework.iOS/README.md) |
| **Sphere10.Framework.Android** | Xamarin.Android integration | [View](../src/Sphere10.Framework.Android/README.md) |
| **Sphere10.Framework.macOS** | Xamarin.macOS integration | [View](../src/Sphere10.Framework.macOS/README.md) |

### Testing

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.NUnit** | NUnit utilities and test helpers | [View](../src/Sphere10.Framework.NUnit/README.md) |

---

## 🎯 By Use Case

| Goal | Start Here |
|------|------------|
| New to the framework | [Getting Started](start-here.md) |
| Using Tools.* utilities | [Tools Reference](tools-reference.md) |
| Working with collections | [Sphere10.Framework](../src/Sphere10.Framework/README.md) |
| Database access | [Sphere10.Framework.Data](../src/Sphere10.Framework.Data/README.md) |
| Cryptography | [Sphere10.Framework.CryptoEx](../src/Sphere10.Framework.CryptoEx/README.md) |
| Networking & RPC | [Sphere10.Framework.Communications](../src/Sphere10.Framework.Communications/README.md) |
| Windows desktop | [Sphere10.Framework.Windows.Forms](../src/Sphere10.Framework.Windows.Forms/README.md) |
| ASP.NET Core web | [Sphere10.Framework.Web.AspNetCore](../src/Sphere10.Framework.Web.AspNetCore/README.md) |
| Mobile development | [iOS](../src/Sphere10.Framework.iOS/README.md) / [Android](../src/Sphere10.Framework.Android/README.md) |

---

## 📂 Documentation Structure

```
docs/
├── README.md                 ← You are here
├── start-here.md             — Quick start guide
├── tools-reference.md        — Tools.* namespace catalog
├── real-world-usage-examples.md — Practical examples
├── architecture/
│   ├── sphere10-framework.md — Framework architecture
│   └── domains.md            — Domain catalog
└── guidelines/
    ├── 3-tier-architecture.md — Architectural patterns
    └── code-styling.md       — Coding standards
```

### All Documentation Files

| Document | Description |
|----------|-------------|
| [start-here.md](start-here.md) | Quick start guide |
| [tools-reference.md](tools-reference.md) | Tools.* namespace catalog |
| [real-world-usage-examples.md](real-world-usage-examples.md) | Practical patterns from tests |
| [architecture/sphere10-framework.md](architecture/sphere10-framework.md) | Framework architecture |
| [architecture/domains.md](architecture/domains.md) | Domain catalog |
| [guidelines/3-tier-architecture.md](guidelines/3-tier-architecture.md) | Architectural patterns |
| [guidelines/code-styling.md](guidelines/code-styling.md) | Coding standards |

---

## 🛠️ Tools.* Namespace

The **Tools namespace** provides IntelliSense-discoverable utilities across the entire framework:

```csharp
using Sphere10.Framework;

// Cryptography
byte[] hash = Tools.Crypto.SHA256(data);

// Text processing
string clean = Tools.Text.RemoveWhitespace(input);

// Database access
var dac = Tools.Sqlite.Open("data.db");

// Windows integration
bool running = Tools.WinTool.IsServiceRunning("MyService");
```

See [Tools Reference](tools-reference.md) for the complete catalog.

---

## 🔗 Quick Links

- [Main README](../README.md) — Project overview and structure
- [Framework Architecture](architecture/sphere10-framework.md) — Design and principles
- [Test Projects](../tests/) — 2000+ unit and integration tests

---

## ⚖️ License

Sphere10 Framework is distributed under the **MIT NON-AI License**.

See [LICENSE](../LICENSE) for details.