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
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Sphere10.Framework.Windows;

/// <summary>Owns a process handle so queries remain tied to one process instance.</summary>
public sealed class WindowsProcess : IDisposable {
	private readonly WinAPI.KERNEL32.KernelHandle _handle;
	private readonly bool _canSynchronize;

	private WindowsProcess(int processId, WinAPI.KERNEL32.KernelHandle handle, bool canSynchronize) {
		ProcessId = processId;
		_handle = handle;
		_canSynchronize = canSynchronize;
	}

	public int ProcessId { get; }

	public ProcessIdentity Identity {
		get {
			EnsureOpen();
			if (!WinAPI.KERNEL32.GetProcessTimes(_handle, out var creation, out _, out _, out _))
				throw new Win32Exception(Marshal.GetLastWin32Error());
			return new ProcessIdentity(ProcessId, DateTime.FromFileTimeUtc(Ticks(creation)));
		}
	}

	public bool HasExited {
		get {
			EnsureOpen();
			if (!_canSynchronize)
				throw new Win32Exception(5, "The process can be queried, but Windows denied synchronization access required to verify whether it has exited.");
			return HasSignaledExit(_handle);
		}
	}

	public string ExecutablePath {
		get {
			EnsureOpen();
			var buffer = new StringBuilder(512);
			var length = (uint)buffer.Capacity;
			if (WinAPI.KERNEL32.QueryFullProcessImageName(_handle, 0, buffer, ref length))
				return buffer.ToString();
			var error = Marshal.GetLastWin32Error();
			if (error != 122)
				throw new Win32Exception(error);
			buffer = new StringBuilder(32768);
			length = (uint)buffer.Capacity;
			if (!WinAPI.KERNEL32.QueryFullProcessImageName(_handle, 0, buffer, ref length))
				throw new Win32Exception(Marshal.GetLastWin32Error());
			return buffer.ToString();
		}
	}

	public static WindowsProcess Open(int processId) {
		Guard.ArgumentGT(processId, 0, nameof(processId));
		var handle = WinAPI.KERNEL32.OpenProcess(WinAPI.KERNEL32.ProcessQueryLimitedInformation | WinAPI.KERNEL32.Synchronize, false, (uint)processId);
		if (!handle.IsInvalid)
			return new WindowsProcess(processId, handle, true);
		var error = Marshal.GetLastWin32Error();
		handle.Dispose();
		// Preserve the existing read-only query access for targets whose ACL denies synchronization.
		// Their timing/path/counter metadata remains available; liveness is explicitly unavailable.
		if (error == 5) {
			handle = WinAPI.KERNEL32.OpenProcess(WinAPI.KERNEL32.ProcessQueryLimitedInformation, false, (uint)processId);
			if (!handle.IsInvalid)
				return new WindowsProcess(processId, handle, false);
			error = Marshal.GetLastWin32Error();
			handle.Dispose();
		}
		throw new Win32Exception(error);
	}

	internal static bool HasSignaledExit(WinAPI.KERNEL32.KernelHandle handle) {
		// GetProcessTimes' exit FILETIME is undefined while a process is alive; its signaled state is authoritative.
		var result = WinAPI.KERNEL32.WaitForSingleObject(handle, 0);
		if (result == WinAPI.KERNEL32.WaitFailed)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The process liveness could not be checked.");
		Guard.Ensure(result == WinAPI.KERNEL32.WaitObject0 || result == WinAPI.KERNEL32.WaitTimeout, "Windows returned an unexpected process wait state.");
		return result == WinAPI.KERNEL32.WaitObject0;
	}

