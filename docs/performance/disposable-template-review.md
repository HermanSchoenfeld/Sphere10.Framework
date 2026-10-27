# Disposable template and ownership review, 25–26 October 2026

This review restores the existing class-owner disposal template, corrects three reproduced cleanup failures and preserves retry after a failed clustered-stream close. Small resource-cleanup scopes remain concrete structs. This report adds correctness and compatibility evidence; it makes no new timing or allocation claims. Earlier performance reports and archives retain their historical source versions.

## Class owners and completion

`Disposable` remains the common abstract root. `SyncDisposableBase` and `AsyncDisposableBase` are abstract directional templates, renamed from the earlier class names. Sync-first cleanup runs on a worker for asynchronous disposal. Async-first synchronous adapters invoke the async factory outside the caller's synchronization context, join its complete operation and propagate its failure.

`ContextScope`, serialization contexts and plugin-registration owners retain their class identity and the existing successful-disposal guard. Sequential sync/async disposal does not repeat completed cleanup. A failed finalizer propagates without marking disposal successful; retry remains subject to the resource's actual behavior. `ContextScope` overrides the public virtual entrypoints to restore ambient context in the caller before delegating to `Disposable`. Its async entrypoint is a non-async front end. No additional scope-state object or disposal lifecycle is introduced.

Transaction adapters now invoke pending begin, close, commit, rollback and notification factories inside the worker bridge before joining them. This prevents an already-captured caller synchronization context from deadlocking the synchronous adapter.

Native UI class owners share the abstract `WinFormsSyncDisposableBase`, which derives from `SyncDisposableBase` and captures one owning WinForms context. The cross-platform async adapter remains a virtual `Task.Run` default. UI synchronous cleanup runs inline and leaves queued callbacks pending; the specialized async hook uses `Post` and a completion source to report cleanup success or failure. This avoids per-owner `Send` wrappers, creator-thread tracking and async completion boilerplate. Wizard disposal completes owned-screen cleanup before closing the dialog and preserves cleanup of screens removed by branching.

## Owning wrappers

`SynchronizedRepository.DisposeAsync()` schedules its existing virtual `Dispose()` primitive. Nested wrappers acquire and release their write scopes on the same worker under the configured recursion policy, retaining exclusion until owned cleanup completes. The child ownership primitive is `Dispose()`; an async-first child joins its full cleanup through its synchronous bridge.

`FragmentedStream.DisposeAsync()` likewise schedules virtual `Dispose()`, preserving subclass `Close()` and `Dispose(true)` cleanup. An `IDisposable` provider is released once during that operation, including dual-interface providers. Async-only providers are awaited after worker cleanup completes.

`ClusteredStream.Close()` retains its actual stream-owner guard. Reentrant close returns while cleanup is running, and a failure resets that guard so cleanup can be retried. A failed descriptor save retains the pending save and owner release. After a successful save, the pending owner callback is consumed before release, so a retry cannot release the acquisition twice or overwrite/remove a replacement stream opened by a successful finalizer. Registered `ActionDisposable` finalizers retain their existing successful-disposal guards: completed callbacks are skipped on retry, while failed and pending callbacks run. Five additional regression cases cover finalizer failure/reentry, replacement streams, descriptor-notification reentry and ObjectStream notification failure. This adds no generic scope-state machinery.

The audit also restores the original `sealed` declarations on `DACScope`, `TaskContextScope`, `ActionTransactionalScope<T>` and `TaskTransactionalScope<T>`, preserves the serialization finalization hook and class-reference identity assertions, and restores LoadingCircle's original encoded license prefix. Optional cleanup in `PropertyGridExTests` uses a nullable scope when no resource was acquired; required scope callbacks remain required.

## Reproduced failures

The retained [diagnostic harness](evidence/20261025-disposable-template-review/review-repros/Program.cs), [before output](evidence/20261025-disposable-template-review/review-repros/before.txt) and [after output](evidence/20261025-disposable-template-review/review-repros/after.txt) exercise the same three cases. These are review reproductions, separate from the NUnit suite totals below.

