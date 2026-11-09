# Widgets

Sample models, services, validators, view models and Razor components behind the legacy dialog, table and wizard pages of the widget gallery.

These types are demonstrations for the Blazor tester, not library APIs. [WidgetGalleryPlugin](../WidgetGalleryPlugin.cs) registers the services, the validator and every view model as transient services. The wizard example starts in `WizardsViewModel`, which builds an `IBlazorWizard` over a new `NewWidgetModel` with the details and summary steps. `NewWidgetWizardStepViewModel` validates the model through `IValidator<NewWidgetModel>` and then replaces the remaining steps, adding `WidgetDimensionsStep` only when `AreDimensionsKnown` is set. It converts the validation outcome with `ToResult()` from the [Extensions](../Extensions/README.md) domain.

The pages show the library's modal and table components with local sample data. `Tables` creates 100 sample blocks in memory, and no page contacts a server. The wizard, modal and table components come from the [Blazor library](../../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md).

## Contents

The domain folder itself declares no types. Its types are grouped by subfolder below.

### Components

| Type | Kind | Purpose |
|---|---|---|
| [`NewWidgetWizardStep`](Components/NewWidgetWizardStep.razor) | class | Razor wizard step titled "Details" that edits the name, description, price and the "Supply dimensions" choice, and declares `WizardStepTraits.AssumeHasNext`. |
| [`WidgetDimensionsStep`](Components/WidgetDimensionsStep.razor) | class | Razor wizard step titled "Properties" that edits the widget's height and length. |

### Models

| Type | Kind | Purpose |
|---|---|---|
| [`NewWidgetModel`](Models/NewWidgetModel.cs) | class | Wizard model with a required name, description and price, an `AreDimensionsKnown` flag and optional height and length in the range 1 to 100. |

### Pages

| Type | Kind | Purpose |
|---|---|---|
| [`Tables`](Pages/Tables.razor) | class | Razor page that shows sample blocks in a `RapidTable`, a `PagedTable` and a `VirtualPagedTable`, and opens an `InfoDialog` for a selected row. |
| [`Wizards`](Pages/Wizards.razor) | class | Razor page that opens the new-widget wizard through `IModalService.ShowWizardAsync`, with an option to allow cancellation, and lists the widgets it created. |

### Services

| Type | Kind | Purpose |
|---|---|---|
| [`IRandomNumberService`](Services/RandomNumberService.cs) | interface | Supplies a random number to the gallery page through `GetRandomNumber()`. |
| [`RandomNumberService`](Services/RandomNumberService.cs) | class | Returns a non-negative `int` built from four bytes of `Tools.Crypto.GenerateCryptographicallyRandomBytes`. |

### Validators

| Type | Kind | Purpose |
|---|---|---|
| [`NewWidgetModelValidator`](Validators/NewWidgetModelValidator.cs) | class | FluentValidation `AbstractValidator<NewWidgetModel>` that requires a price greater than zero and a non-empty name and description. |

### ViewModels

| Type | Kind | Purpose |
|---|---|---|
| [`NewWidgetSummaryViewModel`](ViewModels/NewWidgetSummaryViewModel.cs) | class | View model of the summary step, whose next and previous actions always succeed. |
| [`NewWidgetWizardStepViewModel`](ViewModels/NewWidgetWizardStepViewModel.cs) | class | View model of the details step that validates the model and chooses the remaining wizard steps. |
| [`TablesViewModel`](ViewModels/TablesViewModel.cs) | class | `ExtendedComponentViewModel` that exposes the `INodeService` used by the `Tables` page. |
| [`WidgetDimensionsStepViewModel`](ViewModels/WidgetDimensionsStepViewModel.cs) | class | View model of the dimensions step, whose next and previous actions always succeed. |
| [`WidgetGalleryViewModel`](ViewModels/WidgetGalleryViewModel.cs) | class | View model of the gallery landing page that exposes `IRandomNumberService`. |
| [`WidgetModalViewModel`](ViewModels/WidgetModalViewModel.cs) | class | Empty `ModalViewModel` subclass registered for the gallery's modal examples. |
| [`WizardsViewModel`](ViewModels/WizardsViewModel.cs) | class | Builds the "New Widget" wizard through `IBlazorWizardBuilder<NewWidgetModel>` and keeps the list of finished widgets. |
