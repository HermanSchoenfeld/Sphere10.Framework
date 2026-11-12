# Loader View Models

Provides the view models behind the loader's menus, endpoint selector and endpoint page.

Each view model derives from the library's `ComponentViewModelBase`, and its component inherits `ComponentWithViewModel<T>`, which injects the view model from the service provider and disposes it with the component. `Sphere10Plugin` registers `HomeViewModel`, `ServersViewModel` and `SidebarBrandViewModel` as transient services for the current tester. `AppsMenuViewModel`, `BlockMenuViewModel` and `MainMenuViewModel` follow the routed-application manager of the original plugin shell and serve the components in [Components](../Components/README.md). The view models that subscribe to events implement `IDisposable` to unsubscribe.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`HomeViewModel`](HomeViewModel.cs) | class | Empty view model injected by the `Home` dashboard |
| [`NewServer`](ServersViewModel.cs) | class | Form model for a new endpoint whose `Uri` is required and must be a URL |
| [`ServersViewModel`](ServersViewModel.cs) | class | Exposes the endpoints and active endpoint of `IEndpointManager`, selects an endpoint and adds the `NewServer` entry for `Servers` |
| [`SidebarBrandViewModel`](SidebarBrandViewModel.cs) | class | Exposes the current and available endpoints and selects one for `SidebarBrand` |
