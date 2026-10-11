# Sphere10.Framework.Windows.Forms.Tests

.NET 10 NUnit tests for the native application framework, screen hosting, controls and wizards. Run on Windows with the .NET 10 SDK:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0
```

The project references both the framework and the [WinForms tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md). Demo tests use the tester assembly and its resources directly; they do not compile private copies of its wizard or application configuration.

| Fixture | Coverage |
| --- | --- |
| `DemoPluginRegistrationTests` | Both running demo plugins and each block's source/parent, six independent navigation blocks, preservation of every original screen, four keyed initializers and four keyed finalizers with once-only application logging, unchanged process usage registration, ordinary multi-instance screens, singleton Settings, normal and fresh-empty startup profiles, permanent Notes removal guards and both empty-workspace lifetimes. Executes actual configured menu objects through the native application host and DI registration. |
| `ScreenPolicyTests` | Shared default ordering, permanent singleton lifecycle, tabless empty screens, guarded registration/removal and single/multi-view transitions. |
| `ControlRenderingScopeTests` | Actual native redraw suppression, one final paint, nested and added controls, deferred child layout, handle creation, newly visible controls, hidden/disposed controls, exception recovery and UI-thread asynchronous disposal. |
| `ApplicationHostRenderingTests` | One completed selection and chrome update per native transition, protected application commands with same-caption contextual replacements, common mode setter and specialized host guard behavior. |
| `SharedScreenHostIntegrationTests` | Shared/native DI identity, actual control reference identity, host block registration and browsing, canonical block-owned definition activation with or without menus, owner validation, default activation and object-based command dispatch, guarded multi-instance editors, cached unsaved edits, native tab order and docking, virtual decorator dispatch, distinct blocks with matching names, cancellation and disposal. |
| `WinFormsApplicationPluginTests` | Loaded startup receipts, multiple plugin contributions and individual block navigation, service-only plugins, unloaded DI definitions and failed loads, captured contribution ownership and parent/source links, direct blocks without synthetic plugins and live registration/removal. |
| `WinFormsApplicationLifecycleTests` | Keyed application tasks and existing process-task isolation, once-only execution after block installation, shutdown tasks under the splash before window closure, veto retries and failure cleanup, and splash application events and worker-thread progress/logging. |
| `WinFormsApplicationExitTests` | Application-owned native confirmation, repeated close requests, retained screen and distinct block permissions, retry after native or application vetoes, awaited finalization with a live disabled window and one native permission check, synchronous and asynchronous exit, reentrant close protection, graceful forced close, modal cancellation/escalation, worker/UI-thread dispatch and completion after the real main-window loop, hide-on-close compatibility and session-ending messages. |
| `FormWindowSettingsTests` and `MainFormExitTests` | Accepted placement capture and one shutdown save before window closure, minimized/maximized state, native vetoes, detached tracking, standalone persistence and settings-provider lifetime. |
| `WizardDemoTests` | Actual demo wizard input validation, model updates, back navigation and completion. |

Run the consumer configuration checks by themselves:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~DemoPluginRegistrationTests"
```

Native UI fixtures run on STA threads and avoid parallel execution when they share framework startup or native window state. They do not require external services or demo database credentials.

The running model is `IApplication` → blocks → menus and screens. `LoadedPlugins` records successful startup contributors; navigation uses `ScreenHost.ActiveBlock` and actual command, definition and screen references. Application lifecycle tasks are keyed by plugin name and run through `InitializeAsync` / `FinalizeAsync`; native close requests go through `ExitAsync` so shutdown tasks finish under the splash before window closure. Shared tests cover the plugin builder's name-before-registration guards and the directional sync/async base contracts in [Sphere10.Framework.Application.Tests](../Sphere10.Framework.Application.Tests/README.md).
