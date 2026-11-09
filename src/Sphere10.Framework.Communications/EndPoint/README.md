# Message Endpoints

Provides the whole-message TCP endpoints and connection policies that the JSON-RPC layer runs on.

`IEndPoint` reads and writes `EndpointMessage`s. `TcpEndPoint` connects to a remote address on first use, and `TcpEndPointListener.WaitForMessage` blocks until a client connects and returns that client's `TcpEndPoint`. Messages carry no length prefix, so each `ReadMessage` returns the bytes of one socket read, up to `MaxMessageSize` (4,096 bytes by default). `TcpEndPoint.ReadMessage` rejects control characters other than tab, line feed and carriage return, and `EndpointMessage` converts strings with ASCII encoding, so these endpoints carry text only.

`TcpSecurityPolicies` holds process-wide static limits. `ValidateConnectionCount` caps concurrent RPC connections and throws `TooManyConnectionsException`, and `ValidateJsonQuality` performs the control-character check. `MonitorPotentialAttack` is an empty placeholder. The types use the `Sphere10.Framework.Communications.RPC` namespace and are consumed by [RPC](../RPC/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`EndpointMessage`](EndpointMessage.cs) | class | Message payload as bytes, with ASCII string conversion, a `ToSafeString` that drops control characters other than tab and line breaks, and the endpoint it belongs to |
| [`IEndPoint`](IEndPoint.cs) | interface | Message endpoint contract: description, unique id, wait for a connection, read and write messages, start, stop and open state |
| [`TcpEndPoint`](TcpEndPoint.cs) | class | `IEndPoint` over a `TcpClient` that connects lazily and reads one socket read per message |
| [`TcpEndPointListener`](TcpEndPointListener.cs) | class | `IEndPoint` that listens on a loopback or all-interface port and returns each accepted client as a `TcpEndPoint` |
| [`TcpSecurityPolicies`](TcpSecurityPolicies.cs) | class | Static connection-count limit, JSON control-character validation and unused spoofing and rate-limit settings |
| [`TooManyConnectionsException`](TcpSecurityPolicies.cs) | class | Raised when a new connection would exceed `TcpSecurityPolicies.MaxSimultanousConnecitons` |
