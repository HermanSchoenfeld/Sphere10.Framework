---
name: unit-testing
description: NUnit constraint-model tests only — Assert.That, never ClassicAssert or legacy Assert. Trigger when writing or editing tests.
---

# Unit Testing Skill

Read [testing guidance](../../../docs/guidelines/patterns/testing.md) for runner, assertion and behavioral coverage requirements.

## Framework
- NUnit only. Test projects are libraries with `IsTestProject=true`, `IsPackable=false`, and package references to `NUnit`, `NUnit3TestAdapter`, and `Microsoft.NET.Test.Sdk`. Use the versions already configured for the repository.
- Run and discover tests through `dotnet test` and the IDE test explorer. Add test projects to the solution and use the same runner in CI.
- Do not implement .NET unit or integration suites as console programs, top-level statement runners, custom assertion counters, or hand-written pass/fail exit codes.

## Assertions — constraint model exclusively

```csharp
// ✅ Correct
Assert.That(result, Is.EqualTo(expected));
Assert.That(flag, Is.True);
Assert.That(collection, Is.Not.Empty);
Assert.That(() => Foo(), Throws.InstanceOf<InvalidOperationException>());

// ❌ Never
ClassicAssert.AreEqual(expected, result);
Assert.AreEqual(expected, result);
Assert.IsTrue(flag);
```

- Add a descriptive failure message where it aids diagnosis:
  `Assert.That(result, Is.True, "Signature must verify against the correct public key");`
- Await async assertions: `await Assert.ThatAsync(async () => await DoWorkAsync(), Throws.TypeOf<InvalidOperationException>());`. Match the actual awaited operation and use NUnit's constraints directly; do not pass async-void delegates to synchronous exception assertions or block the operation to make a test synchronous.

## Structure
- `[TestFixture]`, `[Test]`, `[TestCase(...)]`, `[TestCaseSource(...)]`, `[Values(...)]`, and `[Repeat(n)]` as appropriate.
- Organize related fixtures in folders within the test project for their production library. Do not create a separate project for each scenario or former console harness. Preserve meaningful library dependency boundaries.
- Give independent behaviors separate discoverable tests. Parameterize input/output variants instead of hiding them in a loop inside a single omnibus test. Loops remain appropriate for checking one result collection or exercising a sequence within a scenario.
- Use NUnit constraints directly; do not hide them behind homemade `Check`, `Assert`, or `Throws` wrappers.
- Use `[SetUp]`/`[TearDown]` or disposable fixtures for resource lifetimes. If parallel tests mutate fixture fields, use `FixtureLifeCycle(LifeCycle.InstancePerTestCase)` or otherwise isolate that state.
- `[Parallelizable(ParallelScope.Children)]` on fixtures unless tests share mutable state.
- `Tools.NUnit` helpers exist (e.g. 2D array formatting) — prefer them over hand-rolled output.
- New test files get the standard license header (see [code-style](../code-style/SKILL.md)).
- WinForms/control fixtures use `[Apartment(ApartmentState.STA)]` and the required UI dispatch/message-loop helpers. Tests touching native windows, modal dialogs, application singletons or shared process state must not opt into unsafe parallel execution merely to satisfy a fixture default.

## Choose meaningful coverage

- Test behavior and contracts, including important rejection, cancellation and ownership paths when changed. Avoid tests that only repeat member assignments or mirror private implementation structure. Reversible, low-impact edits do not require invented test suites.
- For paired sync/async hierarchies, exercise the chosen primitive and opposite-direction adapter where completion, failure, cancellation, disposal or thread affinity changes. Do not validate async support merely by checking a returned task exists.
- Run the affected project/fixture and complete required repository checks. Broaden or repeat only for a new change, failure or unresolved concern. Report what was actually executed; source inspection is not a passing test run.

## Integration tests
- Keep repository/filesystem/service integration coverage as categorized NUnit fixtures; unit tests and integration tests share the runner, but remain distinguishable.
- Tests requiring an existing user repository, credentials, or live services are opt-in with `[Explicit]` and named categories. Supply configuration through test parameters or environment variables; never bake secrets or workstation paths into fixtures.
- Preserve coverage when migrating a harness: retain its fixtures and behavioral checks, replace custom failure plumbing with NUnit assertions, and verify test discovery and execution. Documentation and CI commands must use the migrated runner.
