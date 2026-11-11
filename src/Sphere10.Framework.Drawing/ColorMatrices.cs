// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Versioning;

namespace Sphere10.Framework.Drawing;

/// <summary>Builds and combines 5x5 GDI+ color matrices using the row-vector convention shared by GDI+ and the Magnification API.</summary>
[SupportedOSPlatform("windows6.1")]
public static class ColorMatrices {
	public const int Size = 5;

	// ITU-R BT.709 luma weights.
	private const float LumaRed = 0.2126f;
	private const float LumaGreen = 0.7152f;
	private const float LumaBlue = 0.0722f;

	public static ColorMatrix Identity => new ColorMatrix();

	/// <summary>Creates a matrix from a 5x5 array indexed [row, column].</summary>
	public static ColorMatrix FromArray(float[,] values) {
		Guard.ArgumentNotNull(values, nameof(values));
		Guard.Argument(values.GetLength(0) == Size && values.GetLength(1) == Size, nameof(values), "A color matrix requires 5x5 values.");
		var matrix = new ColorMatrix();
		for (var row = 0; row < Size; row++)
			for (var column = 0; column < Size; column++) {
				var value = values[row, column];
				Guard.Argument(!float.IsNaN(value) && !float.IsInfinity(value), nameof(values), "Color matrix values must be finite.");
				matrix[row, column] = value;
			}
		return matrix;
	}

	/// <summary>Returns the product <paramref name="first"/> x <paramref name="second"/>, applying <paramref name="first"/> to a color before <paramref name="second"/>.</summary>
	public static ColorMatrix Multiply(ColorMatrix first, ColorMatrix second) {
		Guard.ArgumentNotNull(first, nameof(first));
		Guard.ArgumentNotNull(second, nameof(second));
		var result = new ColorMatrix();
		for (var row = 0; row < Size; row++)
			for (var column = 0; column < Size; column++) {
				var sum = 0.0f;
				for (var index = 0; index < Size; index++)
					sum += first[row, index] * second[index, column];
				result[row, column] = sum;
			}
		return result;
	}

	/// <summary>A translation that adds <paramref name="brightness"/> (-1 to 1) to every color channel.</summary>
	public static ColorMatrix CreateBrightness(float brightness) {
		Guard.Argument(!float.IsNaN(brightness) && !float.IsInfinity(brightness), nameof(brightness), "Brightness must be finite.");
		var matrix = new ColorMatrix();
		matrix[4, 0] = matrix[4, 1] = matrix[4, 2] = brightness;
		return matrix;
	}

	/// <summary>Scales every color channel around mid-gray by <paramref name="contrast"/>; 1 leaves colors unchanged.</summary>
	public static ColorMatrix CreateContrast(float contrast) {
		Guard.Argument(!float.IsNaN(contrast) && !float.IsInfinity(contrast) && contrast >= 0, nameof(contrast), "Contrast must be finite and non-negative.");
		var matrix = new ColorMatrix();
		matrix[0, 0] = matrix[1, 1] = matrix[2, 2] = contrast;
		matrix[4, 0] = matrix[4, 1] = matrix[4, 2] = 0.5f * (1.0f - contrast);
		return matrix;
	}

	/// <summary>
	/// Creates a matrix from a 3x3 RGB transform in the column-vector form that colour-science papers and the W3C filter
	/// specification use (output channel r = sum over c of <c>transform[r, c]</c> x input channel c); alpha is kept.
	/// </summary>
	public static ColorMatrix FromRgbTransform(float[,] transform) {
		Guard.ArgumentNotNull(transform, nameof(transform));
		Guard.Argument(transform.GetLength(0) == 3 && transform.GetLength(1) == 3, nameof(transform), "An RGB transform requires 3x3 values.");
		var matrix = new ColorMatrix();
		for (var output = 0; output < 3; output++)
			for (var input = 0; input < 3; input++) {
				var value = transform[output, input];
				Guard.Argument(!float.IsNaN(value) && !float.IsInfinity(value), nameof(transform), "RGB transform values must be finite.");
				matrix[input, output] = value;
			}
		return matrix;
	}

