---
name: winforms-ui
description: WinForms screens, wizards, and application blocks. Trigger when creating WinFormsApplicationScreen, WinFormsWizardBuilder wizards, WinFormsApplicationBlockBuilder navigation, or CrudGrid screens.
---

# WinForms UI Skill

## Application blocks & screens
- Navigation via `WinFormsApplicationBlock` + `WinFormsApplicationBlockBuilder`:
  ```csharp
  var block =
	  new WinFormsApplicationBlockBuilder()
		  .WithName("Admin")
		  .WithDefaultScreen<DashboardScreen>()
		  .AddMenu(mb => mb.AddScreenItem<UsersScreen>())
		  .Build();
  ```
- Derive screens from `WinFormsApplicationScreen`.
- Use the [crud-grid](../crud-grid/SKILL.md) skill for `CrudGrid` binding, editing, reference pickers, paging, and dropdown layout; use [data-source](../data-source/SKILL.md) when implementing its `IDataSource<T>`.

## Wizards
Use `WinFormsWizardBuilder<T>` (`src/Sphere10.Framework.Windows.Forms/Wizard/WinFormsWizardBuilder.cs`):
```csharp
var wizard =
	new WinFormsWizardBuilder<MyModel>()
		.WithTitle("Setup")
		.WithModel(model)
		.AddScreen(new StepOneScreen())
		.AddScreen(new StepTwoScreen())
		.OnFinished(async m => Result.Success)
		.OnCancelled(m => Result.Success)
		.Build();
```
- `Build()` requires a title, at least one screen, and a finish function (enforced via `Guard.Ensure`).
- Follow the [builder-pattern](../builder-pattern/SKILL.md) skill when extending wizard configuration.

## Remembered user preferences
Use the [user-settings](../user-settings/SKILL.md) skill for preferences that survive application restarts, including the main form's size and monitor, page sizes, and filters.
