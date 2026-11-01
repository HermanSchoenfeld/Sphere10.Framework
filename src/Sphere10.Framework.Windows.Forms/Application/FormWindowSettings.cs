// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Per-user window placement, stored in the coordinate system and DPI used by the application.</summary>
public class FormWindowSettings : SettingsObject {
	public Rectangle Bounds { get; set; }

	public Rectangle WorkingArea { get; set; }

	public string MonitorDeviceName { get; set; } = string.Empty;

	public int Dpi { get; set; } = 96;

	public int? NavigationPaneWidth { get; set; }

	public FormWindowState WindowState { get; set; } = FormWindowState.Normal;
}