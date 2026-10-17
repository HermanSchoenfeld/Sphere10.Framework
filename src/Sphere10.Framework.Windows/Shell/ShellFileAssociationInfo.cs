// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Windows;

/// <summary>Detached strings resolved by the Windows Shell for an extension or an explicitly selected ProgID and verb.</summary>
public sealed record ShellFileAssociationInfo {
	public string Association { get; init; } = string.Empty;
	public string Verb { get; init; }
	public bool IsProgId { get; init; }
	public string ProgId { get; init; } = string.Empty;
	public string FriendlyDocumentName { get; init; } = string.Empty;
	public string FriendlyApplicationName { get; init; } = string.Empty;
	public string ExecutablePath { get; init; } = string.Empty;
	public string Command { get; init; } = string.Empty;
	public string DefaultIcon { get; init; } = string.Empty;
	public string DelegateExecute { get; init; } = string.Empty;
	public string ApplicationId { get; init; } = string.Empty;

	/// <summary>Final native HRESULT for each queried string, keyed by its property name. Zero means success; missing or unsupported fields retain their failure.</summary>
	public Dictionary<string, int> HResults { get; init; } = new(StringComparer.Ordinal);
}
