# WebSocket Packets

Provides `WebSocketsPacket`, the JSON envelope that the WebSocket channel hub and data sources exchange.

A packet carries the channel `Id`, a space-separated command `Message` such as `read <term> <pageLength> <page> <sortProperty> <direction>`, the `Tokens` split from that message, and optional `JsonData`. `ToBytes()` serializes the packet with Newtonsoft.Json and ASCII encoding, and the `byte[]` constructor reverses it. [`ServerWebSocketsChannelHub`](../WebSockets/README.md) routes packets by `Id`, and the [data sources](../DataSource/README.md) interpret the commands.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`WebSocketsPacket`](WebSocketsPacket.cs) | class | JSON envelope with channel id, command message, message tokens and JSON payload, convertible to and from ASCII bytes |
