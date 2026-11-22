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
	// Windows draws every Store app's top-level window in its frame host (ApplicationFrameHost.exe); while the app is on the
	// screen, the app's own window sits inside that frame as its child.
	private const string StoreAppFrameClassName = "ApplicationFrameWindow";
	private const string StoreAppContentClassName = "Windows.UI.Core.CoreWindow";

	/// <summary>Reads the visible window beneath a physical desktop point and verifies its captured owner before returning highlight bounds.</summary>
	public static Task<WindowsWindowTarget> ReadAtPhysicalPointAsync(Point point, CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadAtPhysicalPoint(point, () => cancellationToken.ThrowIfCancellationRequested()), cancellationToken);

	/// <summary>Reads the current pointer and its window on a worker in physical desktop coordinates.</summary>
	public static Task<WindowsWindowTarget> ReadAtPointerAsync(CancellationToken cancellationToken = default) {
		return Task.Run(() => {
			Debug.AssertNotMainThread();
			cancellationToken.ThrowIfCancellationRequested();
			using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
			if (!WinAPI.USER32.GetCursorPos(out var point))
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "The desktop pointer position is unavailable.");
			return ReadAtPhysicalPoint(new Point(point.x, point.y), () => cancellationToken.ThrowIfCancellationRequested());
		}, cancellationToken);
	}

	public static Task<WindowsWindowInfo[]> EnumerateAsync(CancellationToken cancellationToken = default)
		=> EnumerateAsync(true, cancellationToken);

	/// <summary>Optionally omits process name/creation queries when the caller already owns a process inventory.</summary>
	public static Task<WindowsWindowInfo[]> EnumerateAsync(bool includeProcessDetails, CancellationToken cancellationToken = default)
		=> Task.Run(() => Enumerate(includeProcessDetails, () => cancellationToken.ThrowIfCancellationRequested()), cancellationToken);

	/// <summary>Enumerates top-level windows with optional captions, process details and early visible-window filtering.</summary>
	/// <remarks>
	/// Omitting titles leaves <see cref="WindowsWindowInfo.Title"/> empty and avoids synchronous caption messages to this process's UI threads.
	/// Visible-only inventory excludes hidden and minimized windows before querying their captions or processes; it does not test occlusion.
	/// </remarks>
	public static Task<WindowsWindowInfo[]> EnumerateAsync(bool includeProcessDetails, bool includeTitles, bool visibleOnly, CancellationToken cancellationToken = default)
		=> Task.Run(() => Enumerate(WindowsWindowEnumerationScope.TopLevel, includeProcessDetails,
			() => cancellationToken.ThrowIfCancellationRequested(), includeTitles, visibleOnly), cancellationToken);

	public static WindowsWindowInfo[] Enumerate() => Enumerate(true, null);

	public static WindowsWindowInfo[] Enumerate(bool includeProcessDetails) => Enumerate(includeProcessDetails, null);

	/// <summary>Enumerates selected native window families on a worker without changing target windows.</summary>
	public static Task<WindowsWindowInfo[]> EnumerateAsync(WindowsWindowEnumerationScope scope, bool includeProcessDetails = true, CancellationToken cancellationToken = default)
		=> Task.Run(() => Enumerate(scope, includeProcessDetails, () => cancellationToken.ThrowIfCancellationRequested()), cancellationToken);

	public static WindowsWindowInfo[] Enumerate(WindowsWindowEnumerationScope scope, bool includeProcessDetails = true) => Enumerate(scope, includeProcessDetails, null);

	public static bool TryGetInfo(IntPtr handle, out WindowsWindowInfo info) => TryGetInfo(handle, true, out info);

	public static bool TryGetInfo(IntPtr handle, bool includeProcessDetails, out WindowsWindowInfo info) => TryGetInfoCore(handle, includeProcessDetails, null, out info);

	/// <summary>Reads screen-coordinate bounds after verifying the captured process, native thread and window class.</summary>
	/// <remarks>Coordinates follow GetWindowRect's caller DPI context and may include invisible resize borders. No target window is activated or changed.</remarks>
	public static Rectangle GetBounds(WindowsWindowInfo expected) => GetBoundsCore(expected, false);

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

	/// <summary>Verifies a picked window and reads its top-level ancestor and visible physical bounds on a worker.</summary>
	public static Task<WindowsWindowTarget> GetTopLevelTargetAsync(WindowsWindowInfo expected, CancellationToken cancellationToken = default) {
		return Task.Run(() => ExecuteVerified(expected, () => {
			cancellationToken.ThrowIfCancellationRequested();
			var root = WinAPI.USER32.GetAncestor(expected.Handle, WinAPI.USER32.GA_ROOT);
			Guard.Ensure(TryGetInfo(root, out var info), "The selected top-level window is no longer available.");
			return new WindowsWindowTarget(info, GetVisiblePhysicalBounds(info));
		}), cancellationToken);
	}

	/// <summary>Reads the executable path of a verified window owner on a worker.</summary>
	public static Task<string> GetExecutablePathAsync(WindowsWindowInfo expected, CancellationToken cancellationToken = default) {
		return Task.Run(() => ExecuteVerified(expected, () => {
			cancellationToken.ThrowIfCancellationRequested();
			using var process = WindowsProcess.Open(expected.ProcessId);
			return process.ExecutablePath;
		}), cancellationToken);
	}

	/// <summary>Reads, on a worker, the executable path of the application a verified top-level window shows.</summary>
	/// <remarks>
	/// That is the window's own process, except for a Store app: its top-level window belongs to the frame host that Windows
	/// runs for every Store app, and the app's own window sits inside it. This returns the app's executable then, or the frame
	/// host's while the frame holds no app window (the app is suspended). Use it to identify the application a user means, and
	/// <see cref="GetExecutablePathAsync"/> for the process that owns the window.
	/// </remarks>
	public static Task<string> GetApplicationExecutablePathAsync(WindowsWindowInfo expected, CancellationToken cancellationToken = default) {
		return Task.Run(() => ExecuteVerified(expected, () => {
			cancellationToken.ThrowIfCancellationRequested();
			using var process = WindowsProcess.Open(TryGetHostedApplicationProcessId(expected.Handle, out var hosted) ? hosted : expected.ProcessId);
			return process.ExecutablePath;
		}), cancellationToken);
	}

	/// <summary>Whether a top-level window is a Store app's frame that holds the app's own window, and the app's process.</summary>
	internal static bool TryGetHostedApplicationProcessId(IntPtr window, out int processId) {
		processId = 0;
		if (GetClassName(window) != StoreAppFrameClassName)
			return false;
		var content = WinAPI.USER32.FindWindowExW(window, IntPtr.Zero, StoreAppContentClassName, null);
		if (content == IntPtr.Zero || WinAPI.USER32.GetWindowThreadProcessId(content, out var contentProcess) == 0
			|| WinAPI.USER32.GetWindowThreadProcessId(window, out var frameProcess) == 0 || contentProcess == frameProcess)
			return false;
		processId = checked((int)contentProcess);
		return true;
	}

	/// <summary>Shares held process identity and HWND/thread/class admission across read-only window inspectors.</summary>
	internal static T ExecuteVerified<T>(WindowsWindowInfo expected, Func<T> read) {
		Debug.AssertNotMainThread();
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.ArgumentNotNull(read, nameof(read));
		Guard.Argument(expected.Handle != IntPtr.Zero && expected.Handle != new IntPtr(0xFFFF) && expected.ProcessId > 0
			&& expected.ThreadId > 0 && expected.ProcessStartTimeUtc.HasValue, nameof(expected), "A specific window and verified process identity are required.");
		using var process = WinAPI.KERNEL32.OpenProcess(WinAPI.KERNEL32.ProcessQueryLimitedInformation | WinAPI.KERNEL32.Synchronize, false, (uint)expected.ProcessId);
		Guard.Ensure(!process.IsInvalid, "The window's process is no longer accessible.");
		Guard.Ensure(WinAPI.KERNEL32.GetProcessTimes(process, out var creation, out _, out _, out _), "The window's process identity is unavailable.");
		var started = DateTime.FromFileTimeUtc(checked((long)(((ulong)creation.DateTimeHigh << 32) | creation.DateTimeLow)));
		Guard.Ensure(started == expected.ProcessStartTimeUtc && !WindowsProcess.HasSignaledExit(process), "The original process exited or its PID was reused.");
		VerifyWindow(expected);
		var result = read();
		VerifyWindow(expected);
		Guard.Ensure(!WindowsProcess.HasSignaledExit(process), "The original process exited during window inspection.");
		return result;
	}

	internal static string GetClassName(IntPtr handle) {
		var value = new StringBuilder(256);
		WinAPI.USER32.GetClassNameW(handle, value, value.Capacity);
		return value.ToString();
	}

	// A failed query is not evidence of closure. Retire identities only after an observed window/process mismatch or exit.
	internal static bool HasIdentityExpired(WindowsWindowInfo expected) {
		Debug.AssertNotMainThread();
		if (!WinAPI.USER32.IsWindow(expected.Handle))
			return true;
		var threadId = WinAPI.USER32.GetWindowThreadProcessId(expected.Handle, out var processId);
		if (threadId != 0 && (threadId != expected.ThreadId || processId != expected.ProcessId))
			return true;
		var className = GetClassName(expected.Handle);
		if (className.Length > 0 && className != expected.ClassName)
			return true;
		var expired = false;
		Tools.Exceptions.ExecuteCapturingException(() => {
			using var process = WindowsProcess.Open(expected.ProcessId);
			expired = process.Identity.StartTimeUtc != expected.ProcessStartTimeUtc || process.HasExited;
		}, out _, error => error is Win32Exception or InvalidOperationException or ArgumentException);
		return expired;
	}

	private static WindowsWindowTarget ReadAtPhysicalPoint(Point point, Action checkpoint) {
		Debug.AssertNotMainThread();
		checkpoint?.Invoke();
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var handle = WinAPI.USER32.WindowFromPoint(new WinAPI.POINT { x = point.X, y = point.Y });
		if (handle == IntPtr.Zero || !TryGetInfoCore(handle, true, false, out var info) || !info.IsVisible || info.IsMinimized)
			return null;
		checkpoint?.Invoke();
		if (!info.ProcessStartTimeUtc.HasValue)
			return new WindowsWindowTarget(info, Rectangle.Empty);
		var bounds = GetVisiblePhysicalBounds(info);
		checkpoint?.Invoke();
		return new WindowsWindowTarget(info, bounds);
	}

	private static WindowsWindowInfo[] Enumerate(bool includeProcessDetails, Action checkpoint)
		=> Enumerate(WindowsWindowEnumerationScope.TopLevel, includeProcessDetails, checkpoint);

	private static WindowsWindowInfo[] Enumerate(WindowsWindowEnumerationScope scope, bool includeProcessDetails, Action checkpoint, bool includeTitles = true, bool visibleOnly = false) {
		Debug.AssertNotMainThread();
		Guard.Argument(scope != 0 && (scope & ~WindowsWindowEnumerationScope.All) == 0, nameof(scope), "Select a supported window enumeration scope.");
		var windows = new List<WindowsWindowInfo>();
		var visited = new HashSet<IntPtr>();
		Exception callbackError = null;
		WinAPI.USER32.WindowEnumCallback childCallback = (window, _) => {
			Tools.Exceptions.ExecuteCapturingException(() => {
				checkpoint?.Invoke();
				if (visibleOnly && (!WinAPI.USER32.IsWindowVisible(window) || WinAPI.USER32.IsIconic(window)))
					return;
				if (visited.Add(window) && TryGetInfoCore(window, includeProcessDetails, false, out var info, includeTitles))
					windows.Add(info);
			}, out callbackError);
			return callbackError == null;
		};
		WinAPI.USER32.WindowEnumCallback callback = (window, _) => {
			Tools.Exceptions.ExecuteCapturingException(() => {
				checkpoint?.Invoke();
				if (visibleOnly && (!WinAPI.USER32.IsWindowVisible(window) || WinAPI.USER32.IsIconic(window)))
					return;
				if ((scope & WindowsWindowEnumerationScope.TopLevel) != 0 && visited.Add(window) && TryGetInfoCore(window, includeProcessDetails, false, out var info, includeTitles))
					windows.Add(info);
				// EnumChildWindows includes all descendants. Its BOOL return value has no error contract.
				if ((scope & WindowsWindowEnumerationScope.Descendants) != 0)
					WinAPI.USER32.EnumChildWindows(window, childCallback, IntPtr.Zero);
				if (callbackError != null)
					throw callbackError;
			}, out callbackError);
			return callbackError == null;
		};
		if ((scope & (WindowsWindowEnumerationScope.TopLevel | WindowsWindowEnumerationScope.Descendants)) != 0) {
			Marshal.SetLastPInvokeError(0);
			var success = WinAPI.USER32.EnumWindows(callback, IntPtr.Zero);
			var nativeError = Marshal.GetLastPInvokeError();
			checkpoint?.Invoke();
			if (callbackError != null)
				throw callbackError;
			if (!success)
				throw new Win32Exception(nativeError, "Windows could not enumerate desktop windows.");
		}
		if ((scope & WindowsWindowEnumerationScope.MessageOnly) != 0) {
			// Message-only windows are outside EnumWindows. Remember handles to bound HWND reuse during traversal.
			var parent = WinAPI.USER32.HWND_MESSAGE;
			var after = IntPtr.Zero;
			var messageWindows = new HashSet<IntPtr>();
			while (true) {
				checkpoint?.Invoke();
				var window = WinAPI.USER32.FindWindowExW(parent, after, null, null);
				if (window == IntPtr.Zero || !messageWindows.Add(window))
					break;
				after = window;
				if (visibleOnly && (!WinAPI.USER32.IsWindowVisible(window) || WinAPI.USER32.IsIconic(window)))
					continue;
				if (visited.Add(window) && TryGetInfoCore(window, includeProcessDetails, true, out var info, includeTitles))
					windows.Add(info with { IsMessageOnly = true, ParentHandle = parent });
			}
		}
		return windows.ToArray();
	}

	private static bool TryGetInfoCore(IntPtr handle, bool includeProcessDetails, bool? messageOnlyHint, out WindowsWindowInfo info, bool includeTitle = true) {
		info = null;
		if (!WinAPI.USER32.IsWindow(handle)) return false;
		var threadId = WinAPI.USER32.GetWindowThreadProcessId(handle, out var processId);
		if (threadId == 0 || processId == 0) return false;
		var title = string.Empty;
		if (includeTitle) {
			var buffer = new StringBuilder(4096);
			WinAPI.USER32.GetWindowText(handle, buffer, buffer.Capacity);
			title = buffer.ToString();
		}
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
		var style = ReadStyle(handle, WinAPI.USER32.GWL_STYLE, ref error);
		var extendedStyle = ReadStyle(handle, WinAPI.USER32.GWL_EXSTYLE, ref error);
		var parent = WinAPI.USER32.GetParent(handle);
		// GetParent does not reliably return HWND_MESSAGE. Query the documented message-only family explicitly.
		var messageOnly = messageOnlyHint ?? IsMessageOnlyWindow(handle, className);
		var dpi = WinAPI.USER32.GetDpiForWindow(handle);
		info = new WindowsWindowInfo(handle, title, className, checked((int)processId), threadId, processName, processStarted,
			WinAPI.USER32.IsWindowVisible(handle), error) {
			IsMinimized = WinAPI.USER32.IsIconic(handle),
			ParentHandle = messageOnly ? WinAPI.USER32.HWND_MESSAGE : (style.GetValueOrDefault() & WinAPI.USER32.WS_CHILD) != 0 ? parent : IntPtr.Zero,
			OwnerHandle = WinAPI.USER32.GetWindow(handle, WinAPI.USER32.GetWindow_Cmd.GW_OWNER),
			IsMessageOnly = messageOnly, Style = style, ExtendedStyle = extendedStyle, Dpi = dpi == 0 ? null : dpi
		};
		if (WinAPI.USER32.GetWindowThreadProcessId(handle, out var finalProcess) != threadId || finalProcess != processId || GetClassName(handle) != className) {
			info = null;
			return false;
		}
		return true;
	}

	private static bool IsMessageOnlyWindow(IntPtr handle, string className) {
		var visited = new HashSet<IntPtr>();
		var after = IntPtr.Zero;
		while (true) {
			var candidate = WinAPI.USER32.FindWindowExW(WinAPI.USER32.HWND_MESSAGE, after, className, null);
			if (candidate == handle)
				return true;
			if (candidate == IntPtr.Zero || !visited.Add(candidate))
				return false;
			after = candidate;
		}
	}

	private static uint? ReadStyle(IntPtr window, int index, ref string error) {
		Marshal.SetLastPInvokeError(0);
		var value = WinAPI.USER32.GetWindowLong(window, index);
		var nativeError = Marshal.GetLastPInvokeError();
		if (value != IntPtr.Zero || nativeError == 0)
			return unchecked((uint)value.ToInt64());
		error = string.IsNullOrEmpty(error) ? new Win32Exception(nativeError).Message : error + "; " + new Win32Exception(nativeError).Message;
		return null;
	}

	private static Rectangle GetBoundsCore(WindowsWindowInfo expected, bool visibleFrame) {
		return ExecuteVerified(expected, () => {
			WinAPI.RECT rectangle = default;
			// DWM excludes the invisible sizing margins and already returns physical pixels.
			var hasVisibleFrame = visibleFrame
				&& WinAPI.DWMAPI.DwmGetWindowAttribute(expected.Handle, WinAPI.DWMAPI.DWMWA_EXTENDED_FRAME_BOUNDS,
					out rectangle, (uint)Marshal.SizeOf<WinAPI.RECT>()) >= 0
				&& rectangle.Right > rectangle.Left && rectangle.Bottom > rectangle.Top;
			if (!hasVisibleFrame && !WinAPI.USER32.GetWindowRect(expected.Handle, out rectangle))
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "The window bounds are unavailable.");
			return Rectangle.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
		});
	}

	private static void VerifyWindow(WindowsWindowInfo expected) {
		Guard.Ensure(WinAPI.USER32.IsWindow(expected.Handle), "The original window no longer exists.");
		var threadId = WinAPI.USER32.GetWindowThreadProcessId(expected.Handle, out var processId);
		Guard.Ensure(threadId == expected.ThreadId && processId == expected.ProcessId && GetClassName(expected.Handle) == expected.ClassName,
			"The handle no longer identifies the captured window.");
	}
}
