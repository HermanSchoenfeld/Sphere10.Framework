// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Web.VisualRenderer;

/// <summary>A materialized web preview. The source adapter owns metadata retrieval and persistence.</summary>
public sealed record BookmarkBlock : VisualNode {

	public string Url { get; init; } = "";

	public string Title { get; init; } = "";

	public string Description { get; init; } = "";

	public string ImageUrl { get; init; } = "";

	public string IconUrl { get; init; } = "";

	public VisualInline[] Caption { get; init; } = Array.Empty<VisualInline>();
}
