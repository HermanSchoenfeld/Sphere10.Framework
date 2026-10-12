---
name: disposable-scopes
description: Disposable scoping idiom — IScope/ScopeBase hierarchy, Tools.Scope helpers, SynchronizedObject read/write scopes, ICriticalObject access scopes, ContextScope call-context nesting, TransactionalScopeBase commit/rollback. Trigger whenever an operation acquires a resource, enters a state, locks, or needs guaranteed cleanup.
---

# Disposable Scopes Skill

Scoping is a **first-class design idiom** here: anything that acquires a resource, enters a state, takes a lock, or needs guaranteed cleanup is modelled as a disposable **scope object** consumed in a `using` block. Never use bare `try/finally`, `lock`, or manual open/close pairs when a scope exists.

Core principle: **the scope's `Dispose` (or `DisposeAsync`) performs the exit/cleanup**, and subclasses/overrides hook the open and close transitions.

## 1. The `IScope` hierarchy (`src/Sphere10.Framework/Scopes/`)

```csharp
public interface IScope : IDisposable, IAsyncDisposable {
	event EventHandlerEx ScopeEnd;
}
public interface IScope<out T> : IScope { T Item { get; } }
```

- `ScopeBase : Disposable, IScope` — override `OnScopeEnd()` / `OnScopeEndAsync()`; the base raises `ScopeEnd` after them. (Note `ScopeBase` builds on the framework's `Disposable` base — see [oo-design](../oo-design/SKILL.md).)
- Sync/async split follows the codebase dual-sync/async convention:
  - `SyncScope` / `AsyncScope` (abstract bases),
  - `ActionScope` (`SyncScope`, runs an `Action` on end) and `TaskScope` (async equivalent),
  - generic `ActionScope<T>` / `TaskScope<T>` also carry an `Item` (`IScope<T>`).
- `ScopeDecorator` / `ContextScopeDecorator` layer behavior over an inner scope (see [decorator-pattern](../decorator-pattern/SKILL.md)).

## 2. Ad-hoc cleanup — `Tools.Scope` (`Scopes/ScopeTool.cs`)

Factory for one-off scopes. **Prefer these over hand-writing `try/finally`:**

```csharp
using var _ = Tools.Scope.ExecuteOnDispose(() => File.Delete(path));
using (Tools.Scope.DeleteFileOnDispose(tmpFile)) { /* ... */ }
await using (Tools.Scope.DeleteDirOnDisposeAsync(baseDir)) { /* ... */ }
```

Available: `ExecuteOnDispose(Action)`, `ExecuteOnDisposeAsync(Func<Task>)`, prefetched-value overloads `ExecuteOnDispose<T>(Action<T>, T)` / `ExecuteOnDisposeAsync<T>(Func<T,Task>, T)`, `DeleteFileOnDispose[Async]`, `DeleteDirOnDispose[Async]`.

## 3. Thread synchronization — `SynchronizedObject` (`Threading/SynchronizedObject.cs`)

Wraps a `ReaderWriterLockSlim`. **Never call `EnterReadLock`/`Monitor.Enter` directly** on a `SynchronizedObject`; use the scopes:

```csharp
using (collection.EnterReadScope()) { /* reads */ }
using (collection.EnterWriteScope()) { collection.Add(item); }
```

- `EnterReadScope()` / `EnterWriteScope()` return `IDisposable` that exits the lock on dispose.
- Subclass hooks: `OnReadScopeOpen/Closed`, `OnWriteScopeOpen/Closed`.
- `EnsureReadable()` / `EnsureWritable()` throw `SoftwareException` if a member is accessed outside the correct scope — use them at the top of synchronized members.
- `ParentSyncObject` lets child objects share a parent's lock. Generic variant `SynchronizedObject<TReadScope, TWriteScope>` yields typed `IScope` instances.
- Related primitives: `FastLock`, `NonReentrantLock`, `Tools.Thread` helpers.

## 4. Mutually-exclusive access — `ICriticalObject` / `CriticalObject` (`Threading/`)

For exclusive (not read/write) access: `IDisposable EnterAccessScope()`. Used by `ClusteredStreams`, `ObjectSpace`, stream-mapped collections:

```csharp
using (streams.EnterAccessScope()) {
	streams.Add(stream);
}
```

## 5. Call-context nesting — `ContextScope` (`Scopes/ContextScope/`)

Scopes that are **aware of other scopes active in the same logical call context** (via `CallContext.LogicalGetData/LogicalSetData`), keyed by a `ContextID`. Enables nested scopes to share state and a single root to own the commit/abort.

- Policy: `ContextScopePolicy.MustBeRoot`, `MustBeNested`, or (default) either.
- `RootScope` / `IsRootScope` identify the outermost scope; hooks `OnContextStart` (root entered), `OnContextResume` (nested entered), `OnContextEnd` (root disposed).
- Variants: `SyncContextScope`, `AsyncContextScope`, `ActionContextScope`, `TaskContextScope`.

## 6. Transactions — `TransactionalScopeBase<TTransaction>` (`Transactions/`)

A `ContextScope` implementing ACID commit/rollback for repositories/DACs:

```csharp
using (var scope = new SyncTransactionalScope<ContextScopePolicy>(...)) {
	scope.BeginTransaction();
	// ... work ...
	scope.Commit();   // disposing without Commit rolls back
}
```

- Events: `Committing`, `Committed`, `RollingBack`, `RolledBack`.
- Nested scopes **join the parent's transaction** (shared via `RootScope`); only the owning scope commits/rolls back.
- Sync/async: `SyncTransactionalScope`, `AsyncTransactionalScope`; `FileTransactionScope` for file rollback. `TransactionalScopeDecorator` layers over them.

## Rules of thumb
1. Acquire a resource / enter a state / need cleanup → **return or consume a scope**, not an open/close method pair.
2. Consume with `using` (or `await using` for async), or `using var _ =` when the scope spans the whole method.
3. New scope types derive `ScopeBase` (or `ContextScope` when call-context nesting is needed); sync via `SyncScope`, async via `AsyncScope`.
4. Hooks are `OnScopeEnd`/`OnContextStart`/etc. — not logic in `Dispose` directly.
5. Prefer `Tools.Scope.*` factories for ad-hoc cleanup over anonymous `try/finally`.
6. Follow [code-style](../code-style/SKILL.md) (Egyptian braces, tabs) and use `Guard.*` for scope-constructor args (see [guards-and-scopes](../guards-and-scopes/SKILL.md)).
