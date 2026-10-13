---
name: oo-design
description: Object-oriented design idioms of this codebase — interface → abstract base → concrete → decorator layering, long-index range delegation, scopes, events, factories/builders, Tools.* statics. Trigger when designing any new type, hierarchy, or service.
---

# Object-Oriented Design Skill

How types are structured in this codebase. Match these idioms so new code blends in. Canonical examples are cited with paths.

## 1. Contract, inheritance, implementation and decoration
The common layering is **Interface → Abstract base → Concrete → Decorator**, with each layer serving a distinct responsibility:

| Layer | Role | Example |
|---|---|---|
| `IFoo` | Contract, including inline base-interface projections where applicable | `IExtendedList<T>`, `IDataSource<T>`, `ILogger`, `IItemSerializer<T>`, `IBlockchain<...>` |
| `FooBase` / `FooBase<T>` | Abstract inheritance foundation; shares invariants, algorithms and hooks, leaving implementation primitives abstract | `ExtendedListBase<T>`, `DataSourceBase<T>`, `LoggerBase`, `ItemSerializerBase<T>`, `BlockchainBase<...>` |
| `Foo` | Concrete implementation | `ExtendedList<T>`, `ListDataSource<T>`, `TextWriterLogger`, `Blockchain<...>` |
| `FooDecorator` | Behavior layering over `IFoo` | `ExtendedListDecorator`, `DataSourceDecorator`, `LoggerDecorator`, `BlockchainDecorator` |

Use these layers when the abstraction needs their responsibilities; do not add unused layers merely to complete the hierarchy. See [decorator-pattern](../decorator-pattern/SKILL.md). For specialized interface property projections, follow [inline interface bridges](../../../docs/guidelines/patterns/interface-bridges.md).

An inheritance foundation is always abstract and named `FooBase`, even when it implements every interface member. A concrete `Foo` is justified by usable behavior and a genuine direct consumer; do not keep a concrete scaffold merely because adapters derive from it or tests instantiate it. Preserve an existing concrete implementation with real consumers, and introduce a separate abstract base when shared inheritance behavior needs a clearer contract. Do not create unused concrete wrappers to complete the hierarchy mechanically. Simple values, immutable definitions and directly usable actions do not require artificial abstract bases. Follow [the type hierarchy guide](../../../docs/guidelines/patterns/type-hierarchies.md).

## 2. Intermediate "delegation" bases
Insert an abstract base whose **single-item operations delegate to batch/range operations** so subclasses override only the range methods:
- `RangedListBase<T>` — `Add`/`Read`/`Update`/`RemoveAt` call `*Range` counterparts.
- `SyncBatchDataSourceBase<T>` / `AsyncBatchDataSourceBase<T>` — item methods delegate to `*Range`, and a directional base adapts the opposite sync/async API. Follow the execution-direction rules below for adapter completion and cancellation semantics.

Choose item-first or range-first according to the real implementation. A range-first base implements item operations through ranges; an item-first base implements ranges through item operations. `SyncItemDataSourceBase<T>` and `AsyncItemDataSourceBase<T>` provide the latter. Reuse the existing base instead of making both forms forward to each other.

### Sync/async execution direction