	/// <summary>Unavailable counters are null; no sampling, deltas or display policy is applied.</summary>
	public WindowsProcessCounters ReadCounters() {
		EnsureOpen();
		var hasTimes = WinAPI.KERNEL32.GetProcessTimes(_handle, out var creation, out _, out var kernel, out var user);
		var hasIo = WinAPI.KERNEL32.GetProcessIoCounters(_handle, out var io);
		var hasMemory = WinAPI.PSAPI.GetProcessMemoryInfo(_handle, out var memory, (uint)Marshal.SizeOf<WinAPI.PSAPI.PROCESS_MEMORY_COUNTERS_EX>());
		var hasHandles = WinAPI.KERNEL32.GetProcessHandleCount(_handle, out var handles);
		var hasSession = WinAPI.KERNEL32.ProcessIdToSessionId((uint)ProcessId, out var session);
		var priority = WinAPI.KERNEL32.GetPriorityClass(_handle);
		return new WindowsProcessCounters {
			Identity = new ProcessIdentity(ProcessId, hasTimes ? DateTime.FromFileTimeUtc(Ticks(creation)) : null),
			CpuTime = hasTimes ? TimeSpan.FromTicks(Ticks(kernel) + Ticks(user)) : null,
			ReadBytes = hasIo ? io.ReadTransferCount : null, WriteBytes = hasIo ? io.WriteTransferCount : null, OtherBytes = hasIo ? io.OtherTransferCount : null,
			WorkingSetBytes = hasMemory ? checked((long)memory.WorkingSetSize.ToUInt64()) : null,
			PrivateMemoryBytes = hasMemory ? checked((long)memory.PrivateUsage.ToUInt64()) : null,
			HandleCount = hasHandles ? checked((int)handles) : null, SessionId = hasSession ? checked((int)session) : null,
			Priority = priority > 0 ? (ProcessPriorityClass)priority : null
		};
	}

	public WindowsThreadInfo[] GetThreads() => GetThreadsCore(null);

