# Resource Scopes

Reuse the scope that owns resource release, state exit or lock release.

## Scope-Based Patterns

The framework uses **disposable scope objects** (`using` blocks) extensively for resource management, synchronization, and state transitions. **Always follow this pattern**:

- **Thread synchronization** — [SynchronizedObject](../../../src/Sphere10.Framework/Threading/SynchronizedObject.cs) provides `EnterReadScope()` / `EnterWriteScope()` returning `IDisposable`. Many synchronized collection members self-lock; use outer scopes for compound operations, deferred reads and borrowed views. Some families require explicit scopes. Read [synchronization](synchronization.md) for the actual family behavior, shared parents and thread-affinity limits.
- **Access scopes** — [ICriticalObject.EnterAccessScope()](../../../src/Sphere10.Framework/Threading/ICriticalObject.cs) is used by `ObjectSpace`, `ClusteredStreams` and stream-mapped storage; [CriticalObject](../../../src/Sphere10.Framework/Threading/CriticalObject.cs) supplies monitor-backed access scopes.
- **[Tools.Scope](../../../src/Sphere10.Framework/Scopes/ScopeTool.cs)** — utility for ad-hoc cleanup scopes:
  - `Tools.Scope.ExecuteOnDispose(action)` — runs `action` when the scope is disposed.
  - `Tools.Scope.DeleteFileOnDispose(path)` — auto-cleanup temp files.
- **Transactional scopes** — [TransactionalScopeBase<TTransaction>](../../../src/Sphere10.Framework/Transactions/TransactionalScopeBase.cs) centralizes transaction ownership, nesting, commit/rollback coordination and close hooks. Use a concrete backend scope; the common base is abstract and does not create a backend transaction by itself. Read [persistence and transactions](persistence-and-transactions.md) for capability selection, nested participants, backend completion and disposal.
- **[ActionScope](../../../src/Sphere10.Framework/Scopes/ActionScope.cs)** / **[TaskScope](../../../src/Sphere10.Framework/Scopes/TaskScope.cs)** — delegate-backed scopes through `SyncScope` / `AsyncScope`. Their generic variants also carry an `Item`.

```csharp
using (collection.EnterWriteScope()) {
	if (!collection.Contains(item))
		collection.Add(item);
}
```

If an operation acquires a resource, enters a state, or needs guaranteed cleanup, wrap it in the existing scope that expresses that acquire/release contract. Do not reproduce its machinery with a bare `try/finally`. Exception handling and multi-step lifecycle coordination can still require `try`/`catch`/`finally`; preserve their ownership and failure behavior and match the underlying scope contract.

## Cleanup review and domain scopes

Treat cleanup-only `try/finally` in newly written or changed code as a review
defect. Put its exit action in the appropriate existing scope, declared
immediately after acquisition; use `await using` when completion requires
awaiting cleanup. Review native memory, handles, subscriptions, temporary files,
UI busy state and cancellation/task lifetimes, including failure and early-return
paths. Keep `try/catch` where it supplies operation-specific recovery or reporting.
Do not remove cancellation draining or change which failure is reported merely
to shorten a method.

Use `Tools.Scope.ExecuteOnDispose` for a one-off exit action. When the same
resource protocol is repeated, define a named scope in the resource's owning
domain, extend the established directional scope base and implement its end
hook. Keep application state/workflow scopes in the application; reusable
native-resource scopes belong in the Windows library. Prefer existing safe
handles when the native API already has an appropriate handle type.

[UnmanagedMemoryScope](../../../src/Sphere10.Framework.Windows/Misc/UnmanagedMemoryScope.cs)
owns `Marshal.AllocHGlobal` and `Marshal.StringToHGlobalUni` allocations. It must
not free a buffer returned by an API requiring `NetApiBufferFree`, `LocalFree`,
or another allocator. Register the correct API release in its own scope instead.

```csharp
using var allocation = new UnmanagedMemoryScope(bufferSize);
var pointer = allocation.Item;
// Marshal/native operations use pointer only within this scope.
```

Declare scopes in dependency order so disposal reverses acquisition: a native
state or marshalled structure must be closed/destroyed before its backing
allocation is freed; native callbacks must remain rooted until the consumer
task has drained. Resource transfer needs an explicit retained/committed flag
so rollback disposes only resources still owned by the acquiring operation.
Thread-affine cleanup must run on its acquiring thread; use the synchronous-only
scope base for those protocols rather than inheriting an asynchronous adapter
that can dispatch release to another worker.

The example assumes an existing synchronized collection with the default recursion policy. The outer write scope keeps the membership check and addition atomic even when each member self-locks. [IScope](../../../src/Sphere10.Framework/Scopes/IScope.cs) exposes both disposal forms and `ScopeEnd`; [ScopeBase](../../../src/Sphere10.Framework/Scopes/ScopeBase.cs) runs its end hook before the notification. Its async notification uses `Task.Run`, so the generic scope contract does not promise the acquiring thread.

Use `using` for synchronous cleanup and `await using` for asynchronous cleanup. Reader/writer and monitor-backed scopes are thread-affine: dispose them on the acquiring thread and do not retain them across asynchronous suspension that can change threads. An async disposal member on a generic scope does not make lock release safe on a worker.

A scope owns its exit action; it does not imply ownership of every object used inside it. The transaction scope that creates the backend transaction owns its underlying commit/rollback and close. A nested participant's explicit rollback records a rollback vote for that owner. Merely disposing an unfinished child does not automatically cast that vote, and the common disposal path closes an owned transaction without itself calling `Rollback`. Inspect the concrete backend's behavior and roll back abandoned work explicitly when its contract requires it. [TransactionalScopeInterfaceTests](../../../tests/Sphere10.Framework.Tests/Scopes/TransactionalScopeInterfaceTests.cs) show nested ownership; [FileTransactionScope](../../../src/Sphere10.Framework/IO/FileTransactionScope.cs) is a concrete synchronous backend example.

The current [AsyncScope](../../../src/Sphere10.Framework/Scopes/AsyncScope.cs), [AsyncDisposable](../../../src/Sphere10.Framework/Disposables/AsyncDisposable.cs) and [AsyncTransactionalScope<TTransaction>](../../../src/Sphere10.Framework/Transactions/AsyncTransactionalScope.cs) synchronous bridges use [WaitSafe](../../../src/Sphere10.Framework/Extensions/TaskExtensions.cs), which does not join its queued wait. They do not establish awaited cleanup or transaction completion merely because the synchronous method returns. Follow the [adapter completion rules](sync-async.md#adapter-completion-and-cancellation) when changing these legacy paths.

[Disposables](../../../src/Sphere10.Framework/Disposables/Disposables.cs) implements `IDisposable`, with `Add(Action)` and `Add(Func<Task>)` cleanup helpers. The latter wraps [TaskDisposable](../../../src/Sphere10.Framework/Disposables/TaskDisposable.cs); it does not turn the bag into an async disposable or guarantee awaiting through the legacy sync bridge. Use the owner's actual async cleanup path for async resources. See [application ownership](application-ownership.md) for the running application's bag and borrowed services.
