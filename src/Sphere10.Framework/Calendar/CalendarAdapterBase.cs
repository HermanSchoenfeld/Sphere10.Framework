// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// Presents a calendar of one event type as a calendar of another, such as a scheduled-task calendar as the text-and-color calendar a
/// view draws. A derived adapter converts each event both ways through <see cref="ToOuter"/> and <see cref="ToInner"/>; this base reads,
/// adds, changes and removes the inner calendar's events and keeps the inner event behind each outer one.
/// </summary>
/// <remarks>
/// A change or removal goes to the inner event as it was last read through this adapter, so the inner calendar still refuses one that
/// changed meanwhile. Read the events before changing them. <see cref="Changed"/> is the inner calendar's.
/// </remarks>
/// <typeparam name="TLocation">The location type the adapter presents.</typeparam>
/// <typeparam name="TDecoration">The decoration type the adapter presents.</typeparam>
/// <typeparam name="TTag">The tag type the adapter presents.</typeparam>
/// <typeparam name="TInnerLocation">The location type of the adapted calendar.</typeparam>
/// <typeparam name="TInnerDecoration">The decoration type of the adapted calendar.</typeparam>
/// <typeparam name="TInnerTag">The tag type of the adapted calendar.</typeparam>
public abstract class CalendarAdapterBase<TLocation, TDecoration, TTag, TInnerLocation, TInnerDecoration, TInnerTag> : ICalendar<TLocation, TDecoration, TTag> {
	public event EventHandlerEx Changed { add => InternalCalendar.Changed += value; remove => InternalCalendar.Changed -= value; }

	private readonly Dictionary<string, CalendarEvent<TInnerLocation, TInnerDecoration, TInnerTag>> _innerEvents = new();

	protected CalendarAdapterBase(ICalendar<TInnerLocation, TInnerDecoration, TInnerTag> internalCalendar) {
		Guard.ArgumentNotNull(internalCalendar, nameof(internalCalendar));
		InternalCalendar = internalCalendar;
	}

	public virtual string Name => InternalCalendar.Name;

	public virtual bool IsReadOnly => InternalCalendar.IsReadOnly;

	/// <summary>The adapted calendar.</summary>
	protected ICalendar<TInnerLocation, TInnerDecoration, TInnerTag> InternalCalendar { get; }

	public async Task<CalendarEvent<TLocation, TDecoration, TTag>[]> ReadEventsAsync(CancellationToken cancellationToken = default) {
		var events = await InternalCalendar.ReadEventsAsync(cancellationToken).ConfigureAwait(false) ?? Array.Empty<CalendarEvent<TInnerLocation, TInnerDecoration, TInnerTag>>();
		lock (_innerEvents) {
			_innerEvents.Clear();
			foreach (var innerEvent in events.Where(innerEvent => innerEvent?.Id != null))
				_innerEvents[innerEvent.Id] = innerEvent;
		}
		return events.Select(ToOuter).ToArray();
	}

	public async Task<CalendarEvent<TLocation, TDecoration, TTag>> AddAsync(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(calendarEvent, nameof(calendarEvent));
		var added = await InternalCalendar.AddAsync(ToInner(calendarEvent, null), cancellationToken).ConfigureAwait(false);
		Remember(added);
		return ToOuter(added);
	}

	public async Task<CalendarEvent<TLocation, TDecoration, TTag>> UpdateAsync(CalendarEvent<TLocation, TDecoration, TTag> original, CalendarEvent<TLocation, TDecoration, TTag> changed, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(original, nameof(original));
		Guard.ArgumentNotNull(changed, nameof(changed));
		var innerOriginal = GetInnerEvent(original);
		var updated = await InternalCalendar.UpdateAsync(innerOriginal, ToInner(changed, innerOriginal), cancellationToken).ConfigureAwait(false);
		Remember(updated);
		return ToOuter(updated);
	}

	public async Task RemoveAsync(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(calendarEvent, nameof(calendarEvent));
		var innerEvent = GetInnerEvent(calendarEvent);
		await InternalCalendar.RemoveAsync(innerEvent, cancellationToken).ConfigureAwait(false);
		lock (_innerEvents)
			_innerEvents.Remove(innerEvent.Id);
	}

	/// <summary>The inner event behind an event this adapter returned.</summary>
	public CalendarEvent<TInnerLocation, TInnerDecoration, TInnerTag> GetInnerEvent(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent) {
		Guard.ArgumentNotNull(calendarEvent, nameof(calendarEvent));
		CalendarEvent<TInnerLocation, TInnerDecoration, TInnerTag> innerEvent = null;
		lock (_innerEvents)
			Guard.Ensure(calendarEvent.Id != null && _innerEvents.TryGetValue(calendarEvent.Id, out innerEvent),
				$"The event '{calendarEvent.Title}' was not read through the calendar '{Name}'.");
		return innerEvent;
	}

	/// <summary>Presents an inner event. <see cref="CalendarEvent{TLocation, TDecoration, TTag}.Project{TOtherLocation, TOtherDecoration, TOtherTag}"/>
	/// copies the parts both types share.</summary>
	protected abstract CalendarEvent<TLocation, TDecoration, TTag> ToOuter(CalendarEvent<TInnerLocation, TInnerDecoration, TInnerTag> innerEvent);

	/// <summary>Converts an added or changed event for the inner calendar. <paramref name="innerOriginal"/> is the inner event being changed,
	/// whose own location, decoration and tag can be kept, or null when the event is being added.</summary>
	protected abstract CalendarEvent<TInnerLocation, TInnerDecoration, TInnerTag> ToInner(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, CalendarEvent<TInnerLocation, TInnerDecoration, TInnerTag> innerOriginal);

	private void Remember(CalendarEvent<TInnerLocation, TInnerDecoration, TInnerTag> innerEvent) {
		if (innerEvent?.Id == null)
			return;
		lock (_innerEvents)
			_innerEvents[innerEvent.Id] = innerEvent;
	}
}
