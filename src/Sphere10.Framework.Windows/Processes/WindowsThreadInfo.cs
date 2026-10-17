// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;

namespace Sphere10.Framework.Windows;

public sealed record WindowsThreadInfo(int ThreadId, ThreadState? State, ThreadWaitReason? WaitReason, int? BasePriority, int? CurrentPriority, TimeSpan? CpuTime, DateTime? StartTimeUtc) {
	public ThreadPriorityLevel? PriorityLevel { get; init; }

	public bool? PriorityBoostEnabled { get; init; }
}
