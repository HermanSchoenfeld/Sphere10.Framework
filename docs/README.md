# Sphere10 Framework Documentation

**Copyright © 2018-Present Herman Schoenfeld. All rights reserved.**

Welcome to the documentation for **Sphere10 Framework** — a comprehensive .NET 10 framework for building high-performance applications across desktop, mobile, and web platforms.

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
| [**Patterns and Practices**](guidelines/patterns-and-practices.md) | Task index for the focused pattern guides below |

### Pattern Topics

Choose the pages that apply to the task. The [task index](guidelines/patterns-and-practices.md) maps common changes to these guides.

| Guide | Purpose |
|---|---|
| [Framework reuse](guidelines/patterns/framework-reuse.md) | Reuse existing layers and adopt changes in real consumers. |
| [Type hierarchies](guidelines/patterns/type-hierarchies.md) | Contracts, abstract foundations, concrete products and decorators. |
| [Projection adapters](guidelines/patterns/projections.md) | Typed source implementations exposed through another representation. |
| [Sync/async operations](guidelines/patterns/sync-async.md) | Directional bases, completion, cancellation and disposal. |
| [Interface bridges](guidelines/patterns/interface-bridges.md) | Inline projections between specialized and base contracts. |
| [Application ownership](guidelines/patterns/application-ownership.md) | Runtime parents, definitions, plugins and lifecycle ownership. |
| [WinForms designer](guidelines/patterns/winforms-designer.md) | Concrete visual bases, editable layouts and runtime composition. |
| [Reusable WinForms controls](guidelines/patterns/winforms-controls.md) | Selectors, database provider controls, grids, editors and presentation components. |
| [Windows API facades](guidelines/patterns/windows-interop.md) | Native bindings, ownership and application consumption. |
| [Framework utilities](guidelines/patterns/framework-utilities.md) | Existing Tools, validation, conversion, crypto and comparer infrastructure. |
| [Fluent builders](guidelines/patterns/fluent-builders.md) | Configuration composition, validation and product ownership. |
| [Serialization](guidelines/patterns/serialization.md) | Serializer layers and endian-aware persistence contracts. |
| [Collection selection and composition](guidelines/patterns/collections.md) | Extended lists, dictionary families, projections, paging and storage layers. |
| [Synchronization](guidelines/patterns/synchronization.md) | Read/write scopes, shared parents, enumeration and thread affinity. |
| [Statistics without history](guidelines/patterns/stateless-statistics.md) | Live metric aggregates without retaining samples. |
| [Repositories, persistence and transactions](guidelines/patterns/persistence-and-transactions.md) | CRUD, load/save lifecycle, transaction ownership and backend guarantees. |
| [Collections and storage](guidelines/patterns/collections-and-storage.md) | Caches, graph identity, clustered streams and storage comparers. |
| [Object spaces](guidelines/patterns/object-spaces.md) | Typed identity, indexed CRUD, durability and configuration profiling. |
| [Data sources](guidelines/patterns/data-sources.md) | CRUD, paging, validation and item/range delegation. |
| [Per-user UI settings](guidelines/patterns/user-ui-settings.md) | Typed preferences, stable control identities and accepted save boundaries. |
| [Independent projects](guidelines/patterns/new-projects.md) | Internal/client placement and source/package references. |
| [Logging](guidelines/patterns/logging.md) | Logger selection, composition and sink lifetime. |
| [Resource scopes](guidelines/patterns/resource-scopes.md) | Synchronization, transactions and owned cleanup. |
| [Scheduling](guidelines/patterns/scheduling.md) | Job execution, scheduling policy and completion. |
| [Testing](guidelines/patterns/testing.md) | NUnit runners, assertions and meaningful behavioral coverage. |

---

## 📦 Project Documentation

### Core Framework

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework** | Collections, serialization, cryptography, streams, extensions | [View](../src/Sphere10.Framework/README.md) |
| **Sphere10.Framework.Application** | DI, settings, lifecycle, CLI and shared presentation contracts/engines | [View](../src/Sphere10.Framework.Application/README.md) |
| **Sphere10.Framework.Generators** | C# source generators for compile-time code generation | [View](../src/Sphere10.Framework.Generators/README.md) |
| **Sphere10.Framework.Runtime** | Runtime environment detection and diagnostics | [View](../src/Sphere10.Framework.Runtime/) |
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
| **Sphere10.Framework.Consensus** | Blockchain consensus mechanisms and validation | [View](../src/Sphere10.Framework.Consensus/README.md) |

