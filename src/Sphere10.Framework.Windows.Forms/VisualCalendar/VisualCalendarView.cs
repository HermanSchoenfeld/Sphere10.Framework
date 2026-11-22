// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;

namespace Sphere10.Framework.Windows.Forms.VisualCalendar;

/// <summary>The period a calendar shows and how it lays it out.</summary>
public enum VisualCalendarView {
	/// <summary>One day: a column for each of the data source's columns and a row for each time slot.</summary>
	[Description("Day")] Day,

	/// <summary>One week: a column for each day and a row for each time slot. Appointments of every column share their day, side by side
	/// where they overlap.</summary>
	[Description("Week")] Week,

	/// <summary>One month: a column for each of the data source's columns and a row for each day.</summary>
	[Description("Month")] Month
}