| Case | Before correction | After correction |
|---|---|---|
| Derived FragmentedStream managed cleanup after async disposal | 0 calls | 1 call |
| Nested synchronized repositories sharing a gate | Did not complete within 1,500 ms | Completed within 1,500 ms |
| Synchronous transaction commit with a queued caller context | Required an external pump; 1 queued callback | Completed without that pump; 0 queued callbacks |

The before Core binary is identified by SHA-256 `3FCC71301FAAF6FAD95D8C5EE20E29C0D3B12C2185E4F17E6101764211286D54`, matching the earlier [borrow-array snapshot](evidence/20261025-borrow-array-scopes/binary-identities.json). The harness was rerun against the final Core SHA-256 `D215A7A826F2A3B8E7152BF375C15D9E4A7821AA24622BB9C24F1EBA68460F52`; [reproduction metadata](evidence/20261025-disposable-template-review/review-reproductions.json) retains both identities. The bounded waits identify the reproduced deadlocks; they are not throughput measurements.

## Validation

The final Windows Release solution build completed with zero errors and ten existing PKCS package audit warnings. All 13 active test projects in the Windows solution and the additional active `Sphere10.Framework.NET.Tests` project completed their default Release suites, with no fixture filters: **30,573 passed, 29 failed and 310 result entries not executed**. The focused Core Debug check and full SystemExpert source/package checks also completed and are reported separately. The suites are not all green. [Validation](evidence/20261025-disposable-template-review/validation.json) retains the individual TRX counts, and [Framework binary identities](evidence/20261025-disposable-template-review/framework-binary-identities.json) identify the tested assemblies. Preliminary runs are excluded from this table.

The expanded suites exposed existing blockers. All 29 Windows failures occur while creating temporary SAM accounts/groups: 15 user-creation and 14 group-creation cases report access denied under this host's unelevated token. SystemExpert's unchanged boundary fixture fails in both consumer modes, reporting nine native calls in unchanged hardware implementations; the retained [source/provenance audit](evidence/20261025-disposable-template-review/systemexpert-boundary-audit.txt) identifies the original commits and missing Framework helpers. These outcomes remain failures; no assertions are waived and no skip is added.

Test-only setup corrections register the Demo fixture's missing default settings/usage services, compare actual local/named-host administrator-group SIDs instead of assuming the group is empty, remove a lookup for an already-removed product help label, and respect the grid's existing same-row deselection policy in native-input tests. Production SystemExpert code is unchanged by this review.

| Final check | Passed | Failed | Not executed | Status |
|---|---:|---:|---:|---|
| Full Core Release | 13,931 | 0 | 102 | Passed |
| Focused Core Debug | 229 | 0 | 0 | Passed |
| Application | 522 | 0 | 0 | Passed |
| WinForms | 1,310 | 0 | 1 | Passed |
| Communications | 4 | 0 | 0 | Passed |
| Consensus | 41 | 0 | 0 | Passed |
| CryptoEx | 7,920 | 0 | 0 | Passed |
| Data | 72 | 0 | 9 | Passed |
| Windows | 456 | 29 | 4 | Failed: SAM access denied |
| Windows LevelDB | 9 | 0 | 0 | Passed |
| MVC | 24 | 0 | 0 | Passed |
| Blazor | 857 | 0 | 127 | Passed |
| Ordinary profiler | 1,272 | 0 | 64 | Passed |
| HashLib4CSharp | 4,110 | 0 | 3 | Passed |
| NET | 45 | 0 | 0 | Passed |
| SystemExpert source consumer | 1,728 | 1 | 2 | Failed: existing native-boundary assertion |
| SystemExpert frozen `.49` package consumer | 1,727 | 1 | 3 | Failed: existing native-boundary assertion |

Source-consumer validation and package compatibility are distinct. Both SystemExpert builds completed with zero warnings and errors. [Consumer binary identities and dependency manifests](evidence/20261025-disposable-template-review/systemexpert-binary-identities.json) verify seven Framework project references in source mode and seven exact `.49` package references in package mode. The `.49` packages remain frozen at their earlier source snapshot; their check describes existing package compatibility, not the current Framework working tree. Explicit browser, profiling and hardware campaigns require their documented opt-in environments and are separate from the default suites.

