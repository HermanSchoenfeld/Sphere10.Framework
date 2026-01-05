// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using UIKit;
using CoreGraphics;
using CoreGraphics;
using Sphere10.Framework;

namespace Sphere10.Framework.iOS
{
	public class UIScrollViewEx : UIScrollView
	{
		public UIScrollViewEx (CGRect frame) : this(frame, UIColor.White, UIColor.LightGray) {
		}

		public UIScrollViewEx (CGRect frame, UIColor startColor, UIColor endColor) : base(frame) {
			GradientStartColor = startColor;
			GradientEndColor = endColor;
		}

		public UIColor GradientStartColor { get; set; }

		public UIColor GradientEndColor { get; set; }

		public override void Draw (CGRect rect)	{
			var currentContext = UIGraphics.GetCurrentContext();
			var locations = new nfloat[] { 0.0f, 1.0f};
			var colors = new[] { GradientStartColor.CGColor, GradientEndColor.CGColor };
			var rgbColorspace = CGColorSpace.CreateDeviceRGB();
			var glossGradient = new CGGradient(rgbColorspace, colors, locations);
			currentContext.DrawLinearGradient(glossGradient, CGPoint.Empty, Bounds.BottomRight(), CGGradientDrawingOptions.DrawsAfterEndLocation);
			glossGradient.Dispose();
			rgbColorspace.Dispose ();
			base.BackgroundColor = UIColor.Clear;
			base.Draw((CGRect)rect);
		}

	}
}


