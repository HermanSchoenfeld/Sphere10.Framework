// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

/// <summary>Owns an opened kernel device handle and dispatches its control codes. Opening a device neither installs nor starts a driver.</summary>
/// <remarks>Device IO blocks the calling thread; dispatch it from a worker. Consumers own the control codes, buffers and response validation of their driver.</remarks>
public sealed class WindowsDevice : IDisposable {
	private SafeFileHandle _handle;

	private WindowsDevice(SafeFileHandle handle, string path) {
		_handle = handle;
		Path = path;
	}

	/// <summary>The device path this instance opened, such as <c>\\.\Name</c> or <c>\\?\GLOBALROOT\Device\Name</c>.</summary>
	public string Path { get; }

	/// <summary>Opens a device path; the Windows error is thrown unchanged when the caller cannot open it.</summary>
	public static WindowsDevice Open(string path, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.ReadWrite) {
		Guard.ArgumentNotNullOrWhitespace(path, nameof(path));
		var handle = WinAPI.KERNEL32.CreateFileW(path, access, share, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
		if (!handle.IsInvalid)
			return new WindowsDevice(handle, path);
		var error = Marshal.GetLastPInvokeError();
		handle.Dispose();
		throw new Win32Exception(error, $"The device '{path}' could not be opened.");
	}

	/// <summary>Reports the Windows error of opening a device, or zero when the caller can open it; the probe handle is closed at once.</summary>
	public static int ProbeAccess(string path, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.ReadWrite) {
		Guard.ArgumentNotNullOrWhitespace(path, nameof(path));
		using var handle = WinAPI.KERNEL32.CreateFileW(path, access, share, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
		return handle.IsInvalid ? Marshal.GetLastPInvokeError() : 0;
	}

	/// <summary>Dispatches one control code and reports the bytes the driver returned. A false result leaves the Windows error in <see cref="Marshal.GetLastPInvokeError"/>.</summary>
	public bool TryControl(uint code, byte[] input, byte[] output, out int returned) {
		ObjectDisposedException.ThrowIf(_handle == null, this);
		return WinAPI.KERNEL32.DeviceIoControl(_handle, code, input, input?.Length ?? 0, output, output?.Length ?? 0, out returned, IntPtr.Zero);
	}

	/// <summary>Dispatches one control code and throws the Windows error when the driver rejects it.</summary>
	public int Control(uint code, byte[] input, byte[] output = null) {
		if (!TryControl(code, input, output, out var returned))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Control code 0x{code:X8} failed on '{Path}'.");
		return returned;
	}

	public void Dispose() {
		var handle = _handle;
		_handle = null;
		handle?.Dispose();
	}
}
