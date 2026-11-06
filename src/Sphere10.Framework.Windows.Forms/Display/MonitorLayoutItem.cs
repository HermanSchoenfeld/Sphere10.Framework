// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>One display tile shown by <see cref="MonitorLayoutControl"/>; the application supplies its caption and accent.</summary>
public sealed class MonitorLayoutItem {
	/// <summary>A stable identity, such as the GDI device name, used for selection.</summary>
	public string Key { get; set; } = string.Empty;

	/// <summary>The large label drawn in the tile centre, typically the display number.</summary>
	public string Label { get; set; } = string.Empty;

	/// <summary>A short description drawn beneath the label, such as the display name or the effect applied to it.</summary>
	public string Caption { get; set; } = string.Empty;

	/// <summary>The display's desktop bounds in physical pixels; tiles are scaled proportionally into the control.</summary>
	public Rectangle Bounds { get; set; }

	public bool IsPrimary { get; set; }

	/// <summary>Whether the tile is drawn in its accent color; disabled tiles are drawn muted.</summary>
	public bool Enabled { get; set; } = true;

	public Color Accent { get; set; } = Color.SteelBlue;
}
