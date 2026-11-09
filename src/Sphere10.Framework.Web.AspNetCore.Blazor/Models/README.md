# Models

Small records and classes used by the original component generation for search results and paged table requests.

`SearchResult` is what a `Components.SearchInput` provider returns. `ItemRequest` and `ItemsResponse<TItem>` are the request and response of the `Components.Tables.VirtualPagedTable<TItem>` items provider, described in [Components](../Components/README.md). The `UI.Grids` namespace in [UI](../UI/README.md) declares its own `ItemRequest` and `ItemsResponse<TItem>` records with the same shape for the current tables. `Block` is sample data used by the example `INodeService` contract in [Services](../Services/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Block`](Block.cs) | class | Sample data with a block number and a random GUID address, returned by `INodeService`. |
| [`ItemRequest`](ItemsRequest.cs) | record | Requested item index, count, sort column and sort direction for a virtual-paged table. |
| [`ItemsResponse<TItem>`](ItemsResponse.cs) | record | One page of items and the total item count returned to a virtual-paged table. |
| [`SearchResult`](SearchResult.cs) | record | Display name and route `Uri` of one header search result. |
