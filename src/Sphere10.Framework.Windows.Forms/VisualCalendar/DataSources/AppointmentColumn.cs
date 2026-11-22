// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.VisualCalendar;

/// <summary>One column of a calendar. In a resource view every column shows the calendar's own day and stands for a person, room or
/// calendar. In a day-per-column view, such as a week, each column sets <see cref="Date"/> and shows that day.</summary>
public class AppointmentColumn {

	public string Name { get; set; }

	public object UserObject { get; set; }

	/// <summary>The day this column shows in the time-of-day views, or null to show the calendar's own day. The monthly view ignores it.</summary>
	public DateTime? Date { get; set; }

}
