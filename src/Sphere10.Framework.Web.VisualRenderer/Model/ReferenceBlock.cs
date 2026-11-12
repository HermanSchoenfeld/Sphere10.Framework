// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.VisualRenderer;

public sealed record ReferenceBlock : VisualNode {
	public Reference Reference { get; init; } = new();

	/// <summary>Marks a contained page's parent-facing reference, independently of its destination document.</summary>
	public bool IsEmbeddedPage { get; init; }
}
