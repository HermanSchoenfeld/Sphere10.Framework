# UDP Channels

Provides UDP `ProtocolChannel`s for one fixed peer and a UDP server that creates one channel per remote endpoint.

`UDPChannel` exchanges datagrams between one local and one remote `IPEndPoint`, and the peer creates its own channel with the two endpoints swapped. Pass it to the core `ProtocolOrchestrator` with a `Protocol` from `ProtocolBuilder` to carry typed messages, as the UDP protocol recipe in the [project README](../README.md) shows. `UDPServer` listens on `Port` while `Run()` is awaited, creates a nested `UDPServer.UDPServerChannel` for each new remote endpoint and raises `ChannelCreated` and `ChannelDestroyed`. Override `IsEndpointAccepted` to filter peers. `Stop()` ends the loop, and disposing the server stops it when it is running.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`UDPChannel`](UDPChannel.cs) | class | `ProtocolChannel` that sends and receives datagrams between one local and one remote endpoint |
| [`UDPServer`](UDPServer.cs) | class | Async-disposable UDP listener that routes datagrams to a per-endpoint nested `UDPServerChannel` and raises channel created and destroyed events |
