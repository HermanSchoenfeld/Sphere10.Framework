// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using MonoMac.AppKit;
using MonoMac.Foundation;
using System.Drawing;

namespace Sphere10.Framework {

	public class MouseDelegate : NSObject {

		public NSView View { get; set; }
		
		[Export("mouseMoved:")]
		public virtual void MouseMoved (NSEvent theEvent) {
		}

		[Export("mouseEntered:")]
		public virtual void MouseEntered (NSEvent theEvent)	{
		}
		
		[Export("cursorUpdate::")]
		public virtual void CursorUpdate (NSEvent theEvent)	{
		}
		
		[Export("mouseExited:")]
		public virtual void MouseExited (NSEvent theEvent) {
		}
	}
}


