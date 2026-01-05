// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Drawing;
using MonoMac.ObjCRuntime;


namespace Sphere10.Framework {
	public static class NSViewExtensions {

		private static IntPtr selConvertRectToBacking_ = Selector.GetHandle("convertRectToBacking:");

		[Export("convertRectToBacking:")]
		public static RectangleF ConvertRectToBacking(this NSView view, RectangleF aRect)
		{
			RectangleF result;
			if (view.GetPrivateFieldValue<bool>("IsDirectBinding")) {
				Messaging.RectangleF_objc_msgSend_stret_RectangleF(out result, view.Handle, NSViewExtensions.selConvertRectToBacking_, aRect);
			}
			else
			{
				Messaging.RectangleF_objc_msgSendSuper_stret_RectangleF(out result, view.SuperHandle, NSViewExtensions.selConvertRectToBacking_, aRect);
			}
			return result;
		}
	}
}


