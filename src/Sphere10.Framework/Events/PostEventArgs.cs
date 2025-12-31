// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

public class PostEventArgs : EventArgs {
}


public class PostEventArgs<TArgs> : PostEventArgs {
	public TArgs CallArgs { get; set; }
}


public class PostEventArgs<TArgs, TResult> : PostEventArgs<TArgs>
	where TArgs : CallArgs {
	public PostEventArgs(TResult result = default) {
		Result = result;
	}
	public TResult Result { get; set; }
}

