# WinForms AOT Tester

An independent clone of the WinForms Tester for investigating unofficial .NET 10 Native AOT publishing. It retains the complete demonstration sources and dependencies. The ordinary build runs on CoreCLR as a control.

The assembly and product identity differ from the original tester. Its source namespace is retained so the existing designer and resource identities continue to match. The window title identifies the active runtime mode.

From the repository root:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.WinFormsAotTester -c Release -p:BuildRevision=0
dotnet publish utils/Sphere10.Framework.Utils.WinFormsAotTester -c Release -p:BuildRevision=0 -p:PublishProfile=ExperimentalNativeAot -p:UseArtifactsOutput=true -p:ArtifactsPath="$PWD/artifacts/winforms-aot-build" -o artifacts/winforms-aot
```

Native publishing requires Windows x64, the .NET 10 SDK and the Visual C++ native build tools. The opt-in profile bypasses the SDK's WinForms trimming guard, disables unsupported built-in COM interop and retains trimming/AOT warnings. Its descriptor preserves reviewed resource readers, Bitmap/Icon resources, state observers, theme models and startup settings. Entrypoint annotations preserve the tester's registered screens, lifecycle constructors and resource-applied SplitContainer properties. The profile retains binding, UI-editor and default-value support and uses the ordinary ComponentModel paths. Its descriptor also preserves the reflected PropertiesTab constructor.

Use a fresh artifacts directory after changing the root descriptor. The tested Native AOT 10.0.14 incremental target can miss a descriptor-only edit; a new publish directory alone may still copy the previous native binary. A fresh `NativeIntermediateOutputPath` with a trailing separator is another option when retaining managed intermediates. Check that native compilation runs and verify the resulting executable.

For a bounded startup/shutdown check, run the actual executable with a new evidence directory:

```powershell
& ./artifacts/winforms-aot/Sphere10.Framework.Utils.WinFormsAotTester.exe --aot-smoke "$PWD/artifacts/winforms-aot-smoke"
```

This mode uses isolated settings, records main-window readiness and runtime flags in `smoke.log`, exits through application finalization and records `RunnerReturned`. A failure exits with code 1. Ordinary launches retain interactive behavior. Native runtime flags should both be false; a CoreCLR run reports true. The smoke check also opens SourceGrid through its registered host, exercises all seven examples, saves CSV/HTML/PNG and live-control renders, and verifies SingleView/MultiView tab transitions. It also checks actual navigation width, enum-control labels/selections, populated Appointment Book and Visual Grid screens, and real Screen Options nested properties and Apply/Reset. Other demos and persistence paths need their own checks. Success requires every selected case to finish, along with framework finalization. Use --aot-case with navigation, enums, appointment, visualgrid, options, sourcegrid or charting to isolate one case. The charting case walks all seven gallery pages, eight families and 39 explorer examples, including all 35 native chart types; it checks direct PNG exports and live control painting. Smoke diagnostics are written to diagnostics.log; ordinary launches can opt in with --aot-log <file>.

On 9 November 2026, Windows x64 with SDK 10.0.403/runtime 10.0.14 passed native main-window startup, framework shutdown and a saved window-placement/usage-settings round trip. Both dynamic-code flags were false and no CoreCLR/JIT modules were observed. All seven SourceGrid examples passed rendering and export checks; all CSV/HTML outputs matched CoreCLR exactly. The tab strip restored correctly in MultiView. Navigation layout, Appointment Book, Visual Grid and Screen Options passed the additional checks. Other demo screens and full layout parity remain unverified.

The subsequent chart-gallery check also passed all 39 thumbnail and explorer examples across all 35 native chart types, seven pages and eight families, with direct PNG exports, live control images and clean runtime diagnostics. The shared engine registry preserves its renderer constructors, and the experimental descriptor preserves chart-owned logging preference models. Forty focused registry/rendering NUnit cases passed. Chart XML serialization, formula modules, nondefault border skins and complete logging-output sessions remain separate checks.

The ObjectSpace check covers create, add, rename/save, delete, metadata reload and close in both byte orders. It also cold-opens snapshots produced by separate CoreCLR and Native AOT processes before creating a schema. The optional --objectspace-fixtures directory supplies paired .s10space/TSV fixtures produced by an earlier objectspace smoke run. The experimental profile retains the finite Account/Identity schema, closed enum serializer constructors and exact generic method bodies through its internal runtime-directive file. It preserves the existing factory registrations and metadata format. Closing a store disposes it, which flushes a memory space; cleanup after a failed operation ignores a disposal error so the original failure is reported. Forty-two focused builder/configuration/disposal NUnit cases pass.

The `crudeditor` case exercises the real Visual Grid create/edit commands and default modal editor. It verifies canceled creation, rejected DOB validation, accepted creation and editing, canceled editing with Undo, and cleanup of the generated record. The shared grid retains the configured editor's public parameterless constructor; the clone separately retains the Employee/Address property metadata. Standalone native and CoreCLR runs pass, as do the combined cases and 41 existing editor/reference-picker NUnit cases. Use `--aot-case objectspace` or `--aot-case crudeditor` to isolate these additional cases.

The current native EXE is 78,771,712 bytes (75.1 MiB); its 54-file diagnostic publish is 367,589,777 bytes including 280,292,440 symbol bytes. This artifact passes all selected cases together with clean runtime diagnostics and no CoreCLR/JIT modules.


The default experimental profile uses OptimizationPreference=Size with Windows data dehydration. A controlled comparison before chart-gallery coverage reduced the EXE from 89,654,784 to 77,918,720 bytes (13.09%), with the same then-selected checks passing. Its private-memory checkpoint was higher. ExperimentalNativeAotLowMemory disables dehydration: its measured EXE was 88,145,408 bytes with a lower private-memory sample. These single samples are not throughput or cold-start benchmarks. Symbols remain separate from the EXE.

WinForms Native AOT remains unsupported. Native compiler warnings include ActiveX/shell interop and optional database-provider paths that may throw. A linked executable and a visible main form do not establish compatibility for every demo, binding path, dialog or database operation. Keep this profile separate from normal application publishing.

An earlier interactive native build failed in transactional collections (`DecoratorExtensions.AsUnwrapped<,>` reflection). That demonstration has not been reverified with the current profile. Arbitrary runtime-selected ObjectSpace schemas and native file-dialog interactions also need separate checks.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Utils.WinFormsAotTester` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [Lifecycle](Lifecycle/README.md) | Plugin initializers and finalizers that log startup and shutdown progress, and the lifetime that completes demonstration output before shutdown. | [source](Lifecycle/) |
| [Resources](Resources/README.md) | Images and sounds embedded through `Properties/Resources.resx` for the tester's block buttons, menus, toolbars and sound demonstrations. | [source](Resources/) |
| [Screens](Screens/README.md) | The tester's demonstration screens, with the sample entities, data sources, helper controls and streams they use. | [source](Screens/) |
| [Wizard](Wizard/README.md) | The pages and model of the demonstration wizard opened from **Workspace > Wizard > Demo Wizard**. | [source](Wizard/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`ModuleConfiguration`](ModuleConfiguration.cs) | class | Module configuration that registers the usage-count initializer and installs the application's Views menu through `WinFormsDemoPlugin.ConfigureApplicationCommands` during initialization. |
| [`WinFormsComponentDemoPlugin`](WinFormsComponentDemoPlugin.cs) | static class | Configures the Component demonstrations plugin with its Controls, Styling, Data And Collections, Integration and Utilities blocks, lifecycle tasks and output lifetime. |
| [`WinFormsDemoPlugin`](WinFormsDemoPlugin.cs) | static class | Configures the Workspace demonstrations plugin with its Workspace and optional Notes blocks, the demo wizard and the screen-mode commands. |
<!-- domains:end -->

