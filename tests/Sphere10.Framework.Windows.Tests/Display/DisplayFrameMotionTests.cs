// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, Parallelizable(ParallelScope.Children)]
public class DisplayFrameMotionTests {

	[TestCase(7)]
	[TestCase(-12)]
	[TestCase(1)]
	[TestCase(200)]
	public void FindsTheDistanceTheRowsMoved(int moved) {
		// The same content, scrolled: the page moving down by a positive distance puts each row that much higher.
		var previous = Rows(600, 0);
		var current = Rows(600, moved);

		var shift = DisplayFrameMotion.EstimateVerticalShift(previous, current, 256, out var confidence);

		Assert.That(shift, Is.EqualTo(moved));
		Assert.That(confidence, Is.GreaterThan(0.9f), "The distance explains nearly all of the difference between the frames.");
	}

	[Test]
	public void ReportsNoMovementForAnUnchangedOrFlatStripAndForUnrelatedContent() {
		var rows = Rows(400, 0);
		Assert.That(DisplayFrameMotion.EstimateVerticalShift(rows, rows, 128, out var same), Is.Zero);
		Assert.That(same, Is.Zero);

		var flat = new float[400];
		Array.Fill(flat, 57.5f);
		Assert.That(DisplayFrameMotion.EstimateVerticalShift(flat, flat, 128, out _), Is.Zero);

		// Content that was replaced, not moved, lines up at no distance: whatever is returned carries little confidence.
		DisplayFrameMotion.EstimateVerticalShift(Rows(400, 0), Rows(400, 0, seed: 99), 128, out var unrelated);
		Assert.That(unrelated, Is.LessThan(DisplayFrameMotion.DefaultMinimumConfidence));
	}

	[Test]
	public void NeverMeasuresFurtherThanAskedOrThanHalfTheStrip() {
		Assert.That(Math.Abs(DisplayFrameMotion.EstimateVerticalShift(Rows(600, 0), Rows(600, 150), 40, out _)), Is.LessThanOrEqualTo(40));
		Assert.That(Math.Abs(DisplayFrameMotion.EstimateVerticalShift(Rows(100, 0), Rows(100, 70), 256, out _)), Is.LessThanOrEqualTo(50));
		Assert.That(() => DisplayFrameMotion.EstimateVerticalShift(new float[10], new float[11], 4, out _), Throws.ArgumentException);
	}

	[Test]
	public void PrefersTheNearerOfTwoEquallyGoodDistances() {
		// Content that repeats every 20 rows fits at 5, 25, 45 and so on; the page moved the least of these.
		var previous = new float[400];
		var current = new float[400];
		for (var row = 0; row < 400; row++) {
			previous[row] = row % 20 * 10;
			current[row] = (row + 5) % 20 * 10;
		}

		Assert.That(DisplayFrameMotion.EstimateVerticalShift(previous, current, 128, out _), Is.EqualTo(5));
	}

	[Test]
	public void RowSignatureDoesNotChangeWhenColoursAreInverted() {
		var strip = Strip(16, 300, inverted: false);
		var inverted = Strip(16, 300, inverted: true);
		var signature = new float[300];
		var invertedSignature = new float[300];

		DisplayFrameMotion.ReadRowSignature(strip, signature);
		DisplayFrameMotion.ReadRowSignature(inverted, invertedSignature);

		Assert.That(invertedSignature, Is.EqualTo(signature).Within(0.01f), "A colour effect over part of a strip must not look like movement.");
		Assert.That(signature, Has.Some.Not.EqualTo(signature[0]), "Rows of different content have different numbers.");
		Assert.That(() => DisplayFrameMotion.ReadRowSignature(strip, new float[10]), Throws.ArgumentException);
	}

	[Test]
	public void StripsMustAgreeBeforeAMovementCounts() {
		// Three strips saw the page move by 8 (one a row off), one sat on content that does not scroll.
		Assert.That(DisplayFrameMotion.Combine(new[] { 8, 8, 9, 0 }, new[] { 0.9f, 0.8f, 0.7f, 0f }), Is.EqualTo(8));
		// One strip alone, across a playing video, is not a scroll.
		Assert.That(DisplayFrameMotion.Combine(new[] { 0, 0, 31, 0 }, new[] { 0f, 0f, 0.6f, 0f }), Is.Zero);
		// Strips that disagree measured noise.
		Assert.That(DisplayFrameMotion.Combine(new[] { 8, -40, 23 }, new[] { 0.5f, 0.5f, 0.5f }), Is.Zero);
		// Low confidence does not vote.
		Assert.That(DisplayFrameMotion.Combine(new[] { 8, 8 }, new[] { 0.1f, 0.9f }), Is.Zero);
		// A single measured strip is taken at its word.
		Assert.That(DisplayFrameMotion.Combine(new[] { 8 }, new[] { 0.9f }), Is.EqualTo(8));
		Assert.That(DisplayFrameMotion.Combine(ReadOnlySpan<int>.Empty, ReadOnlySpan<float>.Empty), Is.Zero);
		Assert.That(() => DisplayFrameMotion.Combine(new[] { 1 }, new float[2]), Throws.ArgumentException);
	}

	// The signature of a strip showing rows of a long page, starting at the given row of it.
	private static float[] Rows(int count, int firstRow, int seed = 7) {
		var rows = new float[count];
		for (var row = 0; row < count; row++)
			rows[row] = PageRow(row + firstRow, seed);
		return rows;
	}

	// A repeatable number for each row of an endless page, with no pattern a shorter distance could match.
	private static float PageRow(int row, int seed) => (uint)HashCode.Combine(row, seed) % 1000;

	private static DisplayFramePixels Strip(int width, int height, bool inverted) {
		var strip = new DisplayFramePixels();
		var pixels = strip.Resize(width, height);
		for (var y = 0; y < height; y++) {
			for (var x = 0; x < width; x++) {
				var value = (byte)((uint)HashCode.Combine(x / 4, y / 3) % 256);
				var offset = (y * width + x) * DisplayFramePixels.BytesPerPixel;
				pixels[offset] = inverted ? (byte)(255 - value) : value;
				pixels[offset + 1] = inverted ? (byte)(255 - value / 2) : (byte)(value / 2);
				pixels[offset + 2] = inverted ? (byte)(255 - (value ^ 0x55)) : (byte)(value ^ 0x55);
				pixels[offset + 3] = 255;
			}
		}
		return strip;
	}
}
