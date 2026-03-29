// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Provides utility methods for managing Windows security token privileges.
/// </summary>
public class PrivilegeUtil {

	public bool EnablePrivilege(string privilege) {
		return ModifyState(privilege, true);
	}

	public WinAPI.ADVAPI32.LUID LookupPrivilegeID(string name) {
		WinAPI.ADVAPI32.LUID privilegeId;
		if (!WinAPI.ADVAPI32.LookupPrivilegeValue(null, name, out privilegeId)) {
			throw new WindowsException(WinAPI.KERNEL32.GetLastError(), "Unable to retrieve privilege '{0}'", name);
		}
		return privilegeId;
	}

	public bool DisablePrivilege(string privilege) {
		return ModifyState(privilege, false);
	}

	public bool ModifyState(string privilege, bool enable) {
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

	public bool ModifyState(IntPtr token, string privilege, bool enable) {
		return ModifyState(token, LookupPrivilegeID(privilege), enable);
	}

	public bool ModifyState(IntPtr token, WinAPI.ADVAPI32.LUID privilegeID, bool enable) {
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
		if (WinAPI.ADVAPI32.AdjustTokenPrivileges(token, false, ref privilege, 1024, out retLengthInBytes)) {
			retval = true;
		}
		return retval;
	}

}
