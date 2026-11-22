// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Numerics;

namespace Sphere10.Framework.Windows;

/// <summary>Measures how far the content of a display rectangle moved vertically between two frames, as scrolling moves it.</summary>
/// <remarks>
/// A narrow vertical strip of the rectangle is reduced to one number a row, and the distance is the one at which the previous
/// frame's rows best line up with the current frame's. The number does not change when colours are inverted, so a colour
/// effect over part of the strip does not look like movement. Several strips across the rectangle are combined by agreement,
/// which leaves out a strip that crosses a playing video or content that does not scroll.
/// </remarks>
public static class DisplayFrameMotion {
	/// <summary>The share of the frame-to-frame difference a distance must explain before it counts as movement.</summary>
	public const float DefaultMinimumConfidence = 0.25f;

	/// <summary>Reduces each row of a strip to one number: how much the row's pixels differ from their neighbours, and how far its brightness lies from the middle.</summary>
	public static void ReadRowSignature(DisplayFramePixels strip, Span<float> rows) {
		Guard.ArgumentNotNull(strip, nameof(strip));
		Guard.Argument(rows.Length >= strip.Height, nameof(rows), "The signature needs one number for each row.");
		for (var row = 0; row < strip.Height; row++) {
			var pixels = strip.GetRow(row);
			var edges = 0;
			var brightness = pixels[0] + pixels[1] + pixels[2];
			for (var offset = DisplayFramePixels.BytesPerPixel; offset < pixels.Length; offset += DisplayFramePixels.BytesPerPixel) {
				edges += Math.Abs(pixels[offset] - pixels[offset - 4]) + Math.Abs(pixels[offset + 1] - pixels[offset - 3]) + Math.Abs(pixels[offset + 2] - pixels[offset - 2]);
				brightness += pixels[offset] + pixels[offset + 1] + pixels[offset + 2];
			}
			rows[row] = edges + Math.Abs(brightness / (3f * strip.Width) - 127.5f);
		}
	}

	/// <summary>
	/// The number of rows by which the previous signature best lines up with the current one: positive when the content moved up,
	/// as it does when a page scrolls down. Zero when nothing moved or no distance explains the change.
	/// </summary>
	/// <param name="confidence">The share of the difference between the two frames that the returned distance explains, from 0 to 1.</param>
	public static int EstimateVerticalShift(ReadOnlySpan<float> previous, ReadOnlySpan<float> current, int maximumShift, out float confidence) {
		Guard.Argument(previous.Length == current.Length, nameof(current), "Both signatures cover the same rows.");
		Guard.Argument(maximumShift >= 0, nameof(maximumShift), "The largest distance cannot be negative.");
		confidence = 0;
		// At least half the rows must overlap for a distance to be measured at all.
		maximumShift = Math.Min(maximumShift, previous.Length / 2);
		var still = Difference(previous, current, 0);
		if (still <= float.Epsilon || maximumShift == 0)
			return 0;
		var best = 0;
		var bestError = still;
		for (var distance = 1; distance <= maximumShift; distance++) {
			// The nearer of two equally good distances wins, so repeating content is not taken for a long jump.
			var up = Difference(previous, current, distance);
			if (up < bestError) {
				bestError = up;
				best = distance;
			}
			var down = Difference(previous, current, -distance);
			if (down < bestError) {
				bestError = down;
				best = -distance;
			}
		}
		confidence = (still - bestError) / still;
		return best;
	}

	/// <summary>
	/// The distance the strips agree on: the one most of them measured, within one row, with enough confidence. Two strips must
	/// agree unless only one was measured, so a single strip that crosses moving pictures of its own does not pass for a scroll.
	/// Zero when they do not agree or none saw movement.
	/// </summary>
	public static int Combine(ReadOnlySpan<int> shifts, ReadOnlySpan<float> confidences, float minimumConfidence = DefaultMinimumConfidence) {
		Guard.Argument(shifts.Length == confidences.Length, nameof(confidences), "Each distance needs its confidence.");
		var best = 0;
		var bestVotes = 0;
		var bestWeight = 0f;
		for (var candidate = 0; candidate < shifts.Length; candidate++) {
			if (shifts[candidate] == 0 || confidences[candidate] < minimumConfidence)
				continue;
			var votes = 0;
			var weight = 0f;
			for (var other = 0; other < shifts.Length; other++) {
				if (shifts[other] == 0 || confidences[other] < minimumConfidence || Math.Abs(shifts[other] - shifts[candidate]) > 1)
					continue;
				votes++;
				weight += confidences[other];
			}
			if (votes > bestVotes || votes == bestVotes && weight > bestWeight) {
				best = shifts[candidate];
				bestVotes = votes;
				bestWeight = weight;
			}
		}
		return bestVotes >= Math.Min(2, shifts.Length) ? best : 0;
	}

	// The mean difference between the previous rows and the current rows the given distance further up.
	private static float Difference(ReadOnlySpan<float> previous, ReadOnlySpan<float> current, int distance) {
		var from = Math.Max(0, distance);
		var count = previous.Length - Math.Abs(distance);
		var first = previous.Slice(from, count);
		var second = current.Slice(from - distance, count);
		var sum = 0f;
		var index = 0;
		if (Vector.IsHardwareAccelerated && count >= Vector<float>.Count) {
			var total = Vector<float>.Zero;
			for (; index <= count - Vector<float>.Count; index += Vector<float>.Count)
				total += Vector.Abs(new Vector<float>(first.Slice(index)) - new Vector<float>(second.Slice(index)));
			sum = Vector.Sum(total);
		}
		for (; index < count; index++)
			sum += Math.Abs(first[index] - second[index]);
		return sum / count;
	}
}
