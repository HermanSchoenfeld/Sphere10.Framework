// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.VisualRenderer;

public interface IRenderer<out TOutput> {
	TOutput Render(DocumentBlock document, RenderOptions options = null);

	/// <summary>Renders a block and its children without a document frame or page header.</summary>
	TOutput RenderSnippet(VisualNode node, RenderOptions options = null);
}
