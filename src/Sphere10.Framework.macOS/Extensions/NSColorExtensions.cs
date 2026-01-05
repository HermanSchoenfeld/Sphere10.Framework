// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Drawing;
using MonoMac.CoreGraphics;

namespace Sphere10.Framework {
	public static class NSColorExtensions
	{
		public static Color ToColor(this NSColor nsColor) {
			return Color.FromArgb(
				(int)(nsColor.AlphaComponent * 255.0f),
			    (int)(nsColor.RedComponent * 255.0f), 
			    (int)(nsColor.GreenComponent * 255.0f),
			    (int)(nsColor.BlueComponent * 255.0f)
			);
		}

		public static CGColor ToCGColor(this NSColor nsColor) {
			float r,g,b,a;
			nsColor.GetRgba(out r, out g, out b, out a);
			return new CGColor(r, g, b, a);
		}

		
		public static NSColor ToNSColor(this Color color) {
			return NSColor.FromCalibratedRgba(
				(float)color.R / 255.0f,
				(float)color.G / 255.0f,
				(float)color.B / 255.0f,
				(float)color.A / 255.0f
			);
		}
	}
}


