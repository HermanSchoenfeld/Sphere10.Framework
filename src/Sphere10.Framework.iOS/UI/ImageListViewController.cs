// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Foundation;
using UIKit;
using System.Collections;
using System.Collections.Generic;
using CoreGraphics;

namespace Sphere10.Framework.iOS
{


	public class ImageListViewController : UIViewController
	{

		public override void ViewDidLoad ()
		{
			View.AddSubview(new ImageListView{
				Frame = (CGRect)View.Frame
			});
		}		
		
        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
            return false;
        }
		
	}
	
	public class ImageListView : UIScrollView {
		
	}
}

