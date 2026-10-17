// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Sends or posts native messages to a verified borrowed window.</summary>
public static class WindowMessageSender {
	public static Task<NativeWindowMessageResult> PostAsync(WindowsWindowInfo target, uint message, UIntPtr wParam, IntPtr lParam, CancellationToken cancellationToken = default)
		=> Task.Run(() => Dispatch(target, message, wParam, lParam, null, 0, () => cancellationToken.ThrowIfCancellationRequested()), cancellationToken);

	public static NativeWindowMessageResult Post(WindowsWindowInfo target, uint message, UIntPtr wParam, IntPtr lParam)
		=> Dispatch(target, message, wParam, lParam, null, 0, null);

	public static Task<NativeWindowMessageResult> SendAsync(WindowsWindowInfo target, uint message, UIntPtr wParam, IntPtr lParam,
		WinAPI.USER32.SendMessageTimeoutFlags flags, uint timeoutMilliseconds, CancellationToken cancellationToken = default)
		=> Task.Run(() => { Guard.ArgumentGT(timeoutMilliseconds, 0U, nameof(timeoutMilliseconds)); return Dispatch(target, message, wParam, lParam, flags, timeoutMilliseconds, () => cancellationToken.ThrowIfCancellationRequested()); }, cancellationToken);

	public static NativeWindowMessageResult Send(WindowsWindowInfo target, uint message, UIntPtr wParam, IntPtr lParam,
		WinAPI.USER32.SendMessageTimeoutFlags flags, uint timeoutMilliseconds) {
		Guard.ArgumentGT(timeoutMilliseconds, 0U, nameof(timeoutMilliseconds));
		return Dispatch(target, message, wParam, lParam, flags, timeoutMilliseconds, null);
	}

	private static NativeWindowMessageResult Dispatch(WindowsWindowInfo target, uint message, UIntPtr wParam, IntPtr lParam,
		WinAPI.USER32.SendMessageTimeoutFlags? flags, uint timeoutMilliseconds, Action checkpoint) {
		Guard.ArgumentNotNull(target, nameof(target));
		Guard.Argument(target.Handle != IntPtr.Zero && target.Handle != new IntPtr(0xFFFF) && target.ProcessId > 0 && target.ThreadId > 0 && target.ProcessStartTimeUtc.HasValue,
			nameof(target), "A verified specific window and owning process are required.");
		checkpoint?.Invoke();
		using var process = WinAPI.KERNEL32.OpenProcess(WinAPI.KERNEL32.ProcessQueryLimitedInformation | WinAPI.KERNEL32.Synchronize, false, (uint)target.ProcessId);
		Guard.Ensure(!process.IsInvalid, "The window's process is unavailable.");
		Guard.Ensure(WinAPI.KERNEL32.GetProcessTimes(process, out var creation, out _, out _, out _), "The window's process creation time is unavailable.");
		var started = DateTime.FromFileTimeUtc(checked((long)(((ulong)creation.DateTimeHigh << 32) | creation.DateTimeLow)));
		Guard.Ensure(started == target.ProcessStartTimeUtc && !WindowsProcess.HasSignaledExit(process), "The window's original process exited or its PID was reused.");
		Guard.Ensure(WinAPI.USER32.IsWindow(target.Handle), "The original window no longer exists.");
		var threadId = WinAPI.USER32.GetWindowThreadProcessId(target.Handle, out var processId);
		Guard.Ensure(threadId == target.ThreadId && processId == target.ProcessId && WindowsWindow.GetClassName(target.Handle) == target.ClassName, "The handle identifies a different window.");
		checkpoint?.Invoke();
		Marshal.SetLastPInvokeError(0);
		var timestamp = DateTime.UtcNow;
		var watch = Stopwatch.StartNew();
		UIntPtr returned = UIntPtr.Zero;
		var succeeded = flags.HasValue
			? WinAPI.USER32.SendMessageTimeoutW(target.Handle, message, wParam, lParam, flags.Value, timeoutMilliseconds, out returned) != IntPtr.Zero
			: WinAPI.USER32.PostMessageW(target.Handle, message, wParam, lParam);
		var error = succeeded ? 0 : Marshal.GetLastPInvokeError();
		return new NativeWindowMessageResult(succeeded, returned, error, timestamp, watch.ElapsedMilliseconds);
	}

	public static Task SendMainWindowMessageAsync(ProcessIdentity identity, uint message, UIntPtr wParam, IntPtr lParam, bool post = false,
		CancellationToken cancellationToken = default, uint timeoutMilliseconds = 5000) {
		if (!post && message != (uint)WinAPI.USER32.WM.QUIT) Guard.ArgumentGT(timeoutMilliseconds, 0U, nameof(timeoutMilliseconds));
		return ProcessCommandService.ExecuteAsync(identity, process => {
			cancellationToken.ThrowIfCancellationRequested();
			var window = process.MainWindowHandle;
			Guard.Ensure(window != IntPtr.Zero, "The process has no main window.");
			var threadId = WinAPI.USER32.GetWindowThreadProcessId(window, out var owner);
			Guard.Ensure(threadId != 0 && owner == process.Id, "The main window has a different owner.");
			Marshal.SetLastPInvokeError(0);
			var succeeded = message == (uint)WinAPI.USER32.WM.QUIT
				? WinAPI.USER32.PostThreadMessageW(threadId, message, wParam, lParam)
				: post ? WinAPI.USER32.PostMessageW(window, message, wParam, lParam)
					: WinAPI.USER32.SendMessageTimeoutW(window, message, wParam, lParam,
						WinAPI.USER32.SendMessageTimeoutFlags.AbortIfHung | WinAPI.USER32.SendMessageTimeoutFlags.ErrorOnExit, timeoutMilliseconds, out _) != IntPtr.Zero;
			if (!succeeded) throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows rejected or timed out the main-window message.");
		}, cancellationToken);
	}
}
