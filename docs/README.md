# Sphere10 Framework Documentation

**Copyright © 2018-Present Herman Schoenfeld. All rights reserved.**

This index links every guide under `docs/` and the Framework project READMEs. Sphere10 Framework is a family of MIT-licensed .NET 10 libraries for application shells, data access, embedded object storage, collections, serialization and cryptography. Begin with the entry points below, or look up a task in [By Use Case](#-by-use-case).

---

## 📖 Start Here

| Resource | Description |
|----------|-------------|
| [**Getting Started**](start-here.md) | Orientation, installation and first examples |
| [**Tools Reference**](tools-reference.md) | `Tools.*` facades with their declaring projects and representative members |
| [**Real-World Examples**](real-world-usage-examples.md) | Usage examples drawn from the test suites |
| [**Patterns and Practices**](guidelines/patterns-and-practices.md) | Task index that maps a change to the pattern topics it needs |

A console program that references `Sphere10.Framework.Application` starts the Framework, uses it and ends it:

```csharp
using System;
using System.Text;
using Sphere10.Framework;
using Sphere10.Framework.Application;

Sphere10Framework.Instance
	.Build()
	.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
	.Start();
try {
	var digest = Hashers.Hash(CHF.SHA2_256, Encoding.UTF8.GetBytes("Hello"));
	Console.WriteLine(Convert.ToHexString(digest));
} finally {
	Sphere10Framework.Instance.EndFramework();
}
```

The Application module installs the settings and product information services. [Application initialization and shutdown](../src/Sphere10.Framework.Application/README.md#application-initialization-and-shutdown) describes the other modules and the start-up order.

---

## 🎯 By Use Case

| Goal | Start Here |
|------|------------|
| New to the framework | [Getting Started](start-here.md) |
| Using `Tools.*` utilities | [Tools Reference](tools-reference.md) |
| Working with collections | [Collection selection](guidelines/patterns/collections.md) / [Sphere10.Framework](../src/Sphere10.Framework/README.md) |
| Protecting shared state and collection operations | [Synchronization scopes](guidelines/patterns/synchronization.md) |
| Cleanup, locking and pooled-array scopes | [Resource scopes](guidelines/patterns/resource-scopes.md) |
| Deferred, lazy, reloadable or projected values | [Futures](guidelines/patterns/futures.md) |
| Repository CRUD, loading/saving and transaction lifetimes | [Persistence and transactions](guidelines/patterns/persistence-and-transactions.md) |
| ObjectSpace schemas, identity and file transactions | [ObjectSpace guide](../src/Sphere10.Framework/ObjectSpaces/README.md) / [Practices](guidelines/patterns/object-spaces.md) |
| Reopening an ObjectSpace without supplying its schema | [Self-describing storage](usage/self-describing-storage.md) |
| ObjectSpace demonstration and configuration performance | [WinForms tester](../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) / [ObjectSpace profiling suite](../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md) |
| Data sources for grids and inventories | [Data sources](guidelines/patterns/data-sources.md) |
| Database access | [Sphere10.Framework.Data](../src/Sphere10.Framework.Data/README.md) |
| Cryptography | [Cryptography practice](guidelines/patterns/framework-utilities.md#cryptography) / [Sphere10.Framework.CryptoEx](../src/Sphere10.Framework.CryptoEx/README.md) |
| Networking & RPC | [Sphere10.Framework.Communications](../src/Sphere10.Framework.Communications/README.md) |
| Wrapping an external executable | [Process Sentries](guidelines/patterns/process-sentries.md) |
| Shared application ownership, plugins and lifecycle | [Application framework](../src/Sphere10.Framework.Application/README.md#shared-ui-framework) / [Initialization and shutdown](../src/Sphere10.Framework.Application/README.md#application-initialization-and-shutdown) |
| Moving from `Configure*` to `Add*` builder methods | [Builder API migration](usage/builder-api-migration.md) |
| Screen, block and plugin preferences | [Screen Options](usage/screen-options.md) |
| Windows desktop | [Sphere10.Framework.Windows.Forms](../src/Sphere10.Framework.Windows.Forms/README.md) |
| WinForms lifecycle, splash and exit | [Native application lifecycle](../src/Sphere10.Framework.Windows.Forms/README.md#application-lifecycle-and-splash) / [WinForms tester](../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) |
| Windows processes, raw counters, services, networking and storage | [Windows resource abstractions](../src/Sphere10.Framework.Windows/README.md#windows-resource-abstractions) / [Windows ownership layers](guidelines/patterns/framework-reuse.md#windows-abstraction-layers) |
| ASP.NET Core web | [Sphere10.Framework.Web.AspNetCore](../src/Sphere10.Framework.Web.AspNetCore/README.md) |
| Editable Blazor grids | [CRUD grid quick start](../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#crud-grid-quick-start) |
| Custom grid editors and related records | [Editor, reference picker and action example](../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#custom-editors-reference-pickers-and-actions) |
| Blazor navigation workspace | [ApplicationBlock setup](../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#register-a-workspace) |
| Blazor plugin startup and shutdown | [Blazor tester lifecycle](../utils/Sphere10.Framework.Utils.BlazorTester/README.md#plugin-initialization-and-shutdown) |
| Run the web demos | [Blazor tester](../utils/Sphere10.Framework.Utils.BlazorTester/README.md) / [MVC tester](../utils/Sphere10.Framework.Utils.MvcTester/README.md) |
| Legacy Xamarin mobile source | [iOS](../src/Sphere10.Framework.iOS/README.md) / [Android](../src/Sphere10.Framework.Android/README.md) |

---

## 📘 Usage Guides

These guides cover individual APIs and migrations in more depth than the project READMEs.

| Guide | Covers |
|---|---|
| [Keys, graphs and mapped properties](usage/keys-graphs-and-stream-properties.md) | Compound keys, graph construction and traversal, the `IBTree` contract and stream-mapped properties with optional caching |
| [Self-describing storage](usage/self-describing-storage.md) | ObjectSpace and ObjectStream metadata records, their clustered layout, and creating and reopening a store without a supplied schema |
| [Serializing a serializer factory](usage/serializer-factory-serialization.md) | `SerializerFactorySerializer`, the registrations it preserves and its construction limits |
| [Buffered streams](usage/buffered-stream.md) | `BufferedStream` read-ahead, optional length caching, stream ownership and measured file performance |
| [Builder API migration](usage/builder-api-migration.md) | The `Add*`, `With*` and `On*` names that replace earlier builder methods such as `ConfigurePlugin` and `UseModule`, and screen registration with `AddScreen` |
| [Screen Options](usage/screen-options.md) | Screen, block and plugin settings objects, the Options editors on WinForms and Blazor, and grid column layouts kept in screen settings |
| [SourceGrid](usage/sourcegrid.md) | When to use SourceGrid directly instead of VisualGrid, component ownership and the raw engine gallery in the WinForms tester |
| [Portable executable inspection](usage/portable-executable-inspection.md) | PE structure inspection without loading the target, and bounded process-memory reads |
| [Managed diagnostics](usage/managed-diagnostics.md) | Assembly inspection, managed process and dump capture, native stacks and detached symbol resolution |
| [Process inspection](usage/process-inspection.md) | `WindowsProcess` tokens and mitigations, module details, page state and the explicit working-set watch |
| [Blazor theme sources](theming-template-sources.md) | Origin, palettes, font fallbacks and component treatments of each Blazor theme family |

---

## 🏗️ Architecture & Design

| Document | Description |
|----------|-------------|
| [**Framework Architecture**](architecture/sphere10-framework.md) | Design philosophy, layers, core subsystems and platform strategy |
| [**Framework Domains**](architecture/domains.md) | Domains grouped by tier, their dependencies and the domains each kind of application uses |
| [**3-Tier Architecture**](guidelines/3-tier-architecture.md) | Domains, modules, the framework layer and project boundaries |
| [**Code Styling**](guidelines/code-styling.md) | Formatting, naming, member order, namespaces and the `Tools` pattern |
| [**Patterns and Practices**](guidelines/patterns-and-practices.md) | Task index for the pattern topics below |

### Pattern Topics

Each topic records one implementation practice. The [task index](guidelines/patterns-and-practices.md) maps common changes to the topics they need.

| Guide | Purpose |
|---|---|
| [Framework reuse](guidelines/patterns/framework-reuse.md) | Reuse existing layers and adopt changes in real consumers. |
| [Type hierarchies](guidelines/patterns/type-hierarchies.md) | Contracts, abstract foundations, concrete products, decorators, events and template hooks. |
| [Projection adapters](guidelines/patterns/projections.md) | Typed source implementations exposed through another representation. |
| [Sync/async operations](guidelines/patterns/sync-async.md) | Directional bases, completion, cancellation and disposal. |
| [Futures and deferred values](guidelines/patterns/futures.md) | The `IFuture` contract, its factories, ownership and concurrency. |
| [Interface bridges](guidelines/patterns/interface-bridges.md) | Inline projections between specialized and base contracts. |
| [Application ownership](guidelines/patterns/application-ownership.md) | Runtime parents, definitions, plugins, and application and screen lifetimes. |
| [WinForms designer](guidelines/patterns/winforms-designer.md) | Concrete visual bases, editable layouts and runtime composition. |
| [WinForms screen settings](guidelines/patterns/winforms-screen-settings.md) | Screen, block and plugin settings declared with `[Settings]` and edited in Options. |
| [Reusable WinForms controls](guidelines/patterns/winforms-controls.md) | Selectors, database provider controls, grids, editors and presentation components. |
| [Grids and screen hosts](guidelines/patterns/grids-and-screen-hosts.md) | WinForms/Blazor feature mapping, paging, live refresh, retained layouts, tabs and startup choices. |
| [Windows API facades](guidelines/patterns/windows-interop.md) | Native bindings, ownership and application consumption. |
| [Process Sentries](guidelines/patterns/process-sentries.md) | `ProcessSentry` wrappers for external executables, with their cancellation and ownership. |
| [Framework utilities](guidelines/patterns/framework-utilities.md) | `Tools.*` facades, guards, exception capture and cryptography. |
| [Fluent builders](guidelines/patterns/fluent-builders.md) | Configuration composition, validation and product ownership. |
| [Serialization](guidelines/patterns/serialization.md) | Serializer layers and endian-aware persistence contracts. |
| [Collection selection and composition](guidelines/patterns/collections.md) | Extended lists, dictionary families, projections, paging and storage layers. |
| [Synchronization](guidelines/patterns/synchronization.md) | Read/write scopes, shared parents, enumeration and thread affinity. |
| [Statistics without history](guidelines/patterns/stateless-statistics.md) | Live metric aggregates without retaining samples. |
| [Repositories, persistence and transactions](guidelines/patterns/persistence-and-transactions.md) | CRUD, load/save lifecycle, transaction ownership and backend guarantees. |
| [Collections and storage](guidelines/patterns/collections-and-storage.md) | Caches, graph identity, clustered streams and storage comparers. |
| [Object spaces](guidelines/patterns/object-spaces.md) | Typed identity, indexed CRUD, durability and configuration profiling. |
| [Data sources](guidelines/patterns/data-sources.md) | Storage, decorators and compositions, the sync/async hierarchy, change notifications and refresh policy. |
| [Per-user UI settings](guidelines/patterns/user-ui-settings.md) | Typed preferences, stable control identities and accepted save boundaries. |
| [Independent projects](guidelines/patterns/new-projects.md) | Internal/client placement and source/package references. |
| [Logging](guidelines/patterns/logging.md) | Logger selection, composition and sink lifetime. |
| [Resource scopes](guidelines/patterns/resource-scopes.md) | Synchronization, transactions and owned cleanup. |
| [Scheduling](guidelines/patterns/scheduling.md) | Job execution, scheduling policy and completion. |
| [Testing](guidelines/patterns/testing.md) | NUnit conventions, the runner contract, WinForms UI test contexts and startup memory regressions. |

---

## 📦 Project Documentation

### Core Framework

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework** | Collections, serialization, streams, ObjectSpaces, caches, cryptography and extensions | [View](../src/Sphere10.Framework/README.md) |
| **Sphere10.Framework.Application** | Application model, dependency injection, settings, lifecycle, command-line parsing and the UI contracts shared by WinForms and Blazor | [View](../src/Sphere10.Framework.Application/README.md) |
| **Sphere10.Framework.Generators** | `AutoDirty` source generator, which sets a dirty flag when a partial property changes | [View](../src/Sphere10.Framework.Generators/README.md) |
| **Sphere10.Framework.Runtime** | Internal math-expression compiler and C# code builders (not packed, no README) | [Source](../src/Sphere10.Framework.Runtime/) |
| **Sphere10.HashLib4CSharp** | Hash and checksum algorithms, including MD5, SHA, BLAKE2 and CRC | [View](../src/Sphere10.HashLib4CSharp/README.md) |

Folder guides inside these projects:

- [ObjectSpaces](../src/Sphere10.Framework/ObjectSpaces/README.md), [caches](../src/Sphere10.Framework/Cache/README.md) and [BufferedStream](../src/Sphere10.Framework/Streams/README.md) in `Sphere10.Framework`
- [Settings](../src/Sphere10.Framework.Application/Settings/README.md) and the bundled [command-line parser](../src/Sphere10.Framework.Application/CommandLine/README.md) in `Sphere10.Framework.Application`

### Data Access

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.Data** | Database-agnostic `IDAC` contract, SQL builders, transaction scopes, schema metadata and CSV, JSON and XML utilities | [View](../src/Sphere10.Framework.Data/README.md) |
| **Sphere10.Framework.Data.Sqlite** | SQLite provider for file and in-memory databases | [View](../src/Sphere10.Framework.Data.Sqlite/README.md) |
| **Sphere10.Framework.Data.MSSQL** | SQL Server provider | [View](../src/Sphere10.Framework.Data.MSSQL/README.md) |
| **Sphere10.Framework.Data.Firebird** | Firebird provider for server and embedded databases | [View](../src/Sphere10.Framework.Data.Firebird/README.md) |
| **Sphere10.Framework.Data.MySQL** | MySQL provider backed by MySqlConnector | [View](../src/Sphere10.Framework.Data.MySQL/README.md) |
| **Sphere10.Framework.Data.PostgreSQL** | PostgreSQL provider backed by Npgsql | [View](../src/Sphere10.Framework.Data.PostgreSQL/README.md) |
| **Sphere10.Framework.Data.Oracle** | Oracle provider backed by Oracle.ManagedDataAccess.Core | [View](../src/Sphere10.Framework.Data.Oracle/README.md) |
| **Sphere10.Framework.Data.NHibernate** | NHibernate ORM integration | [View](../src/Sphere10.Framework.Data.NHibernate/README.md) |

### Networking & Communications

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.Communications** | TCP, UDP, WebSocket and anonymous-pipe channels, and JSON-RPC clients and servers | [View](../src/Sphere10.Framework.Communications/README.md) |

### Cryptography & Blockchain

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.CryptoEx** | ECDSA, Schnorr and MuSig signatures, ECIES encryption and additional hash algorithms | [View](../src/Sphere10.Framework.CryptoEx/README.md) |
| **Sphere10.Framework.Consensus** | Reversible chain state, chain and branch management, compact targets and ASERT difficulty adjustment | [View](../src/Sphere10.Framework.Consensus/README.md) |

Further documents in these projects:

- [Consensus design document](../src/Sphere10.Framework.Consensus/DESIGN.md)
- [PascalCoin ECIES compatibility](../src/Sphere10.Framework.CryptoEx/PascalCoin/ECIES-compatibility.md) in `Sphere10.Framework.CryptoEx`

### Desktop & Windows

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.Windows** | Processes, services, registry, security, event logs, networking, storage and diagnostics through `Tools.Windows` | [View](../src/Sphere10.Framework.Windows/README.md) |
| **Sphere10.Framework.Windows.Forms** | WinForms application shell, screen hosting, VisualGrid, charts, dialogs, wizards and reusable controls | [View](../src/Sphere10.Framework.Windows.Forms/README.md) |
| **Sphere10.Framework.Windows.Forms.DataVisualization** | Legacy Microsoft DataVisualization chart control, absorbed with its original licenses | [View](../src/Sphere10.Framework.Windows.Forms.DataVisualization/README.md) |
| **Sphere10.Framework.Windows.Forms.Sqlite** | SQLite connection editors | [View](../src/Sphere10.Framework.Windows.Forms.Sqlite/README.md) |
| **Sphere10.Framework.Windows.Forms.MSSQL** | SQL Server connection editors | [View](../src/Sphere10.Framework.Windows.Forms.MSSQL/README.md) |
| **Sphere10.Framework.Windows.Forms.Firebird** | Firebird server and embedded connection editors | [View](../src/Sphere10.Framework.Windows.Forms.Firebird/README.md) |
| **Sphere10.Framework.Windows.Forms.MySQL** | MySQL connection editors | [View](../src/Sphere10.Framework.Windows.Forms.MySQL/README.md) |
| **Sphere10.Framework.Windows.Forms.PostgreSQL** | PostgreSQL connection editors | [View](../src/Sphere10.Framework.Windows.Forms.PostgreSQL/README.md) |
| **Sphere10.Framework.Windows.Forms.Oracle** | Oracle connection editors | [View](../src/Sphere10.Framework.Windows.Forms.Oracle/README.md) |
| **Sphere10.Framework.Windows.LevelDB** | LevelDB key-value storage | [View](../src/Sphere10.Framework.Windows.LevelDB/README.md) |

`Sphere10.Framework.Windows` also documents its [power plan and power setting inventories](../src/Sphere10.Framework.Windows/System/Power/README.md).

### Web & Cross-Platform

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.Web** | HTML, sitemap and menu models without an ASP.NET Core dependency | [View](../src/Sphere10.Framework.Web/README.md) |
| **Sphere10.Framework.Web.AspNetCore** | Shared hosting, dependency injection, logging, HTTP/session utilities and middleware | [View](../src/Sphere10.Framework.Web.AspNetCore/README.md) |
| **Sphere10.Framework.Web.AspNetCore.MVC** | MVC results, Razor view helpers, forms, filters and routing attributes | [View](../src/Sphere10.Framework.Web.AspNetCore.MVC/README.md) |
| **Sphere10.Framework.Web.AspNetCore.Blazor** | Interactive Server components for the ApplicationBlock workspace, CRUD grid, custom editors, dialogs and wizards | [View](../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) |
| **Sphere10.Framework.Drawing** | Color and geometry helpers, plus Windows-only image processing and GDI+ rendering | [View](../src/Sphere10.Framework.Drawing/README.md) |

**[Sphere10.Framework.Web.VisualRenderer](../src/Sphere10.Framework.Web.VisualRenderer/README.md)** renders an independent visual model to static HTML and plain text. Its [documentation](visual-renderer/README.md) covers the object model, default/CMS templates, embedded assets and source adapters. It is included in both Framework solutions and packaged as `Sphere10.Framework.Web.VisualRenderer`.

### Legacy Platforms

These Xamarin-era projects are not part of either solution, so the solution builds do not compile them. The macOS folder has no project file.

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.iOS** | Legacy Xamarin.iOS integration | [View](../src/Sphere10.Framework.iOS/README.md) |
| **Sphere10.Framework.Android** | Legacy Xamarin.Android integration | [View](../src/Sphere10.Framework.Android/README.md) |
| **Sphere10.Framework.macOS** | Legacy macOS integration source | [View](../src/Sphere10.Framework.macOS/README.md) |

### Testing Libraries

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.NUnit** | NUnit assertion helpers and test infrastructure | [View](../src/Sphere10.Framework.NUnit/README.md) |
| **Sphere10.Framework.NUnit.DB** | Database test fixtures for `Sphere10.Framework.Data` providers | [View](../src/Sphere10.Framework.NUnit.DB/README.md) |

### Demos, Profilers and Tests

| Project | Description | README |
|---------|-------------|--------|
| **Sphere10.Framework.Utils.WinFormsTester** | WinForms demonstrations of screens, ObjectSpaces, VisualGrid, charts, the hex editor and wizards | [View](../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) |
| **Sphere10.Framework.Utils.BlazorTester** | Blazor Web App that presents every component demo in one ApplicationBlock shell | [View](../utils/Sphere10.Framework.Utils.BlazorTester/README.md) |
| **Sphere10.Framework.Utils.MvcTester** | Minimal ASP.NET Core MVC host for the MVC library | [View](../utils/Sphere10.Framework.Utils.MvcTester/README.md) |
| **Sphere10.Framework.Utils.Profiler** | Profiling console with [ObjectSpace](../utils/Sphere10.Framework.Utils.Profiler/ObjectSpaces/README.md), [B-tree](../utils/Sphere10.Framework.Utils.Profiler/BTrees/README.md) and [cache](../utils/Sphere10.Framework.Utils.Profiler/Caches/README.md) domains and [parameter studies](../utils/Sphere10.Framework.Utils.Profiler/Studies/README.md) | [View](../utils/Sphere10.Framework.Utils.Profiler/README.md) |
| **Sphere10.Framework.Application.Tests** | UI-independent application, plugin, block and lifecycle contracts | [View](../tests/Sphere10.Framework.Application.Tests/README.md) |
| **Sphere10.Framework.Windows.Forms.Tests** | Windows-only tests of the native application framework, screen hosting, controls and wizards | [View](../tests/Sphere10.Framework.Windows.Forms.Tests/README.md) |
| **Sphere10.Framework.Web.AspNetCore.Blazor.Tests** | Blazor library, shell and gallery samples, plus explicitly selected Playwright browser fixtures | [View](../tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md) |
| **Sphere10.Framework.Utils.Profiler.Tests** | Profiler commands, workloads, study contracts and optional recorded evidence | [View](../tests/Sphere10.Framework.Utils.Profiler.Tests/README.md) |

---

## 📊 Performance Records

These records document the October 2026 performance campaigns and the correctness reviews that followed them. Each record describes the source snapshot it examined, and later records revise or revert some earlier changes. Current APIs are documented in the project READMEs and pattern topics, not in these records.

### Storage and I/O Allocations

The [comment and behavior review](performance/comment-and-behavior-review.md) lists which of these changes were retained with their evidence and which were reverted.

| Date | Record | Subject |
|---|---|---|
| 22 Oct | [Span and buffer allocation optimizations](performance/span-optimizations.md) | First allocation pass over extended lists, buffers, transactional files, fragmented streams and cryptography |
| 22 Oct | [Stream-mapped dictionary hot paths](performance/stream-mapped-dictionary-hotpaths.md) | Profiling of the long-running stream-mapped dictionary integration tests |
| 22 Oct | [ObjectSpace hot paths](performance/object-space-hotpaths.md) | ObjectSpace and Merkleized store profiling, covering selector compilation, Merkle-root publication and digest churn |
| 22 Oct | [ObjectSpace page reuse and tree selection](performance/object-space-page-btree.md) | Pooled transactional page backing, span consolidation and B-tree implementation selection |
| 23 Oct | [Span inheritance review](performance/span-inheritance-review.md) | Removal of the exact-type subclass checks added by the span pass |
| 23 Oct | [Implementation complexity review](performance/optimization-complexity-review.md) | Removal of transport-mode helpers, with array operations delegating to span overloads |
| 23 Oct | [Stream buffer allocation audit](performance/stream-buffer-allocation-audit.md) | Temporary buffers in paged collections, streams, I/O and transactions |
| 23 Oct | [Streamed B-tree node I/O](performance/btree-span-node-io.md) | Span-based B-tree node and serializer buffers, with matched allocation captures |
| 24 Oct | [Comment and behavior review](performance/comment-and-behavior-review.md) | Retained and reverted changes from the records above, with restored comments |

### Scopes and Disposal

Each record in this group builds on the one before it. [Resource scopes](guidelines/patterns/resource-scopes.md) documents the current scope API.

| Date | Record | Subject |
|---|---|---|
| 24 Oct | [Value scope performance](performance/value-scopes.md) | Struct read, write, access and page scopes, with allocation, GC and consumer measurements |
| 24 Oct | [Scope abstraction migration](performance/scope-abstraction.md) | Migration of the remaining scope families and screening of the `CallContext` registry |
| 25 Oct | [Direct scope finalization](performance/scope-composition.md) | Removal of scope lifecycle machinery and the ownership corrections it exposed |
| 25 Oct | [Async disposal corrections](performance/async-disposal-corrections.md) | Async disposal that awaits the actual cleanup, thread-affine scopes and typed scope factories |
| 25 Oct | [Callback scope consolidation](performance/callback-scope-consolidation.md) | Consolidated callback scopes and their synchronous and asynchronous disposal contracts |
| 25 Oct | [Borrowed array scopes](performance/borrow-array-scopes.md) | `BorrowArrayScope<T>` ownership of pooled arrays and the migration of production rental sites |
| 25–26 Oct | [Disposable template review](performance/disposable-template-review.md) | Class-owner disposal templates, three cleanup failure fixes and clustered-stream close retry |

The [evidence folder](performance/evidence/) archives captures, harness sources and validation logs for several of these records. The scope abstraction evidence includes [reproduction steps](performance/evidence/20261024-scope-abstraction/REPRODUCING.md), and the value scope evidence includes its [frozen measurement harness](performance/evidence/20261024-value-scopes/harness/README.md).

---

## 📂 Documentation Structure

| Folder | Contents |
|---|---|
| `docs/` | This index, orientation, the Tools reference, usage examples and the Blazor theme sources |
| [`architecture/`](architecture/) | Architecture overview and domain catalog, with draw.io and PlantUML diagram sources and their images in `resources/` |
| [`guidelines/`](guidelines/) | Code styling, 3-tier architecture and the patterns-and-practices index, with diagram sources and images in `resources/` |
| [`guidelines/patterns/`](guidelines/patterns/) | One implementation practice per file |
| [`usage/`](usage/) | Focused guides to individual APIs |
| [`performance/`](performance/) | Dated performance campaigns and reviews |
| [`performance/evidence/`](performance/evidence/) | Archived captures, harness sources, validation logs and reproduction notes |

---

## 🛠️ Tools.* Namespace

Utility facades live in the global `Tools` namespace, so typing `Tools.` lists the facades declared by the assemblies a project references. Some operations belong to domain types instead of a facade. Hashing, for example, goes through `Hashers`.

```csharp
using System.Text;
using Sphere10.Framework;

// Sphere10.Framework
byte[] data = Encoding.UTF8.GetBytes("Hello");
byte[] digest = Hashers.Hash(CHF.SHA2_256, data);
byte[] salt = Tools.Crypto.GenerateCryptographicallyRandomBytes(16);
string slug = Tools.Url.ToUrlSlug("Hello World");
string identifier = Tools.Text.ToCasing(TextCasing.PascalCase, "order line total");
string compact = " a b c ".RemoveWhitespace(); // an extension method, not a Tools member

// Sphere10.Framework.Data.Sqlite
var dac = Tools.Sqlite.Open("data.db");

// Sphere10.Framework.Windows (Windows only)
bool installed = Tools.Windows.Services.IsServiceInstalled("MyService");
```

See the [Tools Reference](tools-reference.md) for each facade, its declaring project and its representative members.

---

## 🔗 Quick Links

- [Main README](../README.md) — Project overview, installation and package publishing
- [CI test guide](../.github/ci.md) — How GitHub Actions builds and runs the tests, and how to reproduce a run locally
- [Package consumer validation](../scripts/package-consumers/README.md) — What `validate-packages.ps1` checks before a release
- [NuGet publishing](nuget-publishing.md) — Continuous prereleases, alpha/stable tags, OIDC setup, package provenance and recovery
- [Test projects](../tests/) — NUnit test projects for the libraries

---

## ⚖️ License

Sphere10 Framework is distributed under the **MIT License**.

See [LICENSE](../LICENSE) for details.