Counts come from individual TRX result entries. Not-executed entries include existing ignored cases and unselected explicit fixture placeholders; those placeholders are not expanded into speculative case counts. The adapter's aggregate counters and console totals omit some placeholders. The targeted ClusteredStream ownership check passed all 10 cases, and the corrected SystemExpert fixtures passed all 12 cases; these checks are separate from the full-suite totals above.

The product's explicit PawnIO hardware case and symbolic-link fixture did not run in either mode. One existing real-mouse assumption also prevented a package-mode case from running because its window was not foreground; that case ran and passed in source mode. These host-dependent outcomes remain not executed. Browser and explicit performance campaigns were not forced by the ordinary suite run.

The [preliminary-run index](evidence/20261025-disposable-template-review/preliminary-runs.json) preserves earlier failed and aborted outcomes separately. The first full pass's 31 Windows failures and 11 product failures remain recorded; they are superseded by the final rows rather than relabeled. Earlier Demo-host diagnostics reproduce the fixture's missing-service null reference with both then-current and frozen `.49` Framework binaries. These diagnostic limits and the unselected explicit campaigns prevent an all-tests-passed claim.

## Source and evidence boundary

The [source archive](evidence/20261025-disposable-template-review/source.zip) contains 4,250 code/build files, including 4,041 C# and 131 Razor sources. Its SHA-256 is `374B51A5C2C80760C74D585D0FBE4B72753734A7592523BEB374063CE1AEC0BE`. Every archived entry was compared with its live file and manifest hash: zero missing files and zero mismatches. [Source verification](evidence/20261025-disposable-template-review/source-verification.json) records the exact selection and every file hash.

Selection uses cached Git paths plus nonignored untracked live paths. Generated outputs and results are excluded by their output paths; legitimate `Packages` source directories are included, including the three unchanged Windows package source/test files omitted by the older archive. Ordinary resource assets are excluded, so this is a code snapshot rather than a resource-complete distribution.

The separate [SystemExpert snapshot](evidence/20261025-disposable-template-review/systemexpert-source.zip) preserves 544 code/build/configuration files, including 529 C# sources, at product HEAD `1c8aae8d6f198532bbd45bd6366d12f1d4a1051e`. Its SHA-256 is `5777053AE76D4E0535F59A4DD9CF3594E68B5F6C23927AE66C5754E8F4589BF7`; [verification](evidence/20261025-disposable-template-review/systemexpert-source-verification.json) records zero missing entries or live/archive hash mismatches. [Provenance](evidence/20261025-disposable-template-review/systemexpert-source-provenance.json) compares all 541 files from the earlier frozen product snapshot: only the two test files above differ, no production file differs, and all earlier source paths remain represented. The additional entries are `.gitignore`, `LICENSE` and `README.md`. Earlier frozen product evidence and the `.49` package pin remain unchanged.

An [independent final source check](evidence/20261025-disposable-template-review/independent-source-sanity.json) rechecked all 4,250 Framework and 544 SystemExpert live-file hashes and both archive hashes without mismatches. Ordinary engineering notes such as `src/TODO.md` are outside this code/build selection.

The [consolidated validation archive](evidence/20261025-disposable-template-review/validation-logs.zip) retains 123 files, including final build logs/TRX, the first full pass and earlier failed/aborted diagnostics. Its SHA-256 is `6429C2D47E211E80272C93A108B7DDAB530EBF96A71A052FB5DB99350687AA29`. [Archive verification](evidence/20261025-disposable-template-review/validation-log-files.json) checked every archived entry against its source and manifest hash with zero errors. `final/` and `preliminary/` prefixes keep their boundaries explicit.

[Repository sanity checks](evidence/20261025-disposable-template-review/final-repository-sanity.json) confirm both diff checks passed, both indexes are empty and both HEADs are unchanged. The isolated storage-format stash remains unchanged and separate. No commit or push was made. Existing public class names and inheritance changed, so binary consumers require rebuilding. The remaining SAM and native-boundary failures prevent an all-tests-passed verdict.
