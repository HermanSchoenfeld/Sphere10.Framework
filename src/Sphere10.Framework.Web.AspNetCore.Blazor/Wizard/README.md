# Wizard

The current Blazor wizard model and fluent builder whose steps are Blazor component types rendered by `UI.Wizard.BlazorWizardHost`.

These types adapt the shared wizard engine and builder of Sphere10.Framework.Application to Blazor. A step is a concrete, non-generic component type derived from `UI.Wizard.BlazorWizardStepBase`, normally through `UI.Wizard.BlazorWizardStep<TModel>`. The builder is constructed with the shared base's `requiresStart` and `requiresModel` flags set, and the shared base supplies the title, model, cancellation, step composition and finish and cancel callbacks. `AddSphere10Blazor` registers `IBlazorWizardBuilder<TModel>` from this namespace as a transient service. Show a built wizard with `ViewService.WizardDialogAsync` from [Modal](../Modal/README.md), or render it with the host in the `Wizard` folder of [UI](../UI/README.md). The original component generation has its own wizard types with the same names in `Components.Wizard` and `Services`, described in [Components](../Components/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorWizard<TModel>`](BlazorWizard.cs) | class | Shared `Wizard<TModel, Type>` whose steps are component types, with `BlazorWizardOptions` for button styling. |
| [`BlazorWizardBuilder<TModel>`](BlazorWizardBuilder.cs) | class | Builds a `BlazorWizard<TModel>` from step component types with the shared builder state and validation. |
| [`BlazorWizardOptions`](BlazorWizardOptions.cs) | class | CSS classes of a wizard's Next, Back and Cancel buttons. |
| [`IBlazorWizard`](IBlazorWizard.cs) | interface | Shared `IWizard<Type>` contract of this generation with its `BlazorWizardOptions`. |
| [`IBlazorWizard<TModel>`](IBlazorWizard.cs) | interface | Typed wizard contract of this generation with a model of type `TModel`. |
| [`IBlazorWizardBuilder<TModel>`](IBlazorWizardBuilder.cs) | interface | Shared fluent wizard builder contract specialized for Blazor step components of this generation. |
