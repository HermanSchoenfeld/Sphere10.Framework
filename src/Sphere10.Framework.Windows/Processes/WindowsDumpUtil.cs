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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Runtime;

namespace Sphere10.Framework.Windows;

/// <summary>Explicit user-mode dump creation in the caller's isolated diagnostic host, sharing the serialized DbgHelp backend.</summary>
public sealed class WindowsDumpUtil {
	public const string WorkerArgument = "--sphere10-capture-dump";

	public WindowsProcessWorkerHost WorkerHost { get; set; }

	/// <summary>Reads only bytes actually retained in one validated dump range. No live memory or local image substitution is used.</summary>
	public Task<byte[]> ReadMemoryAsync(WindowsDumpInfo dump, ulong address, int maximumBytes = 65536, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(dump, nameof(dump));
		Guard.ArgumentInRange(maximumBytes, 1, 65536, nameof(maximumBytes));
		return Task.Run(() => {
			Debug.AssertNotMainThread();
			cancellationToken.ThrowIfCancellationRequested();
			using var stream = new FileStream(dump.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			Guard.Ensure(stream.Length == dump.FileLength && File.GetLastWriteTimeUtc(dump.FilePath) == dump.LastWriteTimeUtc, "The dump changed; reopen it before reading captured memory.");
			var ranges = dump.MemoryRanges.Where(range => address >= range.Address && address - range.Address < range.Size).ToArray();
			Guard.Ensure(ranges.Length == 1, "The address has no unique retained memory mapping in this dump.");
			var range = ranges[0];
			var displacement = address - range.Address;
			var length = (int)Math.Min((ulong)maximumBytes, range.Size - displacement);
			var offset = checked(range.FileOffset + (long)displacement);
			Guard.Ensure(offset >= 0 && offset <= stream.Length - length, "The captured memory range extends beyond the dump file.");
			stream.Position = offset;
			var result = new byte[length];
			stream.ReadExactly(result);
			cancellationToken.ThrowIfCancellationRequested();
			return result;
		}, cancellationToken);
	}

	internal static (WindowsDumpInfo Dump, ManagedDiagnosticEntry[] Entries, bool Truncated, string[] Issues) ReadNative(
		string path, IDataReader reader, ManagedInspectionOptions options, CancellationToken token, int occupiedEntries = 0) {
		Debug.AssertNotMainThread();
		token.ThrowIfCancellationRequested();
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		using var binary = new EndianBinaryReader(EndianBitConverter.Little, stream, leaveOpen: true);
		var entries = new List<ManagedDiagnosticEntry>();
		var ranges = new List<WindowsDumpMemoryRange>();
		var threads = new List<uint>();
		var issues = new List<string>();
		var truncated = false;
		var characters = 0;
		var maximumEntries = Math.Max(0, options.MaximumEntries - occupiedEntries);
		void Add(string kind, string name, ulong? address, string value) {
			token.ThrowIfCancellationRequested();
			if (entries.Count >= maximumEntries || characters >= 2 * 1024 * 1024) {
				truncated = true;
				return;
			}
			var originalNameLength = name.Length;
			var originalValueLength = value.Length;
			name = name[..Math.Min(name.Length, Math.Min(4096, 2 * 1024 * 1024 - characters))];
			characters += name.Length;
			value = value[..Math.Min(value.Length, Math.Min(4096, 2 * 1024 * 1024 - characters))];
			characters += value.Length;
			truncated |= originalNameLength != name.Length || originalValueLength != value.Length;
			entries.Add(new ManagedDiagnosticEntry("Native Dump", kind, name, address, value));
		}
		void Seek(long offset, long length) {
			Guard.Ensure(offset >= 0 && length >= 0 && offset <= stream.Length && length <= stream.Length - offset, "A minidump stream extends beyond its file.");
			stream.Position = offset;
		}
		void AddMemory(ulong address, ulong size, ulong fileOffset) {
			Guard.Ensure(fileOffset <= long.MaxValue && size <= long.MaxValue && size <= ulong.MaxValue - address, "A minidump memory mapping overflows its address or file range.");
			Guard.Ensure(fileOffset <= (ulong)stream.Length && size <= (ulong)stream.Length - fileOffset, "Captured memory extends beyond the dump file.");
			if (ranges.Count >= maximumEntries) {
				truncated = true;
				return;
			}
			ranges.Add(new WindowsDumpMemoryRange(address, size, (long)fileOffset));
			Add("Native Memory", "Captured Bytes", address, $"{size:N0} bytes; Dump Offset 0x{fileOffset:X}");
		}
		Seek(0, 32);
		if (binary.ReadUInt32() != 0x504d444d)
			return (null, Array.Empty<ManagedDiagnosticEntry>(), false, new[] { "Native stream inspection supports Windows minidumps; this artifact uses another dump format." });
		if (maximumEntries == 0)
			return (new WindowsDumpInfo(Path.GetFullPath(path), stream.Length, File.GetLastWriteTimeUtc(path), Array.Empty<WindowsDumpMemoryRange>()),
				Array.Empty<ManagedDiagnosticEntry>(), true, new[] { "Managed records exhausted the shared entry budget; no native records or memory mappings were retained." });
		binary.ReadUInt32();
		var streamCount = binary.ReadUInt32();
		var directoryOffset = binary.ReadUInt32();
		Guard.Ensure(streamCount <= 4096, "The minidump exceeds the stream directory limit.");
		Seek(directoryOffset, streamCount * 12L);
		var directories = new Dictionary<uint, (uint Offset, uint Size)>();
		for (var index = 0; index < streamCount; index++) {
			var kind = binary.ReadUInt32();
			var size = binary.ReadUInt32();
			var offset = binary.ReadUInt32();
			Guard.Ensure(offset <= stream.Length && size <= stream.Length - offset, "A minidump directory extends beyond its file.");
			// Writers reserve several UnusedStream directory slots; these are valid duplicates.
			if (kind != 0)
				Guard.Ensure(directories.TryAdd(kind, (offset, size)), "A minidump directory has duplicate stream types.");
		}
		Add("Native Capture", "Architecture", null, reader.Architecture.ToString());
		if (directories.TryGetValue(4, out var moduleList)) {
			Seek(moduleList.Offset, moduleList.Size);
			var count = binary.ReadUInt32();
			Guard.Ensure(moduleList.Size >= 4 && count <= (moduleList.Size - 4) / 108, "Invalid minidump module list length.");
			for (var index = 0u; index < count && index < maximumEntries; index++) {
				Seek((long)moduleList.Offset + 4 + index * 108L, 108);
				var address = binary.ReadUInt64();
				var size = binary.ReadUInt32();
				var checksum = binary.ReadUInt32();
				var timestamp = binary.ReadUInt32();
				var nameOffset = binary.ReadUInt32();
				Seek(nameOffset, 4);
				var nameBytes = binary.ReadUInt32();
				Guard.Ensure((nameBytes & 1) == 0, "Invalid UTF-16 minidump module name length.");
				Seek((long)nameOffset + 4, nameBytes);
				truncated |= nameBytes > 8192;
				var name = Encoding.Unicode.GetString(binary.ReadBytes((int)Math.Min(nameBytes, 8192)));
				Add("Native Module", name, address, $"{size:N0} bytes; PE Timestamp 0x{timestamp:X8}; Checksum 0x{checksum:X8}");
			}
			truncated |= count > maximumEntries;
		}
		if (directories.TryGetValue(3, out var threadList)) {
			Seek(threadList.Offset, threadList.Size);
			var count = binary.ReadUInt32();
			Guard.Ensure(threadList.Size >= 4 && count <= (threadList.Size - 4) / 48, "Invalid minidump thread list length.");
			for (var index = 0u; index < count && index < maximumEntries; index++) {
				Seek((long)threadList.Offset + 4 + index * 48L, 48);
				var id = binary.ReadUInt32();
				var suspended = binary.ReadUInt32();
				var priorityClass = binary.ReadUInt32();
				var priority = binary.ReadUInt32();
				var teb = binary.ReadUInt64();
				var stack = binary.ReadUInt64();
				var stackSize = binary.ReadUInt32();
				threads.Add(id);
				Add("Native Thread", $"OS Thread {id}", teb, $"Priority {priority}; Class 0x{priorityClass:X}; Suspend Count {suspended}; Stack 0x{stack:X} ({stackSize:N0} bytes)");
			}
			truncated |= count > maximumEntries;
		}
		if (directories.TryGetValue(6, out var exceptionStream)) {
			Guard.Ensure(exceptionStream.Size >= 168, "Invalid minidump exception stream length.");
			Seek(exceptionStream.Offset, exceptionStream.Size);
			var thread = binary.ReadUInt32();
			binary.ReadUInt32();
			var code = binary.ReadUInt32();
			var flags = binary.ReadUInt32();
			var record = binary.ReadUInt64();
			var address = binary.ReadUInt64();
			var count = binary.ReadUInt32();
			binary.ReadUInt32();
			Guard.Ensure(count <= 15, "Invalid native exception parameter count.");
			var parameters = Enumerable.Range(0, (int)count).Select(_ => $"0x{binary.ReadUInt64():X}");
			Add("Native Exception", $"0x{code:X8}", address, $"OS Thread {thread}; Flags 0x{flags:X}; Nested Record 0x{record:X}; Parameters {string.Join(", ", parameters)}");
		} else
			Add("Native Exception", "Not Recorded", null, "No exception stream was recorded; an explicit snapshot dump need not contain a crashing exception.");
		if (directories.TryGetValue(9, out var memory64)) {
			Seek(memory64.Offset, memory64.Size);
			Guard.Ensure(memory64.Size >= 16, "Invalid minidump memory64 list length.");
			var count = binary.ReadUInt64();
			var offset = binary.ReadUInt64();
			Guard.Ensure(count <= (memory64.Size - 16) / 16, "Invalid minidump memory64 descriptor count.");
			for (var index = 0UL; index < count && index < (ulong)maximumEntries; index++) {
				var address = binary.ReadUInt64();
				var size = binary.ReadUInt64();
				AddMemory(address, size, offset);
				offset = checked(offset + size);
			}
			truncated |= count > (ulong)maximumEntries;
		} else if (directories.TryGetValue(5, out var memory32)) {
			Seek(memory32.Offset, memory32.Size);
			var count = binary.ReadUInt32();
			Guard.Ensure(memory32.Size >= 4 && count <= (memory32.Size - 4) / 16, "Invalid minidump memory descriptor count.");
			for (var index = 0u; index < count && index < maximumEntries; index++)
				AddMemory(binary.ReadUInt64(), binary.ReadUInt32(), binary.ReadUInt32());
			truncated |= count > maximumEntries;
		}
		if (directories.TryGetValue(16, out var memoryInfo)) {
			Seek(memoryInfo.Offset, memoryInfo.Size);
			Guard.Ensure(memoryInfo.Size >= 16, "Invalid minidump memory-info header.");
			var headerSize = binary.ReadUInt32();
			var entrySize = binary.ReadUInt32();
			var count = binary.ReadUInt64();
			Guard.Ensure(headerSize >= 16 && headerSize <= memoryInfo.Size && entrySize >= 48 && entrySize <= 4096
				&& count <= (memoryInfo.Size - headerSize) / entrySize, "Invalid minidump memory-info descriptor count.");
			for (var index = 0UL; index < count && index < (ulong)maximumEntries; index++) {
				Seek(checked((long)memoryInfo.Offset + headerSize + (long)index * entrySize), 48);
				var address = binary.ReadUInt64();
				var allocation = binary.ReadUInt64();
				var allocationProtection = binary.ReadUInt32();
				binary.ReadUInt32();
				var size = binary.ReadUInt64();
				var state = binary.ReadUInt32();
				var protection = binary.ReadUInt32();
				var type = binary.ReadUInt32();
				Add("Native Memory State", "Virtual Region", address, $"{size:N0} bytes; State 0x{state:X}; Protection 0x{protection:X}; Type 0x{type:X}; Allocation 0x{allocation:X}; Allocation Protection 0x{allocationProtection:X}");
			}
			truncated |= count > (ulong)maximumEntries;
		}
		if (options.IncludeStacks) {
			foreach (var thread in threads.Take(32)) {
				if (entries.Count >= maximumEntries) {
					truncated = true;
					break;
				}
				Tools.Exceptions.ExecuteCapturingException(() => {
					var stack = Tools.Windows.Symbols.ReadDumpStack(reader, thread, Math.Min(options.MaximumFramesPerThread, 128), token);
					foreach (var frame in stack.Frames)
						Add("Native Stack", $"OS Thread {thread}: {frame.ModuleName}!{frame.SymbolName}", frame.InstructionPointer, $"SP 0x{frame.StackPointer:X}; Displacement 0x{frame.Displacement:X}");
					issues.AddRange(stack.Issues.Select(issue => $"OS Thread {thread}: {issue}"));
				}, out var error, error => error is not OperationCanceledException);
				if (error != null)
					issues.Add($"OS Thread {thread}: {error.Message[..Math.Min(error.Message.Length, 4096)]}");
			}
			if (threads.Count > 32) {
				truncated = true;
				issues.Add("Native unwinding is limited to the first 32 captured threads, 128 frames per thread and one MiB of captured-memory reads per thread.");
			}
		}
		if (truncated)
			issues.Add("Native dump records reached the configured record/text/stack limit; retained records only are shown.");
		var dump = new WindowsDumpInfo(Path.GetFullPath(path), stream.Length, File.GetLastWriteTimeUtc(path), ranges.ToArray());
		return (dump, entries.ToArray(), truncated, issues.ToArray());
	}

	/// <summary>Captures to a private staging file and publishes only on success. The host must route <see cref="TryRunWorker"/> before starting its UI.</summary>
	public Task<string> CaptureAsync(ProcessIdentity identity, string path, bool includeFullMemory = false, long maximumBytes = 512 * 1024 * 1024, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrEmpty(path, nameof(path));
		Guard.ArgumentInRange(maximumBytes, 1024 * 1024L, 8L * 1024 * 1024 * 1024, nameof(maximumBytes));
		Guard.Ensure(identity.IsVerified, "Dump capture requires a verified process creation time.");
		return Task.Run(async () => {
			Debug.AssertNotMainThread();
			cancellationToken.ThrowIfCancellationRequested();
			var destination = Path.GetFullPath(path);
			Guard.Ensure(!File.Exists(destination), "The dump destination already exists.");
			var staging = destination + "." + Guid.NewGuid().ToString("N") + ".partial";
			using var removeStaging = Tools.Scope.ExecuteOnDispose(() => {
				Tools.Exceptions.ExecuteCapturingException(() => File.Delete(staging), out var error);
				if (error != null)
					SystemLog.Logger.Exception(error, "Remove dump staging file: " + staging);
			});
			var start = (WorkerHost ?? new WindowsProcessWorkerHost()).CreateStartInfo(
				WorkerArgument, identity.ProcessId.ToString(CultureInfo.InvariantCulture),
				identity.StartTimeUtc.Value.Ticks.ToString(CultureInfo.InvariantCulture), staging, includeFullMemory ? "full" : "triage"
			);
			await using var worker = await WindowsProcessWorkerSession.StartAsync(start,
				new WindowsProcessWorkerSessionOptions { KillDescendants = true }, cancellationToken).ConfigureAwait(false);
			var exitCode = 0;
			try {
				using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				timeout.CancelAfter(TimeSpan.FromMinutes(2));
				while (!worker.Exit.IsCompleted) {
					timeout.Token.ThrowIfCancellationRequested();
					if (File.Exists(staging))
						Guard.Ensure(new FileInfo(staging).Length <= maximumBytes, "The dump exceeded its capture size limit.");
					await Task.Delay(100, timeout.Token).ConfigureAwait(false);
				}
				exitCode = await worker.Exit.ConfigureAwait(false);
			} finally {
				await worker.DisposeAsync().ConfigureAwait(false);
				foreach (var error in worker.CleanupErrors)
					SystemLog.Logger.Exception(error, "Finalize dump worker");
			}
			Guard.Ensure(worker.TerminationConfirmed && exitCode == 0 && File.Exists(staging), "Dump capture failed. " + worker.StandardError);
			var length = new FileInfo(staging).Length;
			Guard.Ensure(length >= 32 && length <= maximumBytes, "The dump is empty or exceeded its capture size limit.");
			cancellationToken.ThrowIfCancellationRequested();
			File.Move(staging, destination, overwrite: false);
			return destination;
		}, cancellationToken);
	}

	/// <summary>Routes the existing dump helper command before application startup, preserving the static dispatch contract.</summary>
	public bool TryRunCaptureWorker(string[] arguments, out int exitCode) => TryRunWorker(arguments, out exitCode);

	/// <summary>Call once during app startup, before application initialization, and exit when true.</summary>
	public static bool TryRunWorker(string[] arguments, out int exitCode) {
		exitCode = 0;
		if (arguments.Length == 0 || arguments[0] != WorkerArgument)
			return false;
		Tools.Exceptions.ExecuteCapturingException(() => {
			Guard.Ensure(arguments.Length == 5 && int.TryParse(arguments[1], NumberStyles.None, CultureInfo.InvariantCulture, out _)
				&& long.TryParse(arguments[2], NumberStyles.None, CultureInfo.InvariantCulture, out _), "Invalid dump worker arguments.");
			var identity = new ProcessIdentity(int.Parse(arguments[1], CultureInfo.InvariantCulture), new DateTime(long.Parse(arguments[2], CultureInfo.InvariantCulture), DateTimeKind.Utc));
			Guard.Ensure(arguments[4] is "full" or "triage", "Unknown dump mode.");
			Task.Run(() => CaptureCore(identity, arguments[3], arguments[4] == "full")).WaitSafe();
		}, out var error);
		if (error != null) {
			var message = error.Message;
			Console.Error.WriteLine(message[..Math.Min(message.Length, 4096)]);
			exitCode = 1;
		}
		return true;
	}

	private static void CaptureCore(ProcessIdentity identity, string path, bool fullMemory) {
		Debug.AssertNotMainThread();
		ProcessCommandService.ExecuteNative(identity, 0x400 | 0x10 | 0x40, process => {
			using var file = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
			using (WinAPI.DBGHELP.SyncRoot.EnterWriteScope()) {
				var type = 0x20u | 0x800u | 0x1000u | (fullMemory ? 0x2u : 0u);
				if (!WinAPI.DBGHELP.MiniDumpWriteDump(process, (uint)identity.ProcessId, file.SafeFileHandle, type, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
					throw new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastPInvokeError(), "Create process dump");
			}

			return true;
		}, null);
	}
}
