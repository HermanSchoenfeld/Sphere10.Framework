# Extensions

A conversion from a FluentValidation `ValidationResult` to the Framework `Result` type used by the widget-gallery wizard steps.

`ToResult` returns `Result.Success` for a valid result. For an invalid result it returns a new `Result` with one error per validation failure, taken from each failure's `ErrorMessage`. `NewWidgetWizardStepViewModel` in the [Widgets](../Widgets/README.md) domain calls it after validating its `NewWidgetModel`, so that the wizard can show the messages.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`FluentValidationResultExtensions`](FluentValidationResultExtensions.cs) | static class | Adds `ToResult()` to `FluentValidation.Results.ValidationResult`, copying each failure's message into a Framework `Result`. |
