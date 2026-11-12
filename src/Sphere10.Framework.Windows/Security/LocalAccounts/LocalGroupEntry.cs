// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Sphere10.Framework.Windows.Security;

public sealed class LocalGroupEntry {
	[DisplayName("Group"), Description("The local Windows group name.")]
	public string Name { get; set; } = string.Empty;

	[DisplayName("Description"), Description("The group description.")]
	public string Description { get; set; } = string.Empty;

	[DisplayName("SID"), Description("The original group security identifier.")]
	public string SID => Baseline?.SID ?? string.Empty;

	[DisplayName("Member Count"), Description("Number of captured members; check Availability for inventory failures.")]
	public int MemberCount => Baseline?.MemberCount ?? 0;

	[DisplayName("Availability"), Description("Errors reading the member inventory.")]
	public string Availability => Baseline?.Availability ?? string.Empty;

	[Browsable(false), JsonIgnore]
	public LocalGroupDataSourceBase Owner { get; set; }

	[Browsable(false), JsonIgnore]
	public LocalGroupEntry Origin { get; set; }

	/// <summary>The captured group this row edits, or null for a new group.</summary>
	[Browsable(false), JsonIgnore]
	public LocalGroupInfo Baseline { get; private set; }

	[Browsable(false), JsonIgnore]
	public bool IsPersisted => Baseline != null;

	public LocalGroupEntry Copy() => new() {
		Name = Name,
		Description = Description,
		Owner = Owner,
		Origin = Origin ?? this,
		Baseline = Baseline
	};

	public LocalGroupInfo Proposed(string host) => new() {
		Host = host,
		Name = Name,
		SID = SID,
		Description = Description
	};

	public void Accept(LocalGroupInfo snapshot) {
		Baseline = snapshot;
		Name = snapshot.Name;
		Description = snapshot.Description;
	}
}
