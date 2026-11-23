# Sphere10.Framework.Communications.Tests

NUnit tests for [Sphere10.Framework.Communications](../../src/Sphere10.Framework.Communications/README.md). They cover anonymous-pipe and TCP channels, the TCP connection-count policy, endpoint messages, the JSON-RPC service layer, the UDP server, `ProtocolChannel` disposal, typed protocol dispatch and the orchestrator queue, the `Tools.Network` diagnostics and decoders, `NetworkProbeDataSource` and `Tools.Mail` delivery.

## Run the tests

Run these commands from the repository root. `dotnet test` restores and builds the referenced projects first. `BuildRevision=0` supplies an explicit local build revision. The project targets `net8.0`, `net9.0` and `net10.0`, plus `net11.0` when the building SDK supports .NET 11, and `dotnet test` runs every target. Add `-f net10.0` to run one.

```powershell
dotnet test tests/Sphere10.Framework.Communications.Tests/Sphere10.Framework.Communications.Tests.csproj -c Release -p:BuildRevision=0
```

Run one fixture while developing:

```powershell
dotnet test tests/Sphere10.Framework.Communications.Tests/Sphere10.Framework.Communications.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~ProtocolChannelDisposalTests"
```

Leave out the loopback SMTP fixture, the only one in the `Integration` category:

```powershell
dotnet test tests/Sphere10.Framework.Communications.Tests/Sphere10.Framework.Communications.Tests.csproj -c Release -p:BuildRevision=0 --filter "TestCategory!=Integration"
```

## Requirements

- The TCP and UDP cases bind ephemeral loopback ports. They need no external network or service. `ChannelAndRpcTests` is not parallelizable because `TcpSecurityPolicies` holds process-wide state.
- `MailTlsIntegrationTests` talks to an in-process loopback SMTP listener. It temporarily changes the process-wide SMTP certificate policy, so the fixture is non-parallel.
- `NetworkDiagnosticsTests` resolves `localhost` and traces the route to `127.0.0.1`. `MacAddressTests` reads the local network interfaces. `NetworkToolProbeTests` connects to listeners it opens on the loopback address, and its closed-port case waits about two seconds for Windows to refuse the connection. None of them sends public network traffic.

## Contents

### Project root

The channel, RPC and disposal fixtures.

| Fixture | Purpose |
| --- | --- |
| [ChannelAndRpcTests](ChannelAndRpcTests.cs) | Anonymous-pipe line decoding, accepted TCP endpoints inheriting the listener's message size, connection-slot release in `TcpSecurityPolicies`, `EndpointMessage.ToSafeString`, `ApiService` error reporting, JSON-RPC 2.0 response members and `UDPServer` start and stop. |
| [ProtocolChannelDisposalTests](ProtocolChannelDisposalTests.cs) | Synchronous `Dispose` joining the whole asynchronous `DisposeAsync` override and its failure, and `AnonymousPipe` disposing each owned pipe asynchronously exactly once. |

`RPCSimpleTests.cs` and `RPCComplexTests.cs` are fully commented out. They contain no compiled fixtures.

### Misc

| Fixture | Purpose |
| --- | --- |
| [MailTlsIntegrationTests](Misc/MailTlsIntegrationTests.cs) | `Tools.Mail` plain and STARTTLS delivery to a loopback server, certificate trust and borrowed content. |

### Network

Tests for the [network domain](../../src/Sphere10.Framework.Communications/Network/README.md).

| Fixture | Purpose |
| --- | --- |
| [MacAddressTests](Network/MacAddressTests.cs) | MAC address listing and the default salted address. |
| [NetworkDiagnosticsTests](Network/NetworkDiagnosticsTests.cs) | Whois against a loopback server, host resolution and a loopback trace route. |
| [NetworkProbeDataSourceTests](Network/NetworkProbeDataSourceTests.cs) | `NetworkProbeDataSource` commands, repetition, cancellation, stop and retained-data bounds. |
| [NetworkProtocolTests](Network/NetworkProtocolTests.cs) | DNS response and TLS ClientHello decoding, including malformed input. |
| [NetworkToolProbeTests](Network/NetworkToolProbeTests.cs) | The server a WHOIS answer refers to, and the TCP port probe against open and released loopback ports, invalid arguments and cancellation. |

### Protocol

Tests for the [protocol domain](../../src/Sphere10.Framework.Communications/Protocol/README.md).

| Fixture | Purpose |
| --- | --- |
| [ProtocolOrchestratorQueueTests](Protocol/ProtocolOrchestratorQueueTests.cs) | Outgoing messages wait for orchestrator startup and keep their order, and a failing message reports its envelope while the queue continues. |
| [TypedProtocolAdapterTests](Protocol/TypedProtocolAdapterTests.cs) | Typed command, request, response, handshake and generator dispatch, and `ProtocolBuilder` composition. |
