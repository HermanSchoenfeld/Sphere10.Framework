# Protocol

Message protocols of typed commands, request and response exchanges, two- and three-way handshakes and numbered modes, run by a `ProtocolOrchestrator` over a `ProtocolChannel` byte transport.

A `Protocol` holds a `ProtocolHandshake`, one or more `ProtocolMode` handler tables and a `SerializerFactory` for its message types. A mode maps message types to command handlers, request handlers, response handlers keyed by request and response type, and message generators. `ProtocolBuilder` composes a protocol with `WithHandshake`, `AddCommand`, `OnCommand`, `AddRequest`, `OnRequestResponse` and `SetMode`, and `Build()` validates that the handshake and every mode's handler tables are set and that every message type has a serializer, throwing `ProtocolBuilderException` otherwise. The `Action*` handler classes adapt delegates, and the `*HandlerBase` classes are the bases for handwritten handlers. All types here are in the `Sphere10.Framework.Communications` namespace.

`ProtocolChannel` is the abstract transport. Subclasses implement `OpenInternal`, `CloseInternal`, `ReceiveBytesInternal`, `TrySendBytesInternal` and `IsConnectionAlive`, and the base runs the receive loop and raises open, close and byte events. The TCP, UDP, WebSocket and pipe channels are in the [Communications project](../../Sphere10.Framework.Communications/README.md). `ProtocolOrchestrator` opens the channel when needed, performs the handshake, wraps each message in a `ProtocolMessageEnvelope` with its dispatch type and request ID, serializes envelopes with `ProtocolMessageEnvelopeSerializer`, and dispatches received messages to the active mode. `Start` waits for the handshake to finish without observing its cancellation token, and `RunToEnd` waits until the channel closes or the token is cancelled.

