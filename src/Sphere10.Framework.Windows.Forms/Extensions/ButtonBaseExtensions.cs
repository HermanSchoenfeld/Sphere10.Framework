// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public static class ButtonBaseExtensions {
	/// <summary>
	/// Replaces the button's 96-DPI image with one scaled to the button's DPI and disposes the original, which the button must own, as an image a
	/// designer loaded from resources is. Buttons do not scale their images, so on a scaled display they shrink against the button and its text.
	/// Call it once, after the designer sets the image.
	/// </summary>
	public static void ScaleImageToDeviceDpi(this ButtonBase button) {
		Guard.ArgumentNotNull(button, nameof(button));
		if (button.DeviceDpi == 96 || button.Image is not Bitmap original)
			return;
		// The scaling disposes the bitmap it is given, so it is given a copy: the button still shows the original until the scaled image replaces it.
		var scaled = new Bitmap(original);
		button.ScaleBitmapLogicalToDevice(ref scaled);
		button.Image = scaled;
		original.Dispose();
	}
}
