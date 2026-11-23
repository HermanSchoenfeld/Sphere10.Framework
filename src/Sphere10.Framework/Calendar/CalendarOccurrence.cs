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

/// <summary>One time an event takes place: the event, and the start and end of this occurrence.</summary>
/// <param name="Event">The event that occurs.</param>
/// <param name="Start">When this occurrence starts.</param>
/// <param name="End">When this occurrence ends. It equals <paramref name="Start"/> for an event without duration.</param>
/// <typeparam name="TLocation">Where an event takes place or what it acts on, as the calendar records it, such as a room name or a program.</typeparam>
/// <typeparam name="TDecoration">How a view may present an event, such as a color, as the calendar defines it.</typeparam>
/// <typeparam name="TTag">The calendar's own record of an event, such as an appointment row or a scheduled task.</typeparam>
public sealed record CalendarOccurrence<TLocation, TDecoration, TTag>(CalendarEvent<TLocation, TDecoration, TTag> Event, DateTime Start, DateTime End) {
	/// <summary>Whether the event has other occurrences.</summary>
	public bool IsRecurring => Event.Recurrence.Frequency != CalendarFrequency.Once;
}
