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
/// A named collection of <see cref="CalendarEvent{TLocation, TDecoration, TTag}"/> records that can be read and, unless it is read-only, changed. A person's
/// calendar, an application's job schedule and an operating system's task scheduler are each one calendar, so the same views and
/// tools work over all of them.
/// </summary>
/// <typeparam name="TLocation">Where an event takes place or what it acts on, as the calendar records it, such as a room name or a program.</typeparam>
/// <typeparam name="TDecoration">How a view may present an event, such as a color, as the calendar defines it.</typeparam>
/// <typeparam name="TTag">The calendar's own record of an event, such as an appointment row or a scheduled task.</typeparam>
public interface ICalendar<TLocation, TDecoration, TTag> {
	/// <summary>Raised after the calendar's events were added to, changed or removed, by this object or by what it represents.</summary>
	event EventHandlerEx Changed;

	string Name { get; }

	/// <summary>Whether <see cref="AddAsync"/>, <see cref="UpdateAsync"/> and <see cref="RemoveAsync"/> are refused.</summary>
	bool IsReadOnly { get; }

	/// <summary>Reads every event, whether or not it has occurrences in any particular period.</summary>
	Task<CalendarEvent<TLocation, TDecoration, TTag>[]> ReadEventsAsync(CancellationToken cancellationToken = default);

	/// <summary>Adds an event and returns it as the calendar now holds it, with its identity.</summary>
	Task<CalendarEvent<TLocation, TDecoration, TTag>> AddAsync(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, CancellationToken cancellationToken = default);

	/// <summary>Replaces an event the caller read earlier. It fails when the calendar no longer holds that event as it was read.</summary>
	Task<CalendarEvent<TLocation, TDecoration, TTag>> UpdateAsync(CalendarEvent<TLocation, TDecoration, TTag> original, CalendarEvent<TLocation, TDecoration, TTag> changed, CancellationToken cancellationToken = default);

	/// <summary>Removes an event the caller read earlier. It fails when the calendar no longer holds that event as it was read.</summary>
	Task RemoveAsync(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, CancellationToken cancellationToken = default);
}
