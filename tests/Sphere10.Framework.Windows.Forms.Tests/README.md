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
| `WinFormsApplicationPluginTests` | Shared/native plugin hierarchy, service registration, legacy block compatibility and runtime ownership. |
| `WizardDemoTests` | Actual demo wizard input validation, model updates, back navigation and completion. |

Run the consumer configuration checks by themselves:

```powershell
dotnet test tests/Sphere10.Framework.Windows.Forms.Tests/Sphere10.Framework.Windows.Forms.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~DemoPluginRegistrationTests"
```

Native UI fixtures run on STA threads and avoid parallel execution when they share framework startup or native window state. They do not require external services or demo database credentials.