	public Task<WindowsThreadInfo[]> GetThreadsAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => GetThreadsCore(cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private WindowsThreadInfo[] GetThreadsCore(Action checkpoint) {
		checkpoint?.Invoke();
		using var process = OpenManaged();
		var threads = process.Threads;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			foreach (ProcessThread thread in threads)
				thread.Dispose();
		});
		var result = new List<WindowsThreadInfo>();
		foreach (ProcessThread thread in threads) {
			checkpoint?.Invoke();
			var state = Read(() => thread.ThreadState, (System.Diagnostics.ThreadState?)null);
			result.Add(new WindowsThreadInfo(Read(() => thread.Id, 0), state,
				state == System.Diagnostics.ThreadState.Wait ? Read(() => thread.WaitReason, (ThreadWaitReason?)null) : null,
				Read(() => thread.BasePriority, (int?)null), Read(() => thread.CurrentPriority, (int?)null),
				Read(() => thread.TotalProcessorTime, (TimeSpan?)null), Read(() => thread.StartTime.ToUniversalTime(), (DateTime?)null)));
		}
		checkpoint?.Invoke();
		return result.ToArray();
	}

	public WindowsModuleInfo[] GetModules() => GetModulesCore(null);

	public Task<WindowsModuleInfo[]> GetModulesAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => GetModulesCore(cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private WindowsModuleInfo[] GetModulesCore(Action checkpoint) {
		checkpoint?.Invoke();
		using var process = OpenManaged();
		var modules = process.Modules;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			foreach (ProcessModule module in modules)
				module.Dispose();
		});
		var result = new List<WindowsModuleInfo>();
		foreach (ProcessModule module in modules) {
			checkpoint?.Invoke();
			result.Add(new WindowsModuleInfo(Read(() => module.ModuleName, string.Empty), Read(() => module.FileName, string.Empty),
				Read(() => module.BaseAddress, (IntPtr?)null), Read(() => module.ModuleMemorySize, (int?)null)));
		}
		checkpoint?.Invoke();
		return result.ToArray();
	}

	public string GetCommandLine() => GetCommandLineCore(null);

	public Task<string> GetCommandLineAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => GetCommandLineCore(cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private string GetCommandLineCore(Action checkpoint) {
		checkpoint?.Invoke();
		var identity = Identity;
		Guard.Ensure(identity.IsVerified && !HasExited, "The process identity is unavailable or has exited.");
		checkpoint?.Invoke();
		var options = new System.Management.EnumerationOptions { Timeout = TimeSpan.FromSeconds(3), ReturnImmediately = false, Rewindable = false };
		using var searcher = new ManagementObjectSearcher("root\\cimv2", $"SELECT CommandLine, CreationDate FROM Win32_Process WHERE ProcessId = {ProcessId}", options);
		using var matches = searcher.Get();
		foreach (ManagementObject match in matches) {
			using (match) {
				checkpoint?.Invoke();
				var creation = match["CreationDate"] as string;
				if (!string.IsNullOrEmpty(creation) && Math.Abs((ManagementDateTimeConverter.ToDateTime(creation).ToUniversalTime() - identity.StartTimeUtc.Value).Ticks) < 10)
					return match["CommandLine"] as string ?? string.Empty;
			}
		}
		throw new InvalidOperationException("The process identity no longer matches the command-line query.");
	}

	public WindowsProcessEnvironmentVariable[] GetEnvironment(int maximumBytes = 1048576) => GetEnvironmentCore(maximumBytes, null);

	public Task<WindowsProcessEnvironmentVariable[]> GetEnvironmentAsync(int maximumBytes = 1048576, CancellationToken cancellationToken = default)
		=> Task.Run(() => GetEnvironmentCore(maximumBytes, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public WindowsProcessTokenInfo GetTokenInformation() => GetTokenInformationCore(null);

	public Task<WindowsProcessTokenInfo> GetTokenInformationAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => GetTokenInformationCore(cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	/// <summary>Queries membership for this process. Accounting is available only for this caller's own job; arbitrary target jobs have no documented discoverable handle.</summary>
	public WindowsProcessJobInfo GetJobInformation() {
		EnsureOpen();
		if (!WinAPI.KERNEL32.IsProcessInJob(_handle, IntPtr.Zero, out var isInJob))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		WindowsJobAccountingInfo accounting = null;
		if (isInJob && ProcessId == Environment.ProcessId) {
			if (!WinAPI.KERNEL32.QueryInformationJobObject(IntPtr.Zero, 1, out var data, (uint)Marshal.SizeOf<WinAPI.KERNEL32.JOBOBJECT_BASIC_ACCOUNTING_INFORMATION>(), out _))
				throw new Win32Exception(Marshal.GetLastWin32Error());
			accounting = new(TimeSpan.FromTicks(data.TotalUserTime), TimeSpan.FromTicks(data.TotalKernelTime), TimeSpan.FromTicks(data.PeriodUserTime),
				TimeSpan.FromTicks(data.PeriodKernelTime), data.TotalPageFaultCount, data.TotalProcesses, data.ActiveProcesses, data.TotalTerminatedProcesses);
		}
		return new WindowsProcessJobInfo(isInJob, accounting);
	}

	public Task<WindowsProcessJobInfo> GetJobInformationAsync(CancellationToken cancellationToken = default) => Task.Run(GetJobInformation, cancellationToken);

	public WindowsProcessStringScan GetMemoryStrings(int minimumLength = 4, int maximumResults = 2000, long maximumBytes = 33554432)
		=> GetMemoryStringsCore(minimumLength, maximumResults, maximumBytes, null);

	public Task<WindowsProcessStringScan> GetMemoryStringsAsync(int minimumLength = 4, int maximumResults = 2000, long maximumBytes = 33554432,
		CancellationToken cancellationToken = default) => Task.Run(() => GetMemoryStringsCore(minimumLength, maximumResults, maximumBytes, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public WindowsProcessStringScan GetImageStrings(int minimumLength = 4, int maximumResults = 2000, long maximumBytes = 33554432)
		=> GetImageStringsCore(minimumLength, maximumResults, maximumBytes, null);

	public Task<WindowsProcessStringScan> GetImageStringsAsync(int minimumLength = 4, int maximumResults = 2000, long maximumBytes = 33554432,
		CancellationToken cancellationToken = default) => Task.Run(() => GetImageStringsCore(minimumLength, maximumResults, maximumBytes, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public void Dispose() => _handle.Dispose();

	private WindowsProcessEnvironmentVariable[] GetEnvironmentCore(int maximumBytes, Action checkpoint) {
		Guard.Argument(maximumBytes is >= 4 and <= 16 * 1024 * 1024 && maximumBytes % 2 == 0, nameof(maximumBytes), "Choose an even byte limit from 4 bytes to 16 MiB.");
		checkpoint?.Invoke();
		using var memory = OpenReadHandle();
		var pointerSize = GetTargetPointerSize(memory);
		using var information = new UnmanagedMemoryScope(IntPtr.Size * 6);
		var status = WinAPI.NTDLL.NtQueryInformationProcess(memory, pointerSize == 4 && IntPtr.Size == 8 ? 26 : 0, information.Item,
			(uint)(pointerSize == 4 && IntPtr.Size == 8 ? IntPtr.Size : information.Size), out _);
		CheckNtStatus(status);
		var peb = ToAddress(Marshal.ReadIntPtr(information.Item, pointerSize == 4 && IntPtr.Size == 8 ? 0 : IntPtr.Size));
		var parameterPointer = ReadRemotePointer(memory, peb + (pointerSize == 8 ? 0x20UL : 0x10UL), pointerSize);
		Guard.Ensure(parameterPointer != 0, "The process has no initialized parameter block.");
		// These NT process-parameter layouts are version-sensitive. Validate their lengths/normalization and recheck the environment pointer after reading.
		var environmentOffset = pointerSize == 8 ? 0x80 : 0x48;
		var parameters = ReadRemoteExact(memory, parameterPointer, environmentOffset + pointerSize);
		var length = EndianBitConverter.Little.ToUInt32(parameters, 4);
		var maximumLength = EndianBitConverter.Little.ToUInt32(parameters);
		var flags = EndianBitConverter.Little.ToUInt32(parameters, 8);
		if (length < parameters.Length || maximumLength < length || (flags & 1) == 0)
			throw new NotSupportedException("The native process-parameter layout is unavailable or not normalized.");
		var environment = ReadPointer(parameters, environmentOffset, pointerSize);
		if (environment == 0)
			return Array.Empty<WindowsProcessEnvironmentVariable>();
		var text = new StringBuilder();
		var previousZero = false;
		var examined = 0;
		while (examined < maximumBytes) {
			checkpoint?.Invoke();
			var address = environment + (uint)examined;
			var region = QueryMemory(memory, address);
			Guard.Ensure(IsReadable(region), "The process environment is not readable.");
			var remaining = checked(ToAddress(region.BaseAddress) + region.RegionSize.ToUInt64() - address);
			var count = (int)Math.Min(Math.Min(4096UL, remaining), (ulong)(maximumBytes - examined));
			count -= count % 2;
			Guard.Ensure(count > 0, "The process environment contains a partial UTF-16 character.");
			var bytes = ReadRemoteExact(memory, address, count);
			for (var offset = 0; offset < bytes.Length; offset += 2) {
				var character = (char)EndianBitConverter.Little.ToUInt16(bytes, offset);
				if (previousZero && character == 0) {
					Guard.Ensure(ReadRemotePointer(memory, parameterPointer + (uint)environmentOffset, pointerSize) == environment, "The process changed its environment while it was being read.");
					return ParseEnvironment(text.ToString());
				}
				text.Append(character);
				previousZero = character == 0;
			}
			examined += bytes.Length;
		}
		throw new InvalidDataException("The environment did not terminate within the requested byte limit.");
	}

	internal static WindowsProcessEnvironmentVariable[] ParseEnvironment(string block) {
		var result = new List<WindowsProcessEnvironmentVariable>();
		foreach (var entry in block.Split('\0', StringSplitOptions.RemoveEmptyEntries)) {
			var separator = entry.IndexOf('=', entry[0] == '=' ? 1 : 0);
			if (separator < 0)
				throw new InvalidDataException("The process environment contains a malformed entry.");
			result.Add(new(entry[..separator], entry[(separator + 1)..]));
		}
		return result.ToArray();
	}

	private WindowsProcessTokenInfo GetTokenInformationCore(Action checkpoint) {
		EnsureOpen();
		checkpoint?.Invoke();
		if (!WinAPI.ADVAPI32.OpenProcessToken(_handle, WinAPI.ADVAPI32.TokenQuery, out var token)) {
			var error = Marshal.GetLastWin32Error();
			token?.Dispose();
			throw new Win32Exception(error);
		}
		using (token) {
			var groups = new List<WindowsTokenGroup>();
			using (var buffer = ReadToken(token, 2)) {
				var count = unchecked((uint)Marshal.ReadInt32(buffer.Item));
				var first = Marshal.OffsetOf<WinAPI.ADVAPI32.TOKEN_GROUPS_HEADER>(nameof(WinAPI.ADVAPI32.TOKEN_GROUPS_HEADER.FirstGroup)).ToInt32();
				var size = Marshal.SizeOf<WinAPI.ADVAPI32.SID_AND_ATTRIBUTES>();
				if (count > (buffer.Size - first) / size)
					throw new InvalidDataException("The token group array exceeds its native buffer.");
				for (var index = 0; index < count; index++) {
					checkpoint?.Invoke();
					var group = Marshal.PtrToStructure<WinAPI.ADVAPI32.SID_AND_ATTRIBUTES>(IntPtr.Add(buffer.Item, checked(first + (int)index * size)));
					groups.Add(new(ReadSid(buffer, group.Sid), group.Attributes));
				}
			}
			var privileges = new List<WindowsTokenPrivilege>();
			using (var buffer = ReadToken(token, 3)) {
				var count = unchecked((uint)Marshal.ReadInt32(buffer.Item));
				var size = Marshal.SizeOf<WinAPI.ADVAPI32.LUID_AND_ATTRIBUTES>();
				if (count > (buffer.Size - 4) / size)
					throw new InvalidDataException("The token privilege array exceeds its native buffer.");
				for (var index = 0; index < count; index++) {
					checkpoint?.Invoke();
					var privilege = Marshal.PtrToStructure<WinAPI.ADVAPI32.LUID_AND_ATTRIBUTES>(IntPtr.Add(buffer.Item, checked(4 + (int)index * size)));
					var name = new StringBuilder(256);
					var length = (uint)name.Capacity;
					if (!WinAPI.ADVAPI32.LookupPrivilegeName(null, ref privilege.Luid, name, ref length))
						throw new Win32Exception(Marshal.GetLastWin32Error());
					privileges.Add(new(name.ToString(), ((ulong)(uint)privilege.Luid.HighPart << 32) | privilege.Luid.LowPart, privilege.Attributes));
				}
			}
			checkpoint?.Invoke();
			return new WindowsProcessTokenInfo {
				UserSid = ReadTokenSid(token, 1), OwnerSid = ReadTokenSid(token, 4), PrimaryGroupSid = ReadTokenSid(token, 5), IntegritySid = ReadTokenSid(token, 25),
				SessionId = ReadTokenInteger(token, 12), IsElevated = ReadTokenInteger(token, 20) != 0, ElevationType = ReadTokenInteger(token, 18),
				TokenType = ReadTokenInteger(token, 8), Groups = groups.ToArray(), Privileges = privileges.ToArray()
			};
		}
	}

	private static UnmanagedMemoryScope ReadToken(SafeAccessTokenHandle token, int informationClass) {
		WinAPI.ADVAPI32.GetTokenInformation(token, informationClass, IntPtr.Zero, 0, out var size);
		var error = Marshal.GetLastWin32Error();
		if (error != 122)
			throw new Win32Exception(error);
		Guard.Ensure(size >= 4 && size <= 16 * 1024 * 1024, "The native token buffer exceeds its bound.");
		var buffer = new UnmanagedMemoryScope((int)size);
		var transferred = false;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => { if (!transferred) buffer.Dispose(); });
		if (!WinAPI.ADVAPI32.GetTokenInformation(token, informationClass, buffer.Item, size, out _))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		transferred = true;
		return buffer;
	}

	private static int ReadTokenInteger(SafeAccessTokenHandle token, int informationClass) {
		// These information classes return a DWORD, enum or single-DWORD structure.
		// Some reject a zero-length size probe with ERROR_BAD_LENGTH instead of ERROR_INSUFFICIENT_BUFFER.
		using var buffer = new UnmanagedMemoryScope(sizeof(int));
		if (!WinAPI.ADVAPI32.GetTokenInformation(token, informationClass, buffer.Item, sizeof(int), out var size))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		if (size != sizeof(int))
			throw new InvalidDataException("The native token integer has an unexpected length.");
		return Marshal.ReadInt32(buffer.Item);
	}

	private static string ReadTokenSid(SafeAccessTokenHandle token, int informationClass) {
		using var buffer = ReadToken(token, informationClass);
		return ReadSid(buffer, Marshal.ReadIntPtr(buffer.Item));
	}

	private static string ReadSid(UnmanagedMemoryScope buffer, IntPtr sid) {
		var offset = sid.ToInt64() - buffer.Item.ToInt64();
		if (offset < 0 || offset > buffer.Size - 8 || 8 + Marshal.ReadByte(sid, 1) * 4 > buffer.Size - offset)
			throw new InvalidDataException("A token SID exceeds its native buffer.");
		return new SecurityIdentifier(sid).Value;
	}

	private WindowsProcessStringScan GetImageStringsCore(int minimumLength, int maximumResults, long maximumBytes, Action checkpoint) {
		ValidateStringLimits(minimumLength, maximumResults, maximumBytes);
		EnsureOpen();
		checkpoint?.Invoke();
		using var image = new FileStream(ExecutablePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 65536, FileOptions.SequentialScan);
		var scanner = new ProcessStringScanner(minimumLength, maximumResults);
		var buffer = new byte[65536];
		long examined = 0;
		while (!scanner.IsFull && examined < maximumBytes) {
			checkpoint?.Invoke();
			var read = image.Read(buffer, 0, (int)Math.Min(buffer.Length, maximumBytes - examined));
			if (read == 0)
				break;
			scanner.Feed(buffer, read, (ulong)examined, checkpoint);
			examined += read;
		}
		scanner.EndRegion();
		return new WindowsProcessStringScan { Strings = scanner.Strings, BytesExamined = examined, Truncated = scanner.IsFull || image.Position < image.Length };
	}

	private WindowsProcessStringScan GetMemoryStringsCore(int minimumLength, int maximumResults, long maximumBytes, Action checkpoint) {
		ValidateStringLimits(minimumLength, maximumResults, maximumBytes);
		checkpoint?.Invoke();
		using var memory = OpenReadHandle();
		var pointerSize = GetTargetPointerSize(memory);
		var maximumAddress = pointerSize == 4 ? uint.MaxValue : (ulong)long.MaxValue;
		var scanner = new ProcessStringScanner(minimumLength, maximumResults);
		var buffer = new byte[65536];
		ulong address = 0;
		long examined = 0;
		var skipped = 0;
		while (address < maximumAddress && examined < maximumBytes && !scanner.IsFull) {
			checkpoint?.Invoke();
			var returned = WinAPI.KERNEL32.VirtualQueryEx(memory, ToPointer(address), out var region, (UIntPtr)(uint)Marshal.SizeOf<WinAPI.KERNEL32.MEMORY_BASIC_INFORMATION>());
			if (returned == UIntPtr.Zero) {
				var error = Marshal.GetLastWin32Error();
				if (error == 87)
					break;
				throw new Win32Exception(error);
			}
			var end = checked(ToAddress(region.BaseAddress) + region.RegionSize.ToUInt64());
			Guard.Ensure(end > address, "Windows returned a nonadvancing memory region.");
			if (!IsReadable(region)) {
				skipped++;
				address = end;
				continue;
			}
			while (address < end && examined < maximumBytes && !scanner.IsFull) {
				checkpoint?.Invoke();
				var count = (int)Math.Min(Math.Min((ulong)buffer.Length, end - address), (ulong)(maximumBytes - examined));
				var success = WinAPI.KERNEL32.ReadProcessMemory(memory, ToPointer(address), buffer, (UIntPtr)(uint)count, out var transferred);
				var read = checked((int)transferred.ToUInt64());
				Guard.Ensure(read <= count, "Windows returned an invalid memory-read length.");
				if (read > 0) {
					scanner.Feed(buffer, read, address, checkpoint);
					examined += read;
					address += (uint)read;
				}
				if (!success || read == 0) {
					skipped++;
					address = end;
					break;
				}
			}
			scanner.EndRegion();
		}
		checkpoint?.Invoke();
		return new WindowsProcessStringScan { Strings = scanner.Strings, BytesExamined = examined, SkippedRegions = skipped, Truncated = scanner.IsFull || examined >= maximumBytes };
	}

	private static void ValidateStringLimits(int minimumLength, int maximumResults, long maximumBytes) {
		Guard.ArgumentInRange(minimumLength, 2, 4096, nameof(minimumLength));
		Guard.ArgumentInRange(maximumResults, 1, 100000, nameof(maximumResults));
		Guard.ArgumentGT(maximumBytes, 0, nameof(maximumBytes));
	}

	private WinAPI.KERNEL32.KernelHandle OpenReadHandle() {
		var identity = Identity;
		Guard.Ensure(identity.IsVerified && !HasExited, "The process is no longer available for memory inspection.");
		var handle = WinAPI.KERNEL32.OpenProcess(0x0400 | 0x0010, false, (uint)ProcessId);
		if (handle.IsInvalid) {
			var error = Marshal.GetLastWin32Error();
			handle.Dispose();
			throw new Win32Exception(error);
		}
		var transferred = false;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => { if (!transferred) handle.Dispose(); });
		if (!WinAPI.KERNEL32.GetProcessTimes(handle, out var creation, out _, out _, out _))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		Guard.Ensure(DateTime.FromFileTimeUtc(Ticks(creation)) == identity.StartTimeUtc, "The process ID was reused before memory inspection.");
		transferred = true;
		return handle;
	}

	private static int GetTargetPointerSize(WinAPI.KERNEL32.KernelHandle handle) {
		if (!WinAPI.KERNEL32.IsWow64Process(handle, out var wow64))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		var size = Environment.Is64BitOperatingSystem && !wow64 ? 8 : 4;
		if (size > IntPtr.Size)
			throw new NotSupportedException("A 32-bit inspector cannot read a native 64-bit process. Run the 64-bit application.");
		return size;
	}

	private static WinAPI.KERNEL32.MEMORY_BASIC_INFORMATION QueryMemory(WinAPI.KERNEL32.KernelHandle handle, ulong address) {
		if (WinAPI.KERNEL32.VirtualQueryEx(handle, ToPointer(address), out var region, (UIntPtr)(uint)Marshal.SizeOf<WinAPI.KERNEL32.MEMORY_BASIC_INFORMATION>()) == UIntPtr.Zero)
			throw new Win32Exception(Marshal.GetLastWin32Error());
		return region;
	}

	private static bool IsReadable(WinAPI.KERNEL32.MEMORY_BASIC_INFORMATION region) => region.State == 0x1000 && (region.Protect & 0x101) == 0
		&& (region.Protect & 0xEE) != 0;

	private static byte[] ReadRemoteExact(WinAPI.KERNEL32.KernelHandle handle, ulong address, int count) {
		var buffer = new byte[count];
		var success = WinAPI.KERNEL32.ReadProcessMemory(handle, ToPointer(address), buffer, (UIntPtr)(uint)count, out var transferred);
		var error = success ? 299 : Marshal.GetLastWin32Error();
		if (!success || transferred.ToUInt64() != (uint)count)
			throw new Win32Exception(error);
		return buffer;
	}

	private static ulong ReadRemotePointer(WinAPI.KERNEL32.KernelHandle handle, ulong address, int pointerSize) => ReadPointer(ReadRemoteExact(handle, address, pointerSize), 0, pointerSize);

	private static ulong ReadPointer(byte[] buffer, int offset, int pointerSize) => pointerSize == 8 ? EndianBitConverter.Little.ToUInt64(buffer, offset) : EndianBitConverter.Little.ToUInt32(buffer, offset);

	private static ulong ToAddress(IntPtr pointer) => IntPtr.Size == 8 ? unchecked((ulong)pointer.ToInt64()) : unchecked((uint)pointer.ToInt32());

	private static IntPtr ToPointer(ulong address) => IntPtr.Size == 8 ? new IntPtr(unchecked((long)address)) : new IntPtr(unchecked((int)address));

	private static void CheckNtStatus(int status) {
		if (status < 0)
			throw new Win32Exception(unchecked((int)WinAPI.NTDLL.RtlNtStatusToDosError(status)));
	}

	private Process OpenManaged() {
		var identity = Identity;
		Guard.Ensure(identity.IsVerified && !HasExited, "The process identity is unavailable or has exited.");
		var process = Process.GetProcessById(ProcessId);
		try {
			Guard.Ensure(process.StartTime.ToUniversalTime() == identity.StartTimeUtc, "The process ID has been reused.");
			return process;
		} catch {
			process.Dispose();
			throw;
		}
	}

	private void EnsureOpen() => ObjectDisposedException.ThrowIf(_handle.IsClosed, this);

	private static long Ticks(WinAPI.KERNEL32.FILETIME value) => unchecked((long)(((ulong)value.DateTimeHigh << 32) | value.DateTimeLow));

	private static T Read<T>(Func<T> read, T fallback) {
		try { return read(); }
		catch (Exception error) when (error is Win32Exception or InvalidOperationException or NotSupportedException or ArgumentException or UnauthorizedAccessException or SecurityException) { return fallback; }
	}
}
