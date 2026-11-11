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

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Draws the existing native popup resize grip in the requested corner.</summary>
public sealed class GripRenderer {
	// Attributes
	private static Bitmap _sGripBitmap;

	// Construction and destruction
	private GripRenderer() {
	}

	// Properties
	private static Bitmap GripBitmap => _sGripBitmap;

	public static void RefreshSystemColors(Graphics g, Size size) => InitializeGripBitmap(g, size, true);

	public static void Render(Graphics g, Point location, Size size, GripAlignMode mode) {
		InitializeGripBitmap(g, size, false);

		// Calculate display size and position of grip.
		switch (mode) {
			case GripAlignMode.TopLeft:
				size.Height = -size.Height;
				size.Width = -size.Width;
				break;

			case GripAlignMode.TopRight:
				size.Height = -size.Height;
				break;

			case GripAlignMode.BottomLeft:
				size.Width = -size.Height;
				break;
		}

		// Reverse size grip for left-aligned.
		if (size.Width < 0)
			location.X -= size.Width;
		if (size.Height < 0)
			location.Y -= size.Height;

		g.DrawImage(GripBitmap, location.X, location.Y, size.Width, size.Height);
	}

	public static void Render(Graphics g, Point location, GripAlignMode mode) => Render(g, location, new Size(16, 16), mode);

	// Methods
	private static void InitializeGripBitmap(Graphics graphics, Size size, bool forceRefresh) {
		if (_sGripBitmap == null || forceRefresh || size != _sGripBitmap.Size) {
			// Draw size grip into a bitmap image.
			_sGripBitmap = new Bitmap(size.Width, size.Height, graphics);
			using (var gripG = Graphics.FromImage(_sGripBitmap))
				ControlPaint.DrawSizeGrip(gripG, SystemColors.ButtonFace, 0, 0, size.Width, size.Height);
		}
	}
}
