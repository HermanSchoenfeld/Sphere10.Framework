// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms;

internal class CustomSortHeaderCellController : SourceGrid.Cells.Controllers.ControllerBase {
	private readonly VisualGrid _visualGrid;

	public CustomSortHeaderCellController(VisualGrid visualGrid) => _visualGrid = visualGrid;

	public override void OnMouseUp(CellContext sender, MouseEventArgs e) {
		base.OnMouseUp(sender, e);
		// Releasing a dragged column boundary is not a click on the header.
		if (e.Button != MouseButtons.Left || sender.Cell.FindController(typeof(SourceGrid.Cells.Controllers.Resizable)) is SourceGrid.Cells.Controllers.Resizable { EndedResize: true })
			return;
		var cellRect = sender.Grid.PositionToRectangle(sender.Position);
		var mousePoint = new Point(e.X, e.Y);

		var logicalBorder = new Sphere10.Framework.Drawing.RectangleBorder(new Sphere10.Framework.Drawing.BorderLine(Color.Black, SourceGrid.Cells.Controllers.Resizable.GetGrabWidth(sender.Grid)));
		if (logicalBorder.GetPointPartType(cellRect, mousePoint, out _) == Sphere10.Framework.Drawing.RectanglePartType.ContentArea)
			_visualGrid._grid_SortColumnPressed(sender.Position.Column);
	}
}

