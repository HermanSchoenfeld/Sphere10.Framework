// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sphere10.Framework.Windows;

/// <summary>Owns one OpenTrace consumer and roots its callback until ProcessTrace has drained.</summary>
internal sealed class EtwTraceReader {
	private readonly EtwTraceSessionInfo _session;
	private readonly Action<EtwTraceEventRecord> _receive;
	private readonly Action _started;
	private readonly Action _verify;
	private readonly CancellationToken _cancellationToken;
	private readonly EtwTracePropertyDecoder _decoder = new();
	private long _handle = -1;
	private ExceptionDispatchInfo _callbackFailure;

	public EtwTraceReader(EtwTraceSessionInfo session, Action<EtwTraceEventRecord> receive, CancellationToken cancellationToken, Action started, Action verify) {
		_session = session;
		_receive = receive;
		_cancellationToken = cancellationToken;
		_started = started;
		_verify = verify;
	}

	public void Run() {
		WinAPI.ADVAPI32.EVENT_RECORD_CALLBACK callback = Receive;
		using var callbackLifetime = Tools.Scope.ExecuteOnDispose(() => GC.KeepAlive(callback));
		using var name = UnmanagedMemoryScope.FromUnicodeString(_session.Name);
		using (Tools.Scope.ExecuteOnDispose(Close)) {
			_cancellationToken.ThrowIfCancellationRequested();
			var logfile = new WinAPI.ADVAPI32.EVENT_TRACE_LOGFILEW {
				LoggerName = name.Item,
				ProcessTraceMode = WinAPI.ADVAPI32.PROCESS_TRACE_MODE_REAL_TIME | WinAPI.ADVAPI32.PROCESS_TRACE_MODE_EVENT_RECORD,
				EventRecordCallback = Marshal.GetFunctionPointerForDelegate(callback)
			};
			var handle = WinAPI.ADVAPI32.OpenTraceW(ref logfile);
			if (handle == ulong.MaxValue)
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to open the selected real-time ETW session.");
			Interlocked.Exchange(ref _handle, unchecked((long)handle));
			using var cancellation = _cancellationToken.Register(Close);
			_cancellationToken.ThrowIfCancellationRequested();
			// OpenTrace selects by name. Recheck identity after opening before delivering any records.
			_verify();
			_started?.Invoke();
			var error = WinAPI.ADVAPI32.ProcessTrace(new[] { handle }, 1, IntPtr.Zero, IntPtr.Zero);
			_callbackFailure?.Throw();
			_cancellationToken.ThrowIfCancellationRequested();
			if (error != 0)
				throw new Win32Exception((int)error, "The selected real-time ETW consumer failed.");
		}
	}

	private void Receive(IntPtr pointer) {
		if (_cancellationToken.IsCancellationRequested || _callbackFailure != null)
			return;
		try {
			var record = Marshal.PtrToStructure<WinAPI.ADVAPI32.EVENT_RECORD>(pointer);
			var data = new byte[record.UserDataLength];
			if (data.Length > 0)
				Marshal.Copy(record.UserData, data, 0, data.Length);
			var header = record.EventHeader;
			_receive(new EtwTraceEventRecord(header.ProviderId, header.EventDescriptor.Id, header.EventDescriptor.Version,
				DateTime.FromFileTimeUtc(header.TimeStamp), header.ProcessId, header.ThreadId, data,
				_decoder.Decode(pointer, header, data)));
		} catch (Exception error) {
			// Managed exceptions must never unwind through the native event callback.
			_callbackFailure = ExceptionDispatchInfo.Capture(error);
			Close();
		}
	}

	private void Close() {
		var handle = Interlocked.Exchange(ref _handle, -1);
		if (handle != -1)
			WinAPI.ADVAPI32.CloseTrace(unchecked((ulong)handle));
	}
}
