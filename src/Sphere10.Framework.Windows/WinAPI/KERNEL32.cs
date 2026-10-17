// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {

	public static partial class KERNEL32 {
		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReadProcessMemory(KernelHandle process, IntPtr address, [Out] byte[] buffer, UIntPtr size, out UIntPtr bytesRead);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern UIntPtr VirtualQueryEx(KernelHandle process, IntPtr address, out MEMORY_BASIC_INFORMATION information, UIntPtr length);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process(KernelHandle process, [MarshalAs(UnmanagedType.Bool)] out bool wow64);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsProcessInJob(KernelHandle process, IntPtr job, [MarshalAs(UnmanagedType.Bool)] out bool isInJob);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryInformationJobObject(IntPtr job, int informationClass, out JOBOBJECT_BASIC_ACCOUNTING_INFORMATION information, uint length, out uint returnedLength);

		[StructLayout(LayoutKind.Sequential)]
		public struct MEMORY_BASIC_INFORMATION {
			public IntPtr BaseAddress;
			public IntPtr AllocationBase;
			public uint AllocationProtect;
			public UIntPtr RegionSize;
			public uint State;
			public uint Protect;
			public uint Type;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct JOBOBJECT_BASIC_ACCOUNTING_INFORMATION {
			public long TotalUserTime;
			public long TotalKernelTime;
			public long PeriodUserTime;
			public long PeriodKernelTime;
			public uint TotalPageFaultCount;
			public uint TotalProcesses;
			public uint ActiveProcesses;
			public uint TotalTerminatedProcesses;
		}

		#region Constants

		public const int MAX_PATH = 260;
		public const int ERROR_NO_MORE_FILES = 18;
		public const int ERROR_FILE_NOT_FOUND = 2;
		public const int ERROR_PATH_NOT_FOUND = 3;

		public const uint ES_AWAYMODE_REQUIRED = 0x00000040;
		public const uint ES_CONTINUOUS = 0x80000000;
		public const uint ES_DISPLAY_REQUIRED = 0x00000002;
		public const uint ES_SYSTEM_REQUIRED = 0x00000001;
		public const uint ES_USER_PRESENT = 0x00000004;

		public const uint CTRL_C_EVENT = 0;
		public const uint CTRL_BREAK_EVENT = 1;

		#endregion


		#region Structs

		[StructLayout(LayoutKind.Sequential)]
		public struct FILETIME {
			public uint DateTimeLow;
			public uint DateTimeHigh;
		}


		[Serializable]
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct WIN32_FIND_DATA {
			internal UInt32 FileAttributes;
			internal FILETIME CreationTime;
			internal FILETIME LastAccessTime;
			internal FILETIME LastWriteTime;
			internal UInt32 FileSizeHigh;
			internal UInt32 FileSizeLow;
			internal UInt32 Reserved0;
			internal UInt32 Reserved1;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
			internal String FileName;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
			internal String AlternateFileName;
		}

		#endregion

		#region Enums & Flags

		[Flags]
		public enum LoadLibraryFlags : uint {
			DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
			LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
			LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
			LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
			LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
			LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
		}


		[Flags]
		public enum ErrorModes : uint {
			SYSTEM_DEFAULT = 0x0,
			SEM_FAILCRITICALERRORS = 0x0001,
			SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
			SEM_NOGPFAULTERRORBOX = 0x0002,
			SEM_NOOPENFILEERRORBOX = 0x8000
		}

		#endregion

		#region KERNEL32.DLL Functions

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool FreeConsole();

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool AttachConsole(int dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, UInt32 nSize);


		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetModuleFileName(IntPtr hModule, string fileName, UInt32 bufferSize);


		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern SafeFileHandle CreateFile(string lpFileName, [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess, [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode, IntPtr lpSecurityAttributes,
		                                               [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition, [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);


		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeFileHandle CreateFileW(String lpFileName, [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess, [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode, IntPtr lpSecurityAttributes,
		                                                [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition, [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);


		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteFile(string lpFileName);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteFileW(String lpFileName);


		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CreateDirectory(string lpPathName, IntPtr lpSecurityAttributes);


		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool CreateDirectoryW(string lpPathName, IntPtr lpSecurityAttributes);


		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RemoveDirectory(string lpPathName);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RemoveDirectoryW(string lpPathName);


		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern SafeFindHandle FindFirstFile(string lpFileName, out WIN32_FIND_DATA findData);


		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeFindHandle FindFirstFileW(string lpFileName, out WIN32_FIND_DATA findData);


		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool FindNextFile(SafeFindHandle hndFindFile, out WIN32_FIND_DATA findData);


		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool FindNextFileW(SafeFindHandle hndFindFile, out WIN32_FIND_DATA findData);


		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool FindClose(IntPtr handle);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WIN32_FIND_STREAM_DATA {
			public long StreamSize;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)]
			public string StreamName;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public static extern SafeFindHandle FindFirstStreamW(string fileName, int infoLevel, out WIN32_FIND_STREAM_DATA data, uint flags);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FindNextStreamW(SafeFindHandle search, out WIN32_FIND_STREAM_DATA data);


		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CopyFile(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);


		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CopyFileW(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);


		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern uint GetFullPathName(string lpFileName, uint nBufferLength, [Out] StringBuilder lpBuffer, out StringBuilder lpFilePart);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint GetFullPathNameW(string lpFileName, uint nBufferLength, [Out] StringBuilder lpBuffer, out StringBuilder lpFilePart);


		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetCurrentProcess();


		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr LocalFree(IntPtr hMem);


		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr LoadLibrary(string lpFileName);


		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

		public static IntPtr LoadLibraryEx(string lpfFileName, LoadLibraryFlags flags) {
			return LoadLibraryEx(lpfFileName, IntPtr.Zero, flags);
		}

		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);


		[DllImport("kernel32.dll")]
		public static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage);

		[DllImport("kernel32.dll")]
		public static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

		[DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
		public static extern IntPtr FindResource(IntPtr hModule, string lpName, int lpType);

		[DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
		public static extern IntPtr FindResource(IntPtr hModule, string lpName, string lpType);

		[DllImport("kernel32.dll")]
		public static extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);


		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);


		[DllImport("kernel32.dll")]
		public static extern int FreeResource(IntPtr hglbResource);


		[DllImport("kernel32.dll")]
		public static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);


		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

		[DllImport("kernel32.dll")]
		public static extern ErrorModes SetErrorMode(ErrorModes uMode);

		[DllImport("kernel32.dll")]
		public static extern int GetLastError();


		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr handle);


		[DllImport("kernel32.dll")]
		public static extern uint SetThreadExecutionState(uint esFlags);


		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);

		#endregion


		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public delegate bool ControlHandler(uint controlType);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetConsoleCtrlHandler(ControlHandler handler, [MarshalAs(UnmanagedType.Bool)] bool add);



		public const uint Continue = 0;
		public const uint Cancel = 1;
		public const int ErrorRequestAborted = 1235;

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		public delegate uint ProgressCallback(long totalSize, long totalTransferred, long streamSize, long streamTransferred,
			uint streamNumber, uint reason, IntPtr sourceHandle, IntPtr destinationHandle, IntPtr data);

		[DllImport("kernel32.dll", EntryPoint = "CopyFileExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CopyFileEx(string sourcePath, string destinationPath, ProgressCallback progress, IntPtr data, IntPtr cancel, uint flags);



		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint clientProcessId);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint serverProcessId);



		public const uint ProcessQueryLimitedInformation = 4096;
		public const uint Synchronize = 0x00100000;
		public const uint ThreadQueryLimitedInformation = 0x0800;
		public const uint ThreadSetLimitedInformation = 0x0400;
		public const uint ThreadSuspendResume = 0x0002;
		public const uint WaitObject0 = 0;
		public const uint WaitTimeout = 258;
		public const uint WaitFailed = uint.MaxValue;
		public const int ThreadPriorityErrorReturn = int.MaxValue;
		public const uint SnapshotProcesses = 2;
		public const ushort AllProcessorGroups = 65535;
		[DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
		public static extern KERNEL32.KernelHandle OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		public static extern KERNEL32.KernelHandle OpenThread(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint threadId);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		public static extern uint GetThreadId(KERNEL32.KernelHandle thread);
		[DllImport("kernel32.dll", ExactSpelling = true)]
		public static extern uint GetCurrentThreadId();
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		public static extern uint GetProcessIdOfThread(KERNEL32.KernelHandle thread);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		public static extern uint WaitForSingleObject(KERNEL32.KernelHandle handle, uint milliseconds);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetThreadTimes(KERNEL32.KernelHandle thread, out KERNEL32.FILETIME creation, out KERNEL32.FILETIME exit, out KERNEL32.FILETIME kernel, out KERNEL32.FILETIME user);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		public static extern int GetThreadPriority(KERNEL32.KernelHandle thread);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetThreadPriority(KERNEL32.KernelHandle thread, int priority);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetThreadPriorityBoost(KERNEL32.KernelHandle thread, [MarshalAs(UnmanagedType.Bool)] out bool disabled);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetThreadPriorityBoost(KERNEL32.KernelHandle thread, [MarshalAs(UnmanagedType.Bool)] bool disabled);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		public static extern uint SuspendThread(KERNEL32.KernelHandle thread);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		public static extern uint ResumeThread(KERNEL32.KernelHandle thread);
		[DllImport("kernel32.dll", EntryPoint = "GetProcessTimes", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetProcessTimes(KERNEL32.KernelHandle process, out KERNEL32.FILETIME creation, out KERNEL32.FILETIME exit, out KERNEL32.FILETIME kernel, out KERNEL32.FILETIME user);
		[DllImport("kernel32.dll", EntryPoint = "GetProcessIoCounters", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetProcessIoCounters(KERNEL32.KernelHandle process, out KERNEL32.IO_COUNTERS counters);
		[DllImport("kernel32.dll", EntryPoint = "GetProcessHandleCount", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetProcessHandleCount(KERNEL32.KernelHandle process, out uint count);
		[DllImport("kernel32.dll", EntryPoint = "GetPriorityClass", SetLastError = true)]
		public static extern uint GetPriorityClass(KERNEL32.KernelHandle process);
		[DllImport("kernel32.dll", EntryPoint = "ProcessIdToSessionId", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ProcessIdToSessionId(uint processId, out uint sessionId);
		[DllImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryFullProcessImageName(KERNEL32.KernelHandle process, uint flags, StringBuilder name, ref uint size);
		[DllImport("kernel32.dll", EntryPoint = "CreateToolhelp32Snapshot", SetLastError = true)]
		public static extern KERNEL32.KernelHandle CreateToolhelp32Snapshot(uint flags, uint processId);
		[DllImport("kernel32.dll", EntryPoint = "Process32FirstW", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool Process32First(KERNEL32.KernelHandle snapshot, ref KERNEL32.PROCESSENTRY32 entry);
		[DllImport("kernel32.dll", EntryPoint = "Process32NextW", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool Process32Next(KERNEL32.KernelHandle snapshot, ref KERNEL32.PROCESSENTRY32 entry);
		[DllImport("kernel32.dll", EntryPoint = "GetSystemTimes", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetSystemTimes(out KERNEL32.FILETIME idle, out KERNEL32.FILETIME kernel, out KERNEL32.FILETIME user);
		[DllImport("kernel32.dll", EntryPoint = "GlobalMemoryStatusEx", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GlobalMemoryStatusEx(ref KERNEL32.MEMORYSTATUSEX memory);
		[DllImport("kernel32.dll", EntryPoint = "GetActiveProcessorCount", SetLastError = true)]
		public static extern uint GetActiveProcessorCount(ushort groupNumber);
		[DllImport("kernel32.dll", EntryPoint = "GetCurrentThread", SetLastError = true)]
		public static extern IntPtr GetCurrentThread();
		[DllImport("kernel32.dll", EntryPoint = "GetThreadGroupAffinity", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetThreadGroupAffinity(IntPtr thread, out KERNEL32.GROUP_AFFINITY affinity);
		public sealed class KernelHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid {
			public KernelHandle()
				: base(true) { }
			protected override bool ReleaseHandle() => KERNEL32.CloseHandle(handle);
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct GROUP_AFFINITY {
			public UIntPtr Mask;
			public ushort Group;
			public ushort Reserved1;
			public ushort Reserved2;
			public ushort Reserved3;
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct IO_COUNTERS {
			public ulong ReadOperationCount;
			public ulong WriteOperationCount;
			public ulong OtherOperationCount;
			public ulong ReadTransferCount;
			public ulong WriteTransferCount;
			public ulong OtherTransferCount;
		}
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct PROCESSENTRY32 {
			public uint Size;
			public uint Usage;
			public uint ProcessId;
			public UIntPtr DefaultHeapId;
			public uint ModuleId;
			public uint ThreadCount;
			public uint ParentProcessId;
			public int BasePriority;
			public uint Flags;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string ExecutableFile;
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct MEMORYSTATUSEX {
			public uint Length;
			public uint MemoryLoad;
			public ulong TotalPhysical;
			public ulong AvailablePhysical;
			public ulong TotalPageFile;
			public ulong AvailablePageFile;
			public ulong TotalVirtual;
			public ulong AvailableVirtual;
			public ulong AvailableExtendedVirtual;
		}

		public const uint ProcessDuplicateHandle = 0x40;
		public const uint DuplicateSameAccess = 2;
		public const uint DuplicateCloseSource = 1;
		public const uint FileTypeDisk = 1;

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DuplicateHandle(KERNEL32.KernelHandle sourceProcess, IntPtr sourceHandle,
			IntPtr targetProcess, out KERNEL32.KernelHandle targetHandle, uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint options);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern uint GetFileType(KERNEL32.KernelHandle file);

		[DllImport("kernel32.dll", EntryPoint = "GetFileType", SetLastError = true)]
		public static extern uint GetFileType(Microsoft.Win32.SafeHandles.SafeFileHandle file);

		[DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern uint GetFinalPathNameByHandle(KERNEL32.KernelHandle file, StringBuilder path, uint size, uint flags);
	}

}

