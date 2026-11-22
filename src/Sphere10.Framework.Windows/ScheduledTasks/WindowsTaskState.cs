// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.ScheduledTasks;

/// <summary>The state Task Scheduler reports for a registered task.</summary>
public enum WindowsTaskState {
	Unknown = 0,
	Disabled = 1,
	Queued = 2,
	Ready = 3,
	Running = 4
}
