// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Security.AccessControl;

using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Sphere10.Framework.Windows.Security;

public class SecurityTool {
	public static readonly IActionObserver DefaultActionObserver = new NullActionObserver();


	private class NullActionObserver : IActionObserver {

		#region IActionObserver Members

		public void NotifyAction(string actionName, string objectType, string sourceName, string destName) {
		}

		public void NotifyActionFailed(string action, string objectType, string sourceName, string destName, string reason) {
		}

		public void NotifyInformation(string info, params object[] formatArgs) {
		}

		public void NotifyWarning(string info, params object[] formatArgs) {
		}

		public void NotifyError(string info, params object[] formatArgs) {
		}

		public void NotifyCompleted() {
		}

		#endregion

	}


	/// <summary>
	/// Changes the owner of an object or directory. Make sure SeRestore security is on.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="sid"></param>
	public static void ChangeObjectOwner(string path, SecurityIdentifier sid) {
		var sidPtr = IntPtr.Zero;

		try {
			if (WinAPI.ADVAPI32.ConvertStringSidToSid(sid.ToString(), out sidPtr)) {
				WinAPI.ADVAPI32.SECURITY_DESCRIPTOR secDesc;

				if (!WinAPI.ADVAPI32.InitializeSecurityDescriptor(out secDesc, WinAPI.ADVAPI32.SECURITY_DESCRIPTOR_REVISION)) {
					throw new SoftwareException(
						"Unable to change owner of object '{0}' to SID '{1}'. Applying file/directory security descriptor failed.",
						path,
						sid
					);
				}

				if (!WinAPI.ADVAPI32.SetSecurityDescriptorOwner(ref secDesc, sidPtr, false)) {
					throw new SoftwareException(
						"Unable to change owner of object '{0}' to SID '{1}'. Applying file/directory security descriptor failed.",
						path,
						sid
					);
				}
				if (!WinAPI.ADVAPI32.SetFileSecurity(path, WinAPI.ADVAPI32.OWNER_SECURITY_INFORMATION, ref secDesc)) {
					int error = WinAPI.KERNEL32.GetLastError();
					throw new SoftwareException(
						"Unable to change owner of object '{0}' to SID '{1}'. Applying file/directory security descriptor failed.",
						path,
						sid
					);
				}


			}
		} finally {
			if (sidPtr != IntPtr.Zero) {
				WinAPI.KERNEL32.LocalFree(sidPtr);
			}
		}


	}


	public static string GetUserFromDomainUserString(string domainUser) {
		string[] splits = domainUser.Split('\\');
		Debug.Assert(splits.Length > 0);
		if (splits.Length == 1) {
			return domainUser;
		}
		;
		return splits[1];
	}

	public static string GetDomainFromDomainUserString(string domainUser) {
		string[] splits = domainUser.Split('\\');
		Debug.Assert(splits.Length > 0);
		if (splits.Length == 1) {
			return domainUser;
		}
		;
		return splits[0];
	}


	public static RawSecurityDescriptor ConvertSecurityDescriptor(WinAPI.ADVAPI32.SECURITY_DESCRIPTOR descriptor) {
		throw new NotImplementedException();
	}



	/// <summary>Returns the current Windows user's SID without retaining the native identity handle.</summary>
	public static SecurityIdentifier GetCurrentUserSid() {
		using var identity = WindowsIdentity.GetCurrent();
		return identity.User ?? throw new InvalidOperationException("The current Windows identity has no user SID.");
	}

	/// <summary>Reads the requested security sections without modifying the file.</summary>
	public static FileSecurity GetFileSecurity(string path, AccessControlSections sections) {
		Guard.ArgumentNotNullOrEmpty(path, nameof(path));
		return new FileInfo(path).GetAccessControl(sections);
	}

	/// <summary>Writes only the sections marked as changed in the supplied descriptor.</summary>
	public static void SetFileSecurity(string path, FileSecurity security) {
		Guard.ArgumentNotNullOrEmpty(path, nameof(path));
		Guard.ArgumentNotNull(security, nameof(security));
		new FileInfo(path).SetAccessControl(security);
	}

	public static DirectorySecurity GetDirectorySecurity(string path, AccessControlSections sections) {
		Guard.ArgumentNotNullOrEmpty(path, nameof(path));
		return new DirectoryInfo(path).GetAccessControl(sections);
	}

	public static void SetDirectorySecurity(string path, DirectorySecurity security) {
		Guard.ArgumentNotNullOrEmpty(path, nameof(path));
		Guard.ArgumentNotNull(security, nameof(security));
		new DirectoryInfo(path).SetAccessControl(security);
	}

