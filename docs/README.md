# Sphere10 Framework Framework Documentation

**Copyright © 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved.**

Complete documentation for the Sphere10 Framework framework — a comprehensive .NET 5+ framework for building blockchain applications, DApps, and distributed systems.

---

## 📖 Quick Navigation

### Essential Reading

- **[START-HERE.md](START-HERE.md)** — Begin here for framework overview and quick navigation
- **[DApp-Development-Guide.md](DApp-Development-Guide.md)** — Complete guide for building blockchain applications

### Architecture & Design

- **[Architecture/Sphere10.Framework.md](Architecture/Sphere10.Framework.md)** — Framework composition and core concepts
- **[Architecture/Runtime.md](Architecture/Runtime.md)** — Deployment model, lifecycle, and HAP system
- **[Architecture/Domains.md](Architecture/Domains.md)** — Catalog of all framework domains
- **[Guidelines/3-tier-Architecture.md](Guidelines/3-tier-Architecture.md)** — Architectural patterns and design principles
- **[Guidelines/Code-Styling.md](Guidelines/Code-Styling.md)** — Coding standards and conventions

### Component Documentation

- **[PresentationLayer/README.md](PresentationLayer/README.md)** — Blazor-based UI framework and components

---

## 🎯 By Role

### Developers

**Building a DApp?**
1. Start with [START-HERE.md](START-HERE.md)
2. Read [DApp-Development-Guide.md](DApp-Development-Guide.md)
3. Reference [src/Sphere10.Framework.DApp.Core/README.md](../src/Sphere10.Framework.DApp.Core/README.md)
4. Review [src/Sphere10.Framework.DApp.Node/README.md](../src/Sphere10.Framework.DApp.Node/README.md)

**Working with Collections?**
- [src/Sphere10 Framework/README.md](../src/Sphere10 Framework/README.md)
- [src/Sphere10.Framework.Data/README.md](../src/Sphere10.Framework.Data/README.md)

**Implementing Cryptography?**
- [src/Sphere10.Framework.CryptoEx/README.md](../src/Sphere10.Framework.CryptoEx/README.md)

**Building Networking/RPC?**
- [src/Sphere10.Framework.Communications/README.md](../src/Sphere10.Framework.Communications/README.md)

### Architects

**Designing a system?**
1. [Architecture/Sphere10.Framework.md](Architecture/Sphere10.Framework.md) — Understand framework composition
2. [Architecture/Runtime.md](Architecture/Runtime.md) — Learn deployment model
3. [Guidelines/3-tier-Architecture.md](Guidelines/3-tier-Architecture.md) — Study architectural patterns
4. [DApp-Development-Guide.md](DApp-Development-Guide.md) — Review integration patterns

**Planning infrastructure?**
- [Architecture/Runtime.md](Architecture/Runtime.md) — Node, Host, and HAP architecture
- [src/Sphere10.Framework.DApp.Host/README.md](../src/Sphere10.Framework.DApp.Host/README.md) — Host process management
- [src/Sphere10.Framework.DApp.Node/README.md](../src/Sphere10.Framework.DApp.Node/README.md) — Node architecture

### UI Developers

