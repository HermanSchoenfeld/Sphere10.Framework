// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
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
	public void TransformUsesTheRowVectorConventionAndClampsEachChannel() {
		var negative = ColorMatrices.FromArray(new[,] {
			{ -1f, 0f, 0f, 0f, 0f }, { 0f, -1f, 0f, 0f, 0f }, { 0f, 0f, -1f, 0f, 0f }, { 0f, 0f, 0f, 1f, 0f }, { 1f, 1f, 1f, 0f, 1f }
		});

		Assert.That(negative.Transform(Color.FromArgb(200, 10, 20, 30)), Is.EqualTo(Color.FromArgb(200, 245, 235, 225)));
		Assert.That(ColorMatrices.CreateBrightness(1f).Transform(Color.FromArgb(255, 128, 0, 64)), Is.EqualTo(Color.FromArgb(255, 255, 255, 255)), "Channels above 1 clamp to 255.");
		Assert.That(ColorMatrices.CreateBrightness(-1f).Transform(Color.White), Is.EqualTo(Color.FromArgb(255, 0, 0, 0)), "Channels below 0 clamp to 0.");
	}

	[Test]
	public void ApplyToDrawsTheImageThroughTheMatrixIntoANewBitmapOfTheSameSize() {
		using var source = new Bitmap(4, 3, PixelFormat.Format32bppArgb);
		using (var graphics = Graphics.FromImage(source))
			graphics.Clear(Color.FromArgb(255, 10, 20, 30));
		var negative = ColorMatrices.FromArray(new[,] {
			{ -1f, 0f, 0f, 0f, 0f }, { 0f, -1f, 0f, 0f, 0f }, { 0f, 0f, -1f, 0f, 0f }, { 0f, 0f, 0f, 1f, 0f }, { 1f, 1f, 1f, 0f, 1f }
		});

		using var result = negative.ApplyTo(source);

		Assert.That(result, Is.Not.SameAs(source));
		Assert.That(result.Size, Is.EqualTo(source.Size));
		Assert.That(result.PixelFormat, Is.EqualTo(PixelFormat.Format32bppArgb));
		var pixel = result.GetPixel(2, 1);
		Assert.That(pixel.R, Is.EqualTo(245).Within(1));
		Assert.That(pixel.G, Is.EqualTo(235).Within(1));
		Assert.That(pixel.B, Is.EqualTo(225).Within(1));
		Assert.That(source.GetPixel(2, 1), Is.EqualTo(Color.FromArgb(255, 10, 20, 30)), "The source is left unchanged.");
		Assert.That(() => negative.ApplyTo(null), Throws.ArgumentNullException);
	}

	[Test]
	public void StandardFactoriesMatchTheirDefinitions() {
		Assert.That(ColorMatrices.CreateInversion().Transform(Color.FromArgb(255, 10, 20, 30)), Is.EqualTo(Color.FromArgb(255, 245, 235, 225)));
		Assert.That(ColorMatrices.CreateSaturation(1f).ApproximatelyEquals(ColorMatrices.Identity), Is.True);
		Assert.That(ColorMatrices.CreateHueRotation(0f).ApproximatelyEquals(ColorMatrices.Identity, 0.0001f), Is.True);
		Assert.That(ColorMatrices.CreateHueRotation(360f).ApproximatelyEquals(ColorMatrices.Identity, 0.0001f), Is.True);
		Assert.That(ColorMatrices.CreateSaturation(0f).ApproximatelyEquals(ColorMatrices.CreateMonochrome(), 0.001f), Is.True, "Zero saturation is the W3C grayscale.");
		var gray = ColorMatrices.CreateMonochrome().Transform(Color.FromArgb(255, 255, 0, 0));
		Assert.That((gray.R, gray.G, gray.B), Is.EqualTo((54, 54, 54)), "BT.709 red luma is 0.2126.");
		var red = ColorMatrices.CreateMonochrome(1f, 0f, 0f).Transform(Color.White);
		Assert.That((red.R, red.G, red.B), Is.EqualTo((255, 0, 0)));
		var sepia = ColorMatrices.CreateSepia().Transform(Color.FromArgb(255, 100, 100, 100));
		Assert.That((sepia.R, sepia.G, sepia.B), Is.EqualTo((135, 120, 94)), "Sepia rows sum to 1.351, 1.203 and 0.937.");
	}

	[Test]
	public void RgbTransformsUseTheColumnVectorConvention() {
		// Output red takes the input green: the transform's [0, 1] lands in the GDI+ matrix's [1, 0].
		var swap = ColorMatrices.FromRgbTransform(new[,] { { 0f, 1f, 0f }, { 1f, 0f, 0f }, { 0f, 0f, 1f } });

		Assert.That(swap[1, 0], Is.EqualTo(1f));
		Assert.That(swap.Transform(Color.FromArgb(255, 10, 200, 30)), Is.EqualTo(Color.FromArgb(255, 200, 10, 30)));
		Assert.That(() => ColorMatrices.FromRgbTransform(new float[2, 3]), Throws.ArgumentException);
	}

	[Test]
	public void SmartInversionKeepsHuesAndSwapsLightAndDark() {
		var smart = ColorMatrices.CreateInversion().Then(ColorMatrices.CreateHueRotation(180f));

		Assert.That(smart.Transform(Color.White), Is.EqualTo(Color.FromArgb(255, 0, 0, 0)));
		Assert.That(smart.Transform(Color.Black), Is.EqualTo(Color.FromArgb(255, 255, 255, 255)));
		var red = smart.Transform(Color.FromArgb(255, 200, 40, 40));
		Assert.That(red.R, Is.GreaterThan(red.G).And.GreaterThan(red.B), "A red stays red.");
	}

	[TestCase(ColorVisionDeficiency.Protanopia)]
	[TestCase(ColorVisionDeficiency.Protanomaly)]
	[TestCase(ColorVisionDeficiency.Deuteranopia)]
	[TestCase(ColorVisionDeficiency.Deuteranomaly)]
	[TestCase(ColorVisionDeficiency.Tritanopia)]
	[TestCase(ColorVisionDeficiency.Tritanomaly)]
	[TestCase(ColorVisionDeficiency.Achromatopsia)]
	[TestCase(ColorVisionDeficiency.Achromatomaly)]
	public void ColorVisionDeficienciesKeepNeutralsAndChangeColours(ColorVisionDeficiency deficiency) {
		var matrix = ColorMatrices.CreateColorVisionDeficiency(deficiency);

		Assert.That(matrix.Transform(Color.White), Is.EqualTo(Color.FromArgb(255, 255, 255, 255)), "Each row of the model sums to one, so white stays white.");
		Assert.That(matrix.Transform(Color.Black), Is.EqualTo(Color.FromArgb(255, 0, 0, 0)));
		Assert.That(matrix.Transform(Color.FromArgb(255, 200, 30, 30)), Is.Not.EqualTo(Color.FromArgb(255, 200, 30, 30)));
	}

	[Test]
	public void ThresholdSplitsAtTheLevel() {
		var threshold = ColorMatrices.CreateThreshold(0.5f);

		Assert.That(threshold.Transform(Color.FromArgb(255, 140, 140, 140)), Is.EqualTo(Color.FromArgb(255, 255, 255, 255)));
		Assert.That(threshold.Transform(Color.FromArgb(255, 115, 115, 115)), Is.EqualTo(Color.FromArgb(255, 0, 0, 0)));
		Assert.That(() => ColorMatrices.CreateThreshold(2f), Throws.ArgumentException);
	}

	[Test]
	public void RendererUsesOneFullScreenMatrixOnlyWhenEveryDisplayMatches() {
		var grayscale = ColorMatrices.CreateMonochrome();
		var left = new DisplayColorEffect(@"\\.\DISPLAY1", new Rectangle(0, 0, 1920, 1080), grayscale);
		var right = new DisplayColorEffect(@"\\.\DISPLAY2", new Rectangle(1920, 0, 1920, 1080), ColorMatrices.CreateMonochrome());

		Assert.That(DisplayColorEffectRenderer.GetSharedMatrix(new[] { left, right }).ApproximatelyEquals(grayscale), Is.True);
		Assert.That(DisplayColorEffectRenderer.GetSharedMatrix(new[] { left, right with { Matrix = ColorMatrices.Identity } }), Is.Null);
		Assert.That(DisplayColorEffectRenderer.GetSharedMatrix(new DisplayColorEffect[0]).IsIdentity(), Is.True);
	}

	[Test]
	public void CloneIsIndependent() {
		var matrix = ColorMatrices.CreateBrightness(0.5f);
		var clone = matrix.Clone();
		clone[4, 0] = 0f;

		Assert.That(matrix[4, 0], Is.EqualTo(0.5f));
	}
}
