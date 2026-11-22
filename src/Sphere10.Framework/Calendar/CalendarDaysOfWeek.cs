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

/// <summary>A set of weekdays. The values match the day masks of Windows Task Scheduler and iCalendar order from Sunday.</summary>
[Flags]
public enum CalendarDaysOfWeek {
	None = 0,
	Sunday = 1,
	Monday = 2,
	Tuesday = 4,
	Wednesday = 8,
	Thursday = 16,
	Friday = 32,
	Saturday = 64,
	Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
	Weekend = Saturday | Sunday,
	EveryDay = Weekdays | Weekend
}
