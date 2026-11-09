# Loader Services

Provides the in-memory endpoint manager and mock node data service that the loader pages and the tester's endpoint selector use.

`Sphere10Plugin` registers both as scoped services, so each browser circuit has its own endpoint list and selection. `DefaultEndpointManager` implements `IEndpointManager`. It starts with a fixed list of sample URIs, accepts only absolute HTTP or HTTPS URIs and raises `EndpointAdded` and `EndpointChanged`. `MockNodeService` implements `INodeService` with generated blocks. It follows endpoint changes, pages blocks on request and streams a new sample block every second until the token is cancelled. Neither service contacts a network endpoint. The [ViewModels](../ViewModels/README.md) and [Pages](../Pages/README.md) consume them.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DefaultEndpointManager`](DefaultEndpointManager.cs) | class | Per-circuit `IEndpointManager` with sample endpoints, URI validation, selection and added and changed events |
| [`MockNodeService`](MockNodeService.cs) | class | Disposable `INodeService` that pages generated blocks and streams one new sample block per second |
