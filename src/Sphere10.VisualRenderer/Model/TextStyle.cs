// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.VisualRenderer;

public sealed record TextStyle {
	public bool Bold { get; init; }

	public bool Italic { get; init; }

	public bool Underline { get; init; }

	public bool StrikeThrough { get; init; }

	public bool Code { get; init; }

	public Color Color { get; init; }
}
