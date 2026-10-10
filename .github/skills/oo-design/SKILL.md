---
name: oo-design
description: Object-oriented design idioms of this codebase — interface → abstract base → concrete → decorator layering, long-index range delegation, scopes, events, factories/builders, Tools.* statics. Trigger when designing any new type, hierarchy, or service.
---

# Object-Oriented Design Skill

How types are structured in this codebase. Match these idioms so new code blends in. Canonical examples are cited with paths.

## 1. The four-layer hierarchy
Almost every abstraction follows **Interface → Abstract base → Concrete → Decorator**:

| Layer | Role | Example |
|---|---|---|
| `IFoo` | Contract only | `IExtendedList<T>`, `IDataSource<T>`, `ILogger`, `IItemSerializer<T>`, `IBlockchain<...>` |
| `FooBase` / `FooBase<T>` | Abstract; declares members `abstract`, wires shared concerns (events) | `ExtendedListBase<T>`, `DataSourceBase<T>`, `LoggerBase`, `ItemSerializerBase<T>`, `BlockchainBase<...>` |
| `Foo` | Concrete implementation | `ExtendedList<T>`, `ListDataSource<T>`, `TextWriterLogger`, `Blockchain<...>` |
| `FooDecorator` | Behavior layering over `IFoo` | `ExtendedListDecorator`, `DataSourceDecorator`, `LoggerDecorator`, `BlockchainDecorator` |

Design new abstractions as all four layers, not a single concrete class. See [decorator-pattern](../decorator-pattern/SKILL.md).

An inheritance foundation is always abstract and named `FooBase`, even when it implements every interface member. A concrete `Foo` is justified by usable behavior and a genuine direct consumer; do not keep a concrete scaffold merely because adapters derive from it or tests instantiate it. Preserve an existing concrete implementation with real consumers, and introduce a separate abstract base when shared inheritance behavior needs a clearer contract. Do not create unused concrete wrappers to complete the hierarchy mechanically. Simple values, immutable definitions and directly usable actions do not require artificial abstract bases. Follow [Patterns and Practices](../../../docs/guidelines/patterns-and-practices.md).

## 2. Intermediate "delegation" bases
Insert an abstract base whose **single-item operations delegate to batch/range operations** so subclasses override only the range methods:
- `RangedListBase<T>` — `Add`/`Read`/`Update`/`RemoveAt` call `*Range` counterparts.
- `SyncBatchDataSourceBase<T>` / `AsyncBatchDataSourceBase<T>` — item methods delegate to `*Range`, and one direction (sync or async) wraps the other via `Task.Run` / `.ResultSafe()`.

When you see a "single vs batch" duality, implement the primitive in terms of the batch and let one base bridge them.

## 3. `long`-indexed, range-first APIs
Collections are `long`-indexed (`IExtendedCollection<T>` / `IExtendedList<T>`) with `*Range` methods (`ReadRange`, `InsertRange`, `RemoveRange`, `UpdateRange`). Prefer `long` counts/indices and range operations over `int` and single-item loops. Legacy `int` members are kept only as shims that call the `long` equivalents.

## 4. Composition of orthogonal behavior via decorators
Cross-cutting concerns are decorators, not base-class flags: **Synchronized**, **Observable**, **Transactional**, **Merkle**, **Recyclable**, **ReadOnly** variants all wrap an inner instance (`SynchronizedList<T>`, `SynchronizedDictionary`, `PersistableDecorator`). Prefer wrapping over inheritance for optional behavior.

## 5. Scopes for resource/state
Expose disposable scopes for synchronization and state instead of lock/unlock methods: `SynchronizedObject.EnterReadScope()` / `EnterWriteScope()`, `ICriticalObject.EnterAccessScope()`, `IBlockchainState.EnterUpdateScope()`. See [guards-and-scopes](../guards-and-scopes/SKILL.md).

## 6. Events
Use `EventHandlerEx<TArgs>` for events (e.g. `IBlockchain.BlockApplied`). Decorators forward `add`/`remove` to the inner object rather than re-raising.

## 7. Validation & invariants
`Guard.*` everywhere — never inline `if/throw`. `Guard.ArgumentNotNull(x, nameof(x))` for args, `Guard.Ensure(...)` for internal invariants. See [guards-and-scopes](../guards-and-scopes/SKILL.md).

## 8. Statics vs instances
- **Stateless algorithms/helpers** → `static class` in the global `Tools` namespace (`Tools.Array`, `Tools.Text`, `Tools.Crypto`). See [tools-namespace](../tools-namespace/SKILL.md).
- **Registries / shared singletons** → a `.Default` static instance (`SerializerFactory.Default`, `ComparerFactory.Default`, `Hashers`).
- **Complex configuration** → fluent `FooBuilder` with terminal `.Build()`. See [builder-pattern](../builder-pattern/SKILL.md).

## 9. Generics & self-describing names
Heavy use of generic type parameters for flexibility (`Blockchain<TBlock, TState, TBlockID, TWeight, TOperationID>`). Prefer explicit generic parameters over `object`/`dynamic`. Names are long and self-describing.

Place each `where` constraint on a separate line aligned one tab below the declaration, with the opening brace on the last constraint line. Follow [code-style](../code-style/SKILL.md) in source, tests and examples.

## Anti-patterns to avoid
- A single concrete class with no interface/base/decorator when the concept is reusable.
- Putting optional behavior (sync, read-only, merkle) as a boolean flag or `virtual` on the base.
- `int` indexing on new collection-like types.
- Inline `if/throw`, `Console.WriteLine`, raw BCL crypto/bit-conversion — use the framework equivalents.
