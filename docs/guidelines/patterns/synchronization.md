# Synchronization with Read/Write Scopes

Read this topic when protecting shared state, selecting a synchronized collection,
or coordinating several objects under one lock. Read [resource scopes](resource-scopes.md)
for cleanup and ownership, and the [collection guide](collections.md)
for the collection and storage families.

## Contract and lock lifetime

Application/framework consumers must use the synchronized pattern and
scope-based acquire/release. Prefer `SynchronizedObject` and its
`EnterReadScope()`/`EnterWriteScope()`; do not replace this idiom with raw
`SemaphoreSlim.Wait/WaitAsync/Release` or manually paired lock calls.
For synchronous provider collection, enter/release the write scope within the
same worker delegate. A thread-affine scope must never span an `await`.

`Synchronized*` collections are the designated shared-state pattern in Framework
and its consumers. Use the appropriate collection and its scopes rather than
substituting `ConcurrentDictionary`, another concurrent collection or a separate
locking protocol for an existing synchronized owner. A concrete requirement can
justify a different primitive; document that requirement and the relevant
correctness or measured performance evidence. A screening result alone does not
justify abandoning a pattern that can support the required behavior. Preserve
atomic compound operations and copy borrowed data under a short scope before
yielding, awaiting or processing it outside the lock.

When exclusive asynchronous admission is actually needed, use the framework's
[AsyncSynchronizedGate](../../../src/Sphere10.Framework/Threading/AsyncSynchronizedGate.cs):
`using var admission = await gate.EnterScopeAsync(cancellationToken);` releases
admission on disposal. This is a non-reentrant FIFO gate, not a read/write lock.
Its internal queue/state is protected by short synchronized write scopes;
its admission scope can span `await` without holding a thread-affine lock.
Cancelled queued admissions do not consume ownership or block later callers.
Do not implement another semaphore/gate with manual releases in a consumer.

[ISynchronizedObject](../../../src/Sphere10.Framework/Threading/ISynchronizedObject.cs)
exposes `EnterReadScope()`, `EnterWriteScope()`, `ThreadLock` and
`ParentSyncObject`. [SynchronizedObject](../../../src/Sphere10.Framework/Threading/SynchronizedObject.cs)
implements the scopes with a `ReaderWriterLockSlim`, returning concrete
`ReadScope` and `WriteScope` values: a read scope permits concurrent readers,
while a write scope excludes other readers and writers. Acquire through the
scope methods and release through `using`; do not duplicate their enter/exit
machinery with raw lock calls.

The interface exposes the same concrete `ReadScope` and `WriteScope` return
types as the ordinary methods. Use the standard names through either reference:

```csharp
ISynchronizedObject owner = new SynchronizedObject();
using var scope = owner.EnterWriteScope();
```

`IReaderWriterLock` exposes only the shared `ThreadLock`. Ordinary `ISynchronizedObject` and typed `ISynchronizedObject<TReadScope, TWriteScope>` inherit that lock contract while keeping their distinct scope return types. `SynchronizedObjectBase` resolves the parent lock once for acquisition; the returned scope retains that exact lock even if `ParentSyncObject` changes before disposal.

Decorators forward those concrete results directly. Custom providers implement
the same methods and can return a scope constructed with their cleanup action.
Acquisition and cleanup remain the provider's responsibility; creating a
capturing callback can still allocate. There is one entry point per scope,
without a separate interface method returning `IDisposable`.

```csharp
using Sphere10.Framework;

var values = new SynchronizedDictionary<string, int>();
values["jobs"] = 1;

// Keep the read-modify-write operation under one write scope.
using (values.EnterWriteScope()) {
	values.TryGetValue("jobs", out var count);
	values["jobs"] = count + 1;
}
```

Choose the scope for the entire operation before acquiring it. Two individually
locked calls are not one atomic operation: checking membership then adding, or
reading a list's count then indexing it, can race between calls. Use an outer
write scope for compound mutations and an outer read scope for a consistent
multi-call read. The default recursion policy is `SupportsRecursion`, allowing
self-locking members to run inside the appropriate outer scope. A supplied
parent's lock policy is the effective policy; do not assume recursion when
using `NoRecursion`.

A read scope cannot be upgraded by entering a write scope. This remains true
with `SupportsRecursion`. There is no `EnterUpgradeableReadScope` method on the
framework contract. If the operation may write, enter a write scope initially,
or leave the read scope and recheck the condition after acquiring a write scope.
Do not mutate a collection from its read-locked enumeration.

## Self-locking members and borrowed results

