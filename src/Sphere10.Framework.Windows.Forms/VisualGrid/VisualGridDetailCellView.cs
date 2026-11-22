// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Borrowed single-line cell geometry with a small expansion gutter; no font or image handles are cloned.</summary>
internal sealed class VisualGridDetailCellView : SourceGrid.Cells.Views.Cell {
	private readonly bool _expandable;
	private readonly bool _expanded;
	private readonly int _gutter;
	private readonly int _indent;

	public VisualGridDetailCellView(SourceGrid.Cells.Views.Cell prototype, bool expandable, bool expanded, int gutter, int indent = 0) {
		Prototype = prototype;
		_expandable = expandable;
		_expanded = expanded;
		_gutter = gutter;
		_indent = indent;
		Font = prototype.Font;
		ForeColor = prototype.ForeColor;
		BackColor = prototype.BackColor;
		Border = prototype.Border;
		Padding = new Sphere10.Framework.Drawing.Padding(prototype.Padding.Left + gutter + indent, prototype.Padding.Right, prototype.Padding.Top, prototype.Padding.Bottom);
		WordWrap = prototype.WordWrap;
		TrimmingMode = prototype.TrimmingMode;
		TextAlignment = prototype.TextAlignment;
		ImageAlignment = prototype.ImageAlignment;
		ImageStretch = prototype.ImageStretch;
		ElementsDrawMode = prototype.ElementsDrawMode;
		ElementText = prototype.ElementText;
		ElementImage = prototype.ElementImage;
	}

	public SourceGrid.Cells.Views.Cell Prototype { get; }

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		base.OnDraw(graphics, area);
		if (!_expandable)
			return;
		var size = System.Math.Max(7, _gutter - 6);
		var x = (int)area.Left + _indent + 3;
		var y = (int)(area.Top + (area.Height - size) / 2);
		graphics.Graphics.DrawRectangle(SystemPens.ControlDarkDark, x, y, size, size);
		graphics.Graphics.DrawLine(SystemPens.ControlText, x + 2, y + size / 2, x + size - 2, y + size / 2);
		if (!_expanded)
			graphics.Graphics.DrawLine(SystemPens.ControlText, x + size / 2, y + 2, x + size / 2, y + size - 2);
	}
}
