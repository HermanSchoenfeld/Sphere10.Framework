# 📡 Sphere10.Framework.Communications

<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

**Multi-protocol networking and RPC framework** providing JSON-RPC servers/clients, anonymous pipes, TCP endpoints, WebSockets, and protocol channel abstractions.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Communications
```

## 🏗️ Core Architecture

### IEndPoint Interface

The `IEndPoint` interface abstracts communication endpoints for message-based protocols:

```csharp
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

**Implementations:**
- `TcpEndPoint` - TCP socket communication
- `TcpEndPointListener` - TCP server that accepts connections
- `AnonymousPipeEndpoint` - Inter-process pipe communication

### JSON-RPC Framework

The RPC system uses attributes to define remote-callable services:

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[RpcAPIService("name")]` | Class | Defines a service namespace |
| `[RpcAPIMethod]` | Method | Marks method as remotely callable |
| `[RpcAPIMethod("alias")]` | Method | Marks method with custom RPC name |
| `[RpcAPIArgument("name")]` | Parameter | Maps parameter to JSON field name |

## 🔧 JSON-RPC Server

Create a JSON-RPC server with TCP endpoint:

```csharp
using Sphere10.Framework.Communications.RPC;

// Define service
[RpcAPIService("math")]
public class MathService {
    [RpcAPIMethod]
    public int Add(int a, int b) => a + b;
    
    [RpcAPIMethod]
    public uint AddUnsigned(uint a, uint b) => a + b;
    
    [RpcAPIMethod]
    public float Multiply(float a, float b) => a * b;
}

// Create server
var endpoint = new TcpEndPointListener(port: 8080);
var config = new JsonRpcConfig();
var server = new JsonRpcServer(endpoint, config);

// Register services
var mathService = new MathService();
config.ApiService.AddApi(mathService);

// Start server
server.Start();

// Handle new clients
server.OnNewClient += (handler) => {
    Console.WriteLine($"Client connected: {handler}");
};
```

### JsonRpcConfig

Configuration for JSON-RPC servers and clients:

```csharp
var config = new JsonRpcConfig {
    ConnectionMode = JsonRpcConfig.ConnectionModeEnum.Persistent,  // or Pulsed
    Logger = new ConsoleLogger()
};
```

## 🔧 JSON-RPC Client

Connect to a JSON-RPC server:

```csharp
using Sphere10.Framework.Communications.RPC;

// Connect to server
var endpoint = new TcpEndPoint("localhost", 8080);
var config = new JsonRpcConfig();
var client = new JsonRpcClient(endpoint, config);

// Make RPC calls using batch descriptor
var batch = new ApiBatchCallDescriptor();
batch.AddCall<int>("math.add", new { a = 5, b = 3 });
batch.AddCall<float>("math.multiply", new { a = 2.5f, b = 4.0f });

object[] results = client.RemoteCall(batch);
int sum = (int)results[0];        // 8
float product = (float)results[1]; // 10.0
```

## 🔧 ApiService - Service Registration

The `ApiService` class manages RPC method bindings:

```csharp
var apiService = new ApiService();

// Add service instance
var mathService = new MathService();
apiService.AddApi(mathService);

// Remove service
apiService.RemoveApi(mathService);

// Check if instance is registered
bool isRegistered = apiService.IsApi(mathService);
```

### Service with Custom Parameter Names

```csharp
[RpcAPIService("strings")]
public class StringService {
    [RpcAPIMethod]
    public string Concat(
        [RpcAPIArgument("first")] string text1,
        [RpcAPIArgument("second")] string text2) 
        => text1 + text2;
    
    [RpcAPIMethod("explicit_args")]
    public void ExplicitArguments([RpcAPIArgument("arg1")] uint value) {
        // Custom method name and parameter name
    }
}
```

### Complex Types and Collections

```csharp
public class TestObject {
    public int iVal;
    public string sVal;
    public float[] fArray;
    [JsonConverter(typeof(ByteArrayHexConverter))]
    public byte[] bytesArray;  // Serialized as hex string
}

[RpcAPIService("objects")]
public class ObjectService {
    [RpcAPIMethod]
    public TestObject GetTestObject(TestObject input) {
        return new TestObject {
            iVal = input.iVal + 1,
            sVal = input.sVal + "_processed"
        };
    }
    
