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

namespace Sphere10.Framework {
	public static class NSTableViewColumnExtensions
	{
		public static int GetIndex(this NSTableColumn tableColumn) {
			int index = -1;
			if (tableColumn.TableView != null) {
				var columns = tableColumn.TableView.TableColumns();
				for (int i = 0; i< columns.Length; i++) {
					if (tableColumn == columns[i]) {
						index = i;
						break;
					}
				}
			}
			return index;
		}


	}
}


