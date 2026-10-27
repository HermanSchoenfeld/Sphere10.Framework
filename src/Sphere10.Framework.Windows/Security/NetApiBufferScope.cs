// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Security;

/// <summary>Owns a NetAPI output buffer; interior pointers remain borrowed until this scope ends.</summary>
public readonly struct NetApiBufferScope : IScope<IntPtr> {
	private readonly IntPtr _pointer;

	public NetApiBufferScope(IntPtr pointer) {
		_pointer = pointer;
	}

	public IntPtr Item => _pointer;

	public void Dispose() {
		if (_pointer != IntPtr.Zero)
			WinAPI.NETAPI32.NetApiBufferFree(_pointer);
	}

	public ValueTask DisposeAsync() => new(Task.Run(Dispose));
}
