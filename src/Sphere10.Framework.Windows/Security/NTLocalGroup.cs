// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Linq;


namespace Sphere10.Framework.Windows.Security;

/// <summary>
/// Encapsulates a Local Group from some host machine. A local group can have local user, domain user, and
/// domain groups as members
/// </summary>
public class NTLocalGroup : NTLocalObject {

	/// <summary>
	/// Gets all the members of the group which are other local users or local groups.
	/// </summary>
	/// <returns></returns>
	public NTLocalObject[] GetLocalMembers() {
		return (
			from obj in GetMembers()
			where obj is NTLocalObject
			select (NTLocalObject)obj
		).ToArray();

	}

	/// <summary>
	/// Gets all the members which are active directory objects.
	/// </summary>
	/// <returns></returns>
	public NTRemoteObject[] GetRemoteMembers() {
		return (
			from obj in GetMembers()
			where obj is NTRemoteObject
			select (NTRemoteObject)obj
		).ToArray();

	}

	/// <summary>
	/// Gets all the members which have SIDs but cannot be resolved.
	/// </summary>
	/// <returns></returns>
	public NTDanglingObject[] GetDanglingMembers() {
		return (
			from obj in GetMembers()
			where obj is NTDanglingObject
			select (NTDanglingObject)obj
		).ToArray();
	}

	/// <summary>
	/// Gets all the members belonging to this group.
	/// </summary>
	/// <returns>List of members.</returns>
	public NTObject[] GetMembers() {
		var resume = UIntPtr.Zero;
		return NetApiEnumeration.Read<WinAPI.NETAPI32.LOCALGROUP_MEMBERS_INFO_2, NTObject>(
			(out IntPtr buffer, out uint count) => WinAPI.NETAPI32.NetLocalGroupGetMembersPaged(NTCompatibleHostName, Name, 2, out buffer, WinAPI.NETAPI32.MAX_PREFERRED_LENGTH, out count, out _, ref resume),
			info => {
				var fullName = Marshal.PtrToStringUni(info.lgrmi2_domainandname) ?? string.Empty;
				var domain = SecurityTool.GetDomainFromDomainUserString(fullName);
				var name = SecurityTool.GetUserFromDomainUserString(fullName);
				var sid = new SecurityIdentifier(info.lgrmi2_sid);
				var usage = (WinAPI.ADVAPI32.SidNameUse)info.lgrmi2_sidusage;
				if (usage == WinAPI.ADVAPI32.SidNameUse.DeletedAccount || usage == WinAPI.ADVAPI32.SidNameUse.Unknown)
					return new NTDanglingObject(Host, sid, usage);
				if (!string.Equals(domain, Host, StringComparison.OrdinalIgnoreCase))
					return new NTRemoteObject(Host, domain, name, sid, usage);
				NTLocalObject member;
				if (usage == WinAPI.ADVAPI32.SidNameUse.User)
					member = new NTLocalUser();
				else if (usage == WinAPI.ADVAPI32.SidNameUse.Group || usage == WinAPI.ADVAPI32.SidNameUse.Alias || usage == WinAPI.ADVAPI32.SidNameUse.WellKnownGroup)
					member = new NTLocalGroup();
				else
					return new NTDanglingObject(Host, sid, usage);
				member.Host = Host;
				member.Name = name;
				member.SID = sid;
				member.Checkpoint = Checkpoint;
				member.Refresh();
				return member;
			}, Checkpoint);
	}
	/// <summary>
	/// Adds a user to the group. The user could be a local or domain user.
	/// </summary>
	/// <param name="domainAndName">The username referenced by 'domain\name'.</param>
	/// <remarks>The domain could be the name of an accessible domain or hostmachine.</remarks>
	public void AddMember(string domainAndName) {
		Checkpoint();
		var newGroupMember = new WinAPI.NETAPI32.LOCALGROUP_MEMBERS_INFO_3 {
			lgrmi3_domainandname = domainAndName
		};
		var result = WinAPI.NETAPI32.NetLocalGroupAddMembers(this.NTCompatibleHostName, this.Name, 3, ref newGroupMember, 1);

		if (result != 0) {
			throw new NetApiException(
				result,
				"Unable to add member '{0}' to group '{1}' on host '{2}'",
				domainAndName,
				Name,
				Host
			);
		}
	}

	/// <summary>
	/// Adds a local user to the group. The user is local to the host this group is defined in (HOST).
	/// </summary>
	/// <param name="username">The username referenced by 'domain\name'.</param>
	/// <remarks>The domain could be the name of an accessible domain or hostmachine.</remarks>
	public void AddLocalMember(string username) {
		AddMember(string.Format("{0}\\{1}", Host, username));
	}

	public void AddLocalMember(NTLocalUser localUser) {
		if (localUser.Host.ToUpper() != Host.ToUpper()) {
			throw new WindowsException(
				"Could not add user '{0}\\{1}' to group '{1}\\{2}' as the two objects differed by owner host",
				localUser.Host,
				localUser.Name,
				Host,
				Name
			);
		}
		AddLocalMember(localUser.Name);

	}

	/// <summary>
	/// Deletes a member from the security group.
	/// </summary>
	/// <param name="obj">An object returned by one of the get methods</param>
	public void DeleteMember(NTObject obj) {
		// delete by SID if available
		if (obj.SID != null) {
			DeleteMember(obj.SID);
		} else {
			var toDelete = obj.Name;
			if (obj is NTRemoteObject) {
				toDelete = ((NTRemoteObject)obj).Domain + "\\" + obj.Name;
			}
			DeleteMember(toDelete);
		}
	}

