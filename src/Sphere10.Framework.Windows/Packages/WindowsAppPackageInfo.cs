// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>Detached identity and metadata of an MSIX/AppX package registered for the current Windows user.</summary>
public sealed record WindowsAppPackageInfo {
	public string Name { get; init; } = string.Empty;
	public string FullName { get; init; } = string.Empty;
	public string FamilyName { get; init; } = string.Empty;
	public string Version { get; init; } = string.Empty;
	public string Architecture { get; init; } = string.Empty;
	public string Publisher { get; init; } = string.Empty;
	public string PublisherDisplayName { get; init; } = string.Empty;
	public string DisplayName { get; init; } = string.Empty;
	public string InstallLocation { get; init; } = string.Empty;
	public string LogoUri { get; init; } = string.Empty;
	public string SignatureKind { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public bool IsFramework { get; init; }
	public bool IsResourcePackage { get; init; }
	public bool IsBundle { get; init; }
	public bool IsOptional { get; init; }
	public bool? NonRemovable { get; init; }
	public string MetadataError { get; init; } = string.Empty;
}
