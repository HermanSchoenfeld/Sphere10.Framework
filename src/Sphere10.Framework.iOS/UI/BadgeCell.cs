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
	public class BadgeCell : UITableViewCell {

		private readonly BadgeView _badgeView;

		public BadgeCell(string reuseIdentifier) : this(UITableViewCellStyle.Subtitle, reuseIdentifier) {
		}

		public BadgeCell(UITableViewCellStyle cellStyle, string reuseIdentifier, bool showBadgeView = true) : base(cellStyle, reuseIdentifier) {
			_badgeView = new BadgeView(UIColor.Red, UIColor.White, UIColor.Clear, borderWidth:0.0f);
			ShowBadgeView = true;
		}

		public BadgeView BadgeView { get { return AccessoryView as BadgeView; } }

		public bool ShowBadgeView { 
			get {
				return AccessoryView == _badgeView;
			}
			set {
				if (value) {
					if (AccessoryView != _badgeView) {
						AccessoryView = _badgeView;
					}
				} else {
					if (AccessoryView == _badgeView)
						AccessoryView = null;
				}
			}
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				ContentView.DisposeEx();
				if (_badgeView != null)
					_badgeView.DisposeEx();
			}
			base.Dispose(disposing);
		}

	}
}


