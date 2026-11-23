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
/// The template for a calendar. A derived calendar reads and writes its own store through the <c>*Core</c> hooks; this base checks
/// arguments, refuses changes to a read-only calendar and announces accepted changes.
/// </summary>
/// <typeparam name="TLocation">Where an event takes place or what it acts on, as the calendar records it, such as a room name or a program.</typeparam>
/// <typeparam name="TDecoration">How a view may present an event, such as a color, as the calendar defines it.</typeparam>
/// <typeparam name="TTag">The calendar's own record of an event, such as an appointment row or a scheduled task.</typeparam>
public abstract class CalendarBase<TLocation, TDecoration, TTag> : ICalendar<TLocation, TDecoration, TTag> {
	public event EventHandlerEx Changed;

	protected CalendarBase(string name) {
		Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
		Name = name;
	}

	public string Name { get; }

	public virtual bool IsReadOnly => false;

	public Task<CalendarEvent<TLocation, TDecoration, TTag>[]> ReadEventsAsync(CancellationToken cancellationToken = default) => ReadEventsCoreAsync(cancellationToken);

	public async Task<CalendarEvent<TLocation, TDecoration, TTag>> AddAsync(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, CancellationToken cancellationToken = default) {
		Validate(calendarEvent, nameof(calendarEvent));
		var added = await AddCoreAsync(calendarEvent, cancellationToken).ConfigureAwait(false);
		NotifyChanged();
		return added;
	}

	public async Task<CalendarEvent<TLocation, TDecoration, TTag>> UpdateAsync(CalendarEvent<TLocation, TDecoration, TTag> original, CalendarEvent<TLocation, TDecoration, TTag> changed, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(original, nameof(original));
		Validate(changed, nameof(changed));
		var updated = await UpdateCoreAsync(original, changed, cancellationToken).ConfigureAwait(false);
		NotifyChanged();
		return updated;
	}

	public async Task RemoveAsync(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(calendarEvent, nameof(calendarEvent));
		Guard.Ensure(!IsReadOnly, $"The calendar '{Name}' is read-only.");
		await RemoveCoreAsync(calendarEvent, cancellationToken).ConfigureAwait(false);
		NotifyChanged();
	}

	/// <summary>Announces a change the calendar learned of by other means, such as a notification from its store.</summary>
	protected void NotifyChanged() {
		OnChanged();
		Changed?.Invoke();
	}

	protected virtual void OnChanged() {
	}

	protected abstract Task<CalendarEvent<TLocation, TDecoration, TTag>[]> ReadEventsCoreAsync(CancellationToken cancellationToken);

	protected abstract Task<CalendarEvent<TLocation, TDecoration, TTag>> AddCoreAsync(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, CancellationToken cancellationToken);

	protected abstract Task<CalendarEvent<TLocation, TDecoration, TTag>> UpdateCoreAsync(CalendarEvent<TLocation, TDecoration, TTag> original, CalendarEvent<TLocation, TDecoration, TTag> changed, CancellationToken cancellationToken);

	protected abstract Task RemoveCoreAsync(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, CancellationToken cancellationToken);

	private void Validate(CalendarEvent<TLocation, TDecoration, TTag> calendarEvent, string parameter) {
		Guard.ArgumentNotNull(calendarEvent, parameter);
		Guard.Ensure(!IsReadOnly, $"The calendar '{Name}' is read-only.");
		Guard.Argument(calendarEvent.Recurrence != null, parameter, "A calendar event needs a recurrence.");
		var rule = calendarEvent.Recurrence.Validate();
		Guard.Argument(rule.IsSuccess, parameter, string.Join(" ", rule.ErrorMessages));
	}
}
