# Sphere10.Framework.Windows.Forms.Tests

.NET 10 NUnit tests for the native application framework, screen hosting, controls and wizards. Run on Windows with the .NET 10 SDK:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0
```

The project references both the framework and the [WinForms tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md). Demo tests use the tester assembly and its resources directly; they do not compile private copies of its wizard or application configuration.

| Fixture | Coverage |
| --- | --- |
| `DemoPluginRegistrationTests` | The running demo's plugin and feature blocks, preservation of every original screen, ordinary multi-instance screens, singleton Settings, normal and fresh-empty startup profiles, permanent Notes removal guards and both empty-workspace lifetimes. Executes actual configured menu commands through the native application host and DI registration. |
| `ScreenPolicyTests` | Shared default ordering, permanent singleton lifecycle, tabless empty screens, guarded registration/removal and single/multi-view transitions. |
| `ControlRenderingScopeTests` | Actual native redraw suppression, one final paint, nested and added controls, deferred child layout, handle creation, newly visible controls, hidden/disposed controls, exception recovery and UI-thread asynchronous disposal. |
| `ApplicationHostRenderingTests` | One completed selection and chrome update per native transition, protected application commands with same-caption contextual replacements, common mode setter and specialized host guard behavior. |
| `SharedScreenHostIntegrationTests` | Shared/native DI identity, actual control reference identity, host block registration and browsing, canonical block-owned definition activation with or without menus, owner validation, default activation and command dispatch, guarded multi-instance editors, cached unsaved edits, native tab order and docking, virtual decorator dispatch, duplicate block names, cancellation and disposal. |
| `WinFormsApplicationPluginTests` | Loaded startup receipts, multiple plugin contributions and individual block navigation, service-only plugins, unloaded DI definitions and failed loads, captured contribution ownership, direct block compatibility and live registration/removal. |
| `WizardDemoTests` | Actual demo wizard input validation, model updates, back navigation and completion. |

Run the consumer configuration checks by themselves:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~DemoPluginRegistrationTests"
```

Native UI fixtures run on STA threads and avoid parallel execution when they share framework startup or native window state. They do not require external services or demo database credentials.
