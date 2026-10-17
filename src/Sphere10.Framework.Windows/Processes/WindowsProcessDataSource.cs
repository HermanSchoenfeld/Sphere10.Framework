// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>A native process inventory IDataSource with cached, searchable, sortable pages and explicit reload.</summary>
public sealed class WindowsProcessDataSource : ReloadableSnapshotDataSource<WindowsProcessInventoryEntry> {
	public WindowsProcessDataSource()
		: this(CaptureProcessesAsync) { }

	public WindowsProcessDataSource(Func<CancellationToken, Task<WindowsProcessInventoryEntry[]>> capture)
		: base(capture) {
		DefaultSortProperty = nameof(WindowsProcessInventoryEntry.ExecutableName);
	}

	private static async Task<WindowsProcessInventoryEntry[]> CaptureProcessesAsync(CancellationToken cancellationToken) {
		var startedUtc = DateTime.UtcNow;
		var entries = await Tools.Windows.Processes.GetProcessesAsync(cancellationToken).ConfigureAwait(false);
		var rows = new List<WindowsProcessInventoryEntry>(entries.Length);
		foreach (var entry in entries) {
			cancellationToken.ThrowIfCancellationRequested();
			var row = new WindowsProcessInventoryEntry {
				ProcessId = entry.ProcessId, ParentProcessId = entry.ParentProcessId, ExecutableName = entry.ExecutableName,
				ThreadCount = entry.ThreadCount, CapturedAtUtc = DateTime.UtcNow
			};
			if (entry.ProcessId > 0)
				try {
					using var process = WindowsProcess.Open(entry.ProcessId);
					var counters = process.ReadCounters();
					if (counters.Identity.StartTimeUtc > startedUtc)
						continue;
					row = row with { Counters = counters, CapturedAtUtc = DateTime.UtcNow, CounterTimestamp = Stopwatch.GetTimestamp() };
					try { row = row with { ExecutablePath = process.ExecutablePath }; }
					catch (Exception error) when (IsUnavailable(error)) { row = row with { ExecutablePathError = error }; }
				} catch (Exception error) when (IsUnavailable(error)) {
					row = row with { ReadError = error };
				}
			rows.Add(row);
		}
		cancellationToken.ThrowIfCancellationRequested();
		return rows.ToArray();
	}

	private static bool IsUnavailable(Exception error)
		=> error is Win32Exception or InvalidOperationException or NotSupportedException or ArgumentException or UnauthorizedAccessException or SecurityException;
}
