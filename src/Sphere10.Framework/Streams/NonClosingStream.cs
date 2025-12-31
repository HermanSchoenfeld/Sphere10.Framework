// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.IO;

namespace Sphere10.Framework;

public class NonClosingStream<TInner> : StreamDecorator<TInner> where TInner : Stream {
	public NonClosingStream(TInner innerStream)
		: base(innerStream) {
	}

	public override void Close() {
		// do not close underlying stream
		// Note: overriding dispose is inconsequential
	}
}


public class NonClosingStream : NonClosingStream<Stream>{
	public NonClosingStream(Stream innerStream)
		: base(innerStream) {
	}
}

