// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.VisualRenderer;

/// <summary>Opaque source identity is separate from the DOM identity of this occurrence.</summary>
public sealed record Metadata {
	public string SourceId { get; init; }

	public string Anchor { get; init; }

	public string ObjectType { get; init; } = "block";

	public string Type { get; init; }
}
