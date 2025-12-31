# ðŸ¤– Copilot Instructions for Sphere10 Framework Framework

Welcome, AI coding agents! This guide distills essential knowledge for productive work in the Sphere10 Framework codebase. Focus on these project-specific conventions, workflows, and architecture patterns:

## ðŸ—ï¸ Big Picture Architecture
- **Modular Monorepo**: All core, platform, and DApp projects live under `src/`, with tests in `tests/` and Blazor UI in `blackhole/`.
- **Layered Design**: Key layers include:
  - `Sphere10 Framework` (core utilities, collections, cryptography)
  - `Sphere10.Framework.Data` (ADO.NET abstraction, multi-DB support)
  - `Sphere10.Framework.Communications` (networking: TCP, UDP, WebSockets, RPC)
  - `Sphere10.Framework.DApp.Core` (blockchain primitives, plugins, persistence)
  - `Sphere10.Framework.DApp.Node` (full blockchain node, CLI, APIs)
  - `Sphere10.Framework.DApp.Presentation*` (Blazor UI components, loaders)
- **Cross-Platform**: Supports .NET Framework, .NET Core, Xamarin, and Blazor.

## ðŸ› ï¸ Developer Workflows
- **Build**: Use the solution files in `src/` (`Sphere10 Framework (CrossPlatform).sln`, `Sphere10 Framework (Win).sln`).
- **Test**: All major subsystems have corresponding `*.Tests` projects in `tests/`. Run via standard .NET test runners.
- **Database**: Use `Sphere10.Framework.Data` abstractions for all DB access. See `src/Sphere10.Framework.Data.MSSQL/README.md` for SQL Server usage patterns.
- **Web UI**: Blazor-based UIs are in `blackhole/`. Use `Sphere10.Framework.DApp.Presentation.Loader` for WebAssembly hosting.

## ðŸ“ Project-Specific Patterns
- **Dependency Injection**: Use Sphere10 Framework's built-in DI (see `Sphere10.Framework.Application`).
- **Plugin System**: DApps and nodes use dynamic plugin loading (see `Sphere10.Framework.DApp.Core`).
- **Data Access**: Always use the `Tools.[DB].Open(...)` pattern for DB connections. Avoid direct ADO.NET.
- **Testing**: Prefer Sphere10 Framework's test utilities for integration tests.
- **UI**: Blazor components follow the patterns in `blackhole/Sphere10.Framework.DApp.Presentation/`.

## ðŸ”— Integration & Communication
- **Networking**: Use abstractions in `Sphere10.Framework.Communications` for all protocol implementations.
- **Cross-Component**: Communicate via defined interfaces and plugin contracts, not direct references.
- **External Dependencies**: All cryptography uses Sphere10 Framework or HashLib4CSharp; avoid other crypto libs.

## ðŸ“š Key References
- [README.md](../../README.md): Project map, architecture, and navigation
- [docs/START-HERE.md](../../docs/START-HERE.md): Onboarding and quick links
- [docs/Architecture/Sphere10.Framework.md](../../docs/Architecture/Sphere10.Framework.md): Core architecture
- [src/Sphere10.Framework.Data.MSSQL/README.md](../../src/Sphere10.Framework.Data.MSSQL/README.md): DB patterns
- [blackhole/Sphere10.Framework.DApp.Presentation/README.md](../../blackhole/Sphere10.Framework.DApp.Presentation/README.md): UI patterns

---

**Tip:** When in doubt, search for usage examples in `tests/` or look for README files in each project directory.

