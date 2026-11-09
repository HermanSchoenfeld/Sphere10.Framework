# Protocol Channel Data Sources

Provides experimental CRUD data sources that exchange items with a remote peer over a `ProtocolChannel`.

`ProtocolChannelDataSource<TItem>` derives from the core `AsyncBatchDataSourceBase<TItem>`. Its constructor starts opening the channel, and `OpenTask` completes when the channel opens or faults with the channel's error. It reports read, search, sort and page capabilities and raises `MutatedItems` when the peer sends changes. The WebSocket versions exchange [`WebSocketsPacket`](../Protocols/README.md) envelopes over the channels in [WebSockets](../WebSockets/README.md). They take delegates that turn an item into the text of an initialize, update or id message.

The client sends requests with its `NewDelayed`, `ReadDelayed`, `UpdateDelayed` and similar methods, and reports each server reply through `MutatedItems`. These classes are incomplete. `ClientWebSocketsDataSource<TItem>.CountAsync`, `NewRange` and `CreateRangeAsync` throw `NotImplementedException`, and so do the `...Delayed` members of `ServerWebSocketsDataSource<TItem>`. Prefer the data sources in the core library for production screens.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ClientWebSocketsDataSource<TItem>`](ClientWebSocketsDataSource.cs) | class | Client-side data source that sends `...Delayed` CRUD requests over a `ClientWebSocketsChannel` and raises `MutatedItems` for the server's replies |
| [`ProtocolChannelDataSource<TItem>`](ProtocolChannelDataSource.cs) | abstract class | Batch data source base that opens a `ProtocolChannel`, sends bytes through it and raises `MutatedItems`, with the item-to-text delegate types |
| [`ServerWebSocketsDataSource<TItem>`](ServerWebSocketsDataSource.cs) | class | Server-side data source that keeps items in a dictionary keyed by the id delegate and answers client packets through a `ServerWebSocketsChannelHub` |
