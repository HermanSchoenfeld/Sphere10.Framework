# Plugins

Blazor plugin definitions and builders for the application workspace.

`BlazorPlugin`, `BlazorPluginBuilder` and `IBlazorPlugin` are in the root `Sphere10.Framework.Web.AspNetCore.Blazor` namespace and belong to the current workspace in [Application](../Application/README.md). A plugin groups application blocks, plugin menus and toolbar items, and service registrations. Register one with `AddSphere10BlazorPlugin`, or with `AddPlugin` on the `ApplicationBuilder`. Plugin configuration runs once at startup, and each circuit's application receives copies of the plugin's blocks.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorPlugin`](BlazorPlugin.cs) | class | Startup plugin definition that holds Blazor application blocks, plugin menus and toolbar items on the shared plugin base. |
| [`BlazorPluginBuilder`](BlazorPluginBuilder.cs) | class | Fluent builder that configures a plugin's blocks, menus and services and builds a `BlazorPlugin`. |
| [`IBlazorPlugin`](IBlazorPlugin.cs) | interface | Shared application plugin whose blocks carry Blazor screen and presentation metadata. |
