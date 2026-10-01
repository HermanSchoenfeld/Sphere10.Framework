# Sphere10.Framework.Application.Tests

.NET 10 NUnit tests for the UI-independent application foundation in `Sphere10.Framework.Application.UI`. The project references only [Sphere10.Framework.Application](../../src/Sphere10.Framework.Application/README.md); it runs without a browser, ASP.NET Core host or Windows desktop runtime.

The suite protects the contracts shared by the Blazor and WinForms application frameworks: blocks, menus, metadata snapshots, builders, activation policies, actions and screen lifecycle defaults. Small neutral classes stand in for the screen contracts and product factories supplied by a UI adapter.

## Run the tests

Install the .NET 10 SDK and run commands **from the repository root**. The test command restores and builds dependencies, then runs NUnit. `BuildRevision=0` supplies an explicit local build revision.

```powershell
dotnet test tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj -c Release -p:BuildRevision=0
```

Run the metadata and builder fixtures while changing shared definitions:

```powershell
dotnet test tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~ApplicationMetadataTests|FullyQualifiedName~ApplicationBuilderTests"
```

Check the dependency boundary separately:

```powershell
dotnet test tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj -c Release -p:BuildRevision=0 --filter "TestCategory=Architecture"
```

List discovered cases or save NUnit execution results through the standard test runner:

```powershell
dotnet test tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj -c Release -p:BuildRevision=0 --list-tests
dotnet test tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj -c Release -p:BuildRevision=0 --logger "trx;LogFileName=application-tests.trx" --results-directory obj/test-results/application
```

Quote combined filter expressions so the shell does not interpret `|`. Use `--no-build` only when the matching configuration has already been built with the current source.

## Fixture guide

| Fixture | What it protects |
| --- | --- |
| [UI/ApplicationMetadataTests.cs](UI/ApplicationMetadataTests.cs) | Non-generic block storage through `IApplicationMenu`, defensive menu/item/catalog arrays, snapshots and indexes, stable IDs, ordering, copied event subscriptions, policy conflicts and default-screen selection/validation. |
| [UI/ApplicationBuilderTests.cs](UI/ApplicationBuilderTests.cs) | Independent products from shared builder factories, menu/item definition isolation, copied parameters, synchronous/asynchronous action replacement, screen/action exclusivity and incomplete-definition rejection. |
| [UI/ScreenActivationPolicyTests.cs](UI/ScreenActivationPolicyTests.cs) | Atomic declaration batches, explicit/inferred policy rules, platform validation, locked instance policies and invalid type/mode rejection. |
| [UI/ApplicationActionTests.cs](UI/ApplicationActionTests.cs) | Resolving services from the executing scope, awaited callback completion, cancellation and observable failures. |
| [UI/ApplicationScreenContractTests.cs](UI/ApplicationScreenContractTests.cs) | Default help metadata, navigation permission and cancellation-aware lifecycle methods without UI infrastructure. |
| [Architecture/ApplicationDependencyTests.cs](Architecture/ApplicationDependencyTests.cs) | Application assembly dependency closure remaining independent of UI assemblies. |

The shared `ApplicationBlock` stores `IApplicationMenu` directly, so different menu implementations can coexist. Collection-valued metadata properties return arrays. The tests mutate returned slots and verify that stored menu/item membership, catalog order and ID lookup remain intact, including through the catalog decorator. Array elements retain their original object identities; the ownership guarantee applies to membership rather than deep cloning arbitrary objects. Platform adapters retain their native typed interfaces and ownership rules. The shared builder fixtures use neutral factories; they do not require every platform facade to return a new owned object on repeated Build calls.

## Extend coverage without adding a UI dependency

Add a focused case beside the relevant fixture. Derive a small test builder from the shared builder base when testing factory behavior, or implement the shared screen contract directly when testing policies and lifecycle defaults.

For example, a screen with the default lifecycle needs no component or control base class:

```csharp
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ExampleScreenTests {
	[Test]
	public async Task DefaultScreenAllowsDeactivation() {
		IApplicationScreen screen = new ExampleScreen();
		Assert.That(await screen.CanDeactivateAsync(), Is.True);
	}

	private sealed class ExampleScreen : IApplicationScreen { }
}
```

This illustrates the pattern already covered by `ApplicationScreenContractTests`; add new scenarios for changed behavior rather than duplicating that case. Include the repository license header in new files and follow [the NUnit test guidance](../../.github/skills/unit-testing/SKILL.md).

Use independent fixture state and controlled task completion for asynchronous behavior. Test invariants such as an invalid policy batch leaving the registry unchanged, or a built snapshot retaining its original metadata after the input collections change. Keep actual component/control activation, UI dispatch, retained-screen rendering and platform disposal in the adapter test projects.

## Dependency and adapter validation

`ApplicationDependencyTests` traverses referenced Sphere10 assemblies from the Application assembly and rejects UI assembly families, including ASP.NET Core, JS interop, WinForms, WPF, MAUI and platform-specific framework projects. An `Architecture` failure should be investigated as a dependency boundary change; adding a UI reference to this test project is not a substitute for an adapter test.

The separate [NuGet consumer validation](../../scripts/package-consumers/README.md) checks the resolved package and framework references of an external Application-only consumer. It complements assembly-reference tests by examining the packaged dependency graph.

When shared contracts change, also run the [Blazor adapter suite](../Sphere10.Framework.Web.AspNetCore.Blazor.Tests/README.md):

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~SharedPresentationAdapterTests|FullyQualifiedName~ApplicationBlockTests"
```

On Windows, run the native adapter fixture as well:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~ApplicationPresentationAdapterTests"
```

See [the Application library guide](../../src/Sphere10.Framework.Application/README.md), [the Blazor guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) and [the WinForms guide](../../src/Sphere10.Framework.Windows.Forms/README.md) for their respective integration patterns. Both solutions and CI include this shared suite.

If a filter matches no tests, inspect `--list-tests`; fixture namespaces do not necessarily mirror the `UI` folder. SDK and restore failures happen before NUnit execution, so check `dotnet --info` and the first reported build error before investigating a fixture.