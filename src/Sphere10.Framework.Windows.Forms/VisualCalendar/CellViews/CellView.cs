// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.AppointmentBook;

/// <summary>
/// Draws one line of an appointment in the planning grid's style: a soft fill, tinted text and an outline around the whole appointment.
/// </summary>
internal class CellView : SourceGrid.Cells.Views.Cell {
	private const float TextIndent = 5;

	public CellView(BaseAppointmentBook parent, CellViewModel cellDisplay, bool selected = false) {
		TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
		BackColor = parent.GetAppointmentFillColor(cellDisplay);
		ForeColor = parent.GetAppointmentTextColor(cellDisplay);
		Font = parent.GetAppointmentFont(cellDisplay.FontStyle);
		var outline = selected ? parent.SelectedAppointmentBorderLine : parent.GetAppointmentBorderLine(cellDisplay);

		// The wider selected outline takes its extra width from the indent so the text does not move.
		Padding = new Padding(TextIndent - outline.Width + 1, 2, 0, 0);
		var border = new RectangleBorder {
			Top = BorderLine.NoBorder,
			Left = BorderLine.NoBorder,
			Right = BorderLine.NoBorder,
			Bottom = BorderLine.NoBorder
		};
		if (cellDisplay.Traits.HasFlag(CellTraits.Filled)) {
			border.Left = outline;
			border.Right = outline;
		}
		if (cellDisplay.Traits.HasFlag(CellTraits.Top))
			border.Top = outline;
		if (cellDisplay.Traits.HasFlag(CellTraits.Bottom))
			border.Bottom = outline;
		Border = border;
	}
}
