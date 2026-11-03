// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

public class RowInfoCollectoinHiddenRowCoordinator : StandardHiddenRowCoordinator {
	public RowInfoCollectoinHiddenRowCoordinator(RowInfoCollection rows)
		: base(rows) {
		// when rows are removed, check if some of them were hidden
		// if yes, inform hidden row coordinator that they were removed
		rows.RowsRemoving += delegate (object sender, IndexRangeEventArgs args) {
			for (var i = 0; i < args.Count; i++) {
				var index = i + args.StartIndex;
				if (rows.IsRowVisible(index) == false)
					base.TotalHiddenRows -= 1;
			}

			var range = new CellRange(args.StartIndex, 0, args.StartIndex + args.Count, 1);
			base.HiddenRowRanges.RemoveRange(range);
		};
	}
}
