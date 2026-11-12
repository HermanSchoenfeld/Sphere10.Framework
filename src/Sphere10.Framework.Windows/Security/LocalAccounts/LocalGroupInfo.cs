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

[ReadOnly(true)]
public sealed record LocalGroupInfo {
	private LocalGroupMemberInfo[] _members = Array.Empty<LocalGroupMemberInfo>();

	[DisplayName("Group"), Description("The local group name.")]
	public string Name { get; init; } = string.Empty;

	[DisplayName("Host"), Description("The computer owning this group.")]
	public string Host { get; init; } = string.Empty;

	[DisplayName("SID"), Description("The group's Windows security identifier.")]
	public string SID { get; init; } = string.Empty;

	[DisplayName("Description"), Description("The group description stored by Windows.")]
	public string Description { get; init; } = string.Empty;

	[DisplayName("Members"), Description("Local, domain and unresolved accounts currently belonging to this group.")]
	public LocalGroupMemberInfo[] Members { get => (LocalGroupMemberInfo[])_members.Clone(); init => _members = value?.ToArray() ?? Array.Empty<LocalGroupMemberInfo>(); }

	[DisplayName("Member Count"), Description("Number of members returned by Windows.")]
	public int MemberCount => _members.Length;

	[DisplayName("Availability"), Description("Any failure reading the member inventory.")]
	public string Availability { get; init; } = string.Empty;

	internal bool MatchesEditableState(LocalGroupInfo current, bool includeMembers = false) => current != null && string.Equals(Host, current.Host, StringComparison.OrdinalIgnoreCase) && string.Equals(Name, current.Name, StringComparison.OrdinalIgnoreCase) && SID == current.SID && Description == current.Description && (!includeMembers || Availability == current.Availability && _members.Select(Key).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).SequenceEqual(current._members.Select(Key).OrderBy(value => value, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase));

	private static string Key(LocalGroupMemberInfo member) => member.SID + "|" + member.Identity;
}
