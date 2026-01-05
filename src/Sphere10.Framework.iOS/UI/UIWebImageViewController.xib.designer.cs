// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.iOS {
	
	
	// Base type probably should be MonoTouch.UIKit.UIViewController or subclass
	[Foundation.Register("UIWebImageViewController")]
	public partial class UIWebImageViewController {
		
		private UIKit.UIView __mt_view;
		
		private UIWebImageView __mt_uiWebImageView;
		
		#pragma warning disable 0169
		[Foundation.Export("onNextImage:")]
		partial void onNextImage (UIKit.UIButton sender);

		[Foundation.Connect("view")]
		private UIKit.UIView view {
			get {
				this.__mt_view = ((UIKit.UIView)(this.GetNativeField("view")));
				return this.__mt_view;
			}
			set {
				this.__mt_view = value;
				this.SetNativeField("view", value);
			}
		}
		
		[Foundation.Connect("uiWebImageView")]
		private UIWebImageView uiWebImageView {
			get {
				this.__mt_uiWebImageView = ((UIWebImageView)(this.GetNativeField("uiWebImageView")));
				return this.__mt_uiWebImageView;
			}
			set {
				this.__mt_uiWebImageView = value;
				this.SetNativeField("uiWebImageView", value);
			}
		}
	}
	
	// Base type probably should be MonoTouch.UIKit.UIImageView or subclass
	[Foundation.Register("UIWebImageView")]
	public partial class UIWebImageView {
	}
}

