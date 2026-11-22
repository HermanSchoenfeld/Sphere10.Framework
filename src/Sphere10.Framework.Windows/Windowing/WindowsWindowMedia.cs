// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;

namespace Sphere10.Framework.Windows;

/// <summary>One known media element of a window, in physical desktop pixels: its whole box and the area that shows it.</summary>
/// <remarks>
/// The box can lie partly or wholly outside the area, as a picture does that is cut by the edge of a scrolling page or has
/// not scrolled into view yet. What is visible is the box within the area. Scrolling moves the box and leaves the area in place.
/// </remarks>
public readonly record struct WindowsWindowMedia(string Id, WindowsUiAutomationMediaKind Kind, Rectangle Extent, Rectangle Clip) {
	/// <summary>The visible part of the element; empty while it is out of view.</summary>
	public Rectangle Visible => Rectangle.Intersect(Extent, Clip);
}