	public void DeleteMember(SecurityIdentifier sid) {
		Checkpoint();
		var groupMember = new WinAPI.NETAPI32.LOCALGROUP_MEMBERS_INFO_0();
		var sidPtr = IntPtr.Zero;
		using var nativeSid = Tools.Scope.ExecuteOnDispose(() => {
			if (sidPtr != IntPtr.Zero)
				WinAPI.KERNEL32.LocalFree(sidPtr);
		});
		if (WinAPI.ADVAPI32.ConvertStringSidToSid(sid.ToString(), out sidPtr)) {
			groupMember.lgrmi0_sid = sidPtr;
			var result = WinAPI.NETAPI32.NetLocalGroupDelMembers(this.NTCompatibleHostName, this.Name, 0, ref groupMember, 1);
			if (result != 0) {
				throw new NetApiException(
					result,
					"Unable to delete member with SID '{0}' from group '{1}' on host '{2}'",
					sid,
					Name,
					Host
				);
			}
		} else {
			throw new WindowsException(
				"Unable to delete member with SID '{0}' from group '{1}' on host '{2}'. Could not convert SID to native SID structure.",
				sid,
				Name,
				Host
			);
		}
	}

	public void DeleteLocalMember(string name) {
		DeleteMember(string.Format("{0}\\{1}", Host, name));
	}

	public void DeleteMember(string domainAndName) {
		Checkpoint();
		var groupMember = new WinAPI.NETAPI32.LOCALGROUP_MEMBERS_INFO_3 {
			lgrmi3_domainandname = domainAndName
		};

		var result = WinAPI.NETAPI32.NetLocalGroupDelMembers(
			this.NTCompatibleHostName,
			this.Name,
			3,
			ref groupMember,
			1
		);

		if (result != 0) {
			throw new NetApiException(
				result,
				"Unable to delete member '{0}' from group '{1}' on host '{2}'",
				groupMember.lgrmi3_domainandname,
				Name,
				Host
			);
		}
	}

	public override void Update() {
		Checkpoint();
		var groupInfo = new WinAPI.NETAPI32.LOCALGROUP_INFO_1 {
			lgrpi1_name = this.Name,
			lgrpi1_comment = this.Description
		};
		const int parmError = 0;

		var result = WinAPI.NETAPI32.NetLocalGroupSetInfo(this.NTCompatibleHostName, this.Name, 1, ref groupInfo, parmError);

		if (result != 0) {
			throw new NetApiException(
				result,
				"Unable to update group '{0}' on host '{1}'",
				Name,
				Host
			);
		}

	}

	public override void Refresh() {
		var result = TryLoadLocalGroup(NTCompatibleHostName, Name, this);

		#region Validation

		if (result != 0) {
			throw new NetApiException(
				result,
				"Unable to read group '{0}' on host '{1}'",
				Name,
				Host
			);
		}

		#endregion

	}

	public override void Delete() {
		Checkpoint();
		var result = WinAPI.NETAPI32.NetLocalGroupDel(this.NTCompatibleHostName, this.Name);

		if (result != 0) {
			throw new NetApiException(
				result,
				"Unable to delete group '{0}' on host '{1}'",
				Name,
				Host
			);
		}
	}

	internal static WinAPI.NETAPI32.NET_API_STATUS TryLoadLocalGroup(string host, string name, NTLocalGroup group) {
		WinAPI.NETAPI32.NET_API_STATUS result;
		var bufPtr = IntPtr.Zero;
		group.Checkpoint();
		result = WinAPI.NETAPI32.NetLocalGroupGetInfo(host, name, 1, out bufPtr);

		using var nativeBuffer = new NetApiBufferScope(bufPtr);
		if (result != 0) {
			return result;
		}

		var groupInfo = (WinAPI.NETAPI32.LOCALGROUP_INFO_1)Marshal.PtrToStructure(
			bufPtr,
			typeof(WinAPI.NETAPI32.LOCALGROUP_INFO_1)
		);
		group.Host = host;
		group.Name = groupInfo.lgrpi1_name;
		group.Description = groupInfo.lgrpi1_comment;

		#region Get the group SID

		group.Checkpoint();
		WinAPI.ADVAPI32.SidNameUse nameUse;
		SecurityIdentifier sid;
		string resolvedDomain;
		int resolveError;
		if (SecurityTool.TryLookupAccountName(group.Host, group.Name, out sid, out resolvedDomain, out nameUse, out resolveError)) {
			if (nameUse != WinAPI.ADVAPI32.SidNameUse.Group && nameUse != WinAPI.ADVAPI32.SidNameUse.Alias) {
				throw new WindowsException(
					"Group '{0}' on host '{1}' had a non-group and/or non-alias SID name use.",
					group.Name,
					group.Host
				);
			}
			group.SID = sid;
			group.SidNameUsage = nameUse;
		} else {
			throw new WindowsException(
				"Unable to resolve SID for group '{0}' on host '{1}'. {2}",
				group.Name,
				group.Host,
				WindowsException.ErrorCodeToDescription(resolveError)
			);
		}

		#endregion

		return result;
	}

}

