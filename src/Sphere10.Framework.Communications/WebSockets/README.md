# WebSocket Channels

Provides client and server WebSocket `ProtocolChannel`s, a hub that tracks the server's connections, and an older combined channel.

`ClientWebSocketsChannel` connects to a WebSocket URI and receives its channel `Id` from the server after the upgrade handshake. `ServerWebSocketsChannel` listens on a local endpoint and performs the upgrade itself. When it is created with `hub: true` it owns a `ServerWebSocketsChannelHub`, which creates a new channel for each accepted connection and routes [`WebSocketsPacket`](../Protocols/README.md)s by channel id. The hub can send to one channel or all of them, and `Report()` lists their states. The hub's channel dictionary is not synchronized, so drive it from one thread.

Only unencrypted WebSockets work. Passing `secure: true` throws `NotImplementedException`. `WebSocketsChannel` is the older combined design that supports the server role only, and its client role throws `NotImplementedException`. The [WebSocket data sources](../DataSource/README.md) build on the client channel and the hub.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ClientWebSocketsChannel`](ClientWebSocketsChannel.cs) | class | Client `ProtocolChannel` over `ClientWebSocket` that records the channel id the server assigns and raises `ReceivedWebSocketMessage` |
| [`ServerWebSocketsChannel`](ServerWebSocketsChannel.cs) | class | Server `ProtocolChannel` that accepts a TCP connection, performs the WebSocket upgrade and optionally creates a hub |
| [`ServerWebSocketsChannelHub`](ServerWebSocketsChannelHub.cs) | class | Tracks server channels by id, opens one per new connection, forwards received bytes and sends packets to one or all channels |
| [`WebSocketsChannel`](WebSocketsChannel.cs) | class | Older combined WebSocket `ProtocolChannel` whose server role works and whose client role is not implemented |
