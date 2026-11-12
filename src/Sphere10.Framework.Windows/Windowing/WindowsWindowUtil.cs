// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Discovers the existing borrowed-window queries and verified message operations through Tools.Windows.Windowing.</summary>
	/// <remarks>Synchronous operations retain their worker-thread requirements. No window or native handle is retained by this utility.</remarks>
	public static class Windowing {
		/// <summary>Reads the window at a physical desktop point using the existing worker query.</summary>
		public static Task<WindowsWindowTarget> ReadAtPhysicalPointAsync(Point point, CancellationToken cancellationToken = default)
			=> WindowsWindow.ReadAtPhysicalPointAsync(point, cancellationToken);

		/// <summary>Reads the pointer and its window in physical desktop coordinates on a worker.</summary>
		public static Task<WindowsWindowTarget> ReadAtPointerAsync(CancellationToken cancellationToken = default) => WindowsWindow.ReadAtPointerAsync(cancellationToken);

		/// <summary>Enumerates top-level windows and their available process details on a worker.</summary>
		public static Task<WindowsWindowInfo[]> EnumerateAsync(CancellationToken cancellationToken = default) => WindowsWindow.EnumerateAsync(cancellationToken);

		/// <summary>Enumerates top-level windows, optionally omitting process-detail queries.</summary>
		public static Task<WindowsWindowInfo[]> EnumerateAsync(bool includeProcessDetails, CancellationToken cancellationToken = default)
			=> WindowsWindow.EnumerateAsync(includeProcessDetails, cancellationToken);

		/// <summary>Enumerates the chosen native window families using the existing worker query.</summary>
		public static Task<WindowsWindowInfo[]> EnumerateAsync(WindowsWindowEnumerationScope scope, bool includeProcessDetails = true, CancellationToken cancellationToken = default)
			=> WindowsWindow.EnumerateAsync(scope, includeProcessDetails, cancellationToken);

		/// <summary>Enumerates top-level windows synchronously on the calling worker.</summary>
		public static WindowsWindowInfo[] Enumerate() => WindowsWindow.Enumerate();

		/// <summary>Enumerates top-level windows synchronously with the chosen process-detail policy.</summary>
		public static WindowsWindowInfo[] Enumerate(bool includeProcessDetails) => WindowsWindow.Enumerate(includeProcessDetails);

		/// <summary>Enumerates the chosen window families synchronously on the calling worker.</summary>
		public static WindowsWindowInfo[] Enumerate(WindowsWindowEnumerationScope scope, bool includeProcessDetails = true) => WindowsWindow.Enumerate(scope, includeProcessDetails);

		/// <summary>Reads a borrowed HWND synchronously without acquiring ownership of that window.</summary>
		public static bool TryGetInfo(IntPtr handle, out WindowsWindowInfo info) => WindowsWindow.TryGetInfo(handle, out info);

		/// <summary>Reads a borrowed HWND with the existing optional process-detail query.</summary>
		public static bool TryGetInfo(IntPtr handle, bool includeProcessDetails, out WindowsWindowInfo info) => WindowsWindow.TryGetInfo(handle, includeProcessDetails, out info);

		/// <summary>Verifies the captured owner and reads bounds in the calling worker's DPI context.</summary>
		public static Rectangle GetBounds(WindowsWindowInfo expected) => WindowsWindow.GetBounds(expected);

		/// <summary>Verifies the captured owner and reads outer bounds in physical desktop pixels.</summary>
		public static Rectangle GetPhysicalBounds(WindowsWindowInfo expected) => WindowsWindow.GetPhysicalBounds(expected);

		/// <summary>Reads verified physical visible-frame bounds with the existing outer-bound fallback.</summary>
		public static Rectangle GetVisiblePhysicalBounds(WindowsWindowInfo expected) => WindowsWindow.GetVisiblePhysicalBounds(expected);

		/// <summary>Posts a message to the captured exact window instance using the existing worker dispatch.</summary>
		public static Task<NativeWindowMessageResult> PostAsync(WindowsWindowInfo target, uint message, UIntPtr wParam, IntPtr lParam, CancellationToken cancellationToken = default)
			=> WindowMessageSender.PostAsync(target, message, wParam, lParam, cancellationToken);

		/// <summary>Posts a message synchronously after the existing process and window identity checks.</summary>
		public static NativeWindowMessageResult Post(WindowsWindowInfo target, uint message, UIntPtr wParam, IntPtr lParam)
			=> WindowMessageSender.Post(target, message, wParam, lParam);

		/// <summary>Sends a message on a worker, preserving the existing timeout and flag behavior.</summary>
		public static Task<NativeWindowMessageResult> SendAsync(WindowsWindowInfo target, uint message, UIntPtr wParam, IntPtr lParam,
			WinAPI.USER32.SendMessageTimeoutFlags flags, uint timeoutMilliseconds, CancellationToken cancellationToken = default) {
			return WindowMessageSender.SendAsync(target, message, wParam, lParam, flags, timeoutMilliseconds, cancellationToken);
		}

		/// <summary>Sends a message on the calling worker after the existing identity and timeout checks.</summary>
		public static NativeWindowMessageResult Send(WindowsWindowInfo target, uint message, UIntPtr wParam, IntPtr lParam,
			WinAPI.USER32.SendMessageTimeoutFlags flags, uint timeoutMilliseconds) {
			return WindowMessageSender.Send(target, message, wParam, lParam, flags, timeoutMilliseconds);
		}

		/// <summary>Dispatches to the verified process's main window through the existing message service.</summary>
		public static Task SendMainWindowMessageAsync(ProcessIdentity identity, uint message, UIntPtr wParam, IntPtr lParam, bool post = false,
			CancellationToken cancellationToken = default, uint timeoutMilliseconds = 5000) {
			return WindowMessageSender.SendMainWindowMessageAsync(identity, message, wParam, lParam, post, cancellationToken, timeoutMilliseconds);
		}

		/// <summary>Passes the calling process's foreground right to another process, such as a replacement it started.</summary>
		/// <remarks>Windows grants the right only while the caller may set the foreground itself (it owns the foreground or received
		/// the last input); returns false otherwise or when the process does not exist. The target still activates its own window.</remarks>
		public static bool AllowForeground(int processId) {
			Guard.ArgumentInRange(processId, 1, int.MaxValue, nameof(processId));
			return WinAPI.USER32.AllowSetForegroundWindow((uint)processId);
		}
	}
}
