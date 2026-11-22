// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Sphere10.Framework.Windows;

/// <summary>Finds free room in a title bar: where a square fits beside what an application shows there, and what its pixels show.</summary>
public static class WindowsTitleBarSpace {
	/// <summary>The tolerance, per colour channel, within which neighbouring title-bar pixels count as the same plain background.</summary>
	public const int DefaultTolerance = 16;

	/// <summary>
	/// The left edge of the first place, stepping left from <paramref name="preferredLeft"/>, where a square of the given size keeps
	/// the given gap from every occupied rectangle; null when no such place lies at or right of <paramref name="minimumLeft"/>.
	/// </summary>
	/// <remarks>Each step moves just past the leftmost rectangle that is in the way, so a run of buttons is crossed in one step.</remarks>
	public static int? FindFreeLeft(int preferredLeft, int size, int gap, int minimumLeft, IReadOnlyList<Rectangle> occupied) {
		Guard.ArgumentNotNull(occupied, nameof(occupied));
		Guard.Argument(size > 0 && gap >= 0, nameof(size), "The square needs a size and a gap that is not negative.");
		var left = preferredLeft;
		while (left >= minimumLeft) {
			var candidate = left;
			var blocking = occupied.Where(area => area.Width > 0 && area.Left < candidate + size + gap && area.Right > candidate - gap).ToArray();
			if (blocking.Length == 0)
				return left;
			left = blocking.Min(area => area.Left) - gap - size;
		}
		return null;
	}

	/// <summary>The columns of a strip of pixels that hold content, as rectangles of the strip's height in its own coordinates.</summary>
	/// <remarks>
	/// A column is plain when its pixels agree with each other and with the column beside it within the tolerance, which a plain
	/// background and a smooth gradient pass and text, icons and edges do not. A colour effect over the strip changes nothing.
	/// </remarks>
	public static Rectangle[] FindContent(DisplayFramePixels strip, int tolerance = DefaultTolerance) {
		Guard.ArgumentNotNull(strip, nameof(strip));
		var content = new List<Rectangle>();
		if (strip.Width == 0 || strip.Height == 0)
			return content.ToArray();
		Span<int> previous = stackalloc int[3];
		Span<int> current = stackalloc int[3];
		var runStart = -1;
		for (var x = 0; x < strip.Width; x++) {
			var plain = IsUniformColumn(strip, x, tolerance, current) && (x == 0 || Near(current, previous, tolerance));
			current.CopyTo(previous);
			if (!plain && runStart < 0)
				runStart = x;
			else if (plain && runStart >= 0) {
				content.Add(new Rectangle(runStart, 0, x - runStart, strip.Height));
				runStart = -1;
			}
		}
		if (runStart >= 0)
			content.Add(new Rectangle(runStart, 0, strip.Width - runStart, strip.Height));
		return content.ToArray();
	}

	// Whether one column's pixels agree within the tolerance; its mean colour is left in mean.
	private static bool IsUniformColumn(DisplayFramePixels strip, int x, int tolerance, Span<int> mean) {
		Span<int> lowest = stackalloc int[] { 255, 255, 255 };
		Span<int> highest = stackalloc int[3];
		Span<int> sum = stackalloc int[3];
		for (var y = 0; y < strip.Height; y++) {
			var pixel = strip.GetRow(y).Slice(x * DisplayFramePixels.BytesPerPixel, 3);
			for (var channel = 0; channel < 3; channel++) {
				lowest[channel] = Math.Min(lowest[channel], pixel[channel]);
				highest[channel] = Math.Max(highest[channel], pixel[channel]);
				sum[channel] += pixel[channel];
			}
		}
		var uniform = true;
		for (var channel = 0; channel < 3; channel++) {
			mean[channel] = sum[channel] / strip.Height;
			uniform &= highest[channel] - lowest[channel] <= tolerance;
		}
		return uniform;
	}

	private static bool Near(ReadOnlySpan<int> first, ReadOnlySpan<int> second, int tolerance)
		=> Math.Abs(first[0] - second[0]) <= tolerance && Math.Abs(first[1] - second[1]) <= tolerance && Math.Abs(first[2] - second[2]) <= tolerance;
}
