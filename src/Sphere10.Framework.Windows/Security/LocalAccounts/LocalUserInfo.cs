// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Linq;

namespace Sphere10.Framework.Windows.Security;

/// <summary>A detached local account inventory. It never contains a password.</summary>
[ReadOnly(true)]
public sealed record LocalUserInfo {
	private string[] _groups = Array.Empty<string>();

	[DisplayName("Account Name"), Description("The local Windows logon name.")]
	public string Name { get; init; } = string.Empty;

	[DisplayName("Host"), Description("The computer owning this local account.")]
	public string Host { get; init; } = string.Empty;

	[DisplayName("SID"), Description("The account's Windows security identifier.")]
	public string SID { get; init; } = string.Empty;

	[DisplayName("Full Name"), Description("The account's display name, distinct from its logon name.")]
	public string FullName { get; init; } = string.Empty;

	[DisplayName("Description"), Description("The account description stored by Windows.")]
	public string Description { get; init; } = string.Empty;

	[DisplayName("Account Flags"), Description("The native flags; unrelated flags are preserved when editing.")]
	public UserFlags Flags { get; init; }

	[DisplayName("Enabled"), Description("Whether Windows allows this account to sign in.")]
	public bool Enabled => !Flags.HasFlag(UserFlags.AccountDisable);

	[DisplayName("Locked Out"), Description("Whether Windows currently reports an account lockout.")]
	public bool LockedOut => Flags.HasFlag(UserFlags.Lockout);

	[DisplayName("Password Never Expires"), Description("Whether password expiry is disabled for this account.")]
	public bool PasswordNeverExpires => Flags.HasFlag(UserFlags.DontExpirePassword);

	[DisplayName("Password Required"), Description("Whether Windows requires a password for this account.")]
	public bool PasswordRequired => !Flags.HasFlag(UserFlags.PasswordNotRequired);

	[DisplayName("User Can Change Password"), Description("The reported password-change flag; this screen does not rewrite the account ACL.")]
	public bool CanChangePassword => !Flags.HasFlag(UserFlags.PasswordCantChange);

	[DisplayName("Password Expired"), Description("Whether Windows reports an expired password.")]
	public bool PasswordExpired => Flags.HasFlag(UserFlags.PasswordExpired);

	[DisplayName("Last Logon"), Description("Last local logon reported by Windows; unavailable when no logon was recorded.")]
	public DateTime? LastLogon { get; init; }

	[DisplayName("Password Age"), Description("Time since Windows last changed this account's password.")]
	public TimeSpan PasswordAge { get; init; }

	[DisplayName("Groups"), Description("Direct membership in local groups.")]
	public string[] Groups { get => (string[])_groups.Clone(); init => _groups = value?.ToArray() ?? Array.Empty<string>(); }

	[DisplayName("Availability"), Description("Any failure reading the membership inventory.")]
	public string Availability { get; init; } = string.Empty;

	internal bool MatchesEditableState(LocalUserInfo current) => current != null && string.Equals(Host, current.Host, StringComparison.OrdinalIgnoreCase) && string.Equals(Name, current.Name, StringComparison.OrdinalIgnoreCase) && SID == current.SID && FullName == current.FullName && Description == current.Description && Flags == current.Flags;
}
