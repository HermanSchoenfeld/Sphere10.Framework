// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using UIKit;

namespace Sphere10.Framework.iOS {
	public class UILongPressGestureRecognizerEx : UILongPressGestureRecognizer {
		private Action<UILongPressGestureRecognizerEx> _handler;
		private readonly Token _token;


		public UILongPressGestureRecognizerEx(Action<UILongPressGestureRecognizerEx> handler){
			if (handler == null)
				throw new ArgumentNullException("handler");

			this._handler = handler;
			_token = (this.AddTarget(() => _handler(this)));
		}

		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);
			if (disposing) {
				this.RemoveTarget(_token);
				_token.Dispose();
				_handler = null;
			}
		}
	}
}