The collection decorators generally acquire their own scope for each operation.
Consumers do not need an additional outer scope for every single `Add`, lookup
or indexer call that already self-locks. An outer scope extends protection across
several calls, deferred enumeration or a borrowed view. It does not make bypasses
through the original wrapped collection safe; all users of that storage must
participate in the same synchronization protocol.

| Family | Current scope behavior |
|---|---|
| [SynchronizedCollection](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedCollection.cs), [SynchronizedList](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedList.cs) and [SynchronizedDictionary](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedDictionary.cs) | Ordinary reads and writes self-lock. `GetEnumerator()` holds a read scope until the enumerator is disposed. |
| [SynchronizedExtendedList](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedExtendedList.cs) | Extended reads and writes self-lock, including range calls. Its enumerator retains a read scope, but a returned `ReadRange` or `ContainsRange` sequence can remain deferred depending on the wrapped implementation. Consume it under an outer scope. |
| [SynchronizedQueue](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedQueue.cs) and [SynchronizedHeap](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedHeap.cs) | Collection operations inherit the synchronized decorator; queue enqueue/dequeue and heap pop take write scopes, while peek takes a read scope. |
| [SynchronizedSet](../../../src/Sphere10.Framework/Collections/Synchronized/SynchronizedSet.cs) | Exposes scopes and locks its enumerator, but its current ordinary set methods and properties forward without acquiring a scope. Explicitly scope reads and writes; its name alone does not guarantee self-locking. |
| [Synchronized<T>](../../../src/Sphere10.Framework/Threading/Synchronized.cs) | The `Value` getter/setter self-lock. A mutable referenced value remains borrowed after the getter returns; protect mutations and compound operations with an outer scope. |

`SynchronizedDictionary.Keys` and `Values` return the inner dictionary's
collections after the getter's scope ends. Their live-view or snapshot behavior
comes from that source: a BCL dictionary returns live views, while a B-tree can
materialize lists. The wrapper does not retain a lock for later consumption.
Materialize live views while an outer read scope remains active:

```csharp
using System.Linq;
using Sphere10.Framework;

var values = new SynchronizedDictionary<string, int>();
string[] keys;
using (values.EnterReadScope())
	keys = values.Keys.ToArray();

// The array can now be consumed without retaining the dictionary lock.
foreach (var key in keys) {
	// Use the copied key.
}
```

[CallContext](../../../src/Sphere10.Framework/Scopes/ContextScope/CallContext.cs)
uses a write scope to publish one ambient slot per name and a short read scope
to copy key names before yielding them. The
[registry tests](../../../tests/Sphere10.Framework.Tests/Scopes/CallContextTests.cs)
cover concurrent first publication, execution-flow isolation and writes between
search yields.

Dispose manual enumerators with `using`; `foreach` disposes them automatically.
Abandoning an enumerator can leave its read lock held. Do not pass a locked
enumerator to another thread or hold it across an `await`. For slow processing,
copy the required membership under a read scope and process the copy after
releasing the scope. A membership snapshot does not make mutable elements
immutable or thread safe.

An enumerator or returned stream that retains a concrete scope must own it in
one field or closure until disposal. Transfer ownership only after successful
construction, and release the local acquisition if construction fails. Do not
dispose both the local and its retained copy, or bind a struct's `Dispose` method
to a delegate that captures another copy. This ownership transfer does not
relax thread affinity.

## Borrowed callbacks and UI dispatch

A lifetime scope must not be held while borrowed code synchronously dispatches
to a UI thread that can read or stop the same object. The worker waits for the UI,
while the UI waits for its scope, causing a deadlock. Snapshot the borrowed target
and local options under a short scope, release it, then invoke that target. Its
actual owner controls in-flight completion and disposal.

Owned resources have a different boundary: an owned file writer can remain under
the lifetime write scope so Stop/disposal joins the write before closing it.
[LogOutputSession](../../../src/Sphere10.Framework.Windows.Forms/Controls/Logging/LogOutputSession.cs)
demonstrates both paths. Native event subscriptions should post UI work rather
than synchronously invoke a UI thread that may be stopping the native source.

## Sharing a synchronization root

`ParentSyncObject` makes a child use its parent's `ThreadLock`. This allows
different collection or cache types to protect an invariant under one shared
lock instead of acquiring independent locks in competing orders.

```csharp
using Sphere10.Framework;

var root = new SynchronizedObject();
var amounts = new SynchronizedDictionary<string, int> { ParentSyncObject = root };
var changedKeys = new SynchronizedList<string> { ParentSyncObject = root };

// Both child updates participate in the same write scope.
using (root.EnterWriteScope()) {
	amounts["jobs"] = 2;
	changedKeys.Add("jobs");
}
```

