# 📡 Sphere10.Framework.Communications

<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

Sphere10.Framework.Communications adds network transports and a JSON-RPC layer to Sphere10 Framework. It implements the core `ProtocolChannel` over anonymous pipes, UDP, TCP and WebSockets, and it provides an attribute-based JSON-RPC server and client over TCP. Use the channels as transports for typed protocols built with the core `ProtocolBuilder` and `ProtocolOrchestrator`, and use the RPC types to let remote callers invoke object methods by name.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Communications
```

The package depends on [Sphere10.Framework](../Sphere10.Framework/README.md) and Newtonsoft.Json. It targets `net10.0`, and also `net11.0` when it is built with a .NET 11 SDK. To build against a Framework source checkout instead, reference `src/Sphere10.Framework.Communications/Sphere10.Framework.Communications.csproj` as [Independent Sphere10 applications](../../docs/guidelines/patterns/new-projects.md#interchangeable-source-and-nuget-references) describes.

The channel types use the `Sphere10.Framework.Communications` namespace. The JSON-RPC and TCP endpoint types use `Sphere10.Framework.Communications.RPC`.

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

A service method runs on the thread of the connection that called it. `JsonRpcClientHandler.ClientContext` returns that connection's endpoint UID, so a method can tell its callers apart:

```csharp
using Sphere10.Framework.Communications.RPC;

[RpcAPIService("session")]
public class SessionService {
	[RpcAPIMethod]
	public string Caller() => $"Connection {JsonRpcClientHandler.ClientContext}";
}
```

### Naming rules

| Declaration | Registered name |
|---|---|
| `[RpcAPIService("math")]` on the class | The class's methods are prefixed with `math.` |
| No `[RpcAPIService]`, or an empty name | The methods join the anonymous service and are called without a prefix |
| `[RpcAPIMethod]` | The method's own name, such as `math.add` |
| `[RpcAPIMethod("mul")]` | The alias, such as `math.mul`. Repeat the attribute to register several names |
| `[RpcAPIArgument("first")]` on a parameter | `first` is recorded as the argument name in the method's `ApiMethodDescriptor` |

Service and method names are case-insensitive. Registering a second service under the same non-empty name throws, whereas anonymous services can be registered repeatedly to add methods. Public and non-public instance methods can be exposed.

This service renames its arguments and registers one method under two aliases:

```csharp
using Sphere10.Framework.Communications.RPC;

[RpcAPIService("strings")]
public class StringService {
	[RpcAPIMethod]
	public string Concat([RpcAPIArgument("first")] string text1, [RpcAPIArgument("second")] string text2) => text1 + text2;

	[RpcAPIMethod("explicit_args")]
	[RpcAPIMethod("args")]
	public void ExplicitArguments([RpcAPIArgument("arg1")] uint value) {
		// Registered as strings.explicit_args and strings.args
	}
}
```

The argument names are stored in lower case in the method's descriptor. Callers still pass the values by position. Here `client` is a `JsonRpcClient` that is connected to the server, as [JSON-RPC Client](#-json-rpc-client) shows:

```csharp
using System;
using Sphere10.Framework.Communications.RPC;

ApiServiceManager.RegisterService(new StringService());

var concat = ApiServiceManager.GetService("strings").GetMethod("strings.concat");
foreach (var (name, type) in concat.Arguments)
	Console.WriteLine($"{name}: {type.Name}"); // first: String, second: String

var joined = client.RemoteCall<string>("strings.concat", "ab", "cd"); // "abcd"
client.RemoteCall("strings.args", 7u);
```

### Service registry

`ApiServiceManager` is the process-wide registry that `JsonRpcServer` dispatches through. Each registered service is an `ApiService` that binds method names to the instance's methods. The registry finds a service by name or by a full method name, and the service can describe and call its methods in process:

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Sphere10.Framework.Communications.RPC;

var math = new MathService();
ApiServiceManager.RegisterService(math);

ApiService service = ApiServiceManager.GetServiceFromMethod("math.add"); // the "math" service
Console.WriteLine(service.IsApi(math));                                 // True
ApiMethodDescriptor add = service.GetMethod("math.add");
Console.WriteLine($"{add.MethodName} returns {add.ReturnType.Name}");    // math.add returns Int32

// Dispatch in process, as a client handler does with the parsed request arguments
JsonResponse response = service.Call("math.add", new List<object> { new JValue(5), new JValue(3) });
Console.WriteLine(response.Result);                                     // 8

ApiServiceManager.UnregisterService("math");
```

`Call` expects every argument as a Newtonsoft.Json `JToken`, and it returns error -8 for any other argument. A method name without a dot resolves to the anonymous service.

An `ApiService` can also hold bindings outside the registry. `JsonRpcServer` cannot reach such a service, but it can dispatch calls in process or check a service's bindings in a test:

```csharp
using Sphere10.Framework.Communications.RPC;

var math = new MathService();
var local = new ApiService();
local.AddApi(math);                    // binds math.add and math.mul to this instance
bool isBound = local.IsApi(math);      // true
bool removed = local.RemoveApi(math);  // true when bindings were removed
```

