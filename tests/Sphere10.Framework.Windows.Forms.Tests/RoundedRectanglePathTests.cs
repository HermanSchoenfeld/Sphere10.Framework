// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RoundedRectanglePathTests {
	// GraphicsPath.IsVisible tests whole pixels and excludes the pixels on the right and bottom edges, so the probes sit 1.5 px
	// inside each edge: still outside an 8 px arc, and inside a square corner.
	private static readonly RectangleF Bounds = new(10, 20, 100, 40);

	[Test]
	public void RoundedCornersLeaveTheCornerPointsOutsideAndTheEdgesInside() {
		using var path = Tools.Drawing.CreateRoundedRectanglePath(Bounds, 8);

		AssertFillsBounds(path.GetBounds());
		Assert.That(path.IsVisible(Bounds.X + 1.5f, Bounds.Y + 1.5f), Is.False, "The top-left corner is cut away.");
		Assert.That(path.IsVisible(Bounds.Right - 1.5f, Bounds.Bottom - 1.5f), Is.False, "The bottom-right corner is cut away.");
		Assert.That(path.IsVisible(Bounds.X + 50, Bounds.Y + 1.5f), Is.True, "The straight top edge is filled.");
		Assert.That(path.IsVisible(Bounds.X + 1.5f, Bounds.Y + 20), Is.True, "The straight left edge is filled.");
	}

	[Test]
	public void OnlyTheChosenCornersAreRounded() {
		using var path = Tools.Drawing.CreateRoundedRectanglePath(Bounds, 8, RoundedCorners.TopLeft | RoundedCorners.TopRight);

		Assert.That(path.IsVisible(Bounds.X + 1.5f, Bounds.Y + 1.5f), Is.False);
		Assert.That(path.IsVisible(Bounds.Right - 1.5f, Bounds.Y + 1.5f), Is.False);
		Assert.That(path.IsVisible(Bounds.X + 1.5f, Bounds.Bottom - 1.5f), Is.True, "The bottom corners stay square.");
		Assert.That(path.IsVisible(Bounds.Right - 1.5f, Bounds.Bottom - 1.5f), Is.True);
		AssertFillsBounds(path.GetBounds());
	}

	[TestCase(RoundedCorners.TopLeft)]
	[TestCase(RoundedCorners.BottomRight)]
	[TestCase(RoundedCorners.TopRight | RoundedCorners.BottomLeft)]
	public void EveryCombinationKeepsTheSquareCornersFilled(RoundedCorners corners) {
		using var path = Tools.Drawing.CreateRoundedRectanglePath(Bounds, 8, corners);

		Assert.That(path.IsVisible(Bounds.X + 1.5f, Bounds.Y + 1.5f), Is.EqualTo(!corners.HasFlag(RoundedCorners.TopLeft)));
		Assert.That(path.IsVisible(Bounds.Right - 1.5f, Bounds.Y + 1.5f), Is.EqualTo(!corners.HasFlag(RoundedCorners.TopRight)));
		Assert.That(path.IsVisible(Bounds.Right - 1.5f, Bounds.Bottom - 1.5f), Is.EqualTo(!corners.HasFlag(RoundedCorners.BottomRight)));
		Assert.That(path.IsVisible(Bounds.X + 1.5f, Bounds.Bottom - 1.5f), Is.EqualTo(!corners.HasFlag(RoundedCorners.BottomLeft)));
		Assert.That(path.IsVisible(Bounds.X + 50, Bounds.Y + 20), Is.True);
	}

	[Test]
	public void LargeRadiiAreLimitedToHalfTheShorterSideAndEmptyOrSquareRequestsGiveARectangle() {
		using var pill = Tools.Drawing.CreateRoundedRectanglePath(Bounds, 500);
		AssertFillsBounds(pill.GetBounds());
		Assert.That(pill.IsVisible(Bounds.X + 20, Bounds.Y + 20), Is.True, "The pill is filled at the centre of its rounded end.");
		Assert.That(pill.IsVisible(Bounds.X + 3, Bounds.Y + 3), Is.False);

		using var square = Tools.Drawing.CreateRoundedRectanglePath(new Rectangle(0, 0, 10, 10), 0);
		Assert.That(square.IsVisible(0.5f, 0.5f), Is.True);
		using var none = Tools.Drawing.CreateRoundedRectanglePath(Bounds, 8, RoundedCorners.None);
		Assert.That(none.IsVisible(Bounds.X + 1.5f, Bounds.Y + 1.5f), Is.True);
		using var empty = Tools.Drawing.CreateRoundedRectanglePath(RectangleF.Empty, 8);
		Assert.That(empty.PointCount, Is.GreaterThanOrEqualTo(0));
		Assert.That(() => Tools.Drawing.CreateRoundedRectanglePath(Bounds, -1), Throws.ArgumentException);
	}

	// GDI+ approximates arcs with Béziers, so the outline's bounds match the rectangle to within a few float steps.
	private static void AssertFillsBounds(RectangleF actual) {
		Assert.That(actual.X, Is.EqualTo(Bounds.X).Within(0.001f));
		Assert.That(actual.Y, Is.EqualTo(Bounds.Y).Within(0.001f));
		Assert.That(actual.Width, Is.EqualTo(Bounds.Width).Within(0.001f));
		Assert.That(actual.Height, Is.EqualTo(Bounds.Height).Within(0.001f));
	}
}
