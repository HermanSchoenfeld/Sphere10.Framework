# Threading

Lock owners that hand out disposable scopes, serial and producer-consumer execution primitives, throttles and `Tools.Threads` helpers.

The lock owners here issue the scope values defined in [Scopes](../Scopes/README.md). `CriticalObject` wraps a `Monitor` and returns an `AccessScope` from `EnterAccessScope()`. `SynchronizedObject` wraps a `ReaderWriterLockSlim`, recursive by default, and returns `ReadScope` and `WriteScope` values. A `ParentSyncObject` or `ParentCriticalObject` makes several objects share one parent lock. `Critical<T>` and `Synchronized<T>` guard a single value with those locks. `SynchronizedResource` extends `SynchronizedObject` with initialization when the first read or write scope opens and finalization when the last one closes. All of these scopes are thread-affine, so dispose them synchronously on the thread that acquired them and never hold them across an `await`. Use `AsyncSynchronizedGate` for exclusive FIFO admission around asynchronous work, because its scope may be released on another thread.

`SerialThreadPool` runs queued actions one at a time in order on thread-pool threads, and `SerialThreadPool.Global` logs failures to `SystemLog`. `ProducerConsumerLock` blocks producers or consumers until a condition holds. `Throttle` spaces calls by a minimum interval and coalesces asynchronous callbacks, `DelayedAction` runs an action once pings stop for a while, and `ThroughputOptimizer` tunes a batch size from timing samples. `Tools.Threads` queues actions on the thread pool, raises events asynchronously, runs actions concurrently and measures durations. The transactional scopes in [Transactions](../Transactions/README.md) and many collections build on `SynchronizedObject`.

```csharp
var counter = new Synchronized<int>(0);
using (counter.EnterWriteScope()) {
	counter.Value++;
}
```

`Throttle.ExecuteAsync(Func<Task>)` runs callbacks sequentially using its immutable
`Mode`. Existing constructors retain `NotifyEveryInterval`, so existing callers
continue receiving updates during continuous input.

| Mode | Delivery |
|---|---|
| `None` | Every request runs in FIFO order without an interval delay. |
| `NotifyEveryInterval` | The first callback starts immediately unless a previous start or `RegisterEvent` requires a wait. Pending requests coalesce to the latest callback, with starts spaced by the interval. New requests do not postpone that deadline. |
| `NotifyOnEventBurstFinished` | The latest callback runs after no request has arrived for the interval. Each request restarts the quiet period. |
| `NotifyOnEventBurstStartAndFinished` | The first request runs immediately. Additional requests coalesce into one trailing callback after the quiet period. An isolated request is not delivered twice. |

For updates that must continue every five seconds while notifications arrive:

```csharp
var presentation = new Throttle(TimeSpan.FromSeconds(5), ThrottleMode.NotifyEveryInterval);
// Reuse this instance for each refresh request.
await presentation.ExecuteAsync(PresentAsync);
```

Periodic mode runs only when work is requested; it does not poll while idle.
A callback longer than the interval delays subsequent work without overlapping
callbacks or accumulating missed ticks. `None` retains every pending request;
the other modes retain only the latest.

Concurrent callers share the current drain task, which completes after pending
work and reports callback failures. Failures do not discard remaining work.
Callbacks choose their own dispatcher and cancellation and run outside the
throttle's synchronization scopes. Internal waits do not capture the caller's
synchronization context. A callback may enqueue another request but must not
await this same throttle, because it would await its own completion.