	/// <summary>The negative: every colour channel becomes 1 minus itself; alpha is kept.</summary>
	public static ColorMatrix CreateInversion() {
		var matrix = new ColorMatrix();
		matrix[0, 0] = matrix[1, 1] = matrix[2, 2] = -1.0f;
		matrix[4, 0] = matrix[4, 1] = matrix[4, 2] = 1.0f;
		return matrix;
	}

	/// <summary>
	/// Replaces each colour with its luma (ITU-R BT.709 weights, as the W3C <c>grayscale</c> filter uses) scaled per channel by
	/// <paramref name="red"/>, <paramref name="green"/> and <paramref name="blue"/>: (1, 1, 1) is grayscale, (1, 0, 0) a red monochrome.
	/// </summary>
	public static ColorMatrix CreateMonochrome(float red = 1.0f, float green = 1.0f, float blue = 1.0f) {
		Guard.Argument(new[] { red, green, blue }.All(IsFinite), nameof(red), "Channel weights must be finite.");
		var weights = new[] { red, green, blue };
		var matrix = new ColorMatrix();
		for (var output = 0; output < 3; output++) {
			matrix[0, output] = LumaRed * weights[output];
			matrix[1, output] = LumaGreen * weights[output];
			matrix[2, output] = LumaBlue * weights[output];
		}
		return matrix;
	}

	/// <summary>The sepia tone of the W3C Filter Effects <c>sepia</c> function at full amount (the coefficients Microsoft documents for GDI+).</summary>
	public static ColorMatrix CreateSepia() {
		return FromRgbTransform(new[,] {
			{ 0.393f, 0.769f, 0.189f },
			{ 0.349f, 0.686f, 0.168f },
			{ 0.272f, 0.534f, 0.131f }
		});
	}

	/// <summary>The W3C Filter Effects <c>saturate</c> matrix: 0 is grayscale, 1 leaves colours unchanged and values above 1 intensify them.</summary>
	public static ColorMatrix CreateSaturation(float saturation) {
		Guard.Argument(IsFinite(saturation) && saturation >= 0, nameof(saturation), "Saturation must be finite and non-negative.");
		var s = saturation;
		return FromRgbTransform(new[,] {
			{ 0.213f + 0.787f * s, 0.715f - 0.715f * s, 0.072f - 0.072f * s },
			{ 0.213f - 0.213f * s, 0.715f + 0.285f * s, 0.072f - 0.072f * s },
			{ 0.213f - 0.213f * s, 0.715f - 0.715f * s, 0.072f + 0.928f * s }
		});
	}

	/// <summary>The W3C Filter Effects <c>hue-rotate</c> matrix, which turns hues by <paramref name="degrees"/> while roughly keeping luminance.</summary>
	public static ColorMatrix CreateHueRotation(float degrees) {
		Guard.Argument(IsFinite(degrees), nameof(degrees), "The angle must be finite.");
		var radians = degrees * Math.PI / 180.0;
		var cos = (float)Math.Cos(radians);
		var sin = (float)Math.Sin(radians);
		return FromRgbTransform(new[,] {
			{ 0.213f + cos * 0.787f - sin * 0.213f, 0.715f - cos * 0.715f - sin * 0.715f, 0.072f - cos * 0.072f + sin * 0.928f },
			{ 0.213f - cos * 0.213f + sin * 0.143f, 0.715f + cos * 0.285f + sin * 0.140f, 0.072f - cos * 0.072f - sin * 0.283f },
			{ 0.213f - cos * 0.213f - sin * 0.787f, 0.715f - cos * 0.715f + sin * 0.715f, 0.072f + cos * 0.928f + sin * 0.072f }
		});
	}

	/// <summary>
	/// Black and white: colours whose luma is at least <paramref name="level"/> (0 to 1) become white and the rest black, with a
	/// one-step ramp at the threshold.
	/// </summary>
	public static ColorMatrix CreateThreshold(float level = 0.5f) {
		Guard.Argument(IsFinite(level) && level >= 0 && level <= 1, nameof(level), "The threshold must be between 0 and 1.");
		const float steepness = 255.0f;
		var matrix = CreateMonochrome(steepness, steepness, steepness);
		matrix[4, 0] = matrix[4, 1] = matrix[4, 2] = 0.5f - level * steepness;
		return matrix;
	}

