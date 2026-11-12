# Wizard

Windows Forms presentation of the shared application wizard, with screen base classes, a modal wizard dialog and a fluent builder.

The navigation, step traits and terminal state belong to the shared wizard in [Sphere10.Framework.Application](../../Sphere10.Framework.Application/UI/README.md). `WinFormsWizardBase<T>` adds native presentation. `Start(parent)` initializes every screen, sizes a `WinFormsWizardDialog<T>` to its steps, shows it modally and returns the `WizardResult`. `StepSizing` chooses how: `WizardStepSizing.LargestStep` (the default) gives every step the largest step's size, and `EachStep` resizes the dialog about its centre to each step's own size. Step sizes are the screens' designer sizes in logical units, shown at the dialog's current DPI. With `EachStep`, a screen that overrides `MeasureHeight(width)` gets the height its content needs at that width instead of its designer height; the measure runs after `OnPresent`, so it sees the step's current text. The dialog lays out its content and its Previous and Next row in code: a 12-unit margin on every side, so the content's left edge lines up with Previous, and a 6-unit gap above the buttons. A move to a monitor of another DPI keeps that layout and sizes the dialog for its steps again. Each step is a `WinFormsWizardScreen<T>`, a `UserControlEx` with `Initialize`, `OnPresent`, `OnNext`, `OnPrevious` and `Validate` hooks and access to the shared `Model`.

`WinFormsWizardBuilder<T>` builds a `WinFormsActionWizard<T>` from a title, model, screens and finish and cancel callbacks through the shared builder methods (`WithTitle`, `WithModel`, `AddScreen`, `OnFinished`, `OnCancelled`, `Build`), and the native `WithStepSizing`. A finish callback is required. `WinFormsActionWizard` is the variant whose model is a property bag dictionary, and `DefaultWinFormsWizardScreen` is a screen for that model. Screens raise [state events](../StateEvent/README.md) like any other `UserControlEx`.

## Example

A wizard whose steps differ in size, each shown at its own:

```csharp
using var wizard = new WinFormsWizardBuilder<SetupModel>()
	.WithTitle("Setup")
	.WithModel(model)
	.WithStepSizing(WizardStepSizing.EachStep)
	.AddScreen(new ChooseFolderScreen())
	.AddScreen(new ReviewScreen())
	.OnFinished(setup => Task.FromResult(Result.Success))
	.Build();
var result = await wizard.Start(owner);
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`IWinFormsWizard<TModel>`](IWinFormsWizard.cs) | interface | Shared wizard contract specialized to `WinFormsWizardScreen<TModel>` steps. |
| [`IWinFormsWizardScreen<T>`](IWinFormsWizardScreen.cs) | interface | Wizard step contract with its wizard and initialize, present, next, previous and validate hooks. |
| [`WinFormsActionWizard`](WinFormsActionWizardT.cs) | class | `WinFormsActionWizard<T>` over an `IDictionary<string, object>` property bag. |
| [`WinFormsActionWizard<T>`](WinFormsActionWizard.cs) | class | Wizard that takes its screens and finish and cancel callbacks as constructor arguments. |
| [`WinFormsWizardBase<T>`](WinFormsWizardBase.cs) | abstract class | Native wizard presentation that shows screens in a modal dialog while the shared wizard owns navigation and state. |
| [`WinFormsWizardBuilder<T>`](WinFormsWizardBuilder.cs) | class | Fluent builder that adds screens, callbacks and the step sizing and builds a `WinFormsActionWizard<T>`. |
| [`WizardStepSizing`](WizardStepSizing.cs) | enum | Whether the dialog takes the largest step's size for every step (`LargestStep`, the default) or each step's own (`EachStep`). |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`DefaultWinFormsWizardScreen`](Forms/DefaultWinFormsWizardScreen.cs) | class | Empty wizard screen for property bag wizards. |
| [`WinFormsWizardDialog<T>`](Forms/WinFormsWizardDialog.cs) | class | Modal `FormEx` that hosts the current screen with previous and next buttons and a busy indicator, and routes closing to the wizard's cancel handling. |
| [`WinFormsWizardScreen<T>`](Forms/WinFormsWizardScreen.cs) | class | Base `UserControlEx` for a wizard step with lifecycle and validation hooks, an optional measured height (`MeasureHeight`) and access to the model. |
