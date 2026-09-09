// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.VisualRenderer;

/// <summary>Presentation settings; content links must already be resolved by the caller.</summary>
public sealed record RenderOptions {
	public string[] Themes { get; init; } = Array.Empty<string>();
	public RenderMode Mode { get; init; } = RenderMode.ReadOnly;
	public RenderEnvironment Environment { get; init; } = RenderEnvironment.Offline;
	public string AssetBaseUrl { get; init; }
}
