# 🤖 Copilot Instructions for Sphere10 Framework

Welcome, AI coding agents! This guide provides essential context for productive work in the Sphere10 Framework codebase. Focus on project-specific conventions, workflows, and architecture patterns.

## 🏗️ Project Overview

**Sphere10 Framework** is a comprehensive, production-ready .NET application framework (v3.0.0) providing full-stack support across desktop, mobile, and web platforms. Originally designed for blockchain systems, it has evolved into a general-purpose framework with 45+ interconnected projects.

**Repository**: `Sphere10/Sphere10.Framework` (named `Hydrogen` for historical reasons)  
**Language**: C# / .NET 8.0  
**Core Focus**: Blockchain, DApps, data persistence, cryptography, networking, UI components

## 🗂️ Repository Structure

```
src/                          # Core framework projects & platform integrations
├── Sphere10.Framework/       # Base library (utilities, collections, crypto, serialization)
├── Sphere10.Framework.Data/  # Data access abstraction layer
├── Sphere10.Framework.Communications/  # Networking (TCP, UDP, WebSockets, RPC)
├── Sphere10.Framework.CryptoEx/       # Extended crypto (ECDSA, post-quantum, hashing)
├── Sphere10.Framework.DApp.Core/      # Blockchain/DApp primitives
├── Sphere10.Framework.DApp.Node/      # Full blockchain node implementation
├── Sphere10.Framework.Windows*        # Windows-specific modules
├── Sphere10.Framework.Web.AspNetCore/ # ASP.NET Core integration
├── Sphere10.Framework.Drawing/        # Cross-platform graphics
├── [Platform-specific]: iOS, Android, macOS, NETCore
└── [Database adapters]: MSSQL, SQLite, Firebird, NHibernate
tests/                        # 2000+ unit/integration tests
blackhole/                    # Blazor presentation layer
docs/                         # Architecture, guidelines, education
resources/                    # Branding, fonts, presentations
```

## 🔧 Build & Development

### Solution Files
- `src/Sphere10 Framework (CrossPlatform).sln` — Multi-platform build
- `src/Sphere10 Framework (Win).sln` — Windows-only build

### Testing
- Test runners: NUnit framework
- Coverage: 2000+ tests across all subsystems
- Run tests via standard .NET test runners or IDE

## 🏛️ Architectural Patterns

### Layered Design
1. **Core Framework** (`Sphere10.Framework`) — Utilities, collections, serialization, DI
2. **Data Access** (`Sphere10.Framework.Data`) — ADO.NET abstraction, multi-DB support
3. **Networking** (`Sphere10.Framework.Communications`) — TCP, UDP, WebSockets, RPC
4. **Blockchain/DApp** (`Sphere10.Framework.DApp.*`) — Blocks, wallets, plugins, nodes
5. **Presentation** (`blackhole/`) — Blazor components, WebAssembly hosting

### Design Principles
- **Separation of Concerns**: Single responsibility per project
- **Dependency Injection**: Built-in DI container (not external)
- **Plugin Architecture**: Dynamic loading for extensibility
- **Data Abstraction**: All DB access via `Sphere10.Framework.Data`
- **Cryptography**: Use HashLib4CSharp or framework utilities only

## 📝 Code Conventions

### Naming
- **Namespaces**: `Sphere10.Framework[.Feature]`
- **Classes**: PascalCase, descriptive
- **Methods**: PascalCase
- **Fields**: `_camelCase` (private), `camelCase` (parameters)

### Documentation
- Maintain `README.md` in every project directory
- Use XML comments for public APIs
- Include usage examples in README files
- Link to architecture docs where applicable

## 🔐 Dependencies & Security

### Cryptography (Required)
- **Primary**: HashLib4CSharp
- **Secondary**: Sphere10.Framework.CryptoEx
- ❌ **Avoid**: Other crypto libraries

### External Integrations (Optional)
- **NHibernate**: For ORM support
- **Newtonsoft.Json**: For JSON serialization (where needed)

### License
- **Code**: MIT NON-AI (protects against AI training)
- **Requirement**: Retain notice in file headers during duplication

## 📚 Key References

### Documentation
- [README.md](../../README.md) — Project map and overview
- [docs/START-HERE.md](../../docs/START-HERE.md) — Onboarding guide
- [docs/Architecture/Sphere10.Framework.md](../../docs/Architecture/Sphere10.Framework.md) — Architecture deep-dive
- [docs/DApp-Development-Guide.md](../../docs/DApp-Development-Guide.md) — DApp development
- [docs/Guidelines/Code-Styling.md](../../docs/Guidelines/Code-Styling.md) — Code standards

### Core Project READMEs
- [src/Sphere10.Framework/README.md](../../src/Sphere10.Framework/README.md)
- [src/Sphere10.Framework.Data/README.md](../../src/Sphere10.Framework.Data/README.md)
- [src/Sphere10.Framework.DApp.Core/README.md](../../src/Sphere10.Framework.DApp.Core/README.md)
- [src/Sphere10.Framework.Communications/README.md](../../src/Sphere10.Framework.Communications/README.md)
- [blackhole/Sphere10.Framework.DApp.Presentation/README.md](../../blackhole/Sphere10.Framework.DApp.Presentation/README.md)

## 💼 Common Workflows

### Adding a Feature
1. Implement in appropriate namespace in `src/Sphere10.Framework/`
2. Add tests in `tests/Sphere10.Framework.Tests/`
3. Update project README
4. Run full test suite
5. Commit with descriptive message

### Working with Persistence
1. Use `Sphere10.Framework.ObjectSpaces` for stream-mapped data
2. Define dimensions via attributes or builder
3. Leverage automatic change tracking and instance caching
4. Use `Flush()` to persist and update merkle-trees

### Building a DApp
1. Create DApp class from blockchain primitives
2. Implement consensus rules via plugin system
3. Use `Sphere10.Framework.DApp.Node` for full node
4. Blazor UI via `blackhole/Sphere10.Framework.DApp.Presentation/`

## ⚠️ Common Pitfalls

- ❌ Don't use external DI containers; use built-in
- ❌ Don't bypass `Sphere10.Framework.Data` abstractions
- ❌ Don't add external crypto libraries
- ❌ Don't ignore change tracking in persistence
- ❌ Don't hardcode file paths; use framework abstractions
- ✅ **Do** search `tests/` for usage examples
- ✅ **Do** read project README files before implementing
- ✅ **Do** run full test suite before committing
- ✅ **Do** use interface-based patterns

## 🚀 Version Info

- **Current Version**: 3.0.0
- **Target Framework**: .NET 8.0
- **Package Icon**: `resources/branding/sphere10-icon.png`
- **Documentation Logo**: `resources/branding/sphere-10-framework-logo.jpg`
- **License**: MIT NON-AI

---

**Last Updated**: December 31, 2025 | **Framework Version**: 3.0.0  
**Tip**: Check project README files and `tests/` directory for usage examples!
