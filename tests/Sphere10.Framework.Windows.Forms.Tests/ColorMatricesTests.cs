// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing.Imaging;
using NUnit.Framework;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ColorMatricesTests {
	[Test]
	public void IdentityLeavesMatricesUnchangedWhenMultiplied() {
		var matrix = ColorMatrices.FromArray(new[,] {
			{ 0.3f, 0.3f, 0.3f, 0f, 0f }, { 0.6f, 0.6f, 0.6f, 0f, 0f }, { 0.1f, 0.1f, 0.1f, 0f, 0f }, { 0f, 0f, 0f, 1f, 0f }, { 0f, 0f, 0f, 0f, 1f }
		});

		Assert.That(matrix.Then(ColorMatrices.Identity).ApproximatelyEquals(matrix), Is.True);
		Assert.That(ColorMatrices.Identity.Then(matrix).ApproximatelyEquals(matrix), Is.True);
		Assert.That(ColorMatrices.Identity.IsIdentity(), Is.True);
		Assert.That(matrix.IsIdentity(), Is.False);
	}

	[Test]
	public void MultiplicationFollowsTheRowVectorConvention() {
		// Applying a negative then a brightness offset must offset the inverted result, not the input.
		var negative = ColorMatrices.FromArray(new[,] {
			{ -1f, 0f, 0f, 0f, 0f }, { 0f, -1f, 0f, 0f, 0f }, { 0f, 0f, -1f, 0f, 0f }, { 0f, 0f, 0f, 1f, 0f }, { 1f, 1f, 1f, 0f, 1f }
		});
		var composed = negative.Then(ColorMatrices.CreateBrightness(0.25f));

		Assert.That(composed[0, 0], Is.EqualTo(-1f));
		Assert.That(composed[4, 0], Is.EqualTo(1.25f).Within(0.00001f));
	}

	[Test]
	public void ContrastScalesAroundMidGray() {
		var contrast = ColorMatrices.CreateContrast(2f);

		Assert.That(contrast[0, 0], Is.EqualTo(2f));
		Assert.That(contrast[4, 0], Is.EqualTo(-0.5f));
		Assert.That(contrast[3, 3], Is.EqualTo(1f));
		Assert.That(contrast[4, 4], Is.EqualTo(1f));
	}

	[Test]
	public void InterpolationEndsExactlyAtTheTargetAndExcludesTheStart() {
		var frames = ColorMatrices.Interpolate(ColorMatrices.Identity, ColorMatrices.CreateBrightness(1f), 4);

		Assert.That(frames, Has.Length.EqualTo(4));
		Assert.That(frames[0][4, 0], Is.EqualTo(0.25f).Within(0.00001f));
		Assert.That(frames[3].ApproximatelyEquals(ColorMatrices.CreateBrightness(1f)), Is.True);
	}

	[Test]
	public void ArraysRoundTripAndRejectInvalidShapesOrValues() {
		var matrix = ColorMatrices.CreateContrast(1.5f);

		Assert.That(ColorMatrices.FromArray(matrix.ToArray()).ApproximatelyEquals(matrix), Is.True);
		Assert.That(new ColorMatrix(matrix.ToJaggedArray()).ApproximatelyEquals(matrix), Is.True);
		Assert.That(() => ColorMatrices.FromArray(new float[4, 5]), Throws.ArgumentException);
		Assert.That(() => ColorMatrices.FromArray(new[,] { { float.NaN, 0f, 0f, 0f, 0f }, { 0f, 1f, 0f, 0f, 0f }, { 0f, 0f, 1f, 0f, 0f }, { 0f, 0f, 0f, 1f, 0f }, { 0f, 0f, 0f, 0f, 1f } }), Throws.ArgumentException);
		Assert.That(() => ColorMatrices.CreateContrast(-1f), Throws.ArgumentException);
	}

	[Test]
	public void CloneIsIndependent() {
		var matrix = ColorMatrices.CreateBrightness(0.5f);
		var clone = matrix.Clone();
		clone[4, 0] = 0f;

		Assert.That(matrix[4, 0], Is.EqualTo(0.5f));
	}
}
