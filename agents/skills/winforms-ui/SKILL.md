---
name: winforms-ui
description: Configure or extend the current WinForms application, plugins, blocks, hosted screens, async dialogs and wizards. Use for WinFormsApplicationScreen, WinFormsApplicationPluginBuilder, WinFormsApplicationBlockBuilder, WinFormsWizardBuilder and native UI lifecycle integration.
---

# WinForms UI Skill

Read [application ownership guidance](../../../docs/guidelines/patterns/application-ownership.md) when changing blocks, commands, plugins or application lifecycle.

Use the [WinForms README](../../../src/Sphere10.Framework.Windows.Forms/README.md) for the complete registration, hosting and dialog APIs, and the [shared application README](../../../src/Sphere10.Framework.Application/README.md#shared-ui-framework) for portable contracts. Shared UI types live in `Sphere10.Framework.Application.UI`; native types use `WinForms` prefixes in `Sphere10.Framework.Windows.Forms`.

## Application blocks & screens

Navigation via `WinFormsApplicationBlock` + `WinFormsApplicationBlockBuilder`:

```csharp
using Sphere10.Framework.Windows.Forms;

var block = new WinFormsApplicationBlockBuilder()
	.WithName("Admin")
	.WithDefaultScreen<DashboardScreen>()
	.ConfigureMenu(menu => menu
		.WithText("Users")
		.AddScreenItem<UsersScreen>("Users")
	)
	.Build();
```

- Keep each builder invocation continuous, with no blank separator lines. Put each configuration call on its own line, indent nested child calls one tab deeper and align multiline closing parentheses with the owning call.
- Use `ConfigureMenu` and `ConfigureItem` for nested builder callbacks. `AddMenu(menu)` and `AddItem(item)` register existing definitions; `AddScreenItem` and `AddActionItem` construct definitions directly without a child builder.
- Derive native screens from `WinFormsApplicationScreen`; registration checks that type. The definition owns its title, activation mode and `ScreenKind`. Use `ConfigureItem(item => item.AsScreenItem().WithText(...).WithScreen<TScreen>().AsSingleInstance())` when configuring those policies, or `WithScreen(existingDefinition)` to retain one definition across commands. The host resolves an unspecified mode from the screen type; do not add a separate activation-policy registry.
- The runtime hierarchy is application → blocks → menus and screen definitions. Register a plugin with `services.AddWinFormsApplicationPlugin(...)` or a standalone block with `services.AddApplicationBlock(block)`. A plugin can contribute several independently selectable blocks; standalone blocks need no synthetic plugin. `LoadedPlugins` records successful startup registration, while `IApplication.Blocks` projects live host membership.
- Follow item `Parent` → menu `Parent` → block `Parent` to the application, and block `Source` to its startup plugin. Execute the actual item with `ExecuteMenuItemAsync(item)` or open a registered definition with `OpenScreenAsync(block, definition)`. Do not scan blocks or introduce ID-based command overloads. `ScreenHost` owns active selection; screen `Host` and `Block` are assigned before activation and clear after actual removal, while a hidden singleton remains attached.
- `WinFormsApplicationScreen.OnHide(ref bool cancelHide)` is a synchronous guard; set its cancellation decision before returning. Preserve the host's guards and retention when closing or navigating. Keep control subscriptions, timers and owned resources tied to actual screen disposal, rather than disposing a retained singleton merely because it hides.
- Use the [crud-grid](../crud-grid/SKILL.md) skill for `CrudGrid` binding, editing, reference pickers, paging, and dropdown layout; use [data-source](../data-source/SKILL.md) when implementing its `IDataSource<T>`.

## Application startup and shutdown

- Use `BuildWinFormsApplication()`, `UseMainForm<BlockMainForm>()` and `StartWinFormsApplication()` for the existing runner. Register plugin services through `ConfigureServices`; see the [plugin example](../../../src/Sphere10.Framework.Windows.Forms/README.md#grouping-blocks-into-application-plugins). The runner starts the framework and then awaits application initialization on its UI thread.
- Declare the plugin's `WithName(...)` before `RegisterInitializer` or `RegisterFinalizer`, including factory overloads; unnamed registration throws immediately. Those helpers use the built plugin's unique `Name` as the service key, without a plugin ID or `typeof(IApplication)` key. Use `ConfigureServices` for ordinary dependencies. Keep existing unkeyed process tasks separate from application/plugin tasks.
- Use `SyncApplicationInitializerBase` / `AsyncApplicationInitializerBase` and the corresponding finalizer bases, in `Sphere10.Framework.Application`; override only the chosen primitive. Reuse the [directional sync/async hierarchy](../../../docs/guidelines/patterns/sync-async.md#synchronous-and-asynchronous-implementation-hierarchies), including lifecycle work hooks. Preserve the shared application lifecycle without introducing Stateless.
- Await `IApplication.InitializeAsync`, `FinalizeAsync` and `ExitAsync` on UI paths. The native synchronous adapters preserve UI affinity and pump messages at supported boundaries; do not invent UI-thread waits. `FinalizeAsync` performs cleanup; `ExitAsync` coordinates confirmation and screen/block permissions before closing. `LiteMainForm.OnFormClosing` is sealed; use `OnApplicationExiting` or `ApplicationExitingEvent` for a native veto.
- Enable progress display with `.UseSplashScreen()`. Startup and shutdown splashes show application/plugin tasks alongside the existing process tasks. Approved shutdown keeps the main form alive with interaction disabled until finalizers finish, then closes it last. See [lifecycle and splash](../../../src/Sphere10.Framework.Windows.Forms/README.md#application-lifecycle-and-splash) for force, cancellation and failure behavior; [WinFormsTester](../../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) demonstrates both plugin and process tasks.
- Use `IApplication.Logger`, including global native initializer diagnostics; see [logging](../logging/SKILL.md) for startup routing and portable process tasks. Add only application-owned resources to `Disposables`. DI services, borrowed hosts/forms and shared plugin definitions retain their existing disposal owners.

## Native callbacks and async dialogs

Native `AddActionItem` and native action-item builders require synchronous `Action` / `Action<IUserInterfaceServices>` delegates. Passing an async lambda produces `async void`; command dispatch cannot await its completion, and the native builder rejects the shared task-returning action overload. Keep async work in a `Task`-returning method invoked from an awaited path or a native async event handler that handles failures. Do not present an async menu lambda as a task-returning command.

Use the existing `ShowAsync` dialog helpers or `await dialog.ShowDialogAsync(owner)` on .NET 10. Keep an owned custom form in a `using` scope across the await. `InvokeAsyncEx` dispatches to the UI; it is not a modal-loop replacement. Set closing/validation cancellation and handled flags before the first await. See [async modal dialogs](../../../src/Sphere10.Framework.Windows.Forms/README.md#async-modal-dialogs-net-10) for the supported helpers and synchronous native boundaries.

Use `EnterRenderingScope()` on the UI thread for native tree updates. Preserve controls' own requested visibility when ancestors hide; effective `Control.Visible` is insufficient for restoring redraw. Check native visibility after retained-view transitions as well as managed bounds, and verify real search/dropdown controls. See [native rendering guidance](../../../src/Sphere10.Framework.Windows.Forms/README.md#native-helper-migration).

## Wizards
Use [WinFormsWizardBuilder<T>](../../../src/Sphere10.Framework.Windows.Forms/Wizard/WinFormsWizardBuilder.cs), which derives from the shared `WizardBuilderBase` and builds `WinFormsActionWizard<T>`. This fragment assumes an existing `model` and native owner form:
```csharp
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

var wizard =
	new WinFormsWizardBuilder<MyModel>()
		.WithTitle("Setup")
		.WithModel(model)
		.AddScreen(new StepOneScreen())
		.AddScreen(new StepTwoScreen())
		.OnFinished(wizardModel => Task.FromResult(Result.Success))
		.OnCancelled(wizardModel => Result.Success)
		.Build();

var result = await wizard.Start(owner);
```
- `Build()` requires a title, at least one screen, and a finish function (enforced via `Guard.Ensure`).
- Steps derive from `WinFormsWizardScreen<MyModel>`. Their `Wizard` and `Model` are assigned before `Initialize`, including injected steps. `Validate` runs before `OnNext`; commit accepted input there when validation must precede model mutation. Set `UpdateModelOnStateChanged = false` for that pattern; default `OnPresent` copies the model to the UI.
- For steps that create their successors during navigation, read the shared [dynamic step metadata](../../../src/Sphere10.Framework.Application/README.md#shared-wizard-workflow) and adopt its traits in the real consumer. Verify the first displayed button and missing-successor behavior through the native host.
- `OnFinished` accepts `Func<T, Task<Result>>` or `Func<T, Task<Result<bool>>>`; cancellation supports the task-returning outcome or a synchronous `Result` callback. Async callbacks are awaited through `FinishAsync` / `CancelAsync`; do not route an asynchronous cancel callback through `CancelRequested()`. `Start(owner)` awaits the native modal dialog and disposes that dialog when it completes.
- Follow the [builder-pattern](../builder-pattern/SKILL.md) skill when extending wizard configuration.

## Remembered user preferences
Use the [user-settings](../user-settings/SKILL.md) skill for preferences that survive application restarts, including the main form's size and monitor, page sizes, and filters.
