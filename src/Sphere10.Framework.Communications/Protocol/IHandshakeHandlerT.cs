// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Communications;

public interface IHandshakeHandler<THandshake, TAck, TVerack> : IHandshakeHandler {
	new THandshake GenerateHandshake(ProtocolOrchestrator orchestrator);

	HandshakeOutcome ReceiveHandshake(ProtocolOrchestrator orchestrator, THandshake handshake, out TAck acknowledgement);

	HandshakeOutcome VerifyHandshake(ProtocolOrchestrator orchestrator, THandshake handshake, TAck acknowledgement, out TVerack verifyAcknowledgement);

	bool AcknowledgeHandshake(ProtocolOrchestrator orchestrator, THandshake handshake, TAck acknowledgement, TVerack verifyAcknowledgement);

	#region Base Interface Bridges

	object IHandshakeHandler.GenerateHandshake(ProtocolOrchestrator orchestrator)
		=> GenerateHandshake(orchestrator);

	HandshakeOutcome IHandshakeHandler.ReceiveHandshake(ProtocolOrchestrator orchestrator, object handshake, out object acknowledgement) {
		var result = ReceiveHandshake(orchestrator, (THandshake)handshake, out var ack);
		acknowledgement = ack;
		return result;
	}

	HandshakeOutcome IHandshakeHandler.VerifyHandshake(ProtocolOrchestrator orchestrator, object handshake, object acknowledgement, out object verifyAcknowledgement) {
		var result = VerifyHandshake(orchestrator, (THandshake)handshake, (TAck)acknowledgement, out var verack);
		verifyAcknowledgement = verack;
		return result;
	}

	bool IHandshakeHandler.AcknowledgeHandshake(ProtocolOrchestrator orchestrator, object handshake, object acknowledgement, object verifyAcknowledgement)
		=> AcknowledgeHandshake(orchestrator, (THandshake)handshake, (TAck)acknowledgement, (TVerack)verifyAcknowledgement);

	#endregion
}

