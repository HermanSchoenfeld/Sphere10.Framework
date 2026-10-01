# Original loader shell

The original loader now runs inside [Sphere10.Framework.Utils.BlazorTester](../README.md). Its layouts, menu components, dashboard and endpoint screen are preserved here. Shared modal, wizard, event and plugin-manager services moved into `Sphere10.Framework.Web.AspNetCore.Blazor`.

The active entry point is `../Program.cs`. Endpoint selection and mock node data are scoped to the interactive server session. Superseded WebAssembly project/entry files and static-asset copies remain inactive migration references pending cleanup approval.
