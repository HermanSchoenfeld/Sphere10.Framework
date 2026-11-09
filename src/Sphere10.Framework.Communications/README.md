# 📡 Sphere10.Framework.Communications

<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

Sphere10.Framework.Communications adds network transports and a JSON-RPC layer to Sphere10 Framework. It implements the core `ProtocolChannel` over anonymous pipes, UDP, TCP and WebSockets, and it provides an attribute-based JSON-RPC server and client over TCP. Use the channels as transports for typed protocols built with the core `ProtocolBuilder` and `ProtocolOrchestrator`, and use the RPC types to let remote callers invoke object methods by name.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Communications` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [DataSource](DataSource/README.md) | Provides experimental CRUD data sources that exchange items with a remote peer over a `ProtocolChannel`. | [source](DataSource/) |
| [EndPoint](EndPoint/README.md) | Provides the whole-message TCP endpoints and connection policies that the JSON-RPC layer runs on. | [source](EndPoint/) |
| [JSON](JSON/README.md) | Provides Newtonsoft.Json converters for hexadecimal byte arrays and for integers written in decimal or `0x` hexadecimal. | [source](JSON/) |
| [Pipes](Pipes/README.md) | Provides a `ProtocolChannel` over a pair of anonymous pipes between a parent process and a child process it starts. | [source](Pipes/) |
| [Protocols](Protocols/README.md) | Provides `WebSocketsPacket`, the JSON envelope that the WebSocket channel hub and data sources exchange. | [source](Protocols/) |
| [RPC](RPC/README.md) | Provides an attribute-driven JSON-RPC 2.0 server, client and service registry that run over the TCP message endpoints. | [source](RPC/) |
| [TCP](TCP/README.md) | Provides `TCPChannel`, a `ProtocolChannel` that sends over a new TCP connection per message and receives from connections accepted on a local listener. | [source](TCP/) |
| [UDP](UDP/README.md) | Provides UDP `ProtocolChannel`s for one fixed peer and a UDP server that creates one channel per remote endpoint. | [source](UDP/) |
| [WebSockets](WebSockets/README.md) | Provides client and server WebSocket `ProtocolChannel`s, a hub that tracks the server's connections, and an older combined channel. | [source](WebSockets/) |
<!-- domains:end -->

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Communications
```

