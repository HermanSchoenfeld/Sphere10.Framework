// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using Sphere10.Framework.Drawing;
using Sphere10.Framework.Windows.Forms.Drawing;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Exporter;

/// <summary>
/// Grid print document.
/// </summary>
public class GridPrintDocument : PrintDocument {
	private GridVirtual _grid;
	private Cells.Views.Cell _cellPrintView = new Cells.Views.Cell();
	private Cells.Views.Cell _headerCellPrintView = new Cells.Views.Cell();
	private CellRange _rangeToPrint = CellRange.Empty;

	// Header, footer, title
	private Font _pageHeaderFont = null;
	private string _pageHeaderText = string.Empty;
	private Font _pageFooterFont = null;
	private string _pageFooterText = string.Empty;
	private Font _pageTitleFont = null;
	private string _pageTitleText = string.Empty;
	private bool _repeatFixedRows = false;
	private int _nextRowToPrint = 0;
	private int _nextColumnToPrint = 0;
	private float _headerHeight = 0;
	private float _titleHeight = 0;
	private float _footerHeight = 0;
	private int _pageCount = 0;
	private int _pageNo = 0;
	private Pen _borderPen = null;
	private readonly Font _ownedPageHeaderFont;
	private readonly Font _ownedPageTitleFont;
	private readonly Font _ownedPageFooterFont;

	public GridPrintDocument(GridVirtual grid) {
		_grid = grid;
		// Default to empty background
		_cellPrintView.BackColor = Color.Empty;
		_headerCellPrintView.BackColor = Color.Empty;

		// Keep ownership of the defaults independently of caller-supplied font replacements.
		_ownedPageHeaderFont = new Font(grid.Font, FontStyle.Regular);
		_ownedPageTitleFont = new Font(grid.Font, FontStyle.Bold);
		_ownedPageFooterFont = new Font(grid.Font, FontStyle.Regular);
		_pageHeaderFont = _ownedPageHeaderFont;
		_pageTitleFont = _ownedPageTitleFont;
		_pageFooterFont = _ownedPageFooterFont;
	}

	/// <summary>
	/// Cell view when printing.
	/// </summary>
	public Cells.Views.Cell CellPrintView {
		get {
			return _cellPrintView;
		}
	}

	/// <summary>
	/// Header cell view when printing.
	/// </summary>
	public Cells.Views.Cell HeaderCellPrintView {
		get {
			return _headerCellPrintView;
		}
	}

	public CellRange RangeToPrint {
		get {
			return _rangeToPrint;
		}
		set {
			if (value.Start.Row < 0 || value.Start.Column < 0
									|| value.Start.Row >= _grid.Rows.Count || value.Start.Column >= _grid.Columns.Count)
				throw new ArgumentOutOfRangeException("RangeToPrint");

			_rangeToPrint = value;
		}
	}

	/// <summary>Gets or sets the header font; caller-supplied replacements remain caller-owned.</summary>
	public Font PageHeaderFont {
		get {
			return _pageHeaderFont;
		}
		set {
			_pageHeaderFont = value;
		}
	}

	public string PageHeaderText {
		get {
			return _pageHeaderText;
		}
		set {
			_pageHeaderText = value;
		}
	}

	/// <summary>Gets or sets the footer font; caller-supplied replacements remain caller-owned.</summary>
	public Font PageFooterFont {
		get {
			return _pageFooterFont;
		}
		set {
			_pageFooterFont = value;
		}
	}

	public string PageFooterText {
		get {
			return _pageFooterText;
		}
		set {
			_pageFooterText = value;
		}
	}

	/// <summary>Gets or sets the title font; caller-supplied replacements remain caller-owned.</summary>
	public Font PageTitleFont {
		get {
			return _pageTitleFont;
		}
		set {
			_pageTitleFont = value;
		}
	}

	public string PageTitleText {
		get {
			return _pageTitleText;
		}
		set {
			_pageTitleText = value;
		}
	}

	/// <summary>
	/// Property if fixed rows should be repeated on each page.
	/// </summary>
	public bool RepeatFixedRows {
		get {
			return _repeatFixedRows;
		}
		set {
			_repeatFixedRows = value;
		}
	}

