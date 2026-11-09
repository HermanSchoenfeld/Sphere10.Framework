// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Drawing;
using Sphere10.Framework.Windows.Forms.Drawing;

namespace Sphere10.Framework.Windows.Forms.AppointmentBook;

/// <summary>
/// Draws a free time slot with the grid lines of a SourceGrid cell.
/// </summary>
internal class EmptyCellView : SourceGrid.Cells.Views.Cell {
	public EmptyCellView(BaseAppointmentBook parent) {
		Border = new RectangleBorder(parent.VerticalBorderLine, parent.HorizontalBorderLine);
	}
}

