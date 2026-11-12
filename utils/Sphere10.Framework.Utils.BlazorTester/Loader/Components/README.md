# Loader Components

Holds the original loader shell's Razor navigation and modal components, including the endpoint selector that the current tester still shows.

These components belong to the original standalone loader shell. The current tester compiles them, but only `SidebarBrand` is still rendered, in the sidebar header of the tester's `Workspace`. It shows the endpoint selector over the scoped `IEndpointManager` from [Services](../Services/README.md). The other components remain as migration references for the older routed-plugin shell, which [MainLayout](../Layouts/README.md) composes. Most of them take their state from a view model in [ViewModels](../ViewModels/README.md) through `ComponentWithViewModel<T>`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`LogoutModal`](LogoutModal.razor) | class | Razor static Bootstrap logout confirmation dialog from the original template |
| [`NewServerModal`](NewServerModal.razor) | class | Razor placeholder modal for adding a server, with a fixed input and unbound buttons |
| [`Sidebar`](Sidebar.razor) | class | Razor original sidebar with brand, links to the gallery, modals, tables, wizards and endpoints |
| [`SidebarBrand`](SidebarBrand.razor) | class | Razor brand link and endpoint selector bound to `SidebarBrandViewModel`, with a link to the endpoint management screen |
| [`Topbar`](Topbar.razor) | class | Razor original top bar with a `SearchInput` and the template's alert, message and user dropdowns |
