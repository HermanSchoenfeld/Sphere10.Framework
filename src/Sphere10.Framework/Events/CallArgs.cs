// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

public class CallArgs {
	private readonly object[] _args;

	public CallArgs(params object[] args) {
		_args = args ?? new object[0];
	}

	public object this[int index] {
		get => _args[index];
		set => _args[index] = value;
	}

	public int ArgCount => _args.Length;

	//public object[] Args { get; set; }

}

