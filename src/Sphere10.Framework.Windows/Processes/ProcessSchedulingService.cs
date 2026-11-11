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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Explicit retained-handle scheduling operations. CPU Sets are soft placement preferences; affinity remains a hard primary-group restriction.</summary>
public static class ProcessSchedulingService {
	public static WindowsProcessScheduling Read(ProcessIdentity identity) => ProcessCommandService.ExecuteNative(identity, 0, handle => ReadCore(identity, handle), null);

	public static Task<WindowsProcessScheduling> ReadAsync(ProcessIdentity identity, CancellationToken cancellationToken = default)
		=> Task.Run(() => ProcessCommandService.ExecuteNative(identity, 0, handle => ReadCore(identity, handle), cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static Task SetAffinityAsync(ProcessIdentity identity, ulong affinityMask, CancellationToken cancellationToken = default) {
		return Task.Run(() => ProcessCommandService.ExecuteNative(identity, WinAPI.KERNEL32.ProcessSetInformation, handle => {
			Guard.Argument(affinityMask > 0 && (IntPtr.Size == 8 || affinityMask <= uint.MaxValue), nameof(affinityMask), "Select a nonempty mask representable by this host.");
			if (!WinAPI.KERNEL32.GetProcessAffinityMask(handle, out var current, out var system))
				throw new Win32Exception(Marshal.GetLastPInvokeError());
			Guard.Ensure(current != UIntPtr.Zero && system != UIntPtr.Zero, "This process spans unsupported processor groups. Use CPU Sets instead of a primary-group mask.");
			Guard.Argument((affinityMask & ~system.ToUInt64()) == 0, nameof(affinityMask), "The affinity requests a logical processor outside the current system mask.");
			cancellationToken.ThrowIfCancellationRequested();
			if (!WinAPI.KERNEL32.SetProcessAffinityMask(handle, (UIntPtr)affinityMask))
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows rejected the process affinity.");
			return true;
		}, cancellationToken.ThrowIfCancellationRequested), cancellationToken);
	}

	public static Task SetDefaultCpuSetsAsync(ProcessIdentity identity, uint[] ids, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(ids, nameof(ids));
		var requested = ids.Distinct().ToArray();
		return Task.Run(() => ProcessCommandService.ExecuteNative(identity, WinAPI.KERNEL32.ProcessSetLimitedInformation, handle => {
			var allowed = ReadSystemCpuSets(handle).Where(set => !set.Allocated || set.AllocatedToTargetProcess).Select(set => set.Id).ToHashSet();
			Guard.Argument(requested.All(allowed.Contains), nameof(ids), "One or more CPU Set IDs are unavailable or exclusively reserved for another process.");
			cancellationToken.ThrowIfCancellationRequested();
			if (!WinAPI.KERNEL32.SetProcessDefaultCpuSets(handle, requested.Length == 0 ? null : requested, (uint)requested.Length))
				throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows rejected the process default CPU Sets.");
			return true;
		}, cancellationToken.ThrowIfCancellationRequested), cancellationToken);
	}

	private static WindowsProcessScheduling ReadCore(ProcessIdentity identity, WinAPI.KERNEL32.KernelHandle handle) {
		var result = new WindowsProcessScheduling { Identity = identity, CapturedAtUtc = DateTime.UtcNow };
		if (!WinAPI.KERNEL32.GetProcessAffinityMask(handle, out var process, out var system))
			result = result with { AffinityAvailability = new Win32Exception(Marshal.GetLastPInvokeError()).Message };
		else if (process == UIntPtr.Zero || system == UIntPtr.Zero)
			result = result with { AffinityAvailability = "A Primary-Group Affinity Mask Is Unavailable For This Multi-Group Process." };
		else
			result = result with { AffinityMask = process.ToUInt64(), SystemAffinityMask = system.ToUInt64() };
		Tools.Exceptions.ExecuteCapturingException(() => result = result with { DefaultCpuSetIds = ReadDefaultCpuSets(handle), AvailableCpuSets = ReadSystemCpuSets(handle) },
			out var error, captured => captured is Win32Exception or EntryPointNotFoundException);
		if (error != null)
			result = result with { CpuSetAvailability = error.Message };
		return result;
	}

	private static uint[] ReadDefaultCpuSets(WinAPI.KERNEL32.KernelHandle handle) {
		for (var attempt = 0; attempt < 3; attempt++) {
			if (!WinAPI.KERNEL32.GetProcessDefaultCpuSets(handle, null, 0, out var required) && Marshal.GetLastPInvokeError() != 122)
				throw new Win32Exception(Marshal.GetLastPInvokeError());
			Guard.Ensure(required <= 65536, "Windows returned an excessive CPU Set count.");
			if (required == 0)
				return Array.Empty<uint>();
			var ids = new uint[required];
			if (WinAPI.KERNEL32.GetProcessDefaultCpuSets(handle, ids, (uint)ids.Length, out required)) {
				Guard.Ensure(required <= ids.Length, "Windows returned a CPU Set count outside its buffer.");
				return ids.Take((int)required).ToArray();
			}
			if (Marshal.GetLastPInvokeError() != 122)
				throw new Win32Exception(Marshal.GetLastPInvokeError());
		}
		throw new InvalidOperationException("The process CPU Set list changed repeatedly. Read it again.");
	}

	private static WindowsCpuSet[] ReadSystemCpuSets(WinAPI.KERNEL32.KernelHandle handle) {
		for (var attempt = 0; attempt < 3; attempt++) {
			if (!WinAPI.KERNEL32.GetSystemCpuSetInformation(IntPtr.Zero, 0, out var required, handle, 0) && Marshal.GetLastPInvokeError() != 122)
				throw new Win32Exception(Marshal.GetLastPInvokeError());
			Guard.Ensure(required <= 4 * 1024 * 1024, "Windows returned excessive CPU Set information.");
			if (required == 0)
				return Array.Empty<WindowsCpuSet>();
			using var buffer = new UnmanagedMemoryScope((int)required);
			if (!WinAPI.KERNEL32.GetSystemCpuSetInformation(buffer.Item, (uint)buffer.Size, out var returned, handle, 0)) {
				if (Marshal.GetLastPInvokeError() == 122)
					continue;
				throw new Win32Exception(Marshal.GetLastPInvokeError());
			}
			if (returned > buffer.Size)
				throw new InvalidDataException("The CPU Set information exceeds its native buffer.");
			var result = new List<WindowsCpuSet>();
			for (var offset = 0; offset < returned;) {
				if (returned - offset < 8)
					throw new InvalidDataException("A CPU Set header is truncated.");
				var entry = IntPtr.Add(buffer.Item, offset);
				var size = Marshal.ReadInt32(entry);
				if (size < 8 || size > returned - offset)
					throw new InvalidDataException("A CPU Set entry exceeds its native buffer.");
				if (Marshal.ReadInt32(entry, 4) == 0) {
					if (size < 32)
						throw new InvalidDataException("A CPU Set record is truncated.");
					var flags = Marshal.ReadByte(entry, 19);
					result.Add(new WindowsCpuSet(unchecked((uint)Marshal.ReadInt32(entry, 8)), unchecked((ushort)Marshal.ReadInt16(entry, 12)),
						Marshal.ReadByte(entry, 14), Marshal.ReadByte(entry, 15), Marshal.ReadByte(entry, 18), (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0));
				}
				offset += size;
			}
			return result.ToArray();
		}
		throw new InvalidOperationException("The system CPU Set information changed repeatedly. Read it again.");
	}
}
