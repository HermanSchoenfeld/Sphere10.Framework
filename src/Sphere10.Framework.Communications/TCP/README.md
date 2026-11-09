# TCP Channel

Provides `TCPChannel`, a `ProtocolChannel` that sends over a new TCP connection per message and receives from connections accepted on a local listener.

Opening the channel starts a `TcpListener` on `LocalEndpoint`. Each receive accepts one incoming connection and reads the data available on it, and each send connects to `RemoteEndpoint`, writes the bytes and closes. `IsConnectionAlive` only checks that the listener exists. The source marks this channel for a rewrite, so prefer [UDP](../UDP/README.md), [WebSockets](../WebSockets/README.md) or [anonymous pipes](../Pipes/README.md) when a persistent connection matters. The whole-message TCP endpoints used by JSON-RPC are a separate design in [EndPoint](../EndPoint/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`TCPChannel`](TCPChannel.cs) | class | `ProtocolChannel` that listens on a local endpoint for incoming messages and opens a fresh connection to the remote endpoint for each send |
