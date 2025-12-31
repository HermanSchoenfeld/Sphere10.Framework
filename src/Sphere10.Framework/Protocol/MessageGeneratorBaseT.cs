// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Communications;

public abstract class MessageGeneratorBase<TMessage> : MessageGeneratorBase, IMessageGenerator<TMessage> {

	TMessage IMessageGenerator<TMessage>.Execute(ProtocolOrchestrator orchestrator)
		=> ExecuteInternal(orchestrator);

	public sealed override object Execute(ProtocolOrchestrator orchestrator) {
		Guard.ArgumentNotNull(orchestrator, nameof(orchestrator));
		return ((IMessageGenerator<TMessage>)this).Execute(orchestrator);
	}

	protected abstract TMessage ExecuteInternal(ProtocolOrchestrator orchestrator);

}

