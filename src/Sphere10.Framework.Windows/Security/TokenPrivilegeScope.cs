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
using Microsoft.Win32.SafeHandles;

namespace Sphere10.Framework.Windows.Security;

// Operation-local privileges live on a duplicate effective token. Restore the exact prior thread token;
// never enable privileges globally on the application process for a migration worker.
public readonly struct TokenPrivilegeScope : IScope {
	private readonly int _threadId;
	private readonly SafeAccessTokenHandle _previous;
	private readonly SafeAccessTokenHandle _token;

	public TokenPrivilegeScope(string[] privileges) {
		Guard.ArgumentNotNull(privileges, nameof(privileges));
		_threadId = Environment.CurrentManagedThreadId;
		(_previous, _token) = Acquire(privileges);
	}

	public void Dispose() {
		if (_token is null && _previous is null)
			return;
		Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "Dispose the privilege scope on its acquiring worker thread; never hold it across await.");
		try {
			if (!WinAPI.ADVAPI32.SetThreadToken(IntPtr.Zero, _previous?.DangerousGetHandle() ?? IntPtr.Zero))
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not restore the worker's original security token.");
		} finally {
			_token?.Dispose();
			_previous?.Dispose();
		}
	}

	public ValueTask DisposeAsync() => ValueTask.FromException(new NotSupportedException("This scope does not support async"));

	private static (SafeAccessTokenHandle Previous, SafeAccessTokenHandle Token) Acquire(string[] privileges) {
		SafeAccessTokenHandle previous = null;
		SafeAccessTokenHandle token = null;
		var installed = false;
		SafeAccessTokenHandle effective = null;
		using var rollback = Tools.Scope.ExecuteOnDispose(() => {
			if (!installed) {
				token?.Dispose();
				previous?.Dispose();
			}
		});
		using (Tools.Scope.ExecuteOnDispose(() => {
			if (effective != previous)
				effective?.Dispose();
		})) {
			if (WinAPI.ADVAPI32.OpenThreadToken(WinAPI.KERNEL32.GetCurrentThread(), WinAPI.ADVAPI32.TokenQuery | WinAPI.ADVAPI32.TokenDuplicate | WinAPI.ADVAPI32.TokenImpersonate, true, out previous)) {
				effective = previous;
			} else {
				var error = Marshal.GetLastWin32Error();
				previous?.Dispose();
				previous = null;
				if (error != WinAPI.ADVAPI32.ErrorNoToken)
					throw new Win32Exception(error);
				if (!WinAPI.ADVAPI32.OpenProcessToken(WinAPI.KERNEL32.GetCurrentProcess(), WinAPI.ADVAPI32.TokenQuery | WinAPI.ADVAPI32.TokenDuplicate, out effective))
					throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			if (!WinAPI.ADVAPI32.DuplicateTokenEx(effective, WinAPI.ADVAPI32.TokenQuery | WinAPI.ADVAPI32.TokenAdjustPrivileges | WinAPI.ADVAPI32.TokenImpersonate,
				IntPtr.Zero, 2, 2, out token))
				throw new Win32Exception(Marshal.GetLastWin32Error());
			foreach (var privilege in privileges) {
				Guard.ArgumentNotNullOrEmpty(privilege, nameof(privileges));
				if (!WinAPI.ADVAPI32.LookupPrivilegeValue(null, privilege, out var luid))
					throw new Win32Exception(Marshal.GetLastWin32Error(), $"Cannot locate privilege '{privilege}'.");
				var state = new WinAPI.ADVAPI32.TokenPrivilege { Count = 1, Luid = luid, Attributes = 2 };
				if (!WinAPI.ADVAPI32.AdjustTokenPrivileges(token, false, ref state, (uint)Marshal.SizeOf<WinAPI.ADVAPI32.TokenPrivilege>(), out _, out _))
					throw new Win32Exception(Marshal.GetLastWin32Error(), $"Cannot enable privilege '{privilege}'.");
				var error = Marshal.GetLastWin32Error();
				if (error != 0)
					throw new Win32Exception(error, $"The current identity does not hold privilege '{privilege}'. Run as an authorized administrator or choose another identity.");
			}
			if (!WinAPI.ADVAPI32.SetThreadToken(IntPtr.Zero, token.DangerousGetHandle()))
				throw new Win32Exception(Marshal.GetLastWin32Error());
			installed = true;
		}
		return (previous, token);
	}
}
