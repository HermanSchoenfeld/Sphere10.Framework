// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Utils.BlazorTester.Showcase.Wizards;

/// <summary>An independent draft owned by one workspace setup wizard.</summary>
public sealed class WorkspaceSetupDraft {
	public string Name { get; set; } = string.Empty;
	public string Mode { get; set; } = "Solo";
	public int Capacity { get; set; } = 6;
	public string Access { get; set; } = "Invitation only";
	public bool WeeklyDigest { get; set; } = true;
	public string Reviewers { get; set; } = string.Empty;
	public string Preset { get; set; } = "Custom";
	public bool Acknowledged { get; set; }

	public WorkspaceSetupDraft Copy() => new() {
		Name = Name,
		Mode = Mode,
		Capacity = Capacity,
		Access = Access,
		WeeklyDigest = WeeklyDigest,
		Reviewers = Reviewers,
		Preset = Preset,
		Acknowledged = Acknowledged
	};
}
