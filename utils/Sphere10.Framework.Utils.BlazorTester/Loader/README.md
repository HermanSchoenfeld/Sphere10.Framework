# Original loader shell

Retained loader content is hosted by the current [Blazor tester](../README.md), which uses `UI.Application.ApplicationShell` from `Sphere10.Framework.Web.AspNetCore.Blazor`. The endpoint selector, dashboard and endpoint screen remain runnable in that shared workspace; the older standalone shell is a migration reference.

The active entry point is [Program.cs](../Program.cs). [Sphere10Plugin.Configure](Sphere10Plugin.cs) contributes the Workspace block, circuit-scoped endpoint/state services and named plugin startup/shutdown tasks. Widget Gallery contributes the Legacy examples block containing the retained loader screens. Blocks are the runtime navigation children; plugins appear in `IApplication.LoadedPlugins` as startup contributors.

Endpoint selection and mock node data are scoped to the interactive server circuit. Superseded WebAssembly project/entry files and static-asset copies remain inactive migration references pending cleanup approval. Use the root tester project and [current hosting guide](../../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md) when integrating these components.