`AddApi` throws when a method name is already bound, and it does not take ownership of the instance.

### Arguments and types

Calls pass arguments by position in a JSON array. The server rejects named arguments in a JSON object with error -6, so `[RpcAPIArgument]` names do not affect dispatch. Arguments and results are converted with Newtonsoft.Json, and a `byte[]` argument travels as a hex string. Add `[JsonConverter(typeof(ByteArrayHexConverter))]` to the `byte[]` members of argument and result types so that both sides read them as hex:

```csharp
using System.Linq;
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

	[RpcAPIMethod]
	public Payload[] Repeat(Payload input, int count) => Enumerable.Repeat(input, count).ToArray();
}
```

The client passes and receives the same types. Objects arrive as JSON objects and arrays as JSON arrays, and `RemoteCall<T>` converts them back to `T`:

```csharp
var payload = new Payload { Count = 1, Name = "demo", Data = new byte[] { 0xDE, 0xAD } };
Payload echoed = client.RemoteCall<Payload>("objects.echo", payload);
Payload[] copies = client.RemoteCall<Payload[]>("objects.repeat", payload, 3);
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

A Stratum-style session uses `Persistant` mode on both sides of one connection. The server pushes calls through the `JsonRpcClientHandler` that `OnNewClient` passes to it. The client runs its own `JsonRpcClientHandler`, whose loop serves the server's calls and reads the results of the client's calls:

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Communications.RPC;

var config = new JsonRpcConfig {
	ConnectionMode = JsonRpcConfig.ConnectionModeEnum.Persistant,
	IgnoreEmptyReturnValue = true,
	Logger = new ConsoleLogger()
};

// Server process, with MathService registered
var server = new JsonRpcServer(new TcpEndPointListener(isLocal: true, port: 27002, maxListener: 5), config);
server.OnNewClient += handler => handler.RemoteCall("miner.notify", "job-1");
server.Start();

// Client process, with a "miner" service that has a Notify(string jobId) method registered
var client = new JsonRpcClientHandler(new TcpEndPoint("127.0.0.1", 27002), config);
client.Start();
var sum = client.RemoteCall<int>("math.add", 5, 3); // the client's own loop reads the result
client.Stop();
```

Each process dispatches the calls it receives through the `ApiServiceManager` registry of that process. Set `IgnoreEmptyReturnValue` in this mode when methods return nothing, because a response without a `result` member is not matched to its caller. A call whose result does not arrive within `MaxTimeWaitingForResult` throws a `JsonRpcException` with code -11.

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

### The IEndPoint contract

`JsonRpcServer`, `JsonRpcClient` and `JsonRpcClientHandler` work through this interface, so another message transport can implement it:

```csharp
namespace Sphere10.Framework.Communications.RPC;

public interface IEndPoint {
	string GetDescription();
	ulong GetUID();
	IEndPoint WaitForMessage();
	EndpointMessage ReadMessage();
	void WriteMessage(EndpointMessage message);
	bool IsOpened();
	void Start();
	void Stop();
}
```

| Member | `TcpEndPoint` | `TcpEndPointListener` |
|---|---|---|
| `GetDescription` | The remote `address:port` once connected | The local `address:port` |
| `GetUID` | A random identifier, which `JsonRpcClientHandler.ClientContext` reports | A random identifier |
| `WaitForMessage` | Returns the endpoint itself | Accepts a connection and returns its endpoint |
| `ReadMessage` and `WriteMessage` | Read or write one message, connecting first when needed | `ReadMessage` accepts a connection and reads from it. `WriteMessage` writes to the endpoint that the message's `Stream` names |
| `IsOpened` | Whether the socket is still connected | Whether the listener is bound |
| `Start` and `Stop` | Connect to the remote address when one was given, or close the socket | Start listening with the backlog, or stop |

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

Either side can exchange raw text lines instead of bytes. `TryWaitForString` completes with `true` when the next line that arrives equals its value. It completes with `false` when its token is cancelled, so pass a token with a timeout:

```csharp
using System;
using System.Threading;

pipe.ReceivedString += line => Console.WriteLine($"Received: {line}");
pipe.SentString += line => Console.WriteLine($"Sent: {line}");

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var ready = pipe.TryWaitForString("ready", timeout.Token); // subscribes before the child can answer
await pipe.TrySendString("start", timeout.Token);
if (!await ready)
	Console.WriteLine("The child did not answer with 'ready' in time");
```

When the next line has different text, `TryWaitForString` stops watching and completes only when its token is cancelled.

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

### WebSockets

A `ServerWebSocketsChannel` listens on its local endpoint, and its `Open` completes when one client has connected and finished the upgrade handshake. The server then sends the channel ID, which the client's `Open` reads into `Id`:

