---
name: disposable-scopes
description: Disposable scoping idiom — IScope/ScopeBase hierarchy, Tools.Scope helpers, SynchronizedObject read/write scopes, ICriticalObject access scopes, ContextScope call-context nesting, TransactionalScopeBase commit/rollback. Trigger whenever an operation acquires a resource, enters a state, locks, or needs guaranteed cleanup.
---

# Disposable Scopes Skill

Read [resource scope guidance](../../../docs/guidelines/patterns/resource-scopes.md) for cleanup, synchronization and transaction ownership.

Scoping is a **first-class design idiom** here. Reuse the scope that owns resource release, state exit or lock release, and consume it with `using` or `await using`. Do not duplicate an existing scope's acquire/release machinery. Exception handling and multi-step lifecycle coordination can still require `try`/`catch`/`finally`; do not mechanically replace them with a scope that has different ownership or failure behavior.

Core principle: **the scope's `Dispose` (or `DisposeAsync`) performs the exit/cleanup**, and subclasses/overrides hook the open and close transitions.

## 1. The `IScope` hierarchy (`src/Sphere10.Framework/Scopes/`)

```csharp
public interface IScope : IDisposable, IAsyncDisposable {
	event EventHandlerEx ScopeEnd;
}

public interface IScope<out T> : IScope {
	T Item { get; }
}
```

- [ScopeBase](../../../src/Sphere10.Framework/Scopes/ScopeBase.cs) extends `Disposable` and `IScope`. Its synchronous and asynchronous end hooks are abstract; each disposal direction runs its hook and raises `ScopeEnd` after successful completion. The async notification is scheduled with `Task.Run`, so it does not promise the caller's thread. See [oo-design](../oo-design/SKILL.md).
- Sync/async split follows the codebase dual-sync/async convention:
  - `SyncScope` / `AsyncScope` (abstract bases),
  - `ActionScope` (`SyncScope`, runs an `Action` on end) and `TaskScope` (async equivalent),
  - generic `ActionScope<T>` / `TaskScope<T>` also carry an `Item` (`IScope<T>`).
- `ScopeDecorator<TScope>` / `ContextScopeDecorator<TScope>` forward the inner scope contract (see [decorator-pattern](../decorator-pattern/SKILL.md)).

