// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using Sphere10.Framework.Windows.Security;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>
	/// Provides utility methods for managing Windows security token privileges.
	/// </summary>
	public static class Security {
		/// <summary>Enables required privileges on an operation-local effective token and restores the previous identity on disposal.
		/// Acquire and dispose synchronously on the same worker thread; do not hold across await.</summary>
		public static TokenPrivilegeScope EnterScope(params string[] privileges) {
			Guard.ArgumentNotNull(privileges, nameof(privileges));
			return privileges.Length == 0 ? default : new TokenPrivilegeScope((string[])privileges.Clone());
		}

		/// <summary>Attempts operation-local privilege admission. Missing privileges return false; other native failures are reported.</summary>
		public static bool TryEnterScope(string[] privileges, out TokenPrivilegeScope scope) {
			TokenPrivilegeScope acquired = default;
			Tools.Exceptions.ExecuteCapturingException<System.ComponentModel.Win32Exception>(() => acquired = EnterScope(privileges), out var error,
				failure => failure.NativeErrorCode == WinAPI.ADVAPI32.ErrorNotAllAssigned);
			scope = acquired;
			return error == null;
		}

		public static bool EnablePrivilege(string privilege) => ModifyState(privilege, true);

		public static WinAPI.ADVAPI32.LUID LookupPrivilegeID(string name) {
			WinAPI.ADVAPI32.LUID privilegeId;
			if (!WinAPI.ADVAPI32.LookupPrivilegeValue(null, name, out privilegeId)) {
				throw new WindowsException(WinAPI.KERNEL32.GetLastError(), "Unable to retrieve privilege '{0}'", name);
			}
			return privilegeId;
		}

		public static bool DisablePrivilege(string privilege) => ModifyState(privilege, false);

		public static bool ModifyState(string privilege, bool enable) {
			IntPtr processToken = WinAPI.KERNEL32.GetCurrentProcess();
			IntPtr hToken = IntPtr.Zero;
			try {
				if (!WinAPI.ADVAPI32.OpenProcessToken(
					    processToken,
					    WinAPI.NETAPI32.TOKEN_ADJUST_PRIVILEGES,
					    out hToken
				    )) {
					return false;
				}
				return ModifyState(hToken, LookupPrivilegeID(privilege), enable);
			} finally {
				if (hToken != IntPtr.Zero) {
					WinAPI.KERNEL32.CloseHandle(hToken);
				}
			}
		}

		public static bool ModifyState(IntPtr token, string privilege, bool enable) => ModifyState(token, LookupPrivilegeID(privilege), enable);

		public static bool ModifyState(IntPtr token, WinAPI.ADVAPI32.LUID privilegeID, bool enable) {
			bool retval = false;
			WinAPI.ADVAPI32.TOKEN_PRIVILEGES privilege;
			privilege.PrivilegeCount = 1;
			privilege.Privileges = new WinAPI.ADVAPI32.LUID_AND_ATTRIBUTES[privilege.PrivilegeCount];
			privilege.Privileges[0].Luid = privilegeID;
			if (enable) {
				privilege.Privileges[0].Attributes = WinAPI.NETAPI32.SE_PRIVILEGE_ENABLED;
			} else {
				privilege.Privileges[0].Attributes = 0;
			}
			uint retLengthInBytes;
			if (WinAPI.ADVAPI32.AdjustTokenPrivileges(token, false, ref privilege, 0, out retLengthInBytes)) {
				retval = Marshal.GetLastWin32Error() == 0;
			}
			return retval;
		}

	}
}
