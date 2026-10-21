// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Queries and controls a captured native thread instance, retaining its process and thread handles throughout each operation.</summary>
public class ThreadUtil {

	public WindowsThreadInfo Read(ThreadIdentity identity) => ReadCore(identity, null);

	public Task<WindowsThreadInfo> ReadAsync(ThreadIdentity identity, CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadCore(identity, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public void SetPriority(ThreadIdentity identity, ThreadPriorityLevel priority) => SetPriorityCore(identity, priority, null);

	public Task SetPriorityAsync(ThreadIdentity identity, ThreadPriorityLevel priority, CancellationToken cancellationToken = default)
		=> Task.Run(() => SetPriorityCore(identity, priority, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public void SetPriorityBoost(ThreadIdentity identity, bool enabled) => SetPriorityBoostCore(identity, enabled, null);

	public Task SetPriorityBoostAsync(ThreadIdentity identity, bool enabled, CancellationToken cancellationToken = default)
		=> Task.Run(() => SetPriorityBoostCore(identity, enabled, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	/// <summary>Increments the native suspend count and returns its previous value. The caller owns the corresponding resume operation.</summary>
	public uint Suspend(ThreadIdentity identity) => SuspendCore(identity, null);

	/// <summary>Cancellation is accepted before native dispatch; cancellation after dispatch does not hide an accepted suspension.</summary>
	public Task<uint> SuspendAsync(ThreadIdentity identity, CancellationToken cancellationToken = default)
		=> Task.Run(() => SuspendCore(identity, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	/// <summary>Decrements the native suspend count once and returns its previous value. A value greater than one means the thread remains suspended.</summary>
	public uint Resume(ThreadIdentity identity) => ResumeCore(identity, null);

	public Task<uint> ResumeAsync(ThreadIdentity identity, CancellationToken cancellationToken = default)
		=> Task.Run(() => ResumeCore(identity, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private static WindowsThreadInfo ReadCore(ThreadIdentity identity, Action checkpoint) => WithThread(identity, 0, thread => {
		// Native queries stay tied to the verified handle. Unavailable optional scheduler fields remain null.
		if (!WinAPI.KERNEL32.GetThreadTimes(thread, out var creation, out _, out var kernel, out var user))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The thread timing information could not be read.");
		var priority = WinAPI.KERNEL32.GetThreadPriority(thread);
		if (priority == WinAPI.KERNEL32.ThreadPriorityErrorReturn)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The thread priority could not be read.");
		if (!WinAPI.KERNEL32.GetThreadPriorityBoost(thread, out var boostDisabled))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The thread priority boost state could not be read.");
		System.Diagnostics.ThreadState? state = null;
		ThreadWaitReason? waitReason = null;
		int? basePriority = null;
		int? currentPriority = null;
		using (var process = Process.GetProcessById(identity.Process.ProcessId)) {
			var threads = process.Threads;
			using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
				foreach (ProcessThread item in threads)
					item.Dispose();
			});
			foreach (ProcessThread item in threads) {
				if (item.Id != identity.ThreadId)
					continue;
				state = ReadOptional(() => item.ThreadState);
				waitReason = state == System.Diagnostics.ThreadState.Wait ? ReadOptional(() => item.WaitReason) : null;
				basePriority = ReadOptional(() => item.BasePriority);
				currentPriority = ReadOptional(() => item.CurrentPriority);
				break;
			}
		}
		EnsureLive(thread, "The thread exited while its information was being read.");
		return new WindowsThreadInfo(identity.ThreadId, state, waitReason, basePriority, currentPriority,
			TimeSpan.FromTicks(checked(Ticks(kernel) + Ticks(user))), DateTime.FromFileTimeUtc(Ticks(creation))) {
			PriorityLevel = (ThreadPriorityLevel)priority,
			PriorityBoostEnabled = !boostDisabled
		};
	}, checkpoint);

	private static void SetPriorityCore(ThreadIdentity identity, ThreadPriorityLevel priority, Action checkpoint) {
		Guard.Argument(Enum.IsDefined(priority), nameof(priority), "Choose a supported thread priority level.");
		WithThread(identity, WinAPI.KERNEL32.ThreadSetLimitedInformation, thread => {
			if (!WinAPI.KERNEL32.SetThreadPriority(thread, (int)priority))
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "The thread priority could not be changed.");
			return true;
		}, checkpoint);
	}

	private static void SetPriorityBoostCore(ThreadIdentity identity, bool enabled, Action checkpoint) {
		WithThread(identity, WinAPI.KERNEL32.ThreadSetLimitedInformation, thread => {
			if (!WinAPI.KERNEL32.SetThreadPriorityBoost(thread, !enabled))
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "The thread priority boost state could not be changed.");
			return true;
		}, checkpoint);
	}

	private static uint SuspendCore(ThreadIdentity identity, Action checkpoint) => WithThread(identity, WinAPI.KERNEL32.ThreadSuspendResume, thread => {
		var previousCount = WinAPI.KERNEL32.SuspendThread(thread);
		if (previousCount == uint.MaxValue)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The thread could not be suspended.");
		return previousCount;
	}, checkpoint);

	private static uint ResumeCore(ThreadIdentity identity, Action checkpoint) => WithThread(identity, WinAPI.KERNEL32.ThreadSuspendResume, thread => {
		var previousCount = WinAPI.KERNEL32.ResumeThread(thread);
		if (previousCount == uint.MaxValue)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The thread could not be resumed.");
		return previousCount;
	}, checkpoint);

	private static T WithThread<T>(ThreadIdentity identity, uint additionalAccess, Func<WinAPI.KERNEL32.KernelHandle, T> operation, Action checkpoint) {
		Guard.ArgumentGT(identity.Process.ProcessId, 0, nameof(identity));
		Guard.ArgumentGT(identity.ThreadId, 0, nameof(identity));
		Guard.Ensure(identity.IsVerified, "Process and thread creation times are required before accessing the thread.");
		checkpoint?.Invoke();

		// Keep both native objects alive through dispatch; never reopen a numeric ID after verification.
		using var process = WinAPI.KERNEL32.OpenProcess(WinAPI.KERNEL32.ProcessQueryLimitedInformation | WinAPI.KERNEL32.Synchronize,
			false, (uint)identity.Process.ProcessId);
		if (process.IsInvalid)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The process could not be opened to verify its identity.");
		if (!WinAPI.KERNEL32.GetProcessTimes(process, out var processCreation, out _, out _, out _))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The process creation time could not be read.");
		Guard.Ensure(DateTime.FromFileTimeUtc(Ticks(processCreation)) == identity.Process.StartTimeUtc, "The process identity changed or its ID was reused.");
		EnsureLive(process, "The owning process has exited.");
		using var thread = WinAPI.KERNEL32.OpenThread(WinAPI.KERNEL32.ThreadQueryLimitedInformation | WinAPI.KERNEL32.Synchronize | additionalAccess,
			false, (uint)identity.ThreadId);
		if (thread.IsInvalid)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The thread could not be opened to verify its identity.");
		var threadId = WinAPI.KERNEL32.GetThreadId(thread);
		if (threadId == 0)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The native thread ID could not be read.");
		var processId = WinAPI.KERNEL32.GetProcessIdOfThread(thread);
		if (processId == 0)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The thread's owning process ID could not be read.");
		Guard.Ensure(threadId == identity.ThreadId && processId == identity.Process.ProcessId, "The thread belongs to a different process or its ID was reused.");
		if (!WinAPI.KERNEL32.GetThreadTimes(thread, out var threadCreation, out _, out _, out _))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The thread creation time could not be read.");
		Guard.Ensure(DateTime.FromFileTimeUtc(Ticks(threadCreation)) == identity.StartTimeUtc, "The thread creation time changed or its ID was reused.");
		EnsureLive(thread, "The thread has exited.");
		EnsureLive(process, "The owning process has exited.");
		checkpoint?.Invoke();
		return operation(thread);
	}

	private static void EnsureLive(WinAPI.KERNEL32.KernelHandle handle, string message) {
		// Exit-time output is undefined for a live thread; a zero-time native wait is authoritative.
		var result = WinAPI.KERNEL32.WaitForSingleObject(handle, 0);
		if (result == WinAPI.KERNEL32.WaitFailed)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The native object liveness could not be checked.");
		Guard.Ensure(result == WinAPI.KERNEL32.WaitTimeout, message);
	}

	private static T? ReadOptional<T>(Func<T> read)
		where T : struct {
		T? value = null;
		Tools.Exceptions.ExecuteCapturingException(() => value = read(), out _, error => error is Win32Exception or InvalidOperationException);
		return value;
	}

	private static long Ticks(WinAPI.KERNEL32.FILETIME value) => checked((long)(((ulong)value.DateTimeHigh << 32) | value.DateTimeLow));
}
