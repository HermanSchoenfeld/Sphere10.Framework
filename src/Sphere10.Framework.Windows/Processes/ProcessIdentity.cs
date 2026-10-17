// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

public readonly record struct ProcessIdentity(
	int ProcessId,
	DateTime? StartTimeUtc
) {

	public bool IsVerified => StartTimeUtc.HasValue;

	/// <summary>Captures one live process instance using an owned handle, preventing PID recycling during the capture.</summary>
	public static ProcessIdentity Capture(int processId) {
		Guard.ArgumentGT(processId, 0, nameof(processId));
		using var process = WinAPI.KERNEL32.OpenProcess(WinAPI.KERNEL32.ProcessQueryLimitedInformation | WinAPI.KERNEL32.Synchronize, false, (uint)processId);
		if (process.IsInvalid)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The process could not be opened to verify its identity.");
		if (!WinAPI.KERNEL32.GetProcessTimes(process, out var creation, out _, out _, out _))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The process creation time could not be read.");
		Guard.Ensure(!WindowsProcess.HasSignaledExit(process), "The process exited while its identity was being captured.");
		var started = DateTime.FromFileTimeUtc(checked((long)(((ulong)creation.DateTimeHigh << 32) | creation.DateTimeLow)));
		return new ProcessIdentity(processId, started);
	}

	public static Task<ProcessIdentity> CaptureAsync(int processId, CancellationToken cancellationToken = default) => Task.Run(() => {
		cancellationToken.ThrowIfCancellationRequested();
		return Capture(processId);
	}, cancellationToken);

	public override string ToString() => StartTimeUtc.HasValue ? $"{ProcessId} / {StartTimeUtc:O}" : $"{ProcessId} / start time unavailable";
}
