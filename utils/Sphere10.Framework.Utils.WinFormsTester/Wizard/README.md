# Wizard

The pages and model of the demonstration wizard opened from **Workspace > Wizard > Demo Wizard**.

`WinFormsDemoPlugin` builds the wizard with `WinFormsWizardBuilder<DemoWizardModel>`, starting from a fresh `DemoWizardModel.Default` for each run. The pages run in the order `EnterNameScreen`, `EnterAgeScreen`, `CantGoBackScreen` and `ConfirmScreen`, and the finish handler shows the collected name and age. Every page derives from `DemoWizardScreenBase`, a non-generic `WinFormsWizardScreen<DemoWizardModel>` that the WinForms designer can open. The base sets `UpdateModelOnStateChanged` to `false`, so a page copies its input into the model only in `OnNext` after `Validate` succeeds, and restores it in `CopyModelToUI` when the page is shown again. Screens registered in blocks are in [Screens](../Screens/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CantGoBackScreen`](CantGoBackScreen.cs) | class | Wizard page that sets `HidePrevious` when presented, so the user cannot return to earlier pages. |
| [`ConfirmScreen`](ConfirmScreen.cs) | class | Final wizard page that displays the collected name and age. |
| [`DemoWizardModel`](DemoWizardModel.cs) | class | Wizard session model with the trimmed name and nullable age, created unanswered by `Default`. |
| [`DemoWizardScreenBase`](DemoWizardScreenBase.cs) | class | Non-generic designer base for the demonstration pages that disables automatic model updates. |
| [`EnterAgeScreen`](EnterAgeScreen.cs) | class | Wizard page that requires a non-negative whole-number age before advancing. |
| [`EnterNameScreen`](EnterNameScreen.cs) | class | Wizard page that requires a name and a checked confirmation box before advancing. |
