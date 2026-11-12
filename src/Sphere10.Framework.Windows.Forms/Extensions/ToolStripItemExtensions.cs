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

public static class ToolStripItemExtensions {
	/// <summary>Sets an image the caller owns and disposes the image it replaces, which must also have been owned by the caller.</summary>
	public static void ReplaceImage(this ToolStripItem item, Image image) {
		Guard.ArgumentNotNull(item, nameof(item));
		var previous = item.Image;
		item.Image = image;
		if (!ReferenceEquals(previous, image))
			previous?.Dispose();
	}

	/// <summary>
	/// Renders the icon at a pixel size, normally a logical size converted with the hosting form's
	/// <see cref="Control.LogicalToDeviceUnits(int)"/>, and sets it through <see cref="ReplaceImage(ToolStripItem, Image)"/>.
	/// The icon's closest frame is scaled when it has none of that size. WinForms keeps tool strip images at the size they were
	/// created for, so call it again after every DPI change.
	/// </summary>
	public static void ReplaceImage(this ToolStripItem item, Icon icon, int pixelSize) {
		Guard.ArgumentNotNull(item, nameof(item));
		Guard.ArgumentNotNull(icon, nameof(icon));
		Guard.ArgumentGT(pixelSize, 0, nameof(pixelSize));
		item.ReplaceImage(icon.ToBitmap(pixelSize, pixelSize));
	}
}
