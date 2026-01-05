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

namespace Sphere10.Framework {


	public static class NSAlignmentExtensions
	{
		public static NSTextAlignment ToNSTextAlignment(this ContentAlignment alignment) {
			NSTextAlignment retval = NSTextAlignment.Center;
			switch(alignment) {
				case ContentAlignment.BottomCenter:
					retval = NSTextAlignment.Center;
					break;
				case ContentAlignment.BottomLeft:
					retval = NSTextAlignment.Left;
					break;
				case ContentAlignment.BottomRight:
					retval = NSTextAlignment.Right;
					break;
				case ContentAlignment.MiddleCenter:
					retval = NSTextAlignment.Center;
					break;
				case ContentAlignment.MiddleLeft:
					retval = NSTextAlignment.Left;
					break;
				case ContentAlignment.MiddleRight:
					retval = NSTextAlignment.Right;
					break;
				case ContentAlignment.TopCenter:
					retval = NSTextAlignment.Center;
					break;
				case ContentAlignment.TopLeft:
					retval = NSTextAlignment.Left;
					break;
				case ContentAlignment.TopRight:
					retval = NSTextAlignment.Right;
					break;
				default:
					retval = NSTextAlignment.Center;
					break;
			}
			return retval;
		}


	}
}


