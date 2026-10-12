---
name: protocols
description: Build application message protocols with Sphere10 Framework Protocol, ProtocolBuilder, and ProtocolOrchestrator over ProtocolChannel transports. Use for typed commands, request/response exchanges, handshakes, protocol modes, and their wire serialization.
---

# Application Protocols

Use the existing framework protocol stack to separate an application's message contract from transport and connection state. Types live in `Sphere10.Framework.Communications`, including those implemented in the core `Sphere10.Framework` project.

Before implementing an application against this checkout, read [current source limitations](references/current-source-limitations.md). The recipes express the intended architecture, but several paths in the present implementation are incomplete. Recheck the relevant source before adopting a workaround or assuming a limitation still exists.

## Structure

| Piece | Responsibility |
|---|---|
| `Protocol` | Shared definition: handshake, numbered modes, typed handler registries, and serializer factory. |
| `ProtocolBuilder` | Composes that definition through fluent registration and validates it with `Build()`. |
| `ProtocolOrchestrator` | One connection's handshake state, active mode, outgoing/incoming queues, and request correlation. |
| `ProtocolChannel` | Transport bytes, endpoint roles, open/close, and resource lifetime. Implementations include UDP, TCP, and WebSockets. |

Keep message DTOs and the protocol assembly method together in the application's communications layer. Prefer a small `AppProtocol.Build(...)` composition method, as in the recipes, with service-backed handlers supplied by the application. Build the same wire contract on both ends, while supplying each endpoint's local behavior. Create a fresh orchestrator for each connection; avoid connection state in a shared protocol's handler closures.

Read [builder-pattern](../builder-pattern/SKILL.md) when extending builders and [serialization](../serialization/SKILL.md) when defining the wire contract. Apply the repository's code style to C# examples and implementation.

## Register exchanges

Use a distinct DTO type per operation so handler lookup can distinguish messages. The framework dispatches by the message's concrete runtime type, with no inheritance fallback.

- `AddCommand<TMessage>(Handler)` registers a one-way command. Handler overloads accept no arguments, the message, or `(ProtocolOrchestrator, TMessage)`.
- `AddRequestResponse<TRequest, TResponse>(RequestHandler, ResponseHandler)` registers both sides of an exchange. The request handler returns the response; the response handler receives the original request and returned response. Orchestrator-aware overloads are available for both.
- `ConfigureCommand<TMessage>(Builder => Builder.HandleWith(Handler))` offers a nested builder.
- `ConfigureRequest<TRequest>(Builder => Builder.HandleRequestWith(RequestHandler).HandleResponseWith(ResponseHandler))` offers the request/response builder; use a request-taking handler overload for the typed response chain.
- For service classes, use `ICommandHandler<TMessage>`, `IRequestHandler<TRequest, TResponse>`, and `IResponseHandler<TRequest, TResponse>` or their typed `*HandlerBase` classes. When deriving from a base, implement its type properties as well as `Execute`.

This construction example uses already registered primitive types to demonstrate the current builder without custom serializer configuration:

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Communications;

var ProtocolDefinition = new ProtocolBuilder()
	.AddRequestResponse<string, int>(
		Request => Request.Length,
		(Request, Response) => SystemLog.Info($"Length of '{Request}': {Response}")
	)
	.AddCommand<byte[]>(Message => SystemLog.Info($"Received {Message.Length} bytes"))
	.Build();
