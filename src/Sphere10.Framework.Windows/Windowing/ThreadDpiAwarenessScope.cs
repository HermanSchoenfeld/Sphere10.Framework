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

namespace Sphere10.Framework.Windows;

/// <summary>Temporarily changes the current native thread's DPI awareness and restores its exact previous context.</summary>
/// <remarks>Use synchronously on the creating thread, nested in reverse disposal order. Never hold this scope across an await.</remarks>
public sealed class ThreadDpiAwarenessScope : SyncOnlyScope {
	private readonly IntPtr _previousContext;
	private readonly int _threadId;
	private bool _restored;

	public ThreadDpiAwarenessScope(DpiAwarenessContext context) {
		Guard.Argument(Enum.IsDefined(context), nameof(context), "Select a supported Windows DPI awareness context.");
		_threadId = Environment.CurrentManagedThreadId;
		_previousContext = WinAPI.USER32.SetThreadDpiAwarenessContext(new IntPtr((int)context));
		if (_previousContext == IntPtr.Zero)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not set the thread DPI awareness context.");
	}

	protected override void OnScopeEnd() {
		if (_restored)
			return;
		Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "DPI awareness must be restored on the thread that entered its scope.");
		if (WinAPI.USER32.SetThreadDpiAwarenessContext(_previousContext) == IntPtr.Zero)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not restore the thread DPI awareness context.");
		_restored = true;
	}
}
