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

Test changed behavior and contracts, including material rejection, cancellation, ownership and completion paths. Avoid tests that mirror private implementation details or only repeat assignments. Run affected fixtures and required checks; broaden or repeat for a new change, failure or unresolved concern, and report what actually ran. The skill distinguishes opt-in live-service integration fixtures from ordinary unit/integration coverage.

See [sync/async hierarchies](sync-async.md) for opposite-direction adapter checks and [application ownership](application-ownership.md) for lifecycle and cleanup contracts. New test source files follow [Code Styling](../code-styling.md).
