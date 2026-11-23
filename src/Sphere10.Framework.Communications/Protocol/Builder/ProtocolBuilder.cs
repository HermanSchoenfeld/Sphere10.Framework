// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;

namespace Sphere10.Framework.Communications;

/// <summary>Composes a retained protocol definition without executing its runtime message handlers.</summary>
public class ProtocolBuilder {

	private readonly Protocol _protocol;

	public ProtocolBuilder() : this(SerializerFactory.Default) { }

	public ProtocolBuilder(SerializerFactory baseFactory) {
		Guard.ArgumentNotNull(baseFactory, nameof(baseFactory));
		_protocol = new Protocol();
		ActiveMode = 0;
	}

	public int ActiveMode { get; private set; }

	public SerializerFactory BaseFactory { get; }

	public ProtocolBuilder SetMode(int mode) {
		Guard.Argument(mode != 0, nameof(mode), "Handshake (mode 0) cannot be edited directly, use Handshake property.");
		Guard.ArgumentInRange(mode, 1, _protocol.Modes.Length, nameof(mode));
		if (mode == _protocol.Modes.Length) 
			_protocol.AddMode();
		ActiveMode = mode;
		return this;
	}

	/// <summary>Immediately configures and replaces the protocol's handshake definition.</summary>
	public ProtocolBuilder WithHandshake(Action<ProtocolHandshakeBuilder> handshakeBuild) {
		var handshakeBuilder = new ProtocolHandshakeBuilder(this);
		handshakeBuild(handshakeBuilder);
		_protocol.Handshake = handshakeBuilder.Build();
		return this;
	}

	/// <summary>Immediately configures and registers one request/response handler pair.</summary>
	public ProtocolBuilder AddRequest<TRequest>(Action<ProtocolRequestBuilder<TRequest>> requestBuild) => AddRequest(requestBuild, out _);

	/// <summary>Adds one configured request/response pair and returns its exact registered handlers.</summary>
	public ProtocolBuilder AddRequest<TRequest>(Action<ProtocolRequestBuilder<TRequest>> requestBuild,
		out (IRequestHandler RequestHandler, IResponseHandler ResponseHandler) registration) {
		var requestBuilder = new ProtocolRequestBuilder<TRequest>();
		requestBuild(requestBuilder);
		registration = requestBuilder.Build();
		return AddRequestResponse(registration.RequestHandler, registration.ResponseHandler);
	}

	/// <summary>Registers runtime request and response callbacks that do not require message arguments.</summary>
	public ProtocolBuilder OnRequestResponse<TRequest, TResponse>(Func<TResponse> requestHandler, Action responseHandler)
		=> OnRequestResponse<TRequest, TResponse>((_, _) => requestHandler(), (_, _, _) => responseHandler());

	/// <summary>Registers runtime request and response callbacks over their typed messages.</summary>
	public ProtocolBuilder OnRequestResponse<TRequest, TResponse>(Func<TRequest, TResponse> requestHandler, Action<TRequest, TResponse> responseHandler)
		=> OnRequestResponse<TRequest, TResponse>((_, request) => requestHandler(request), (_, request, response) => responseHandler(request, response));

	/// <summary>Registers runtime request and response callbacks with their executing orchestrator.</summary>
	public ProtocolBuilder OnRequestResponse<TRequest, TResponse>(Func<ProtocolOrchestrator, TRequest, TResponse> requestHandler, Action<ProtocolOrchestrator, TRequest, TResponse> responseHandler) {
		return AddRequestResponse(
			new ActionRequestHandler<TRequest, TResponse>(requestHandler), 
			new ActionResponseHandler<TRequest, TResponse>(responseHandler)
		);
	}

	public ProtocolBuilder AddRequestResponse(IRequestHandler requestHandler, IResponseHandler responseHandler)
		=> AddRequestResponse(requestHandler, responseHandler, out _);

	/// <summary>Adds existing compatible exchange handlers and retains their registration identity.</summary>
	public ProtocolBuilder AddRequestResponse(IRequestHandler requestHandler, IResponseHandler responseHandler,
		out (IRequestHandler RequestHandler, IResponseHandler ResponseHandler) registration) {
		Guard.ArgumentNotNull(requestHandler, nameof(requestHandler));
		Guard.ArgumentNotNull(responseHandler, nameof(responseHandler));
		Guard.Ensure(requestHandler.RequestType == responseHandler.RequestType, "Request type mismatch on handlers");
		Guard.Ensure(requestHandler.ResponseType == responseHandler.ResponseType, "Response type mismatch on handlers");
		_protocol.Modes[ActiveMode].RequestHandlers.Add(requestHandler.RequestType, requestHandler);
		_protocol.Modes[ActiveMode].ResponseHandlers.Add(responseHandler.RequestType, responseHandler.ResponseType, responseHandler);
		registration = (requestHandler, responseHandler);
		return this;
	}

	/// <summary>Immediately configures and registers one command handler through its child builder.</summary>
	public ProtocolBuilder AddCommand<TMessage>(Action<ProtocolCommandBuilder<TMessage>> commandBuild) => AddCommand(commandBuild, out _);

	/// <summary>Adds one configured command and returns its exact registered handler.</summary>
	public ProtocolBuilder AddCommand<TMessage>(Action<ProtocolCommandBuilder<TMessage>> commandBuild, out ICommandHandler registration) {
		var commandBuilder = new ProtocolCommandBuilder<TMessage>();
		commandBuild(commandBuilder);
		registration = commandBuilder.Build();
		return AddCommand(registration);
	}

	/// <summary>Registers a runtime command callback that does not require the message.</summary>
	public ProtocolBuilder OnCommand<TMessage>(Action commandHandler) => OnCommand<TMessage>(_ => commandHandler());

	/// <summary>Registers a runtime command callback over its typed message.</summary>
	public ProtocolBuilder OnCommand<TMessage>(Action<TMessage> commandHandler) => OnCommand<TMessage>((_, m) => commandHandler(m));

	/// <summary>Registers a runtime command callback with its executing orchestrator.</summary>
	public ProtocolBuilder OnCommand<TMessage>(Action<ProtocolOrchestrator, TMessage> commandHandler) => AddCommand(new ActionCommandHandler<TMessage>(commandHandler));

	public ProtocolBuilder AddCommand(ICommandHandler commandHandler) => AddCommand(commandHandler, out _);

	/// <summary>Adds an existing command handler and returns that same registered product.</summary>
	public ProtocolBuilder AddCommand(ICommandHandler commandHandler, out ICommandHandler registration) {
		Guard.ArgumentNotNull(commandHandler, nameof(commandHandler));
		_protocol.Modes[ActiveMode].CommandHandlers.Add(commandHandler.MessageType, commandHandler);
		registration = commandHandler;
		return this;
	}

	/// <summary>Immediately applies the supplied serializer-factory configuration callback.</summary>
	public ProtocolBuilder AddSerializers(Action<SerializerFactory> serializerFactoryConfig) {
		serializerFactoryConfig(BaseFactory);
		return this;
	}

	public ProtocolBuilder AutoBuildSerializers() {
		foreach(var type in _protocol.Modes.SelectMany(x => x.GetAllUsedMessageTypes())) 
			if (!BaseFactory.ContainsSerializer(type))
				BaseFactory.RegisterAutoBuild(type);
		return this;
	}

	public Protocol Build() {
		// Convention: Handshake uses Mode 0 serializers (App)
		var result = _protocol.Validate();
		if (result.IsFailure)
			throw new ProtocolBuilderException(result.ErrorMessages.ToDelimittedString(Environment.NewLine));
		return _protocol;
	}

}

