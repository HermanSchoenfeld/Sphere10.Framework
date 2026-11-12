// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Security;

/// <summary>Application testing boundary over the Framework's local NT account resources.</summary>
public interface ILocalAccountsBackend {
	string HostName { get; }

	Task<LocalUserInfo[]> GetUsersAsync(CancellationToken cancellationToken = default);

	Task<LocalGroupInfo[]> GetGroupsAsync(CancellationToken cancellationToken = default);

	Task<LocalUserInfo> ReadUserAsync(string name, CancellationToken cancellationToken = default);

	Task<LocalGroupInfo> ReadGroupAsync(string name, CancellationToken cancellationToken = default);

	Task<LocalUserInfo> CreateUserAsync(LocalUserInfo proposed, string password, CancellationToken cancellationToken = default);

	Task<LocalUserInfo> UpdateUserAsync(LocalUserInfo expected, LocalUserInfo proposed, CancellationToken cancellationToken = default);

	Task DeleteUserAsync(LocalUserInfo expected, CancellationToken cancellationToken = default);

	Task<LocalUserInfo> ResetPasswordAsync(LocalUserInfo expected, string password, CancellationToken cancellationToken = default);

	Task<LocalGroupInfo> CreateGroupAsync(LocalGroupInfo proposed, CancellationToken cancellationToken = default);

	Task<LocalGroupInfo> UpdateGroupAsync(LocalGroupInfo expected, LocalGroupInfo proposed, CancellationToken cancellationToken = default);

	Task DeleteGroupAsync(LocalGroupInfo expected, CancellationToken cancellationToken = default);

	Task<LocalGroupInfo> AddMemberAsync(LocalGroupInfo expected, string identity, CancellationToken cancellationToken = default);

	Task<LocalGroupInfo> RemoveMemberAsync(LocalGroupInfo expected, LocalGroupMemberInfo member, CancellationToken cancellationToken = default);
}
