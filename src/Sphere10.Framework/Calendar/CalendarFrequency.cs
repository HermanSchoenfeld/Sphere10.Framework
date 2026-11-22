// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;

namespace Sphere10.Framework;

/// <summary>How a <see cref="CalendarRecurrence"/> repeats.</summary>
public enum CalendarFrequency {
	/// <summary>One occurrence, at the start.</summary>
	Once,

	/// <summary>Every <see cref="CalendarRecurrence.Interval"/> days.</summary>
	Daily,

	/// <summary>On chosen weekdays, every <see cref="CalendarRecurrence.Interval"/> weeks.</summary>
	Weekly,

	/// <summary>On chosen days of chosen months, such as the 1st and 15th, or the last day.</summary>
	Monthly,

	/// <summary>On chosen weekdays of chosen weeks of chosen months, such as the second Tuesday.</summary>
	[Description("Monthly By Weekday")]
	MonthlyDayOfWeek
}
