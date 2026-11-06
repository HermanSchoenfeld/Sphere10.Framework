// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing.Imaging;

namespace Sphere10.Framework.Windows;

/// <summary>Converts between GDI+ color matrices and the Magnification API's native color effect layout.</summary>
internal static class MagnificationColorEffects {
	public static WinAPI.MAGNIFICATION.MAGCOLOREFFECT ToNative(ColorMatrix matrix) {
		Guard.ArgumentNotNull(matrix, nameof(matrix));
		var effect = new WinAPI.MAGNIFICATION.MAGCOLOREFFECT { Transform = new float[25] };
		for (var row = 0; row < 5; row++)
			for (var column = 0; column < 5; column++)
				effect.Transform[row * 5 + column] = matrix[row, column];
		return effect;
	}

	public static ColorMatrix ToColorMatrix(in WinAPI.MAGNIFICATION.MAGCOLOREFFECT effect) {
		var values = effect.ToMatrix();
		var matrix = new ColorMatrix();
		for (var row = 0; row < 5; row++)
			for (var column = 0; column < 5; column++)
				matrix[row, column] = values[row, column];
		return matrix;
	}
}
