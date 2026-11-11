// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Decorators;

public class DecoratorSelection : DecoratorBase {
	private Selection.SelectionBase _selection;

	public DecoratorSelection(Selection.SelectionBase selection) => _selection = selection;

	public override bool IntersectWith(CellRange range) => _selection.IntersectsWith(range);

	public override void Draw(RangePaintEventArgs e) {
		var region = _selection.GetSelectionRegion();

		if (region.IsEmpty())
			return;

		// get visible range for scrollable area
		var visibleScrollabeRange = _selection.Grid.RangeAtAreaExpanded(CellPositionType.Scrollable);

		var brush = e.GraphicsCache.BrushCache.GetBrush(_selection.BackColor);

		var focusContext = new CellContext(e.Grid, _selection.ActivePosition);
		// get focus rectangle
		// clipped to visible range
		var focusClippedRange = visibleScrollabeRange.Intersect(new CellRange(_selection.ActivePosition, _selection.ActivePosition));
		var focusRect = e.Grid.PositionToRectangle(focusClippedRange.Start);

		//Draw each selection range
		foreach (var rangeToLoop in region) {
			// intersect given range with visible range
			// this way we ensure we don't loop through thousands
			// of rows to calculate rectToDraw
			var rng = visibleScrollabeRange.Intersect(rangeToLoop);

			var rectToDraw = e.Grid.RangeToRectangle(rng);
			if (rectToDraw == System.Drawing.Rectangle.Empty)
				continue;

			var regionToDraw = new System.Drawing.Region(rectToDraw);

			if (rectToDraw.IntersectsWith(focusRect))
				regionToDraw.Exclude(focusRect);

			e.GraphicsCache.Graphics.FillRegion(brush, regionToDraw);

			//Draw the border only if there isn't a editing cell
			// and is the range that contains the focus or there is a single range
			if (rng.Contains(_selection.ActivePosition) || region.Count == 1) {
				if (focusContext.IsEditing() == false)
					_selection.Border.Draw(e.GraphicsCache, rectToDraw);
			}
		}

		//Draw Focus
		var brushFocus = e.GraphicsCache.BrushCache.GetBrush(_selection.FocusBackColor);
		e.GraphicsCache.Graphics.FillRectangle(brushFocus, focusRect);
	}
}
