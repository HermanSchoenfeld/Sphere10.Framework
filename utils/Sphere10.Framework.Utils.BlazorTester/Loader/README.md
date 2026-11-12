# Original loader shell

Retained loader content is hosted by the current [Blazor tester](../README.md), which uses `UI.Application.ApplicationShell` from `Sphere10.Framework.Web.AspNetCore.Blazor`. The endpoint selector, dashboard and endpoint screen remain runnable in that shared workspace; the older standalone shell is a migration reference.

The active entry point is [Program.cs](../Program.cs). [Sphere10Plugin.Configure](Sphere10Plugin.cs) contributes the Workspace block, circuit-scoped endpoint/state services and named plugin startup/shutdown tasks. Widget Gallery contributes the Legacy examples block containing the retained loader screens. Blocks are the runtime navigation children; plugins appear in `IApplication.LoadedPlugins` as startup contributors.

Endpoint selection and mock node data are scoped to the interactive server circuit. Superseded WebAssembly project/entry files and static-asset copies remain inactive migration references pending cleanup approval. Use the root tester project and [current hosting guide](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) when integrating these components.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.DApp.Presentation.Loader` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [Components](Components/README.md) | Holds the original loader shell's Razor navigation and modal components, including the endpoint selector that the current tester still shows. | [source](Components/) |
| [Layouts](Layouts/README.md) | Holds the original loader shell's main layout, which composes its sidebar, top bar, modal host and footer. | [source](Layouts/) |
| [Pages](Pages/README.md) | Holds the original loader's dashboard, endpoint management and not-found pages, which the current tester still hosts. | [source](Pages/) |
| [Plugins](Plugins/README.md) | Empty folder retained from the original loader layout, with no source files or types. | [source](Plugins/) |
| [Services](Services/README.md) | Provides the in-memory endpoint manager and mock node data service that the loader pages and the tester's endpoint selector use. | [source](Services/) |
| [ViewModels](ViewModels/README.md) | Provides the view models behind the loader's menus, endpoint selector and endpoint page. | [source](ViewModels/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`App`](App.razor) | class | Razor router of the original WebAssembly loader over the plugin routing assemblies with `MainLayout` and a `NotFound` fallback, excluded from the current tester |
| [`DataSourceOptions`](ServerOptions.cs) | class | Options bound from the `DataSource` configuration section, holding the configured server URIs |
| [`Program`](Program.cs) | class | Original WebAssembly entry point that registers view models, modal, wizard, endpoint, node and plugin services, excluded from the current tester |
| [`Sphere10Plugin`](Sphere10Plugin.cs) | static class | Configures the tester's Workspace plugin: scoped status, endpoint and node services, the workspace block with its screens, toolbar and menus, and lifecycle tasks |
<!-- domains:end -->

