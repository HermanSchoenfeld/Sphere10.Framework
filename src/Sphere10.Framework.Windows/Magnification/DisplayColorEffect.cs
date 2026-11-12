// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Drawing.Imaging;

namespace Sphere10.Framework.Windows;

/// <summary>The colour transformation one display should show, for <see cref="DisplayColorEffectRenderer"/>.</summary>
/// <param name="DeviceName">The display's GDI device name, such as <c>\\.\DISPLAY1</c> (<c>Screen.DeviceName</c>).</param>
/// <param name="Bounds">The display's desktop bounds in physical pixels, as a per-monitor DPI aware process sees them.</param>
/// <param name="Matrix">The transformation; the identity leaves the display's colours unchanged.</param>
public sealed record DisplayColorEffect(string DeviceName, Rectangle Bounds, ColorMatrix Matrix);
