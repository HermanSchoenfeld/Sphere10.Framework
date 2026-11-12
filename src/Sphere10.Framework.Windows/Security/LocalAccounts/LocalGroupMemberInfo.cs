// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;

namespace Sphere10.Framework.Windows.Security;

[ReadOnly(true)]
public sealed record LocalGroupMemberInfo {
	[DisplayName("Account"), Description("The exact computer\\account or domain\\account identity.")]
	public string Identity { get; init; } = string.Empty;

	[DisplayName("SID"), Description("The member security identifier, including unresolved identities.")]
	public string SID { get; init; } = string.Empty;

	[DisplayName("Type"), Description("The native account type reported by Windows.")]
	public string Type { get; init; } = string.Empty;
}
