# JSON-RPC

Provides an attribute-driven JSON-RPC 2.0 server, client and service registry that run over the TCP message endpoints.

Mark a class with `[RpcAPIService("name")]` and its callable methods with `[RpcAPIMethod]`, then register an instance with the static `ApiServiceManager.RegisterService`. `JsonRpcServer` accepts connections from a `TcpEndPointListener` on its own thread and gives each client a `JsonRpcClientHandler`, which resolves calls through `ApiServiceManager` and dispatches them by name with positional arguments. `JsonRpcClient` sends single calls with `RemoteCall` or `RemoteCall<T>` and batches with an `ApiBatchCallDescriptor`. `JsonRpcConfig` chooses pulsed or persistent connections and how long a caller waits for a result. The [project README](../README.md) documents the naming rules, error codes and wire format.

The registry is process-wide, so register services before clients connect. Handlers run calls concurrently, so service methods must be thread-safe. The transport has no authentication or encryption. Endpoints live in [EndPoint](../EndPoint/README.md), and `byte[]` values travel as hex through [`ByteArrayHexConverter`](../JSON/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ApiBatchCallDescriptor`](ApiBatchCallDescriptor.cs) | class | Collects method calls with their expected return types for one batch `RemoteCall` |
| [`ApiDescriptor`](ApiDescriptor.cs) | class | Holder for an API name and its method descriptors, with protected fields only and no current use |
| [`ApiMethodDescriptor`](ApiMethodDescriptor.cs) | class | Registered method name, argument names and types, return type and the bound delegate that executes it |
| [`ApiRemoteService`](ApiRemoteService.cs) | abstract class | Client-side service base over an `IEndPoint` with abstract single, typed and batch `RemoteCall` methods |
| [`ApiService`](ApiService.cs) | class | Binds an object's `[RpcAPIMethod]` methods by lower-case name and invokes them from JSON arguments, returning a `JsonResponse` |
| [`ApiServiceManager`](ApiServiceManager.cs) | class | Static, lock-protected registry of services keyed by service name that resolves a `service.method` call to its service |
| [`ApiServiceProxy`](ApiServiceProxy.cs) | class | `ApiService` that wraps a registered object which does not derive from `ApiService` |
| [`JsonRequest`](JsonRequest.cs) | class | JSON-RPC 2.0 request envelope with `method`, `params` and `id` |
| [`JsonResponse`](JsonResult.cs) | class | JSON-RPC 2.0 response envelope with `result`, `error` and `id` |
| [`JsonRpcClient`](JsonRpcClient.cs) | class | `ApiRemoteService` that serializes calls and batches as JSON-RPC over an `IEndPoint` and converts the results |
| [`JsonRpcClientHandler`](JsonRpcClientHandler.cs) | class | Server-side per-connection handler that reads requests on a task, executes them and writes responses, with `OnStart` and `OnStop` events |
| [`JsonRpcConfig`](JsonRpcConfig.cs) | class | Connection mode (`Pulsed` or `Persistant`), empty-result handling, result timeout and optional logger |
| [`JsonRpcException`](JsonApiException.cs) | class | JSON-RPC error with a numeric `code` and a `message`, serialized into error responses |
| [`JsonRpcServer`](JsonRpcServer.cs) | class | Accept loop on a dedicated thread that enforces the connection limit and starts a `JsonRpcClientHandler` per client |
| [`RpcAPIArgumentAttribute`](ApiAttributes.cs) | class | Parameter attribute that records an explicit argument name in the method descriptor |
| [`RpcAPIMethodAttribute`](ApiAttributes.cs) | class | Method attribute that exposes a method under its own name or an alias, and may repeat |
| [`RpcAPIServiceAttribute`](ApiAttributes.cs) | class | Class attribute that sets the service name used as the method prefix |
