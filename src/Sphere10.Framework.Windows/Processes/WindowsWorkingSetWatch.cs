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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>One exclusive in-process drain owner. Starting enables persistent kernel monitoring until target exit; disposing only stops this reader.</summary>
public sealed class WindowsWorkingSetWatch : IDisposable {
	private static readonly SynchronizedObject RegistrySync = new();

	private static readonly HashSet<ProcessIdentity> Readers = new();

	private readonly WinAPI.KERNEL32.KernelHandle _handle;

	private readonly SynchronizedObject _sync = new();

	private bool _disposed;

	private WindowsWorkingSetWatch(ProcessIdentity process, WinAPI.KERNEL32.KernelHandle handle) {
		Process = process;
		_handle = handle;
	}

	public ProcessIdentity Process { get; }

	/// <summary>The caller must explicitly confirm persistent target monitoring before starting. It cannot be disabled by this API.</summary>
	public static Task<WindowsWorkingSetWatch> StartAsync(ProcessIdentity process, CancellationToken cancellationToken = default)
		=> Task.Run(() => StartCore(process, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	/// <summary>Synchronous worker primitive for an already-background scheduler; the native core asserts against UI-thread use.</summary>
	public WindowsWorkingSetSample Read(int maximumRecords = 65536) {
		Guard.ArgumentInRange(maximumRecords, 1, 262144, nameof(maximumRecords));
		return ReadCore(maximumRecords, null);
	}

	public Task<WindowsWorkingSetSample> ReadAsync(int maximumRecords = 65536, CancellationToken cancellationToken = default) {
		Guard.ArgumentInRange(maximumRecords, 1, 262144, nameof(maximumRecords));
		return Task.Run(() => ReadCore(maximumRecords, cancellationToken.ThrowIfCancellationRequested), cancellationToken);
	}

	public void Dispose() {
		using var scope = _sync.EnterWriteScope();
		if (_disposed)
			return;
		_disposed = true;
		_handle.Dispose();
		using (RegistrySync.EnterWriteScope())
			Readers.Remove(Process);
	}

	private static WindowsWorkingSetWatch StartCore(ProcessIdentity process, Action checkpoint) {
		Debug.AssertNotMainThread();
		Guard.Ensure(process.IsVerified && process.ProcessId > 0, "Working-set monitoring requires a verified live process.");
		using (RegistrySync.EnterWriteScope())
			Guard.Ensure(Readers.Add(process), "This process already has an active working-set reader in this application.");
		var transferred = false;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			if (!transferred) {
				using (RegistrySync.EnterWriteScope())
					Readers.Remove(process);
			}
		});
		// Enabling is a process-information mutation. Current Windows rejects a query-only handle even though older PSAPI documentation lists only query access.
		var result = ProcessCommandService.ExecuteNative(process, WinAPI.KERNEL32.ProcessQueryInformation | WinAPI.KERNEL32.ProcessSetInformation, handle => {
			var current = WinAPI.KERNEL32.GetCurrentProcess();
			if (!WinAPI.KERNEL32.DuplicateHandle(current, handle, current, out var retained, 0, false, 2))
				throw new Win32Exception(Marshal.GetLastPInvokeError());
			var accepted = false;
			using var release = Tools.Scope.ExecuteOnDispose(() => {
				if (!accepted)
					retained.Dispose();
			});
			checkpoint?.Invoke();
			if (!WinAPI.PSAPI.InitializeProcessForWsWatch(retained))
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not enable working-set monitoring.");
			// Do not throw cancellation after initialization: the caller must receive the owner of the now-enabled kernel state.
			accepted = true;
			return new WindowsWorkingSetWatch(process, retained);
		}, checkpoint);
		transferred = true;
		return result;
	}

	private WindowsWorkingSetSample ReadCore(int maximumRecords, Action checkpoint) {
		Debug.AssertNotMainThread();
		using var scope = _sync.EnterWriteScope();
		ObjectDisposedException.ThrowIf(_disposed, this);
		checkpoint?.Invoke();
		Guard.Ensure(!WindowsProcess.HasSignaledExit(_handle), "The monitored process exited. Its working-set reader has stopped.");
		var capacity = Math.Min(maximumRecords + 1, 4096);
		while (true) {
			checkpoint?.Invoke();
			var buffer = new WinAPI.PSAPI.WS_WATCH_INFORMATION_EX[capacity];
			var itemSize = Marshal.SizeOf<WinAPI.PSAPI.WS_WATCH_INFORMATION_EX>();
			var bytes = checked((uint)(buffer.Length * itemSize));
			if (!WinAPI.PSAPI.GetWsChangesEx(_handle, buffer, ref bytes)) {
				var error = Marshal.GetLastPInvokeError();
				if (error == 122 && capacity < maximumRecords + 1) {
					capacity = Math.Min(maximumRecords + 1, capacity * 2);
					continue;
				}

				if (error == 259)
					return new WindowsWorkingSetSample(Process, DateTime.UtcNow, Array.Empty<WindowsWorkingSetFault>(), 0, "Windows reported no available records or a concurrent external drain.");
				throw new Win32Exception(error, error == 122
					? "The kernel working-set buffer exceeds the configured drain limit. Increase the limit before retrying; no complete drain was returned."
					: "Working-set changes could not be drained.");
			}

			Guard.Ensure(bytes <= buffer.Length * itemSize, "Windows returned an invalid working-set buffer length.");
			var rows = new List<WindowsWorkingSetFault>(Math.Min(capacity - 1, maximumRecords));
			var count = (int)(bytes / itemSize);
			for (var index = 0; index < count; index++) {
				var row = buffer[index];
				if (row.FaultingPc == IntPtr.Zero)
					return new WindowsWorkingSetSample(Process, DateTime.UtcNow, rows.ToArray(), Address(row.FaultingVa), string.Empty);
				Guard.Ensure(rows.Count < maximumRecords, "The working-set drain exceeds the configured record limit.");
				rows.Add(new WindowsWorkingSetFault(Address(row.FaultingPc), Address(row.FaultingVa), row.FaultingThreadId.ToUInt64(), row.Flags.ToUInt64()));
			}

			throw new InvalidOperationException("Windows returned working-set records without the required terminator; completeness is unavailable.");
		}
	}

	private static ulong Address(IntPtr value) => IntPtr.Size == 8 ? unchecked((ulong)value.ToInt64()) : unchecked((uint)value.ToInt32());
}
