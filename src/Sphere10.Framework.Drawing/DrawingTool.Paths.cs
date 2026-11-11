// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using Sphere10.Framework;

// ReSharper disable CheckNamespace
namespace Tools;

public partial class Drawing {
	/// <summary>
	/// Creates a closed rectangle outline whose chosen corners are quarter circles of the given radius; the other corners stay square.
	/// The radius is limited to half the shorter side, so a large radius gives a pill or a circle, and an empty rectangle gives a
	/// plain rectangle figure. The caller owns the path.
	/// </summary>
	[SupportedOSPlatform("windows")]
	public static GraphicsPath CreateRoundedRectanglePath(RectangleF bounds, float radius, RoundedCorners corners = RoundedCorners.All) {
		Guard.Argument(radius >= 0 && !float.IsNaN(radius) && !float.IsInfinity(radius), nameof(radius), "The radius must be a finite, non-negative value.");
		var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
		var path = new GraphicsPath();
		if (diameter <= 0 || corners == RoundedCorners.None) {
			path.AddRectangle(bounds);
			return path;
		}
		// Clockwise from the top-left, each corner is an arc or, when square, a line from the corner point halfway along the next
		// edge (GDI+ drops zero-length lines, so a bare corner point would be lost). The implicit joins draw the remaining edges.
		var halfWidth = bounds.Width / 2;
		var halfHeight = bounds.Height / 2;
		if (corners.HasFlag(RoundedCorners.TopLeft))
			path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
		else
			path.AddLine(bounds.X, bounds.Y, bounds.X + halfWidth, bounds.Y);
		if (corners.HasFlag(RoundedCorners.TopRight))
			path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
		else
			path.AddLine(bounds.Right, bounds.Y, bounds.Right, bounds.Y + halfHeight);
		if (corners.HasFlag(RoundedCorners.BottomRight))
			path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
		else
			path.AddLine(bounds.Right, bounds.Bottom, bounds.Right - halfWidth, bounds.Bottom);
		if (corners.HasFlag(RoundedCorners.BottomLeft))
			path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
		else
			path.AddLine(bounds.X, bounds.Bottom, bounds.X, bounds.Bottom - halfHeight);
		path.CloseFigure();
		return path;
	}

	/// <inheritdoc cref="CreateRoundedRectanglePath(RectangleF, float, RoundedCorners)"/>
	[SupportedOSPlatform("windows")]
	public static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius, RoundedCorners corners = RoundedCorners.All)
		=> CreateRoundedRectanglePath((RectangleF)bounds, radius, corners);
}
