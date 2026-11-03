// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

public class PreEventArgs : EventArgs {
	// Argumentless notifications have no payload to forward to their completion event.
	internal virtual void CopyCallArgsTo(PostEventArgs target) {
	}
}


public class PreEventArgs<TArgs> : PreEventArgs
	where TArgs : CallArgs {
	public PreEventArgs() {
	}

	public TArgs CallArgs { get; set; }

	// Preserve the operation's argument instance for the matching completion notification.
	internal override void CopyCallArgsTo(PostEventArgs target) {
		if (target is PostEventArgs<TArgs> typedTarget)
			typedTarget.CallArgs = CallArgs;
	}
}