	/// <summary>Reads security through an owned read-only registry handle.</summary>
	public static RegistrySecurity GetRegistrySecurity(string host, string path, AccessControlSections sections) {
		using var key = Tools.Windows.Registry.OpenKey(host, path, false);
		return key.GetAccessControl(sections);
	}

	/// <summary>Opens the existing key with the rights required by the changed descriptor sections.</summary>
	public static void SetRegistrySecurity(string host, string path, RegistrySecurity security, AccessControlSections changedSections) {
		Guard.ArgumentNotNull(security, nameof(security));
		if (changedSections == AccessControlSections.None)
			return;
		var rights = RegistryRights.ReadKey;
		if ((changedSections & (AccessControlSections.Access | AccessControlSections.Audit)) != 0)
			rights |= RegistryRights.ChangePermissions;
		if ((changedSections & (AccessControlSections.Owner | AccessControlSections.Group)) != 0)
			rights |= RegistryRights.TakeOwnership;
		var update = new RegistrySecurity();
		update.SetSecurityDescriptorBinaryForm(security.GetSecurityDescriptorBinaryForm(), changedSections);
		using var key = Tools.Windows.Registry.OpenKey(host, path, rights);
		key.SetAccessControl(update);
	}

	/// <summary>Performs one account-name lookup on the selected host. Failures return the native Windows error code.</summary>
	public static bool TryLookupAccountName(string host, string accountName, out SecurityIdentifier sid, out string domain,
		out WinAPI.ADVAPI32.SidNameUse usage, out int win32Error) {
		Guard.ArgumentNotNullOrEmpty(accountName, nameof(accountName));
		sid = null;
		domain = null;
		usage = WinAPI.ADVAPI32.SidNameUse.Invalid;
		win32Error = 0;
		uint sidLength = 0, domainLength = 0;
		if (!WinAPI.ADVAPI32.LookupAccountName(host, accountName, IntPtr.Zero, ref sidLength, null, ref domainLength, out var nativeUsage)) {
			win32Error = Marshal.GetLastPInvokeError();
			if (win32Error != 122)
				return false;
		}
		if (sidLength == 0) {
			win32Error = 87;
			return false;
		}
		using (var allocation = new UnmanagedMemoryScope(checked((int)sidLength))) {
			var pointer = allocation.Item;
			var domainBuffer = new StringBuilder(checked((int)Math.Max(1, domainLength)));
			if (!WinAPI.ADVAPI32.LookupAccountName(host, accountName, pointer, ref sidLength, domainBuffer, ref domainLength, out nativeUsage)) {
				win32Error = Marshal.GetLastPInvokeError();
				return false;
			}
			sid = new SecurityIdentifier(pointer);
			domain = domainBuffer.ToString();
			usage = nativeUsage;
			win32Error = 0;
			return true;
		}
	}

	/// <summary>Performs one SID lookup on the selected host. ERROR_NONE_MAPPED remains distinct from permission and network failures.</summary>
	public static bool TryLookupAccountSid(string host, string sid, out string name, out string domain,
		out WinAPI.ADVAPI32.SidNameUse usage, out int win32Error) {
		var identifier = new SecurityIdentifier(sid);
		var bytes = new byte[identifier.BinaryLength];
		identifier.GetBinaryForm(bytes, 0);
		using var allocation = new UnmanagedMemoryScope(bytes.Length);
		var pointer = allocation.Item;
		name = domain = null;
		usage = WinAPI.ADVAPI32.SidNameUse.Invalid;
		win32Error = 0;
		{
			Marshal.Copy(bytes, 0, pointer, bytes.Length);
			uint nameLength = 0, domainLength = 0;
			if (!WinAPI.ADVAPI32.LookupAccountSidW(host, pointer, null, ref nameLength, null, ref domainLength, out var nativeUsage)) {
				win32Error = Marshal.GetLastPInvokeError();
				if (win32Error != 122)
					return false;
			}
			var nameBuffer = new StringBuilder(checked((int)Math.Max(1, nameLength)));
			var domainBuffer = new StringBuilder(checked((int)Math.Max(1, domainLength)));
			if (!WinAPI.ADVAPI32.LookupAccountSidW(host, pointer, nameBuffer, ref nameLength, domainBuffer, ref domainLength, out nativeUsage)) {
				win32Error = Marshal.GetLastPInvokeError();
				return false;
			}
			name = nameBuffer.ToString();
			domain = domainBuffer.ToString();
			usage = nativeUsage;
			win32Error = 0;
			return true;
		}
	}
}

