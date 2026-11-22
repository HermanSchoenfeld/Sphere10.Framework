// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;

namespace Sphere10.Framework.Windows;

/// <summary>Physical desktop rectangles belonging to one verified top-level window, before clipping against other windows.</summary>
/// <remarks>Consumers copy the area array when retaining a request. An empty array preserves no area of the window.</remarks>
public sealed record WindowsWindowRegion(WindowsWindowInfo Window, Rectangle[] Areas) {
	/// <summary>The media elements behind the areas, including known ones that are out of view, for a consumer that follows them as the content moves. Empty when the source does not know them.</summary>
	public WindowsWindowMedia[] Media { get; init; } = Array.Empty<WindowsWindowMedia>();
}
