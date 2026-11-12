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
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows;

public static class ProcessCommandService {
	public static Task ExecuteAsync(ProcessIdentity identity, Action<Process> action, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.ArgumentGT(identity.ProcessId, 0, nameof(identity));
		Guard.Ensure(identity.StartTimeUtc.HasValue, "The process start time must be available before applying a process command.");
		return Task.Run(() => {
			ExecuteNative(identity, 0, _ => {
				using var process = Process.GetProcessById(identity.ProcessId);
				Guard.Ensure(!process.HasExited && process.StartTime.ToUniversalTime() == identity.StartTimeUtc.Value, "The process exited or its PID was reused.");
				action(process);
				return true;
			}, cancellationToken.ThrowIfCancellationRequested);
		}, cancellationToken);
	}

	public static bool IsCurrent(ProcessIdentity identity) {
		if (!identity.IsVerified || identity.ProcessId <= 0)
			return false;
		using var handle = WinAPI.KERNEL32.OpenProcess(WinAPI.KERNEL32.ProcessQueryLimitedInformation | WinAPI.KERNEL32.Synchronize, false, (uint)identity.ProcessId);
		return Matches(handle, identity);
	}

	internal static T ExecuteNative<T>(ProcessIdentity identity, uint additionalAccess, Func<WinAPI.KERNEL32.KernelHandle, T> action, Action checkpoint) {
		Debug.AssertNotMainThread();
		Guard.ArgumentGT(identity.ProcessId, 0, nameof(identity));
		Guard.Ensure(identity.IsVerified, "The process start time must be available before applying a process command.");
		checkpoint?.Invoke();
		// The same retained native object is verified and used for dispatch; no reopen by numeric PID occurs after verification.
		using var handle = WinAPI.KERNEL32.OpenProcess(WinAPI.KERNEL32.ProcessQueryLimitedInformation | WinAPI.KERNEL32.Synchronize | additionalAccess, false, (uint)identity.ProcessId);
		Guard.Ensure(Matches(handle, identity), "The process exited, became inaccessible or its PID was reused.");
		checkpoint?.Invoke();
		return action(handle);
	}

	private static bool Matches(WinAPI.KERNEL32.KernelHandle handle, ProcessIdentity identity) {
		if (handle.IsInvalid || !WinAPI.KERNEL32.GetProcessTimes(handle, out var creation, out _, out _, out _))
			return false;
		var started = DateTime.FromFileTimeUtc(checked((long)(((ulong)creation.DateTimeHigh << 32) | creation.DateTimeLow)));
		var matches = false;
		Tools.Exceptions.ExecuteCapturingException<Win32Exception>(() => matches = started == identity.StartTimeUtc && !WindowsProcess.HasSignaledExit(handle), out _);
		return matches;
	}
}
