# Testing

Read the [unit-testing skill](../../../agents/skills/unit-testing/SKILL.md) for fixture structure, resource lifetimes, async assertions and meaningful coverage.

## Framework & Conventions

- Use **NUnit** as the test framework.
- **Do not** use `ClassicAssert` or any legacy NUnit assertion style (`Assert.IsTrue`, `Assert.AreEqual`, `ClassicAssert.IsTrue`, etc.).
- **Always** use modern **constraint-model assertions** via `Assert.That(...)` or awaited `Assert.ThatAsync(...)`:
  ```csharp
  // ✅ Correct
  Assert.That(result, Is.EqualTo(expected));
  Assert.That(flag, Is.True);
  Assert.That(collection, Is.Not.Empty);
  Assert.That(() => Foo(), Throws.InstanceOf<InvalidOperationException>());
  await Assert.ThatAsync(async () => await FooAsync(), Throws.InstanceOf<InvalidOperationException>());

  // ❌ Wrong — do not use
  ClassicAssert.AreEqual(expected, result);
  ClassicAssert.IsTrue(flag);
  Assert.IsTrue(flag);
  ```
- Include a descriptive failure message where it aids diagnosis: `Assert.That(result, Is.True, "Signature must verify against the correct public key");`
- Use `[TestFixture]`, `[Test]`, `[TestCase(...)]`, `[Values(...)]`, `[Repeat(n)]` attributes as appropriate.
- Use `[Parallelizable(ParallelScope.Children)]` on test fixtures unless tests share mutable state.

## Runner and project contract

Test suites are NUnit library projects with `IsTestProject=true`, `IsPackable=false`, and references to `NUnit`, `NUnit3TestAdapter` and `Microsoft.NET.Test.Sdk`. Use the existing project and central package configuration; flags can be inherited or imported rather than repeated in every project. The [application test project](../../../tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj) shows the explicit project contract, [Directory.Build.props](../../../Directory.Build.props) supplies the default non-packable setting, and [Directory.Packages.props](../../../Directory.Packages.props) owns the package versions. The packable [NUnit helper library](../../../src/Sphere10.Framework.NUnit/Sphere10.Framework.NUnit.csproj) and [database helper library](../../../src/Sphere10.Framework.NUnit.DB/Sphere10.Framework.NUnit.DB.csproj) are reusable production packages, not test suites.

Discover and run a selected test project with `dotnet test` or the IDE test explorer. The current [CI workflow](../../../.github/workflows/main.yml) builds the CrossPlatform solution, transfers compiled test assemblies to its test job, and runs seven named assemblies with `dotnet vstest` and the NUnit adapter. It does not run every solution fixture or the WinForms suite; run the affected project explicitly when it falls outside that CI selection.

Do not implement .NET unit or integration suites as console programs, top-level runners, custom assertion counters or hand-written pass/fail exit codes. Preserve fixtures and behavioral checks when migrating a harness, and verify discovery and execution through NUnit.

Await async constraint assertions; do not pass async-void delegates to synchronous exception assertions or block an async operation merely to make its test synchronous. WinForms/control fixtures require their STA and UI dispatcher/message-loop setup. Shared native windows, modal dialogs, application singletons and process state must not be made unsafely parallel merely to satisfy the fixture default. See the [WinForms test project](../../../tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj) and [async modal fixtures](../../../tests/Sphere10.Framework.Windows.Forms.Tests/AsyncModalTests.cs) for the Windows target, `[NonParallelizable]`, `[Apartment(ApartmentState.STA)]` and real message-loop usage.

