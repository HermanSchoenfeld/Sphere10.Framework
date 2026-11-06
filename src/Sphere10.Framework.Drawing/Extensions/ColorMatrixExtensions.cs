// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

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
	public static ColorMatrix Then(this ColorMatrix matrix, ColorMatrix next) {
		return ColorMatrices.Multiply(matrix, next);
	}

	/// <summary>Applies contrast around mid-gray and then a brightness offset after this matrix's own transformation.</summary>
	public static ColorMatrix WithBrightnessContrast(this ColorMatrix matrix, float brightness, float contrast) {
		return matrix.Then(ColorMatrices.CreateContrast(contrast)).Then(ColorMatrices.CreateBrightness(brightness));
	}

	public static bool IsIdentity(this ColorMatrix matrix, float tolerance = 0.00001f) {
		return ColorMatrices.AreEqual(matrix, ColorMatrices.Identity, tolerance);
	}

	public static bool ApproximatelyEquals(this ColorMatrix matrix, ColorMatrix other, float tolerance = 0.00001f) {
		return ColorMatrices.AreEqual(matrix, other, tolerance);
	}
}
