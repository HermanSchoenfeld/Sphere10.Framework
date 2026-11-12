// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows.Security;

/// <summary>Serialized local-account workflows and captured-state checks.</summary>
public sealed class LocalAccountsService {
	private readonly ILocalAccountsBackend _backend;
	private readonly AsyncSynchronizedGate _admission = new();

	internal const UserFlags EditableFlags = UserFlags.AccountDisable | UserFlags.DontExpirePassword | UserFlags.PasswordNotRequired;

	public LocalAccountsService()
		: this(new WindowsLocalAccountsBackend()) {
	}

	public LocalAccountsService(ILocalAccountsBackend backend) {
		Guard.ArgumentNotNull(backend, nameof(backend));
		_backend = backend;
	}

	public string HostName => _backend.HostName;

	public async Task<LocalUserInfo[]> GetUsersAsync(CancellationToken cancellationToken = default) {
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		return await _backend.GetUsersAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task<LocalGroupInfo[]> GetGroupsAsync(CancellationToken cancellationToken = default) {
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		return await _backend.GetGroupsAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task<LocalUserInfo> ReadUserAsync(string name, CancellationToken cancellationToken = default) {
		ValidateName(name, false);
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		return await _backend.ReadUserAsync(name, cancellationToken).ConfigureAwait(false);
	}

	public async Task<LocalGroupInfo> ReadGroupAsync(string name, CancellationToken cancellationToken = default) {
		ValidateName(name, true);
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		return await _backend.ReadGroupAsync(name, cancellationToken).ConfigureAwait(false);
	}

	public async Task<LocalUserInfo> CreateUserAsync(LocalUserInfo proposed, string password, ILogger logger, CancellationToken cancellationToken = default) {
		ValidateName(proposed?.Name, false);
		ValidateText(proposed.FullName, 256, "Full name");
		ValidateText(proposed.Description, 256, "Description");
		Guard.Ensure(!string.IsNullOrEmpty(password) || proposed.Flags.HasFlag(UserFlags.PasswordNotRequired), "Enter an initial password, or explicitly choose Password not required.");
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		Guard.Ensure(await _backend.ReadUserAsync(proposed.Name, cancellationToken).ConfigureAwait(false) == null, "A user with this name already exists.");
		var flags = UserFlags.NormalAccount | UserFlags.Script | (proposed.Flags & EditableFlags);
		var result = await _backend.CreateUserAsync(proposed with { Host = HostName, Flags = flags }, password, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: created local user {HostName}\\{result.Name}.");
		return result;
	}

	public async Task<LocalUserInfo> UpdateUserAsync(LocalUserInfo expected, LocalUserInfo proposed, ILogger logger, CancellationToken cancellationToken = default) {
		ValidateText(proposed.FullName, 256, "Full name");
		ValidateText(proposed.Description, 256, "Description");
		Guard.Ensure(string.Equals(expected.Name, proposed.Name, StringComparison.OrdinalIgnoreCase), "An existing account's logon name cannot be changed in this editor.");
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		await RequireUserAsync(expected, cancellationToken).ConfigureAwait(false);
		var result = await _backend.UpdateUserAsync(expected, proposed with { Flags = (expected.Flags & ~EditableFlags) | (proposed.Flags & EditableFlags) }, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: updated local user {HostName}\\{result.Name}.");
		return result;
	}

	public async Task DeleteUserAsync(LocalUserInfo expected, ILogger logger, CancellationToken cancellationToken = default) {
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		await RequireUserAsync(expected, cancellationToken).ConfigureAwait(false);
		await _backend.DeleteUserAsync(expected, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: deleted local user {HostName}\\{expected.Name}. Its files and profile were not deleted.");
	}

	public Task<LocalUserInfo> SetEnabledAsync(LocalUserInfo expected, bool enabled, ILogger logger, CancellationToken cancellationToken = default) => UpdateUserAsync(expected, expected with { Flags = enabled ? expected.Flags & ~UserFlags.AccountDisable : expected.Flags | UserFlags.AccountDisable }, logger, cancellationToken);

	public async Task<LocalUserInfo> UnlockAsync(LocalUserInfo expected, ILogger logger, CancellationToken cancellationToken = default) {
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		await RequireUserAsync(expected, cancellationToken).ConfigureAwait(false);
		Guard.Ensure(expected.LockedOut, "This account is not currently locked out.");
		var result = await _backend.UpdateUserAsync(expected, expected with { Flags = expected.Flags & ~UserFlags.Lockout }, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: unlocked local user {HostName}\\{expected.Name}.");
		return result;
	}

	public async Task<LocalUserInfo> ResetPasswordAsync(LocalUserInfo expected, string password, ILogger logger, CancellationToken cancellationToken = default) {
		Guard.Ensure(!string.IsNullOrEmpty(password), "Enter a new password.");
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		await RequireUserAsync(expected, cancellationToken).ConfigureAwait(false);
		var result = await _backend.ResetPasswordAsync(expected, password, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: reset the password for local user {HostName}\\{expected.Name}. Password contents are not logged.");
		return result;
	}

	public async Task<LocalGroupInfo> CreateGroupAsync(LocalGroupInfo proposed, ILogger logger, CancellationToken cancellationToken = default) {
		ValidateName(proposed?.Name, true);
		ValidateText(proposed.Description, 256, "Description");
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		Guard.Ensure(await _backend.ReadGroupAsync(proposed.Name, cancellationToken).ConfigureAwait(false) == null, "A group with this name already exists.");
		var result = await _backend.CreateGroupAsync(proposed with { Host = HostName }, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: created local group {HostName}\\{result.Name}.");
		return result;
	}

	public async Task<LocalGroupInfo> UpdateGroupAsync(LocalGroupInfo expected, LocalGroupInfo proposed, ILogger logger, CancellationToken cancellationToken = default) {
		ValidateText(proposed.Description, 256, "Description");
		Guard.Ensure(string.Equals(expected.Name, proposed.Name, StringComparison.OrdinalIgnoreCase), "An existing group's name cannot be changed in this editor.");
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		await RequireGroupAsync(expected, false, cancellationToken).ConfigureAwait(false);
		var result = await _backend.UpdateGroupAsync(expected, proposed, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: updated local group {HostName}\\{result.Name}.");
		return result;
	}

	public async Task DeleteGroupAsync(LocalGroupInfo expected, ILogger logger, CancellationToken cancellationToken = default) {
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		await RequireGroupAsync(expected, true, cancellationToken).ConfigureAwait(false);
		await _backend.DeleteGroupAsync(expected, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: deleted local group {HostName}\\{expected.Name}. Member accounts were not deleted.");
	}

	public async Task<LocalGroupInfo> AddMemberAsync(LocalGroupInfo expected, string identity, ILogger logger, CancellationToken cancellationToken = default) {
		ValidateIdentity(identity);
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		await RequireGroupAsync(expected, true, cancellationToken).ConfigureAwait(false);
		Guard.Ensure(!expected.Members.Any(member => string.Equals(member.Identity, identity, StringComparison.OrdinalIgnoreCase)), "The selected account already belongs to this group.");
		var result = await _backend.AddMemberAsync(expected, identity, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: added {identity} to local group {HostName}\\{expected.Name}.");
		return result;
	}

	public async Task<LocalGroupInfo> RemoveMemberAsync(LocalGroupInfo expected, LocalGroupMemberInfo member, ILogger logger, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(member, nameof(member));
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		await RequireGroupAsync(expected, true, cancellationToken).ConfigureAwait(false);
		Guard.Ensure(expected.Members.Any(current => current.SID == member.SID && string.Equals(current.Identity, member.Identity, StringComparison.OrdinalIgnoreCase)), "The selected member no longer belongs to this captured group.");
		var result = await _backend.RemoveMemberAsync(expected, member, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: removed {member.Identity} from local group {HostName}\\{expected.Name}.");
		return result;
	}

	public async Task<LocalGroupInfo> AddUserToGroupAsync(LocalUserInfo expectedUser, LocalGroupInfo expectedGroup, ILogger logger, CancellationToken cancellationToken = default) {
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		await RequireUserAsync(expectedUser, cancellationToken).ConfigureAwait(false);
		await RequireGroupAsync(expectedGroup, true, cancellationToken).ConfigureAwait(false);
		var identity = HostName + "\\" + expectedUser.Name;
		Guard.Ensure(!expectedGroup.Members.Any(member => member.SID == expectedUser.SID), "This user already belongs to the selected group.");
		var result = await _backend.AddMemberAsync(expectedGroup, identity, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: added {identity} to local group {HostName}\\{expectedGroup.Name}.");
		return result;
	}

	public async Task<LocalGroupInfo> RemoveUserFromGroupAsync(LocalUserInfo expectedUser, LocalGroupInfo expectedGroup, ILogger logger, CancellationToken cancellationToken = default) {
		using var scope = await _admission.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		await RequireUserAsync(expectedUser, cancellationToken).ConfigureAwait(false);
		await RequireGroupAsync(expectedGroup, true, cancellationToken).ConfigureAwait(false);
		var member = expectedGroup.Members.SingleOrDefault(candidate => candidate.SID == expectedUser.SID);
		Guard.Ensure(member != null, "This user is not in the selected group's captured membership.");
		var result = await _backend.RemoveMemberAsync(expectedGroup, member, cancellationToken).ConfigureAwait(false);
		(logger ?? new NoOpLogger()).Info($"Local accounts: removed {HostName}\\{expectedUser.Name} from local group {HostName}\\{expectedGroup.Name}.");
		return result;
	}

	public static void ValidateName(string name, bool group) {
		Guard.Ensure(!string.IsNullOrWhiteSpace(name) && name == name.Trim() && name.Length <= (group ? 256 : 20), group ? "Enter a group name of 1-256 characters without leading/trailing whitespace." : "Enter a local logon name of 1-20 characters without leading/trailing whitespace.");
		Guard.Ensure(name.IndexOfAny("\\/\"[]:;|=,+*?<>".ToCharArray()) < 0 && !name.Any(char.IsControl) && !name.EndsWith('.') && !name.All(character => character == '.'), "Account names cannot contain control characters or \\/\"[]:;|=,+*?<>, or end with a period.");
	}

	public static void ValidateIdentity(string identity) {
		Guard.Ensure(!string.IsNullOrWhiteSpace(identity) && identity == identity.Trim() && identity.Count(character => character == '\\') == 1 && !identity.Any(char.IsControl), "Enter an explicit computer\\account or domain\\account identity.");
		var parts = identity.Split('\\');
		Guard.Ensure(parts.All(part => !string.IsNullOrWhiteSpace(part)), "Both domain/computer and account must be specified.");
	}

	internal static void ValidateText(string value, int limit, string field) => Guard.Ensure(value != null && value.Length <= limit && !value.Contains('\0'), $"{field} cannot exceed {limit} characters or contain NUL characters.");

	private async Task RequireUserAsync(LocalUserInfo expected, CancellationToken cancellationToken) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.Ensure(string.Equals(expected.Host, HostName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(expected.SID), "This account does not belong to the local inventory or has no resolved SID.");
		Guard.Ensure(expected.MatchesEditableState(await _backend.ReadUserAsync(expected.Name, cancellationToken).ConfigureAwait(false)), "The user changed or was replaced after it was selected. Refresh Users and try again.");
	}

	private async Task RequireGroupAsync(LocalGroupInfo expected, bool members, CancellationToken cancellationToken) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.Ensure(string.Equals(expected.Host, HostName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(expected.SID), "This group does not belong to the local inventory or has no resolved SID.");
		Guard.Ensure(!members || string.IsNullOrEmpty(expected.Availability), "Windows did not provide this group's complete membership. Refresh before changing membership or deleting the group.");
		Guard.Ensure(expected.MatchesEditableState(await _backend.ReadGroupAsync(expected.Name, cancellationToken).ConfigureAwait(false), members), "The group or its membership changed after it was selected. Refresh Groups and try again.");
	}
}