	/// <summary>
	/// Return calculated page count.
	/// </summary>
	public int PageCount {
		get {
			if (_pageCount == 0)
				throw new ArgumentNullException("Page count not yet calculated");
			return _pageCount;
		}
	}

	protected override void OnBeginPrint(PrintEventArgs e) {
		base.OnBeginPrint(e);
		if (RangeToPrint.IsEmpty())
			RangeToPrint = new CellRange(0, 0, _grid.Rows.Count - 1, _grid.Columns.Count - 1);

		// Force no border
		_cellPrintView.Border = RectangleBorder.NoBorder;
		_headerCellPrintView.Border = RectangleBorder.NoBorder;

		_nextRowToPrint = RangeToPrint.Start.Row;
		_nextColumnToPrint = RangeToPrint.Start.Column;

		_headerHeight = 0;
		_titleHeight = 0;
		_footerHeight = 0;
		_pageCount = 0;
		_pageNo = 0;
	}

	protected override void OnPrintPage(PrintPageEventArgs e) {
		base.OnPrintPage(e);
		if (_pageCount == 0) {
			_headerHeight = this.GetHeaderHeight(e.Graphics);
			_titleHeight = this.GetTitleHeight(e.Graphics);
			_footerHeight = this.GetFooterHeight(e.Graphics);
			_pageCount = this.PrecalculatePageCount(e);
			_pageNo = 1;
		}

		if (_rangeToPrint.IsEmpty())
			return;

		if (_borderPen == null)
			_borderPen = new Pen(Color.Black);

		var area = new RectangleF(e.MarginBounds.Left,
			e.MarginBounds.Top + _headerHeight + (_nextRowToPrint == RangeToPrint.Start.Row ? _titleHeight : 0),
			e.MarginBounds.Width,
			e.MarginBounds.Height - _headerHeight - (_nextRowToPrint == RangeToPrint.Start.Row ? _titleHeight : 0) - _footerHeight);

		this.DrawHeader(e.Graphics,
			new RectangleF(e.MarginBounds.Left, e.MarginBounds.Top, e.MarginBounds.Width, _headerHeight),
			_pageNo,
			_pageCount);

		if (_pageNo == 1)
			this.DrawTitle(e.Graphics,
				new RectangleF(e.MarginBounds.Left, e.MarginBounds.Top + _headerHeight, e.MarginBounds.Width, _titleHeight),
				_pageNo,
				_pageCount);

		var columnHasTopBorder = new List<int>();
		var rowHasLeftBorder = new List<int>();
		var printedRanges = new RangeCollection();

		var pageFirstRow = _nextRowToPrint;
		var pageFirstColumn = _nextColumnToPrint;
		var pageLastColumn = RangeToPrint.End.Column;

		// Pre-calculate width of the table in current page
		float pageColumnWidth = 0;
		for (var index = _nextColumnToPrint; index <= RangeToPrint.End.Column; index++) {
			var colWidth = this.GetColumnWidth(e.Graphics, index);
			if (index == _nextColumnToPrint && colWidth > area.Width)
				colWidth = area.Width;
			if (pageColumnWidth + colWidth <= area.Width)
				pageColumnWidth += colWidth;
			else
				break;
		}

		// Support for fixed row repeat
		if (RepeatFixedRows && _grid.ActualFixedRows > RangeToPrint.Start.Row)
			_nextRowToPrint = RangeToPrint.Start.Row;

		var curY = area.Top;
		while (_nextRowToPrint <= RangeToPrint.End.Row) {
			// If repeated rows are printed, resume printing next rows
			if (RepeatFixedRows && _nextRowToPrint >= _grid.ActualFixedRows && _nextRowToPrint < pageFirstRow)
				_nextRowToPrint = pageFirstRow;
			var rowHeight = this.GetRowHeight(e.Graphics, _nextRowToPrint);
			// Check if row fits in current page
			if (curY + rowHeight > area.Bottom)
				break;
			var curX = area.Left;
			while (_nextColumnToPrint <= pageLastColumn) {
				var colWidth = this.GetColumnWidth(e.Graphics, _nextColumnToPrint);
				// Check if column fits in current page
				if (curX + colWidth > area.Right) {
					// If single column does not fit in page, force it
					if (_nextColumnToPrint == pageFirstColumn)
						colWidth = area.Right - curX;
					else {
						pageLastColumn = _nextColumnToPrint - 1;
						break;
					}
				}
				RectangleF cellRectangle;
				var position = new Position(_nextRowToPrint, _nextColumnToPrint);
				var range = _grid.PositionToCellRange(position);

				// Check if cell is spanned
				if (range.ColumnsCount > 1 || range.RowsCount > 1) {
					var rangeSize = _grid.RangeToSize(range);
					// Is the first position, draw allways
					if (range.Start == position) {
						cellRectangle = new RectangleF(curX, curY, rangeSize.Width, rangeSize.Height);
						printedRanges.Add(range);
					} else {
						// Draw only if this cell is not already drawn on current page
						if (!printedRanges.ContainsCell(position)) {
							// Calculate offset
							var sX = curX;
							for (var i = position.Column - 1; i >= range.Start.Column; i--) {
								var cw = this.GetColumnWidth(e.Graphics, i);
								sX -= cw;
							}
							var sY = curY;
							for (var i = position.Row - 1; i >= range.Start.Row; i--) {
								var cw = this.GetRowHeight(e.Graphics, i);
								sY -= cw;
							}
							cellRectangle = new RectangleF(sX, sY, rangeSize.Width, rangeSize.Height);
							printedRanges.Add(range);
						} else
							cellRectangle = RectangleF.Empty;
					}
				} else
					cellRectangle = new RectangleF(curX, curY, colWidth, rowHeight);

				if (!cellRectangle.IsEmpty) {
					var cell = _grid.GetCell(position);
					if (cell != null) {
						var context = new CellContext(_grid, position, cell);
						var clip = new RectangleF(Math.Max(cellRectangle.Left, area.Left),
							Math.Max(cellRectangle.Top, area.Top),
							Math.Min(cellRectangle.Right, area.Left + pageColumnWidth) - Math.Max(cellRectangle.Left, area.Left),
							Math.Min(cellRectangle.Bottom, area.Bottom) - Math.Max(cellRectangle.Top, area.Top));
						using var prevClip = e.Graphics.Clip;
						using var cellClip = new Region(clip);
						try {
							e.Graphics.Clip = cellClip;
							this.DrawCell(e.Graphics, context, cellRectangle);
						} finally {
							// Restore clip region
							e.Graphics.Clip = prevClip;
						}
						// Check if left border can be drawn in current page
						if (!ContainsInRange(rowHasLeftBorder, range.Start.Row, range.End.Row)
							&& cellRectangle.Left >= area.Left) {
							e.Graphics.DrawLine(_borderPen, cellRectangle.Left, clip.Top, cellRectangle.Left, clip.Bottom);
							AddRange(rowHasLeftBorder, range.Start.Row, range.End.Row);
						}
						// Check if top border can be drawn in current page
						if (!ContainsInRange(columnHasTopBorder, range.Start.Column, range.End.Column)
							&& cellRectangle.Top >= area.Top) {
							e.Graphics.DrawLine(_borderPen, clip.Left, cellRectangle.Top, clip.Right, cellRectangle.Top);
							AddRange(columnHasTopBorder, range.Start.Column, range.End.Column);
						}
						// Check if right border can be drawn in current page
						if (cellRectangle.Right <= area.Right)
							e.Graphics.DrawLine(_borderPen, cellRectangle.Right, clip.Top, cellRectangle.Right, clip.Bottom);
						// Check if bottom border can be drawn in current page
						if (cellRectangle.Bottom <= area.Bottom)
							e.Graphics.DrawLine(_borderPen, clip.Left, cellRectangle.Bottom, clip.Right, cellRectangle.Bottom);
					}
				}
				// Set next column position
				curX += colWidth;
				_nextColumnToPrint++;
			}
			// Set next row Y position
			curY += rowHeight;
			_nextRowToPrint++;
			_nextColumnToPrint = pageFirstColumn;
		}
		// If we have not reached last column, we will continue on next page
		if (pageLastColumn != RangeToPrint.End.Column) {
			_nextRowToPrint = pageFirstRow;
			_nextColumnToPrint = pageLastColumn + 1;
		} else
			_nextColumnToPrint = RangeToPrint.Start.Column;

		this.DrawFooter(e.Graphics,
			new RectangleF(e.MarginBounds.Left, e.MarginBounds.Bottom - _footerHeight, e.MarginBounds.Width, _footerHeight),
			_pageNo,
			_pageCount);

		// If we have not reached last row we will continue on next page
		e.HasMorePages = (_nextRowToPrint <= RangeToPrint.End.Row);
		// If there are no more pages, release resources
		if (e.HasMorePages)
			_pageNo++;
	}

