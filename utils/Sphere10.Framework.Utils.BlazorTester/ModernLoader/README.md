# Second-generation loader migration reference

This directory contains inactive WebAssembly migration scaffolding. Use the root [Blazor tester](../README.md) and [current library guide](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) for supported hosting and application APIs.

The second-generation content was consolidated into the tester's [Modern/UI/Index.razor](../Modern/UI/Index.razor). `/modern` is a compatibility route to the registered Component gallery / Visual grid screen inside `Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application.ApplicationShell`; it does not start the former loader shell.

The active host uses .NET 10 Interactive Server rendering, named plugins with application-local lifecycle tasks, and an in-memory `IDataSource<T>` grid source. The former hardcoded WebSocket transport is inactive. The original theme, artwork and font assets are preserved in the tester's `wwwroot/modern` folder. Files remaining here are superseded scaffolding pending cleanup approval and are not active solution projects.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.DApp.Presentation2.Loader` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`App`](App.razor) | class | Razor root component that routes the loader's assembly and the `MainLayout` assembly, with a not-found message. |
| [`Program`](Program.cs) | class | WebAssembly entry point that mounts `App` at `#app`, registers a scoped `HttpClient` for the host base address and runs the host. |
<!-- domains:end -->

