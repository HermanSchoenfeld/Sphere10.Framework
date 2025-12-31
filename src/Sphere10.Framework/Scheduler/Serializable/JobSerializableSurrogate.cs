// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Xml.Serialization;

namespace Sphere10.Framework;

public class JobSerializableSurrogate {
	public string JobType { get; set; }
	public string Name { get; set; }
	public JobStatus Status { get; set; }
	public JobPolicy Policy { get; set; }

	[XmlElement("Interval", typeof(IntervalScheduleSerializableSurrogate))]
	[XmlElement("DayOfWeek", typeof(DayOfWeekScheduleSerializableSurrogate))]
	[XmlElement("DayOfMonth", typeof(DayOfMonthScheduleSerializableSurrogate))]
	public JobScheduleSerializableSurrogate[] Schedules { get; set; }

}