	protected override void OnEndPrint(PrintEventArgs e) {
		base.OnEndPrint(e);
		// Release resources
		if (_borderPen != null) {
			_borderPen.Dispose();
			_borderPen = null;
		}
	}

	/// <summary>Releases this document's fonts and drawing pen while preserving borrowed resources.</summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			_ownedPageHeaderFont.Dispose();
			_ownedPageTitleFont.Dispose();
			_ownedPageFooterFont.Dispose();
			_borderPen?.Dispose();
			_borderPen = null;
		}
		base.Dispose(disposing);
	}

	protected virtual float GetHeaderHeight(Graphics g) {
		if (string.IsNullOrEmpty(this.PageHeaderText))
			return 0;
		return g.MeasureString(this.PageHeaderText, this.PageHeaderFont).Height * 1.5f;
	}

	protected virtual float GetTitleHeight(Graphics g) {
		if (string.IsNullOrEmpty(this.PageTitleText))
			return 0;
		return g.MeasureString(this.PageTitleText, this.PageTitleFont).Height * 2f;
	}

	protected virtual float GetFooterHeight(Graphics g) {
		if (string.IsNullOrEmpty(this.PageFooterText))
			return 0;
		return g.MeasureString(this.PageFooterText, this.PageFooterFont).Height * 1.5f;
	}

	protected virtual void DrawHeader(Graphics g, RectangleF clip, int pageNo, int pageCount) {
		DrawText(g, this.PageHeaderText, this.PageHeaderFont, Brushes.Black, clip, pageNo, pageCount);
	}

	protected virtual void DrawTitle(Graphics g, RectangleF clip, int pageNo, int pageCount) {
		DrawText(g, this.PageTitleText, this.PageTitleFont, Brushes.Black, clip, pageNo, pageCount);
	}

	protected virtual void DrawFooter(Graphics g, RectangleF clip, int pageNo, int pageCount) {
		DrawText(g, this.PageFooterText, this.PageFooterFont, Brushes.Black, clip, pageNo, pageCount);
	}

	protected virtual int PrecalculatePageCount(PrintPageEventArgs e) {
		var pageCount = 1;
		var columnPageCount = 1;

		float totalWidth = e.MarginBounds.Width;
		float pageFirstColumn = RangeToPrint.Start.Column;
		for (var c = RangeToPrint.Start.Column; c <= RangeToPrint.End.Column; c++) {
			var colWidth = this.GetColumnWidth(e.Graphics, c);
			if (c == pageFirstColumn && colWidth > e.MarginBounds.Width)
				colWidth = e.MarginBounds.Width;
			if (totalWidth - colWidth >= 0)
				totalWidth -= colWidth;
			else {
				columnPageCount++;
				totalWidth = e.MarginBounds.Width;
				pageFirstColumn = c;
				c--;
			}
		}

		float fixedRowHeight = 0;
		var totalHeight = e.MarginBounds.Height - _headerHeight - _titleHeight - _footerHeight;
		for (var r = RangeToPrint.Start.Row; r <= RangeToPrint.End.Row; r++) {
			var rowHeight = this.GetRowHeight(e.Graphics, r);
			if (RepeatFixedRows && r < _grid.ActualFixedRows - RangeToPrint.Start.Row)
				fixedRowHeight += rowHeight;
			if (totalHeight - rowHeight >= 0)
				totalHeight -= rowHeight;
			else {
				// If fixed rows takes whole page, disable fixed row repeating
				if (r <= _grid.ActualFixedRows) {
					fixedRowHeight = 0;
					_repeatFixedRows = false;
				}
				totalHeight = e.MarginBounds.Height - _headerHeight - _footerHeight - fixedRowHeight;
				pageCount++;
				r--;
			}
		}
		return pageCount * columnPageCount;
	}

	protected virtual float GetRowHeight(Graphics g, int row) {
		return (float)_grid.Rows.GetHeight(row);
	}

	protected virtual float GetColumnWidth(Graphics g, int column) {
		return (float)_grid.Columns.GetWidth(column);
	}

	protected virtual void DrawCell(Graphics g, CellContext context, RectangleF rect) {
		if (context.Cell == null)
			return;
		using var graphics = new GraphicsCache(g);
		if (context.Cell is Cells.Virtual.ColumnHeader || context.Cell is Cells.ColumnHeader
													|| context.Cell is Cells.Virtual.RowHeader || context.Cell is Cells.RowHeader
													|| context.Cell is Cells.Virtual.Header || context.Cell is Cells.Header)
			_headerCellPrintView.DrawCell(context, graphics, rect);
		else {
			// Copy text alignment
			_cellPrintView.TextAlignment = context.Cell.View.TextAlignment;
			// If cell view copy some more view options
			if (context.Cell.View is SourceGrid.Cells.Views.Cell) {
				_cellPrintView.AnchorArea = ((Cells.Views.Cell)context.Cell.View).AnchorArea;
				_cellPrintView.ImageAlignment = ((Cells.Views.Cell)context.Cell.View).ImageAlignment;
				_cellPrintView.ImageStretch = ((Cells.Views.Cell)context.Cell.View).ImageStretch;
				_cellPrintView.Padding = ((Cells.Views.Cell)context.Cell.View).Padding;
				_cellPrintView.TrimmingMode = ((Cells.Views.Cell)context.Cell.View).TrimmingMode;
				_cellPrintView.WordWrap = ((Cells.Views.Cell)context.Cell.View).WordWrap;
			}
			_cellPrintView.DrawCell(context, graphics, rect);
		}
	}

	private static void DrawText(Graphics graphics, string text, Font font, Brush brush, RectangleF clip, int pageNo, int pageCount) {
		if (string.IsNullOrEmpty(text))
			return;
		var parts = text.Split('\t');
		using var format = StringFormat.GenericDefault;
		format.LineAlignment = StringAlignment.Center;
		format.Trimming = StringTrimming.Character;
		if (!string.IsNullOrEmpty(parts[0])) {
			format.Alignment = StringAlignment.Near;
			graphics.DrawString(ReplaceValues(parts[0], pageNo, pageCount), font, brush, clip, format);
		}
		if (parts.GetLength(0) > 1 && !string.IsNullOrEmpty(parts[1])) {
			format.Alignment = StringAlignment.Center;
			graphics.DrawString(ReplaceValues(parts[1], pageNo, pageCount), font, brush, clip, format);
		}
		if (parts.GetLength(0) > 2 && !string.IsNullOrEmpty(parts[2])) {
			format.Alignment = StringAlignment.Far;
			graphics.DrawString(ReplaceValues(parts[2], pageNo, pageCount), font, brush, clip, format);
		}
	}

	private static string ReplaceValues(string text, int pageNo, int pageCount) {
		return text.Replace("[PageNo]", pageNo.ToString())
			.Replace("[PageCount]", pageCount.ToString());
	}

	// Helper methods
	private static void AddRange(List<int> list, int from, int to) {
		for (var index = from; index <= to; index++)
			list.Add(index);
	}

	private static bool ContainsInRange(List<int> list, int from, int to) {
		foreach (var index in list)
			if (index >= from && index <= to)
				return true;
		return false;
	}
}
