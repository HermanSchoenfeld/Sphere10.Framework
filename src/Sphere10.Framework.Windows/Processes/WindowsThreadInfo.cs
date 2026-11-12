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
	/// <summary>True only after this read verified retained process/thread handles, IDs, creation times and liveness.</summary>
	public bool IdentityVerified { get; init; }

	public ThreadPriorityLevel? PriorityLevel { get; init; }

	public bool? PriorityBoostEnabled { get; init; }

	/// <summary>Raw CPU cycles; cycle counts must not be converted into elapsed time.</summary>
	public ulong? CycleTime { get; init; }

	/// <summary>Win32 thread start routine when supported and permitted, not the current instruction pointer.</summary>
	public ulong? StartAddress { get; init; }
}
