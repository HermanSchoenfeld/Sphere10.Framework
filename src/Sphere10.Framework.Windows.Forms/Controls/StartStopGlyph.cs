// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Draws the green start triangle and the red stop square shared by the start/stop buttons.</summary>
internal static class StartStopGlyph {
	public static readonly Color StartColor = Color.FromArgb(46, 160, 67);
	public static readonly Color StopColor = Color.FromArgb(209, 52, 56);

	/// <summary>Creates a caller-owned square image of the stop square, or of the start triangle.</summary>
	public static Bitmap Create(bool stop, int size) {
		Guard.ArgumentInRange(size, 8, 256, nameof(size));
		var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
		using var graphics = Graphics.FromImage(bitmap);
		using var brush = new SolidBrush(stop ? StopColor : StartColor);
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.PixelOffsetMode = PixelOffsetMode.Half;
		var inset = size * 0.19f;
		if (stop)
			graphics.FillRectangle(brush, inset, inset, size - 2 * inset, size - 2 * inset);
		else
			graphics.FillPolygon(brush, new[] { new PointF(inset + size * 0.05f, inset - size * 0.03f), new PointF(size - inset + size * 0.03f, size / 2f),
				new PointF(inset + size * 0.05f, size - inset + size * 0.03f) });
		return bitmap;
	}
}
