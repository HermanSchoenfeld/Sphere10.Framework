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

public sealed record MediaBlock : VisualNode {
	public MediaType Type { get; init; }

	public string Url { get; init; } = "";

	public string FileName { get; init; } = "";

	public string AltText { get; init; } = "";

	public string Size { get; init; }

	public VisualInline[] Caption { get; init; } = Array.Empty<VisualInline>();

	public EmbedProvider Provider { get; init; }

	public string ProviderId { get; init; }
}
