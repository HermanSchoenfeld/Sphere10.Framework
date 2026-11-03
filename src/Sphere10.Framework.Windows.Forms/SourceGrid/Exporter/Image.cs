// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Exporter;

/// <summary>
/// An utility class to export a grid to a csv delimited format file.
/// </summary>
public class Image {
	public Image() {
	}

	public virtual System.Drawing.Bitmap Export(GridVirtual grid, CellRange rangeToExport) {
		System.Drawing.Bitmap bitmap = null;

		try {
			var size = grid.RangeToSize(rangeToExport);

			bitmap = new System.Drawing.Bitmap(size.Width, size.Height);

			using (var graphic = System.Drawing.Graphics.FromImage(bitmap)) {
				Export(grid, graphic, rangeToExport, new System.Drawing.Point(0, 0));
			}
		} catch (Exception) {
			if (bitmap != null) {
				bitmap.Dispose();
				bitmap = null;
			}

			throw;
		}

		return bitmap;
	}

	public virtual void Export(GridVirtual grid, System.Drawing.Graphics graphics, CellRange rangeToExport, System.Drawing.Point destinationLocation) {
		if (rangeToExport.IsEmpty())
			return;

		var cellPoint = destinationLocation;

		using (var graphicsCache = new Sphere10.Framework.Drawing.GraphicsCache(graphics)) {
			for (var r = rangeToExport.Start.Row; r <= rangeToExport.End.Row; r++) {
				var rowHeight = grid.Rows.GetHeight(r);

				for (var c = rangeToExport.Start.Column; c <= rangeToExport.End.Column; c++) {
					Rectangle cellRectangle;
					var position = new Position(r, c);

					var cellSize = new Size(grid.Columns.GetWidth(c), rowHeight);

					var range = grid.PositionToCellRange(position);

					//support for RowSpan or ColSpan
					//Note: for now I draw only the merged cell at the first position
					// (this can cause a problem if you export partial range that contains a partial merged cells)
					if (range.ColumnsCount > 1 || range.RowsCount > 1) {
						//Is the first position
						if (range.Start == position) {
							var rangeSize = grid.RangeToSize(range);

							cellRectangle = new Rectangle(cellPoint, rangeSize);
						} else
							cellRectangle = Rectangle.Empty;
					} else
						cellRectangle = new Rectangle(cellPoint, cellSize);

					if (cellRectangle.IsEmpty == false) {
						var cell = grid.GetCell(position);
						var context = new CellContext(grid, position, cell);
						ExportCell(context, graphicsCache, cellRectangle);
					}

					cellPoint = new Point(cellPoint.X + cellSize.Width, cellPoint.Y);
				}

				cellPoint = new Point(destinationLocation.X, cellPoint.Y + rowHeight);
			}
		}
	}

	protected virtual void ExportCell(CellContext context, Sphere10.Framework.Drawing.GraphicsCache graphics, System.Drawing.Rectangle rectangle) {
		if (context.Cell != null)
			context.Cell.View.DrawCell(context, graphics, rectangle);
	}
}
