// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Sphere10.Framework.Windows;

/// <summary>Owns an explicit WMI COM connection on its calling thread to support Native AOT without runtime COM marshalling.</summary>
public sealed class WindowsManagementQuery : SyncDisposable {
	private readonly int _threadId;
	private readonly bool _uninitialize;
	private readonly int _timeoutMilliseconds;
	private IntPtr _services;

	internal WindowsManagementQuery(TimeSpan timeout, string scope = @"\\.\root\cimv2") {
		Guard.Argument(timeout > TimeSpan.Zero && timeout.TotalMilliseconds <= int.MaxValue, nameof(timeout), "The query timeout must be positive and fit native milliseconds.");
		_threadId = Environment.CurrentManagedThreadId;
		_timeoutMilliseconds = checked((int)Math.Ceiling(timeout.TotalMilliseconds));
		var initialized = WinAPI.OLE32.CoInitializeEx(IntPtr.Zero, 0);
		if (initialized != unchecked((int)0x80010106))
			Marshal.ThrowExceptionForHR(initialized);
		_uninitialize = initialized >= 0;
		try {
			var locator = Tools.Windows.COM.CreateManagementLocator();
			using var releaseLocator = Tools.Scope.ExecuteOnDispose(() => Marshal.Release(locator));
			var nativeScope = Marshal.StringToBSTR(scope);
			using var freeScope = Tools.Scope.ExecuteOnDispose(() => Marshal.FreeBSTR(nativeScope));
			Check(WinAPI.WBEMCLI.ConnectServer(locator, nativeScope, out _services));
			Check(WinAPI.OLE32.CoSetProxyBlanket(_services, 10, 0, IntPtr.Zero, 3, 3, IntPtr.Zero, 0));
		} catch {
			DisposeInternal();
			throw;
		}
	}

	public IEnumerable<WindowsManagementObject> Read(string query) {
		CheckThread();
		var language = Marshal.StringToBSTR("WQL");
		using var freeLanguage = Tools.Scope.ExecuteOnDispose(() => Marshal.FreeBSTR(language));
		var nativeQuery = Marshal.StringToBSTR(query);
		using var freeQuery = Tools.Scope.ExecuteOnDispose(() => Marshal.FreeBSTR(nativeQuery));
		Check(WinAPI.WBEMCLI.ExecQuery(_services, language, nativeQuery, out var enumerator));
		using var releaseEnumerator = Tools.Scope.ExecuteOnDispose(() => Marshal.Release(enumerator));
		for (;;) {
			var status = WinAPI.WBEMCLI.Next(enumerator, _timeoutMilliseconds, out var pointer, out var count);
			if (status < 0 || status == 0x40004) {
				if (pointer != IntPtr.Zero)
					Marshal.Release(pointer);
				Check(status);
			}
			if (count == 0)
				yield break;
			yield return new WindowsManagementObject(pointer, this);
		}
	}

	public WindowsManagementObject GetObject(string path) {
		CheckThread();
		var nativePath = Marshal.StringToBSTR(path);
		using var freePath = Tools.Scope.ExecuteOnDispose(() => Marshal.FreeBSTR(nativePath));
		Check(WinAPI.WBEMCLI.GetObject(_services, nativePath, out var item, out var call));
		return Complete(item, call, _timeoutMilliseconds);
	}

	internal WindowsManagementObject Invoke(string path, string method, WindowsManagementObject input, TimeSpan timeout) {
		CheckThread();
		var nativePath = Marshal.StringToBSTR(path);
		using var freePath = Tools.Scope.ExecuteOnDispose(() => Marshal.FreeBSTR(nativePath));
		var nativeMethod = Marshal.StringToBSTR(method);
		using var freeMethod = Tools.Scope.ExecuteOnDispose(() => Marshal.FreeBSTR(nativeMethod));
		Check(WinAPI.WBEMCLI.ExecMethod(_services, nativePath, nativeMethod, input?.Pointer ?? IntPtr.Zero, out var item, out var call));
		return Complete(item, call, checked((int)Math.Ceiling(timeout.TotalMilliseconds)));
	}

	internal static void Check(int result) {
		if (result == 0x40004)
			throw CreateManagementException(ManagementStatus.Timedout, "The WMI provider did not return within the query timeout.", null);
		if (result < 0 && (result & unchecked((int)0xFFFFF000)) == unchecked((int)0x80041000))
			throw CreateManagementException((ManagementStatus)result, Marshal.GetExceptionForHR(result)?.Message, null);
		Marshal.ThrowExceptionForHR(result);
	}

	protected override void DisposeInternal() {
		CheckThread();
		if (_services != IntPtr.Zero) {
			Marshal.Release(_services);
			_services = IntPtr.Zero;
		}
		if (_uninitialize)
			WinAPI.OLE32.CoUninitialize();
	}

	private WindowsManagementObject Complete(IntPtr item, IntPtr call, int timeout) {
		using var releaseCall = Tools.Scope.ExecuteOnDispose(() => { if (call != IntPtr.Zero) Marshal.Release(call); });
		try {
			if (call != IntPtr.Zero) {
				if (item != IntPtr.Zero)
					Marshal.Release(item);
				item = IntPtr.Zero;
				Check(WinAPI.WBEMCLI.GetResultObject(call, timeout, out item));
			}
			return item == IntPtr.Zero ? null : new WindowsManagementObject(item, this);
		} catch {
			if (item != IntPtr.Zero)
				Marshal.Release(item);
			throw;
		}
	}

	// Public constructors discard the provider code. This direct accessor retains the existing exception contract without runtime reflection or COM activation.
	[UnsafeAccessor(UnsafeAccessorKind.Constructor)]
	private static extern ManagementException CreateManagementException(ManagementStatus errorCode, string message, ManagementBaseObject errorInformation);

	private void CheckThread() => Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "WMI connection use and disposal must stay on the acquiring thread.");
}
