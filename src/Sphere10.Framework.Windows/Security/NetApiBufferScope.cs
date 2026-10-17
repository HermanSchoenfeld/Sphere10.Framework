// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Security;

/// <summary>Owns a NetAPI output buffer; interior pointers remain borrowed until this scope ends.</summary>
public sealed class NetApiBufferScope : SyncScope, IScope<IntPtr> {
	private IntPtr _pointer;

	public NetApiBufferScope(IntPtr pointer) {
		_pointer = pointer;
	}

	public IntPtr Item => _pointer;

	protected override void OnScopeEnd() {
		if (_pointer != IntPtr.Zero)
			WinAPI.NETAPI32.NetApiBufferFree(_pointer);
		_pointer = IntPtr.Zero;
	}
}
