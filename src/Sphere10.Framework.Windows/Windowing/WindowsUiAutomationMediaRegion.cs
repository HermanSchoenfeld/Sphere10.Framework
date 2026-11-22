// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;

namespace Sphere10.Framework.Windows;

/// <summary>Detached media identity and visible rectangular bounds in physical desktop pixels.</summary>
public readonly record struct WindowsUiAutomationMediaRegion(WindowsUiAutomationMediaKind Kind, string RuntimeId, RectangleF Bounds) {
	/// <summary>The element's whole box as its provider reports it, which can reach beyond the area that shows it. Empty when the reader did not record it.</summary>
	public RectangleF Extent { get; init; }

	/// <summary>The area that shows the element: its window's visible frame cut down by every scrolling ancestor. <see cref="Bounds"/> is <see cref="Extent"/> within it.</summary>
	public RectangleF Clip { get; init; }
}
