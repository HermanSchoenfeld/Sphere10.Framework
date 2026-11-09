# Plugins

Blazor plugin definitions and builders for the application workspace, and the older routed-plugin navigation model.

`BlazorPlugin`, `BlazorPluginBuilder` and `IBlazorPlugin` are in the root `Sphere10.Framework.Web.AspNetCore.Blazor` namespace and belong to the current workspace in [Application](../Application/README.md). A plugin groups application blocks, plugin menus and toolbar items, and service registrations. Register one with `AddSphere10BlazorPlugin`, or with `AddPlugin` on the `ApplicationBuilder`. Plugin configuration runs once at startup, and each circuit's application receives copies of the plugin's blocks.

The `BlazorRouted*` types in the `Plugins` namespace are the earlier model, in which plugins describe applications, blocks and pages by URL route rather than as hosted screens. `AddSphere10BlazorPlugins<TPluginLocator>()` registers the locator, `DefaultBlazorRoutedPluginManager` and `DefaultBlazorRoutedApplicationManager` as scoped services. The plugin manager activates each type that the locator returns, and the application manager selects the application and page that match the current `NavigationManager` location.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BlazorPlugin`](BlazorPlugin.cs) | class | Startup plugin definition that holds Blazor application blocks, plugin menus and toolbar items on the shared plugin base. |
| [`BlazorPluginBuilder`](BlazorPluginBuilder.cs) | class | Fluent builder that configures a plugin's blocks, menus and services and builds a `BlazorPlugin`. |
| [`BlazorRoutedApplication`](BlazorRoutedApplication.cs) | class | Routed application with a route, name, icon and its routed blocks. |
| [`BlazorRoutedApplicationBlock`](BlazorRoutedApplicationBlock.cs) | class | Named block with an icon and the routed pages it groups. |
| [`BlazorRoutedApplicationPage`](BlazorRoutedApplicationPage.cs) | class | Routed page with a route, name, icon and menu items. |
| [`BlazorRoutedApplicationPageSelectedEventArgs`](BlazorRoutedApplicationPageSelectedEventArgs.cs) | class | Event arguments that carry the page selected by navigation. |
| [`BlazorRoutedApplicationSelectedEventArgs`](BlazorRoutedApplicationSelectedEventArgs.cs) | class | Event arguments that carry the application selected by navigation. |
| [`BlazorRoutedMenuItem`](BlazorRoutedMenuItem.cs) | class | Routed menu entry with a heading, route, icon path and child items. |
| [`BlazorRoutedMenuItemExtensions`](BlazorRoutedMenuItemExtensions.cs) | static class | `Merge` combines routed menu trees by heading and `Copy` deep-copies a routed menu tree. |
| [`BlazorRoutedPlugin`](BlazorRoutedPlugin.cs) | abstract class | Base routed plugin that registers the view models of its assembly before calling `ConfigureServicesInternal`. |
| [`DefaultBlazorRoutedApplicationManager`](DefaultBlazorRoutedApplicationManager.cs) | class | Tracks the routed application and page matching the current location and raises selection events when it changes. |
| [`DefaultBlazorRoutedPluginManager`](DefaultBlazorRoutedPluginManager.cs) | class | Activates the plugin types returned by an `IBlazorRoutedPluginLocator` and forwards service configuration to each plugin. |
| [`IBlazorPlugin`](IBlazorPlugin.cs) | interface | Shared application plugin whose blocks carry Blazor screen and presentation metadata. |
| [`IBlazorRoutedApplication`](IBlazorRoutedApplication.cs) | interface | Routable, named application with an icon and routed blocks. |
| [`IBlazorRoutedApplicationBlock`](IBlazorRoutedApplicationBlock.cs) | interface | Named block with an icon and routed pages. |
| [`IBlazorRoutedApplicationManager`](IBlazorRoutedApplicationManager.cs) | interface | Routed applications, the selected application and page, and their selection events. |
| [`IBlazorRoutedApplicationPage`](IBlazorRoutedApplicationPage.cs) | interface | Routable, named page with an icon and menu items. |
| [`IBlazorRoutedMenuProvider`](IBlazorRoutedMenuProvider.cs) | interface | Supplies an array of `BlazorRoutedMenuItem` entries. |
| [`IBlazorRoutedPlugin`](IBlazorRoutedPlugin.cs) | interface | Routed plugin that exposes its applications and configures its services. |
| [`IBlazorRoutedPluginLocator`](IBlazorRoutedPluginLocator.cs) | interface | Returns the routed plugin types that the application should load. |
| [`IBlazorRoutedPluginManager`](IBlazorRoutedPluginManager.cs) | interface | Loaded routed plugins and their combined service configuration. |
| [`IIconItem`](IIconItem.cs) | interface | Item with a Font Awesome CSS icon class for lists and menus. |
| [`INamedItem`](INamedItem.cs) | interface | Item with a display name. |
| [`IRoutablePage`](IRoutablePage.cs) | interface | Item with a navigation route. |
| [`NavigationManagerExtensions`](NavigationManagerExtensions.cs) | static class | `ToBaseRelativePathWithSlash` returns a `NavigationManager` base-relative path with a leading slash. |
| [`StringExtensions`](StringExtensions.cs) | static class | Route string helpers that take the first path segment and trim query parameters and fragments. |
