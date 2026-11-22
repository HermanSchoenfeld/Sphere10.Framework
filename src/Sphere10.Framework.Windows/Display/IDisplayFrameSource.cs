// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;

namespace Sphere10.Framework.Windows;

/// <summary>Hands out rectangles of one display's frames as the desktop composes them.</summary>
/// <remarks>A source belongs to one thread at a time. Rectangles are physical desktop pixels; whatever lies outside the display is left out.</remarks>
public interface IDisplayFrameSource : IDisposable {
	/// <summary>The display's rectangle on the desktop, in physical pixels.</summary>
	Rectangle Bounds { get; }

	/// <summary>
	/// Waits for the display's next changed frame and copies the given rectangles of it, one buffer each. Returns false when no
	/// frame arrived in time or the display could not be read just now; the buffers are then unchanged.
	/// </summary>
	bool TryRead(TimeSpan timeout, ReadOnlySpan<Rectangle> areas, ReadOnlySpan<DisplayFramePixels> pixels);
}
