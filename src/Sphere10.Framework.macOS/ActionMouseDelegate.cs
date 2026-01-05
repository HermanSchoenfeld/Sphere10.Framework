// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using MonoMac.AppKit;

namespace Sphere10.Framework {
	public sealed class ActionMouseDelegate : MouseDelegate {
	
		public ActionMouseDelegate() {
		}

		public Action<NSEvent> MouseMovedAction { get; set; }
		public Action<NSEvent> MouseEnteredAction { get; set; }
		public Action<NSEvent> CursorUpdateAction { get; set; }
		public Action<NSEvent> MouseExitedAction { get; set; }

		public sealed override void MouseMoved(NSEvent theEvent) {
			if (MouseMovedAction != null)
				MouseMovedAction(theEvent);
		}


		public sealed override void MouseEntered (NSEvent theEvent)	{
			if (MouseEnteredAction != null) 
				MouseEnteredAction(theEvent);
		}
		
		public sealed override void CursorUpdate (NSEvent theEvent)	{
			if (CursorUpdateAction != null)
				CursorUpdateAction(theEvent);
		}
		
		public sealed override void MouseExited (NSEvent theEvent) {
			if (MouseExitedAction != null)
				MouseExitedAction(theEvent);
		}
	}
}


