# Loader Pages

Holds the original loader's dashboard, endpoint management and not-found pages, which the current tester still hosts.

`Home` and `Servers` carry no route of their own. The tester's `LegacyDashboardScreen` and `EndpointScreen` render them inside the Legacy examples block. `Servers` lists the sample endpoints of the scoped `IEndpointManager` from [Services](../Services/README.md) through `ServersViewModel`, and adding or selecting an endpoint updates the [`SidebarBrand`](../Components/README.md) selector in the same circuit. `NotFound` is the `/not-found` page that the tester's router uses for unknown URLs.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Home`](Home.razor) | class | Razor legacy dashboard of static, illustrative statistics cards |
| [`Servers`](Servers.razor) | class | Razor endpoint sample that lists, selects and validates new endpoint URIs through `ServersViewModel` without contacting them |