    [RpcAPIMethod]
    public TestObject[] GetTestObjectArray(TestObject input) {
        return new[] { input, input };
    }
}
```

## 🔗 Anonymous Pipes

For inter-process communication:

```csharp
using Sphere10.Framework.Communications;

// Server side - spawn child process with pipe handles
var serverPipe = AnonymousPipe.ToChildProcess(
    processPath: "child.exe",
    arguments: "",
    argInjectorFunc: (args, readHandle, writeHandle) => 
        $"{args} --read={readHandle} --write={writeHandle}"
);

await serverPipe.Open();
await serverPipe.TrySendString("Hello from parent");
var response = await serverPipe.ReceiveString(CancellationToken.None);

// Client side - connect using handles from arguments
var clientPipe = AnonymousPipe.FromChildProcess(
    new AnonymousPipeEndpoint(readHandle, writeHandle)
);

await clientPipe.Open();
var message = await clientPipe.ReceiveString(CancellationToken.None);
await clientPipe.TrySendString("Hello from child");
```

### AnonymousPipe Events

```csharp
pipe.ReceivedString += (message) => Console.WriteLine($"Received: {message}");
pipe.SentString += (message) => Console.WriteLine($"Sent: {message}");
```

## 🔗 TCP Endpoints

### TcpEndPoint - Client Connection

```csharp
using Sphere10.Framework.Communications.RPC;

var endpoint = new TcpEndPoint("192.168.1.100", 8080);
endpoint.MaxMessageSize = 8192;  // Default: 4096

endpoint.Start();

// Send message
endpoint.WriteMessage(new EndpointMessage("Hello Server"));

// Receive message
var response = endpoint.ReadMessage();
Console.WriteLine(response.ToSafeString());

endpoint.Stop();
```

### TcpEndPointListener - Server

```csharp
var listener = new TcpEndPointListener(8080);
listener.Start();

// Wait for client connection
IEndPoint clientEndpoint = listener.WaitForMessage();
Console.WriteLine($"Client connected: {clientEndpoint.GetDescription()}");

// Communicate with client
var message = clientEndpoint.ReadMessage();
clientEndpoint.WriteMessage(new EndpointMessage("Response"));
```

## 🔗 Protocol Channels

Abstract base for bidirectional communication:

```csharp
using Sphere10.Framework.Communications;

public abstract class ProtocolChannel {
    public ProtocolChannelState State { get; }
    public CommunicationRole Initiator { get; }
    
    public Task Open();
    public Task Close();
    public bool IsConnectionAlive();
    
    public Task<bool> TrySendBytes(ReadOnlyMemory<byte> bytes, CancellationToken ct);
    public Task<byte[]> ReceiveBytes(CancellationToken ct);
}
```

**States:** `Closed`, `Opening`, `Open`, `Closing`

## 🛡️ Security Policies

The `TcpSecurityPolicies` class provides connection-level security:

```csharp
// Validate connection count
TcpSecurityPolicies.ValidateConnectionCount(
    TcpSecurityPolicies.MaxConnecitonPolicy.ConnectionOpen);

// Monitor for potential attacks
TcpSecurityPolicies.MonitorPotentialAttack(
    TcpSecurityPolicies.AttackType.ConnectionFlod, 
    clientEndpoint);

// Validate JSON message quality
TcpSecurityPolicies.ValidateJsonQuality(messageBytes, bytesRead);
```

## 📁 Project Structure

| Directory | Description |
|-----------|-------------|
| `RPC/` | JSON-RPC server, client, attributes, service management |
| `EndPoint/` | IEndPoint interface and TCP implementations |
| `Pipes/` | Anonymous pipe communication |
| `TCP/` | TCP channel implementation |
| `UDP/` | UDP communication |
| `WebSockets/` | WebSocket protocol support |
| `DataSource/` | Data source abstractions for protocols |

## ✅ Best Practices

- **Stateless Services** - Design RPC services as stateless for scalability
- **Exception Handling** - Exceptions are serialized as JSON-RPC error responses
- **Thread Safety** - RPC service methods should be thread-safe
- **Connection Mode** - Use `Persistent` for frequent calls, `Pulsed` for occasional
- **Message Size** - Configure `MaxMessageSize` for your payload requirements

## ⚖️ License

Distributed under the **MIT NON-AI License**.

See the LICENSE file for full details. More information: [Sphere10 NON-AI-MIT License](https://sphere10.com/legal/NON-AI-MIT)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

