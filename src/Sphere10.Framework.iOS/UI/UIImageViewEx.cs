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
using Foundation;

namespace Sphere10.Framework.iOS {
	public class UIImageViewEx : UIImageView	{
		public event EventHandler TouchBegan;
		public event EventHandler TouchEnded;

		public UIImageViewEx (CGRect frame) : base(frame) {
			Initialize();
		}

		public UIImageViewEx (UIImage image) : base(image) {
			Initialize();
		}

		private void Initialize() {
			UserInteractionEnabled = true;
		}

		public override void TouchesBegan (NSSet touches, UIEvent evt)	{
			if (TouchBegan != null)
				TouchBegan(this, EventArgs.Empty);
		}

		public override void TouchesEnded (NSSet touches, UIEvent evt)	{
			if (TouchEnded != null)
				TouchEnded(this, EventArgs.Empty);
		}


	}
}


