// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Stored plan values and separately reported policy overrides; policy presence does not establish effective precedence.</summary>
public sealed record WindowsPowerSettingInfo {
	public Guid PlanId { get; init; }
	public Guid SubgroupId { get; init; }
	public Guid Id { get; init; }
	public string Subgroup { get; init; }
	public string Name { get; init; }
	public string Description { get; init; }
	public uint? AcValue { get; init; }
	public uint? DcValue { get; init; }
	public string AcText { get; init; }
	public string DcText { get; init; }
	public string Units { get; init; }
	public uint? Minimum { get; init; }
	public uint? Maximum { get; init; }
	public uint? Increment { get; init; }
	public bool Hidden { get; init; }
	public string AcPolicy { get; init; }
	public string DcPolicy { get; init; }
	public string PolicySource { get; init; }
	public string ReadError { get; init; }
}
