---
name: winforms-ui
description: WinForms screens, wizards, and application blocks. Trigger when creating WinFormsApplicationScreen, WinFormsWizardBuilder wizards, WinFormsApplicationBlockBuilder navigation, or CrudGrid screens.
---

# WinForms UI Skill

## Application blocks & screens

Navigation via `WinFormsApplicationBlock` + `WinFormsApplicationBlockBuilder`:

```csharp
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
- Derive screens from `WinFormsApplicationScreen`.
- Use the shared `IApplication` as the runtime root: its live `Blocks` and `LoadedPlugins` expose membership and successful startup contributors; `ScreenHost` owns active block/screen selection. Follow `Parent`, `Source`, `Host` and `Block` instead of searching for owners.
- Declare the plugin's `WithName(...)` before `RegisterInitializer` or `RegisterFinalizer`, including factory overloads. Names must be unique among loaded plugins. Use `SyncApplicationInitializerBase` / `AsyncApplicationInitializerBase` and the corresponding finalizer bases; override only the chosen primitive.
- Await `IApplication.InitializeAsync`, `FinalizeAsync` and `ExitAsync` on UI paths. The synchronous forms follow the directional pattern and native message pumping. Closing the main form defers to application exit; approved shutdown displays the finalization splash and closes the form last. Use `IApplication.Logger` and place application-owned resources in `Disposables`.
- Use the [crud-grid](../crud-grid/SKILL.md) skill for `CrudGrid` binding, editing, reference pickers, paging, and dropdown layout; use [data-source](../data-source/SKILL.md) when implementing its `IDataSource<T>`.

## Wizards
Use `WinFormsWizardBuilder<T>` (`src/Sphere10.Framework.Windows.Forms/Wizard/WinFormsWizardBuilder.cs`):
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
		.OnFinished(m => Task.FromResult(Result.Success))
		.OnCancelled(m => Result.Success)
		.Build();
```
- `Build()` requires a title, at least one screen, and a finish function (enforced via `Guard.Ensure`).
- Follow the [builder-pattern](../builder-pattern/SKILL.md) skill when extending wizard configuration.

## Remembered user preferences
Use the [user-settings](../user-settings/SKILL.md) skill for preferences that survive application restarts, including the main form's size and monitor, page sizes, and filters.