Configure the parent before publishing or using the child. Keep the parent
chain acyclic and its identity stable while scopes can be open or waiting.
The nongeneric `SynchronizedObject` captures the acquired lock in its concrete
scope and releases that same lock. Changing a parent still splits mutual
exclusion between existing and new users; taking a scope on the old lock is
insufficient to safely reparent active users. The generic
`SynchronizedObject<TReadScope, TWriteScope>` also captures the acquired lock
and releases that same instance.

[PooledCacheReaper](../../../src/Sphere10.Framework/Cache/PooledCacheReaper.cs)
is a concrete consumer: it parents differently typed caches to itself before
use and retains that parent when deregistering a member. In
[PooledCacheReaperTests](../../../tests/Sphere10.Framework.Tests/Cache/PooledCacheReaperTests.cs),
`MemberDisposal_RemovesCapacityAndPreservesOpenScopeLockIdentity` checks that
disposal inside an outstanding member scope preserves the lock used for exit.
`Registration_RejectsCachesThatHaveAlreadyBeenUsedOrParented` checks the setup
boundary.

A scope owns a lock acquisition, not the synchronized object's payload or its
parent. Disposing a scope releases its acquisition; it does not dispose the
collection, cached values or shared root. `SynchronizedObject` itself is not
`IDisposable`. If an owner explicitly disposes a `ReaderWriterLockSlim`, it must
first stop all users and wait for outstanding scopes; a child must not dispose
the parent's borrowed lock. Do not dispose `ThreadLock` merely because a child
has finished one operation.

## Implementing synchronized members

Derive from `SynchronizedObject` when synchronization is intrinsic to a type,
or delegate to one when a decorator already has another base. Use
`OnReadScopeOpen/Closed` and `OnWriteScopeOpen/Closed` only for the additional
scope behavior the type needs. The nongeneric base acquires the lock before
calling its open hook and unwinds that acquisition if the hook throws. Its
concrete scope releases the captured lock before calling the closed hook.
Disposal directly performs that sequence; there is no scope phase tracking or
reentry suppression. If a closed hook throws, the acquisition has already been
released. Do not repeat disposal to handle that notification failure.

