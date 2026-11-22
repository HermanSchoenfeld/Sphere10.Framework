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

/// <summary>A calendar that keeps its events in memory, for a user's own entries, for samples and for tests. An added event without an
/// identity is given one.</summary>
public class MemoryCalendar : CalendarBase {
	private readonly object _lock = new();
	private readonly List<CalendarEvent> _events = new();

	public MemoryCalendar(string name, params CalendarEvent[] events)
		: base(name) {
		Guard.ArgumentNotNull(events, nameof(events));
		_events.AddRange(events.Select(Identify));
	}

	protected override Task<CalendarEvent[]> ReadEventsCoreAsync(CancellationToken cancellationToken) {
		lock (_lock)
			return Task.FromResult(_events.ToArray());
	}

	protected override Task<CalendarEvent> AddCoreAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken) {
		var added = Identify(calendarEvent);
		lock (_lock) {
			Guard.Ensure(_events.All(existing => existing.Id != added.Id), $"The calendar '{Name}' already has an event with the identity '{added.Id}'.");
			_events.Add(added);
		}
		return Task.FromResult(added);
	}

	protected override Task<CalendarEvent> UpdateCoreAsync(CalendarEvent original, CalendarEvent changed, CancellationToken cancellationToken) {
		var updated = changed with { Id = original.Id };
		lock (_lock)
			_events[Locate(original)] = updated;
		return Task.FromResult(updated);
	}

	protected override Task RemoveCoreAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken) {
		lock (_lock)
			_events.RemoveAt(Locate(calendarEvent));
		return Task.CompletedTask;
	}

	// The event must still be the one the caller read, so a change made meanwhile is never overwritten.
	private int Locate(CalendarEvent expected) {
		var index = _events.FindIndex(existing => existing.Id == expected.Id);
		Guard.Ensure(index >= 0 && Equals(_events[index], expected), $"The event '{expected.Title}' changed or was removed since it was read.");
		return index;
	}

	private static CalendarEvent Identify(CalendarEvent calendarEvent)
		=> string.IsNullOrEmpty(calendarEvent.Id) ? calendarEvent with { Id = Guid.NewGuid().ToString("N") } : calendarEvent;
}
