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

namespace Sphere10.Framework.iOS {

	/// <summary>
	/// Inherit from this to workaround the bug seen here (http://stackoverflow.com/questions/14081410/extended-uibutton-border-is-not-initially-drawn)
	/// </summary>
	public abstract class UIButtonEx : UIButton {

		public UIButtonEx(UIButtonType buttonType) : base(buttonType) { }

		public override CGRect Frame {
			get {
				return (CGRect)base.Frame;
			}
			set {
				var temp = TranslatesAutoresizingMaskIntoConstraints;
				TranslatesAutoresizingMaskIntoConstraints = false;
				var constraints = new [] {
(NSLayoutConstraint)(					NSLayoutConstraint.Create(this, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1.0f, value.Width)),
(NSLayoutConstraint)(					NSLayoutConstraint.Create(this, NSLayoutAttribute.Height, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 1.0f, value.Height)
)				};
				AddConstraints(constraints);
				SizeToFit();
				RemoveConstraints(constraints);
				base.Frame = value;
				TranslatesAutoresizingMaskIntoConstraints = temp;
			}
		}
	}
}


