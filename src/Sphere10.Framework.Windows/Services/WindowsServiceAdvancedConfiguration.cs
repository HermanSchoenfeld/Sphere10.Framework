// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Services;

/// <summary>Detached read-only optional SCM configuration. Null fields and Issues preserve unavailable sections.</summary>
public sealed record WindowsServiceAdvancedConfiguration {
	public string Name { get; init; }
	public bool? DelayedAutoStart { get; init; }
	public bool? FailureActionsOnNonCrashFailures { get; init; }
	public uint? ServiceSidType { get; init; }
	public uint? PreshutdownTimeoutMilliseconds { get; init; }
	public uint? LaunchProtection { get; init; }
	public string[] RequiredPrivileges { get; init; } = Array.Empty<string>();
	public uint? FailureResetPeriodSeconds { get; init; }
	public string FailureRebootMessage { get; init; }
	public string FailureCommand { get; init; }
	public WindowsServiceFailureAction[] FailureActions { get; init; } = Array.Empty<WindowsServiceFailureAction>();
	public WindowsServiceTrigger[] Triggers { get; init; } = Array.Empty<WindowsServiceTrigger>();
	public WindowsServiceConfigurationIssue[] Issues { get; init; } = Array.Empty<WindowsServiceConfigurationIssue>();
}
