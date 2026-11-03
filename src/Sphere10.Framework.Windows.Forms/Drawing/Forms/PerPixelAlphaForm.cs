// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

namespace Sphere10.Framework.Windows.Forms;

/// <para>Your PerPixel form should inherit this class</para>
/// <author><name>Rui Godinho Lopes</name><email>rui@ruilopes.com</email></author>
public class PerPixelAlphaForm : Form {
	public PerPixelAlphaForm() {
		// This form should not have a border or else Windows will clip it.
		FormBorderStyle = FormBorderStyle.None;
	}


	/// <para>Changes the current bitmap.</para>
	public void SetBitmap(Bitmap bitmap) {
		SetBitmap(bitmap, 255);
	}


	/// <para>Changes the current bitmap with a custom opacity level.  Here is where all happens!</para>
	public void SetBitmap(Bitmap bitmap, byte opacity) {
		if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
			throw new ApplicationException("The bitmap must be 32ppp with alpha-channel.");

		Tools.Windows.Win32.UpdateLayeredWindow(Handle, bitmap, opacity, new Point(Left, Top), bitmap.Size);
	}


	protected override CreateParams CreateParams {
		get {
			var cp = base.CreateParams;
			if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
				return cp;

				cp.ExStyle |= 0x00080000; // This form has to have the WS_EX_LAYERED extended style
			return cp;
		}
	}
}

