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

/// <summary>Queries and operates on Windows interactive/RDS sessions. Native permissions and remote-control consent remain Windows policy.</summary>
public class SessionUtil {
	public virtual WindowsSessionInfo[] GetSessions(string serverName = null) => GetSessionsCore(serverName, null);

	public virtual Task<WindowsSessionInfo[]> GetSessionsAsync(string serverName = null, CancellationToken cancellationToken = default)
		=> Task.Run(() => GetSessionsCore(serverName, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public virtual WindowsSessionInfo GetSession(uint sessionId, string serverName = null) {
		using var server = new WtsServerScope(serverName);
		return Query(server.Handle, sessionId, NormalizeServer(serverName));
	}

	public virtual Task<WindowsSessionInfo> GetSessionAsync(uint sessionId, string serverName = null, CancellationToken cancellationToken = default)
		=> Task.Run(() => GetSession(sessionId, serverName), cancellationToken);

	public virtual uint CurrentSessionId {
		get {
			using var process = Process.GetCurrentProcess();
			return checked((uint)process.SessionId);
		}
	}

	public virtual bool IsCurrentSessionRemote => GetSession(CurrentSessionId).ClientProtocolType == 2;

	public virtual Task<uint> GetCurrentSessionIdAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => CurrentSessionId, cancellationToken);

	public virtual Task<bool> IsCurrentSessionRemoteAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => IsCurrentSessionRemote, cancellationToken);

	public virtual void Logoff(WindowsSessionInfo expected) => Change(expected, (server, id) => WinAPI.WTSAPI32.WTSLogoffSession(server, id, false));

	public virtual Task LogoffAsync(WindowsSessionInfo expected, CancellationToken cancellationToken = default) => Task.Run(() => Logoff(expected), cancellationToken);

	public virtual void Disconnect(WindowsSessionInfo expected) => Change(expected, (server, id) => WinAPI.WTSAPI32.WTSDisconnectSession(server, id, false));

	public virtual Task DisconnectAsync(WindowsSessionInfo expected, CancellationToken cancellationToken = default) => Task.Run(() => Disconnect(expected), cancellationToken);

	/// <summary>Requests an informational message without waiting for its user's response.</summary>
	public virtual void SendMessage(WindowsSessionInfo expected, string title, string message, uint timeoutSeconds = 60) {
		Guard.ArgumentNotNull(title, nameof(title));
		Guard.ArgumentNotNull(message, nameof(message));
		Guard.Argument(title.Length <= 1024 && message.Length <= 32768 && !title.Contains('\0') && !message.Contains('\0'), nameof(message), "Use a bounded title/message without embedded null characters.");
		Change(expected, (server, id) => WinAPI.WTSAPI32.WTSSendMessageW(server, id, title, checked((uint)title.Length * 2), message,
			checked((uint)message.Length * 2), 0x40, timeoutSeconds, out _, false));
	}

	public virtual Task SendMessageAsync(WindowsSessionInfo expected, string title, string message, uint timeoutSeconds = 60, CancellationToken cancellationToken = default)
		=> Task.Run(() => SendMessage(expected, title, message, timeoutSeconds), cancellationToken);

