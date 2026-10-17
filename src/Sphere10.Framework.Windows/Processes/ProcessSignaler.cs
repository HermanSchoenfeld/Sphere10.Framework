// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;

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
		Guard.Argument(signal is WinAPI.KERNEL32.CTRL_C_EVENT or WinAPI.KERNEL32.CTRL_BREAK_EVENT, nameof(signal), "Choose CTRL+C or CTRL+BREAK.");
		WinAPI.KERNEL32.FreeConsole();
		if (!WinAPI.KERNEL32.AttachConsole(processId))
			throw new InvalidOperationException($"Could not attach to console of process (PID {processId}). It may not be a console application.");
		WinAPI.KERNEL32.ControlHandler handler = _ => true;
		using var attachedConsole = Tools.Scope.ExecuteOnDispose(() => {
			WinAPI.KERNEL32.FreeConsole();
			WinAPI.KERNEL32.SetConsoleCtrlHandler(handler, false);
			GC.KeepAlive(handler);
		});
		if (!WinAPI.KERNEL32.SetConsoleCtrlHandler(handler, true))
			throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not protect the signal sender.");
		if (!WinAPI.KERNEL32.GenerateConsoleCtrlEvent(signal, 0))
			throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows rejected the console control signal.");
	}

	public string GetProcessName(int processId) {
		using var process = Process.GetProcessById(processId);
		return process.ProcessName;
	}

	public WindowsProcessInfo[] GetProcesses() => GetProcessesCore(null);

	public Task<WindowsProcessInfo[]> GetProcessesAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => GetProcessesCore(cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private static WindowsProcessInfo[] GetProcessesCore(Action checkpoint) {
		checkpoint?.Invoke();
		using var snapshot = WinAPI.KERNEL32.CreateToolhelp32Snapshot(WinAPI.KERNEL32.SnapshotProcesses, 0);
		if (snapshot.IsInvalid)
			throw new Win32Exception(Marshal.GetLastWin32Error());
		var entry = new WinAPI.KERNEL32.PROCESSENTRY32 { Size = (uint)Marshal.SizeOf<WinAPI.KERNEL32.PROCESSENTRY32>() };
		var result = new List<WindowsProcessInfo>();
		if (!WinAPI.KERNEL32.Process32First(snapshot, ref entry))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		do {
			checkpoint?.Invoke();
			result.Add(new WindowsProcessInfo((int)entry.ProcessId, (int)entry.ParentProcessId, entry.ExecutableFile, (int)entry.ThreadCount));
		} while (WinAPI.KERNEL32.Process32Next(snapshot, ref entry));
		var error = Marshal.GetLastWin32Error();
		if (error != WinAPI.KERNEL32.ERROR_NO_MORE_FILES)
			throw new Win32Exception(error);
		checkpoint?.Invoke();
		return result.ToArray();
	}
}
