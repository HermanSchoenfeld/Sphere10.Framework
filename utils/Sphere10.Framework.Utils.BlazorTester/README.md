# Sphere10.Framework.Utils.BlazorTester

A .NET 10 Blazor Web App using Interactive Server rendering. It restores both archived component galleries and loader shells in one runnable utility. No WebAssembly workload, backend node or WebSocket server is required.

In Visual Studio, set `Sphere10.Framework.Utils.BlazorTester` as the startup project and use its named launch profile. F5 or Ctrl+F5 opens the ApplicationBlock workspace in your browser.

From a terminal at the repository root:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.BlazorTester -p:BuildRevision=0
```

The terminal command starts the web server and stays running. Open [http://localhost:5187/application](http://localhost:5187/application) in your browser; `dotnet run` does not open a browser automatically. The terminal should print `Now listening on: http://localhost:5187`. Stop the server with Ctrl+C when finished.

| Route | Demonstration |
| --- | --- |
| `/` | Original loader dashboard and plugin navigation |
| `/widget-gallery` | Original widget gallery |
| `/widget-gallery/modals` | Information and confirmation dialogs |
| `/widget-gallery/tables` | Paged, virtual and streaming tables |
| `/widget-gallery/wizards` | Widget wizard, validation, optional dimensions and summary |
| `/servers` | Per-session sample endpoint selection |
| `/application` | ApplicationBlock workspace, retained screens, guarded editor, multi-instance scratchpads and scoped actions |
| `/modern` | Second-generation shell, editable grid with custom cells/actions, tables, dialogs and branching wallet wizard |

`Loader` contains the original shell and sample services. `WidgetGallery` retains the original widget models, validators and view models. `Modern` contains the second-generation sample page, custom cell components and wallet wizard examples. Reusable services, plugin managers, table components and wizard frameworks now live in `src/Sphere10.Framework.Web.AspNetCore.Blazor`.

The endpoint selector stores values for the current circuit only and never contacts those endpoints. The wallet wizard demonstrates navigation and validation without creating or persisting a wallet. The grid uses a growable local `ListDataSource` with 73 initial sample records. Its create, edit, paging, custom-column and delete actions require no external service.

The host supports prerendered Interactive Server rendering, .NET 10 not-found pages, fingerprinted static assets and import maps. Modal hosts import their own isolated JavaScript module. Static assets are local under `wwwroot`; the host also loads the library's grid script. The archived second-generation themes, fonts, artwork and vendor bundle are retained. The active host does not load the old vendor bundle, service workers, or archived WebAssembly entry points.

## ApplicationBlock workspace

`Application/WorkspaceRegistration.cs` builds two blocks with the same fluent configuration idioms as WinForms. Open `/application` to try:

- Increment the overview counter, switch screens and return: the component instance is retained.
- Edit a session note: switching, closing and internal navigation are blocked until Save or Discard.
- Open multiple scratchpads: each has its own component, text and browser-history entry.
- Run the async action: its service is scoped to the current circuit.
- Switch to the component gallery and exercise the existing grid, dialogs and wizard in a hosted application screen.

The URL carries `block`, `screen` and `instance` query values. Back/Forward selects retained screens; refreshing starts a fresh circuit and recreates the requested screen. Sample data is not persisted.

See [the library guide](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) for hosting, customization and screen lifecycle contracts.

## Verification

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests -p:BuildRevision=0
```

Tests preserve the archived table, wizard, plugin and menu behavior, and render all gallery routes with their layouts. Additional coverage checks streaming cancellation/dispatcher behavior, scope isolation, grid paging/creation/deletion, zero-based virtual paging, ApplicationBlock registration, retained component disposal, overlapping route cancellation and dirty-screen guards.

## Retained migration scaffolding

The original `Loader`, `ModernLoader` and `WidgetGallery` project files, old WebAssembly entry points, and original static-asset copies remain temporarily as inactive migration references pending approval to remove superseded files. They are excluded from the active tester and are not solution projects. Use only the project and command above.
