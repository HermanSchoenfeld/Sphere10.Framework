// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Files;

public sealed class WindowsFileInspection {
	public string Path { get; init; }
	public long SizeBytes { get; init; }
	public DateTime LastWriteUtc { get; init; }
	public string Sha256 { get; init; }
	public string Company { get; init; }
	public string Product { get; init; }
	public string Description { get; init; }
	public string FileVersion { get; init; }
	public string ProductVersion { get; init; }
	public string OriginalFilename { get; init; }
	public int TrustStatus { get; init; }
	public string Signer { get; init; }
	public string Issuer { get; init; }
	public string Thumbprint { get; init; }
	public int CertificateErrorCode { get; init; }
}
