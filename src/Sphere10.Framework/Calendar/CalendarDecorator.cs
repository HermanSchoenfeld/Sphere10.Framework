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
/// Wraps a calendar of the same event types and forwards every member to it. Derive to change part of its behavior, such as hiding events
/// or refusing changes, without reimplementing the rest.
/// </summary>
/// <typeparam name="TLocation">The type of the events' locations.</typeparam>
/// <typeparam name="TDecoration">The type of the events' decorations.</typeparam>
/// <typeparam name="TTag">The type of the calendar's own records of its events.</typeparam>
/// <typeparam name="TConcrete">The type of the decorated calendar.</typeparam>
public abstract class CalendarDecorator<TLocation, TDecoration, TTag, TConcrete> : ICalendar<TLocation, TDecoration, TTag>
	where TConcrete : ICalendar<TLocation, TDecoration, TTag> {
	public event EventHandlerEx Changed { add => InternalCalendar.Changed += value; remove => InternalCalendar.Changed -= value; }

	protected CalendarDecorator(TConcrete internalCalendar) {
		Guard.ArgumentNotNull(internalCalendar, nameof(internalCalendar));
		InternalCalendar = internalCalendar;
	}

	public virtual string Name => InternalCalendar.Name;

	public virtual bool IsReadOnly => InternalCalendar.IsReadOnly;

	/// <summary>The decorated calendar.</summary>
	protected TConcrete InternalCalendar { get; }

	public virtual Task<CalendarEvent<TLocation, TDecoration, TTag>[]> ReadEventsAsync(CancellationToken cancellationToken = default)
		=> InternalCalendar.ReadEventsAsync(cancellationToken);

	public virtual Task<CalendarEvent<TLocation, TDecoration, TTag>> AddAsync(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, CancellationToken cancellationToken = default)
		=> InternalCalendar.AddAsync(calendarEvent, cancellationToken);

	public virtual Task<CalendarEvent<TLocation, TDecoration, TTag>> UpdateAsync(CalendarEvent<TLocation, TDecoration, TTag> original, CalendarEvent<TLocation, TDecoration, TTag> changed, CancellationToken cancellationToken = default)
		=> InternalCalendar.UpdateAsync(original, changed, cancellationToken);

	public virtual Task RemoveAsync(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, CancellationToken cancellationToken = default)
		=> InternalCalendar.RemoveAsync(calendarEvent, cancellationToken);
}

/// <summary>Wraps any calendar of the same event types. See <see cref="CalendarDecorator{TLocation, TDecoration, TTag, TConcrete}"/>.</summary>
/// <typeparam name="TLocation">The type of the events' locations.</typeparam>
/// <typeparam name="TDecoration">The type of the events' decorations.</typeparam>
/// <typeparam name="TTag">The type of the calendar's own records of its events.</typeparam>
public abstract class CalendarDecorator<TLocation, TDecoration, TTag> : CalendarDecorator<TLocation, TDecoration, TTag, ICalendar<TLocation, TDecoration, TTag>> {
	protected CalendarDecorator(ICalendar<TLocation, TDecoration, TTag> internalCalendar)
		: base(internalCalendar) {
	}
}
