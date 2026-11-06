// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace Sphere10.Framework.Drawing;

/// <summary>Builds and combines 5x5 GDI+ color matrices using the row-vector convention shared by GDI+ and the Magnification API.</summary>
[SupportedOSPlatform("windows6.1")]
public static class ColorMatrices {
	public const int Size = 5;

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
}
