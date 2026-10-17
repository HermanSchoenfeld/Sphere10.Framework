// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Native ETW session queries and explicitly selected real-time consumers.</summary>
public class EtwTraceUtil {
	/// <summary>Adds or removes real-time delivery without stopping the session or replacing its file output.</summary>
	public virtual void SetRealTime(EtwTraceSessionInfo expected, bool enabled) {
		var current = VerifySession(expected);
		var structureSize = Marshal.SizeOf<WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES>();
		var nameBytes = System.Text.Encoding.Unicode.GetBytes(current.Name + '\0');
		var size = checked(structureSize + nameBytes.Length);
		using (var allocation = new UnmanagedMemoryScope(size)) {
			var buffer = allocation.Item;
			Marshal.Copy(new byte[size], 0, buffer, size);
			var properties = new WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES {
				Wnode = new WinAPI.ADVAPI32.WNODE_HEADER {
					BufferSize = (uint)size, Guid = current.Guid,
					Flags = WinAPI.ADVAPI32.WNODE_FLAG_TRACED_GUID
				},
				LogFileMode = enabled ? current.LogFileMode | WinAPI.ADVAPI32.EVENT_TRACE_REAL_TIME_MODE
					: current.LogFileMode & ~WinAPI.ADVAPI32.EVENT_TRACE_REAL_TIME_MODE,
				LoggerNameOffset = (uint)structureSize
			};
			Marshal.StructureToPtr(properties, buffer, false);
			Marshal.Copy(nameBytes, 0, IntPtr.Add(buffer, structureSize), nameBytes.Length);
			var error = WinAPI.ADVAPI32.ControlTraceW(current.SessionId, null, buffer, WinAPI.ADVAPI32.EVENT_TRACE_CONTROL_UPDATE);
			if (error != 0)
				throw new Win32Exception((int)error, "Unable to update real-time ETW delivery for the selected session.");
		}
	}

	public virtual Task SetRealTimeAsync(EtwTraceSessionInfo expected, bool enabled, CancellationToken cancellationToken = default)
		=> Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			SetRealTime(expected, enabled);
		}, cancellationToken);

	/// <summary>Consumes detached events until cancellation or the selected session ends. Cancellation closes and drains only this consumer.</summary>
	public virtual Task ConsumeRealTimeAsync(EtwTraceSessionInfo expected, Action<EtwTraceEventRecord> receive, CancellationToken cancellationToken = default, Action started = null) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.ArgumentNotNull(receive, nameof(receive));
		return Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			var session = VerifySession(expected);
			Guard.Ensure((session.LogFileMode & WinAPI.ADVAPI32.EVENT_TRACE_REAL_TIME_MODE) != 0, "The selected ETW session does not have real-time delivery enabled.");
			new EtwTraceReader(session, receive, cancellationToken, started, () => VerifySession(expected)).Run();
		}, cancellationToken);
	}

	protected virtual EtwTraceSessionInfo VerifySession(EtwTraceSessionInfo expected) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		var current = GetSessions().SingleOrDefault(session => session.Name == expected.Name);
		Guard.Ensure(current != null && current.Guid == expected.Guid && current.SessionId == expected.SessionId
			&& string.Equals(current.LogFilePath, expected.LogFilePath, StringComparison.OrdinalIgnoreCase),
			"The selected ETW session ended or was replaced. No foreign session was changed.");
		return current;
	}

	public virtual EtwTraceSessionInfo[] GetSessions() {
		for (var capacity = 64; capacity <= 1024; capacity *= 2) {
			var pointers = new IntPtr[capacity];
			var structureSize = Marshal.SizeOf<WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES>();
			const int stringBytes = 2048;
			var bufferSize = structureSize + 2 * stringBytes;
			using (var allocations = new Disposables()) {
				for (var index = 0; index < pointers.Length; index++) {
					var allocation = new UnmanagedMemoryScope(bufferSize);
					allocations.Add(allocation);
					pointers[index] = allocation.Item;
					Marshal.Copy(new byte[bufferSize], 0, pointers[index], bufferSize);
					var properties = new WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES {
						Wnode = new WinAPI.ADVAPI32.WNODE_HEADER { BufferSize = (uint)bufferSize },
						LoggerNameOffset = (uint)structureSize,
						LogFileNameOffset = (uint)(structureSize + stringBytes)
					};
					Marshal.StructureToPtr(properties, pointers[index], false);
				}
				var error = WinAPI.ADVAPI32.QueryAllTracesW(pointers, (uint)capacity, out var count);
				if (error == 234)
					continue;
				if (error != 0)
					throw new Win32Exception((int)error, "Unable to query accessible ETW sessions.");
				Guard.Ensure(count <= capacity, "Windows returned an invalid ETW session count.");
				var result = new EtwTraceSessionInfo[count];
				for (var index = 0; index < result.Length; index++) {
					var properties = Marshal.PtrToStructure<WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES>(pointers[index]);
					result[index] = new EtwTraceSessionInfo(ReadString(pointers[index], properties.LoggerNameOffset, bufferSize),
						properties.Wnode.Guid, properties.Wnode.HistoricalContext,
						ReadString(pointers[index], properties.LogFileNameOffset, bufferSize), properties.LogFileMode, properties.EventsLost);
				}
				return result;
			}
		}
		throw new InvalidOperationException("ETW session enumeration exceeded its safe capacity.");
	}

	public virtual Task<EtwTraceSessionInfo[]> GetSessionsAsync(CancellationToken cancellationToken = default) => Task.Run(() => {
		cancellationToken.ThrowIfCancellationRequested();
		return GetSessions();
	}, cancellationToken);

	private static string ReadString(IntPtr buffer, uint offset, int bufferSize) {
		if (offset == 0)
			return string.Empty;
		Guard.Ensure(offset < bufferSize && offset % 2 == 0, "Windows returned an invalid ETW string offset.");
		var pointer = IntPtr.Add(buffer, (int)offset);
		var maximum = (bufferSize - (int)offset) / 2;
		for (var length = 0; length < maximum; length++)
			if (Marshal.ReadInt16(pointer, length * 2) == 0)
				return Marshal.PtrToStringUni(pointer, length) ?? string.Empty;
		throw new InvalidOperationException("Windows returned an unterminated ETW session string.");
	}
}
