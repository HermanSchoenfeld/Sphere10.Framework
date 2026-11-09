# Original widget gallery

Restored wizard, modal and table examples run in [Sphere10.Framework.Utils.BlazorTester](../README.md) at `/widget-gallery`, `/widget-gallery/wizards`, `/widget-gallery/modals` and `/widget-gallery/tables`.

[WidgetGalleryPlugin.Configure](WidgetGalleryPlugin.cs) contributes two independent runtime blocks, **Component gallery** and **Legacy examples**, to the current application. It declares its unique plugin name before registering one initializer and one finalizer through the builder helpers. Those tasks run through the circuit's `IApplication` lifecycle; the [tester guide](../README.md#plugin-initialization-and-shutdown) explains startup, exit and reconnect behavior.

Widget model validation uses FluentValidation; table data and endpoint selections are local samples. Hosted screens use canonical command objects and their `Parent`, `Host` and `Block` relationships; browser IDs are resolved by the workspace route. The old standalone project file and asset copies are inactive migration references pending cleanup approval. Use the root tester project and [current library guide](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) for integration.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.DApp.Presentation.WidgetGallery` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [Extensions](Extensions/README.md) | A conversion from a FluentValidation `ValidationResult` to the Framework `Result` type used by the widget-gallery wizard steps. | [source](Extensions/) |
| [Widgets](Widgets/README.md) | Sample models, services, validators, view models and Razor components behind the legacy dialog, table and wizard pages of the widget gallery. | [source](Widgets/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`WidgetGallery`](WidgetGallery.razor) | class | Razor landing page of the legacy plugin gallery that shows a random number and links to the dialog, table, wizard and endpoint examples. |
| [`WidgetGalleryPlugin`](WidgetGalleryPlugin.cs) | static class | `Configure(BlazorPluginBuilder)` names the "Widget Gallery" plugin, registers its services, initializer and finalizer, and adds the Component gallery and Legacy examples blocks with their screens and menus. |
<!-- domains:end -->

