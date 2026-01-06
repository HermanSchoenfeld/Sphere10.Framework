# Sphere10 Framework Documentation

**Copyright © 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved.**

Complete documentation for **Sphere10 Framework** — a comprehensive .NET 8.0 framework for building full-stack applications across desktop, mobile, and web platforms with advanced data structures, cryptography, persistence, and networking capabilities.

---

## 📖 Quick Navigation

### Essential Resources

- **[Getting Started](start-here.md)** — New to Sphere10 Framework? Begin here
- **[Tools Reference](tools-reference.md)** — Complete catalog of Tools.* namespace utilities
- **[Real-World Usage Examples](real-world-usage-examples.md)** — Practical examples and patterns

### Architecture & Design

- **[Sphere10 Framework Architecture](architecture/sphere10-framework.md)** — Framework composition and core concepts
- **[Framework Domains](architecture/domains.md)** — Catalog of 40+ framework domains
- **[3-Tier Architecture Pattern](guidelines/3-tier-architecture.md)** — Architectural patterns and design principles
- **[Code Styling Standards](guidelines/code-styling.md)** — Coding standards and conventions

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
| **New to the framework?** | [Getting Started](start-here.md) |
| **Learn Tools.* namespace** | [Tools Reference](tools-reference.md) |
| **Build with collections** | [Sphere10.Framework Core](../src/Sphere10.Framework/README.md) |
| **Implement database access** | [Data Access Layer](../src/Sphere10.Framework.Data/README.md) |
| **Use cryptography** | [Cryptography Module](../src/Sphere10.Framework.CryptoEx/README.md) |
| **Build networking/RPC** | [Communications & Networking](../src/Sphere10.Framework.Communications/README.md) |
| **Windows development** | [Windows Integration](../src/Sphere10.Framework.Windows/README.md) |
| **Web application (ASP.NET)** | [Web & ASP.NET Core](../src/Sphere10.Framework.Web.AspNetCore/README.md) |

---

## 📂 Documentation Structure

```
docs/
├── index.md (you are here)
├── start-here.md — Quick start guide
├── tools-reference.md — Tools.* namespace catalog
├── real-world-usage-examples.md — Practical examples
├── architecture/
│   ├── sphere10-framework.md — Framework overview
│   ├── domains.md — Domain catalog
│   └── resources/
├── guidelines/
│   ├── 3-tier-architecture.md — Architecture patterns
│   ├── code-styling.md — Code standards
│   └── resources/
├── education/
│   └── README.md — Learning resources
└── presentation-layer/
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

See: [architecture/sphere10-framework.md](architecture/sphere10-framework.md)

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

See: [tools-reference.md](tools-reference.md) for complete catalog

---

## 📚 Related Resources

- **[README.md](../README.md)** — Project overview and structure
- **[architecture/domains.md](architecture/domains.md)** — Complete domain reference
- **[guidelines/code-styling.md](guidelines/code-styling.md)** — Coding standards

---

## ⚖️ License

Sphere10 Framework is distributed under the **MIT NON-AI License**.

See [LICENSE](../LICENSE) file for details. This license protects the codebase from AI model training.

---

**Version**: 3.0.0  
**Target Framework**: .NET 8.0  
**Last Updated**: December 31, 2025


