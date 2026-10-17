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

using Microsoft.Win32.SafeHandles;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {

	public static partial class ADVAPI32 {
		[DllImport("advapi32.dll", EntryPoint = "OpenProcessToken", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenProcessToken(KERNEL32.KernelHandle process, uint access, out SafeAccessTokenHandle token);

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetTokenInformation(SafeAccessTokenHandle token, int informationClass, IntPtr information, uint length, out uint returnLength);

		[DllImport("advapi32.dll", EntryPoint = "LookupPrivilegeNameW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LookupPrivilegeName(string systemName, ref LUID luid, StringBuilder name, ref uint length);

		[StructLayout(LayoutKind.Sequential)]
		public struct SID_AND_ATTRIBUTES {
			public IntPtr Sid;
			public uint Attributes;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct TOKEN_GROUPS_HEADER {
			public uint Count;
			public SID_AND_ATTRIBUTES FirstGroup;
		}

		public const uint EVENT_TRACE_REAL_TIME_MODE = 0x00000100;
		public const uint PROCESS_TRACE_MODE_REAL_TIME = 0x00000100;
		public const uint PROCESS_TRACE_MODE_EVENT_RECORD = 0x10000000;
		public const uint EVENT_TRACE_CONTROL_UPDATE = 2;
		public const uint WNODE_FLAG_TRACED_GUID = 0x00020000;

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		public delegate void EVENT_RECORD_CALLBACK(IntPtr record);

		[StructLayout(LayoutKind.Sequential)]
		public struct EVENT_DESCRIPTOR {
			public ushort Id;
			public byte Version;
			public byte Channel;
			public byte Level;
			public byte Opcode;
			public ushort Task;
			public ulong Keyword;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct EVENT_HEADER {
			public ushort Size;
			public ushort HeaderType;
			public ushort Flags;
			public ushort EventProperty;
			public uint ThreadId;
			public uint ProcessId;
			public long TimeStamp;
			public Guid ProviderId;
			public EVENT_DESCRIPTOR EventDescriptor;
			public ulong ProcessorTime;
			public Guid ActivityId;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct EVENT_RECORD {
			public EVENT_HEADER EventHeader;
			public uint BufferContext;
			public ushort ExtendedDataCount;
			public ushort UserDataLength;
			public IntPtr ExtendedData;
			public IntPtr UserData;
			public IntPtr UserContext;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct EVENT_TRACE_HEADER {
			public ushort Size;
			public ushort FieldTypeFlags;
			public uint Version;
			public uint ThreadId;
			public uint ProcessId;
			public long TimeStamp;
			public Guid Guid;
			public ulong ProcessorTime;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct EVENT_TRACE {
			public EVENT_TRACE_HEADER Header;
			public uint InstanceId;
			public uint ParentInstanceId;
			public Guid ParentGuid;
			public IntPtr MofData;
			public uint MofLength;
			public uint ClientContext;
		}

		[StructLayout(LayoutKind.Sequential, Size = 172)]
		public struct TRACE_TIME_ZONE_INFORMATION {
			public int Bias;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct TRACE_LOGFILE_HEADER {
			public uint BufferSize;
			public uint Version;
			public uint ProviderVersion;
			public uint NumberOfProcessors;
			public long EndTime;
			public uint TimerResolution;
			public uint MaximumFileSize;
			public uint LogFileMode;
			public uint BuffersWritten;
			public Guid LogInstanceGuid;
			public IntPtr LoggerName;
			public IntPtr LogFileName;
			public TRACE_TIME_ZONE_INFORMATION TimeZone;
			public long BootTime;
			public long PerfFreq;
			public long StartTime;
			public uint ReservedFlags;
			public uint BuffersLost;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct EVENT_TRACE_LOGFILEW {
			public IntPtr LogFileName;
			public IntPtr LoggerName;
			public long CurrentTime;
			public uint BuffersRead;
			public uint ProcessTraceMode;
			public EVENT_TRACE CurrentEvent;
			public TRACE_LOGFILE_HEADER LogfileHeader;
			public IntPtr BufferCallback;
			public uint BufferSize;
			public uint Filled;
			public uint EventsLost;
			public IntPtr EventRecordCallback;
			public uint IsKernelTrace;
			public IntPtr Context;
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern uint ControlTraceW(ulong sessionHandle, string sessionName, IntPtr properties, uint controlCode);

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern ulong OpenTraceW(ref EVENT_TRACE_LOGFILEW logfile);

		[DllImport("advapi32.dll", ExactSpelling = true)]
		public static extern uint ProcessTrace([In] ulong[] handles, uint count, IntPtr startTime, IntPtr endTime);

		[DllImport("advapi32.dll", ExactSpelling = true)]
		public static extern uint CloseTrace(ulong traceHandle);

		[StructLayout(LayoutKind.Sequential)]
		public struct WNODE_HEADER {
			public uint BufferSize;
			public uint ProviderId;
			public ulong HistoricalContext;
			public long TimeStamp;
			public Guid Guid;
			public uint ClientContext;
			public uint Flags;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct EVENT_TRACE_PROPERTIES {
			public WNODE_HEADER Wnode;
			public uint BufferSize;
			public uint MinimumBuffers;
			public uint MaximumBuffers;
			public uint MaximumFileSize;
			public uint LogFileMode;
			public uint FlushTimer;
			public uint EnableFlags;
			public int AgeLimit;
			public uint NumberOfBuffers;
			public uint FreeBuffers;
			public uint EventsLost;
			public uint BuffersWritten;
			public uint LogBuffersLost;
			public uint RealTimeBuffersLost;
			public IntPtr LoggerThreadId;
			public uint LogFileNameOffset;
			public uint LoggerNameOffset;
		}

		[DllImport("advapi32.dll", EntryPoint = "QueryAllTracesW", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern uint QueryAllTracesW([In, Out] IntPtr[] properties, uint propertyCount, out uint loggerCount);

		// ReSharper disable InconsistentNaming

		#region Constants

		public const int SECURITY_DESCRIPTOR_REVISION = (1);
		public const int OWNER_SECURITY_INFORMATION = 0x1;

		public const UInt32 SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
		public const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
		public const UInt32 SE_PRIVILEGE_REMOVED = 0x00000004;
		public const UInt32 SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;
		public const Int32 ANYSIZE_ARRAY = 1;

		#endregion

		#region Structs

		[StructLayout(LayoutKind.Sequential)]
		public struct LUID {
			public uint LowPart;
			public int HighPart;
		}


		[StructLayout(LayoutKind.Sequential)]
		public struct LUID_AND_ATTRIBUTES {
			public LUID Luid;
			public UInt32 Attributes;
		}


		[StructLayout(LayoutKind.Sequential)]
		public struct SECURITY_DESCRIPTOR {
			public byte revision;
			public byte size;
			public short control;
			public IntPtr owner;
			public IntPtr group;
			public IntPtr sacl;
			public IntPtr dacl;
		}


		public struct TOKEN_PRIVILEGES {
			public UInt32 PrivilegeCount;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = ANYSIZE_ARRAY)]
			public LUID_AND_ATTRIBUTES[] Privileges;
		}


		[StructLayout(LayoutKind.Sequential)]
		public struct SECURITY_ATTRIBUTES {
			public UInt32 nLength;
			public IntPtr lpSecurityDescriptor;
			[MarshalAs(UnmanagedType.Bool)] public bool bInheritHandle;
		}

		#endregion

		#region Enums & Flags

		public enum SidNameUse : int {
			Alias = 4,
			Computer = 9,
			DeletedAccount = 6,
			Domain = 3,
			Group = 2,
			Invalid = 7,
			Unknown = 8,
			User = 1,
			WellKnownGroup = 5
		}


		public enum LOGON_TYPE {
			LOGON32_LOGON_INTERACTIVE = 2,
			LOGON32_LOGON_NETWORK = 3,
			LOGON32_LOGON_BATCH = 4,
			LOGON32_LOGON_SERVICE = 5,
			LOGON32_LOGON_UNLOCK = 7,
			LOGON32_LOGON_NETWORK_CLEARTEXT = 8,
			LOGON32_LOGON_NEW_CREDENTIALS = 9,
		}


		public enum LOGON_PROVIDER {
			LOGON32_PROVIDER_DEFAULT = 0,
			LOGON32_PROVIDER_WINNT35 = 1,
			LOGON32_PROVIDER_WINNT40 = 2,
			LOGON32_PROVIDER_WINNT50 = 3,
		}


		public enum SECURITY_IMPERSONATION_LEVEL : int {
			SecurityAnonymous = 0,
			SecurityIdentification = 1,
			SecurityImpersonation = 2,
			SecurityDelegation = 3
		}


		public enum TOKEN_TYPE : int {
			TokenPrimary = 1,
			TokenImpersonation
		}

		#endregion

		#region ADVAP32.DLL functions

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);


		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LookupPrivilegeName(string lpSystemName, IntPtr lpLuid, StringBuilder lpName, ref int cchName);


		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);


		// Use this signature if you want the previous state information returned
		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, UInt32 BufferLengthInBytes, ref TOKEN_PRIVILEGES PreviousState,
		                                                out UInt32 ReturnLengthInBytes);


		public static bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, UInt32 BufferLengthInBytes, out UInt32 ReturnLengthInBytes) {
			return _AdjustTokenPrivileges(TokenHandle, DisableAllPrivileges, ref NewState, BufferLengthInBytes, IntPtr.Zero, out ReturnLengthInBytes);
		}


		[DllImport("advapi32.dll", EntryPoint = "AdjustTokenPrivileges", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool _AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, UInt32 BufferLengthInBytes, [Out] IntPtr PreviousState,
		                                                  out UInt32 ReturnLengthInBytes);


		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool InitializeSecurityDescriptor(out SECURITY_DESCRIPTOR SecurityDescriptor, uint dwRevision);


		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetSecurityDescriptorOwner(ref SECURITY_DESCRIPTOR pSecurityDescriptor, IntPtr pOwner, bool bOwnerDefaulted);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetFileSecurity(string lpFileName, UInt32 SecurityInformation, ref SECURITY_DESCRIPTOR pSecurityDescriptor);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ConvertStringSidToSid(string StringSid, out IntPtr ptrSid);


		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LookupAccountName(string lpSystemName, string lpAccountName, IntPtr Sid, ref uint cbSid, StringBuilder ReferencedDomainName, ref uint cchReferencedDomainName, out SidNameUse peUse);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern uint GetSecurityDescriptorLength(IntPtr securityDescriptor);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LookupAccountSid(string lpSystemName, IntPtr SID, StringBuilder lpName, ref uint cchName, StringBuilder ReferencedDomainName, ref uint cchReferencedDomainName, out SidNameUse peUse);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LookupAccountSidW(string lpSystemName, IntPtr SID, StringBuilder lpName, ref uint cchName, StringBuilder ReferencedDomainName, ref uint cchReferencedDomainName, out SidNameUse peUse);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DuplicateToken(IntPtr ExistingTokenHandle, SECURITY_IMPERSONATION_LEVEL impersonationLevel, out IntPtr DuplicateTokenHandle);


		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, ref SECURITY_ATTRIBUTES lpTokenAttributes, SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, out IntPtr phNewToken);

		public static bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, out IntPtr phNewToken) {
			return _DuplicateTokenEx(hExistingToken, dwDesiredAccess, IntPtr.Zero, ImpersonationLevel, TokenType, out phNewToken);
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, EntryPoint = "DuplicateTokenEx", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private extern static bool _DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, IntPtr lpTokenAttributes, SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, out IntPtr phNewToken);


		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RevertToSelf();


		[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
		public static extern bool ConvertStringSidToSidW(string stringSID, out IntPtr SID);

		#endregion

		// ReSharper restore InconsistentNaming

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int RegRenameKey(SafeRegistryHandle key, string subKeyName, string newKeyName);



		public const uint TokenDuplicate = 0x0002;
		public const uint TokenImpersonate = 0x0004;
		public const uint TokenQuery = 0x0008;
		public const uint TokenAdjustPrivileges = 0x0020;
		public const int ErrorNoToken = 1008;
		public const int ErrorNotAllAssigned = 1300;

		[StructLayout(LayoutKind.Sequential)]
		public struct TokenPrivilege {
			public uint Count;
			public ADVAPI32.LUID Luid;
			public uint Attributes;
		}

		[DllImport("advapi32.dll", EntryPoint = "LogonUserW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LogonUser(string userName, string domain, string password, int logonType, int provider, out SafeAccessTokenHandle token);

		[DllImport("advapi32.dll", EntryPoint = "OpenThreadToken", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenThreadToken(IntPtr thread, uint access, [MarshalAs(UnmanagedType.Bool)] bool openAsSelf, out SafeAccessTokenHandle token);

		[DllImport("advapi32.dll", EntryPoint = "OpenProcessToken", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenProcessToken(IntPtr process, uint access, out SafeAccessTokenHandle token);

		[DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DuplicateTokenEx(SafeAccessTokenHandle token, uint access, IntPtr attributes, int impersonationLevel, int tokenType, out SafeAccessTokenHandle duplicate);

		[DllImport("advapi32.dll", EntryPoint = "SetThreadToken", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetThreadToken(IntPtr thread, IntPtr token);

		[DllImport("advapi32.dll", EntryPoint = "AdjustTokenPrivileges", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AdjustTokenPrivileges(SafeAccessTokenHandle token, [MarshalAs(UnmanagedType.Bool)] bool disableAll, ref TokenPrivilege privilege,
			uint bufferSize, out TokenPrivilege previous, out uint requiredSize);



		public const uint SC_MANAGER_CONNECT = 1;
		public const uint SERVICE_QUERY_CONFIG = 1;
		public const uint SERVICE_CHANGE_CONFIG = 2;
		public const uint SERVICE_QUERY_STATUS = 4;
		public const uint SERVICE_NO_CHANGE = uint.MaxValue;
		public const uint SERVICE_CONFIG_DESCRIPTION = 1;

		[StructLayout(LayoutKind.Sequential)]
		public struct QUERY_SERVICE_CONFIG {
			public uint ServiceType;
			public uint StartType;
			public uint ErrorControl;
			public IntPtr BinaryPath;
			public IntPtr LoadOrderGroup;
			public uint TagId;
			public IntPtr Dependencies;
			public IntPtr StartName;
			public IntPtr DisplayName;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SERVICE_STATUS_PROCESS {
			public uint ServiceType;
			public uint CurrentState;
			public uint ControlsAccepted;
			public uint Win32ExitCode;
			public uint ServiceSpecificExitCode;
			public uint CheckPoint;
			public uint WaitHint;
			public uint ProcessId;
			public uint ServiceFlags;
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public static extern IntPtr OpenSCManagerW(string machine, string database, uint access);
		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public static extern IntPtr OpenServiceW(IntPtr manager, string name, uint access);
		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseServiceHandle(IntPtr handle);
		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryServiceConfigW(IntPtr service, IntPtr buffer, uint size, out uint needed);
		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryServiceConfig2W(IntPtr service, uint level, IntPtr buffer, uint size, out uint needed);
		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryServiceStatusEx(IntPtr service, uint level, out SERVICE_STATUS_PROCESS status, uint size, out uint needed);
		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ChangeServiceConfigW(IntPtr service, uint type, uint startType, uint errorControl, string binaryPath,
			string loadOrderGroup, IntPtr tagId, string dependencies, string account, string password, string displayName);


	}

}

