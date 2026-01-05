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
using System.Data;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Drawing;
using Sphere10.Framework;

namespace Sphere10.Framework.Data {
	public static class NSTableViewExtensions
	{

		public static void SetDataSourceEx(this NSTableView tableView, DataTable dataTable) {
			tableView.RemoveAllColumns();
			//dataTable.Columns[0].ColumnMapping = MappingType.Hidden;
			dataTable.Columns.Cast<DataColumn>().ForEach(c => {
				var col = new NSTableColumn();
				col.HeaderCell.Title = c.ColumnName;
				col.HeaderToolTip = c.ColumnName;
				col.Hidden = c.ColumnMapping == MappingType.Hidden;
				tableView.AddColumn(col);
			});
			tableView.SizeLastColumnToFit();
			tableView.DataSource = new ADOTableViewDataSource(dataTable);
			tableView.ReloadData();
		}
	

	}

}





