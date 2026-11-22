// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A click on a title-bar button: the borrowed window it belongs to and the button's physical screen bounds.</summary>
/// <remarks>The window is detached; pass it back to the verified window calls before acting on it. Open a menu at the bounds.</remarks>
public sealed record WindowTitleBarButtonClick(WindowsWindowInfo Window, Rectangle Bounds);
