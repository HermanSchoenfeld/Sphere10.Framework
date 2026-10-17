// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sphere10.Framework.Windows;

/// <summary>Owns a remote WTS server handle. The local-current-server pseudo handle is borrowed and never closed.</summary>
public sealed class WtsServerScope : IDisposable {
	private IntPtr _handle;
	private int _disposed;

	public WtsServerScope(string serverName = null) {
		Guard.Argument(serverName == null || !serverName.Contains('\0'), nameof(serverName), "A server name cannot contain null characters.");
		if (string.IsNullOrEmpty(serverName) || serverName == "." || serverName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
			return;
		_handle = WinAPI.WTSAPI32.WTSOpenServerW(serverName);
		if (_handle == IntPtr.Zero)
			throw new Win32Exception(Marshal.GetLastPInvokeError());
	}

	public IntPtr Handle {
		get { ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this); return _handle; }
	}

	public void Dispose() {
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
			return;
		var handle = Interlocked.Exchange(ref _handle, IntPtr.Zero);
		if (handle != IntPtr.Zero)
			WinAPI.WTSAPI32.WTSCloseServer(handle);
	}
}
