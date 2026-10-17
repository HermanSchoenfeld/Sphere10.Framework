// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Files;

public sealed record FileHandleCapture {
	private readonly FileHandleInfo[] _rows = Array.Empty<FileHandleInfo>();

	public FileHandleInfo[] Rows {
		get => (FileHandleInfo[])_rows.Clone();
		init => _rows = value == null ? Array.Empty<FileHandleInfo>() : (FileHandleInfo[])value.Clone();
	}
	public DateTime CapturedAtUtc { get; init; }
	public bool IsPartial { get; init; }
	public string Availability { get; init; } = string.Empty;
}
