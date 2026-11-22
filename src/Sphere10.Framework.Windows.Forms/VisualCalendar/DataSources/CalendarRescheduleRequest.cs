// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.VisualCalendar;

/// <summary>The user's request, made by dragging or resizing in the view, to give an occurrence new times.</summary>
/// <param name="Calendar">The calendar that holds the event.</param>
/// <param name="TargetCalendar">The calendar the occurrence was dropped on, which differs when it was dragged to another calendar's column.</param>
/// <param name="Occurrence">The occurrence as it stands.</param>
/// <param name="NewStart">The requested start.</param>
/// <param name="NewEnd">The requested end.</param>
public sealed record CalendarRescheduleRequest(ICalendar Calendar, ICalendar TargetCalendar, CalendarOccurrence Occurrence, DateTime NewStart, DateTime NewEnd);
