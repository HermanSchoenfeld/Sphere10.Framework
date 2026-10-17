// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;


namespace Sphere10.Framework.Windows.Security;

/// <summary>
/// Encapsulates a remote host.
/// </summary>
/// <remarks>Native inventory uses the caller's Windows credentials and the remote server's access policy.
/// Reachable DNS does not establish permission to enumerate or modify its users, groups or shares.</remarks>
public class NTHost : NTObject {
	static private readonly NTHost _currentMachine = new NTHost(null);
	internal delegate bool AccountNameLookup(string host, string accountName, out SecurityIdentifier sid, out string domain,
		out WinAPI.ADVAPI32.SidNameUse usage, out int win32Error);

	/// <summary>
	/// Resolves the host name and attempts its identity lookup. Inventory access is established by each native query.
	/// </summary>
	/// <param name="host">Name of host to query, or null/empty for local host.</param>
	public NTHost(string host) : this(host, null) {
	}

	public NTHost(string host, Action checkpoint) : this(host, checkpoint, null) {
	}

	internal NTHost(string host, Action checkpoint, AccountNameLookup suppliedLookup) {
		Checkpoint = checkpoint ?? (() => { });
		Checkpoint();
		string hostToConnectTo = string.IsNullOrEmpty(host) ? Environment.MachineName : host;
		IPHostEntry hostEntry = Dns.GetHostEntry(hostToConnectTo);
		Host = hostToConnectTo;
		Aliases = hostEntry.Aliases;
		Addresses = hostEntry.AddressList;
		Name = hostEntry.HostName;

		try {

			#region Get SID for host

			var lookup = suppliedLookup ?? new AccountNameLookup(SecurityTool.TryLookupAccountName);
			var nameUse = WinAPI.ADVAPI32.SidNameUse.Unknown;
			SecurityIdentifier sid = null;


			// search SID for all aliases & name
			var namesToSearch = new List<string>();
			var resolveErrors = new List<string>();
			namesToSearch.Add(this.Host);
			namesToSearch.AddRange(Aliases);
			namesToSearch.Add(Name);
			var found = false;
			var nameMatched = string.Empty;
			foreach (var name in namesToSearch) {
				int resolveError;
				string resolvedDomain;
				Checkpoint();
				if (lookup(Host, name, out sid, out resolvedDomain, out nameUse, out resolveError)) {
					found = true;
					nameMatched = name;
					break;
				} else {
					resolveErrors.Add(WindowsException.ErrorCodeToDescription(resolveError));
				}
			}
			if (!found) {
				throw new Exception(
					string.Format(
						"Could not establish SID for host '{0}'. Tried the following symbolic aliases '{1}' and each failed with win32 errors '{2}'",
						this.Name,
						string.Join(",", namesToSearch.ToArray()),
						string.Join(",", resolveErrors.ToArray())
					)
				);
			}

			if (nameUse != WinAPI.ADVAPI32.SidNameUse.Computer && nameUse != WinAPI.ADVAPI32.SidNameUse.Domain) {
				throw new Exception(
					string.Format(
						"Host SID resolved to a non-computer and non-domain object. Host was '{0}' and first SID alias matched was '{1}' out of the of list '{2}'.",
						Name,
						nameMatched,
						string.Join(",", namesToSearch.ToArray())
					)
				);
			}
			base.SID = sid;
			base.SidNameUsage = nameUse;

			#endregion

		} catch (OperationCanceledException) {
			throw;
		} catch (Exception error) {
			SID = null;
			SidNameUsage = WinAPI.ADVAPI32.SidNameUse.Invalid;
			SidResolutionError = error.Message;
		}
	}
	/// <summary>
	/// Gets the current machine.
	/// </summary>
	public static NTHost CurrentMachine {
		get { return NTHost._currentMachine; }
	}

	public override string FullName {
		get { return Name; }
	}

	public string[] Aliases { get; set; }

	public IPAddress[] Addresses { get; set; }

	public Action Checkpoint { get; }

