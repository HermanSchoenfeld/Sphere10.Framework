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
using Microsoft.Diagnostics.Runtime;

namespace Sphere10.Framework.Windows;

/// <summary>One Windows PSS VA clone and its captured thread contexts. No reads fall back to the changing live process.</summary>
internal sealed class WindowsDiagnosticInfo : IDisposable {
	private IntPtr _snapshot;
	private DataTarget _cloneTarget;
	private bool _disposed;

	private WindowsDiagnosticInfo(ProcessIdentity identity, DataTargetOptions options, CancellationToken cancellationToken) {
		Identity = identity;
		CapturedAtUtc = DateTime.UtcNow;
		try {
			ProcessCommandService.ExecuteNative(identity, 0x80 | 0x400 | 0x10 | 0x40, process => {
				Guard.Ensure(Environment.Is64BitProcess && RuntimeInformation.ProcessArchitecture == Architecture.X64, "PSS diagnostic capture currently requires an x64 host and x64 target.");
				if (!WinAPI.KERNEL32.IsWow64Process(process, out var wow64))
					throw new Win32Exception(Marshal.GetLastPInvokeError());
				Guard.Ensure(!wow64, "Cross-architecture PSS capture is not supported; use an x64 target or open a supported dump.");
				Check(WinAPI.KERNEL32.PssCaptureSnapshot(process, 0x80000000 | 0x1 | 0x80 | 0x100, 0x10001f, out _snapshot), "Capture process snapshot");
				return true;
			}, cancellationToken.ThrowIfCancellationRequested);
			cancellationToken.ThrowIfCancellationRequested();
			Check(WinAPI.KERNEL32.PssQuerySnapshot(_snapshot, 1, out var clone, (uint)IntPtr.Size), "Read snapshot clone");
			CloneHandle = clone;
			var contexts = ReadContexts(cancellationToken);
			var cloneId = WinAPI.KERNEL32.GetSnapshotProcessId(clone);
			Guard.Ensure(cloneId != 0, "The process snapshot has no readable VA clone.");
			// A VA clone does not execute. Passive attachment here reads frozen pages, never the live source.
			_cloneTarget = DataTarget.AttachToProcess(checked((int)cloneId), false, options);
			Target = new DataTarget(new SnapshotReader(_cloneTarget.DataReader, contexts), options);
		} catch {
			Dispose();
			throw;
		}
	}

	public ProcessIdentity Identity { get; }

	public DateTime CapturedAtUtc { get; }

	public IntPtr CloneHandle { get; }

	public DataTarget Target { get; }

	public Dictionary<uint, DateTime> ThreadCreationTimes { get; } = new();

	public static WindowsDiagnosticInfo Capture(ProcessIdentity identity, DataTargetOptions options, CancellationToken cancellationToken) {
		Debug.AssertNotMainThread();
		return new WindowsDiagnosticInfo(identity, options, cancellationToken);
	}

	public void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		try {
			Target?.Dispose();
		} finally {
			try {
				_cloneTarget?.Dispose();
			} finally {
				if (_snapshot != IntPtr.Zero) {
					// Threadless clones can otherwise be kept alive by another inventory's query handles indefinitely.
					// This handle is the owned clone from PssQuerySnapshot, never the source process.
					if (CloneHandle != IntPtr.Zero)
						WinAPI.KERNEL32.TerminateSnapshotClone(CloneHandle, 0);
					using var owner = Process.GetCurrentProcess();
					var error = WinAPI.KERNEL32.PssFreeSnapshot(owner.Handle, _snapshot);
					_snapshot = IntPtr.Zero;
					Check(error, "Release process snapshot");
				}
			}
		}
	}

	private Dictionary<uint, byte[]> ReadContexts(CancellationToken cancellationToken) {
		Check(WinAPI.KERNEL32.PssWalkMarkerCreate(IntPtr.Zero, out var marker), "Create snapshot thread cursor");
		using var release = Tools.Scope.ExecuteOnDispose(() => WinAPI.KERNEL32.PssWalkMarkerFree(marker));
		var contexts = new Dictionary<uint, byte[]>();
		var visited = 0;
		while (true) {
			cancellationToken.ThrowIfCancellationRequested();
			var error = WinAPI.KERNEL32.PssWalkSnapshot(_snapshot, 3, marker, out var entry, (uint)Marshal.SizeOf<WinAPI.KERNEL32.PSS_THREAD_ENTRY>());
			if (error == 259)
				break;
			Check(error, "Read snapshot thread context");
			Guard.Ensure(++visited <= 20000, "The snapshot exceeds the thread capture limit (20,000).");
			if (entry.ContextRecord == IntPtr.Zero || entry.SizeOfContextRecord == 0)
				continue;
			var bytes = new byte[entry.SizeOfContextRecord];
			Marshal.Copy(entry.ContextRecord, bytes, 0, bytes.Length);
			contexts[entry.ThreadId] = bytes;
			var ticks = ((long)entry.CreateTime.dwHighDateTime << 32) | (uint)entry.CreateTime.dwLowDateTime;
			ThreadCreationTimes[entry.ThreadId] = DateTime.FromFileTimeUtc(ticks);
		}

		return contexts;
	}

	private static void Check(uint error, string operation) {
		if (error != 0)
			throw new Win32Exception(checked((int)error), operation);
	}

	#region Inner Types

	private sealed class SnapshotReader(IDataReader reader, Dictionary<uint, byte[]> contexts) : IDataReader {
		public string DisplayName => reader.DisplayName;

		public bool IsThreadSafe => false;

		public OSPlatform TargetPlatform => reader.TargetPlatform;

		public Architecture Architecture => reader.Architecture;

		public int ProcessId => reader.ProcessId;

		public int PointerSize => reader.PointerSize;

		public IEnumerable<ModuleInfo> EnumerateModules() => reader.EnumerateModules();

		public int Read(ulong address, Span<byte> buffer) => reader.Read(address, buffer);

		public bool Read<T>(ulong address, out T value)
			where T : unmanaged {
			return reader.Read(address, out value);
		}

		public T Read<T>(ulong address)
			where T : unmanaged {
			return reader.Read<T>(address);
		}

		public bool ReadPointer(ulong address, out ulong value) => reader.ReadPointer(address, out value);

		public ulong ReadPointer(ulong address) => reader.ReadPointer(address);

		public void FlushCachedData() {
		}

		public bool GetThreadContext(uint threadId, uint contextFlags, Span<byte> destination) {
			if (!contexts.TryGetValue(threadId, out var bytes) || bytes.Length > destination.Length)
				return false;
			bytes.CopyTo(destination);
			return true;
		}
	}

	#endregion
}
