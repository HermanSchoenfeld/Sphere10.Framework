// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// A named collection of <see cref="CalendarEvent"/> records that can be read and, unless it is read-only, changed. A person's
/// calendar, an application's job schedule and an operating system's task scheduler are each one calendar, so the same views and
/// tools work over all of them.
/// </summary>
public interface ICalendar {
	/// <summary>Raised after the calendar's events were added to, changed or removed, by this object or by what it represents.</summary>
	event EventHandlerEx Changed;

	string Name { get; }

	/// <summary>Whether <see cref="AddAsync"/>, <see cref="UpdateAsync"/> and <see cref="RemoveAsync"/> are refused.</summary>
	bool IsReadOnly { get; }

	/// <summary>Reads every event, whether or not it has occurrences in any particular period.</summary>
	Task<CalendarEvent[]> ReadEventsAsync(CancellationToken cancellationToken = default);

	/// <summary>Adds an event and returns it as the calendar now holds it, with its identity.</summary>
	Task<CalendarEvent> AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);

	/// <summary>Replaces an event the caller read earlier. It fails when the calendar no longer holds that event as it was read.</summary>
	Task<CalendarEvent> UpdateAsync(CalendarEvent original, CalendarEvent changed, CancellationToken cancellationToken = default);

	/// <summary>Removes an event the caller read earlier. It fails when the calendar no longer holds that event as it was read.</summary>
	Task RemoveAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);
}
