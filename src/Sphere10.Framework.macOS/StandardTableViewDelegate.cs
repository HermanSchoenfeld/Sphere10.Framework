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
	public class StandardTableViewDelegate : NSTableViewDelegate {

		public StandardTableViewDelegate() {

		}

		public override bool ShouldSelectRow(NSTableView tableView, int row) {
			return true;
		}

		public override bool ShouldSelectTableColumn(NSTableView tableView, NSTableColumn tableColumn) {
			return false;
		}

		public override bool ShouldEditTableColumn(NSTableView tableView, NSTableColumn tableColumn, int row) {
			return false;
		}


			
	}

}


