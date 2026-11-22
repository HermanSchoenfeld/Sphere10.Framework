// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.VisualCalendar;

/// <summary>A column of the week view. It stands for a day, not for a column of the data source, so the calendar reports each
/// appointment's own column in its events and to the data source. A free region selected in a week reports this column, whose
/// <see cref="AppointmentColumn.Date"/> is the day.</summary>
public sealed class DayColumn : AppointmentColumn {
}
