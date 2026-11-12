// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Explicit optional process sample, separate from inexpensive shared counter polling.</summary>
public sealed record WindowsProcessMetadata {
	public ProcessIdentity Identity { get; init; }
	public DateTime CapturedAtUtc { get; init; }
	public string Architecture { get; init; } = string.Empty;
	public string UserSid { get; init; } = string.Empty;
	public string UserName { get; init; } = string.Empty;
	public string IntegritySid { get; init; } = string.Empty;
	public WindowsProcessMitigation[] Mitigations { get; init; } = Array.Empty<WindowsProcessMitigation>();
	public string Availability { get; init; } = string.Empty;
}
