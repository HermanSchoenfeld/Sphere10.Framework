// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Security.AccessControl;
using System.Runtime.InteropServices;
using System.IO;

namespace Sphere10.Framework.Windows.Security;

public class NTShare : NTLocalObject {
	private byte[] _securityDescriptorBytes;
	//_securityDescriptor; 


	internal NTShare() {
	}

	public WinAPI.NETAPI32.SHARE_TYPE Type { get; set; }

	public WinAPI.NETAPI32.GlobalSharePermission Permissions { get; set; }

	public int MaxUses { get; set; }

	public int CurrentUses { get; set; }

	public string ServerPath { get; set; }

	public string Password { get; set; }

	public int Reserved { get; set; }

	public IntPtr SecurityDescriptor { get; set; }

	/// <summary>Owned managed descriptor snapshot. Native inventory buffers never escape the read operation.</summary>
	public byte[] SecurityDescriptorBytes {
		get => _securityDescriptorBytes == null ? null : (byte[])_securityDescriptorBytes.Clone();
		set { _securityDescriptorBytes = value == null ? null : (byte[])value.Clone(); SecurityDescriptor = IntPtr.Zero; }
	}

	public override void Update() {
		Checkpoint();
		var descriptor = SecurityDescriptor;
		using var ownedDescriptor = descriptor == IntPtr.Zero && _securityDescriptorBytes?.Length > 0 ? new UnmanagedMemoryScope(_securityDescriptorBytes.Length) : default;
		if (ownedDescriptor.Item != IntPtr.Zero) {
			Marshal.Copy(_securityDescriptorBytes, 0, ownedDescriptor.Item, _securityDescriptorBytes.Length);
			descriptor = ownedDescriptor.Item;
		}
		var shareInfo = new WinAPI.NETAPI32.SHARE_INFO_502 {
			shi502_current_uses = (uint)CurrentUses,
			shi502_max_uses = (uint)MaxUses,
			shi502_netname = Name,
			shi502_passwd = Password,
			shi502_path = ServerPath,
			shi502_permissions = (uint)Permissions,
			shi502_remark = Description,
			shi502_reserved = (uint)Reserved,
			shi502_security_descriptor = descriptor,
			shi502_type = (uint)Type
		};


		#region Get security descriptor

		// WinAPI.NETAPI32.SECURITY_DESCRIPTOR secDescriptor;
		//byte[] buffer = new byte[SecurityDescriptor.BinaryLength];
		//SecurityDescriptor.GetBinaryForm(buffer, 0);
		//shareInfo.shi502_security_descriptor = (WinAPI.NETAPI32.SECURITY_DESCRIPTOR)buffer;

		#endregion

		uint parmError = 0;

		var result = WinAPI.NETAPI32.NetShareSetInfo(
			this.NTCompatibleHostName,
			this.Name,
			502,
			ref shareInfo,
			out parmError
		);

		if (result != WinAPI.NETAPI32.NET_API_STATUS.NERR_Success) {
			throw new NetApiException(
				result,
				"Unable to update share '{0}' on host '{1}'.",
				Name,
				Host
			);
		}
	}

	public override void Refresh() {
		var result = TryLoadLocalShare(NTCompatibleHostName, Name, this);

		#region Validation

		if (result != 0) {
			throw new NetApiException(
				result,
				"Unable to read share '{0}' on host '{1}'",
				Name,
				Host
			);
		}

		#endregion

	}

	public override void Delete() {
		Checkpoint();
		var result = WinAPI.NETAPI32.NetShareDel(
			this.NTCompatibleHostName,
			this.Name,
			0
		);

		if (result != WinAPI.NETAPI32.NET_API_STATUS.NERR_Success) {
			throw new NetApiException(
				result,
				"Unable to delete share '{0}' on host '{1}'",
				Name,
				Host
			);
		}
	}

	public override string FullName {
		get { return string.Format("{0}{1}{2}", NTCompatibleHostName, Path.DirectorySeparatorChar, Name); }
	}

	public ShareSecurity GetAccessControl() {
		return GetAccessControl(AccessControlSections.All);
	}

	public ShareSecurity GetAccessControl(AccessControlSections sections) {
		Checkpoint();
		return new ShareSecurity(FullName, sections);
	}

	public void SetAccessControl(ShareSecurity security) {
		Checkpoint();
		SetAccessControl(FullName, security);
	}

	public static ShareSecurity GetAccessControl(string path) {
		return new ShareSecurity(path, AccessControlSections.All);
	}

	public static void SetAccessControl(string path, ShareSecurity security) {
		security.Persist(path);
	}


	internal void CopyNativeInfo(string host, WinAPI.NETAPI32.SHARE_INFO_502 shareInfo) {
		CurrentUses = (int)shareInfo.shi502_current_uses;
		Description = shareInfo.shi502_remark;
		Host = host;
		MaxUses = (int)shareInfo.shi502_max_uses;
		Name = shareInfo.shi502_netname;
		Password = shareInfo.shi502_passwd;
		ServerPath = shareInfo.shi502_path;
		Permissions = (WinAPI.NETAPI32.GlobalSharePermission)shareInfo.shi502_permissions;
		Reserved = (int)shareInfo.shi502_reserved;
		SecurityDescriptor = IntPtr.Zero;
		Type = (WinAPI.NETAPI32.SHARE_TYPE)shareInfo.shi502_type;
		_securityDescriptorBytes = null;
		if (shareInfo.shi502_security_descriptor != IntPtr.Zero) {
			var length = checked((int)WinAPI.ADVAPI32.GetSecurityDescriptorLength(shareInfo.shi502_security_descriptor));
			_securityDescriptorBytes = new byte[length];
			Marshal.Copy(shareInfo.shi502_security_descriptor, _securityDescriptorBytes, 0, length);
		}
		Description = shareInfo.shi502_remark;

	}

	internal static WinAPI.NETAPI32.NET_API_STATUS TryLoadLocalShare(string host, string name, NTShare share) {
		WinAPI.NETAPI32.NET_API_STATUS result;
		var bufPtr = IntPtr.Zero;
		share.Checkpoint();
		result = WinAPI.NETAPI32.NetShareGetInfo(host, name, 502, out bufPtr);

		using var nativeBuffer = new NetApiBufferScope(bufPtr);
		if (result != WinAPI.NETAPI32.NET_API_STATUS.NERR_Success) {
			return result;
		}

		var shareInfo = (WinAPI.NETAPI32.SHARE_INFO_502)Marshal.PtrToStructure(
			bufPtr,
			typeof(WinAPI.NETAPI32.SHARE_INFO_502)
		);

		share.CopyNativeInfo(host, shareInfo);

		return result;
	}

}

