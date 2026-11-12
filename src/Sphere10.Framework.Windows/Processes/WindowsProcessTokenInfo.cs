// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsProcessTokenInfo {
	public ProcessIdentity Process { get; init; }
	public DateTime CapturedAtUtc { get; init; }
	public string UserSid { get; init; }
	public string OwnerSid { get; init; }
	public string PrimaryGroupSid { get; init; }
	public string IntegritySid { get; init; }
	public int SessionId { get; init; }
	public bool IsElevated { get; init; }
	public int ElevationType { get; init; }
	public int TokenType { get; init; }
	public WindowsTokenGroup[] Groups { get; init; } = Array.Empty<WindowsTokenGroup>();
	public WindowsTokenPrivilege[] Privileges { get; init; } = Array.Empty<WindowsTokenPrivilege>();
	public bool? HasRestrictions { get; init; }
	public bool? IsRestricted { get; init; }
	public bool? IsAppContainer { get; init; }
	public bool? IsLessPrivilegedAppContainer { get; init; }
	public bool? IsSandboxed { get; init; }
	public bool? UIAccess { get; init; }
	public bool? VirtualizationAllowed { get; init; }
	public bool? VirtualizationEnabled { get; init; }
	public uint? MandatoryPolicyFlags { get; init; }
	public string AppContainerSid { get; init; }
	public uint? AppContainerNumber { get; init; }
	public WindowsTokenGroup[] RestrictedSids { get; init; } = Array.Empty<WindowsTokenGroup>();
	public WindowsTokenGroup[] Capabilities { get; init; } = Array.Empty<WindowsTokenGroup>();
	public string[] Issues { get; init; } = Array.Empty<string>();
}

