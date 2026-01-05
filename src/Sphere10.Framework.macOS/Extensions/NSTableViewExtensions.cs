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
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Drawing;


namespace Sphere10.Framework {
	public static class NSTableViewExtensions
	{
		public static void RemoveAllColumns(this NSTableView tableView) {
			while (tableView.ColumnCount > 0) {
				tableView.RemoveColumn(tableView.TableColumns()[0]);
			}
		}


		public static void SizeColumsEqually(this NSTableView tableView) {
			if (tableView.ColumnCount > 0) {
				var frame = tableView.GetFrameForSizing();
				var colWidth = (int)Math.Round( frame.Width / (float)tableView.ColumnCount, 0);
				tableView.TableColumns().ForEach( c => c.MaxWidth = c.MinWidth = c.Width = colWidth);
				//tableView.TableColumns().ForEach( c => Console.WriteLine(c.Width));

			}
		}

		public static void SizeRowsEqually(this NSTableView tableView) {
			if (tableView.RowCount > 0) {
				var frame = tableView.GetFrameForSizing();
				var rowHeight = (int)Math.Round( frame.Height / (float)tableView.RowCount, 0);
				tableView.RowHeight = rowHeight;
			}
		}


		public static RectangleF GetFrameForSizing(this NSTableView tableView) {
			return tableView.Frame;
		}


		// http://stackoverflow.com/questions/4674163/nstablecolumn-size-to-fit-contents

		public static void SizeColumnsByContent(this NSTableView tableView, int minWidth = 10) {
			Contract.Requires(tableView != null);
			Contract.Requires(tableView.DataSource != null);
			var columns = tableView.TableColumns();
			var widths = tableView.DetermineColumnContentFitSizes();
			for (int col=0; col < tableView.ColumnCount; col++) {
				columns[col].Width = widths[col];
				//columns[col].ResizingMask = NSTableColumnResizing.UserResizingMask;
			}
		}


		private static int[] DetermineColumnContentFitSizes(this NSTableView tableView, bool includeHeader = true, int minWidth = 10) {
			Contract.Requires(tableView != null);
			Contract.Requires(tableView.DataSource != null);
			var columns = tableView.TableColumns();
			var widths = new int[columns.Count()];

			// Start with the header fit widths
			for (int i = 0; i < columns.Count(); i++) 
				widths[i] = includeHeader ? (int)columns[i].HeaderCell.DetermineFitWidth(columns[i].HeaderCell.StringValue.ToNSString()) : minWidth;

			// Record the max encountered fit-width for each cell in column
			for (int row=0; row< tableView.RowCount; row++) {
				for (int col=0; col < tableView.ColumnCount; col++) {
					// Determine what the fit width is for this cell
					var column = columns[col];
					var objectValue = tableView.DataSource.GetObjectValue(tableView, column, row);
					var width = column.DataCell.DetermineFitWidth(objectValue, minWidth);

					// Record the max width encountered for current coolumn
					widths[col] = Math.Max(widths[col], width);
				}
			}
			return widths;
		}

	}

}