### Desktop & Windows

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.Windows** | Registry, services, event logging | [View](../src/Sphere10.Framework.Windows/README.md) |
| **Sphere10.Framework.Windows.Forms** | UI components, data binding | [View](../src/Sphere10.Framework.Windows.Forms/README.md) |
| **Sphere10.Framework.Windows.Forms.Sqlite** | WinForms with SQLite data binding | [View](../src/Sphere10.Framework.Windows.Forms.Sqlite/README.md) |
| **Sphere10.Framework.Windows.Forms.MSSQL** | WinForms with SQL Server data binding | [View](../src/Sphere10.Framework.Windows.Forms.MSSQL/README.md) |
| **Sphere10.Framework.Windows.Forms.Firebird** | WinForms with Firebird data binding | [View](../src/Sphere10.Framework.Windows.Forms.Firebird/README.md) |
| **Sphere10.Framework.Windows.LevelDB** | LevelDB key-value storage | [View](../src/Sphere10.Framework.Windows.LevelDB/README.md) |

### Web & Cross-Platform

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.Web** | ASP.NET-independent HTML and web models | [View](../src/Sphere10.Framework.Web/README.md) |
| **Sphere10.Framework.Web.AspNetCore** | Shared HTTP, middleware, logging, and lifecycle | [View](../src/Sphere10.Framework.Web.AspNetCore/README.md) |
| **Sphere10.Framework.Web.AspNetCore.MVC** | MVC forms, filters, routing, and results | [View](../src/Sphere10.Framework.Web.AspNetCore.MVC/README.md) |
| **Sphere10.Framework.Web.AspNetCore.Blazor** | ApplicationBlock workspace, CRUD grid, custom editors, dialogs and wizards | [View](../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) |
| **Sphere10.Framework.Drawing** | Graphics and image manipulation | [View](../src/Sphere10.Framework.Drawing/README.md) |
| **Sphere10.VisualRenderer** | Visual document model to HTML and plain text | Independent component at `Components/Sphere10.VisualRenderer` |
| **Sphere10.Framework.iOS** | Xamarin.iOS integration | [View](../src/Sphere10.Framework.iOS/README.md) |
| **Sphere10.Framework.Android** | Xamarin.Android integration | [View](../src/Sphere10.Framework.Android/README.md) |
| **Sphere10.Framework.macOS** | Xamarin.macOS integration | [View](../src/Sphere10.Framework.macOS/README.md) |

### Testing

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.NUnit** | NUnit utilities and test helpers | [View](../src/Sphere10.Framework.NUnit/README.md) |
| **Sphere10.Framework.NUnit.DB** | Database-specific NUnit testing utilities | [View](../src/Sphere10.Framework.NUnit.DB/README.md) |

---

## 🎯 By Use Case

