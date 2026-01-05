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

namespace Sphere10.Framework.iOS
{
	public class UITextViewEx : UITextView	{
		public UITextViewEx (UIColor backgroundColor, float cornerRadius = 8.0f) {
			Initialize(backgroundColor, cornerRadius);
		}

		private void Initialize(UIColor backgroundColor, float cornerRadius) {
			this.BackgroundColor =backgroundColor;
			this.Layer.BorderWidth = 1.0f;
			this.Layer.BorderColor = UIColor.Gray.CGColor;
			this.Layer.CornerRadius = cornerRadius;
			this.Layer.ShadowRadius = cornerRadius;
			this.Layer.MasksToBounds = true;
			this.UserInteractionEnabled = true;
		}
	}
}


