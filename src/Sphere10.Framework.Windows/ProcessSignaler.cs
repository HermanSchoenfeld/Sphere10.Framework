// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Provides utility methods for sending window messages and console control signals to processes.
/// </summary>
public class ProcessSignaler {

	public const uint ENDSESSION_CLOSEAPP = 0x00000001;

	/// <summary>
	/// Sends or posts a window message to the specified window handle.
	/// </summary>
	/// <param name="hwnd">The target window handle.</param>
	/// <param name="msg">The window message identifier.</param>
	/// <param name="wParam">The WPARAM value.</param>
	/// <param name="lParam">The LPARAM value.</param>
	/// <param name="synchronous">If <c>true</c>, uses SendMessage (blocking); otherwise uses PostMessage (non-blocking).</param>
	public void SendWindowMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, bool synchronous = true) {
		if (synchronous)
			WinAPI.USER32.SendMessage(hwnd, msg, wParam, lParam);
		else
			WinAPI.USER32.PostMessage(hwnd, msg, wParam, lParam);
	}

	/// <summary>
	/// Sends or posts a window message to the main window of the specified process.
	/// </summary>
	/// <param name="processId">The target process ID.</param>
	/// <param name="msg">The window message identifier.</param>
	/// <param name="wParam">The WPARAM value.</param>
	/// <param name="lParam">The LPARAM value.</param>
	/// <param name="synchronous">If <c>true</c>, uses SendMessage (blocking); otherwise uses PostMessage (non-blocking).</param>
	/// <exception cref="InvalidOperationException">Thrown when the process has no main window.</exception>
	public void SendWindowMessage(int processId, uint msg, IntPtr wParam, IntPtr lParam, bool synchronous = true) {
		using var process = Process.GetProcessById(processId);
		var hwnd = process.MainWindowHandle;
		if (hwnd == IntPtr.Zero)
			throw new InvalidOperationException($"Process '{process.ProcessName}' (PID {process.Id}) has no main window.");

		SendWindowMessage(hwnd, msg, wParam, lParam, synchronous);
	}

	/// <summary>
	/// Sends a console control signal (e.g., CTRL+C or CTRL+BREAK) to the specified process.
	/// </summary>
	/// <param name="processId">The target process ID.</param>
	/// <param name="signal">The console control signal (use <see cref="WinAPI.KERNEL32.CTRL_C_EVENT"/> or <see cref="WinAPI.KERNEL32.CTRL_BREAK_EVENT"/>).</param>
	/// <exception cref="InvalidOperationException">Thrown when the console cannot be attached.</exception>
	public void SendConsoleSignal(int processId, uint signal) {
		WinAPI.KERNEL32.FreeConsole();
		if (!WinAPI.KERNEL32.AttachConsole(processId))
			throw new InvalidOperationException($"Could not attach to console of process (PID {processId}). It may not be a console application.");
		try {
			WinAPI.KERNEL32.SetConsoleCtrlHandler(IntPtr.Zero, true);
			WinAPI.KERNEL32.GenerateConsoleCtrlEvent(signal, 0);
			WinAPI.KERNEL32.SetConsoleCtrlHandler(IntPtr.Zero, false);
		} finally {
			WinAPI.KERNEL32.FreeConsole();
		}
	}

}
