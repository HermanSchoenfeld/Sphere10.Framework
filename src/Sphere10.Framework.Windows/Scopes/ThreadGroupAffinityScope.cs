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

/// <summary>Temporarily pins the current native thread to one processor-group mask and restores its exact previous affinity.</summary>
/// <remarks>Use synchronously on the creating thread, nested in reverse disposal order. Never hold this scope across an await.</remarks>
public readonly struct ThreadGroupAffinityScope : IScope {
	private readonly WinAPI.KERNEL32.GROUP_AFFINITY _previous;
	private readonly int _threadId;

	public ThreadGroupAffinityScope(ushort group, ulong mask) {
		Guard.Argument(mask != 0, nameof(mask), "Select at least one processor of the group.");
		Guard.Argument(IntPtr.Size == 8 || mask <= uint.MaxValue, nameof(mask), "A 32-bit process addresses at most 32 processors per group.");
		_threadId = Environment.CurrentManagedThreadId;
		var selected = new WinAPI.KERNEL32.GROUP_AFFINITY { Group = group, Mask = (UIntPtr)mask };
		if (!WinAPI.KERNEL32.SetThreadGroupAffinity(WinAPI.KERNEL32.GetCurrentThread(), ref selected, out _previous))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not set the thread group affinity.");
	}

	public void Dispose() {
		// A default instance never entered a scope; a real previous affinity always has at least one processor.
		if (_previous.Mask == UIntPtr.Zero)
			return;
		Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "Thread group affinity must be restored on the thread that entered its scope.");
		var previous = _previous;
		if (!WinAPI.KERNEL32.SetThreadGroupAffinity(WinAPI.KERNEL32.GetCurrentThread(), ref previous, out _))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not restore the thread group affinity.");
	}

	public ValueTask DisposeAsync() => ValueTask.FromException(new NotSupportedException("This scope does not support async"));
}