| Goal | Start Here |
|------|------------|
| New to the framework | [Getting Started](start-here.md) |
| Using Tools.* utilities | [Tools Reference](tools-reference.md) |
| Deferred/lazy/reloadable values and projections | [Future contracts](guidelines/patterns/futures.md) |
| Wrapping an external executable | [Process Sentries](guidelines/patterns/process-sentries.md) |
| Working with collections | [Collection selection](guidelines/patterns/collections.md) / [Sphere10.Framework](../src/Sphere10.Framework/README.md) |
| Protecting shared state and collection operations | [Synchronization scopes](guidelines/patterns/synchronization.md) |
| Repository CRUD, loading/saving and transaction lifetimes | [Persistence and transactions](guidelines/patterns/persistence-and-transactions.md) |
| ObjectSpace schemas, identity and file transactions | [ObjectSpace guide](../src/Sphere10.Framework/ObjectSpaces/README.md) / [Practices](guidelines/patterns/object-spaces.md) |
| ObjectSpace demonstration and configuration performance | [WinForms tester](../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) / [ObjectSpace profiling suite](../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md) |
| Database access | [Sphere10.Framework.Data](../src/Sphere10.Framework.Data/README.md) |
| Cryptography | [Sphere10.Framework.CryptoEx](../src/Sphere10.Framework.CryptoEx/README.md) |
| Networking & RPC | [Sphere10.Framework.Communications](../src/Sphere10.Framework.Communications/README.md) |
| Windows desktop | [Sphere10.Framework.Windows.Forms](../src/Sphere10.Framework.Windows.Forms/README.md) |
| Windows processes, raw counters, services, networking and storage | [Windows resource abstractions](../src/Sphere10.Framework.Windows/README.md#windows-resource-abstractions) and [ownership layers](guidelines/patterns/framework-reuse.md#windows-abstraction-layers); application collectors compose these APIs. |
| ASP.NET Core web | [Sphere10.Framework.Web.AspNetCore](../src/Sphere10.Framework.Web.AspNetCore/README.md) |
| Editable Blazor grids | [Complete grid example](../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#crud-grid-quick-start) |
| Custom grid editors and related records | [Editor, reference picker and action example](../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#custom-editors-reference-pickers-and-actions) |
| Blazor navigation workspace | [ApplicationBlock setup](../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#register-a-workspace) |
| Shared application ownership, plugins and lifecycle | [Application framework](../src/Sphere10.Framework.Application/README.md#shared-ui-framework) / [Initialization and shutdown](../src/Sphere10.Framework.Application/README.md#application-initialization-and-shutdown) |
| WinForms lifecycle, splash and exit | [Native application lifecycle](../src/Sphere10.Framework.Windows.Forms/README.md#application-lifecycle-and-splash) / [WinForms tester](../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) |
| Blazor plugin startup and shutdown | [Demo lifecycle](../utils/Sphere10.Framework.Utils.BlazorTester/README.md#plugin-initialization-and-shutdown) |
| Run the web demos | [Blazor tester](../utils/Sphere10.Framework.Utils.BlazorTester/README.md) / [MVC tester](../utils/Sphere10.Framework.Utils.MvcTester/README.md) |
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
    ├── code-styling.md       — Coding standards
    ├── patterns-and-practices.md — Task index for focused pattern guides
    └── patterns/
        ├── framework-reuse.md
        ├── type-hierarchies.md
        ├── projections.md
        ├── sync-async.md
        ├── interface-bridges.md
        ├── application-ownership.md
        ├── framework-utilities.md
        ├── fluent-builders.md
        ├── serialization.md
        ├── collections.md
        ├── collections-and-storage.md
        ├── synchronization.md
        ├── persistence-and-transactions.md
        ├── object-spaces.md
        ├── data-sources.md
        ├── logging.md
        ├── resource-scopes.md
        ├── scheduling.md
        └── testing.md
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
| [guidelines/patterns-and-practices.md](guidelines/patterns-and-practices.md) | Task index for focused pattern guides |
| [guidelines/patterns/framework-reuse.md](guidelines/patterns/framework-reuse.md) | Reuse and adoption in real consumers |
| [guidelines/patterns/type-hierarchies.md](guidelines/patterns/type-hierarchies.md) | Contract, inheritance, concrete and decorator responsibilities |
| [guidelines/patterns/projections.md](guidelines/patterns/projections.md) | Conversion directions, typed cores and projected views |
| [guidelines/patterns/sync-async.md](guidelines/patterns/sync-async.md) | Directional execution, completion, cancellation and disposal |
| [guidelines/patterns/interface-bridges.md](guidelines/patterns/interface-bridges.md) | Inline specialized interface projections |
| [guidelines/patterns/application-ownership.md](guidelines/patterns/application-ownership.md) | Runtime structure, lifecycle and ownership |
| [guidelines/patterns/framework-utilities.md](guidelines/patterns/framework-utilities.md) | Existing utility infrastructure |
| [guidelines/patterns/fluent-builders.md](guidelines/patterns/fluent-builders.md) | Configuration composition and validation |
| [guidelines/patterns/serialization.md](guidelines/patterns/serialization.md) | Serializer and endian-aware persistence contracts |
| [guidelines/patterns/collections.md](guidelines/patterns/collections.md) | Collection selection, contracts and composition |
| [guidelines/patterns/synchronization.md](guidelines/patterns/synchronization.md) | Read/write scopes, shared parents and borrowed results |
| [guidelines/patterns/persistence-and-transactions.md](guidelines/patterns/persistence-and-transactions.md) | Repositories, loading/saving and transaction ownership |
| [guidelines/patterns/collections-and-storage.md](guidelines/patterns/collections-and-storage.md) | Caches, graph identity and persistent storage layers |
| [guidelines/patterns/object-spaces.md](guidelines/patterns/object-spaces.md) | Object identity, indexed CRUD, persistence and profiling |
| [guidelines/patterns/data-sources.md](guidelines/patterns/data-sources.md) | CRUD, paging and delegation |
| [guidelines/patterns/logging.md](guidelines/patterns/logging.md) | Logger composition and ownership |
| [guidelines/patterns/resource-scopes.md](guidelines/patterns/resource-scopes.md) | Synchronization, transactions and scoped cleanup |
| [guidelines/patterns/scheduling.md](guidelines/patterns/scheduling.md) | Job execution and lifetime |
| [guidelines/patterns/testing.md](guidelines/patterns/testing.md) | Test runners, assertions and coverage |

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

Sphere10 Framework is distributed under the **MIT License**.

See [LICENSE](../LICENSE) for details.
