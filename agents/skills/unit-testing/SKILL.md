---
name: unit-testing
description: NUnit constraint-model tests only — Assert.That, never ClassicAssert or legacy Assert. Trigger when writing or editing tests.
---

# Unit Testing Skill

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

## Structure
- `[TestFixture]`, `[Test]`, `[TestCase(...)]`, `[TestCaseSource(...)]`, `[Values(...)]`, and `[Repeat(n)]` as appropriate.
- Organize related fixtures in folders within the test project for their production library. Do not create a separate project for each scenario or former console harness. Preserve meaningful library dependency boundaries.
- Give independent behaviors separate discoverable tests. Parameterize input/output variants instead of hiding them in a loop inside a single omnibus test. Loops remain appropriate for checking one result collection or exercising a sequence within a scenario.
- Use NUnit constraints directly; do not hide them behind homemade `Check`, `Assert`, or `Throws` wrappers.
- Use `[SetUp]`/`[TearDown]` or disposable fixtures for resource lifetimes. If parallel tests mutate fixture fields, use `FixtureLifeCycle(LifeCycle.InstancePerTestCase)` or otherwise isolate that state.
- `[Parallelizable(ParallelScope.Children)]` on fixtures unless tests share mutable state.
- `Tools.NUnit` helpers exist (e.g. 2D array formatting) — prefer them over hand-rolled output.
- New test files get the standard license header (see [code-style](../code-style/SKILL.md)).

## Integration tests
- Keep repository/filesystem/service integration coverage as categorized NUnit fixtures; unit tests and integration tests share the runner, but remain distinguishable.
- Tests requiring an existing user repository, credentials, or live services are opt-in with `[Explicit]` and named categories. Supply configuration through test parameters or environment variables; never bake secrets or workstation paths into fixtures.
- Preserve coverage when migrating a harness: retain its fixtures and behavioral checks, replace custom failure plumbing with NUnit assertions, and verify test discovery and execution. Documentation and CI commands must use the migrated runner.