Inside a running WinForms dispatcher, use `await Assert.ThatAsync(operation,
Throws.InstanceOf<ExpectedException>())` for asynchronous failure checks. With
the repository's NUnit 4.x package, `Assert.ThrowsAsync`, `CatchAsync` and
`DoesNotThrowAsync` are synchronous bridges; do not nest them inside the UI loop.
Their blocking/message-pump behavior can interrupt the fixture's dispatcher or
produce a different exception that accidentally satisfies a broad assertion.
Await the operation, assert its precise failure, then continue on the same live
dispatcher. NUnit documents the move from sync-over-async to awaitable constraints
in [its NUnit 4 guidance](https://docs.nunit.org/articles/nunit/Towards-NUnit4.html#proper-asyncawait-you-can-now-await-the-asserts).

Test changed behavior and contracts, including material rejection, cancellation, ownership and completion paths. Avoid tests that mirror private implementation details or only repeat assignments. Run affected fixtures and required checks; broaden or repeat for a new change, failure or unresolved concern, and report what actually ran. The skill distinguishes opt-in live-service integration fixtures from ordinary unit/integration coverage.

See [sync/async hierarchies](sync-async.md) for opposite-direction adapter checks and [application ownership](application-ownership.md) for lifecycle and cleanup contracts. New test source files follow [Code Styling](../code-styling.md).

## WinForms UI test contexts

`[Apartment(ApartmentState.STA)]` selects the apartment; it does not provide a UI
dispatcher or guarantee that an awaited continuation returns to the constructing
thread. NUnit can install `SafeSynchronizationContext`, which posts continuations
to worker threads. Native control creation or layout after an await can then race
disposal on the fixture thread, including `Dispose() cannot be called while doing
CreateHandle()` failures.

Use the existing `RunWithMessageLoop` fixture approach: install a
`WindowsFormsSynchronizationContext`, run an owned form through `Application.Run`,
await the test inside its live dispatcher, and restore the previous context with
a cleanup scope. Keep the watchdog and exception reporting used by the existing
fixtures. See [CrudGridRefreshTests](../../../tests/Sphere10.Framework.Windows.Forms.Tests/CrudGridRefreshTests.cs)
for initial binding and disposal coverage and
[AsyncModalTests](../../../tests/Sphere10.Framework.Windows.Forms.Tests/AsyncModalTests.cs)
for native modal dispatch. Keep fixtures that share native or application state
nonparallel.

This also applies to construction-only tests when controls can begin asynchronous
work. Changing a nested control's visibility can create handles and trigger
`OnLoad` before its screen is hosted. Retain the construction assertions, then
join any pending initialization/binding on the owning dispatcher before disposal.
In intentional disposal-during-read tests, cancel or release the controlled read
and await its completion to verify that it cannot touch disposed controls. Do not
substitute timing sleeps or a test-only production dispatcher redesign for the
correct fixture context and resource lifetime.

For first binding, layout reveal or deferred native creation, capture the
constructing thread ID and observe `HandleCreated` callbacks. Record callbacks
without throwing assertions through native dispatch, then assert their thread
IDs after the awaited operation completes. Verify loading, visibility and actual
results as well as thread ownership; passing an isolated constructor case under
an automatically installed Forms context does not prove it is safe under the
full NUnit fixture context.

## Startup memory regressions

Always compare startup memory before and after dependency/runtime/host upgrades
and changes to startup, cache construction or resource loading. Routine text or
layout edits need this check only when they affect those allocations or address
a measured memory regression. Use an available prior build or isolated revision
for the baseline, preserve unrelated work, and report a missing baseline rather
than inventing a comparison.

Use a fresh isolated process for each revision so static caches and earlier
tests do not contaminate the result. Exercise the actual runner, registrations,
forms and resource initialization with temporary settings and replacements for
global input hooks, autorun and other external effects. Compare the same runtime,
architecture, configuration, dependency mode and startup workload. A test host's
own baseline is part of its measurements; distinguish that from the published
application. Check the actual published form when bundling/runtime differences
could explain the result.

Record private bytes, working set and managed heap separately at comparable
startup checkpoints, including after the main window is ready. Investigate a
material increase before calling an upgrade complete. Use retained managed
memory, allocation counts and a focused heap/owner trace to distinguish live
objects, collectible startup garbage and native/mapped memory. A diagnostic full
collection may clarify retention in the isolated host; label those readings
separately. Do not mask the cause with production `GC.Collect`, working-set
trimming or arbitrary memory limits. Report actual before/after numbers and
measurement conditions, not just a successful build or a claimed improvement.

For high-cardinality caches, inspect
[group count and bucket capacity](collections.md#sparse-groups-and-capacity).
Add a regression when the allocation is material: construct a representative
bounded fixture outside the measured interval, warm unrelated initialization,
then measure the relevant operation's allocation with an appropriate API such
as `GC.GetAllocatedBytesForCurrentThread()` for synchronous work. Select a budget
from that workload, verify populated results and collision/growth behavior, and
avoid brittle elapsed-time or process-private-byte assertions in the unit test.
The fresh-host comparison remains the evidence for the application's footprint.
