# Sphere10.Framework.Communications.Tests

NUnit tests for [Sphere10.Framework.Communications](../../src/Sphere10.Framework.Communications/README.md). They cover anonymous-pipe and TCP channels, the TCP connection-count policy, endpoint messages, the JSON-RPC service layer, the UDP server and `ProtocolChannel` disposal.

## Run the tests

Run these commands from the repository root. `dotnet test` restores and builds the referenced projects first. `BuildRevision=0` supplies an explicit local build revision.

```powershell
dotnet test tests/Sphere10.Framework.Communications.Tests/Sphere10.Framework.Communications.Tests.csproj -c Release -p:BuildRevision=0
```

Run one fixture while developing:

```powershell
dotnet test tests/Sphere10.Framework.Communications.Tests/Sphere10.Framework.Communications.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~ProtocolChannelDisposalTests"
```

## Requirements

The TCP and UDP cases bind ephemeral loopback ports. They need no external network or service. `ChannelAndRpcTests` is not parallelizable because `TcpSecurityPolicies` holds process-wide state.

## Contents

### Project root

The channel, RPC and disposal fixtures.

| Fixture | Purpose |
| --- | --- |
| [ChannelAndRpcTests](ChannelAndRpcTests.cs) | Anonymous-pipe line decoding, accepted TCP endpoints inheriting the listener's message size, connection-slot release in `TcpSecurityPolicies`, `EndpointMessage.ToSafeString`, `ApiService` error reporting, JSON-RPC 2.0 response members and `UDPServer` start and stop. |
| [ProtocolChannelDisposalTests](ProtocolChannelDisposalTests.cs) | Synchronous `Dispose` joining the whole asynchronous `DisposeAsync` override and its failure, and `AnonymousPipe` disposing each owned pipe asynchronously exactly once. |

`RPCSimpleTests.cs` and `RPCComplexTests.cs` are fully commented out. They contain no compiled fixtures.
