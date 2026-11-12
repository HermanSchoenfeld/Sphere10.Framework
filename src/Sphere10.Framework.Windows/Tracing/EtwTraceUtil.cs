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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Native ETW session queries and explicitly selected real-time consumers.</summary>
	public static class TraceSessions {
		/// <summary>Decodes a detached SampledProfile observation without starting or configuring a trace session.</summary>
		public static bool TryReadCpuProfileSample(EtwTraceEventRecord record, out EtwCpuProfileSample sample) => EtwCpuProfileDecoder.TryReadSample(record, out sample);

		/// <summary>Decodes detached StackWalk evidence while preserving its original timestamp and availability.</summary>
		public static bool TryReadCpuProfileStack(EtwTraceEventRecord record, out EtwCpuProfileStack stack) => EtwCpuProfileDecoder.TryReadStack(record, out stack);

		/// <summary>Starts one explicitly named manifest-provider session. An existing session is never adopted or replaced.</summary>
		public static EtwTraceSessionInfo StartRealTimeSession(string name, Guid sessionGuid, int bufferSizeKilobytes = 64, int minimumBuffers = 4, int maximumBuffers = 64)
			=> StartRealTimeSessionCore(name, sessionGuid, bufferSizeKilobytes, minimumBuffers, maximumBuffers, false);

		public static Task<EtwTraceSessionInfo> StartRealTimeSessionAsync(string name, Guid sessionGuid, int bufferSizeKilobytes = 64,
			int minimumBuffers = 4, int maximumBuffers = 64, CancellationToken cancellationToken = default) {
			return Task.Run(() => {
				cancellationToken.ThrowIfCancellationRequested();
				return StartRealTimeSession(name, sessionGuid, bufferSizeKilobytes, minimumBuffers, maximumBuffers);
			}, cancellationToken);
		}

		/// <summary>Creates a separately owned multiplexed system logger. It never adopts NT Kernel Logger/CKCL/GlobalLogger or changes sampling intervals.</summary>
		public static EtwTraceSessionInfo StartSystemRealTimeSession(string name, Guid sessionGuid, int bufferSizeKilobytes = 64, int minimumBuffers = 4, int maximumBuffers = 64) {
			Debug.AssertNotMainThread();
			Guard.Argument(!string.Equals(name, "NT Kernel Logger", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(name, "Circular Kernel Context Logger", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(name, "GlobalLogger", StringComparison.OrdinalIgnoreCase), nameof(name), "Use a new private system-logger name; reserved global loggers are never adopted.");
			Guard.Argument(sessionGuid != new Guid("9e814aad-3204-11d2-9a82-006008a86939"), nameof(sessionGuid), "A private system logger must have its own new GUID rather than SystemTraceControlGuid.");
			return StartRealTimeSessionCore(name, sessionGuid, bufferSizeKilobytes, minimumBuffers, maximumBuffers, true);
		}

		public static Task<EtwTraceSessionInfo> StartSystemRealTimeSessionAsync(string name, Guid sessionGuid, int bufferSizeKilobytes = 64,
			int minimumBuffers = 4, int maximumBuffers = 64, CancellationToken cancellationToken = default) {
			return Task.Run(() => {
				cancellationToken.ThrowIfCancellationRequested();
				return StartSystemRealTimeSession(name, sessionGuid, bufferSizeKilobytes, minimumBuffers, maximumBuffers);
			}, cancellationToken);
		}

		/// <summary>Replaces kernel stack-event selection only on the exact caller-owned system logger. An empty array disables its stack tracing.</summary>
		public static void SetStackTracing(EtwTraceSessionInfo expected, EtwStackEvent[] events) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNull(events, nameof(events));
			Guard.Argument(events.Length <= 256 && events.All(item => item != null && item.ProviderId != Guid.Empty), nameof(events), "Select at most 256 valid classic event classes/types.");
			var current = VerifySession(expected);
			Guard.Ensure((current.LogFileMode & WinAPI.ADVAPI32.EVENT_TRACE_SYSTEM_LOGGER_MODE) != 0, "Kernel stack tracing requires the exact owned system-logger session.");
			var selected = events.Distinct().ToArray();
			var itemSize = Marshal.SizeOf<WinAPI.ADVAPI32.CLASSIC_EVENT_ID>();
			using var buffer = new UnmanagedMemoryScope(Math.Max(1, checked(itemSize * selected.Length)));
			for (var index = 0; index < selected.Length; index++) {
				var item = new WinAPI.ADVAPI32.CLASSIC_EVENT_ID { EventGuid = selected[index].ProviderId, Type = selected[index].Type, Reserved = new byte[7] };
				Marshal.StructureToPtr(item, IntPtr.Add(buffer.Item, index * itemSize), false);
			}
			var error = WinAPI.ADVAPI32.TraceSetInformation(current.SessionId, 3, selected.Length == 0 ? IntPtr.Zero : buffer.Item, checked((uint)(selected.Length * itemSize)));
			if (error != 0)
				throw new Win32Exception((int)error, "Unable to configure kernel stack events on the exact owned system logger.");
		}

		public static Task SetStackTracingAsync(EtwTraceSessionInfo expected, EtwStackEvent[] events, CancellationToken cancellationToken = default) {
			Guard.ArgumentNotNull(events, nameof(events));
			var selected = events.ToArray();
			return Task.Run(() => {
				cancellationToken.ThrowIfCancellationRequested();
				SetStackTracing(expected, selected);
			}, cancellationToken);
		}

		/// <summary>Enables a manifest provider only on the exact session supplied by its owner.</summary>
		public static void EnableProvider(EtwTraceSessionInfo expected, Guid providerId, ulong keywords, byte level = 5, uint timeoutMilliseconds = 1000) {
			Debug.AssertNotMainThread();
			var session = VerifySession(expected);
			var error = WinAPI.ADVAPI32.EnableTraceEx2(session.SessionId, ref providerId, WinAPI.ADVAPI32.EVENT_CONTROL_CODE_ENABLE_PROVIDER,
				level, keywords, 0, timeoutMilliseconds, IntPtr.Zero);
			if (error != 0)
				throw new Win32Exception((int)error, "Unable to enable the requested ETW provider on the owned session.");
		}

		public static Task EnableProviderAsync(EtwTraceSessionInfo expected, Guid providerId, ulong keywords, byte level = 5,
			uint timeoutMilliseconds = 1000, CancellationToken cancellationToken = default) {
			return Task.Run(() => {
				cancellationToken.ThrowIfCancellationRequested();
				EnableProvider(expected, providerId, keywords, level, timeoutMilliseconds);
			}, cancellationToken);
		}

		/// <summary>Opt-in stack capture uses ENABLE_TRACE_PARAMETERS on this exact owned session; it changes no global kernel logger or registry setting.</summary>
		public static void EnableProvider(EtwTraceSessionInfo expected, Guid providerId, EtwTraceProviderOptions options) {
			Guard.ArgumentNotNull(options, nameof(options));
			if (!options.CaptureStackTrace) { EnableProvider(expected, providerId, options.Keywords, options.Level, options.TimeoutMilliseconds); return; }
			Debug.AssertNotMainThread();
			var session = VerifySession(expected);
			var parameters = new WinAPI.ADVAPI32.ENABLE_TRACE_PARAMETERS { Version = WinAPI.ADVAPI32.ENABLE_TRACE_PARAMETERS_VERSION_2,
				EnableProperty = WinAPI.ADVAPI32.EVENT_ENABLE_PROPERTY_STACK_TRACE, SourceId = expected.Guid };
			using var memory = new UnmanagedMemoryScope(Marshal.SizeOf<WinAPI.ADVAPI32.ENABLE_TRACE_PARAMETERS>());
			Marshal.StructureToPtr(parameters, memory.Item, false);
			var error = WinAPI.ADVAPI32.EnableTraceEx2(session.SessionId, ref providerId, WinAPI.ADVAPI32.EVENT_CONTROL_CODE_ENABLE_PROVIDER,
				options.Level, options.Keywords, 0, options.TimeoutMilliseconds, memory.Item);
			if (error != 0)
				throw new Win32Exception((int)error, "The requested owned-provider stack capture could not be enabled.");
		}

		public static Task EnableProviderAsync(EtwTraceSessionInfo expected, Guid providerId, EtwTraceProviderOptions options, CancellationToken cancellationToken = default) {
			Guard.ArgumentNotNull(options, nameof(options));
			var selected = options with { };
			return Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); EnableProvider(expected, providerId, selected); }, cancellationToken);
		}

		/// <summary>Releases one provider admission only on the exact caller-owned session, without stopping its other providers.</summary>
		public static void DisableProvider(EtwTraceSessionInfo expected, Guid providerId, uint timeoutMilliseconds = 1000) {
			Debug.AssertNotMainThread();
			var session = VerifySession(expected);
			var error = WinAPI.ADVAPI32.EnableTraceEx2(session.SessionId, ref providerId, WinAPI.ADVAPI32.EVENT_CONTROL_CODE_DISABLE_PROVIDER,
				0, 0, 0, timeoutMilliseconds, IntPtr.Zero);
			if (error != 0)
				throw new Win32Exception((int)error, "Unable to release the requested provider from the owned ETW session.");
		}

		public static Task DisableProviderAsync(EtwTraceSessionInfo expected, Guid providerId, uint timeoutMilliseconds = 1000, CancellationToken cancellationToken = default)
			=> Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); DisableProvider(expected, providerId, timeoutMilliseconds); }, cancellationToken);

		/// <summary>Requests supported provider rundown without changing any other session.</summary>
		public static void CaptureProviderState(EtwTraceSessionInfo expected, Guid providerId, ulong keywords, byte level = 5) {
			Debug.AssertNotMainThread();
			var session = VerifySession(expected);
			var error = WinAPI.ADVAPI32.EnableTraceEx2(session.SessionId, ref providerId, WinAPI.ADVAPI32.EVENT_CONTROL_CODE_CAPTURE_STATE,
				level, keywords, 0, 1000, IntPtr.Zero);
			if (error != 0)
				throw new Win32Exception((int)error, "The ETW provider did not accept the requested rundown.");
		}

		/// <summary>Returns the verified session's native counters without resetting or stopping it.</summary>
		public static EtwTraceSessionInfo GetSession(EtwTraceSessionInfo expected) {
			Debug.AssertNotMainThread();
			return VerifySession(expected);
		}

		public static Task<EtwTraceSessionInfo> GetSessionAsync(EtwTraceSessionInfo expected, CancellationToken cancellationToken = default) {
			return Task.Run(() => {
				cancellationToken.ThrowIfCancellationRequested();
				return GetSession(expected);
			}, cancellationToken);
		}

		/// <summary>Stops only a still-matching session. Closing a consumer is a different operation.</summary>
		public static void StopSession(EtwTraceSessionInfo expected) {
			Debug.AssertNotMainThread();
			var current = VerifySession(expected);
			var error = StopSessionCore(current);
			if (error != 0)
				throw new Win32Exception((int)error, "Unable to stop the owned ETW session.");
		}

		/// <summary>Stops a matching owned session, or returns false when that resource already ended or was replaced.</summary>
		public static bool StopSessionIfCurrent(EtwTraceSessionInfo expected) {
			Debug.AssertNotMainThread();
			var current = FindSession(expected);
			if (current == null)
				return false;
			var error = StopSessionCore(current);
			if (error == 0)
				return true;
			if (FindSession(expected) == null)
				return false;
			throw new Win32Exception((int)error, "Unable to stop the still-current owned ETW session.");
		}

		public static Task<bool> StopSessionIfCurrentAsync(EtwTraceSessionInfo expected, CancellationToken cancellationToken = default) {
			return Task.Run(() => {
				cancellationToken.ThrowIfCancellationRequested();
				return StopSessionIfCurrent(expected);
			}, cancellationToken);
		}

		public static Task StopSessionAsync(EtwTraceSessionInfo expected, CancellationToken cancellationToken = default) {
			return Task.Run(() => {
				cancellationToken.ThrowIfCancellationRequested();
				StopSession(expected);
			}, cancellationToken);
		}

		/// <summary>Adds or removes real-time delivery without stopping the session or replacing its file output.</summary>
		public static void SetRealTime(EtwTraceSessionInfo expected, bool enabled) {
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

		public static Task SetRealTimeAsync(EtwTraceSessionInfo expected, bool enabled, CancellationToken cancellationToken = default)
			=> Task.Run(() => {
				cancellationToken.ThrowIfCancellationRequested();
				SetRealTime(expected, enabled);
			}, cancellationToken);

		/// <summary>Consumes detached events until cancellation or the selected session ends. Cancellation closes and drains only this consumer.</summary>
		public static Task ConsumeRealTimeAsync(EtwTraceSessionInfo expected, Action<EtwTraceEventRecord> receive, CancellationToken cancellationToken = default, Action started = null) {
			Guard.ArgumentNotNull(expected, nameof(expected));
			Guard.ArgumentNotNull(receive, nameof(receive));
			return Task.Run(() => {
				cancellationToken.ThrowIfCancellationRequested();
				var session = VerifySession(expected);
				Guard.Ensure((session.LogFileMode & WinAPI.ADVAPI32.EVENT_TRACE_REAL_TIME_MODE) != 0, "The selected ETW session does not have real-time delivery enabled.");
				new EtwTraceReader(session, receive, cancellationToken, started, () => VerifySession(expected)).Run();
			}, cancellationToken);
		}

		/// <summary>Resolves only the exact name, GUID, native handle and output identity supplied by the resource owner.</summary>
		public static EtwTraceSessionInfo FindSession(EtwTraceSessionInfo expected) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNull(expected, nameof(expected));
			return GetSessions().SingleOrDefault(session => session.Name == expected.Name && session.Guid == expected.Guid
				&& session.SessionId == expected.SessionId && string.Equals(session.LogFilePath, expected.LogFilePath, StringComparison.OrdinalIgnoreCase));
		}

		public static EtwTraceSessionInfo[] GetSessions() {
			for (var capacity = 64; capacity <= 1024; capacity *= 2) {
				var pointers = new IntPtr[capacity];
				var structureSize = Marshal.SizeOf<WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES>();
				const int stringBytes = 2048;
				var bufferSize = structureSize + 2 * stringBytes;
				var allocations = new UnmanagedMemoryScope[capacity];
				try {
					for (var index = 0; index < pointers.Length; index++) {
						allocations[index] = new UnmanagedMemoryScope(bufferSize);
						pointers[index] = allocations[index].Item;
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
							ReadString(pointers[index], properties.LogFileNameOffset, bufferSize), properties.LogFileMode, properties.EventsLost) {
							RealTimeBuffersLost = properties.RealTimeBuffersLost, LogBuffersLost = properties.LogBuffersLost, BuffersWritten = properties.BuffersWritten
						};
					}
					return result;
				} finally {
					for (var index = 0; index < allocations.Length; index++)
						allocations[index].Dispose();
				}
			}
			throw new InvalidOperationException("ETW session enumeration exceeded its safe capacity.");
		}

		public static Task<EtwTraceSessionInfo[]> GetSessionsAsync(CancellationToken cancellationToken = default) => Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			return GetSessions();
		}, cancellationToken);

		private static EtwTraceSessionInfo StartRealTimeSessionCore(string name, Guid sessionGuid, int bufferSizeKilobytes, int minimumBuffers, int maximumBuffers, bool systemLogger) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNullOrEmpty(name, nameof(name));
			Guard.Argument(name.Length < 1024 && !name.Contains('\0'), nameof(name), "An ETW session requires a valid, descriptive name.");
			Guard.Argument(sessionGuid != Guid.Empty, nameof(sessionGuid), "An owned session requires a nonempty identity.");
			Guard.ArgumentInRange(bufferSizeKilobytes, 4, 1024, nameof(bufferSizeKilobytes));
			Guard.ArgumentInRange(minimumBuffers, 2, 1024, nameof(minimumBuffers));
			Guard.ArgumentInRange(maximumBuffers, minimumBuffers, 1024, nameof(maximumBuffers));
			var structureSize = Marshal.SizeOf<WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES>();
			var nameBytes = System.Text.Encoding.Unicode.GetBytes(name + '\0');
			var size = checked(structureSize + nameBytes.Length);
			using var allocation = new UnmanagedMemoryScope(size);
			Marshal.Copy(new byte[size], 0, allocation.Item, size);
			var properties = new WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES {
				Wnode = new WinAPI.ADVAPI32.WNODE_HEADER { BufferSize = (uint)size, Guid = sessionGuid, ClientContext = 2, Flags = WinAPI.ADVAPI32.WNODE_FLAG_TRACED_GUID },
				BufferSize = (uint)bufferSizeKilobytes, MinimumBuffers = (uint)minimumBuffers, MaximumBuffers = (uint)maximumBuffers,
				LogFileMode = WinAPI.ADVAPI32.EVENT_TRACE_REAL_TIME_MODE | (systemLogger ? WinAPI.ADVAPI32.EVENT_TRACE_SYSTEM_LOGGER_MODE : 0), FlushTimer = 1, LoggerNameOffset = (uint)structureSize
			};
			Marshal.StructureToPtr(properties, allocation.Item, false);
			Marshal.Copy(nameBytes, 0, IntPtr.Add(allocation.Item, structureSize), nameBytes.Length);
			var error = WinAPI.ADVAPI32.StartTraceW(out var handle, name, allocation.Item);
			if (error != 0)
				throw new Win32Exception((int)error, "Unable to start the requested ETW session. Existing sessions were left unchanged.");
			return new EtwTraceSessionInfo(name, sessionGuid, handle, string.Empty, properties.LogFileMode, 0);
		}

		private static uint StopSessionCore(EtwTraceSessionInfo current) {
			var structureSize = Marshal.SizeOf<WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES>();
			var size = checked(structureSize + (current.Name.Length + 1) * 2);
			using var allocation = new UnmanagedMemoryScope(size);
			Marshal.Copy(new byte[size], 0, allocation.Item, size);
			var properties = new WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES {
				Wnode = new WinAPI.ADVAPI32.WNODE_HEADER { BufferSize = (uint)size, Guid = current.Guid },
				LoggerNameOffset = (uint)structureSize
			};
			Marshal.StructureToPtr(properties, allocation.Item, false);
			return WinAPI.ADVAPI32.ControlTraceW(current.SessionId, null, allocation.Item, WinAPI.ADVAPI32.EVENT_TRACE_CONTROL_STOP);
		}

		private static EtwTraceSessionInfo VerifySession(EtwTraceSessionInfo expected) {
			var current = FindSession(expected);
			Guard.Ensure(current != null,
				"The selected ETW session ended or was replaced. No foreign session was changed.");
			return current;
		}

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
}
