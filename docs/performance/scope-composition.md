# Direct scope finalization, 25 October 2026

The current callback API and its validation are documented in [callback scope consolidation](callback-scope-consolidation.md). Callback names and measurements below describe this report's historical source snapshot.

This report describes the retained source and binaries identified below. A subsequent async-disposal review found remaining inline adapters and corrected their execution contracts; see [async disposal corrections](async-disposal-corrections.md). The measurements and archives here remain historical evidence of the recorded version, rather than validation of the later corrections.

Scopes now finalize their resource directly. The lifecycle components, phase flags, pending-disposal owners and struct facades over private state classes have been removed. `SyncActionScope` and `AsyncActionScope` are small value types containing their callback and optional item. Context, transaction and serialization types retain their original class identity and domain data.

`SyncActionScope.Dispose()` invokes its callback synchronously. Its `DisposeAsync()` schedules that synchronous callback through `Task.Run`, preserving the original `SyncScope` contract. `AsyncActionScope.DisposeAsync()` awaits its asynchronous callback; synchronous disposal waits for the returned operation. Dispose one owning scope once. Scope copies do not transfer ownership or coordinate finalization.

The async adapter audit follows each original contract. Former `SyncScope` and synchronous context/transaction adapters offload their synchronous callbacks. Existing thread-affine UI overrides retain their calling-thread behavior, and DPI/token scopes retain their original rejection of async disposal. Native monitor/read/write scopes retain the thread-affine behavior of their original `ActionDisposable` providers.

The abstract context and transaction adapters retain their original domain inheritance. Concrete types no longer forward through nested `State` classes. Non-abstract types introduced with a `Base` suffix were removed or renamed; the shared lock contract is `IReaderWriterLock`. Existing public subclass hooks were restored, including rendering cleanup and transaction callbacks.

## Ownership corrections

Removing scope bookkeeping exposed existing callers that relied on repeated finalization being suppressed:

- Endian readers/writers now offer `leaveOpen` constructors. ObjectStream uses them when returning a stream to its caller, so temporary serialization helpers do not close that caller's stream or release its retained lock. Existing constructor defaults still close their stream.
- ClusteredStream retains its ordinary stream close-once contract. Descriptor persistence happens before closure is committed, allowing a failed save to be retried; reentrant or repeated close cannot release its acquisition again. Read post-operation notifications run when the returned stream's owner closes it.
- HashingStream clears its retained borrow after returning the hasher successfully, preserving ordinary repeated stream disposal.
- Each ObjectSpace owns a separate cleanup bag. The previous mutable `Disposables.None` singleton let one space invoke cleanup registered by another space.

These changes preserve resource ownership without adding lifecycle machinery to scope primitives. Their regression coverage includes failed descriptor persistence/retry, reentrant stream close, independent object-space cleanup across all eight storage traits, and distinct simultaneous hasher borrows after repeated stream disposal.

## Comparison boundary

The baseline is the **previous completed struct scope abstraction**, whose frozen source is the [earlier optimized archive](evidence/20261024-scope-abstraction/optimized-source.zip), SHA-256 `FF753CEE764BA969E2362338B2890BC5B847F5A5C45478C1B1FB016502017D12`. It already contains the earlier class-to-struct improvements documented in the [value-scope report](value-scopes.md) and [scope abstraction report](scope-abstraction.md). Those gains are not attributed again here.

The comparison includes direct finalization, restored async adapters and the ownership corrections above. Storage results therefore describe the complete change, not the effect of removing phase fields in isolation.

Ten fresh serial NUnit hosts ran on .NET 10.0.13, x64: baseline/optimized/optimized/baseline for 36 synchronous consumer workloads and separately for four context workloads, plus one host per variant for two GC stress workloads. Each host used five warmup fixtures and five measured fixtures. Normal/context medians pool ten measurements per variant; GC medians pool five two-million-scope batches. All 820 measured samples completed with verified checksums and operations. Both variants used the same local data directory. Setup, validation, reopen, disposal and source hashing are outside the measured consumer region.

The exact [baseline harness](evidence/20261024-scope-composition/harness/baseline.zip) and [optimized harness](evidence/20261024-scope-composition/harness/optimized.zip) each contain four source files. Three are byte-identical. The fourth changes scope type names and adapts the completed callback from `Task` to `ValueTask`; workload labels and operations remain unchanged. The retained reader checks this exact adaptation. Its inherited context-harness conditions contain an older explanation for excluding async comparisons; this study compares synchronous context batches and tests async behavior separately.

## Allocations and layout

Native lock scopes and direct action scopes remain allocation-free on their synchronous paths. There is no additional GC allocation reduction to claim for those paths. On x64, access/read/write/restore values shrink from 24 to 16 bytes; synchronous action scopes shrink from 16 to 8 bytes; asynchronous action scopes shrink from 24 to 8 bytes. Seven profiler tests enforce these layout budgets.

