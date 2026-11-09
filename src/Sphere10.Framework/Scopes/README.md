# Scopes

Disposable scope values that release a lock, rental, page, stream position or custom finalizer, plus call-context scopes that share one root across nested calls.

`IScope` is `IDisposable` and `IAsyncDisposable`, and `IScope<T>` adds the `Item` the scope guards. Most scopes are `readonly struct` values with one owner, normally a `using` local, and must be disposed exactly once. Each scope states which disposal it supports. `MonitorScope`, `AccessScope`, `ProducerConsumerScope`, `ReadScope` and `WriteScope` hold thread-affine locks, so they must be disposed synchronously on the acquiring thread, and their `DisposeAsync` returns a faulted task. `SyncScope` only supports `Dispose`, and `AsyncScope` only supports `DisposeAsync`. `ActionScope` and `TaskScope` support both, with `TaskScope.Dispose` blocking on its asynchronous finalizer. Prefer the factory methods on `Tools.Scope`, such as `ExecuteOnDispose`, `EnterBorrowArrayScope`, `EnterRestorePositionScope`, `EnterLockScope` and `DeleteFileOnDispose`, over calling constructors.

The `ContextScope` subfolder holds class-based scopes that register themselves in `CallContext` under a context name. The first scope created for a name becomes the root, and scopes created inside its call or async flow join it. Only the root runs the context finalizer, and every scope runs its own scope finalizer. `ContextScopePolicy` can demand that a scope is the root or nested. The transactional scopes in [Transactions](../Transactions/README.md) build on `ContextScope`. The reader/writer lock owners that issue `ReadScope` and `WriteScope` are in [Threading](../Threading/README.md).

```csharp
using (var buffer = Tools.Scope.EnterBorrowArrayScope(4096)) {
	var bytesRead = stream.Read(buffer.Item, 0, buffer.Length);
}

using (Tools.Scope.EnterRestorePositionScope(stream)) {
	stream.Position = 0;
	var header = stream.ReadByte();
}
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AccessScope`](AccessScope.cs) | struct | Holds a critical object's monitor and releases it on synchronous disposal on the acquiring thread. |
| [`ActionScope`](ActionScope.cs) | struct | Runs a synchronous finalizer on `Dispose`, or on a thread-pool task for `DisposeAsync`. |
| [`ActionScope<T>`](ActionScope.cs) | struct | `ActionScope` whose finalizer receives the scope's `Item`. |
| [`AsyncScope`](AsyncScope.cs) | struct | Awaits an asynchronous finalizer on `DisposeAsync` and throws `NotSupportedException` from `Dispose`. |
| [`AsyncScope<T>`](AsyncScope.cs) | struct | `AsyncScope` whose finalizer receives the scope's `Item`. |
| [`BorrowArrayScope<T>`](BorrowArrayScope.cs) | struct | Rents an array from an `ArrayPool<T>` (the shared pool by default), exposes the requested length as `Span`, and returns the array on disposal. |
| [`IScope`](IScope.cs) | interface | Scope contract that combines `IDisposable` and `IAsyncDisposable`. |
| [`IScope<T>`](IScope.cs) | interface | Scope that also exposes the `Item` it guards. |
| [`MonitorScope`](MonitorScope.cs) | struct | Enters a `Monitor` on construction and exits it on synchronous disposal. |
| [`NoOpScope`](NoOpScope.cs) | struct | Scope whose disposal does nothing. |
| [`NoOpScope<T>`](NoOpScope.cs) | struct | Carries a borrowed item without finalizing it. |
| [`PageScope<TItem>`](PageScope.cs) | struct | Keeps a paged-list page open and closes it, or restores its cache purge flag, on disposal. |
| [`ProducerConsumerScope`](ProducerConsumerScope.cs) | struct | Releases the monitor and role semaphore that a `ProducerConsumerLock` acquired. |
| [`ReadScope`](ReadScope.cs) | struct | Releases one read lock of a synchronized object, or runs a cleanup callback, on synchronous disposal. |
| [`ReadScope<TScope>`](ReadScope.cs) | struct | Disposes a caller-selected inner scope and then releases the read lock. |
| [`RestorePositionScope`](RestorePositionScope.cs) | struct | Restores a stream's captured position on disposal without closing the stream. |
| [`SyncScope`](SyncScope.cs) | struct | Runs a synchronous finalizer on `Dispose` and faults `DisposeAsync`. |
| [`SyncScope<T>`](SyncScope.cs) | struct | `SyncScope` whose finalizer receives the scope's `Item`. |
| [`TaskScope`](TaskScope.cs) | struct | Awaits an asynchronous finalizer on `DisposeAsync`, and blocks on it from `Dispose`. |
| [`TaskScope<T>`](TaskScope.cs) | struct | `TaskScope` whose finalizer receives the scope's `Item`. |
| [`Tools.Scope`](ScopeTool.cs) | tools | Factory methods for every scope here, plus lock, semaphore, hasher, page, single-instance and delete-on-dispose scopes. |
| [`WriteScope`](WriteScope.cs) | struct | Releases one write lock of a synchronized object, or runs a cleanup callback, on synchronous disposal. |
| [`WriteScope<TScope>`](WriteScope.cs) | struct | Disposes a caller-selected inner scope and then releases the write lock. |

### ContextScope

| Type | Kind | Purpose |
|---|---|---|
| [`ActionContextScope`](ContextScope/ActionContextScope.cs) | class | Synchronous context scope that runs a context finalizer when the root ends and an optional scope finalizer when each scope ends. |
| [`AsyncContextScope`](ContextScope/AsyncContextScope.cs) | abstract class | Context scope whose finalization hooks are asynchronous, with synchronous disposal blocking on them. |
| [`CallContext`](ContextScope/CallContext.cs) | static class | Named `AsyncLocal` slots that flow with the call and async context, used to find the current root scope. |
| [`ContextScope`](ContextScope/ContextScope.cs) | abstract class | Base scope that becomes the root for its context name or joins an existing root, and runs start, resume and end hooks. |
| [`ContextScopePolicy`](ContextScope/ContextScopePolicy.cs) | enum | Whether a context scope may be root or nested (`None`), must be root (`MustBeRoot`) or must be nested (`MustBeNested`). |
| [`IContextScope`](ContextScope/IContextScope.cs) | interface | Scope with a context name, a policy and the root scope of its context. |
| [`SyncContextScope`](ContextScope/SyncContextScope.cs) | abstract class | Context scope whose finalization hooks are synchronous, with asynchronous disposal running them on a thread-pool task. |
| [`TaskContextScope`](ContextScope/TaskContextScope.cs) | class | Asynchronous context scope that awaits a context finalizer when the root ends and an optional scope finalizer when each scope ends. |
