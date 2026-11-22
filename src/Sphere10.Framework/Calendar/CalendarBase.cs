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
public abstract class CalendarBase : ICalendar {
	public event EventHandlerEx Changed;

	protected CalendarBase(string name) {
		Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
		Name = name;
	}

	public string Name { get; }

	public virtual bool IsReadOnly => false;

	public Task<CalendarEvent[]> ReadEventsAsync(CancellationToken cancellationToken = default) => ReadEventsCoreAsync(cancellationToken);

	public async Task<CalendarEvent> AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default) {
		Validate(calendarEvent, nameof(calendarEvent));
		var added = await AddCoreAsync(calendarEvent, cancellationToken).ConfigureAwait(false);
		NotifyChanged();
		return added;
	}

	public async Task<CalendarEvent> UpdateAsync(CalendarEvent original, CalendarEvent changed, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(original, nameof(original));
		Validate(changed, nameof(changed));
		var updated = await UpdateCoreAsync(original, changed, cancellationToken).ConfigureAwait(false);
		NotifyChanged();
		return updated;
	}

	public async Task RemoveAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default) {
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

	protected abstract Task<CalendarEvent[]> ReadEventsCoreAsync(CancellationToken cancellationToken);

	protected abstract Task<CalendarEvent> AddCoreAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken);

	protected abstract Task<CalendarEvent> UpdateCoreAsync(CalendarEvent original, CalendarEvent changed, CancellationToken cancellationToken);

	protected abstract Task RemoveCoreAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken);

	private void Validate(CalendarEvent calendarEvent, string parameter) {
		Guard.ArgumentNotNull(calendarEvent, parameter);
		Guard.Ensure(!IsReadOnly, $"The calendar '{Name}' is read-only.");
		Guard.Argument(calendarEvent.Recurrence != null, parameter, "A calendar event needs a recurrence.");
		var rule = calendarEvent.Recurrence.Validate();
		Guard.Argument(rule.IsSuccess, parameter, string.Join(" ", rule.ErrorMessages));
	}
}
