# Sphere10 Framework Documentation

**Copyright © 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved.**

Complete documentation for **Sphere10 Framework** — a comprehensive .NET 8.0 framework for building full-stack applications across desktop, mobile, and web platforms with advanced data structures, cryptography, persistence, and networking capabilities.

---

## 📖 Quick Navigation

### Essential Resources

- **[START-HERE.md](START-HERE.md)** — New to Sphere10 Framework? Begin here
- **[Tools-Reference.md](Tools-Reference.md)** — Complete catalog of Tools.* namespace utilities
- **[REAL_WORLD_USAGE_EXAMPLES.md](REAL_WORLD_USAGE_EXAMPLES.md)** — Practical examples and patterns

### Architecture & Design

- **[Architecture/Sphere10.Framework.md](Architecture/Sphere10.Framework.md)** — Framework composition and core concepts
- **[Architecture/Domains.md](Architecture/Domains.md)** — Catalog of 30+ framework domains
- **[Guidelines/3-tier-Architecture.md](Guidelines/3-tier-Architecture.md)** — Architectural patterns and design principles
- **[Guidelines/Code-Styling.md](Guidelines/Code-Styling.md)** — Coding standards and conventions

### Component-Specific Docs

See individual project READMEs in `src/` for component-specific documentation:
- Collections, serialization, persistence: [src/Sphere10.Framework/README.md](../src/Sphere10.Framework/README.md)
- Database access: [src/Sphere10.Framework.Data/README.md](../src/Sphere10.Framework.Data/README.md)
- Cryptography: [src/Sphere10.Framework.CryptoEx/README.md](../src/Sphere10.Framework.CryptoEx/README.md)
- Networking & RPC: [src/Sphere10.Framework.Communications/README.md](../src/Sphere10.Framework.Communications/README.md)
- Windows integration: [src/Sphere10.Framework.Windows/README.md](../src/Sphere10.Framework.Windows/README.md)
- Web & ASP.NET Core: [src/Sphere10.Framework.Web.AspNetCore/README.md](../src/Sphere10.Framework.Web.AspNetCore/README.md)

---

## 🎯 By Use Case

| Goal | Start Here |
|------|-----------|
| **New to the framework?** | [START-HERE.md](START-HERE.md) |
| **Learn Tools.* namespace** | [Tools-Reference.md](Tools-Reference.md) |
| **Build with collections** | [src/Sphere10.Framework/README.md](../src/Sphere10.Framework/README.md) |
| **Implement database access** | [src/Sphere10.Framework.Data/README.md](../src/Sphere10.Framework.Data/README.md) |
| **Use cryptography** | [src/Sphere10.Framework.CryptoEx/README.md](../src/Sphere10.Framework.CryptoEx/README.md) |
| **Build networking/RPC** | [src/Sphere10.Framework.Communications/README.md](../src/Sphere10.Framework.Communications/README.md) |
| **Windows development** | [src/Sphere10.Framework.Windows/README.md](../src/Sphere10.Framework.Windows/README.md) |
| **Web application (ASP.NET)** | [src/Sphere10.Framework.Web.AspNetCore/README.md](../src/Sphere10.Framework.Web.AspNetCore/README.md) |

---

## 📂 Documentation Structure

```
docs/
├── README.md (you are here)
├── START-HERE.md — Quick start guide
├── Tools-Reference.md — Tools.* namespace catalog
├── REAL_WORLD_USAGE_EXAMPLES.md — Practical examples
├── Architecture/
│   ├── Sphere10.Framework.md — Framework overview
│   ├── Domains.md — Domain catalog
│   └── resources/
├── Guidelines/
│   ├── 3-tier-Architecture.md — Architecture patterns
│   ├── Code-Styling.md — Code standards
│   └── resources/
├── Education/
│   └── README.md — Learning resources
└── PresentationLayer/
    ├── README.md — Blazor UI framework
    └── resources/
```

---

## 🔍 Documentation by Topic

### Framework Fundamentals

**What is Sphere10 Framework?**
- A comprehensive, production-ready .NET 8.0 framework
- 45+ interconnected projects
- Low-level, high-performance utilities
- No external dependencies for core functionality
- Full-stack support: desktop, mobile, web

See: [Architecture/Sphere10.Framework.md](Architecture/Sphere10.Framework.md)

### Data Structures & Collections

Sphere10 Framework provides 50+ collection types:
- Extended lists, stream-mapped, paged, recyclable, observable
- Merkle-tree implementations
- Clustered streams for multi-stream storage
- Thread-safe concurrent collections

See: [src/Sphere10.Framework/README.md](../src/Sphere10.Framework/README.md)

### Data Access & Persistence

Universal database abstraction layer supporting:
- SQL Server, SQLite, Firebird, NHibernate
- Transactional scopes with ACID semantics
- Parameterized queries (SQL injection safe)
- Type mapping and conversion

See: [src/Sphere10.Framework.Data/README.md](../src/Sphere10.Framework.Data/README.md)

### Cryptography & Security

Comprehensive cryptographic support:
- Hashing algorithms (SHA, BLAKE2, etc.)
- Digital signatures and verification
- Key derivation and management
- Post-quantum resistance
- VRF (Verifiable Random Function)

See: [src/Sphere10.Framework.CryptoEx/README.md](../src/Sphere10.Framework.CryptoEx/README.md)

### Networking & Communication

Multi-protocol networking:
- TCP, UDP, WebSockets
- JSON-RPC framework
- Service-oriented RPC with attributes
- Protocol abstraction for extensibility

See: [src/Sphere10.Framework.Communications/README.md](../src/Sphere10.Framework.Communications/README.md)

### Platform Integration

- **Windows**: Registry, services, event logging, security
- **Web**: ASP.NET Core middleware, HTML utilities
- **Mobile**: iOS, Android, macOS integration
- **Cross-platform**: .NET Core, Xamarin

See individual platform project READMEs in `src/`

### Testing & Quality

Comprehensive testing support:
- NUnit testing framework integration
- Database testing utilities
- Test fixtures and helpers
- 2000+ framework tests included

See: [src/Sphere10.Framework.NUnit/README.md](../src/Sphere10.Framework.NUnit/README.md)

---

## 🛠️ Tools.* Namespace

The **Tools namespace** provides global, IntelliSense-discoverable utilities:

```csharp
using Tools;

// Discovery-first pattern
byte[] hash = Tools.Crypto.SHA256(data);
string sanitized = Tools.Text.RemoveWhitespace(input);
var connection = Tools.Sqlite.Create(connectionString);
```

See: [Tools-Reference.md](Tools-Reference.md) for complete catalog

---

## 📚 Related Resources

- **[README.md](../README.md)** — Project overview and structure
- **[PUBLISHING.md](../PUBLISHING.md)** — Package and publish guide
- **[Architecture/Domains.md](Architecture/Domains.md)** — Complete domain reference
- **[Guidelines/Code-Styling.md](Guidelines/Code-Styling.md)** — Coding standards

---

## ⚖️ License

Sphere10 Framework is distributed under the **MIT NON-AI License**.

See [LICENSE](../LICENSE) file for details. This license protects the codebase from AI model training.

---

**Version**: 3.0.0  
**Target Framework**: .NET 8.0  
**Last Updated**: December 31, 2025