`Mode` applies to `ExecuteAsync`. The existing `Wait`, `WaitAsync`, `CanPass`,
`RegisterEvent` and `IsCallerFirstInStampede` contracts remain unchanged.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AsyncSynchronizedGate`](AsyncSynchronizedGate.cs) | class | FIFO exclusive admission for asynchronous work, whose `EnterScopeAsync` result releases the gate when disposed. |
| [`Critical<T>`](Critical.cs) | class | Holds one value whose getter and setter each run inside the object's monitor. |
| [`CriticalObject`](CriticalObject.cs) | class | `ICriticalObject` over a `Monitor` lock, optionally shared with a parent, with open and close hooks. |
| [`DelayedAction`](DelayedAction.cs) | class | Runs an action on the thread pool once `Ping()` has not been called for a given duration. |
| [`FastLock`](FastLock.cs) | class | Recursive exclusive lock over `ReaderWriterLockSlim` whose `EnterLockScope()` returns a synchronous scope. |
| [`ICriticalObject`](ICriticalObject.cs) | interface | Monitor-locked object with a lock, a parent, an `IsLocked` flag and `EnterAccessScope()`. |
| [`IReaderWriterLock`](IReaderWriterLock.cs) | interface | Exposes the `ReaderWriterLockSlim` that an object's read and write scopes use. |
| [`ISynchronizedObject`](ISynchronizedObject.cs) | interface | Reader/writer-locked object with `EnterReadScope()`, `EnterWriteScope()` and a parent. |
| [`ISynchronizedObject<TReadScope, TWriteScope>`](ISynchronizedObject.cs) | interface | Reader/writer-locked object whose scopes also dispose caller-selected inner scope structs. |
| [`ISynchronizedResource`](ISynchronizedResource.cs) | interface | `ISynchronizedResource<NoOpScope, NoOpScope>` for resources without inner scopes. |
| [`ISynchronizedResource<TReadScope, TWriteScope>`](ISynchronizedResource.cs) | interface | Synchronized object that raises initializing, initialized, finalizing and finalized events for its read and write scopes. |
| [`NonReentrantLock`](NonReentrantLock.cs) | class | Exclusive lock that throws `InvalidOperationException` when the owning thread tries to acquire it again. |
| [`ProducerConsumerLock`](ProducerConsumerLock.cs) | class | Monitor-like lock where producers and consumers block or delay until a condition holds, returning a `ProducerConsumerScope`. |
| [`ProducerConsumerType`](ProducerConsumerType.cs) | enum | `Consumer` or `Producer` role. |
| [`ReaderWriterLockTiny`](ReadWriterLockTiny.cs) | struct | Spinning reader/writer lock in one integer, with no recursion and an unchecked read-to-write upgrade. |
| [`SameThreadSynchronizationContext`](SameThreadSynchronizationContext.cs) | class | Synchronization context whose `Run` executes an async method and all its continuations on the calling thread. |
| [`SerialThreadPool`](SerialThreadPool.cs) | class | Executes queued actions serially in submission order on thread-pool threads, with an error handler. |
| [`SerialThreadPoolPolicy`](SerialThreadPoolPolicy.cs) | enum | Whether a thread drains the whole queue (`Burst`) or one item per acquisition (`Intermittent`). |
| [`Synchronized<T>`](Synchronized.cs) | class | Holds one value whose getter takes a read scope and whose setter takes a write scope. |
| [`SynchronizationCallback`](SyncronizationCallback.cs) | class | Callback bound to a `SynchronizationContext`, convertible to and from the common .NET callback delegates. |
| [`SynchronizedObject`](SynchronizedObject.cs) | class | `ISynchronizedObject` over a `ReaderWriterLockSlim`, recursive by default. |
| [`SynchronizedObject<TReadScope, TWriteScope>`](SynchronizedObject.cs) | class | Synchronized object whose scopes pair the lock with inner scope structs. |
| [`SynchronizedObjectBase`](SynchronizedObjectBase.cs) | abstract class | Owns the reader/writer lock and the scope open, close and readable or writable checks used by subclasses. |
| [`SynchronizedResource`](SynchronizedResource.cs) | abstract class | `SynchronizedResource<NoOpScope, NoOpScope>` for resources without inner scopes. |
| [`SynchronizedResource<TReadScope, TWriteScope>`](SynchronizedResource.cs) | abstract class | Synchronized object that initializes a resource when the first read or write scope opens and finalizes it when the last one closes. |
| [`TaskCompletionSourceEx`](TaskCompletionSourceEx.cs) | class | Non-generic `TaskCompletionSource` for the .NET Standard build only. |
| [`Throttle`](Throttle.cs) | class | Spaces calls by a minimum interval or rate, with blocking and asynchronous waits, stampede detection and coalesced asynchronous execution. |
| [`ThrottleMode`](ThrottleMode.cs) | enum | Selects FIFO, periodic, trailing quiet-period, or leading-and-trailing asynchronous delivery. |
| [`ThroughputOptimizer`](ThroughputOptimizer.cs) | class | Suggests a batch size between bounds by comparing the throughput of registered timing samples. |
| [`Tools.Threads`](ThreadTool.cs) | tools | Thread-pool queuing, asynchronous event raising, concurrent execution, monitor scopes and duration measurement. |
