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
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Temporarily changes the current native thread's DPI awareness and restores its exact previous context.</summary>
/// <remarks>Use synchronously on the creating thread, nested in reverse disposal order. Never hold this scope across an await.</remarks>
public readonly struct ThreadDpiAwarenessScope : IScope {
	private readonly IntPtr _previousContext;
	private readonly int _threadId;

	public ThreadDpiAwarenessScope(DpiAwarenessContext context)
		: this(ToNative(context)) {
	}

	private ThreadDpiAwarenessScope(IntPtr nativeContext) {
		_threadId = Environment.CurrentManagedThreadId;
		_previousContext = WinAPI.USER32.SetThreadDpiAwarenessContext(nativeContext);
		if (_previousContext == IntPtr.Zero)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not set the thread DPI awareness context.");
	}

	/// <summary>Enters the DPI awareness a window was created with, so that coordinates read and written in the scope are the window's own.</summary>
	/// <remarks>A window keeps the awareness it was created with whatever its thread uses later; WinForms fixes it when a control is constructed.</remarks>
	public static ThreadDpiAwarenessScope OfWindow(IntPtr window) {
		var context = WinAPI.USER32.GetWindowDpiAwarenessContext(window);
		if (context == IntPtr.Zero)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The window's DPI awareness could not be read.");
		return new ThreadDpiAwarenessScope(context);
	}

	public void Dispose() {
		if (_previousContext == IntPtr.Zero)
			return;
		Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "DPI awareness must be restored on the thread that entered its scope.");
		if (WinAPI.USER32.SetThreadDpiAwarenessContext(_previousContext) == IntPtr.Zero)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not restore the thread DPI awareness context.");
	}

	public ValueTask DisposeAsync() => ValueTask.FromException(new NotSupportedException("This scope does not support async"));

	// Validated before the thread's context is changed.
	private static IntPtr ToNative(DpiAwarenessContext context) {
		Guard.Argument(Enum.IsDefined(context), nameof(context), "Select a supported Windows DPI awareness context.");
		return new IntPtr((int)context);
	}
}
