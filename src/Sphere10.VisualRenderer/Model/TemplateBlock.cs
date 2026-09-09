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

/// <summary>Neutral composition extension. Scalars are literal template values; slots render typed nodes.</summary>
public sealed record TemplateBlock : VisualNode {
	public string Template { get; init; } = "";

	public IReadOnlyDictionary<string, string> Tokens { get; init; } = new Dictionary<string, string>();

	public IReadOnlyDictionary<string, VisualNode> Slots { get; init; } = new Dictionary<string, VisualNode>();

	public string[] Themes { get; init; } = Array.Empty<string>();
}
