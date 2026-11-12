// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Sphere10.Framework.Windows.Security;

/// <summary>A source-owned edit row. Secret inputs are internal and excluded from snapshot/export properties.</summary>
public sealed class LocalUserEntry {
	[DisplayName("Account Name"), Description("The local Windows logon name.")]
	public string Name { get; set; } = string.Empty;

	[DisplayName("Full Name"), Description("The display name, distinct from the logon name.")]
	public string FullName { get; set; } = string.Empty;

	[DisplayName("Description"), Description("The account description.")]
	public string Description { get; set; } = string.Empty;

	[DisplayName("Enabled"), Description("Whether this account can sign in.")]
	public bool Enabled { get; set; } = true;

	[DisplayName("Password Required"), Description("Whether a password is required.")]
	public bool PasswordRequired { get; set; } = true;

	[DisplayName("Password Never Expires"), Description("Whether this account's password is exempt from expiry.")]
	public bool PasswordNeverExpires { get; set; }

	[DisplayName("SID"), Description("The original account security identifier.")]
	public string SID => Baseline?.SID ?? string.Empty;

	[DisplayName("Locked Out"), Description("The latest captured Windows lockout state.")]
	public bool LockedOut => Baseline?.LockedOut == true;

	[DisplayName("Last Logon"), Description("The last recorded local logon, if available.")]
	public DateTime? LastLogon => Baseline?.LastLogon;

	[DisplayName("Groups"), Description("Direct membership in local groups.")]
	public string[] Groups => Baseline?.Groups ?? Array.Empty<string>();

	[DisplayName("Account Flags"), Description("All captured Windows account flags.")]
	public UserFlags Flags => Baseline?.Flags ?? UserFlags.NormalAccount | UserFlags.Script;

	[DisplayName("Availability"), Description("Membership inventory errors, if any.")]
	public string Availability => Baseline?.Availability ?? string.Empty;

	[Browsable(false), JsonIgnore]
	public LocalUserDataSourceBase Owner { get; set; }

	[Browsable(false), JsonIgnore]
	public LocalUserEntry Origin { get; set; }

	/// <summary>The captured account this row edits, or null for a new account.</summary>
	[Browsable(false), JsonIgnore]
	public LocalUserInfo Baseline { get; private set; }

	/// <summary>The password of a new account until it is created; never part of a snapshot or export.</summary>
	[Browsable(false), JsonIgnore]
	public string InitialPassword { get; set; }

	[Browsable(false), JsonIgnore]
	public bool IsPersisted => Baseline != null;

	public LocalUserEntry Copy() => new() {
		Name = Name,
		FullName = FullName,
		Description = Description,
		Enabled = Enabled,
		PasswordRequired = PasswordRequired,
		PasswordNeverExpires = PasswordNeverExpires,
		Owner = Owner,
		Origin = Origin ?? this,
		Baseline = Baseline
	};

	public LocalUserInfo Proposed(string host) {
		var flags = Flags & ~LocalAccountsService.EditableFlags;
		if (!Enabled)
			flags |= UserFlags.AccountDisable;
		if (!PasswordRequired)
			flags |= UserFlags.PasswordNotRequired;
		if (PasswordNeverExpires)
			flags |= UserFlags.DontExpirePassword;
		return new LocalUserInfo {
			Host = host,
			Name = Name,
			SID = SID,
			FullName = FullName,
			Description = Description,
			Flags = flags
		};
	}

	public void Accept(LocalUserInfo snapshot) {
		Baseline = snapshot;
		Name = snapshot.Name;
		FullName = snapshot.FullName;
		Description = snapshot.Description;
		Enabled = snapshot.Enabled;
		PasswordRequired = snapshot.PasswordRequired;
		PasswordNeverExpires = snapshot.PasswordNeverExpires;
		InitialPassword = null;
	}
}
