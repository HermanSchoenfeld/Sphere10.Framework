// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Xml.Serialization;

namespace Sphere10.Framework;

/// <summary>The stored form of an <see cref="OnRecurrenceSchedule{T}"/>. Day, month and week sets are stored as their numeric masks.</summary>
public class RecurrenceScheduleSerializableSurrogate : JobScheduleSerializableSurrogate {

	[XmlAttribute] public CalendarFrequency Frequency { get; set; }

	[XmlAttribute] public string Start { get; set; }

	[XmlAttribute] public int Interval { get; set; }

	[XmlAttribute] public int DaysOfWeek { get; set; }

	[XmlAttribute] public string DaysOfMonth { get; set; }

	[XmlAttribute] public bool LastDayOfMonth { get; set; }

	[XmlAttribute] public int Months { get; set; }

	[XmlAttribute] public int WeeksOfMonth { get; set; }

	[XmlAttribute] public long RepeatEveryMilliseconds { get; set; }

	[XmlAttribute] public long RepeatForMilliseconds { get; set; }

	[XmlAttribute] public string CreatedOn { get; set; }
}