In the current code `ProtocolBuilder.BaseFactory` is never assigned, so `AddSerializers` and `AutoBuildSerializers` fail with a null reference, and the factory passed to the constructor is ignored. A new `Protocol` copies the registrations of `SerializerFactory.Default` when it is created. Serializer registration is described in [Serialization](../Serialization/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Acknowledge3WayHandshakeDelegate<THandshake, TAck, TVerack>`](HandshakeDelegates.cs) | delegate | Final step of a three-way handshake, where the initiator checks the verification acknowledgement. |
| [`Action2WayHandshakeHandler<THandshake, TAck>`](Action2WayHandshakeHandler.cs) | class | Two-way handshake handler built from generate, receive and verify delegates, with `Void` as its verification type. |
| [`Action3WayHandshakeHandler<THandshake, TAck, TVerack>`](Action3WayHandshakeHandler.cs) | class | Three-way handshake handler built from generate, receive, verify and acknowledge delegates. |
| [`ActionCommandHandler`](ActionCommandHandler.cs) | class | Untyped command handler that runs an `Action<ProtocolOrchestrator, object>`. |
| [`ActionCommandHandler<TMessage>`](ActionCommandHandlerT.cs) | class | Typed command handler that runs an `Action<ProtocolOrchestrator, TMessage>`. |
| [`ActionMessageGeneratorT<TMessage>`](ActionMessageGeneratorT.cs) | class | Message generator that creates a message with a `Func<ProtocolOrchestrator, TMessage>`. |
| [`ActionRequestHandler`](ActionRequestHandler.cs) | class | Untyped request handler that returns the response of a delegate. |
| [`ActionRequestHandler<TRequest, TResponse>`](ActionRequestHandlerT.cs) | class | Typed request handler that returns the response of a `Func<ProtocolOrchestrator, TRequest, TResponse>`. |
| [`ActionResponseHandler`](ActionResponseHandler.cs) | class | Untyped response handler that runs a delegate with the request and its response. |
| [`ActionResponseHandler<TRequest, TResponse>`](ActionResponseHandlerT.cs) | class | Typed response handler that runs an `Action<ProtocolOrchestrator, TRequest, TResponse>`. |
| [`CommandHandlerBase`](CommandHandlerBase.cs) | abstract class | Base command handler declaring its message type and an untyped `Execute`. |
| [`CommandHandlerBase<TMessage>`](CommandHandlerBaseT.cs) | abstract class | Typed command handler base that casts the message and calls the typed `Execute`. |
| [`CommunicationRole`](CommunicationRole.cs) | enum | Server or client side of a channel. |
| [`GenerateHandshakeDelegate<THandshake>`](HandshakeDelegates.cs) | delegate | Creates the initiator's handshake message. |
| [`HandshakeHandlerBase`](HandshakeHandlerBase.cs) | abstract class | Untyped handshake handler base with generate, receive, verify and acknowledge steps. |
| [`HandshakeHandlerBase<THandshake, TAck, TVerack>`](HandshakeHandlerBaseT.cs) | abstract class | Typed handshake handler base that seals the untyped steps and casts to the typed ones. |
| [`HandshakeOutcome`](HandshakeOutcome.cs) | enum | Result of a handshake step, which is accepted, too busy, report or block. |
| [`ICommandHandler`](ICommandHandler.cs) | interface | Executes a received command message of a declared type. |
| [`ICommandHandler<TMessage>`](ICommandHandlerT.cs) | interface | Typed command handler with a default untyped `Execute`. |
| [`IHandshakeHandler`](IHandshakeHandler.cs) | interface | Untyped generate, receive, verify and acknowledge steps of a handshake. |
| [`IHandshakeHandler<THandshake, TAck, TVerack>`](IHandshakeHandlerT.cs) | interface | Typed handshake steps with default untyped implementations. |
| [`IMessageGenerator`](IMessageGenerator.cs) | interface | Creates an outgoing message for an orchestrator. |
| [`IMessageGenerator<TMessage>`](IMessageGeneratorT.cs) | interface | Typed message generator with a default untyped `Execute`. |
| [`IRequestHandler`](IRequestHandler.cs) | interface | Answers a request of a declared type with a response of a declared type. |
| [`IRequestHandler<TRequest, TResponse>`](IRequestHandlerT.cs) | interface | Typed request handler with a default untyped `Execute`. |
| [`IResponseHandler`](IResponseHandler.cs) | interface | Handles a received response together with the request that caused it. |
| [`IResponseHandler<TRequest, TResponse>`](IResponseHandlerT.cs) | interface | Typed response handler with a default untyped `Execute`. |
| [`MessageGeneratorBase`](MessageGeneratorBase.cs) | abstract class | Base message generator with an untyped `Execute`. |
| [`MessageGeneratorBase<TMessage>`](MessageGeneratorBaseT.cs) | abstract class | Typed message generator base whose subclasses implement `ExecuteInternal`. |
| [`Protocol`](Protocol.cs) | class | Protocol definition of handshake, modes and serializer factory, with `AddMode` and `Validate`. |
| [`ProtocolChannel`](ProtocolChannel.cs) | abstract class | Disposable byte transport with open and close lifecycle, state, timeouts, a receive loop and byte events. |
| [`ProtocolChannelInitiator`](ProtocolChannelInitiator.cs) | enum | Whether the local or the remote end initiated a channel. |
| [`ProtocolChannelState`](ProtocolChannelState.cs) | enum | Channel opening, open, closing or closed. |
| [`ProtocolDispatchType`](ProtocolDispatchType.cs) | enum | Envelope kind, which is a command, request or response. |
| [`ProtocolException`](ProtocolException.cs) | class | `SoftwareException` for protocol runtime errors. |
| [`ProtocolHandshake`](ProtocolHandshake.cs) | class | Handshake definition with type, initiator, handler and the sync, ack and verack message types. |
| [`ProtocolHandshakeType`](ProtocolHandshakeType.cs) | enum | No handshake, two-way or three-way. |
| [`ProtocolMessageEnvelope`](ProtocolMessageEnvelope.cs) | class | Wire envelope carrying a dispatch type, request ID and message. |
| [`ProtocolMessageEnvelopeSerializer`](ProtocolMessageEnvelopeSerializer.cs) | class | Serializes an envelope as a marker, dispatch type, request ID, payload length and payload. |
| [`ProtocolMode`](ProtocolMode.cs) | class | Numbered table of request, response and command handlers and message generators, with `Validate`. |
| [`ProtocolOrchestrator`](ProtocolOrchestrator.cs) | class | Runs a protocol over a channel with the handshake, queued sending and receiving, dispatch to handlers, and state and message events. |
| [`ProtocolOrchestratorQueue`](ProtocolOrchestratorQueue.cs) | enum | Inbound or outbound queue, reported with message errors. |
| [`ProtocolOrchestratorState`](ProtocolOrchestratorState.cs) | enum | Orchestrator not started, handshaking, started or finished. |
| [`ReceiveHandshakeDelegate<THandshake, TAck>`](HandshakeDelegates.cs) | delegate | Responder step that inspects a handshake and returns an outcome and acknowledgement. |
| [`RequestHandlerBase`](RequestHandlerBase.cs) | abstract class | Base request handler declaring request and response types and an untyped `Execute`. |
| [`RequestHandlerBase<TRequest, TResponse>`](RequestHandlerBaseT.cs) | abstract class | Typed request handler base that seals the untyped `Execute`. |
| [`ResponseHandlerBase`](ResponseHandlerBase.cs) | abstract class | Base response handler declaring request and response types and an untyped `Execute`. |
| [`ResponseHandlerBase<TRequest, TResponse>`](ResponseHandlerBaseT.cs) | abstract class | Typed response handler base that seals the untyped `Execute`. |
| [`Verify2WayHandshakeDelegate<THandshake, TAck>`](HandshakeDelegates.cs) | delegate | Initiator step that verifies the acknowledgement of a two-way handshake. |
| [`Verify3WayHandshakeDelegate<THandshake, TAck, TVerack>`](HandshakeDelegates.cs) | delegate | Initiator step that verifies an acknowledgement and returns a verification acknowledgement. |

### Builder

| Type | Kind | Purpose |
|---|---|---|
| [`ProtocolBuilder`](Builder/ProtocolBuilder.cs) | class | Fluent composition of handshake, modes, commands and request and response pairs, ending in a validated `Build()`. |
| [`ProtocolBuilderException`](Builder/ProtocolException.cs) | class | `SoftwareException` thrown when a built protocol fails validation or a builder is misused. |
| [`ProtocolCommandBuilder<TMessage>`](Builder/ProtocolCommandBuilder.cs) | class | Configures the handler of one command type from a delegate or handler instance. |
| [`ProtocolHandshakeBuilder`](Builder/ProtocolHandshakeBuilder.cs) | class | Selects a two-way or three-way handshake and configures its delegates through nested builders. |
| [`ProtocolRequestBuilder<TRequest>`](Builder/ProtocolRequestBuilder.cs) | class | Configures the request handler and, through a nested `ResponseBuilder`, the response handler of one request type. |