```

For a domain protocol, replace primitives with operation DTOs after setting up the factory path described below. Register each command/request key only once per mode; response handlers are keyed by `(request type, response type)`. The builder requires request/response handlers to agree on both types.

Handlers use synchronous `Action`/`Func` contracts. Do not pass an `async` lambda to an `Action` overload or return a `Task` as a response DTO. The orchestrator schedules handlers on the thread pool, so handler completion and shared mutable state are not serialized by the incoming queue. Route stateful application work through its existing synchronization or serialized event-processing mechanism.

## Define serialization and modes

Use a protocol-owned `SerializerFactory` copied from `SerializerFactory.Default`. Register commands, request and response DTOs, every handshake DTO, and concrete polymorphic payload types. Use `RegisterAutoBuild<T>()` for suitable simple DTOs or explicit `IItemSerializer<T>` registrations when the wire layout needs control. `Protocol.Validate()` checks top-level serializer availability, not full compatibility between peers.

Both endpoints need matching type codes, member layouts, and endianness. Use a shared deterministic registration routine; for a versioned wire contract, assign stable type codes and explicit serializers where appropriate. `SetMinTypeCode(...)` establishes a starting range, not a complete compatibility strategy. The orchestrator currently uses little-endian envelope serialization. Follow the serialization skill when defining serializers; do not introduce `System.BitConverter`.

`ConfigureSerialization(...)` and `AutoBuildSerializers()` are the intended builder entry points, but their factory wiring is broken in this checkout. Use the documented direct `Protocol` construction alternative or repair that wiring when it is part of the application task. Do not mutate `SerializerFactory.Default` to make `Build()` pass.

Mode 0 is the initial application dispatch mode. The orchestrator distinguishes handshaking with its own state and remains in mode 0 after the handshake. Register mode 0 handlers before calling `SetMode(1)`; additional modes must be added consecutively. `SetMode(0)` is rejected by the builder despite mode 0 being its initial target.

At runtime, change `Orchestrator.ActiveMode` only to an existing index (`0 <= Mode < Protocol.Modes.Length`). Modes are local handler selections: envelopes contain no mode number and there is no automatic peer negotiation. Coordinate changes explicitly on both ends, and complete pending exchanges first because responses resolve handlers in the mode active when they arrive.

## Handshake and lifetime

Leave `Protocol.Handshake.Type` as `None` when no handshake is needed. The intended fluent handshake is `ConfigureHandshake(Builder => Builder.UseTwoWay().InitiatedBy(CommunicationRole.Client).HandleWith<Sync, Ack>(Generate, Receive, Verify))`; `UseThreeWay()` adds a `Verack` message and acknowledgement callback. The initiator is selected by the channel's `LocalRole`.

The delegate sequence is:

1. Initiator generates `Sync` from the orchestrator.
2. Receiver handles `Sync`, returns `HandshakeOutcome`, and assigns `out Ack`.
3. Initiator verifies `Sync` and `Ack`, returning `HandshakeOutcome`; a three-way verifier also assigns `out Verack`.
4. For three-way handshakes, the receiver's acknowledgement callback receives all three messages and returns `bool`.

Only `HandshakeOutcome.Accepted` advances successfully. Register the handshake message types in order: Sync, Ack, then Verack when used. A handshake is an application negotiation mechanism; put any required identity/version checks in these handlers. Recheck the three-way defects in the source-status reference before using it.

Construct the channel, build the protocol, then construct `new ProtocolOrchestrator(Channel, ProtocolDefinition) { Logger = Logger }`. Attach `MessageError`, `StateChanged`, and any useful message diagnostics before startup. Queue failures and handler failures have different reporting paths; supply an `ILogger` as well as observing `MessageError`.

Await `Start()` before sending application traffic. It opens the channel when necessary and waits for any configured handshake. Start both endpoints concurrently for a handshake; awaiting one side before starting the other can stall. Use `SendMessage(ProtocolDispatchType.Request, Request)` or `SendMessage(ProtocolDispatchType.Command, Command)`. Sending enqueues work and returns `void`; it neither awaits transmission nor returns a response task. Request handlers produce response envelopes automatically with the original request ID.

Keep the connection alive until the application has observed the response/completion it needs, then call `Finish()`. Own the channel with a disposable scope (`using`/`await using` as appropriate to the concrete channel); the orchestrator itself is not disposable. `RunToEnd(CancellationToken)` waits for finish/closure and supports application cancellation, but UDP cannot detect a remote endpoint closing. Give UDP applications an explicit lifetime or shutdown condition. Recheck startup cancellation behavior in the source-status reference.

## Verify the application path

For protocol implementation changes, use focused tests for message/envelope round trips, peer serializer compatibility, the chosen handshake's success/rejection paths, command delivery, original-request response correlation, coordinated mode changes, and bounded startup/shutdown as relevant. Exercise the actual selected channel's framing; a socket read is not automatically a complete message. See [unit-testing](../unit-testing/SKILL.md) before writing tests.

## Source examples

Paths below are relative to the repository root:

- `recipes/AbstractProtocol.AnonymousPipeComplex/AppProtocol.cs`: intended fluent composition, nested builders, handshake delegates, and typed handlers. Treat it as an architectural example, subject to the current source limitations.
- `recipes/AbstractProtocol.UPDSimple/AppProtocol.cs` and `Program.cs`: endpoint roles, orchestrator events, sending, and explicit UDP shutdown.
- `src/Sphere10.Framework/Protocol/`: protocol definitions, builders, handlers, envelope, and orchestrator.
- `src/Sphere10.Framework/Serialization/Factory/SerializerFactory.cs`: registration and type-code behavior.
- `src/Sphere10.Framework.Communications/`: concrete transport implementations.