Use `using` for synchronous cleanup and `await using` for asynchronous cleanup. The existing `AsyncScope` and `AsyncDisposable` synchronous adapters use `WaitSafe`, whose current implementation does not join its queued wait. Do not assume a synchronous `Dispose()` on an async scope waits for cleanup. Follow [the adapter completion rules](../../../docs/guidelines/patterns/sync-async.md#adapter-completion-and-cancellation) when changing these older bridges; neither disposal nor event notification should report completed cleanup while work remains pending.

## 2. Ad-hoc cleanup — `Tools.Scope` (`Scopes/ScopeTool.cs`)

Use [Tools.Scope](../../../src/Sphere10.Framework/Scopes/ScopeTool.cs) for one-off cleanup:

```csharp
using var cleanup = Tools.Scope.ExecuteOnDispose(() => ownedResource.Dispose());
using (Tools.Scope.DeleteFileOnDispose(temporaryPath)) {
	// Create and use the temporary file within the cleanup scope.
}
await using var directoryCleanup = Tools.Scope.DeleteDirOnDisposeAsync(temporaryDirectory);
```

Available: `ExecuteOnDispose(Action)`, `ExecuteOnDisposeAsync(Func<Task>)`, prefetched-value overloads `ExecuteOnDispose<T>(Action<T>, T)` and `ExecuteOnDispose<T>(Func<T, Task>, T)`, plus `DeleteFileOnDispose[Async]` and `DeleteDirOnDispose[Async]`. The async prefetched-value overload is named **`ExecuteOnDispose`**, without an `Async` suffix, and returns `IAsyncDisposable`.

## 3. Thread synchronization — `SynchronizedObject` (`Threading/SynchronizedObject.cs`)

For read/write locking, read [synchronization](../../../docs/guidelines/patterns/synchronization.md). Inspect whether the selected member self-locks, then choose the outer scope needed for compound operations, deferred ranges or borrowed views. Configure shared parents before use and keep lock acquisition and disposal on the same thread.

For exclusive access, use the actual resource's `ICriticalObject.EnterAccessScope()` contract described below. A generic scope's async disposal surface does not change the underlying lock's thread affinity.

## 4. Mutually-exclusive access — `ICriticalObject` / `CriticalObject` (`Threading/`)

For exclusive (not read/write) access: `IDisposable EnterAccessScope()`. Used by `ClusteredStreams`, `ObjectSpace`, stream-mapped collections:

```csharp
using (streams.EnterAccessScope()) {
	using var stream = streams.Add();
	// Write to the newly allocated clustered stream.
}
```

The monitor-backed access scope is also thread-affine; keep it on the acquiring thread and do not hold it across asynchronous suspension.

## 5. Call-context nesting — `ContextScope` (`Scopes/ContextScope/`)

Scopes that are **aware of other scopes active in the same logical call context**, keyed by a `ContextID`. The framework's [CallContext](../../../src/Sphere10.Framework/Scopes/ContextScope/CallContext.cs) implements `LogicalGetData`/`LogicalSetData` with `AsyncLocal` storage. Nesting shares context; the concrete scope supplies its state and transaction semantics.

- Policy: `ContextScopePolicy.MustBeRoot`, `MustBeNested` or `None` to permit either.
- `RootScope` / `IsRootScope` identify the outermost scope; hooks `OnContextStart` (root entered), `OnContextResume` (nested entered), `OnContextEnd` (root disposed).
- Variants: `SyncContextScope`, `AsyncContextScope`, `ActionContextScope`, `TaskContextScope`.

## 6. Transactions — `TransactionalScopeBase<TTransaction>` (`Transactions/`)

For transactional work, read [persistence and transactions](../../../docs/guidelines/patterns/persistence-and-transactions.md). Select the existing concrete backend scope or its directional extension base, identify the transaction owner, and verify begin/commit/rollback and close behavior before consuming it. Inspect nested rollback voting and backend disposal rather than assuming a scope supplies automatic rollback or durability.

Use the [persistence skill](../persistence/SKILL.md) when repository or load/save lifecycle contracts also participate. A collection's own transactional backend is a separate owner from an unrelated transaction scope.

## 7. Owned resource bags

[Disposables](../../../src/Sphere10.Framework/Disposables/Disposables.cs) groups owned resources and supports cleanup delegates. It implements `IDisposable`; do not assume the bag itself exposes `DisposeAsync` or guarantees awaited disposal of `TaskDisposable`. Use the owner's actual async cleanup path for async resources. The running application exposes an owned bag, but borrowed hosts and DI-managed services retain their own disposal owner; see [application ownership](../../../src/Sphere10.Framework.Application/README.md#application-initialization-and-shutdown).

## Rules of thumb

1. Return or consume the existing scope for resource/state ownership; choose a new scope only when the existing contracts cannot express the required exit behavior.
2. Consume with `using` or `await using` according to the cleanup direction, and keep its lifetime no longer than the protected work requires.
3. Reuse `ScopeBase` or the established directional scope bases; use `ContextScope` when logical-context nesting is required. New inheritance-only foundations are abstract and use the `Base` suffix; retain existing public family names.
4. Hooks are `OnScopeEnd`/`OnContextStart`/etc. — not logic in `Dispose` directly.
5. Prefer `Tools.Scope.*` factories for ad-hoc cleanup over anonymous `try/finally`.
6. Follow [code-style](../code-style/SKILL.md) (Egyptian braces, tabs) and use `Guard.*` for scope-constructor args (see [guards-and-scopes](../guards-and-scopes/SKILL.md)).
