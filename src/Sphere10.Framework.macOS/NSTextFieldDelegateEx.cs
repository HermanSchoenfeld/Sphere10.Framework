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
	public class NSTextFieldDelegateEx : NSTextFieldDelegate {

	

		public override void Changed(MonoMac.Foundation.NSNotification notification) {
			var x = 1;

		}


		public override void DidChangeValue(string forKey) {
			var x = 1;
		}

		public override void DidChange(MonoMac.Foundation.NSKeyValueChange changeKind, MonoMac.Foundation.NSIndexSet indexes, MonoMac.Foundation.NSString forKey) {
			var x = 1;
		}

		public override void DidChange(MonoMac.Foundation.NSString forKey, MonoMac.Foundation.NSKeyValueSetMutationKind mutationKind, MonoMac.Foundation.NSSet objects) {
			var x = 1;
		}

		public override void WillChange(MonoMac.Foundation.NSKeyValueChange changeKind, MonoMac.Foundation.NSIndexSet indexes, MonoMac.Foundation.NSString forKey) {
			var x = 1;
		}

		public override void WillChange(MonoMac.Foundation.NSString forKey, MonoMac.Foundation.NSKeyValueSetMutationKind mutationKind, MonoMac.Foundation.NSSet objects) {
			var x = 1;
		}

		public override void WillChangeValue(string forKey) {
			var x = 1;
		}

		public override void EditingBegan(MonoMac.Foundation.NSNotification notification) {
			var x = 1;
		}

		public override void EditingEnded(MonoMac.Foundation.NSNotification notification) {
			var x = 1;
		}

	
	}
}