**Building a UI?**
1. [PresentationLayer/README.md](PresentationLayer/README.md) — Component library and patterns
2. [src/Sphere10.Framework.DApp.Presentation/README.md](../src/Sphere10.Framework.DApp.Presentation/README.md) — Presentation framework
3. [DApp-Development-Guide.md](DApp-Development-Guide.md#plugin-architecture) — Plugin system

---

## 📂 Documentation Structure

```
docs/
├── README.md (you are here)
├── START-HERE.md
├── DApp-Development-Guide.md
├── Architecture/
│   ├── Sphere10.Framework.md
│   ├── Runtime.md
│   ├── Domains.md
│   └── resources/
├── Guidelines/
│   ├── 3-tier-Architecture.md
│   ├── Code-Styling.md
│   └── resources/
├── Education/
│   └── README.md
└── PresentationLayer/
    ├── README.md
    ├── Sphere10 Framework-Requirements.md
    ├── Design/
    └── resources/
```

---

## 🔍 By Topic

| Topic | Documentation |
|-------|-----------------|
| **Framework Overview** | [Architecture/Sphere10.Framework.md](Architecture/Sphere10.Framework.md) |
| **DApp Development** | [DApp-Development-Guide.md](DApp-Development-Guide.md) |
| **Deployment & Runtime** | [Architecture/Runtime.md](Architecture/Runtime.md) |
| **Architecture Patterns** | [Guidelines/3-tier-Architecture.md](Guidelines/3-tier-Architecture.md) |
| **Code Standards** | [Guidelines/Code-Styling.md](Guidelines/Code-Styling.md) |
| **Domain Catalog** | [Architecture/Domains.md](Architecture/Domains.md) |
| **UI Components** | [PresentationLayer/README.md](PresentationLayer/README.md) |
| **Collections & Data** | [src/Sphere10 Framework/README.md](../src/Sphere10 Framework/README.md) |
| **Cryptography** | [src/Sphere10.Framework.CryptoEx/README.md](../src/Sphere10.Framework.CryptoEx/README.md) |
| **Consensus** | [DApp-Development-Guide.md](DApp-Development-Guide.md#consensus-integration) |
| **Networking** | [src/Sphere10.Framework.Communications/README.md](../src/Sphere10.Framework.Communications/README.md) |

---

## 📚 Core Domains

The Sphere10 Framework framework provides 30+ domains across multiple categories:

### Collections & Data Structures
- Collections (maps, lists, sets, trees)
- Serialization and deserialization
- Data access (SQL, NoSQL, file-based)
- Object spaces and consensus streams

### Cryptography & Security
- Signatures (ECDSA, EdDSA, DSS, RSA)
- Encryption (AES, RSA, ECC)
- Hashing (SHA256, Keccak, Blake)
- Key derivation and management
- Zero-knowledge proofs

### Networking & Communication
- P2P protocols
- JSON RPC services
- WebSocket support
- Message routing

### Blockchain & Consensus
- Block validation
- Transaction processing
- Consensus mechanisms (PoW, PoS)
- Merkle trees and proofs
- Smart contracts

### Application Framework
- Dependency injection
- Configuration management
- Plugin architecture
- Lifecycle management
- Event system

See [Architecture/Domains.md](Architecture/Domains.md) for the complete catalog.

---

## 🚀 Getting Started

### First Time?

1. Read [START-HERE.md](START-HERE.md) for orientation
2. Review [Architecture/Sphere10.Framework.md](Architecture/Sphere10.Framework.md) for framework overview
3. Choose your path:
   - **Building a DApp**: [DApp-Development-Guide.md](DApp-Development-Guide.md)
   - **Designing a system**: [Guidelines/3-tier-Architecture.md](Guidelines/3-tier-Architecture.md)
   - **Building a UI**: [PresentationLayer/README.md](PresentationLayer/README.md)

### Project-Specific Documentation

Each project in `/src` has its own README with examples and usage patterns:

**Core Framework**
- [Sphere10 Framework](../src/Sphere10 Framework/README.md) — Collections, utilities, core types
- [Sphere10.Framework.NET](../src/Sphere10.Framework.NET/README.md) — .NET-specific utilities
- [Sphere10.Framework.NETCore](../src/Sphere10.Framework.NETCore/README.md) — .NET Core extensions

**Data & Storage**
- [Sphere10.Framework.Data](../src/Sphere10.Framework.Data/README.md) — Database abstraction
- [Sphere10.Framework.Data.Sqlite](../src/Sphere10.Framework.Data.Sqlite/README.md) — SQLite implementation
- [Sphere10.Framework.Data.MSSQL](../src/Sphere10.Framework.Data.MSSQL/README.md) — SQL Server implementation
- [Sphere10.Framework.Data.Firebird](../src/Sphere10.Framework.Data.Firebird/README.md) — Firebird implementation

**Cryptography**
- [Sphere10.Framework.CryptoEx](../src/Sphere10.Framework.CryptoEx/README.md) — Advanced cryptography

**DApp Framework**
- [Sphere10.Framework.DApp.Core](../src/Sphere10.Framework.DApp.Core/README.md) — Blockchain core
- [Sphere10.Framework.DApp.Node](../src/Sphere10.Framework.DApp.Node/README.md) — Node implementation
- [Sphere10.Framework.DApp.Host](../src/Sphere10.Framework.DApp.Host/README.md) — Host management
- [Sphere10.Framework.DApp.Presentation](../src/Sphere10.Framework.DApp.Presentation/README.md) — UI framework

**UI & Presentation**
- [Sphere10.Framework.DApp.Presentation](../src/Sphere10.Framework.DApp.Presentation/README.md) — Blazor components
- [Sphere10.Framework.DApp.Presentation.Loader](../src/Sphere10.Framework.DApp.Presentation.Loader/README.md) — Web app loader

**Networking**
- [Sphere10.Framework.Communications](../src/Sphere10.Framework.Communications/README.md) — RPC and messaging
- [Sphere10.Framework.Web.AspNetCore](../src/Sphere10.Framework.Web.AspNetCore/README.md) — ASP.NET Core integration

**Platform-Specific**
- [Sphere10.Framework.Windows](../src/Sphere10.Framework.Windows/README.md) — Windows integration
- [Sphere10.Framework.Drawing](../src/Sphere10.Framework.Drawing/README.md) — Graphics and drawing

---

## ❓ Common Questions

**Where do I start?**
→ [START-HERE.md](START-HERE.md)

**How do I build a DApp?**
→ [DApp-Development-Guide.md](DApp-Development-Guide.md)

**What's the framework architecture?**
→ [Architecture/Sphere10.Framework.md](Architecture/Sphere10.Framework.md)

**How does deployment work?**
→ [Architecture/Runtime.md](Architecture/Runtime.md)

**What design patterns should I use?**
→ [Guidelines/3-tier-Architecture.md](Guidelines/3-tier-Architecture.md)

**How do I structure my code?**
→ [Guidelines/Code-Styling.md](Guidelines/Code-Styling.md)

**What UI components are available?**
→ [PresentationLayer/README.md](PresentationLayer/README.md)

---

## 📖 Reading Guide

### For Understanding the Framework

1. [Architecture/Sphere10.Framework.md](Architecture/Sphere10.Framework.md) — What Sphere10 Framework is and how it's organized
2. [Architecture/Domains.md](Architecture/Domains.md) — What domains are available
3. [Guidelines/3-tier-Architecture.md](Guidelines/3-tier-Architecture.md) — How to structure applications

### For Building Applications

1. [DApp-Development-Guide.md](DApp-Development-Guide.md) — Complete DApp development reference
2. Project-specific READMEs in `/src` — Domain-specific guidance
3. [Guidelines/Code-Styling.md](Guidelines/Code-Styling.md) — Code standards

### For System Design

1. [Architecture/Sphere10.Framework.md](Architecture/Sphere10.Framework.md) — Framework composition
2. [Architecture/Runtime.md](Architecture/Runtime.md) — Deployment and runtime model
3. [Guidelines/3-tier-Architecture.md](Guidelines/3-tier-Architecture.md) — Architectural patterns
4. [DApp-Development-Guide.md](DApp-Development-Guide.md) — Integration patterns

### For UI Development

1. [PresentationLayer/README.md](PresentationLayer/README.md) — Component library
2. [src/Sphere10.Framework.DApp.Presentation/README.md](../src/Sphere10.Framework.DApp.Presentation/README.md) — Framework details

---

## 🔗 Related Resources

- **[GitHub Repository](https://github.com/HermanSchoenfeld/Sphere10 Framework)** — Source code
- **[NuGet Packages](https://www.nuget.org/packages?q=Sphere10 Framework)** — Published packages
- **[Source Code Documentation](../src/README.md)** — Project structure

---

**Version**: 2.0  
**Last Updated**: December 2025  
**Author**: Sphere 10 Software


