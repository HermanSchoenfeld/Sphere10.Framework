# Loader Components

Holds the original loader shell's Razor navigation and modal components, including the endpoint selector that the current tester still shows.

These components belong to the original standalone loader shell. The current tester compiles them, but only `SidebarBrand` is still rendered, in the sidebar header of the tester's `Workspace`. It shows the endpoint selector over the scoped `IEndpointManager` from [Services](../Services/README.md). The other components remain as migration references for the older routed-plugin shell, which [MainLayout](../Layouts/README.md) composes. Most of them take their state from a view model in [ViewModels](../ViewModels/README.md) through `ComponentWithViewModel<T>`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AppsMenu`](AppsMenu.razor) | class | Razor sidebar list of routed plugin applications as icon links, driven by `AppsMenuViewModel` |
| [`BlockMenu`](BlockMenu.razor) | class | Razor collapsible sidebar menu of the selected application's blocks and pages, driven by `BlockMenuViewModel` |
| [`MainMenu`](MainMenu.razor) | class | Razor top navigation bar of the selected application's menu items and dropdowns, driven by `MainMenuViewModel` |
| [`Sidebar`](Sidebar.razor) | class | Razor original sidebar with brand, links to the gallery, modals, tables, wizards and endpoints, and the block and apps menus |
| [`SidebarBrand`](SidebarBrand.razor) | class | Razor brand link and endpoint selector bound to `SidebarBrandViewModel`, with a link to the endpoint management screen |
| [`Topbar`](Topbar.razor) | class | Razor original top bar with the main menu, a `SearchInput` and the template's alert, message and user dropdowns |
