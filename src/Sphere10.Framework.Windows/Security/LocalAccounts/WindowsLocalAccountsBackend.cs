// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows.Security;

/// <summary>Local administration through existing NT resources. Native calls execute off the UI thread.</summary>
public sealed class WindowsLocalAccountsBackend : ILocalAccountsBackend {
	public string HostName => Environment.MachineName;

	public Task<LocalUserInfo[]> GetUsersAsync(CancellationToken cancellationToken = default) => RunAsync(host => {
		var users = host.GetLocalUsers(out var errors);
		if (errors.Length > 0)
			throw new AggregateException("Windows could not provide a complete local user inventory. No partial inventory is presented as complete.", errors);
		return users.Where(user => user != null).Select(ProjectUser).OrderBy(user => user.Name, StringComparer.OrdinalIgnoreCase).ToArray();
	}, cancellationToken);

	public Task<LocalGroupInfo[]> GetGroupsAsync(CancellationToken cancellationToken = default) => RunAsync(host => host.GetLocalGroups().Select(ProjectGroup).OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase).ToArray(), cancellationToken);

	public Task<LocalUserInfo> ReadUserAsync(string name, CancellationToken cancellationToken = default) => RunAsync(host => host.TryGetLocalUser(name, out var user) ? ProjectUser(user) : null, cancellationToken);

	public Task<LocalGroupInfo> ReadGroupAsync(string name, CancellationToken cancellationToken = default) => RunAsync(host => host.TryGetLocalGroup(name, out var group) ? ProjectGroup(group) : null, cancellationToken);

	public Task<LocalUserInfo> CreateUserAsync(LocalUserInfo proposed, string password, CancellationToken cancellationToken = default) => RunAsync(host => {
		var user = host.CreateLocalUser(proposed.Name, password, proposed.Flags);
		using var passwordLifetime = Tools.Scope.ExecuteOnDispose(() => user.Password = null);
		LocalUserInfo result = null;
		Tools.Exceptions.ExecuteCapturingException(() => {
			user.FullName = proposed.FullName;
			user.Description = proposed.Description;
			user.Flags = proposed.Flags;
			user.Password = null;
			user.Update();
			user.Refresh();
			result = ProjectUser(user);
		}, out var error);
		if (error != null)
			throw new InvalidOperationException($"Windows created user '{proposed.Name}', but applying its settings failed. Refresh Users to inspect the account; it has not been silently deleted.", error);
		return result;
	}, cancellationToken);

	public Task<LocalUserInfo> UpdateUserAsync(LocalUserInfo expected, LocalUserInfo proposed, CancellationToken cancellationToken = default) => RunAsync(host => {
		var user = RequireUser(host, expected);
		user.FullName = proposed.FullName;
		user.Description = proposed.Description;
		user.Flags = proposed.Flags;
		user.Password = null;
		user.Update();
		user.Refresh();
		return ProjectUser(user);
	}, cancellationToken);

	public Task DeleteUserAsync(LocalUserInfo expected, CancellationToken cancellationToken = default) => RunAsync(host => {
		RequireUser(host, expected).Delete();
		return true;
	}, cancellationToken);

	public Task<LocalUserInfo> ResetPasswordAsync(LocalUserInfo expected, string password, CancellationToken cancellationToken = default) => RunAsync(host => {
		var user = RequireUser(host, expected);
		using (Tools.Scope.ExecuteOnDispose(() => user.Password = null)) {
			user.Password = password;
			user.Update();
		}

		user.Refresh();
		return ProjectUser(user);
	}, cancellationToken);

	public Task<LocalGroupInfo> CreateGroupAsync(LocalGroupInfo proposed, CancellationToken cancellationToken = default) => RunAsync(host => ProjectGroup(host.CreateLocalGroup(proposed.Name, proposed.Description)), cancellationToken);

	public Task<LocalGroupInfo> UpdateGroupAsync(LocalGroupInfo expected, LocalGroupInfo proposed, CancellationToken cancellationToken = default) => RunAsync(host => {
		var group = RequireGroup(host, expected, false);
		group.Description = proposed.Description;
		group.Update();
		group.Refresh();
		return ProjectGroup(group);
	}, cancellationToken);

	public Task DeleteGroupAsync(LocalGroupInfo expected, CancellationToken cancellationToken = default) => RunAsync(host => {
		RequireGroup(host, expected, true).Delete();
		return true;
	}, cancellationToken);

	public Task<LocalGroupInfo> AddMemberAsync(LocalGroupInfo expected, string identity, CancellationToken cancellationToken = default) => RunAsync(host => {
		var group = RequireGroup(host, expected, true);
		group.AddMember(identity);
		return ProjectGroup(group);
	}, cancellationToken);

	public Task<LocalGroupInfo> RemoveMemberAsync(LocalGroupInfo expected, LocalGroupMemberInfo member, CancellationToken cancellationToken = default) => RunAsync(host => {
		var group = RequireGroup(host, expected, true);
		if (!string.IsNullOrEmpty(member.SID))
			group.DeleteMember(new SecurityIdentifier(member.SID));
		else
			group.DeleteMember(member.Identity);
		return ProjectGroup(group);
	}, cancellationToken);

	/// <summary>Projects a native group member into its captured identity, SID and type.</summary>
	public static LocalGroupMemberInfo ProjectMember(NTObject member) {
		Guard.ArgumentNotNull(member, nameof(member));
		var sid = member.SID?.Value ?? string.Empty;
		var identity = string.IsNullOrEmpty(member.Name)
			? string.IsNullOrEmpty(sid) ? "Unresolved Account" : sid
			: (member is NTRemoteObject remote ? remote.Domain : member.Host) + "\\" + member.Name;
		return new LocalGroupMemberInfo {
			Identity = identity,
			SID = sid,
			Type = (member is NTDanglingObject dangling ? dangling.NameUse : member.SidNameUsage).ToString()
		};
	}

	private static NTLocalUser RequireUser(NTHost host, LocalUserInfo expected) {
		Guard.Ensure(host.TryGetLocalUser(expected.Name, out var user) && expected.MatchesEditableState(ProjectUser(user)), "The user changed or was replaced after it was selected. Refresh Users and try again.");
		return user;
	}

	private static NTLocalGroup RequireGroup(NTHost host, LocalGroupInfo expected, bool members) {
		Guard.Ensure(host.TryGetLocalGroup(expected.Name, out var group) && expected.MatchesEditableState(ProjectGroup(group), members), "The group or its membership changed after it was selected. Refresh Groups and try again.");
		return group;
	}

	private static LocalUserInfo ProjectUser(NTLocalUser user) {
		string[] groups = Array.Empty<string>();
		string error = string.Empty;
		{
			Tools.Exceptions.ExecuteCapturingException(() => groups = user.GetMembership().Select(group => group.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(), out var failure, capturedException => !(capturedException is OperationCanceledException));
			if (failure != null) {
				error = failure.Message;
			}
		}

		return new LocalUserInfo {
			Host = user.Host,
			Name = user.Name,
			SID = user.SID?.Value ?? string.Empty,
			FullName = user.FullName ?? string.Empty,
			Description = user.Description ?? string.Empty,
			Flags = user.Flags,
			LastLogon = user.LastLogon == DateTime.MinValue ? null : user.LastLogon,
			PasswordAge = user.PasswordAge,
			Groups = groups,
			Availability = error
		};
	}

	private static LocalGroupInfo ProjectGroup(NTLocalGroup group) {
		LocalGroupMemberInfo[] members = Array.Empty<LocalGroupMemberInfo>();
		string error = string.Empty;
		{
			Tools.Exceptions.ExecuteCapturingException(() => members = group.GetMembers().Select(ProjectMember).OrderBy(member => member.Identity, StringComparer.OrdinalIgnoreCase).ToArray(), out var failure, capturedException => !(capturedException is OperationCanceledException));
			if (failure != null) {
				error = failure.Message;
			}
		}

		return new LocalGroupInfo {
			Host = group.Host,
			Name = group.Name,
			SID = group.SID?.Value ?? string.Empty,
			Description = group.Description ?? string.Empty,
			Members = members,
			Availability = error
		};
	}

	private static Task<T> RunAsync<T>(Func<NTHost, T> operation, CancellationToken cancellationToken) => Task.Run(() => {
		Debug.AssertNotMainThread();
		cancellationToken.ThrowIfCancellationRequested();
		var host = new NTHost(Environment.MachineName, cancellationToken.ThrowIfCancellationRequested);
		var result = operation(host);
		// Once a native write completed, return its outcome even when cancellation arrives afterward.
		return result;
	}, cancellationToken);
}
