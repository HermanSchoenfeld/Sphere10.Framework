# Async disposal and typed scope factories, 25 October 2026

The current callback API and its validation are documented in [callback scope consolidation](callback-scope-consolidation.md). This report retains historical callback names and the source snapshot identified below.

The follow-up review found async disposal methods whose bodies still called synchronous cleanup inline, including several native/UI scopes and shared disposable adapters. Adding `async` without an await did not make that cleanup asynchronous. The earlier [scope composition measurements](scope-composition.md) remain evidence of their archived source version; they do not validate these subsequent corrections.

## Execution contracts

- `SyncDisposable`, `SyncRepositoryBase`, `SyncApplicationBase` and thread-neutral synchronous scopes return worker completion. Deliberately blocked cleanup must not block the calling thread.
- Async-first callbacks, cleanup bags and providers await the actual cleanup operation. `DisposableResource` forwards to `Disposables.DisposeAsync`, which preserves registration order and the bag's exception policy. Synchronous bridges join completion and propagate failures.
- FragmentedStream async disposal chooses the provider's async operation once, avoiding `Stream.DisposeAsync` redispatch into synchronous provider disposal. Pipe cleanup similarly owns and awaits its streams rather than closing them through both their wrappers and their async owners.
- NHibernate exposes `FlushAsync` but no session async close/dispose API in the installed version. Its scope awaits flushing, then runs ordered session close/dispose on a worker and retains error collection.
- UI scopes queue cleanup to their owning dispatcher and return that completion. Wizard screen cleanup finishes before its dialog closes. MVC form cleanup writes through `TextWriter.WriteAsync`; MVC's synchronous ending-tag helper renders into a local writer first.
- Native monitor/read/write locks, execution-state, DPI and token restoration require their acquiring thread. Their scope async members reject unsupported disposal without starting cleanup; use synchronous `using`. `FastLock`, `NonReentrantLock` and producer/consumer monitor scopes use `SyncOnlyActionScope<T>` rather than scheduling a release on the wrong worker.

The synchronized repository's async disposal schedules its complete lock acquisition, inner async cleanup and release boundary on one worker. That worker joins inner completion while retaining its native write lock; the caller receives the worker task. Other legacy CRUD async adapters are outside this disposal correction.

`ReadScope` and `WriteScope` now take their owner, extract and capture `owner.ThreadLock`, then acquire and invoke the open hook. A failed hook unwinds that captured lock. Reparenting in an open callback or before disposal cannot redirect its release.

These changes add no generic lifecycle/state holders to scopes. Empty/no-op cleanup can still return a completed task. Async scheduling has its ordinary task/delegate cost; the earlier synchronous allocation measurements are not a claim of allocation-free async disposal.

## Factory discovery and allocation

`Tools.Scope` supplies concrete return types for read/write/access scopes, restore-position, pages, borrowed hashers, locks, semaphore admission, callbacks, process single-instance and no-op values. Thin factories call the existing owner API so virtual/interface behavior and resource ownership remain there.

Windows and WinForms contribute static extension members targeting the core `Tools.Scope` class. Three separate probe assemblies compiled successfully with SDK 10.0.402 and explicit C# 14.0. The consumer requires `using Tools;` for extension lookup; removing it produces CS0117 even with the fully qualified `Tools.Scope.Rendering(...)` spelling. Core has no platform project reference, and the platform assemblies do not declare another `Tools.Scope` type.

```csharp
using Tools;

using var read = Tools.Scope.Read(collection);
using var restore = Tools.Scope.RestorePosition(stream);
await using var rendering = Tools.Scope.Rendering(control);
```

Factories for value scopes return their specific structs without boxing. Assigning those values to `IDisposable`, `IAsyncDisposable` or `IScope` boxes them. Existing rendering/highlight scopes are classes: their factories preserve their reference identity and ordinary object allocation. Capturing callbacks can still allocate; generic static callbacks plus an item avoid that closure. A regression warms the same factory call sites and measures 10,000 batches of read/write/access/restore/lock/semaphore/callback activations with resources created outside the measured region. Cleanup results and resource release are verified separately from the allocation measurement.

## Validation

The [result manifest](evidence/20261025-async-disposal/validation.json) records these final checks:

| Check | Passed | Skipped |
|---|---:|---:|
| Full core Release suite | 13,796 | 102 |
| Focused core Debug suite | 472 | 1 |
| Communications disposal | 4 | 0 |
| Data/DAC/NHibernate disposal | 23 | 0 |
| Application suite | 514 | 0 |
| MVC form scopes | 6 | 0 |
| Windows scope/session fixtures | 33 | 0 |
| WinForms scope/host/wizard fixtures | 203 | 0 |
| Ordinary profiler fixtures | 1,269 | 0 |

The final Windows Release solution build completed with zero errors and ten existing PKCS package audit warnings. The warmed factory test passed in both configurations with **zero thread-allocated bytes across 80,000 acquisitions and synchronous disposals**. Explicit performance captures were not rerun by the ordinary profiler suite.

The [source snapshot](evidence/20261025-async-disposal/source.zip) contains 4,106 C# source, project and build files, excluding ordinary resources and generated output. Its SHA-256 is `762CBAD082D5410C85164646A82322E7FB58C9B9F3136A57DBBD5E5FAD6AF9C2`; [source verification](evidence/20261025-async-disposal/source-verification.json) found no missing or mismatched files. The tested Release core assembly SHA-256 is `BCDCD67D4E7D7B635CFE81A09C3E7902ECB57379BFC2063AC4CF75B23BCCFEA4`. [Validation logs](evidence/20261025-async-disposal/validation-logs.zip) retain final checks and intermediate diagnostics; the manifest identifies the passing final logs.

Tests exercise pending operations, blocked synchronous finalizers, fault propagation, owner-thread rejection/release, queued UI completion, ordered cleanup, single provider disposal, open-hook reparenting/unwind and factory resource lifetimes. Three canonical guidance skills, workspace setup discovery and 470 local links were also checked. This correction does not claim a new throughput gain or replace the earlier performance captures.
