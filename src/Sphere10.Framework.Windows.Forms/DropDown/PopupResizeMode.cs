// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;
using static Sphere10.Framework.Windows.WinAPI.USER32;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Specifies the popup edges available for interactive resizing.</summary>
public enum PopupResizeMode {
	None = 0,

	// Individual styles.
	Left = 1,
	Top = 2,
	Right = 4,
	Bottom = 8,

	// Combined styles.
	All = (Top | Left | Bottom | Right),
	TopLeft = (Top | Left),
	TopRight = (Top | Right),
	BottomLeft = (Bottom | Left),
	BottomRight = (Bottom | Right),
}