```csharp
using System;
using System.Net;
using System.Text;
using Sphere10.Framework.Communications;

var server = new ServerWebSocketsChannel(new IPEndPoint(IPAddress.Loopback, 8090), remoteEndpoint: null, secure: false);
server.ReceivedBytes += bytes => Console.WriteLine($"Server received: {Encoding.UTF8.GetString(bytes.Span)}");
var serverOpened = server.Open(); // completes when a client has connected

var client = new ClientWebSocketsChannel("ws://127.0.0.1:8090", secure: false);
await client.Open();
await serverOpened;
Console.WriteLine($"Channel ID: {client.Id}");
await client.TrySendBytes(Encoding.UTF8.GetBytes("Hello"));
await client.Close();
```

Both channels send every message as one WebSocket text frame. The `ReceivedWebSocketMessage` event reports each received frame, including the close frame that starts the closing handshake.

### Custom channels

A new transport derives from `ProtocolChannel` and implements its template methods. The base class runs the receive loop, which calls `ReceiveBytesInternal` until the channel closes, and it raises the events. This in-memory channel delivers each send to the receive loop of its peer:

```csharp
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sphere10.Framework.Communications;

public sealed class InMemoryChannel : ProtocolChannel {
	private readonly Channel<byte[]> _inbox = Channel.CreateUnbounded<byte[]>();
	private bool _connected;

	public InMemoryChannel(CommunicationRole role) {
		LocalRole = role;
	}

	public override CommunicationRole LocalRole { get; }

	public InMemoryChannel Peer { get; set; }

	public override bool IsConnectionAlive() => _connected;

	protected override Task OpenInternal() {
		_connected = true;
		return Task.CompletedTask;
	}

	protected override Task CloseInternal() {
		_connected = false;
		return Task.CompletedTask;
	}

	protected override async Task<byte[]> ReceiveBytesInternal(CancellationToken cancellationToken)
		=> await _inbox.Reader.ReadAsync(cancellationToken);

	protected override Task<bool> TrySendBytesInternal(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
		=> Task.FromResult(Peer is { _connected: true } && Peer._inbox.Writer.TryWrite(bytes.ToArray()));
}
```

Any channel is used through the same members and events:

```csharp
var a = new InMemoryChannel(CommunicationRole.Client);
var b = new InMemoryChannel(CommunicationRole.Server) { Peer = a };
a.Peer = b;
a.Opened += () => Console.WriteLine("a opened");
a.Closed += () => Console.WriteLine("a closed");
b.ReceivedBytes += bytes => Console.WriteLine($"b received {bytes.Length} bytes");

await a.Open();
await b.Open();
await a.TrySendBytes(new byte[] { 1, 2, 3 });
await a.Close();
await b.Close();
```

`TrySendBytes` returns `false` when `IsConnectionAlive` is `false` or the transport rejects the bytes. `Close` cancels a pending `ReceiveBytesInternal` after 150 milliseconds if the receive loop has not ended by then, and the channel raises `Closed` when the loop ends.

## 🛡️ Security Policies

`TcpSecurityPolicies` holds process-wide settings for the JSON-RPC server, and two of its checks are active:

- `ValidateConnectionCount` limits concurrent RPC connections to `MaxSimultanousConnecitons` (200 by default) and throws `TooManyConnectionsException` beyond it. A rejected connection does not count towards the limit, and `JsonRpcServer` closes its socket.
- `ValidateJsonQuality` rejects received bytes that contain control characters other than tab, line feed and carriage return. `TcpEndPoint.ReadMessage` applies it to every read.

A custom server on `TcpEndPointListener` can share the same connection limit. Every successful `ConnectionOpen` check needs a matching `ConnectionClose`:

```csharp
using Sphere10.Framework.Communications.RPC;

TcpSecurityPolicies.MaxSimultanousConnecitons = 50; // shared by every server in the process

var listener = new TcpEndPointListener(isLocal: true, port: 27003, maxListener: 5);
listener.Start();
IEndPoint client = listener.WaitForMessage();
try {
	TcpSecurityPolicies.ValidateConnectionCount(TcpSecurityPolicies.MaxConnecitonPolicy.ConnectionOpen);
} catch (TooManyConnectionsException) {
	client.Stop(); // a rejected connection holds no slot
	return;
}
try {
	var request = client.ReadMessage(); // ReadMessage has already applied ValidateJsonQuality
	client.WriteMessage(new EndpointMessage("OK"));
} finally {
	client.Stop();
	TcpSecurityPolicies.ValidateConnectionCount(TcpSecurityPolicies.MaxConnecitonPolicy.ConnectionClose);
}
```

Bytes read from another source can be checked directly. `ValidateJsonQuality` throws `IllegalValueException` from the `Sphere10.Framework` namespace:

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Communications.RPC;

var buffer = new byte[4096];
var bytesRead = stream.Read(buffer, 0, buffer.Length);
try {
	TcpSecurityPolicies.ValidateJsonQuality(buffer, bytesRead);
} catch (IllegalValueException) {
	// The bytes contain a control character other than tab, line feed or carriage return
}
```

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
