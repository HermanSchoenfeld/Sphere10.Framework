// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Sphere10.Framework.Windows.Security;

/// <summary>Runs the complete synchronous worker under supplied credentials and restores the caller's existing identity.</summary>
public class Impersonator {
	public static void RunAs(string userName, string domain, string password, Action code)
		=> RunAs(userName, domain, password, WinAPI.ADVAPI32.LOGON_TYPE.LOGON32_LOGON_INTERACTIVE,
			WinAPI.ADVAPI32.LOGON_PROVIDER.LOGON32_PROVIDER_DEFAULT, WinAPI.ADVAPI32.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, false, false, code);

	public static void RunAs(string userName, string domain, string password, WinAPI.ADVAPI32.LOGON_TYPE logonType,
		WinAPI.ADVAPI32.LOGON_PROVIDER logonProvider, WinAPI.ADVAPI32.SECURITY_IMPERSONATION_LEVEL impersonationLevel,
		bool usePrimaryToken, bool attemptEnableSeRestorePrivilege, Action code) {
		Guard.ArgumentNotNullOrEmpty(userName, nameof(userName));
		Guard.ArgumentNotNull(code, nameof(code));
		if (!WinAPI.ADVAPI32.LogonUser(userName, domain, password, (int)logonType, (int)logonProvider, out Microsoft.Win32.SafeHandles.SafeAccessTokenHandle token))
			throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not log on as '{domain}\\{userName}'.");
		using (token) {
			if (!WinAPI.ADVAPI32.DuplicateTokenEx(token, WinAPI.ADVAPI32.TokenQuery | WinAPI.ADVAPI32.TokenDuplicate | WinAPI.ADVAPI32.TokenImpersonate | WinAPI.ADVAPI32.TokenAdjustPrivileges,
				IntPtr.Zero, (int)impersonationLevel, usePrimaryToken ? 1 : 2, out var duplicate))
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not create the worker identity for '{userName}'.");
			using (duplicate) {
				if (attemptEnableSeRestorePrivilege && !Tools.Windows.Security.ModifyState(duplicate.DangerousGetHandle(), "SeRestorePrivilege", true))
					throw new Win32Exception(Marshal.GetLastWin32Error(), "The supplied identity does not hold SeRestorePrivilege.");
				WindowsIdentity.RunImpersonated(duplicate, code);
			}
		}
	}
}