| Measured batch | Operations | Before bytes | After bytes | Reduction |
|---|---:|---:|---:|---:|
| ObjectStream update | 128 | 672,640 | 445,888 | 33.71% |
| ObjectStream read | 128 | 177,152 | 174,080 | 1.73% |
| Memory indexed read | 32 | 98,856 | 95,144 | 3.75% |
| Memory Merkle indexed read | 32 | 98,856 | 95,144 | 3.75% |
| File indexed read | 32 | 182,664 | 178,952 | 2.03% |
| Memory mutation and flush | 32 | 24,757,376 | 24,263,160 | 2.00% |
| File Merkle mutation and commit | 32 | 42,360,468 | 41,837,856 | 1.23% |
| Root action context | 16,384 | 5,898,240 | 5,767,168 | 2.22% |
| Nested action context | 16,384 | 1,179,648 | 1,048,576 | 11.11% |

Root/nested contexts allocate 352/64 bytes per operation, down from 360/72. They remain real reference owners. ObjectSpace construction also creates its own cleanup bag; fixture construction is excluded from the operation table.

Both GC stress paths allocate zero measured bytes, with zero Gen0 collections and zero recorded GC pause before and after. Borrowing a hasher still allocates 32 bytes per operation in its existing pool path. Async offloading through `Task.Run` schedules work and has its normal allocation cost; the synchronous allocation results do not describe that adapter.

## Timing limits

Short-batch timings are mixed. Pooled screening medians show less elapsed time for direct action finalization, concrete read locks, list reads and context construction. Dictionary reads, enumeration and several storage reads show more elapsed time. These results do not establish an overall throughput gain.

Host variation is substantial: the dictionary-read host medians were 2,034 and 1,801 ticks before, versus 1,866 and 3,514 afterward. Concrete read-lock host medians also span 7,535–14,334 ticks across variants. Pooled percentages alone would overstate the certainty of a gain or regression. The [full comparison](evidence/20261024-scope-composition/comparison.json) and CSV retain every workload, including unfavorable results. No timing thresholds are asserted.

No completed fresh BenchmarkDotNet comparison is claimed for this final source. Earlier confirmation attempts exhausted local temporary storage and are not used as evidence. Reconfirm dictionary, enumeration and storage throughput with independent longer BenchmarkDotNet runs before drawing a throughput conclusion. Earlier historical reports remain unchanged.

## Validation and retained evidence

The final Windows solution build passed with zero errors and ten existing PKCS package audit warnings. Debug and profiler builds passed without warnings or errors.

| Final check | Passed | Skipped | Elapsed |
|---|---:|---:|---:|
| Full Core Release | 13,734 | 102 | 4 min 37 sec |
| Focused Debug scopes/context | 410 | 1 | 9 sec |
| Windows | 35 | 0 | 460 ms |
| WinForms | 143 | 0 | 15 sec |
| DAC | 20 | 0 | 423 ms |
| MVC | 4 | 0 | 188 ms |
| Full ordinary profiler | 1,269 | 0 | 50 sec |

The profiler total includes nine evidence-integrity cases and seven layout cases. The Debug paging skip covers an existing generic cache-key boxing path. Explicit performance fixtures are invoked separately from the ordinary profiler suite. Gated async tests prove that worker-based adapters return while a synchronous callback is blocked, await actual completion and deliver callback failures through the returned operation. They cover action, context, transaction, serialization and stream adapters; UI override and synchronous-only native contracts have separate regressions.

The [validation manifest](evidence/20261024-scope-composition/validation.json) and [logs](evidence/20261024-scope-composition/validation-logs.zip) retain the final outcomes. Earlier incomplete, disk-limited or pre-correction runs are excluded from these final pass counts.

The [optimized source archive](evidence/20261024-scope-composition/optimized-source.zip) contains 4,097 source/build files and hashes to `BCB9C88958F52BE3E783ADA3AE5648332B984FA912D3F2F23C1929D39A73DC72`. Every entry was independently compared with current source: zero missing files and zero mismatches. The measured Core SHA-256 is `1FB9A74E407A8FDE382D25785DE457C06884199BFE82EC0D0E77BB654E5CDD01`. See [source verification](evidence/20261024-scope-composition/source-verification.json).

To repeat the measurements, build each source variant in Release with `BuildRevision=0`. Run `ValueScopePerformanceTests.CaptureScopeConsumers` in separate serial hosts with `SPHERE10_SCOPE_SAMPLES=5`, `SPHERE10_SCOPE_WARMUPS=5`, `SPHERE10_SCOPE_SCALE=1`, and explicit `SPHERE10_SCOPE_SOURCEROOT`, `SPHERE10_SCOPE_DATAROOT`, `SPHERE10_SCOPE_ARTIFACTROOT` and `SPHERE10_SCOPE_LABEL`. Run the context fixture similarly, and select `CriticalGcStress,ReadGcStress` through `SPHERE10_SCOPE_WORKLOADS` for GC batches. Keep each variant's matching binaries and harness together. The [comparison script](evidence/20261024-scope-composition/compare-captures.ps1) recomputes medians from retained raw JSON; its default paths operate within the evidence directory.

Public return types and former inheritance extension points changed, so binary consumers must be rebuilt. Existing context, transaction and serialization classes retain their domain extension points. Scope use requires one owner and one finalization call. The isolated storage-format work remains unapplied.
