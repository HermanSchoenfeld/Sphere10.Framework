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

public sealed record CodeBlock : VisualNode {
	public string Code { get; init; } = "";

	public CodeLanguage Language { get; init; } = CodeLanguage.Text;

	public VisualInline[] Caption { get; init; } = Array.Empty<VisualInline>();
}
