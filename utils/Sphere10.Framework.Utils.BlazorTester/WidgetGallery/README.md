# Original widget gallery

Restored wizard, modal and table examples run in [Sphere10.Framework.Utils.BlazorTester](../README.md) at `/widget-gallery`, `/widget-gallery/wizards`, `/widget-gallery/modals` and `/widget-gallery/tables`.

[WidgetGalleryPlugin.Configure](WidgetGalleryPlugin.cs) contributes two independent runtime blocks, **Component gallery** and **Legacy examples**, to the current application. It declares its unique plugin name before registering one initializer and one finalizer through the builder helpers. Those tasks run through the circuit's `IApplication` lifecycle; the [tester guide](../README.md#plugin-initialization-and-shutdown) explains startup, exit and reconnect behavior.

Widget model validation uses FluentValidation; table data and endpoint selections are local samples. Hosted screens use canonical command objects and their `Parent`, `Host` and `Block` relationships; browser IDs are resolved by the workspace route. The old standalone project file and asset copies are inactive migration references pending cleanup approval. Use the root tester project and [current library guide](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) for integration.
