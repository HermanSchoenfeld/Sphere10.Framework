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

/// <summary>What occupies a strip of a window's title bar: the physical rectangles of its buttons, tabs, icons and text.</summary>
public sealed record WindowsTitleBarContent {
	/// <summary>How the content was found; <see cref="WindowsTitleBarContentSource.None"/> when it could not be read.</summary>
	public WindowsTitleBarContentSource Source { get; init; }

	/// <summary>Physical desktop rectangles of the content, each within the strip that was read.</summary>
	public Rectangle[] Occupied { get; init; } = Array.Empty<Rectangle>();

	/// <summary>Why the accessibility tree was not used, for content read from pixels, and why nothing could be read, for none.</summary>
	public string Availability { get; init; } = string.Empty;
}

/// <summary>How a window's title-bar content was found.</summary>
public enum WindowsTitleBarContentSource {
	/// <summary>Neither the application's accessibility tree nor its pixels could be read.</summary>
	None = 0,

	/// <summary>The application reported its title-bar controls to accessibility clients.</summary>
	Accessibility = 1,

	/// <summary>The application reported nothing there, and the window's own pixels were read.</summary>
	Pixels = 2
}