	/// <summary>Why this host identity could not be resolved; inventory may still be available.</summary>
	public string SidResolutionError { get; private set; }

	public static bool IsLocalHost(string host) {
		host = host?.Trim().TrimStart('\\');
		if (string.IsNullOrEmpty(host) || host == "." || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(host, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
			return true;
		return string.Equals(Dns.GetHostEntry(host).HostName, CurrentMachine.Name, StringComparison.OrdinalIgnoreCase);
	}

	public static bool CanConnectTo(string hostname) {
		try {
			var host = new NTHost(hostname);
		} catch {
			return false;
		}
		return true;
	}

	/// <summary>
	/// Returns the local users on the host.
	/// </summary>
	/// <returns></returns>
	public NTLocalGroup[] GetLocalGroups() {
		var resume = UIntPtr.Zero;
		return NetApiEnumeration.Read<WinAPI.NETAPI32.LOCALGROUP_INFO_0, NTLocalGroup>(
			(out IntPtr buffer, out uint count) => WinAPI.NETAPI32.NetLocalGroupEnumPaged(NTCompatibleHostName, 0, out buffer, WinAPI.NETAPI32.MAX_PREFERRED_LENGTH, out count, out _, ref resume),
			info => {
				var group = new NTLocalGroup { Host = Host, Name = info.name, Checkpoint = Checkpoint };
				group.Refresh();
				return group;
			}, Checkpoint);
	}
	public bool TryGetLocalObject(string remoteName, out NTLocalObject obj) {
		var retval = false;
		obj = null;
		NTLocalUser localUser;
		;
		NTLocalGroup localGroup;
		if (TryGetLocalUser(remoteName, out localUser)) {
			obj = localUser;
			retval = true;
		} else if (TryGetLocalGroup(remoteName, out localGroup)) {
			obj = localGroup;
			retval = true;
		}
		return retval;
	}

	public bool TryGetLocalGroup(string name, out NTLocalGroup group) {
		return TryGetLocalGroup(name, out group, Checkpoint);
	}

	public bool TryGetLocalGroup(string name, out NTLocalGroup group, Action checkpoint) {
		checkpoint?.Invoke();
		group = new NTLocalGroup { Checkpoint = checkpoint ?? (() => { }) };
		if (NTLocalGroup.TryLoadLocalGroup(NTCompatibleHostName, name, group) == 0) {
			return true;
		} else {
			group = null;
			return false;
		}
	}

	/// <summary>
	/// Creates a local group on the host.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="description"></param>
	public NTLocalGroup CreateLocalGroup(string name, string description) => CreateLocalGroup(name, description, Checkpoint);

	public NTLocalGroup CreateLocalGroup(string name, string description, Action checkpoint) {
		checkpoint?.Invoke();
		var error = 0;
		var newLocalGroup = new WinAPI.NETAPI32.LOCALGROUP_INFO_1();
		newLocalGroup.lgrpi1_name = name;
		newLocalGroup.lgrpi1_comment = description;
		var result = WinAPI.NETAPI32.NetLocalGroupAdd(this.NTCompatibleHostName, 1, ref newLocalGroup, ref error);
		if (result != 0) {
			throw new NetApiException(
				result,
				"Unable to create local group '{0}' on host '{1}'",
				name,
				Host
			);
		}

		NTLocalGroup localGroup = new NTLocalGroup();
		localGroup.Host = Host;
		localGroup.Name = name;
		localGroup.Description = description;
		localGroup.Checkpoint = checkpoint ?? (() => { });
		localGroup.Refresh();

		return localGroup;
	}


	/// <summary>
	/// Returns the local users on the host.
	/// </summary>
	/// <returns></returns>
	public NTLocalUser[] GetLocalUsers() {
		var users = GetLocalUsers(out var errors);
		if (errors.Length > 0)
			throw new AggregateException("Some local users could not be read.", errors);
		return users;
	}

	public NTLocalUser[] GetLocalUsers(out Exception[] errors) {
		uint resume = 0;
		var failures = new List<Exception>();
		var users = NetApiEnumeration.Read<WinAPI.NETAPI32.USER_INFO_0, NTLocalUser>(
			(out IntPtr buffer, out uint count) => WinAPI.NETAPI32.NetUserEnumPaged(NTCompatibleHostName, 0, 2, out buffer, WinAPI.NETAPI32.MAX_PREFERRED_LENGTH, out count, out _, ref resume),
			info => {
				try {
					var user = new NTLocalUser { Host = Host, Name = info.Username, Checkpoint = Checkpoint };
					user.Refresh();
					return user;
				} catch (OperationCanceledException) { throw; } catch (Exception error) {
					failures.Add(error);
					return null;
				}
			}, Checkpoint);
		errors = failures.ToArray();
		return users;
	}
	public bool TryGetLocalUser(string name, out NTLocalUser user) {
		return TryGetLocalUser(name, out user, Checkpoint);
	}

	public bool TryGetLocalUser(string name, out NTLocalUser user, Action checkpoint) {
		checkpoint?.Invoke();
		user = new NTLocalUser { Checkpoint = checkpoint ?? (() => { }) };
		if (NTLocalUser.TryLoadLocalUser(NTCompatibleHostName, name, user) == 0) {
			return true;
		}
		user = null;
		return false;
	}

	/// <summary>
	/// Creates a local user on the host.
	/// </summary>
	/// <param name="name"></param>
	public NTLocalUser CreateLocalUser(string name, string password)
		=> CreateLocalUser(name, password, UserFlags.NormalAccount | UserFlags.Script, Checkpoint);

	public NTLocalUser CreateLocalUser(string name, string password, Action checkpoint)
		=> CreateLocalUser(name, password, UserFlags.NormalAccount | UserFlags.Script, checkpoint);

	/// <summary>Creates a local user with flags applied during the native creation call, including PasswordNotRequired.</summary>
	/// <remarks>Script is required by Windows. Windows still enforces account permissions and password policy.</remarks>
	public NTLocalUser CreateLocalUser(string name, string password, UserFlags initialFlags)
		=> CreateLocalUser(name, password, initialFlags, Checkpoint);

	public NTLocalUser CreateLocalUser(string name, string password, UserFlags initialFlags, Action checkpoint) {
		Guard.Argument(initialFlags.HasFlag(UserFlags.Script), nameof(initialFlags), "Windows requires the Script flag when creating a user.");
		Guard.Argument(!initialFlags.HasFlag(UserFlags.Lockout), nameof(initialFlags), "A new user cannot be created in a locked-out state.");
		checkpoint?.Invoke();
		var newUser = new WinAPI.NETAPI32.USER_INFO_1 {
			usri1_name = name,
			usri1_password = password,
			usri1_flags = (uint)initialFlags,
			usri1_priv = (int)UserPrivilege.User,
			usri1_home_dir = null,
			comment = null,
			usri1_script_path = null
		}; // Create an new instance of the USER_INFO_1 struct
		var result = WinAPI.NETAPI32.NetUserAdd(this.NTCompatibleHostName, 1, ref newUser, 0);
		if (result != 0) {
			throw new NetApiException(
				result,
				"Unable to create local user '{0}' on host '{1}'",
				name,
				Host
			);
		}

		var user = new NTLocalUser {
			Host = Host,
			Name = name,
			Checkpoint = checkpoint ?? (() => { })
		};
		user.Refresh();
		return user;

	}


	public NTShare[] GetShares(out Exception[] errors) {
		uint resume = 0;
		var failures = new List<Exception>();
		var shares = NetApiEnumeration.Read<WinAPI.NETAPI32.SHARE_INFO_0, NTShare>(
			(out IntPtr buffer, out uint count) => WinAPI.NETAPI32.NetShareEnumPaged(NTCompatibleHostName, 0, out buffer, WinAPI.NETAPI32.MAX_PREFERRED_LENGTH, out count, out _, ref resume),
			info => {
				try {
					var share = new NTShare { Host = Host, Name = info.shi0_netname, Checkpoint = Checkpoint };
					share.Refresh();
					return share;
				} catch (OperationCanceledException) { throw; } catch (Exception error) {
					failures.Add(error);
					return null;
				}
			}, Checkpoint);
		errors = failures.ToArray();
		return shares;
	}
	public NTShare[] GetShares() {
		Exception[] errors;
		return GetShares(out errors);
	}


	public bool TryGetShare(string name, out NTShare share) {
		return TryGetShare(name, out share, Checkpoint);
	}

	public bool TryGetShare(string name, out NTShare share, Action checkpoint) {
		checkpoint?.Invoke();
		share = new NTShare { Checkpoint = checkpoint ?? (() => { }) };
		if (NTShare.TryLoadLocalShare(NTCompatibleHostName, name, share) == 0) {
			return true;
		} else {
			share = null;
			return false;
		}

	}

	public bool CheckShare(string name, out WinAPI.NETAPI32.SHARE_TYPE shareType) {
		return WinAPI.NETAPI32.NetShareCheck(this.NTCompatibleHostName, name, out shareType) == WinAPI.NETAPI32.NET_API_STATUS.NERR_Success;
	}

	public bool ContainsShare(string name) {
		NTShare share = null;
		return TryGetShare(name, out share);
	}

	public NTShare CreateShare(string name, string description, WinAPI.NETAPI32.SHARE_TYPE type, string path) => CreateShare(name, description, type, path, Checkpoint);

	public NTShare CreateShare(string name, string description, WinAPI.NETAPI32.SHARE_TYPE type, string path, Action checkpoint) {
		checkpoint?.Invoke();
		var newShare = new WinAPI.NETAPI32.SHARE_INFO_502 {
			shi502_netname = name,
			shi502_remark = description,
			shi502_type = (uint)type,
			shi502_path = path
		};
		uint paramErr;
		var result = WinAPI.NETAPI32.NetShareAdd(this.Host, 502, ref newShare, out paramErr);

		if (result != 0) {
			throw new NetApiException(
				result,
				"Unable to create local share '{0}' on host '{1}'",
				name,
				Host
			);
		}

		var share = new NTShare {
			Host = Host,
			Name = name,
			Checkpoint = checkpoint ?? (() => { })
		};
		share.Refresh();
		return share;

	}

	public string ResolveUNCPathToLocalPath(string uncPath) {
		Debug.Assert(uncPath != null);
		var uri = new Uri(uncPath);
		if (!uri.IsUnc) {
			throw new SoftwareException("Unable to resolve the local path of the UNC path '{0}' as the supplied UNC path was invalid", uncPath);
		}

		var sharePath = Uri.UnescapeDataString(uri.AbsolutePath).Split('/');
		if (sharePath.Length <= 1) {
			throw new SoftwareException("Unable to resolve the local path of the UNC path '{0}' as the supplied UNC path was invalid", uncPath);
		}
		var shareName = sharePath[1];
		NTShare share;
		if (!TryGetShare(shareName, out share)) {
			throw new SoftwareException("Unable to resolve local path of UNC path '{0}' as the share '{1}' could not be retrieved on host '{2}", uncPath, shareName, Name);
		}
		var localPath = new StringBuilder();
		localPath.Append(share.ServerPath);
		for (var i = 2; i < sharePath.Length; i++) {
			if (!localPath.ToString().EndsWith(Path.DirectorySeparatorChar.ToString())) {
				localPath.Append(Path.DirectorySeparatorChar);
			}
			localPath.Append(sharePath[i]);
		}
		return localPath.ToString();
	}

	public static string ResolveUNCPathToHostLocalPath(string uncPath) {
		Debug.Assert(uncPath != null);
		var uri = new Uri(uncPath);
		if (!uri.IsUnc) {
			throw new SoftwareException("Unable to resolve the local path of the UNC path '{0}' as the supplied UNC path was invalid", uncPath);
		}
		return new NTHost(uri.Host).ResolveUNCPathToLocalPath(uncPath);
	}


}

