# Callback scope consolidation, 25 October 2026

A later feature adds typed pooled-array ownership and migrates production rental sites; see [borrowed array scopes](borrow-array-scopes.md). This report's source, allocation capture and validation artifacts remain the earlier callback snapshot.

Callback cleanup now uses `Scope` / `Scope<T>` with either an `Action` or a `Func<ValueTask>`. `SyncScope` / `SyncScope<T>` permit synchronous disposal only; `AsyncScope` / `AsyncScope<T>` permit asynchronous disposal only. `NoOpScope` / `NoOpScope<T>` explicitly perform no cleanup and can retain a borrowed item. These names replace the earlier `SyncActionScope`, `AsyncActionScope` and `SyncOnlyActionScope` families. Binary consumers must rebuild.

The readonly structs contain their callback and optional item, with no lifecycle flags, disposal-state classes, reentry suppression or cached pending cleanup. Required callbacks and resource dependencies are guarded at construction. Default required-resource scopes are invalid; use the no-op family for intentional empty cleanup. Keep one owner and one normal disposal per acquisition. Copying or boxing a scope does not create another acquisition or coordinate finalization.

## Disposal contracts

| Scope | Synchronous disposal | Asynchronous disposal |
|---|---|---|
| `Scope` with `Action` | Invokes the callback directly | Offloads through `Task.Run` and returns completion |
| `Scope` with `Func<ValueTask>` | Starts the callback on a worker outside the caller's synchronization context and waits for its complete operation | Awaits the callback's actual operation |
| `SyncScope` | Invokes the callback directly | Rejects with `NotSupportedException` before cleanup |
| `AsyncScope` | Rejects with `NotSupportedException` before cleanup | Awaits the callback's actual operation |
| `NoOpScope` | No cleanup | No cleanup |

The generic forms preserve the supplied `Item` and accept static callbacks to avoid closure allocation. Asynchronous callback failures, including a throw before returning a `ValueTask`, fault the returned disposal operation. Each disposal consumes its returned operation once, including pending source-backed `ValueTask` instances. Explicit repeated disposal invokes cleanup again.

Monitor/read/write acquisitions and other thread-affine scopes retain synchronous owner-thread release and reject async disposal without starting cleanup. Thread-neutral resources can use worker cleanup; UI resources use their dispatcher. Context, transaction and serialization owners remain their actual classes with domain identity and hooks. The final context bridge starts asynchronous finalization outside the caller's synchronization context before synchronously joining it. See the current [resource scope guidance](../guidelines/patterns/resource-scopes.md) for the API and ownership rules, including `Tools.Scope` factories.

## Current allocation screening

One fresh Release NUnit host captured 40 workloads on .NET 10.0.13, Windows x64, with two warmup fixtures and five measured fixtures per workload. All **200 samples** passed their semantic checks. Setup, warmup, verification, reopen, fixture disposal and evidence output are excluded from the measured region. Each sample uses a fresh deterministic fixture; no forced GC is used.

The following are median thread-allocated bytes per measured batch. The [allocation summary](evidence/20261025-callback-scope-consolidation/allocation-summary.json) includes every workload, operation count and timing observation; the [raw capture](evidence/20261025-callback-scope-consolidation/04c8e248e7504cd7a995ed314008422a/scope-consumers.json) retains every sample and its source/runtime identities.

| Current workload | Operations per batch | Allocated bytes |
|---|---:|---:|
| Concrete/interface access, read and write scope batches | 16,384 each | 0 |
| Dual-direction `Scope` synchronous callbacks, with/without item | 16,384 each | 0 |
| Synchronous-only `SyncScope`, with/without item | 16,384 each | 0 |
| Completed async callbacks through `Scope` / `AsyncScope`, with item variant | 16,384 each | 0 |
| List/dictionary reads and writes; cache hits | 2,048 each | 0 |
| List / dictionary enumeration | 2,048 each | 196,608 / 212,992 |
| Hasher borrowing | 16,384 | 524,288 |
| Clustered stream read / write | 128 each | 92,160 / 340,416 |
| ObjectStream read / update | 128 each | 175,104 / 446,912 |
| Memory indexed / Merkle indexed read | 32 each | 95,144 / 95,144 |
| File indexed / Merkle indexed read | 32 each | 178,952 / 179,168 |
| File Merkle mutation and commit | 32 | 41,850,736 |

