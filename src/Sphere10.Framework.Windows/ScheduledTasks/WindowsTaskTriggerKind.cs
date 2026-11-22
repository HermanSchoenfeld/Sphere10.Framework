// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.ScheduledTasks;

/// <summary>What starts a scheduled task. The first five kinds follow the clock; the others follow something that happens.</summary>
public enum WindowsTaskTriggerKind {
	/// <summary>Once, at a date and time.</summary>
	Time,

	/// <summary>Every so many days.</summary>
	Daily,

	/// <summary>On chosen weekdays, every so many weeks.</summary>
	Weekly,

	/// <summary>On chosen days of chosen months.</summary>
	Monthly,

	/// <summary>On chosen weekdays of chosen weeks of chosen months.</summary>
	MonthlyDayOfWeek,

	/// <summary>When Windows starts.</summary>
	Boot,

	/// <summary>When a user, or any user, signs in.</summary>
	Logon,

	/// <summary>When the computer becomes idle.</summary>
	Idle,

	/// <summary>When an event matching a query is written to an event log.</summary>
	Event,

	/// <summary>When the task is registered or updated.</summary>
	Registration,

	/// <summary>When a session connects, disconnects, locks or unlocks.</summary>
	SessionStateChange,

	/// <summary>A trigger this library does not model. It is kept exactly as Windows defines it.</summary>
	Other
}
