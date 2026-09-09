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

/// <summary>A materialized page, or fragment when RenderFrame is false. ShowPageHeader=false renders children directly.</summary>
public sealed record DocumentBlock : VisualNode {
	public string Title { get; init; } = "";

	public string Name { get; init; } = "";

	public string Description { get; init; } = "";

	public VisualInline[] Subtitle { get; init; } = Array.Empty<VisualInline>();

	public string[] Keywords { get; init; } = Array.Empty<string>();

	public string Author { get; init; } = "";

	public DateTimeOffset? CreatedTime { get; init; }

	public DateTimeOffset? UpdatedTime { get; init; }

	public Icon Icon { get; init; }

	public string CoverUrl { get; init; }

	public bool TitleOnCover { get; init; }

	public string PageStyle { get; init; } = "wide";

	public bool RenderFrame { get; init; } = true;

	public bool ShowPageHeader { get; init; } = true;

	public VisualNode[] Children { get; init; } = Array.Empty<VisualNode>();

	public string[] Themes { get; init; } = Array.Empty<string>();

	/// <summary>Trusted scalar presentation values, never source objects or service callbacks.</summary>
	public IReadOnlyDictionary<string, string> Tokens { get; init; } = new Dictionary<string, string>();

	public IReadOnlyDictionary<string, VisualNode> Slots { get; init; } = new Dictionary<string, VisualNode>();
}
