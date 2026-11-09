# Loader View Models

Provides the view models behind the loader's menus, endpoint selector and endpoint page.

Each view model derives from the library's `ComponentViewModelBase`, and its component inherits `ComponentWithViewModel<T>`, which injects the view model from the service provider and disposes it with the component. `Sphere10Plugin` registers `HomeViewModel`, `ServersViewModel` and `SidebarBrandViewModel` as transient services for the current tester. `AppsMenuViewModel`, `BlockMenuViewModel` and `MainMenuViewModel` follow the routed-application manager of the original plugin shell and serve the components in [Components](../Components/README.md). The view models that subscribe to events implement `IDisposable` to unsubscribe.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AppsMenuViewModel`](AppsMenuViewModel.cs) | class | Lists the routed applications and tracks the selected one for `AppsMenu` |
| [`BlockMenuViewModel`](BlockMenuViewModel.cs) | class | Tracks the selected application and block and exposes its blocks for `BlockMenu` |
| [`HomeViewModel`](HomeViewModel.cs) | class | Empty view model injected by the `Home` dashboard |
| [`MainMenuViewModel`](MainMenuViewModel.cs) | class | Merges the default menu items with the selected page's menu items for `MainMenu` |
| [`NewServer`](ServersViewModel.cs) | class | Form model for a new endpoint whose `Uri` is required and must be a URL |
| [`ServersViewModel`](ServersViewModel.cs) | class | Exposes the endpoints and active endpoint of `IEndpointManager`, selects an endpoint and adds the `NewServer` entry for `Servers` |
| [`SidebarBrandViewModel`](SidebarBrandViewModel.cs) | class | Exposes the current and available endpoints and selects one for `SidebarBrand` |
