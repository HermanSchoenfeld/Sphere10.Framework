# Wizard

Windows Forms presentation of the shared application wizard, with screen base classes, a modal wizard dialog and a fluent builder.

The navigation, step traits and terminal state belong to the shared wizard in [Sphere10.Framework.Application](../../Sphere10.Framework.Application/UI/README.md). `WinFormsWizardBase<T>` adds native presentation. `Start(parent)` initializes every screen, sizes a `WinFormsWizardDialog<T>` to the largest screen, shows it modally and returns the `WizardResult`. Each step is a `WinFormsWizardScreen<T>`, a `UserControlEx` with `Initialize`, `OnPresent`, `OnNext`, `OnPrevious` and `Validate` hooks and access to the shared `Model`.

`WinFormsWizardBuilder<T>` builds a `WinFormsActionWizard<T>` from a title, model, screens and finish and cancel callbacks through the shared builder methods (`WithTitle`, `WithModel`, `AddScreen`, `OnFinished`, `OnCancelled`, `Build`). A finish callback is required. `WinFormsActionWizard` is the variant whose model is a property bag dictionary, and `DefaultWinFormsWizardScreen` is a screen for that model. Screens raise [state events](../StateEvent/README.md) like any other `UserControlEx`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`IWinFormsWizard<TModel>`](IWinFormsWizard.cs) | interface | Shared wizard contract specialized to `WinFormsWizardScreen<TModel>` steps. |
| [`IWinFormsWizardScreen<T>`](IWinFormsWizardScreen.cs) | interface | Wizard step contract with its wizard and initialize, present, next, previous and validate hooks. |
| [`WinFormsActionWizard`](WinFormsActionWizardT.cs) | class | `WinFormsActionWizard<T>` over an `IDictionary<string, object>` property bag. |
| [`WinFormsActionWizard<T>`](WinFormsActionWizard.cs) | class | Wizard that takes its screens and finish and cancel callbacks as constructor arguments. |
| [`WinFormsWizardBase<T>`](WinFormsWizardBase.cs) | abstract class | Native wizard presentation that shows screens in a modal dialog while the shared wizard owns navigation and state. |
| [`WinFormsWizardBuilder<T>`](WinFormsWizardBuilder.cs) | class | Fluent builder that adds screens and callbacks and builds a `WinFormsActionWizard<T>`. |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`DefaultWinFormsWizardScreen`](Forms/DefaultWinFormsWizardScreen.cs) | class | Empty wizard screen for property bag wizards. |
| [`WinFormsWizardDialog<T>`](Forms/WinFormsWizardDialog.cs) | class | Modal `FormEx` that hosts the current screen with previous and next buttons and a busy indicator, and routes closing to the wizard's cancel handling. |
| [`WinFormsWizardScreen<T>`](Forms/WinFormsWizardScreen.cs) | class | Base `UserControlEx` for a wizard step with lifecycle and validation hooks and access to the model. |
