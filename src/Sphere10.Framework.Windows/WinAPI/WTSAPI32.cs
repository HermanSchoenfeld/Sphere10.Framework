// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {

	public static class WTSAPI32 {
		public enum WTS_CONNECTSTATE_CLASS { Active, Connected, ConnectQuery, Shadow, Disconnected, Idle, Listen, Reset, Down, Init }

		public enum WTS_INFO_CLASS { WTSInitialProgram, WTSApplicationName, WTSWorkingDirectory, WTSOEMId, WTSSessionId, WTSUserName, WTSWinStationName,
			WTSDomainName, WTSConnectState, WTSClientBuildNumber, WTSClientName, WTSClientDirectory, WTSClientProductId, WTSClientHardwareId, WTSClientAddress,
			WTSClientDisplay, WTSClientProtocolType, WTSIdleTime, WTSLogonTime, WTSIncomingBytes, WTSOutgoingBytes, WTSIncomingFrames, WTSOutgoingFrames,
			WTSClientInfo, WTSSessionInfo }

		[StructLayout(LayoutKind.Sequential)]
		public struct WTS_SESSION_INFO { public uint SessionId; public IntPtr WinStationName; public WTS_CONNECTSTATE_CLASS State; }

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WTSINFO {
			public WTS_CONNECTSTATE_CLASS State;
			public uint SessionId;
			public uint IncomingBytes, OutgoingBytes, IncomingFrames, OutgoingFrames, IncomingCompressedBytes, OutgoingCompressedBytes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string WinStationName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 17)] public string Domain;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)] public string UserName;
			public long ConnectTime, DisconnectTime, LastInputTime, LogonTime, CurrentTime;
		}

		[DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		public static extern IntPtr WTSOpenServerW(string serverName);

		[DllImport("wtsapi32.dll", ExactSpelling = true)]
		public static extern void WTSCloseServer(IntPtr server);

		[DllImport("wtsapi32.dll", ExactSpelling = true)]
		public static extern void WTSFreeMemory(IntPtr memory);

		[DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WTSEnumerateSessionsW(IntPtr server, uint reserved, uint version, out IntPtr sessions, out uint count);

		[DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WTSQuerySessionInformationW(IntPtr server, uint sessionId, WTS_INFO_CLASS infoClass, out IntPtr buffer, out uint byteCount);

		[DllImport("wtsapi32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WTSLogoffSession(IntPtr server, uint sessionId, [MarshalAs(UnmanagedType.Bool)] bool wait);

		[DllImport("wtsapi32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WTSDisconnectSession(IntPtr server, uint sessionId, [MarshalAs(UnmanagedType.Bool)] bool wait);

		[DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WTSSendMessageW(IntPtr server, uint sessionId, string title, uint titleBytes, string message, uint messageBytes,
			uint style, uint timeoutSeconds, out uint response, [MarshalAs(UnmanagedType.Bool)] bool wait);

		[DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WTSStartRemoteControlSessionW(string serverName, uint sessionId, byte hotkey, ushort modifiers);


		public enum WTS_CONFIG_CLASS {
			WTSUserConfigInitialProgram,
			WTSUserConfigWorkingDirectory,
			WTSUserConfigfInheritInitialProgram,
			WTSUserConfigfAllowLogonTerminalServer,
			WTSUserConfigTimeoutSettingsConnections,
			WTSUserConfigTimeoutSettingsDisconnections,
			WTSUserConfigTimeoutSettingsIdle,
			WTSUserConfigfDeviceClientDrives,
			WTSUserConfigfDeviceClientPrinters,
			WTSUserConfigfDeviceClientDefaultPrinter,
			WTSUserConfigBrokenTimeoutSettings,
			WTSUserConfigReconnectSettings,
			WTSUserConfigModemCallbackSettings,
			WTSUserConfigModemCallbackPhoneNumber,
			WTSUserConfigShadowingSettings,
			WTSUserConfigTerminalServerProfilePath,
			WTSUserConfigTerminalServerHomeDir,
			WTSUserConfigTerminalServerHomeDirDrive,
			WTSUserConfigfTerminalServerRemoteHomeDir
		}


		///// TODO


		[DllImport("Wtsapi32.dll", EntryPoint = "WTSQueryUserConfig")]
		public static extern bool WTSQueryUserConfig(
			[MarshalAs(UnmanagedType.LPTStr)] string pServerName,
			[MarshalAs(UnmanagedType.LPTStr)] string pUserName,
			WTS_CONFIG_CLASS WTSConfigClass,
			StringBuilder ppBuffer,
			out uint pBytesReturned);

		[DllImport("Wtsapi32.dll", EntryPoint = "WTSSetUserConfig")]
		public static extern bool WTSSetUserConfig(
			[MarshalAs(UnmanagedType.LPTStr)] string pServerName,
			[MarshalAs(UnmanagedType.LPTStr)] string pUserName,
			WTS_CONFIG_CLASS WTSConfigClass,
			[MarshalAs(UnmanagedType.LPTStr)] string pBuffer,
			uint dataLength);


	}

}

