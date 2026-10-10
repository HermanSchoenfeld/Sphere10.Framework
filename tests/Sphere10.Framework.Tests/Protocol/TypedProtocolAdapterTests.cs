// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Communications;

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class TypedProtocolAdapterTests {
	private TestChannel _channel;
	private ProtocolOrchestrator _orchestrator;

	[SetUp]
	public void SetUp() {
		_channel = new TestChannel();
		_orchestrator = new ProtocolOrchestrator(_channel, new Protocol());
	}

	[TearDown]
	public void TearDown() => _channel.Dispose();

	[Test]
	public void CommandDispatchUsesTypedOperation([Values(false, true)] bool useBaseClass) {
		string receivedCommand = null;
		ProtocolOrchestrator receivedOrchestrator = null;
		var handler = CreateCommandHandler(useBaseClass, (orchestrator, command) => {
			receivedOrchestrator = orchestrator;
			receivedCommand = command;
		});
		var untyped = (ICommandHandler)handler;

		untyped.Execute(_orchestrator, "command");

		Assert.That(receivedCommand, Is.EqualTo("command"));
		Assert.That(receivedOrchestrator, Is.SameAs(_orchestrator));
	}

	[Test]
	public void CommandRejectsWrongTypeAndNullBeforeCallingHandler([Values(false, true)] bool useBaseClass, [Values(null, 7)] object command) {
		var called = false;
		var handler = (ICommandHandler)CreateCommandHandler(useBaseClass, (_, _) => called = true);

		Assert.That(() => handler.Execute(_orchestrator, command), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("command"));
		Assert.That(called, Is.False);
	}

	[Test]
	public void RequestDispatchBoxesTypedResult([Values(false, true)] bool useBaseClass) {
		var handler = CreateRequestHandler(useBaseClass, (orchestrator, request) => {
			Assert.That(orchestrator, Is.SameAs(_orchestrator));
			return request.Length;
		});
		var untyped = (IRequestHandler)handler;

		var response = untyped.Execute(_orchestrator, "request");

		Assert.That(response, Is.TypeOf<int>().And.EqualTo(7));
	}

	[Test]
	public void RequestRejectsWrongTypeAndNullBeforeCallingHandler([Values(false, true)] bool useBaseClass, [Values(null, 7)] object request) {
		var called = false;
		var handler = (IRequestHandler)CreateRequestHandler(useBaseClass, (_, _) => {
			called = true;
			return 0;
		});

		Assert.That(() => handler.Execute(_orchestrator, request), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("request"));
		Assert.That(called, Is.False);
	}

	[Test]
	public void ResponseDispatchUnboxesResponse([Values(false, true)] bool useBaseClass) {
		(string Request, int Response) received = default;
		var handler = CreateResponseHandler(useBaseClass, (orchestrator, request, response) => {
			Assert.That(orchestrator, Is.SameAs(_orchestrator));
			received = (request, response);
		});
		var untyped = (IResponseHandler)handler;

		untyped.Execute(_orchestrator, "request", 11);

		Assert.That(received, Is.EqualTo(("request", 11)));
	}

	[TestCase(false, 7, 11, "request")]
	[TestCase(true, 7, 11, "request")]
	[TestCase(false, null, 11, "request")]
	[TestCase(true, null, 11, "request")]
	[TestCase(false, "request", "wrong", "response")]
	[TestCase(true, "request", "wrong", "response")]
	[TestCase(false, "request", null, "response")]
	[TestCase(true, "request", null, "response")]
	public void ResponseRejectsInvalidArgumentsBeforeCallingHandler(bool useBaseClass, object request, object response, string parameterName) {
		var called = false;
		var handler = (IResponseHandler)CreateResponseHandler(useBaseClass, (_, _, _) => called = true);

		Assert.That(() => handler.Execute(_orchestrator, request, response), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo(parameterName));
		Assert.That(called, Is.False);
	}

	[Test]
	public void HandshakeDispatchPreservesOutValuesAndOutcome([Values(false, true)] bool useBaseClass) {
		var handler = CreateHandshakeHandler(useBaseClass);
		var untyped = (IHandshakeHandler)handler;

		var handshake = untyped.GenerateHandshake(_orchestrator);
		var received = untyped.ReceiveHandshake(_orchestrator, handshake, out var acknowledgement);
		var verified = untyped.VerifyHandshake(_orchestrator, handshake, acknowledgement, out var verifyAcknowledgement);
		var acknowledged = untyped.AcknowledgeHandshake(_orchestrator, handshake, acknowledgement, verifyAcknowledgement);

		Assert.That(handshake, Is.EqualTo("handshake"));
		Assert.That(acknowledgement, Is.TypeOf<int>().And.EqualTo(9));
		Assert.That(verifyAcknowledgement, Is.TypeOf<long>().And.EqualTo(10L));
		Assert.That(received, Is.EqualTo(HandshakeOutcome.Accepted));
		Assert.That(verified, Is.EqualTo(HandshakeOutcome.TooBusy));
		Assert.That(acknowledged, Is.True);
	}

	[Test]
	public void HandshakePreservesRawCastFailures([Values(false, true)] bool useBaseClass) {
		var handler = (IHandshakeHandler)CreateHandshakeHandler(useBaseClass);

		Assert.That(() => handler.ReceiveHandshake(_orchestrator, 7, out _), Throws.TypeOf<InvalidCastException>());
		Assert.That(() => handler.VerifyHandshake(_orchestrator, "handshake", "wrong", out _), Throws.TypeOf<InvalidCastException>());
		Assert.That(() => handler.AcknowledgeHandshake(_orchestrator, "handshake", 9, 10), Throws.TypeOf<InvalidCastException>());
		Assert.That(() => handler.VerifyHandshake(_orchestrator, "handshake", null, out _), Throws.TypeOf<NullReferenceException>());
	}

	[Test]
	public void HandshakeAllowsNullReferenceMessage([Values(false, true)] bool useBaseClass) {
		var handler = (IHandshakeHandler)CreateHandshakeHandler(useBaseClass);

		var outcome = handler.ReceiveHandshake(_orchestrator, null, out var acknowledgement);

		Assert.That(outcome, Is.EqualTo(HandshakeOutcome.Accepted));
		Assert.That(acknowledgement, Is.EqualTo(0));
	}

	[Test]
	public void GeneratorDispatchBoxesTypedValueAndPreservesHook([Values(false, true)] bool useBaseClass) {
		var handler = (IMessageGenerator)CreateGenerator(useBaseClass);

		Assert.That(handler.Execute(_orchestrator), Is.TypeOf<int>().And.EqualTo(17));
	}

	[Test]
	public void GeneratorRejectsNullOrchestrator([Values(false, true)] bool useBaseClass) {
		var handler = (IMessageGenerator)CreateGenerator(useBaseClass);

		Assert.That(() => handler.Execute(null), Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("orchestrator"));
	}

	[Test]
	public void PublicBaseClassApisRetainUntypedDispatchAndMetadata() {
		var command = new ActionCommandHandler<string>((_, _) => { });
		var request = new ActionRequestHandler<string, int>((_, value) => value.Length);
		var response = new ActionResponseHandler<string, int>((_, _, _) => { });
		var handshake = new BaseHandshakeHandler();
		var generator = new BaseGenerator();

		Assert.That(command.MessageType, Is.EqualTo(typeof(string)));
		Assert.That(request.RequestType, Is.EqualTo(typeof(string)));
		Assert.That(request.ResponseType, Is.EqualTo(typeof(int)));
		Assert.That(response.RequestType, Is.EqualTo(typeof(string)));
		Assert.That(response.ResponseType, Is.EqualTo(typeof(int)));
		Assert.That(((RequestHandlerBase)request).Execute(_orchestrator, "request"), Is.EqualTo(7));
		Assert.That(handshake.GenerateHandshake(_orchestrator), Is.EqualTo("handshake"));
		Assert.That(generator.Execute(_orchestrator), Is.EqualTo(17));
	}

	[Test]
	public void ClassOverridesStillTakePrecedenceOverInterfaceDefaults() {
		var handler = new OverriddenCommandHandler();
		var untyped = (ICommandHandler)handler;

		untyped.Execute(_orchestrator, 7);

		Assert.That(handler.UntypedCommand, Is.EqualTo(7));
		Assert.That(untyped.MessageType, Is.EqualTo(typeof(object)));
	}

	[Test]
	public void PublicClassAdaptersRetainVirtualDispatchWhenTypedInterfacesAreReimplemented() {
		var command = new RemappedCommandHandler();
		var request = new RemappedRequestHandler();
		var response = new RemappedResponseHandler();

		((ICommandHandler<string>)command).Execute(_orchestrator, "command");
		Assert.That(command.Received, Is.EqualTo("interface"));
		((ICommandHandler)command).Execute(_orchestrator, "command");
		Assert.That(command.Received, Is.EqualTo("class"));

		Assert.That(((IRequestHandler<string, int>)request).Execute(_orchestrator, "request"), Is.EqualTo(-1));
		Assert.That(((IRequestHandler)request).Execute(_orchestrator, "request"), Is.EqualTo(7));

		((IResponseHandler<string, int>)response).Execute(_orchestrator, "request", 7);
		Assert.That(response.Received, Is.EqualTo("interface"));
		((IResponseHandler)response).Execute(_orchestrator, "request", 7);
		Assert.That(response.Received, Is.EqualTo("class"));
	}

	[Test]
	public void PublicHandshakeAdaptersRetainVirtualDispatchWhenTypedInterfaceIsReimplemented() {
		var handler = new RemappedHandshakeHandler();
		var typed = (IHandshakeHandler<string, int, long>)handler;
		var untyped = (IHandshakeHandler)handler;

		Assert.That(typed.ReceiveHandshake(_orchestrator, "handshake", out var typedAcknowledgement), Is.EqualTo(HandshakeOutcome.TooBusy));
		Assert.That(typedAcknowledgement, Is.EqualTo(-1));
		Assert.That(typed.VerifyHandshake(_orchestrator, "handshake", 9, out var typedVerifyAcknowledgement), Is.EqualTo(HandshakeOutcome.Accepted));
		Assert.That(typedVerifyAcknowledgement, Is.EqualTo(-2L));
		Assert.That(typed.AcknowledgeHandshake(_orchestrator, "handshake", 9, 10L), Is.False);

		Assert.That(untyped.ReceiveHandshake(_orchestrator, "handshake", out var acknowledgement), Is.EqualTo(HandshakeOutcome.Accepted));
		Assert.That(acknowledgement, Is.EqualTo(9));
		Assert.That(untyped.VerifyHandshake(_orchestrator, "handshake", 9, out var verifyAcknowledgement), Is.EqualTo(HandshakeOutcome.TooBusy));
		Assert.That(verifyAcknowledgement, Is.EqualTo(10L));
		Assert.That(untyped.AcknowledgeHandshake(_orchestrator, "handshake", 9, 10L), Is.True);
		Assert.That(untyped.GenerateHandshake(_orchestrator), Is.EqualTo("remapped"));
	}

	[Test]
	public void ProtocolBuilderRegistersAndDispatchesDirectTypedHandlers() {
		string receivedCommand = null;
		var receivedResponse = 0;
		var commandHandler = new DirectCommandHandler((_, command) => receivedCommand = command);
		var requestHandler = new DirectRequestHandler((_, request) => request.Length);
		var responseHandler = new DirectResponseHandler((_, _, response) => receivedResponse = response);
		var protocol = new ProtocolBuilder()
			.ConfigureCommand<string>(command => command.HandleWith(commandHandler))
			.ConfigureRequest<string>(request => request
				.HandleRequestWith(requestHandler)
				.HandleResponseWith(responseHandler)
			)
			.Build();

		var mode = protocol.Modes[0];
		mode.CommandHandlers[typeof(string)].Execute(_orchestrator, "command");
		var response = mode.RequestHandlers[typeof(string)].Execute(_orchestrator, "request");
		mode.ResponseHandlers[typeof(string), typeof(int)].Execute(_orchestrator, "request", response);

		Assert.That(receivedCommand, Is.EqualTo("command"));
		Assert.That(receivedResponse, Is.EqualTo(7));
		Assert.That(protocol.Validate().IsSuccess, Is.True);
	}

	private static ICommandHandler<string> CreateCommandHandler(bool useBaseClass, Action<ProtocolOrchestrator, string> action)
		=> useBaseClass ? new ActionCommandHandler<string>(action) : new DirectCommandHandler(action);

	private static IRequestHandler<string, int> CreateRequestHandler(bool useBaseClass, Func<ProtocolOrchestrator, string, int> action)
		=> useBaseClass ? new ActionRequestHandler<string, int>(action) : new DirectRequestHandler(action);

	private static IResponseHandler<string, int> CreateResponseHandler(bool useBaseClass, Action<ProtocolOrchestrator, string, int> action)
		=> useBaseClass ? new ActionResponseHandler<string, int>(action) : new DirectResponseHandler(action);

	private static IHandshakeHandler<string, int, long> CreateHandshakeHandler(bool useBaseClass)
		=> useBaseClass ? new BaseHandshakeHandler() : new DirectHandshakeHandler();

	private static IMessageGenerator<int> CreateGenerator(bool useBaseClass)
		=> useBaseClass ? new BaseGenerator() : new DirectGenerator();

	private sealed class DirectCommandHandler : ICommandHandler<string> {
		private readonly Action<ProtocolOrchestrator, string> _action;

		public DirectCommandHandler(Action<ProtocolOrchestrator, string> action) => _action = action;

		public Type MessageType => typeof(string);

		public void Execute(ProtocolOrchestrator orchestrator, string command) => _action(orchestrator, command);
	}

	private sealed class OverriddenCommandHandler : CommandHandlerBase<string> {
		public object UntypedCommand { get; private set; }

		public override Type MessageType => typeof(object);

		public override void Execute(ProtocolOrchestrator orchestrator, object command) => UntypedCommand = command;

		public override void Execute(ProtocolOrchestrator orchestrator, string command) => UntypedCommand = command;
	}

	private sealed class RemappedCommandHandler : CommandHandlerBase<string>, ICommandHandler<string> {
		public string Received { get; private set; }

		public override Type MessageType => typeof(string);

		public override void Execute(ProtocolOrchestrator orchestrator, string command) => Received = "class";

		void ICommandHandler<string>.Execute(ProtocolOrchestrator orchestrator, string command) => Received = "interface";
	}

	private sealed class RemappedRequestHandler : RequestHandlerBase<string, int>, IRequestHandler<string, int> {
		public override Type RequestType => typeof(string);

		public override Type ResponseType => typeof(int);

		public override int Execute(ProtocolOrchestrator orchestrator, string request) => request.Length;

		int IRequestHandler<string, int>.Execute(ProtocolOrchestrator orchestrator, string request) => -1;
	}

	private sealed class RemappedResponseHandler : ResponseHandlerBase<string, int>, IResponseHandler<string, int> {
		public string Received { get; private set; }

		public override Type RequestType => typeof(string);

		public override Type ResponseType => typeof(int);

		public override void Execute(ProtocolOrchestrator orchestrator, string request, int response) => Received = "class";

		void IResponseHandler<string, int>.Execute(ProtocolOrchestrator orchestrator, string request, int response) => Received = "interface";
	}

	private sealed class DirectRequestHandler : IRequestHandler<string, int> {
		private readonly Func<ProtocolOrchestrator, string, int> _action;

		public DirectRequestHandler(Func<ProtocolOrchestrator, string, int> action) => _action = action;

		public Type RequestType => typeof(string);

		public Type ResponseType => typeof(int);

		public int Execute(ProtocolOrchestrator orchestrator, string request) => _action(orchestrator, request);
	}

	private sealed class DirectResponseHandler : IResponseHandler<string, int> {
		private readonly Action<ProtocolOrchestrator, string, int> _action;

		public DirectResponseHandler(Action<ProtocolOrchestrator, string, int> action) => _action = action;

		public Type RequestType => typeof(string);

		public Type ResponseType => typeof(int);

		public void Execute(ProtocolOrchestrator orchestrator, string request, int response) => _action(orchestrator, request, response);
	}

	private sealed class DirectHandshakeHandler : IHandshakeHandler<string, int, long> {
		public string GenerateHandshake(ProtocolOrchestrator orchestrator) => "handshake";

		public HandshakeOutcome ReceiveHandshake(ProtocolOrchestrator orchestrator, string handshake, out int acknowledgement) {
			acknowledgement = handshake?.Length ?? 0;
			return HandshakeOutcome.Accepted;
		}

		public HandshakeOutcome VerifyHandshake(ProtocolOrchestrator orchestrator, string handshake, int acknowledgement, out long verifyAcknowledgement) {
			verifyAcknowledgement = acknowledgement + 1L;
			return HandshakeOutcome.TooBusy;
		}

		public bool AcknowledgeHandshake(ProtocolOrchestrator orchestrator, string handshake, int acknowledgement, long verifyAcknowledgement)
			=> handshake == "handshake" && acknowledgement == 9 && verifyAcknowledgement == 10L;
	}

	private class BaseHandshakeHandler : HandshakeHandlerBase<string, int, long> {
		protected override string GenerateHandshakeInternal(ProtocolOrchestrator orchestrator) => "handshake";

		public override HandshakeOutcome ReceiveHandshake(ProtocolOrchestrator orchestrator, string handshake, out int acknowledgement) {
			acknowledgement = handshake?.Length ?? 0;
			return HandshakeOutcome.Accepted;
		}

		public override HandshakeOutcome VerifyHandshake(ProtocolOrchestrator orchestrator, string handshake, int acknowledgement, out long verifyAcknowledgement) {
			verifyAcknowledgement = acknowledgement + 1L;
			return HandshakeOutcome.TooBusy;
		}

		public override bool AcknowledgeHandshake(ProtocolOrchestrator orchestrator, string handshake, int acknowledgement, long verifyAcknowledgement)
			=> handshake == "handshake" && acknowledgement == 9 && verifyAcknowledgement == 10L;
	}

	private sealed class RemappedHandshakeHandler : BaseHandshakeHandler, IHandshakeHandler<string, int, long> {
		string IHandshakeHandler<string, int, long>.GenerateHandshake(ProtocolOrchestrator orchestrator) => "remapped";

		HandshakeOutcome IHandshakeHandler<string, int, long>.ReceiveHandshake(ProtocolOrchestrator orchestrator, string handshake, out int acknowledgement) {
			acknowledgement = -1;
			return HandshakeOutcome.TooBusy;
		}

		HandshakeOutcome IHandshakeHandler<string, int, long>.VerifyHandshake(ProtocolOrchestrator orchestrator, string handshake, int acknowledgement, out long verifyAcknowledgement) {
			verifyAcknowledgement = -2L;
			return HandshakeOutcome.Accepted;
		}

		bool IHandshakeHandler<string, int, long>.AcknowledgeHandshake(ProtocolOrchestrator orchestrator, string handshake, int acknowledgement, long verifyAcknowledgement) => false;
	}

	private sealed class DirectGenerator : IMessageGenerator<int> {
		public int Execute(ProtocolOrchestrator orchestrator) => 17;
	}

	private sealed class BaseGenerator : MessageGeneratorBase<int> {
		protected override int ExecuteInternal(ProtocolOrchestrator orchestrator) => 17;
	}

	private sealed class TestChannel : ProtocolChannel {
		public override CommunicationRole LocalRole => CommunicationRole.Client;

		public override bool IsConnectionAlive() => false;

		protected override Task OpenInternal() => Task.CompletedTask;

		protected override Task CloseInternal() => Task.CompletedTask;

		protected override Task<byte[]> ReceiveBytesInternal(CancellationToken cancellationToken) => Task.FromResult(Array.Empty<byte>());

		protected override Task<bool> TrySendBytesInternal(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) => Task.FromResult(false);
	}
}
