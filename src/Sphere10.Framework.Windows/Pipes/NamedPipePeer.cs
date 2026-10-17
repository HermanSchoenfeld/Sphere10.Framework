// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

public static class NamedPipePeer {
	public static int GetClientProcessId(NamedPipeServerStream pipe) {
		Guard.ArgumentNotNull(pipe, nameof(pipe));
		if (!WinAPI.KERNEL32.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out var processId))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		return checked((int)processId);
	}

	public static int GetServerProcessId(NamedPipeClientStream pipe) {
		Guard.ArgumentNotNull(pipe, nameof(pipe));
		if (!WinAPI.KERNEL32.GetNamedPipeServerProcessId(pipe.SafePipeHandle, out var processId))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		return checked((int)processId);
	}
}
