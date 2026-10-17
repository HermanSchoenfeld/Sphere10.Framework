// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Detached native Shell folder-item fields. Dates retain the provider's local/unspecified convention.</summary>
public sealed record WindowsShellItemInfo {
	public string Namespace { get; init; }
	public string ParsingName { get; init; }
	public string Name { get; init; }
	public string Type { get; init; }
	public bool IsFolder { get; init; }
	public long? SizeBytes { get; init; }
	public string OriginalDirectory { get; init; }
	public DateTime? DateDeleted { get; init; }
	public string Availability { get; init; }
}
