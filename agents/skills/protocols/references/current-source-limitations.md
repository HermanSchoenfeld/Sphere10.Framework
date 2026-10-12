# Current source limitations

These observations describe the source inspected when this skill was extracted. Recheck the named methods in the target checkout; they are implementation defects or missing guarantees, not the intended design. Do not copy a recipe and claim that its network exchange works without testing it. Repair only the paths needed by the authorized task.

All source paths below are relative to the repository root.

## Builder and factory

In `src/Sphere10.Framework/Protocol/Builder/ProtocolBuilder.cs`, the constructor validates `baseFactory` but never assigns `BaseFactory` or copies its registrations to `_protocol.Factory`. Consequently, `ConfigureSerialization` passes null to its callback, `AutoBuildSerializers` dereferences null when processing message types, and supplying a custom constructor factory does not configure the built protocol. `Build()` validates against `_protocol.Factory`, so it cannot be followed by a custom-serializer fix if missing serializers already cause it to fail.

When factory wiring cannot be changed in the current task, direct construction is an available API-level alternative:

```csharp
var ProtocolDefinition = new Protocol();
ProtocolDefinition.Factory.RegisterAutoBuild<RequestStatus>();
ProtocolDefinition.Factory.RegisterAutoBuild<StatusResult>();
ProtocolDefinition.Modes[0].RequestHandlers.Add(
	typeof(RequestStatus),
	new ActionRequestHandler<RequestStatus, StatusResult>(HandleRequest)
);
ProtocolDefinition.Modes[0].ResponseHandlers.Add(
	typeof(RequestStatus),
	typeof(StatusResult),
	new ActionResponseHandler<RequestStatus, StatusResult>(HandleResponse)
);
var Validation = ProtocolDefinition.Validate();
Guard.Ensure(Validation.IsSuccess, "Invalid application protocol");
```

`RequestStatus`, `StatusResult`, and the handler methods are application-defined. Include `using Sphere10.Framework;` and `using Sphere10.Framework.Communications;`. This bypasses builder factory wiring; it does not fix the transport/envelope issues below.

Even after wiring the factory, `AutoBuildSerializers()` enumerates modes only and omits `Protocol.Handshake.MessageTypes`. Register handshake types explicitly. `Protocol.Validate()` does not fully validate handshake configuration.

## Three-way handshake

In `Builder/ProtocolHandshakeBuilder.cs`, `ThreeWayHandshakeBuilder.HandleWith(IHandshakeHandler, ...)` fails to assign `_handler`; `Build()` therefore throws. In `Action3WayHandshakeHandler.cs`, the three-delegate constructor supplies a null fourth callback to a constructor that rejects null. Use the explicit fourth callback when constructing that handler directly.

In `ProtocolOrchestrator.AdvanceHandshakeStep`, the receiver does not assign the arriving Verack message to `_handshakeVerack` before calling `AcknowledgeHandshake`. Directly constructing `ProtocolHandshake` can bypass the builder defect but does not correct this runtime behavior.

## Envelope wire format

In `src/Sphere10.Framework/Protocol/ProtocolMessageEnvelopeSerializer.cs`, `Serialize` writes the `long` returned by payload `CalculateSize`, while `Deserialize` reads an `Int32` length and computes a header with an `Int32` length slot. `CalculateSize` also omits dispatch, request ID, and length fields. Resolve the width/size mismatch and verify envelope round trips before relying on transport tests. Preserve an established wire format if compatibility with existing peers is required.

## Orchestrator guarantees

In `src/Sphere10.Framework/Protocol/ProtocolOrchestrator.cs`:

- `TryStart(CancellationToken)` does not use its token for opening or waiting on `_handshakeFinishedTrigger`; the `Start(TimeSpan)` overload alone does not bound a stalled handshake. Add a tested bounded startup/cleanup path when the application requires one.
- Outstanding requests are registered after sending, are not removed after a response, and have no expiry. This leaves a fast-response race and unbounded retention. There is no built-in per-request timeout, duplicate suppression, or exactly-once processing guarantee.
- Command, request, and response handlers run on separate thread-pool work items. They may overlap even though envelope processing uses a queue.
- `ProtocolMessageEnvelope` carries dispatch type, request ID, and message only. Active mode is local state; it is not carried on the wire.

The recipes are useful for extracting the application's structure. They do not establish that cancellation, correlation, framing, or three-way handshakes are reliable in this checkout.