The package depends on [Sphere10.Framework](../Sphere10.Framework/README.md) and Newtonsoft.Json. It targets `net10.0`, and also `net11.0` when it is built with a .NET 11 SDK. To build against a Framework source checkout instead, reference `src/Sphere10.Framework.Communications/Sphere10.Framework.Communications.csproj` as [Independent Sphere10 applications](../../docs/guidelines/patterns/new-projects.md#interchangeable-source-and-nuget-references) describes.

The channel types use the `Sphere10.Framework.Communications` namespace. The JSON-RPC and TCP endpoint types use `Sphere10.Framework.Communications.RPC`.

## 🏗️ Core Architecture

| Area | Main types | Purpose |
|---|---|---|
| JSON-RPC | `JsonRpcServer`, `JsonRpcClient`, `ApiServiceManager`, `JsonRpcConfig` | Expose attributed methods over TCP and call them by name |
| RPC attributes | `[RpcAPIService]`, `[RpcAPIMethod]`, `[RpcAPIArgument]` | Declare service, method and argument names |
| Message endpoints | `IEndPoint`, `TcpEndPoint`, `TcpEndPointListener`, `EndpointMessage` | Carry whole text messages for the RPC layer |
| Protocol channels | `AnonymousPipe`, `UDPChannel`, `TCPChannel`, `ClientWebSocketsChannel`, `ServerWebSocketsChannel` | Byte transports derived from the core `ProtocolChannel` |
| JSON converters | `ByteArrayHexConverter`, `HexadecimalValueConverterReader` | Hexadecimal byte arrays and integers for Newtonsoft.Json |
| Data sources | `ProtocolChannelDataSource<TItem>`, `ClientWebSocketsDataSource<TItem>`, `ServerWebSocketsDataSource<TItem>` | Experimental CRUD data sources over protocol channels, whose WebSocket versions throw `NotImplementedException` from several members. The constructor starts opening the channel, and `OpenTask` completes or faults with that open |

## 🔧 JSON-RPC Server

Mark a class with `[RpcAPIService]` and each callable method with `[RpcAPIMethod]`:

```csharp
using Sphere10.Framework.Communications.RPC;

[RpcAPIService("math")]
public class MathService {
	[RpcAPIMethod]
	public int Add(int a, int b) => a + b;

	[RpcAPIMethod("mul")]
	public float Multiply(float a, float b) => a * b;
}
```

Register the service with `ApiServiceManager` and start a server on a TCP listener. The server resolves every request through that process-wide registry, so register services before clients connect.

```csharp
using System;
using Sphere10.Framework.Communications.RPC;

var service = new MathService();
ApiServiceManager.RegisterService(service); // exposes math.add and math.mul

var listener = new TcpEndPointListener(isLocal: true, port: 27001, maxListener: 5);
var server = new JsonRpcServer(listener, new JsonRpcConfig());
server.OnNewClient += handler => Console.WriteLine("Client connected");
server.Start();

// Serve requests until shutdown, then:
server.Stop();
ApiServiceManager.UnregisterService(service);
```

`isLocal: true` binds the listener to the loopback address, and `false` binds it to every interface. `maxListener` is the listener's connection backlog. Each accepted connection gets a `JsonRpcClientHandler` that processes its calls on a separate task, so service methods must tolerate concurrent calls.

### Naming rules

| Declaration | Registered name |
|---|---|
| `[RpcAPIService("math")]` on the class | The class's methods are prefixed with `math.` |
| No `[RpcAPIService]`, or an empty name | The methods join the anonymous service and are called without a prefix |
| `[RpcAPIMethod]` | The method's own name, such as `math.add` |
| `[RpcAPIMethod("mul")]` | The alias, such as `math.mul`. Repeat the attribute to register several names |
| `[RpcAPIArgument("first")]` on a parameter | `first` is recorded as the argument name in the method's `ApiMethodDescriptor` |

Service and method names are case-insensitive. Registering a second service under the same non-empty name throws, whereas anonymous services can be registered repeatedly to add methods. Public and non-public instance methods can be exposed.

### Arguments and types

Calls pass arguments by position in a JSON array. The server rejects named arguments in a JSON object with error -6, so `[RpcAPIArgument]` names do not affect dispatch. Arguments and results are converted with Newtonsoft.Json, and a `byte[]` argument travels as a hex string. Add `[JsonConverter(typeof(ByteArrayHexConverter))]` to the `byte[]` members of argument and result types so that both sides read them as hex:

```csharp
using Newtonsoft.Json;
using Sphere10.Framework.Communications;
using Sphere10.Framework.Communications.RPC;

public class Payload {
	public int Count;
	public string Name;

	[JsonConverter(typeof(ByteArrayHexConverter))]
	public byte[] Data; // sent as a hex string
}

[RpcAPIService("objects")]
public class ObjectService {
	[RpcAPIMethod]
	public Payload Echo(Payload input) => input;
}
```

An exception thrown by a service method becomes an error response with code -9 that carries the exception's message. A missing method returns -2, or -7 when its service is unknown. A wrong argument count returns -3, and an argument of the wrong type returns -5.

### Wire format

Requests and responses use the JSON-RPC 2.0 envelope. Each message is a single request, a batch array, or several requests on separate lines:

```text
--> {"jsonrpc":"2.0","method":"math.add","params":[5,3],"id":1}
<-- {"jsonrpc":"2.0","result":8,"id":1}
```

The server answers with the request's own ID, whether it is a number, a string or `null`. Error objects carry `code` and `message` members. The persistent-mode result matching in `JsonRpcClientHandler` still expects integer IDs, which `JsonRpcClient` always sends.

## 🔧 JSON-RPC Client

```csharp
using Sphere10.Framework.Communications.RPC;

var client = new JsonRpcClient(new TcpEndPoint("127.0.0.1", 27001), new JsonRpcConfig());

var sum = client.RemoteCall<int>("math.add", 5, 3);           // 8
var product = client.RemoteCall<float>("math.mul", 2.5f, 4f); // 10

var batch = new ApiBatchCallDescriptor();
batch.Call<int>("math.add", 1, 2);
batch.Call<float>("math.mul", 3f, 3f);
object[] results = client.RemoteCall(batch);                  // 3 and 9
```

`RemoteCall<T>` throws when the server returns an error. Use the non-generic `RemoteCall(method, args)` for methods without a return value. A batch returns one entry per call, which holds the converted result, `null` for a method without a return value, or the `JsonRpcException` that the call produced.

### JsonRpcConfig

| Member | Default | Effect |
|---|---|---|
| `ConnectionMode` | `Pulsed` | `Pulsed` opens a connection for each call, and the server closes it after replying. `Persistant` keeps the connection open for Stratum-style sessions in which a `JsonRpcClientHandler` reads both requests and the results of its own calls |
| `IgnoreEmptyReturnValue` | `false` | When `true`, a call to a method without a return value gets no response, and the caller does not wait for one |
| `MaxTimeWaitingForResult` | `30000` | Milliseconds that a caller waits for a result in `Persistant` mode |
| `Logger` | `null` | Optional `ILogger` for connection and dispatch messages, which `JsonRpcServer.SetLogger` can also set on the server's configuration |

The code spells the mode `Persistant`. A plain `JsonRpcClient` should use `Pulsed`, because only a `JsonRpcClientHandler` reads the results that `Persistant` mode waits for.

## 🔗 TCP Message Endpoints

The RPC layer moves whole messages through `IEndPoint`. `TcpEndPoint` connects to its remote address on first use, and `TcpEndPointListener.WaitForMessage` blocks until a client connects and then returns that client's endpoint:

```csharp
using System;
using Sphere10.Framework.Communications.RPC;

// Server process
var listener = new TcpEndPointListener(isLocal: false, port: 8080, maxListener: 5);
listener.Start();
IEndPoint client = listener.WaitForMessage(); // waits for a connection
Console.WriteLine($"Connected: {client.GetDescription()}");
var request = client.ReadMessage();
client.WriteMessage(new EndpointMessage("Response"));

// Client process
var endpoint = new TcpEndPoint("192.168.1.100", 8080) { MaxMessageSize = 8192 };
endpoint.WriteMessage(new EndpointMessage("Hello server"));
Console.WriteLine(endpoint.ReadMessage().ToSafeString());
endpoint.Stop();
```

- Messages carry no length prefix. Each `ReadMessage` returns the bytes of one socket read, up to `MaxMessageSize` (4,096 bytes by default).
- Endpoints that `TcpEndPointListener` accepts take the listener's `MaxMessageSize`.
- `TcpEndPoint.ReadMessage` rejects control characters other than tab, line feed and carriage return, so these endpoints carry text only. `EndpointMessage` converts strings with ASCII encoding.
- The RPC layer has no authentication or encryption. Bind the listener with `isLocal: true` or protect the network path when callers are untrusted.

## 🔗 Protocol Channels

`ProtocolChannel` is defined in the core Sphere10.Framework package, in the `Sphere10.Framework.Communications` namespace. `Open` or `TryOpen` opens a channel and starts its receive loop, and `Close` stops it. Data is sent with `TrySendBytes` and arrives through the `ReceivedBytes` event. `State` moves through `Closed`, `Opening`, `Open` and `Closing`, and the `Opening`, `Opened`, `Closing` and `Closed` events report each change. The channel raises its events asynchronously.

| Channel | Transport | State of the implementation |
|---|---|---|
| `AnonymousPipe` | Two anonymous pipes between a parent process and a child process that it starts | Tests cover its disposal |
| `UDPChannel` | Datagrams between one local and one remote endpoint | The UDP recipe below uses it |
| `UDPServer` and `UDPServerChannel` | One channel per remote UDP endpoint | `Run` listens on `Port` until `Stop`, and raises `ChannelCreated` and `ChannelDestroyed`. No recipe or test exchanges datagrams through it |
| `TCPChannel` | A new TCP connection for every send, and one accepted connection per receive | Its source marks it for a rewrite |
| `ClientWebSocketsChannel`, `ServerWebSocketsChannel` and `ServerWebSocketsChannelHub` | WebSockets, with a channel ID that the server sends after the upgrade handshake completes | Unencrypted only. Passing `secure: true` throws `NotImplementedException` |
| `WebSocketsChannel` | An older combined WebSocket channel | Server role only. The client role throws `NotImplementedException` when it opens, sends or receives |

### Anonymous pipes

The parent process starts the child and passes it the two pipe handles on its command line:

```csharp
using System;
using System.Text;
using Sphere10.Framework.Communications;

var pipe = AnonymousPipe.ToChildProcess(
	"child.exe",
	argInjectorFunc: (args, readHandle, writeHandle) => $"{args} --read={readHandle} --write={writeHandle}");
pipe.ReceivedBytes += bytes => Console.WriteLine(Encoding.UTF8.GetString(bytes.Span));
await pipe.Open();
await pipe.TrySendBytes(Encoding.UTF8.GetBytes("Hello from parent"));
```

The child builds an `AnonymousPipeEndpoint` from the same two handles under the parent's names, and `AnonymousClientPipe` reverses their directions:

```csharp
// readHandle and writeHandle are the --read and --write values from the command line.
var pipe = AnonymousPipe.FromChildProcess(new AnonymousPipeEndpoint(readHandle, writeHandle));
pipe.ReceivedBytes += bytes => Console.WriteLine(Encoding.UTF8.GetString(bytes.Span));
await pipe.Open();
await pipe.TrySendBytes(Encoding.UTF8.GetBytes("Hello from child"));
```

- `ToChildProcess` requires the executable to exist. Without an `argInjectorFunc`, it appends the read handle and then the write handle to the arguments.
- Each message travels as one line of text. `TrySendBytes` writes the bytes as Base64, and the receive loop decodes each Base64 line to raise `ReceivedBytes`.
- `TrySendString` writes a raw line, and the `ReceivedString` and `SentString` events report raw lines. A line that is not Base64 raises only `ReceivedString` on the receiving pipe. A raw line that happens to be valid Base64 also raises `ReceivedBytes`, so do not mix raw strings and bytes when the strings could look like Base64.
- On Windows, each send first waits for the other process to drain the previous message. Other platforms write directly.
- Closing the parent's pipe waits up to `DefaultTimeout` (five seconds) for the child to exit, and kills the child if it is still running.

### UDP and typed protocols

A `UDPChannel` exchanges datagrams with one peer, which creates its own channel with the two endpoints swapped:

```csharp
using System;
using System.Net;
using Sphere10.Framework.Communications;

var local = new IPEndPoint(IPAddress.Loopback, 9001);
var peer = new IPEndPoint(IPAddress.Loopback, 9002);
var channel = new UDPChannel(local, peer, CommunicationRole.Client);
channel.ReceivedBytes += bytes => Console.WriteLine($"Received {bytes.Length} bytes");
await channel.Open();
await channel.TrySendBytes(new byte[] { 1, 2, 3 });
await channel.Close();
```

For typed messages, pass any channel to the core `ProtocolOrchestrator` together with a `Protocol` that `ProtocolBuilder` builds. The orchestrator opens the channel, runs the handshake and dispatches requests, responses and commands, as the core library's [protocol overview](../Sphere10.Framework/README.md#-protocol-and-communication) describes. The [UDP protocol recipe](../../recipes/AbstractProtocol.UPDSimple/Program.cs) runs a client and a server over `UDPChannel` with a three-way handshake. It belongs to the Windows solution and binds UDP ports 100 and 101 on the loopback address:

```powershell
dotnet run --project ./recipes/AbstractProtocol.UPDSimple/AbstractProtocol.UDPSimple.csproj
```

## 🛡️ Security Policies

`TcpSecurityPolicies` holds process-wide settings for the JSON-RPC server, and two of its checks are active:

- `ValidateConnectionCount` limits concurrent RPC connections to `MaxSimultanousConnecitons` (200 by default) and throws `TooManyConnectionsException` beyond it. A rejected connection does not count towards the limit, and `JsonRpcServer` closes its socket.
- `ValidateJsonQuality` rejects received bytes that contain control characters other than tab, line feed and carriage return. `TcpEndPoint.ReadMessage` applies it to every read.

`MonitorPotentialAttack` is a placeholder. The spoofing and connection-rate settings (`MaxSpoofStringPerClient`, `MaxClientConnectionPerSecond` and their ban thresholds) are not used yet.

## 🧪 Tests

The [Communications tests](../../tests/Sphere10.Framework.Communications.Tests/) exercise protocol-channel and anonymous-pipe disposal in `ProtocolChannelDisposalTests`. `ChannelAndRpcTests` covers anonymous-pipe line decoding, accepted TCP endpoint sizes, the connection limit, `EndpointMessage.ToSafeString`, service dispatch errors, the JSON-RPC response members and `UDPServer` start and stop. The networked JSON-RPC fixtures in `RPCSimpleTests.cs` and `RPCComplexTests.cs` are commented out, so no active test runs a client against a server. Run the project from the repository root:

```powershell
dotnet test ./tests/Sphere10.Framework.Communications.Tests/Sphere10.Framework.Communications.Tests.csproj --configuration Release
```

GitHub Actions runs this test assembly in its test job.

## 📁 Project Structure

| Directory | Contents |
|---|---|
| `RPC/` | JSON-RPC server, client and client handler, the service registry, attributes and wire types |
| `EndPoint/` | `IEndPoint`, `EndpointMessage`, `TcpEndPoint`, `TcpEndPointListener` and `TcpSecurityPolicies` |
| `JSON/` | `ByteArrayHexConverter`, and `HexadecimalValueConverterReader`, which reads integers written in decimal or `0x` hexadecimal |
| `Pipes/` | `AnonymousPipe`, its server and client sides, and `AnonymousPipeEndpoint` |
| `TCP/` | `TCPChannel` |
| `UDP/` | `UDPChannel` and `UDPServer` |
| `WebSockets/` | Client, server and hub WebSocket channels, and the older `WebSocketsChannel` |
| `Protocols/` | `WebSocketsPacket`, the JSON envelope that the WebSocket data sources exchange |
| `DataSource/` | `ProtocolChannelDataSource<TItem>` and the WebSocket client and server data sources |

## ⚖️ License

Distributed under the **MIT License**.

See [LICENSE](../../LICENSE) for full details. More information: [MIT License](https://opensource.org/license/mit)

## 👤 Author

**Herman Schoenfeld** - Software Engineer