	/// <summary>
	/// Simulates a colour vision deficiency. Dichromacies and anomalous trichromacies use the physiologically based model of
	/// Machado, Oliveira and Fernandes (IEEE TVCG 15(6), 2009) at severity 1.0 and 0.6; achromatopsia is BT.709 grayscale and
	/// achromatomaly keeps 40 % saturation. Like most screen tools it applies the model to gamma-encoded values.
	/// </summary>
	public static ColorMatrix CreateColorVisionDeficiency(ColorVisionDeficiency deficiency) {
		return deficiency switch {
			ColorVisionDeficiency.Protanopia => FromRgbTransform(new[,] {
				{ 0.152286f, 1.052583f, -0.204868f },
				{ 0.114503f, 0.786281f, 0.099216f },
				{ -0.003882f, -0.048116f, 1.051998f }
			}),
			ColorVisionDeficiency.Protanomaly => FromRgbTransform(new[,] {
				{ 0.385450f, 0.769005f, -0.154455f },
				{ 0.100526f, 0.829802f, 0.069673f },
				{ -0.007345f, -0.011366f, 1.018711f }
			}),
			ColorVisionDeficiency.Deuteranopia => FromRgbTransform(new[,] {
				{ 0.367322f, 0.860646f, -0.227968f },
				{ 0.280085f, 0.672501f, 0.047413f },
				{ -0.011820f, 0.042940f, 0.968881f }
			}),
			ColorVisionDeficiency.Deuteranomaly => FromRgbTransform(new[,] {
				{ 0.547494f, 0.607765f, -0.155259f },
				{ 0.181692f, 0.781742f, 0.036566f },
				{ -0.010410f, 0.027275f, 0.983136f }
			}),
			ColorVisionDeficiency.Tritanopia => FromRgbTransform(new[,] {
				{ 1.255528f, -0.076749f, -0.178779f },
				{ -0.078411f, 0.930809f, 0.147602f },
				{ 0.004733f, 0.691367f, 0.303900f }
			}),
			ColorVisionDeficiency.Tritanomaly => FromRgbTransform(new[,] {
				{ 1.104996f, -0.046633f, -0.058363f },
				{ -0.032137f, 0.971635f, 0.060503f },
				{ 0.001336f, 0.317922f, 0.680742f }
			}),
			ColorVisionDeficiency.Achromatopsia => CreateMonochrome(),
			ColorVisionDeficiency.Achromatomaly => CreateSaturation(0.4f),
			_ => throw new ArgumentOutOfRangeException(nameof(deficiency), deficiency, "Unknown colour vision deficiency.")
		};
	}

	/// <summary>Returns <paramref name="steps"/> matrices moving linearly from just after <paramref name="from"/> up to and including <paramref name="to"/>.</summary>
	public static ColorMatrix[] Interpolate(ColorMatrix from, ColorMatrix to, int steps) {
		Guard.ArgumentNotNull(from, nameof(from));
		Guard.ArgumentNotNull(to, nameof(to));
		Guard.ArgumentGT(steps, 0, nameof(steps));
		var frames = new ColorMatrix[steps];
		for (var step = 0; step < steps; step++) {
			var frame = new ColorMatrix();
			var progress = (step + 1) / (float)steps;
			for (var row = 0; row < Size; row++)
				for (var column = 0; column < Size; column++)
					frame[row, column] = from[row, column] + (to[row, column] - from[row, column]) * progress;
			frames[step] = frame;
		}
		return frames;
	}

	/// <summary>Whether two matrices agree within <paramref name="tolerance"/> in every element.</summary>
	public static bool AreEqual(ColorMatrix first, ColorMatrix second, float tolerance = 0.00001f) {
		if (ReferenceEquals(first, second))
			return true;
		if (first == null || second == null)
			return false;
		for (var row = 0; row < Size; row++)
			for (var column = 0; column < Size; column++)
				if (Math.Abs(first[row, column] - second[row, column]) > tolerance)
					return false;
		return true;
	}

	private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
