# Resource Scopes

A scope owns finalization at an operation's boundary. Use the existing concrete scope with `using` or `await using`. Small cleanup scopes are structs; ambient contexts, transactions and serialization contexts remain classes where their domain requires identity.

## Scope-Based Patterns

- **Read/write synchronization** — `ISynchronizedObject.EnterReadScope()` / `EnterWriteScope()` return `ReadScope` / `WriteScope`. Synchronized collections often self-lock; use outer scopes for compound operations and borrowed views. See [synchronization](synchronization.md).
- **Exclusive access** — `ICriticalObject.EnterAccessScope()` returns `AccessScope`. Clustered streams, ObjectStreams and ObjectSpaces forward this monitor acquisition.
- **Paging** — `IPagedList.EnterOpenPageScope()` returns `PageScope<TItem>`. A memory-page scope restores the entry's previous `CanPurge` value, preserving nested pins; a dynamic stream-page scope closes the page.
- **Hasher borrowing** — `Hashers.EnterBorrowHasherScope()` returns `Hashers.HasherScope`. It returns the hasher to its pool; it does not dispose the hasher or pool.
- **Array borrowing** — `Tools.Scope.EnterBorrowArrayScope(length)` returns `BorrowArrayScope<byte>`; `EnterBorrowArrayScope<T>(length)` rents other element types. The scope returns its array to the originating pool and exposes the requested logical range through `Span`.
- **Ad-hoc cleanup** — use `Tools.Scope.ExecuteOnDispose` for an `Action`, or `Tools.Scope.ExecuteOnDisposeAsync` for a `Func<ValueTask>`. These factories return concrete [ActionScope](../../../src/Sphere10.Framework/Scopes/ActionScope.cs) / `ActionScope<T>` or [TaskScope](../../../src/Sphere10.Framework/Scopes/TaskScope.cs) / `TaskScope<T>` structs, preserving unboxed cleanup and their opposite-direction adapters. `SyncScope` and `AsyncScope` restrict disposal to one direction. [Tools.Scope](../../../src/Sphere10.Framework/Scopes/ScopeTool.cs) also supplies domain factories, including file/directory cleanup.
- **Context and transaction participation** — `ActionContextScope`, `TaskContextScope`, `ActionTransactionalScope<T>` and `TaskTransactionalScope<T>` are classes with their actual ambient or transaction identity. See [context lifetime](#context-lifetime) and [persistence and transactions](persistence-and-transactions.md).

```csharp
using (collection.EnterWriteScope()) {
	if (!collection.Contains(item))
		collection.Add(item);
}

using var restore = stream.EnterRestorePositionSeek(offset, SeekOrigin.Begin);
// Disposal restores the borrowed stream's original position.
```

Use the existing domain scope rather than reproducing its acquire/release protocol in consuming `try/finally`. Keep exception handling where it supplies recovery, unwinds a failed acquisition or transfers ownership.

Choose placement by reuse and coupling, not by which type exposes the acquisition method. Reusable lock scopes (`AccessScope`, `ReadScope` / `WriteScope`, including their generic forms, and `ProducerConsumerScope`), stream restoration (`RestorePositionScope`) and paging (`PageScope<TItem>`) remain namespace-level types in `Scopes`, alongside general callback, no-op, monitor, array-rental and ambient-context abstractions. A public subsystem abstraction does not become component-specific merely because it manages a resource, and platform availability alone does not make a scope use-case-specific. Nest an actual component helper when its cleanup is coupled to that component's implementation, such as `Hashers.HasherScope` returning a hasher to that component's pool. Keep existing acquisition methods and factories returning concrete scopes without changing acquisition syntax or disposal behavior.

### Tools.Scope factories

Name methods whose purpose is activating a scope `EnterXXScope`, including platform extensions. Use the resource or cleanup purpose in `XX`, such as `EnterReadScope` or `EnterBorrowArrayScope`. Preserve operation-bearing names when a method also performs another operation or describes its timing: `EnterRestorePositionSeek` seeks immediately before returning its restoration scope; `DeleteFileOnDispose` explicitly defers deletion; `AutoPersistWindowSettings` installs automatic event-driven persistence. Keep established operation names such as `BeginBootstrapForm` and `FromUnicodeString`; returning a scope does not make them generic scope factories. `ExecuteOnDispose` and `ExecuteOnDisposeAsync` express deferred callback execution and return the corresponding concrete scope without runtime delegate-type selection.

`Tools.Scope` also activates domain scopes through their existing owners. Its methods return the specific scope type, preserving unboxed `using var` locals for value scopes. Existing class scopes, including rendering and highlight scopes, retain their reference identity and ordinary object allocation:

```csharp
using var read = Tools.Scope.EnterReadScope(collection);
using var restore = Tools.Scope.EnterRestorePositionSeek(stream, offset);
using var access = Tools.Scope.EnterAccessScope(objectSpace);
```

Core factories include `EnterReadScope`, `EnterWriteScope`, `EnterAccessScope`, `EnterRestorePositionScope`, `EnterRestorePositionSeek`, `EnterBorrowHasherScope`, `EnterBorrowArrayScope`, `EnterOpenPageScope`, `EnterLockScope`, `EnterWaitScope`, `EnterSingleApplicationInstanceScope`, `EnterNoOpScope` and `EnterValueScope<T>`, alongside `DeleteFileOnDispose`, `DeleteFileOnDisposeAsync`, `DeleteDirOnDispose` and `DeleteDirOnDisposeAsync`. Typed read/write owners preserve their custom inner-scope contracts. `EnterSyncScope` creates a synchronous-only callback scope; `EnterAsyncScope` creates an asynchronous-only callback scope. `EnterValueScope<T>` returns `NoOpScope<T>` and borrows its item without disposing it.

Windows and WinForms assemblies contribute C# 14 **static extension members** targeting the existing core `Tools.Scope` type. Reference the relevant assembly and add `using Tools;` for extension lookup, even when writing the fully qualified `Tools.Scope.EnterRenderingScope(...)`. Windows factories include unmanaged memory/string allocation, execution-state, DPI, console attachment and token privileges. WinForms supplies `EnterRenderingScope`, `EnterHighlightScope` and `EnterUpdateScope`. These factories keep the returned resource's thread and ownership requirements; references to a platform assembly do not make its scopes cross-platform.

Partial classes combine source within one assembly; they cannot add these factories across assembly boundaries. Static extension members provide that syntax without a core-to-platform dependency, duplicate facade types, reflection or boxing.

## Choosing a disposal implementation

[IScope](../../../src/Sphere10.Framework/Scopes/IScope.cs) extends `IDisposable` and `IAsyncDisposable`. There is no scope-end event or generic lifecycle framework.

[ActionScope](../../../src/Sphere10.Framework/Scopes/ActionScope.cs) accepts an `Action`; [TaskScope](../../../src/Sphere10.Framework/Scopes/TaskScope.cs) accepts a `Func<ValueTask>`. Both are readonly structs with a typed finalizer and support both disposal directions. `ActionScope<T>` accepts `(T item, Action<T> finalizer)`; `TaskScope<T>` accepts `(T item, Func<T, ValueTask> finalizer)`. Each exposes the original `Item`. A static callback plus item avoids a captured closure. The finalizer's type is fixed by the scope type, with no runtime `Delegate` type dispatch, lifecycle flags or state-holder objects.

Consumers use `Tools.Scope.ExecuteOnDispose(Action)` / `ExecuteOnDispose<T>(Action<T>, T item)` or `Tools.Scope.ExecuteOnDisposeAsync(Func<ValueTask>)` / `ExecuteOnDisposeAsync<T>(Func<T, ValueTask>, T item)`. Generic factories take the callback first and the item second. They construct the concrete struct directly without wrapping the callback, boxing the result or adding a closure. Keep direct constructor use in the factory implementation and tests of the underlying scope primitives.

[SyncScope](../../../src/Sphere10.Framework/Scopes/SyncScope.cs) / `SyncScope<T>` accept an `Action` and support synchronous disposal only. [AsyncScope](../../../src/Sphere10.Framework/Scopes/AsyncScope.cs) / `AsyncScope<T>` accept a `Func<ValueTask>` and support asynchronous disposal only. Calling the unsupported direction throws or returns a faulted operation with `NotSupportedException` before invoking cleanup.

All callback constructors guard their required finalizer with `Guard.ArgumentNotNull`; resource scopes also guard required resource inputs. A null callback is not optional cleanup, and a default value is not a valid required-resource scope. Generic callback data may itself be null when the callback's contract permits it. For intentionally empty cleanup, use [NoOpScope](../../../src/Sphere10.Framework/Scopes/NoOpScope.cs) or `NoOpScope<T>`; their default values are valid, and the generic form retains a borrowed `Item` without disposing it.

Each explicit disposal of these cleanup structs invokes the finalizer again. Cleanup structs do not track phases, suppress reentry, cache a pending operation or provide automatic retry/idempotence.

Native scopes store their resource directly and perform that resource's finalization. Keep domain classes such as [ContextScope](../../../src/Sphere10.Framework/Scopes/ContextScope/ContextScope.cs), [TransactionalScopeBase<T>](../../../src/Sphere10.Framework/Transactions/TransactionalScopeBase.cs) and [SerializationContext](../../../src/Sphere10.Framework/Serialization/SerialiationContext.cs) as their actual owners. Do not wrap those classes in artificial struct handles or introduce generic state-holder classes for disposal.

Use the `Base` suffix only for an actual abstract class. The directional disposable templates are named `SyncDisposable` and `AsyncDisposable` within the existing `Disposable` lineage. Structs cannot inherit from classes or other structs.

### Class owners

The [Disposable](../../../src/Sphere10.Framework/Disposables/Disposable.cs) root supplies the class disposal template. Class owners select their implementation direction by inheriting from [SyncDisposable](../../../src/Sphere10.Framework/Disposables/SyncDisposable.cs) or [AsyncDisposable](../../../src/Sphere10.Framework/Disposables/AsyncDisposable.cs). These are abstract template bases, separate from the small resource-cleanup structs above. Override the chosen cleanup primitive instead of storing another disposable base in a property or repeating its adapter in each concrete class.

`SyncDisposable` leaves `FreeManagedResources()` to the implementation. Its default virtual `FreeManagedResourcesAsync()` adapter schedules that primitive with `Task.Run` and represents its complete execution and failure. A platform base can specialize where synchronous cleanup executes. `AsyncDisposable` leaves `FreeManagedResourcesAsync()` to the implementation. Its sealed synchronous adapter invokes the async factory on a worker outside the caller's synchronization context, then joins the entire operation and propagates its failure. Convert a returned `ValueTask` with `AsTask()` once inside that worker factory; waiting outside the caller's context cannot repair an operation already started on it.

[WinFormsSyncDisposableBase](../../../src/Sphere10.Framework.Windows.Forms/Disposables/WinFormsSyncDisposableBase.cs) is the shared abstract platform specialization used by rendering, highlight, screen-transition, hot-key and wizard owners. It captures the owning WinForms context once. Synchronous disposal runs `FreeManagedResources()` inline on the calling UI thread, preserving its ordering without draining queued callbacks. Its async hook queues that same primitive with `Post` and returns a completion source that represents cleanup success or failure. Owners reuse this execution policy rather than adding per-owner `Send` wrappers, thread IDs, disposal-state holders or async completion boilerplate. Async cleanup requires a live UI dispatcher.

Keep common disposal orchestration in `Disposable` and the directional adapters in these bases. Retain the domain inheritance of context, transaction and serialization owners. A lightweight borrowed-resource scope still returns its concrete struct and finalizes directly. Dispatcher requirements belong to the platform owner; do not add creator-thread tracking or UI machinery to the general disposable bases. See [disposal and platform integration](sync-async.md#disposal-and-platform-integration).

Class owners retain the existing `Disposable` protocol: after successful sequential disposal, later synchronous or asynchronous disposal calls do not repeat cleanup. A failed cleanup propagates without marking disposal successful; whether retry is safe depends on the resource. `SerializationContext` and the plugin-registration owner use `SyncDisposable` while preserving their class identity. This guard does not make concurrent disposal safe and does not apply to copies of cleanup structs.

### Ownership and copies

Match the ownership convention of the API being mirrored. .NET-style stream readers, writers and decorators use `leaveOpen`: `true` borrows the underlying stream; `false` closes it on disposal. Preserve the existing default of `false` for `EndianBinaryReader`, `EndianBinaryWriter` and `EncryptedStream`. Framework abstract data types and containers retain positive `ownsStream` ownership: `true` disposes the stream; `false` borrows it. When passing their ownership choice to a `leaveOpen` API, invert it at that boundary.

Acquire once, keep one owner and dispose that acquisition once, normally through one `using` declaration. Explicit repeated disposal of an action scope invokes its callback again. Repeated disposal of a native scope can release a lock or pooled resource again; no shared scope bookkeeping makes it safe.

Assigning a scope struct to `IDisposable`, `IAsyncDisposable` or `IScope` boxes a copy. Return concrete values through provider interfaces, and use `where TScope : struct, IScope` for generic consumers. A struct method group such as `scope.Dispose` also captures a copy. Copying a value copies its resource reference or finalizer; it does not create another acquisition.

Do not dispose both copies after transferring ownership to a retained field or closure. Reference scopes retain their ordinary class identity; copying their reference does not promise concurrent disposal, completion joining or retry. Serialize access according to the actual resource contract.

```csharp
using var cleanup = Tools.Scope.ExecuteOnDispose<IDisposable>(
	static resource => resource.Dispose(),
	ownedResource
);
```

Use `ExecuteOnDisposeAsync` for asynchronous finalization. Wrap an existing `Task` at the callback boundary:

```csharp
await using var cleanup = Tools.Scope.ExecuteOnDisposeAsync<string>(
	static path => new ValueTask(Tools.FileSystem.DeleteFileAsync(path)),
	filePath
);
```

The generic scope's `Item` remains the supplied item. Use the async factory for a task-returning callback: an expression lambda passed to the synchronous factory can bind to `Action<T>` and discard its returned task. Async factories accept `Func<ValueTask>` or `Func<T, ValueTask>`; wrapping a task once avoids competing `Task`/`ValueTask` overloads for async lambdas.

A static callback plus item can avoid a captured closure on the synchronous path. Creating a capturing callback still allocates; returning a struct does not eliminate allocations in the callback, resource or pool. The asynchronous adapter can allocate its worker task and queued argument capture.

### Array rentals

[BorrowArrayScope<T>](../../../src/Sphere10.Framework/Scopes/BorrowArrayScope.cs) implements `IScope<T[]>`. Its constructors accept `(int minimumLength, bool clearArray = false)` for the shared pool, or `(ArrayPool<T> pool, int minimumLength, bool clearArray = false)` for an explicit pool. They guard the pool and nonnegative length before renting. `Item` is the actual rented array, which may be larger than requested; `Length` and `Span` describe only the requested range.

```csharp
using var rental = Tools.Scope.EnterBorrowArrayScope(byteCount, clearArray: true);
stream.ReadExactly(rental.Span);
Consume(rental.Span);
```

`Dispose()` directly calls the originating pool's `Return(Item, clearArray)`, including when a `using` body throws. Clearing is requested on return; newly rented contents are unspecified. `DisposeAsync()` schedules the pool return on a worker and represents its completion and failure. Shared-pool synchronous rent/return can avoid allocation after warmup, while a pool miss may allocate an array and async scheduling has its ordinary task/closure costs. The factories also accept an explicit pool as the first argument.

Keep one owner and return each rental once. Arrays and spans must not be used after return; scope copies or an escaping view do not make duplicate return or reuse safe. A consumer that exposes an array/span can require its existing retirement policy instead of returning that storage while the view survives. When ownership is genuinely optional, a nullable `BorrowArrayScope<T>` can represent its absence; a default required rental is invalid. The scope owns the rental it acquires; retain the consumer's actual ownership policy without adding generic lifecycle flags.

`MemoryBuffer` represents its own contiguous array and keeps an ordinary `byte[]` internally. Do not introduce pooled backing storage, rental modes or exposed-view retirement tracking into that type. Transactional byte pages use the same owned-array buffer; temporary operation scratch can still use `EnterBorrowArrayScope` where its lifetime is bounded.

### Escaping acquisitions

If a returned stream or enumerator retains a scope, transfer it to one field or one closure. Its disposal then owns release. Unwind the acquisition if construction fails.

Use `enumerator.OnDispose(Action)` to register a callback. Its wrapper invokes that callback on every disposal call; registering a callback does not transfer resource ownership.

Use `enumerator.DisposeWith<T, TScope>(scope)` to transfer one concrete struct resource to the enumerator. The `where TScope : struct, IDisposable` constraint avoids boxing the resource or capturing its `Dispose` method group. [ScopeEnumerator](../../../src/Sphere10.Framework/Collections/Iterators/ScopeEnumerator.cs) retains the value in a field; the decorator itself remains a class allocation. Synchronized collection enumerators retain their read acquisition until disposed. Release these acquisitions on the acquiring thread.

The wrapper releases the resource before disposing the inner enumerator and records release only after that call succeeds. Later disposal calls skip the successfully released resource and still forward disposal to the inner enumerator, including after an inner disposal failure. If resource release throws, the error propagates and the inner enumerator is not disposed by that call. There is no automatic retry: an explicit retry is appropriate only when the resource's own contract permits it.

Passing a struct still copies it. After a successful transfer, the original local must not also be disposed. Do not declare it with `using` before returning the enumerator, because the declaration would release the acquisition before the caller consumes the result. Keep acquisition unwind when construction or obtaining the inner enumerator fails before the transfer completes:

```csharp
public override IEnumerator<TItem> GetEnumerator() {
	var readScope = EnterReadScope();
	try {
		return
			base.GetEnumerator()
				.DisposeWith(readScope);
	} catch {
		readScope.Dispose();
		throw;
	}
}
```

`SynchronizedObject<TReadScope, TWriteScope>` requires struct `IScope` implementations and returns `ReadScope<TReadScope>` / `WriteScope<TWriteScope>`. Each wrapper disposes the custom inner scope while holding the lock, then releases that same captured lock. Failed construction releases the acquisition. If inner finalization throws, the later lock release has not run; recovery must follow the inner resource's contract.

## Synchronous and asynchronous cleanup

Preserve the chosen scope's actual execution contract rather than applying one adapter to every scope:

| Scope | `DisposeAsync()` behavior |
|---|---|
| `ActionScope` / `ActionScope<T>` | Schedules the finalizer with `Task.Run` and returns its completion. Callback failures fault that operation. |
| `TaskScope` / `TaskScope<T>` | Awaits the supplied `ValueTask`. A callback that throws before returning also faults the returned operation. |
| `SyncScope` / `SyncScope<T>` | Rejects async disposal with `NotSupportedException` without starting cleanup. |
| `AsyncScope` / `AsyncScope<T>` | Awaits the supplied `ValueTask`; synchronous disposal is unsupported. |
| `NoOpScope` / `NoOpScope<T>` | Completes immediately without finalizing an item. |
| `AccessScope`, `ReadScope`, `WriteScope`, including typed read/write wrappers | Rejects async disposal with `NotSupportedException` without starting cleanup. Use synchronous `using` on the acquiring thread. |
| `Hashers.HasherScope`, `PageScope<TItem>` | Retains the worker-based cleanup of the original action scope. Prefer synchronous `using` when the surrounding owner requires its thread. |
| `BorrowArrayScope<T>` | Schedules return to its originating pool on a worker and returns that completion. |
| `RestorePositionScope` | Its added async member offloads the position setter. The original type exposed synchronous disposal only. |
| `ThreadDpiAwarenessScope`, `TokenPrivilegeScope`, `AlwaysOnScope` | Reports `NotSupportedException`: restoration belongs to the acquiring thread. |
| Control-backed rendering/update scopes | Queue cleanup to the owning UI dispatcher and return its completion. They require a live dispatcher. |

`ActionScope.Dispose()` calls its callback directly. `TaskScope.Dispose()` starts the callback on a worker outside the caller's synchronization context, then waits for the complete operation and propagates its failure. It consumes the returned `ValueTask` once, including a pending source-backed operation. Prefer `await using` for asynchronous cleanup. `SyncScope.Dispose()` invokes its callback directly; `AsyncScope.Dispose()` rejects synchronous disposal.

Monitor/read/write acquisitions belong to their acquiring thread. Use synchronous `using` for these locks and other synchronous-only scopes. An async member must not conceal blocking cleanup behind `async` without an await, or call `Dispose()` inline and return a completed task. Offload thread-neutral synchronous cleanup; use the resource's real async API when available. If cleanup requires the original thread and no dispatcher is available, explicitly reject async disposal before changing the resource. Do not move a native lock release to a worker or await a custom inner finalizer while retaining a thread-affine lock.

`AsyncSynchronizedGate.EnterScopeAsync()` returns `Task<ActionScope<AsyncSynchronizedGate>>`. Await acquisition once and retain one owner of its result. Its semaphore admission can span an `await`; a thread-affine monitor/read/write acquisition cannot.

[Disposables](../../../src/Sphere10.Framework/Disposables/Disposables.cs) is an owned resource bag. Its `DisposeAsync()` awaits each async-disposable child in registration order and offloads synchronous-only children. It preserves its exception policy. Its `Add(Func<Task>)` wraps `TaskDisposable`; `DisposableResource` forwards async cleanup to the bag. See [application ownership](application-ownership.md) and [adapter completion](sync-async.md#adapter-completion-and-cancellation).

## Context lifetime

[CallContext](../../../src/Sphere10.Framework/Scopes/ContextScope/CallContext.cs) stores named values in `AsyncLocal<object>` slots. Child execution contexts inherit the value, and assignments in a child do not replace the parent's value. Sharing a transaction/session reference does not make concurrent use safe.

`IContextScope` adds `ContextID`, `Policy` and `RootScope`. `MustBeRoot` rejects an existing matching root; `MustBeNested` requires one; `None` permits either. The root is the actual context-scope class instance. Only the root runs its context finalizer; each scope runs its own finalizer.

`ContextScope` inherits `Disposable` and reuses its successful-disposal guard. Its overrides of the public virtual `Dispose()` and `DisposeAsync()` entrypoints restore the previous ambient value in the caller before delegating to the base template. The async override is a non-async front end so restoration happens before cleanup can suspend; AsyncLocal assignments inside an async method cannot flow back to its caller. This dispatch also applies through `Disposable` and scope-interface references. Restoration checks the current root's identity so disposing an older scope does not replace a newer root. Failed root construction restores the prior value as well. No additional scope-disposal state is introduced.

Context, transaction and serialization scopes retain their own domain rules, without a struct facade or a new shared scope-state layer. Cleanup structs do not cache pending cleanup or suppress repeated callbacks. Class owners retain their existing `Disposable` or domain disposal protocol; neither reference identity nor a sequential disposal guard makes simultaneous disposal safe.

## Cleanup review and domain scopes

Implement a small finalizer for the actual resource and return its concrete struct when identity is unnecessary. Use the `ExecuteOnDispose` / `ExecuteOnDisposeAsync` factories for delegate cleanup, or `SyncScope` / `AsyncScope` when the opposite direction is unsupported. Validate required constructor inputs before acquisition or mutation. Retain a class when the domain itself requires identity, nesting or a shared session; do not add a lifecycle protocol merely to make a scope.

Review acquisition failure, ownership transfer, copies, allocator pairing, nesting, exception paths, callback ordering, execution thread and awaited completion. Test async adapters with deliberately pending or blocked cleanup: the call must return before cleanup completes, completion must wait for the resource, and faults must propagate through that operation. Merely returning a task or marking a method `async` does not establish this. Keep one normal disposal per acquisition. Do not assume a failed finalizer can safely repeat: it might have released the resource before a later notification failed. Preserve the original error and the backend's actual recovery behavior.

[UnmanagedMemoryScope](../../../src/Sphere10.Framework.Windows/Misc/UnmanagedMemoryScope.cs) owns `Marshal.AllocHGlobal` and `Marshal.StringToHGlobalUni`. It must not free buffers allocated by APIs requiring `NetApiBufferFree`, `LocalFree` or another allocator. Declare dependent scopes in acquisition order so disposal reverses dependencies; keep native callbacks rooted until their consumer has drained.

Page scopes preserve virtual dispatch, cache pinning and transaction behavior. Keep one owner for each borrowed hasher; a scope does not remove allocation inside its pool.

A transaction scope owns the backend transaction it creates. A nested participant's explicit rollback records a vote for that owner. Disposing an unfinished child does not automatically vote rollback, and common disposal closes an owned transaction without itself calling `Rollback`. Business events such as `Committing`/`Committed` remain domain notifications. Inspect backend close behavior and explicitly roll back abandoned work when required.

The [scope composition](../../performance/scope-composition.md), [value-scope](../../performance/value-scopes.md) and [whole-abstraction](../../performance/scope-abstraction.md) reports retain the implementations and measurements from their recorded source snapshots. Use the current `ActionScope` / `TaskScope` contracts above for new code.
