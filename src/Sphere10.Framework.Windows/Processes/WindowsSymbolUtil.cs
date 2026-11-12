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
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Runtime;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Bounded x64 PSS unwinding and detached historical address resolution. Symbols default to local image directories.</summary>
	public static class Symbols {
		public static Task<WindowsThreadStack> CaptureStackAsync(ThreadIdentity identity, int maximumFrames = 128, string symbolSearchPath = null, CancellationToken cancellationToken = default) {
			Guard.ArgumentInRange(maximumFrames, 1, 512, nameof(maximumFrames));
			return Task.Run(() => Threads.WithThread(identity, 0, _ => {
				using var snapshot = WindowsDiagnosticInfo.Capture(identity.Process, LocalDiagnosticFileLocator.CreateOptions(), cancellationToken);
				return Capture(snapshot, identity, maximumFrames, symbolSearchPath, cancellationToken);
			}, cancellationToken.ThrowIfCancellationRequested), cancellationToken);
		}

		/// <summary>Resolves retained addresses only against caller-supplied historical image mappings and matching local PE files. Never opens a live process.</summary>
		public static Task<WindowsResolvedAddresses> ResolveAddressesAsync(ulong[] addresses, WindowsSymbolModule[] modules,
			string symbolSearchPath = null, CancellationToken cancellationToken = default) {
			Guard.ArgumentNotNull(addresses, nameof(addresses));
			Guard.ArgumentNotNull(modules, nameof(modules));
			Guard.ArgumentInRange(addresses.Length, 1, 512, nameof(addresses));
			Guard.ArgumentInRange(modules.Length, 0, 20000, nameof(modules));
			var capturedAddresses = (ulong[])addresses.Clone();
			var capturedModules = (WindowsSymbolModule[])modules.Clone();
			return Task.Run(() => ResolveDetached(capturedAddresses, capturedModules, symbolSearchPath, cancellationToken), cancellationToken);
		}

		internal static WindowsResolvedAddresses ReadDumpStack(IDataReader reader, uint threadId, int maximumFrames, CancellationToken cancellationToken) {
			Debug.AssertNotMainThread();
			Guard.Ensure(reader.Architecture == Architecture.X64, "Offline native unwinding currently supports x64 user-mode dumps.");
			var context = new byte[4096];
			Guard.Ensure(reader.GetThreadContext(threadId, 0x10001f, context), "The dump does not contain this thread's register context.");
			using var sessionMemory = new UnmanagedMemoryScope(1);
			var session = sessionMemory.Item;
			return WalkCapturedContext(reader, context, reader.EnumerateModules().Take(20000).ToArray(), session, maximumFrames, 1024 * 1024, null, cancellationToken);
		}

		private static WindowsResolvedAddresses ResolveDetached(ulong[] addresses, WindowsSymbolModule[] modules, string symbolSearchPath, CancellationToken token) {
			Debug.AssertNotMainThread();
			var issues = new List<string>();
			var files = new List<FileStream>();
			var accepted = new List<WindowsSymbolModule>();
			using var release = Tools.Scope.ExecuteOnDispose(() => {
				foreach (var file in files)
					file.Dispose();
			});
			var relevant = modules.Where(module => module != null && module.BaseAddress > 0 && module.ImageSize > 0
				&& module.ImageSize - 1 <= ulong.MaxValue - module.BaseAddress
				&& addresses.Any(address => address >= module.BaseAddress && address - module.BaseAddress < module.ImageSize)).ToArray();
			foreach (var module in relevant.Take(512)) {
				token.ThrowIfCancellationRequested();
				var overlapping = relevant.Any(other => !ReferenceEquals(other, module) && (other.BaseAddress >= module.BaseAddress
					? other.BaseAddress - module.BaseAddress < module.ImageSize : module.BaseAddress - other.BaseAddress < other.ImageSize));
				if (overlapping) {
					issues.Add($"0x{module.BaseAddress:X}: captured module mappings overlap; no symbol file was admitted.");
					continue;
				}

				Tools.Exceptions.ExecuteCapturingException(() => {
					Guard.Ensure(module.TimeDateStamp.HasValue, "The capture contains no PE timestamp; file identity cannot be matched.");
					Guard.Ensure(LocalDiagnosticFileLocator.IsLocalPath(module.ImagePath), "Only explicit local image paths are admitted by default.");
					var file = new FileStream(module.ImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
					var retained = false;
					using var reject = Tools.Scope.ExecuteOnDispose(() => {
						if (!retained)
							file.Dispose();
					});
					Guard.Ensure(file.Length <= 512L * 1024 * 1024, "The matching symbol image exceeds the 512 MiB local-image limit.");
					using var reader = new PEReader(file, PEStreamOptions.LeaveOpen);
					var headers = reader.PEHeaders;
					Guard.Ensure(headers.PEHeader != null && unchecked((uint)headers.CoffHeader.TimeDateStamp) == module.TimeDateStamp
						&& (uint)headers.PEHeader.SizeOfImage == module.ImageSize && (!module.Checksum.HasValue || headers.PEHeader.CheckSum == module.Checksum),
						"The local PE timestamp, image size or supplied checksum differs from the captured image.");
					files.Add(file);
					accepted.Add(module);
					retained = true;
				}, out var error, error => error is IOException or UnauthorizedAccessException or InvalidOperationException or BadImageFormatException or NotSupportedException or ArgumentException);
				if (error != null)
					issues.Add($"{Path.GetFileName(module.ImagePath)} @ 0x{module.BaseAddress:X}: {error.Message}");
			}

			if (relevant.Length > 512)
				issues.Add("Only the first 512 relevant captured module mappings were examined.");
			var paths = accepted.Select(module => Path.GetDirectoryName(module.ImagePath)).Distinct(StringComparer.OrdinalIgnoreCase);
			var searchPath = symbolSearchPath ?? string.Join(";", paths);
			if (string.IsNullOrWhiteSpace(searchPath))
				searchPath = System.Environment.SystemDirectory;
			var frames = new List<WindowsStackFrame>();
			using (WinAPI.DBGHELP.SyncRoot.EnterWriteScope()) {
				token.ThrowIfCancellationRequested();
				// DbgHelp permits a unique nonzero session identifier when invadeProcess is false. This allocated identity cannot refer to a target PID.
				using var contextMemory = new UnmanagedMemoryScope(1);
				var context = contextMemory.Item;
				var previous = WinAPI.DBGHELP.SymGetOptions();
				WinAPI.DBGHELP.SymSetOptions(0x2 | 0x4 | 0x10 | 0x80 | 0x200 | 0x1000 | 0x80000);
				using var restore = Tools.Scope.ExecuteOnDispose(() => WinAPI.DBGHELP.SymSetOptions(previous));
				if (!WinAPI.DBGHELP.SymInitialize(context, searchPath, false))
					throw new Win32Exception(Marshal.GetLastPInvokeError(), "Initialize detached symbol context");
				using var cleanup = Tools.Scope.ExecuteOnDispose(() => WinAPI.DBGHELP.SymCleanup(context));
				var loaded = new List<WindowsSymbolModule>();
				foreach (var module in accepted) {
					token.ThrowIfCancellationRequested();
					if (WinAPI.DBGHELP.SymLoadModuleEx(context, IntPtr.Zero, module.ImagePath, Path.GetFileName(module.ImagePath), module.BaseAddress, module.ImageSize, IntPtr.Zero, 0) != 0)
						loaded.Add(module);
					else
						issues.Add($"{Path.GetFileName(module.ImagePath)}: DbgHelp could not admit the matching local image (Windows Error {Marshal.GetLastPInvokeError()}).");
				}

				foreach (var address in addresses) {
					token.ThrowIfCancellationRequested();
					var module = loaded.SingleOrDefault(item => address >= item.BaseAddress && address - item.BaseAddress < item.ImageSize);
					frames.Add(module == null ? new WindowsStackFrame(address, 0, 0, string.Empty, string.Empty, 0, null, null)
						: ResolveAddress(context, new WinAPI.DBGHELP.STACKFRAME64 { AddrPC = Address(address) }, module.ImagePath, module.BaseAddress));
				}
			}

			token.ThrowIfCancellationRequested();
			return new WindowsResolvedAddresses(DateTime.UtcNow, frames.ToArray(), issues.ToArray());
		}

		private static WindowsThreadStack Capture(WindowsDiagnosticInfo snapshot, ThreadIdentity identity, int maximumFrames, string symbolSearchPath, CancellationToken cancellationToken) {
			Debug.AssertNotMainThread();
			var reader = snapshot.Target.DataReader;
			Guard.Ensure(reader.Architecture == Architecture.X64, "Native unwinding currently supports x64 user-mode stacks.");
			var context = new byte[4096];
			Guard.Ensure(snapshot.ThreadCreationTimes.TryGetValue((uint)identity.ThreadId, out var started) && started == identity.StartTimeUtc, "The captured thread identity differs from the selected thread instance.");
			Guard.Ensure(reader.GetThreadContext((uint)identity.ThreadId, 0x10001f, context), "The thread has no captured context in this snapshot.");
			var modules = reader.EnumerateModules().Take(20000).ToArray();
			var result = WalkCapturedContext(reader, context, modules, snapshot.CloneHandle, maximumFrames, 16 * 1024 * 1024, symbolSearchPath, cancellationToken);
			return new WindowsThreadStack(identity, snapshot.CapturedAtUtc, result.Frames, result.Frames.Length == maximumFrames || result.Issues.Length > 0, result.Issues);
		}

		private static WindowsResolvedAddresses WalkCapturedContext(IDataReader reader, byte[] context, ModuleInfo[] modules, IntPtr process,
			int maximumFrames, long maximumReadBytes, string symbolSearchPath, CancellationToken cancellationToken) {
			var paths = modules.Select(module => Path.GetDirectoryName(module.FileName)).Where(LocalDiagnosticFileLocator.IsLocalPath).Distinct(StringComparer.OrdinalIgnoreCase);
			var searchPath = symbolSearchPath ?? string.Join(";", paths);
			if (string.IsNullOrWhiteSpace(searchPath))
				searchPath = System.Environment.SystemDirectory;
			var frames = new List<WindowsStackFrame>();
			var issues = new List<string>();
			var memoryLimitReached = false;
			using (WinAPI.DBGHELP.SyncRoot.EnterWriteScope()) {
				cancellationToken.ThrowIfCancellationRequested();
				var previousOptions = WinAPI.DBGHELP.SymGetOptions();
				// Ignore environment/embedded PDB paths. Network lookup is possible only through an explicitly supplied search path.
				WinAPI.DBGHELP.SymSetOptions(0x2 | 0x4 | 0x10 | 0x80 | 0x200 | 0x1000 | 0x80000);
				using var restore = Tools.Scope.ExecuteOnDispose(() => WinAPI.DBGHELP.SymSetOptions(previousOptions));
				if (!WinAPI.DBGHELP.SymInitialize(process, searchPath, false))
					throw new Win32Exception(Marshal.GetLastPInvokeError(), "Initialize local symbols");
				using var cleanup = Tools.Scope.ExecuteOnDispose(() => WinAPI.DBGHELP.SymCleanup(process));
				foreach (var module in modules) {
					cancellationToken.ThrowIfCancellationRequested();
					var localName = new LocalDiagnosticFileLocator(System.Array.Empty<string>()).FindPEImage(module.FileName, module.IndexTimeStamp, module.IndexFileSize, true);
					WinAPI.DBGHELP.SymLoadModuleEx(process, IntPtr.Zero, localName, Path.GetFileName(module.FileName), module.ImageBase, checked((uint)Math.Min(uint.MaxValue, module.ImageSize)), IntPtr.Zero, localName == null ? 1u : 0u);
				}

				using var contextMemory = new UnmanagedMemoryScope(context.Length);
				var contextPointer = contextMemory.Item;
				Marshal.Copy(context, 0, contextPointer, context.Length);
				var frame = new WinAPI.DBGHELP.STACKFRAME64 {
					AddrPC = Address(EndianBitConverter.Little.ToUInt64(context, 248)),
					AddrStack = Address(EndianBitConverter.Little.ToUInt64(context, 152)),
					AddrFrame = Address(EndianBitConverter.Little.ToUInt64(context, 160))
				};
				var remainingReadBytes = maximumReadBytes;
				WinAPI.DBGHELP.ReadMemoryRoutine read = (IntPtr _, ulong address, IntPtr buffer, uint length, out uint readCount) => {
					readCount = 0;
					if (cancellationToken.IsCancellationRequested)
						return false;
					if (length > 1024 * 1024 || length > remainingReadBytes) {
						memoryLimitReached = true;
						return false;
					}
					remainingReadBytes -= length;
					var bytes = new byte[(int)length];
					var count = reader.Read(address, bytes);
					if (count > 0)
						Marshal.Copy(bytes, 0, buffer, count);
					readCount = (uint)count;
					return count == length;
				};
				for (var index = 0; index <= maximumFrames && frames.Count < maximumFrames; index++) {
					cancellationToken.ThrowIfCancellationRequested();
					if (frame.AddrPC.Offset == 0)
						break;
					var repeated = frames.Count > 0 && frames[^1].InstructionPointer == frame.AddrPC.Offset && frames[^1].StackPointer == frame.AddrStack.Offset;
					if (repeated && index > 1) {
						issues.Add("Unwinding stopped at a repeated instruction/stack address.");
						break;
					}

					if (!repeated)
						frames.Add(Resolve(process, frame, modules));
					if (!WinAPI.DBGHELP.StackWalk64(0x8664, process, IntPtr.Zero, ref frame, contextPointer, read, WinAPI.DBGHELP.SymFunctionTableAccess64, WinAPI.DBGHELP.SymGetModuleBase64, IntPtr.Zero))
						break;
				}

				GC.KeepAlive(read);
			}

			cancellationToken.ThrowIfCancellationRequested();
			if (memoryLimitReached)
				issues.Add($"Unwinding reached its {maximumReadBytes:N0}-byte captured-memory budget or one MiB native-read limit.");
			if (frames.Count == maximumFrames)
				issues.Add("Unwinding reached the configured frame limit.");
			return new WindowsResolvedAddresses(DateTime.UtcNow, frames.ToArray(), issues.ToArray());
		}

		private static WinAPI.DBGHELP.ADDRESS64 Address(ulong value) {
			return new() {
				Offset = value,
				Mode = 3
			};
		}

		private static WindowsStackFrame Resolve(IntPtr process, WinAPI.DBGHELP.STACKFRAME64 frame, ModuleInfo[] modules) {
			var address = frame.AddrPC.Offset;
			var module = modules.LastOrDefault(item => address >= item.ImageBase && item.ImageSize > 0 && address - item.ImageBase < (ulong)item.ImageSize);
			return ResolveAddress(process, frame, module?.FileName, module?.ImageBase);
		}

		private static WindowsStackFrame ResolveAddress(IntPtr process, WinAPI.DBGHELP.STACKFRAME64 frame, string modulePath, ulong? moduleBase) {
			var address = frame.AddrPC.Offset;
			var symbol = string.Empty;
			ulong displacement = 0;
			using var symbolMemory = new UnmanagedMemoryScope(88 + 2048 * 2);
			var buffer = symbolMemory.Item;
			Marshal.WriteInt32(buffer, 88);
			Marshal.WriteInt32(buffer, 80, 2048);
			if (WinAPI.DBGHELP.SymFromAddr(process, address, out displacement, buffer)) {
				var count = Math.Clamp(Marshal.ReadInt32(buffer, 76), 0, 2048);
				symbol = Marshal.PtrToStringUni(buffer + 84, count);
			}

			var line = new WinAPI.DBGHELP.IMAGEHLP_LINE64 {
				SizeOfStruct = (uint)Marshal.SizeOf<WinAPI.DBGHELP.IMAGEHLP_LINE64>()
			};
			var hasLine = WinAPI.DBGHELP.SymGetLineFromAddr(process, address, out _, ref line);
			return new WindowsStackFrame(
				address, frame.AddrStack.Offset, frame.AddrFrame.Offset, Path.GetFileName(modulePath), symbol, displacement,
				hasLine ? Marshal.PtrToStringUni(line.FileName) : null, hasLine ? line.LineNumber : null
			) {
				ModulePath = modulePath,
				ModuleBaseAddress = moduleBase
			};
		}
	}
}