Follow the [directional implementation guide](../../../docs/guidelines/patterns/sync-async.md#synchronous-and-asynchronous-implementation-hierarchies). The architectural reference is `IRepository` → `RepositoryBase` → `SyncRepositoryBase` / `AsyncRepositoryBase`.

- The interface declares both APIs; the common abstract base leaves both execution primitives abstract and owns shared invariants, events and state.
- `SyncFooBase` adapts async calls to the sync primitive; `AsyncFooBase` adapts sync calls to the async primitive. Concrete classes override their chosen primitive instead of repeating bridges.
- Use the current application initializer/finalizer bases for completion-guaranteed adapter examples. An async-first blocking bridge starts the async factory outside the caller's synchronization context and waits for its completion. The current `WaitSafe` queues a wait without joining it; `ResultSafe` receives an already-started task. Follow the guide's completion rules rather than copying those older adapters.
- Preserve cancellation semantics, the chosen disposal direction and UI thread affinity. Do not use a boolean execution mode, mutual forwarding, `async void` or discarded operation tasks.
- Default interface bridges project specialized properties or compatible members through their base contract. Keep new bridges in place under `#region Base Interface Bridges`, without helper classes; execution-direction adapters belong in the directional classes. See [IApplication](../../../src/Sphere10.Framework.Application/UI/IApplication.cs) and [ISynchronizedObject](../../../src/Sphere10.Framework/Threading/ISynchronizedObject.cs).

## 3. `long`-indexed, range-first APIs
The extended collection contracts are `long`-indexed (`IExtendedCollection<T>` / `IExtendedList<T>`) with `*Range` methods (`ReadRange`, `InsertRange`, `RemoveRange`, `UpdateRange`). Preserve their `long` counts and indices; inherited `int` members bridge to the wider contract. This does not change the `int` paging parameters of `IDataSource<T>` or the required signatures of BCL collection contracts.

For collection work, read [collection selection and composition](../../../docs/guidelines/patterns/collections.md) before choosing a base or wrapper. For repository and load/save lifecycle work, read [persistence and transactions](../../../docs/guidelines/patterns/persistence-and-transactions.md); CRUD, saving and transaction completion have distinct owners.

## 4. Composition of orthogonal behavior via decorators
Use decorators for optional, orthogonal behavior such as synchronization, observation, persistence and read-only access. Existing collection families also provide transactional, merkle and recyclable wrappers. Extend the established decorator for that abstraction rather than adding unrelated mode flags to its inheritance foundation; do not create wrappers that have no consumer.

## 5. Scopes for resource/state
Expose disposable scopes for synchronization and state instead of lock/unlock methods: `SynchronizedObject.EnterReadScope()` / `EnterWriteScope()`, `ICriticalObject.EnterAccessScope()`, `IBlockchainState.EnterUpdateScope()`. See [guards-and-scopes](../guards-and-scopes/SKILL.md).

## 6. Events
Use the established `EventHandlerEx` or typed `EventHandlerEx<TArgs>` contract for framework events (e.g. `IApplication.Changed` and `IBlockchain.BlockApplied`). Decorators forward `add`/`remove` to the inner object rather than re-raising.

## 7. Validation & invariants
Use `Guard.ArgumentNotNull(x, nameof(x))` and the corresponding `Guard` checks for arguments, and `Guard.Ensure(...)` for internal invariants. Domain exceptions and error propagation retain the abstraction's required semantics. See [guards-and-scopes](../guards-and-scopes/SKILL.md).

## 8. Statics vs instances
- **Stateless algorithms/helpers** → `static class` in the global `Tools` namespace (`Tools.Array`, `Tools.Text`, `Tools.Crypto`). See [tools-namespace](../tools-namespace/SKILL.md).
- **Registries / shared singletons** → a `.Default` static instance (`SerializerFactory.Default`, `ComparerFactory.Default`, `Hashers`).
- **Complex configuration** → fluent `FooBuilder` with terminal `.Build()`. See [builder-pattern](../builder-pattern/SKILL.md).

## 9. Generics & self-describing names
Heavy use of generic type parameters for flexibility (`Blockchain<TBlock, TState, TBlockID, TWeight, TOperationID>`). Prefer explicit generic parameters over `object`/`dynamic`. Names are long and self-describing.

Place each `where` constraint on a separate line aligned one tab below the declaration, with the opening brace on the last constraint line. Follow [code-style](../code-style/SKILL.md) in source, tests and examples.

## Anti-patterns to avoid
- Adding unused interfaces, bases, concrete wrappers or decorators merely to complete a hierarchy.
- Selecting execution direction through a boolean mode or mutual forwarding; bypassing existing decorators for optional cross-cutting behavior.
- Narrowing extended collection indices to `int` without a checked compatibility bridge.
- Hand-written argument guards, logging through `Console.WriteLine`, or raw BCL crypto/bit conversion where a suitable framework equivalent exists.
