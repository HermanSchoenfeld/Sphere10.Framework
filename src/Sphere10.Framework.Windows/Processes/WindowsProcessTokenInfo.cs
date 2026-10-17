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
}

