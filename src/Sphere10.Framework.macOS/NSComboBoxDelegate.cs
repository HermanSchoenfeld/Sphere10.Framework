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

namespace Sphere10.Framework {
	public class NSComboBoxDelegateEx : NSComboBoxDelegate {

		public event EventHandler SelectionChanged;
		public event EventHandler SelectionChanging;
		public event EventHandler WillDissmiss;
		public event EventHandler WillPopUp;

		public sealed override void comboBoxSelectionDidChange(NSNotification notification) {
			if (SelectionChanged != null) {
				SelectionChanged(notification.Object, EventArgs.Empty);
			}
		}
		
		public sealed override void comboBoxSelectionIsChanging(NSNotification notification) {
			if (SelectionChanging != null) {
				SelectionChanging(notification.Object, EventArgs.Empty);
			}
		}
		
		public sealed override void comboBoxWillDismiss(NSNotification notification) {
			if (WillDissmiss != null) {
				WillDissmiss(notification.Object, EventArgs.Empty);
			}
		}
		
		public sealed override void comboBoxWillPopUp(NSNotification notification)	{
			if (WillPopUp != null) {
				WillPopUp(notification.Object, EventArgs.Empty);
			}
		}

	
	}
}