Default `ReadScope` and `WriteScope` values own nothing. Keep one owner and one
normal disposal per acquisition; copying a scope copies the same acquisition,
and assigning it to `IDisposable` boxes a copy. Direct concrete `using` calls keep
the value. See [resource scopes](resource-scopes.md#choosing-a-disposal-implementation)
for ownership transfer, callback helpers and binary compatibility.

`EnsureReadable()` and `EnsureWritable()` verify the current thread's held
lock; they do not acquire one. In the current base, `EnsureReadable()` checks
read or upgradeable-read ownership, while `EnsureWritable()` checks write
ownership. **A write lock alone does not satisfy `EnsureReadable()`.** Account
for that concrete behavior when designing guarded members instead of assuming
that the helper accepts every state that is logically safe to read.

[SynchronizedResource](../../../src/Sphere10.Framework/Threading/SynchronizedResource.cs)
adds resource initialization/finalization around tracked scopes. It is a
separate abstract lifecycle foundation, not a prerequisite for a synchronized
collection or a drop-in replacement for the simple lock owner.

The generic `SynchronizedObject<TReadScope, TWriteScope>` requires struct `IScope`
types and returns `ReadScope<TReadScope>` / `WriteScope<TWriteScope>`. The wrapper
disposes its inner scope while holding the lock, then releases the acquisition.
If inner cleanup throws, lock release has not run; recovery follows the inner
resource's contract rather than an automatic retry protocol. A construction
failure unwinds the acquired lock. Its
`ISynchronizedObject<TReadScope, TWriteScope>` contract is separate from the
nongeneric value-scope contract, and its typed parent must use compatible scope
types. Both families use owner-based `ReadScope` / `WriteScope` construction.
Each scope extracts and captures `owner.ThreadLock` before acquisition, then
runs the owner's open hook. Failed hooks release that captured lock; changing
the parent's lock during or after the hook cannot change which lock is released.
Their shared `IReaderWriterLock` contract exposes only `ThreadLock`.
The base stores one parent reference and keeps its lock lookup nonvirtual;
sharing the lock contract does not introduce a nongeneric scope bypass.
[TypedSynchronizedObjectAdapterTests](../../../tests/Sphere10.Framework.Tests/Threading/TypedSynchronizedObjectAdapterTests.cs)
cover typed dispatch, parent compatibility, and synchronous scope lifetime.

## Thread affinity and asynchronous work

Reader/writer and monitor acquisitions belong to the acquiring thread. Keep
these scope blocks synchronous and dispose on that thread. The typed scope's
`IAsyncDisposable` shape does not remove this requirement. Its `DisposeAsync()`
returns `NotSupportedException` without starting cleanup or releasing the lock.
An earlier `await` can still move the caller to another thread. Use synchronous
`using` for these lock scopes.

For async work, take a short synchronous scope to capture or update state,
release it before awaiting, then acquire a new scope and revalidate any state
that may have changed. If an operation must serialize an entire asynchronous
workflow, use the scope-returning `AsyncSynchronizedGate` described above rather
than wrapping it in a thread-affine read/write scope. The existing
[ApplicationScreenHost](../../../src/Sphere10.Framework.Application/UI/ApplicationScreenHost.cs)
coordinates transitions with `SemaphoreSlim.WaitAsync` and releases through
`ActionScope`; that is an existing implementation, not the
consumer pattern for new coordination code.

[SynchronizedRepository](../../../src/Sphere10.Framework/Repository/SynchronizedRepository.cs)
has existing CRUD async adapters that call `ContinueOnSameThread` inside these
scopes. The [task extension](../../../src/Sphere10.Framework/Extensions/TaskExtensions.cs)
uses [SameThreadSynchronizationContext](../../../src/Sphere10.Framework/Threading/SameThreadSynchronizationContext.cs)
to block and pump the calling thread until the wrapped task completes. That
context is installed without restoring the previous one. This is an existing
compatibility path with blocking/context effects, not a general pattern for
nonblocking async locking.
Its disposal adapter instead schedules its existing synchronous `Dispose`
primitive on a worker. Acquisition, nested synchronized wrappers and release
remain on that worker; inner repositories use their synchronous disposal
contract, including the complete sync-over-async bridge for async-first owners.
The caller receives the worker's task.

## Choosing other existing scope primitives

- [ICriticalObject](../../../src/Sphere10.Framework/Threading/ICriticalObject.cs)
  and [CriticalObject](../../../src/Sphere10.Framework/Threading/CriticalObject.cs)
  supply monitor-backed exclusive access and shared `ParentCriticalObject`.
  `EnterAccessScope()` returns concrete `AccessScope` through both the class and
  interface. Custom providers implement that same return contract. The built-in
  scope captures the acquired monitor, unwinds a failed open hook, and calls the
  closed hook only after release. ObjectSpace and clustered streams forward this
  access contract rather than using reader/writer scopes.
- [FastLock](../../../src/Sphere10.Framework/Threading/FastLock.cs) supplies a
  recursive exclusive `EnterLockScope()` backed by a reader/writer write lock,
  returning a concrete `SyncScope<ReaderWriterLockSlim>`.
- [NonReentrantLock](../../../src/Sphere10.Framework/Threading/NonReentrantLock.cs)
  supplies exclusive `EnterLockScope()` and rejects reentry by the owning
  thread. Although backed by `SemaphoreSlim`, its public acquire/release
  contract is synchronous and thread-affine. It returns a concrete
  `SyncScope<NonReentrantLock>`.
- `Tools.Threads.EnterLockScope()` returns a concrete `MonitorScope` for an ordinary monitor. `AccessScope` separately retains the required `CriticalObject` owner and its closed notification; it has no plain-lock or callback mode.
  `SemaphoreSlim.EnterWaitScope()` returns a concrete
  `ActionScope<SemaphoreSlim>` after synchronous acquisition. These helpers
  do not introduce asynchronous lock scopes.

- [ProducerConsumerLock](../../../src/Sphere10.Framework/Threading/ProducerConsumerLock.cs) returns `ProducerConsumerScope`, which directly owns its monitor, role semaphore and acquiring thread identifier. Encapsulate this acquisition in its named value type instead of repeating tuple payloads or callback casts throughout the API.

Use synchronous `using` for `FastLock` and `NonReentrantLock`, whose release must
stay on their acquiring thread. Their `SyncScope<T>` rejects async
disposal before invoking its callback. The semaphore admission scope remains
thread-neutral and can offload release through `ActionScope<T>`.

Choose the contract used by the actual resource. A synchronization scope only
coordinates access; it does not make a mutation transactional or durable. See
[persistence and transactions](persistence-and-transactions.md) for transaction
ownership, and [resource scopes](resource-scopes.md) for cleanup.

[ParallelClusteredStreamsTests](../../../tests/Sphere10.Framework.Tests/ClusteredStreams/ParallelClusteredStreamsTests.cs)
show actual parallel use of synchronized dictionaries/lists beside the storage
access protocol, and `OpenedStreamKeepsLockAfterAccessScopeClosed` demonstrates
that an opened clustered stream can retain its own acquisition after the outer
access scope ends. Resource lifetime can therefore be longer than the nearest
visible scope; dispose the returned resource according to its own contract.
