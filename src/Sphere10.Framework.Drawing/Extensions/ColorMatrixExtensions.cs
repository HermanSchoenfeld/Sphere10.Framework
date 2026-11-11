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
using System.Runtime.Versioning;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework;

[SupportedOSPlatform("windows6.1")]
public static class ColorMatrixExtensions {
	public static ColorMatrix Clone(this ColorMatrix matrix) {
		Guard.ArgumentNotNull(matrix, nameof(matrix));
		var clone = new ColorMatrix();
		for (var row = 0; row < ColorMatrices.Size; row++)
			for (var column = 0; column < ColorMatrices.Size; column++)
				clone[row, column] = matrix[row, column];
		return clone;
	}

	/// <summary>Returns the matrix as a 5x5 array indexed [row, column].</summary>
	public static float[,] ToArray(this ColorMatrix matrix) {
		Guard.ArgumentNotNull(matrix, nameof(matrix));
		var values = new float[ColorMatrices.Size, ColorMatrices.Size];
		for (var row = 0; row < ColorMatrices.Size; row++)
			for (var column = 0; column < ColorMatrices.Size; column++)
				values[row, column] = matrix[row, column];
		return values;
	}

	/// <summary>Returns the matrix as the jagged array layout used by GDI+ constructors.</summary>
	public static float[][] ToJaggedArray(this ColorMatrix matrix) {
		Guard.ArgumentNotNull(matrix, nameof(matrix));
		var values = new float[ColorMatrices.Size][];
		for (var row = 0; row < ColorMatrices.Size; row++) {
			values[row] = new float[ColorMatrices.Size];
			for (var column = 0; column < ColorMatrices.Size; column++)
				values[row][column] = matrix[row, column];
		}
		return values;
	}

	/// <summary>Composes a transformation that applies this matrix first and <paramref name="next"/> afterwards.</summary>
	public static ColorMatrix Then(this ColorMatrix matrix, ColorMatrix next) => ColorMatrices.Multiply(matrix, next);

	/// <summary>Applies contrast around mid-gray and then a brightness offset after this matrix's own transformation.</summary>
	public static ColorMatrix WithBrightnessContrast(this ColorMatrix matrix, float brightness, float contrast)
		=> matrix.Then(ColorMatrices.CreateContrast(contrast)).Then(ColorMatrices.CreateBrightness(brightness));

	/// <summary>
	/// Transforms one colour as GDI+ and the Magnification API do: the colour is the row vector (R, G, B, A, 1) scaled to 0..1,
	/// multiplied by the matrix, and each channel of the result is clamped back to 0..255.
	/// </summary>
	public static Color Transform(this ColorMatrix matrix, Color color) {
		Guard.ArgumentNotNull(matrix, nameof(matrix));
		Span<float> input = stackalloc[] { color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f, 1f };
		Span<float> output = stackalloc float[4];
		for (var column = 0; column < 4; column++) {
			var sum = 0f;
			for (var row = 0; row < ColorMatrices.Size; row++)
				sum += input[row] * matrix[row, column];
			output[column] = sum;
		}
		return Color.FromArgb(ToChannel(output[3]), ToChannel(output[0]), ToChannel(output[1]), ToChannel(output[2]));
	}

	/// <summary>Draws the image through the matrix into a new 32-bit ARGB bitmap of the same pixel size; the caller owns the result.</summary>
	public static Bitmap ApplyTo(this ColorMatrix matrix, Image image) {
		Guard.ArgumentNotNull(matrix, nameof(matrix));
		Guard.ArgumentNotNull(image, nameof(image));
		var result = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
		try {
			using var graphics = Graphics.FromImage(result);
			using var attributes = new ImageAttributes();
			attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
			graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
			return result;
		} catch {
			result.Dispose();
			throw;
		}
	}

	public static bool IsIdentity(this ColorMatrix matrix, float tolerance = 0.00001f) => ColorMatrices.AreEqual(matrix, ColorMatrices.Identity, tolerance);

	public static bool ApproximatelyEquals(this ColorMatrix matrix, ColorMatrix other, float tolerance = 0.00001f) => ColorMatrices.AreEqual(matrix, other, tolerance);

	private static int ToChannel(float value) => (int)(Math.Clamp(value, 0f, 1f) * 255f + 0.5f);
}