	public virtual void StartRemoteControl(WindowsSessionInfo expected, byte stopHotkey = 0x7B, ushort stopModifiers = 2) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.Argument(stopHotkey != 0 && stopModifiers is >= 1 and <= 7, nameof(stopHotkey), "Select a stop key with Shift, Control and/or Alt modifiers.");
		Guard.Ensure(IsCurrentSessionRemote, "Windows requires remote control to be started from an existing Remote Desktop session.");
		Guard.Ensure(expected.SessionId != CurrentSessionId || !string.Equals(expected.ServerName, Environment.MachineName, StringComparison.OrdinalIgnoreCase), "A session cannot remote-control itself.");
		Change(expected, (_, id) => WinAPI.WTSAPI32.WTSStartRemoteControlSessionW(expected.ServerName, id, stopHotkey, stopModifiers));
	}

	public virtual Task StartRemoteControlAsync(WindowsSessionInfo expected, byte stopHotkey = 0x7B, ushort stopModifiers = 2, CancellationToken cancellationToken = default)
		=> Task.Run(() => StartRemoteControl(expected, stopHotkey, stopModifiers), cancellationToken);

	private static WindowsSessionInfo[] GetSessionsCore(string serverName, Action checkpoint) {
		checkpoint?.Invoke();
		using var server = new WtsServerScope(serverName);
		var success = WinAPI.WTSAPI32.WTSEnumerateSessionsW(server.Handle, 0, 1, out var pointer, out var count);
		var error = Marshal.GetLastPInvokeError();
		using var memory = new WtsMemoryScope(pointer);
		if (!success)
			throw new Win32Exception(error);
		Guard.Ensure(count <= 100000 && (count == 0 || pointer != IntPtr.Zero), "Windows returned an invalid session enumeration.");
		var size = Marshal.SizeOf<WinAPI.WTSAPI32.WTS_SESSION_INFO>();
		var result = new List<WindowsSessionInfo>();
		for (var index = 0; index < count; index++) {
			checkpoint?.Invoke();
			var native = Marshal.PtrToStructure<WinAPI.WTSAPI32.WTS_SESSION_INFO>(IntPtr.Add(pointer, checked(index * size)));
			var row = Query(server.Handle, native.SessionId, NormalizeServer(serverName));
			result.Add(row with { StationName = Marshal.PtrToStringUni(native.WinStationName) ?? string.Empty, State = native.State });
		}
		return result.ToArray();
	}

	private static WindowsSessionInfo Query(IntPtr server, uint sessionId, string serverName) {
		var success = WinAPI.WTSAPI32.WTSQuerySessionInformationW(server, sessionId, WinAPI.WTSAPI32.WTS_INFO_CLASS.WTSSessionInfo, out var pointer, out var bytes);
		var error = Marshal.GetLastPInvokeError();
		using var memory = new WtsMemoryScope(pointer);
		var row = new WindowsSessionInfo { SessionId = sessionId, ServerName = serverName, ReadErrorCode = success ? 0 : error };
		if (success && pointer != IntPtr.Zero && bytes >= Marshal.SizeOf<WinAPI.WTSAPI32.WTSINFO>()) {
			var info = Marshal.PtrToStructure<WinAPI.WTSAPI32.WTSINFO>(pointer);
			row = ProjectSessionInformation(sessionId, serverName, info);
		} else if (success) {
			row = row with { ReadErrorCode = 13 };
		}
		if (row.ReadErrorCode != 0) {
			row = row with { UserName = QueryText(server, sessionId, WinAPI.WTSAPI32.WTS_INFO_CLASS.WTSUserName),
				Domain = QueryText(server, sessionId, WinAPI.WTSAPI32.WTS_INFO_CLASS.WTSDomainName) };
		}
		row = row with { ClientName = QueryText(server, sessionId, WinAPI.WTSAPI32.WTS_INFO_CLASS.WTSClientName) };
		var protocol = WinAPI.WTSAPI32.WTSQuerySessionInformationW(server, sessionId, WinAPI.WTSAPI32.WTS_INFO_CLASS.WTSClientProtocolType, out var protocolPointer, out var protocolBytes);
		using var protocolMemory = new WtsMemoryScope(protocolPointer);
		return row with { ClientProtocolType = protocol && protocolPointer != IntPtr.Zero && protocolBytes >= 2 ? unchecked((ushort)Marshal.ReadInt16(protocolPointer)) : null };
	}

	internal static WindowsSessionInfo ProjectSessionInformation(uint requestedId, string serverName, WinAPI.WTSAPI32.WTSINFO info) {
		// Windows can return a zero SessionId in successful WTSSessionInfo responses for listener stations.
		// Such data is display-only and must never provide the identity used by a management operation.
		if (info.SessionId != requestedId || info.State == WinAPI.WTSAPI32.WTS_CONNECTSTATE_CLASS.Listen)
			return new WindowsSessionInfo { SessionId = requestedId, ServerName = serverName, State = info.State, ReadErrorCode = 13 };
		return new WindowsSessionInfo { SessionId = requestedId, ServerName = serverName, State = info.State, StationName = info.WinStationName,
			UserName = info.UserName, Domain = info.Domain, LogonTimeUtc = Time(info.LogonTime) };
	}

	private static string QueryText(IntPtr server, uint sessionId, WinAPI.WTSAPI32.WTS_INFO_CLASS info) {
		var success = WinAPI.WTSAPI32.WTSQuerySessionInformationW(server, sessionId, info, out var pointer, out var bytes);
		using var memory = new WtsMemoryScope(pointer);
		return success && pointer != IntPtr.Zero && bytes >= 2 ? (Marshal.PtrToStringUni(pointer, checked((int)bytes / 2)) ?? string.Empty).TrimEnd('\0') : string.Empty;
	}

	private static void Change(WindowsSessionInfo expected, Func<IntPtr, uint, bool> action) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.Ensure(expected.IsVerified, "The session's logon identity is unavailable; refresh and select a verified user session.");
		using var server = new WtsServerScope(expected.ServerName);
		var current = Query(server.Handle, expected.SessionId, NormalizeServer(expected.ServerName));
		Guard.Ensure(expected.MatchesIdentity(current), "The session logged off or its ID was reused. Refresh and select the current session.");
		if (!action(server.Handle, expected.SessionId))
			throw new Win32Exception(Marshal.GetLastPInvokeError());
	}

	private static string NormalizeServer(string serverName) => string.IsNullOrEmpty(serverName) || serverName == "." ? Environment.MachineName : serverName;

	private static DateTime? Time(long fileTime) {
		if (fileTime <= 0)
			return null;
		DateTime? value = null;
		Tools.Exceptions.ExecuteCapturingException<ArgumentOutOfRangeException>(() => value = DateTime.FromFileTimeUtc(fileTime), out _);
		return value;
	}
}
