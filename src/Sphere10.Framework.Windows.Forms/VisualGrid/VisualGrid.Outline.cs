// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

// Draws a bound source's outline in one column of this grid: each row indented by its depth, with an expander beside a row that has rows
// beneath it. The source serves the shown rows in tree order and keeps their expansion; this grid only draws it and asks for changes.
public partial class VisualGrid {
	#region Fields

	private readonly ActionCache<OutlineViewKey, VisualGridDetailCellView> _outlineViews = new(key => new(key.Prototype, key.Expandable, key.Expanded, key.Gutter, key.Indent),
		_ => 1, reapStrategy: CacheReapPolicy.LongestIdle, maxCapacity: 256);

	#endregion

	#region Properties

	/// <summary>
	/// The bound source's outline, usually the source itself. While it serves an outline, the <see cref="OutlineColumnKey"/> column indents each row
	/// by its depth and shows an expander beside a row with rows beneath it; a click on the expander, or Right and Left on the selected row, shows or
	/// hides them. One grid shows a whole tree this way, where nested child tables need a control for every expanded row.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IHierarchyOutline Outline { get; set; }

	/// <summary>The column whose cells are indented by the outline, usually the first; the first visible column when unset.</summary>
	[DefaultValue(null)]
	public string OutlineColumnKey { get; set; }

	private bool IsOutlined => Outline?.IsOutline == true;

	private int OutlineGutter => Math.Max(12, (int)Math.Round(18 * DeviceDpi / 96D));

	private int OutlineColumn => Math.Max(0, Array.FindIndex(_columnsBindings, column => column.PreferenceKey == OutlineColumnKey));

	#endregion

	#region Methods

	/// <summary>Shows or hides the rows beneath <paramref name="row"/> and reads the outline again.</summary>
	public Task SetOutlineExpandedAsync(object row, bool expanded) {
		Guard.Ensure(Outline != null, "The grid has no outline.");
		Outline.SetOutlineExpanded(row, expanded);
		return PresentOutlineAsync();
	}

	/// <summary>Shows every row of the outline.</summary>
	public Task ExpandAllAsync() {
		Guard.Ensure(Outline != null, "The grid has no outline.");
		Outline.ExpandAll();
		return PresentOutlineAsync();
	}

	/// <summary>Shows only the outline's top-level rows.</summary>
	public Task CollapseAllAsync() {
		Guard.Ensure(Outline != null, "The grid has no outline.");
		Outline.CollapseAll();
		return PresentOutlineAsync();
	}

	// A changed expansion changes what the source serves, as a published change does: it is read at once, with the rows staying usable, rather
	// than through the user refresh's 250 ms throttle and disabled grid (each expander click took that quarter second, 22 November 2026). A read
	// already running may predate the change, so it is awaited and another one requested.
	private async Task PresentOutlineAsync() {
		if (_refreshTask is { IsCompleted: false } running)
			await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => running);
		await RequestGridRefresh(true);
	}

	private void DecorateOutlineRow(int row, object entity) {
		if (!IsOutlined || _grid.ColumnsCount == 0 || _grid[row, OutlineColumn] is not SourceGrid.Cells.Cell cell)
			return;
		var prototype = cell.View is VisualGridDetailCellView previous ? previous.Prototype : cell.View as SourceGrid.Cells.Views.Cell;
		if (prototype == null)
			return;
		var expandable = Outline.HasOutlineChildren(entity);
		var gutter = OutlineGutter;
		cell.View = _outlineViews[new OutlineViewKey(prototype, expandable, expandable && Outline.IsOutlineExpanded(entity), gutter, Outline.GetOutlineDepth(entity) * gutter)];
	}

	// The expander sits at the start of the indented cell, after the indentation of the row's depth.
	private bool TryHitOutline(Point location, out object entity) {
		entity = null;
		if (!IsOutlined)
			return false;
		var position = _grid.PositionAtPoint(location);
		if (position.IsEmpty() || position.Column != OutlineColumn || !_rowToEntityMap.TryGetValue(position.Row, out var row) || !Outline.HasOutlineChildren(row))
			return false;
		var gutter = OutlineGutter;
		var left = _grid.PositionToRectangle(position).Left + Outline.GetOutlineDepth(row) * gutter;
		if (location.X < left || location.X >= left + gutter)
			return false;
		entity = row;
		return true;
	}

	#endregion

	#region Inner Types

	/// <summary>Keys a cached outline cell view by its original formatting, expansion state and indentation.</summary>
	private readonly record struct OutlineViewKey(SourceGrid.Cells.Views.Cell Prototype, bool Expandable, bool Expanded, int Gutter, int Indent);

	#endregion
}