Some raw workload labels retain their earlier spelling: `ScopeDirect` / `ScopeItemDirect` now exercise `NoOpScope` / `NoOpScope<T>`; `ActionScopeDirect` / `ActionScopeStateDirect` exercise `Scope` / `Scope<T>`; `TaskScopeCompletedDirect` exercises the completed `ValueTask` callback of `Scope`. The four newly named directional workloads exercise `SyncScope`, `SyncScope<T>`, `AsyncScope` and `AsyncScope<T>` directly.

The warmed factory regression also passed in Release and Debug: 10,000 batches of eight acquisitions and synchronous disposals allocate zero thread-local bytes, with resources and cached callbacks prepared before measurement. That test checks the factory facade; it does not establish allocation-free resource construction or async scheduling.

This is a **current-source allocation capture**, not a controlled before/after comparison. Short-batch elapsed times are retained as screening observations, without timing thresholds or a throughput claim. Completed async callback measurements do not describe suspended callbacks, worker scheduling or synchronous-over-async bridges. Hasher borrowing still allocates 32 bytes per operation in its existing pool path. Historical performance reports describe their own archived sources and cannot establish a speedup for this consolidation.

## Validation boundary

The Windows Release solution build passed with zero errors and ten existing PKCS package audit warnings. The [validation manifest](evidence/20261025-callback-scope-consolidation/validation.json) records the checks and their retained logs/TRX.

| Check | Passed | Skipped |
|---|---:|---:|
| Full Core Release, before final context bridge and semaphore guards | 13,831 | 102 |
| Final focused Core Release | 423 | 0 |
| Final focused Core Debug | 398 | 1 |
| Communications disposal | 4 | 0 |
| Data disposal | 3 | 0 |
| Application | 514 | 0 |
| MVC scope fixtures | 6 | 0 |
| Windows scopes/session | 34 | 0 |
| WinForms scopes/host/wizard/grid | 206 | 0 |
| Blazor rendering/lifecycle | 101 | 0 |
| Ordinary profiler fixtures | 1,272 | 63 explicit diagnostics not run |
| Explicit allocation capture, separate from ordinary fixtures | 1 | 0 |

The full Core run preceded the final context bridge and semaphore argument guards. The corrected source was rebuilt and then checked by the final focused Release/Debug and component suites above; **the full Core suite was not rerun on that final binary**. The focused tests cover worker initiation, pending cleanup, callback faults and cancellation, one-time consumption of source-backed operations, unsupported-direction rejection, lock-owner release and required constructor inputs. The Debug skip is the existing allocation-sensitive paging fixture. Not-run counts come from individual TRX outcomes; the ordinary profiler run leaves explicit diagnostic fixtures unselected, and the allocation capture above runs separately.

## Retained source and results

The [source archive](evidence/20261025-callback-scope-consolidation/source.zip) contains **4,105 C# source, project, solution and build/configuration files**, SHA-256 `5D1F4F4DCBFA542BA9DA282EEE6F96468ACCDE28A670E8867D182EAC7CE1F9FF`. It excludes generated output/results and ordinary resource assets; it is a code snapshot rather than a resource-complete distribution. [Verification](evidence/20261025-callback-scope-consolidation/source-verification.json) compared every archive entry with the preserved source: no missing or mismatched files. All 1,121 source hashes recorded by the capture also match that archive.

The measured Release Core assembly SHA-256 is `7E3EF985003AD9EEF7FBED831DB57271479088FF050A17E44DC662AB1232B88F`, version `3.1.3.0`, built with `BuildRevision=0`. [Binary identities](evidence/20261025-callback-scope-consolidation/binary-identities.json) preserve final validation assembly hashes before subsequent package builds. The [compressed validation archive](evidence/20261025-callback-scope-consolidation/validation-logs.zip) contains 25 final build/test/capture logs and TRX files; it excludes superseded failures and unrelated product checks. Its SHA-256 is `34DDA048DA1FEC3A7D4CC1AABBD86DB28B1AE4D884178E84C727D3AB0ECAE310`.

To repeat the capture, build Release with `BuildRevision=0` and run the explicit `ValueScopePerformanceTests.CaptureScopeConsumers` fixture in a fresh host with `SPHERE10_SCOPE_SAMPLES=5`, `SPHERE10_SCOPE_WARMUPS=2` and `SPHERE10_SCOPE_SCALE=1`. Set explicit `SPHERE10_SCOPE_SOURCEROOT`, `SPHERE10_SCOPE_DATAROOT`, `SPHERE10_SCOPE_ARTIFACTROOT` and `SPHERE10_SCOPE_LABEL` values. Keep the source, harness and binaries together. See the [profiler instructions](../../tests/Sphere10.Framework.Utils.Profiler.Tests/README.md) for environment controls and separate optional GC/context workloads.
