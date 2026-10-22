// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Queries borrowed native windows without transferring window ownership.</summary>
public static class WindowsWindow {
	public static Task<WindowsWindowInfo[]> EnumerateAsync(CancellationToken cancellationToken = default)
		=> EnumerateAsync(true, cancellationToken);

	/// <summary>Optionally omits process name/creation queries when the caller already owns a process inventory.</summary>
	public static Task<WindowsWindowInfo[]> EnumerateAsync(bool includeProcessDetails, CancellationToken cancellationToken = default)
		=> Task.Run(() => Enumerate(includeProcessDetails, () => cancellationToken.ThrowIfCancellationRequested()), cancellationToken);

	public static WindowsWindowInfo[] Enumerate() => Enumerate(true, null);

	public static WindowsWindowInfo[] Enumerate(bool includeProcessDetails) => Enumerate(includeProcessDetails, null);

	private static WindowsWindowInfo[] Enumerate(bool includeProcessDetails, Action checkpoint) {
		var windows = new List<WindowsWindowInfo>();
		Exception callbackError = null;
		WinAPI.USER32.WindowEnumCallback callback = (window, _) => {
			Tools.Exceptions.ExecuteCapturingException(() => {
				checkpoint?.Invoke();
				if (TryGetInfo(window, includeProcessDetails, out var info)) windows.Add(info);
			}, out callbackError);
			return callbackError == null;
		};
		Marshal.SetLastPInvokeError(0);
		var success = WinAPI.USER32.EnumWindows(callback, IntPtr.Zero);
		var nativeError = Marshal.GetLastPInvokeError();
		checkpoint?.Invoke();
		if (callbackError != null) throw callbackError;
		if (!success) throw new Win32Exception(nativeError, "Windows could not enumerate desktop windows.");
		return windows.ToArray();
	}

	public static bool TryGetInfo(IntPtr handle, out WindowsWindowInfo info) => TryGetInfo(handle, true, out info);

	public static bool TryGetInfo(IntPtr handle, bool includeProcessDetails, out WindowsWindowInfo info) {
		info = null;
		if (!WinAPI.USER32.IsWindow(handle)) return false;
		var threadId = WinAPI.USER32.GetWindowThreadProcessId(handle, out var processId);
		if (threadId == 0 || processId == 0) return false;
		var title = new StringBuilder(4096);
		WinAPI.USER32.GetWindowText(handle, title, title.Capacity);
		var className = GetClassName(handle);
		var processName = string.Empty;
		DateTime? processStarted = null;
		var error = string.Empty;
		if (includeProcessDetails) {
			Tools.Exceptions.ExecuteCapturingException(() => {
				using var process = Process.GetProcessById(checked((int)processId));
				processName = process.ProcessName;
				processStarted = process.StartTime.ToUniversalTime();
			}, out var failure, exception => exception is Win32Exception or InvalidOperationException or ArgumentException or NotSupportedException);
			error = failure?.Message ?? string.Empty;
		}
		if (WinAPI.USER32.GetWindowThreadProcessId(handle, out var owner) != threadId || owner != processId) return false;
		info = new WindowsWindowInfo(handle, title.ToString(), className, checked((int)processId), threadId, processName, processStarted,
			WinAPI.USER32.IsWindowVisible(handle), error) { IsMinimized = WinAPI.USER32.IsIconic(handle) };
		return true;
	}

	/// <summary>Reads screen-coordinate bounds after verifying the captured process, native thread and window class.</summary>
	/// <remarks>Coordinates follow GetWindowRect's caller DPI context and may include invisible resize borders. No target window is activated or changed.</remarks>
	public static Rectangle GetBounds(WindowsWindowInfo expected) => GetBoundsCore(expected, false);

	private static Rectangle GetBoundsCore(WindowsWindowInfo expected, bool visibleFrame) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.Argument(expected.Handle != IntPtr.Zero && expected.Handle != new IntPtr(0xFFFF) && expected.ProcessId > 0
			&& expected.ThreadId > 0 && expected.ProcessStartTimeUtc.HasValue, nameof(expected), "A specific window and verified process identity are required.");
		using var process = WinAPI.KERNEL32.OpenProcess(WinAPI.KERNEL32.ProcessQueryLimitedInformation | WinAPI.KERNEL32.Synchronize, false, (uint)expected.ProcessId);
		Guard.Ensure(!process.IsInvalid, "The window's process is no longer accessible.");
		Guard.Ensure(WinAPI.KERNEL32.GetProcessTimes(process, out var creation, out _, out _, out _), "The window's process identity is unavailable.");
		var started = DateTime.FromFileTimeUtc(checked((long)(((ulong)creation.DateTimeHigh << 32) | creation.DateTimeLow)));
		Guard.Ensure(started == expected.ProcessStartTimeUtc && !WindowsProcess.HasSignaledExit(process), "The original process exited or its PID was reused.");
		VerifyWindow(expected);
		WinAPI.RECT rectangle = default;
		// DWM excludes the invisible sizing margins and already returns physical pixels.
		var hasVisibleFrame = visibleFrame
			&& WinAPI.DWMAPI.DwmGetWindowAttribute(expected.Handle, WinAPI.DWMAPI.DWMWA_EXTENDED_FRAME_BOUNDS,
				out rectangle, (uint)Marshal.SizeOf<WinAPI.RECT>()) >= 0
			&& rectangle.Right > rectangle.Left && rectangle.Bottom > rectangle.Top;
		if (!hasVisibleFrame && !WinAPI.USER32.GetWindowRect(expected.Handle, out rectangle))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The window bounds are unavailable.");
		VerifyWindow(expected);
		return Rectangle.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
	}

	/// <summary>Reads verified outer window bounds in physical desktop pixels, independent of the caller's DPI awareness.</summary>
	/// <remarks>Includes invisible resize borders, like GetWindowRect. Does not change the process DPI policy or the target window.</remarks>
	public static Rectangle GetPhysicalBounds(WindowsWindowInfo expected) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		return GetBounds(expected);
	}

	/// <summary>Reads verified visible frame bounds in physical desktop pixels, excluding invisible resize margins when DWM supplies them.</summary>
	/// <remarks>Falls back to physical outer bounds when composition does not provide a usable frame. Preserves the caller's DPI context and target window.</remarks>
	public static Rectangle GetVisiblePhysicalBounds(WindowsWindowInfo expected) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		return GetBoundsCore(expected, true);
	}

	private static void VerifyWindow(WindowsWindowInfo expected) {
		Guard.Ensure(WinAPI.USER32.IsWindow(expected.Handle), "The original window no longer exists.");
		var threadId = WinAPI.USER32.GetWindowThreadProcessId(expected.Handle, out var processId);
		Guard.Ensure(threadId == expected.ThreadId && processId == expected.ProcessId && GetClassName(expected.Handle) == expected.ClassName,
			"The handle no longer identifies the captured window.");
	}

	internal static string GetClassName(IntPtr handle) {
		var value = new StringBuilder(256);
		WinAPI.USER32.GetClassNameW(handle, value, value.Capacity);
		return value.ToString();
	}
}
