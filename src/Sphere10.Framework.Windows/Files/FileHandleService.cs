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
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Files;

public sealed class FileHandleService {
	public const string WorkerArgument = "--sphere10-file-handle-worker";
	private static readonly AsyncSynchronizedGate _workers = new();
	private static ProcessIdentity? _unfinishedWorker;
	private const int MaximumRows = 10000;
	private readonly WindowsProcessWorkerHost _workerHost;

	public FileHandleService()
		: this(new WindowsProcessWorkerHost()) {
	}

	public FileHandleService(WindowsProcessWorkerHost workerHost) {
		Guard.ArgumentNotNull(workerHost, nameof(workerHost));
		_workerHost = workerHost;
	}

	public Task<FileHandleCapture> CaptureAsync(ProcessIdentity? identity = null, CancellationToken cancellationToken = default) {
		if (identity.HasValue)
			Guard.Argument(identity.Value.IsVerified && identity.Value.ProcessId > 0, nameof(identity), "Per-process file inspection requires a verified PID and creation time.");
		return Task.Run(() => RunWorkerAsync("handles", identity, null, cancellationToken), cancellationToken);
	}

	/// <summary>Captures selected object types through the same bounded helper. Other-object closure is never offered.</summary>
	public Task<FileHandleCapture> CaptureObjectsAsync(FileHandleCaptureOptions options, ProcessIdentity? identity = null, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(options, nameof(options));
		Guard.Argument(options.ObjectTypes is { Length: > 0 and <= 64 } && options.ObjectTypes.All(type => !string.IsNullOrWhiteSpace(type) && type.Length <= 128), nameof(options), "Select At Least One Bounded Object Type Or * For All Types");
		if (identity.HasValue)
			Guard.Argument(identity.Value.IsVerified && identity.Value.ProcessId > 0, nameof(identity), "Per-Process Object Inspection Requires A Verified PID And Creation Time");
		var selected = options with { ObjectTypes = options.ObjectTypes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() };
		return Task.Run(() => RunWorkerAsync("objects", identity, JsonSerializer.Serialize(selected, FileHandleJsonContext.Default.FileHandleCaptureOptions), cancellationToken), cancellationToken);
	}

	public Task<FileHandleCapture> FindBlockingProcessesAsync(string path, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrWhitespace(path, nameof(path));
		var fullPath = Path.GetFullPath(path);
		return Task.Run(() => RunWorkerAsync("blockers", null, fullPath, cancellationToken), cancellationToken);
	}

	/// <summary>Explicitly closes one captured disk-file handle after rechecking its owning process and native object. The source can still race the final check.</summary>
	public void ForceRelease(FileHandleInfo expected) {
		ValidateReleaseInput(expected);
		var owner = ReadOwner(expected.ProcessId, expected.Identity, DateTime.UtcNow);
		using var process = owner.Handle;
		Guard.Ensure(string.IsNullOrEmpty(owner.Availability), owner.Availability);
		ValidateReleaseEntry(expected, owner.Identity, FindReleaseEntry(expected));
		var sourceHandle = new IntPtr(unchecked((long)expected.HandleValue.Value));
		var copied = WinAPI.KERNEL32.DuplicateHandle(process, sourceHandle, WinAPI.KERNEL32.GetCurrentProcess(), out var duplicate,
			0, false, WinAPI.KERNEL32.DuplicateSameAccess);
		var copyError = Marshal.GetLastPInvokeError();
		using (duplicate) {
			if (!copied)
				throw new Win32Exception(copyError, "The captured file handle could not be duplicated for verification; no source-close request was issued.");
			Guard.Ensure(WinAPI.KERNEL32.GetFileType(duplicate) == WinAPI.KERNEL32.FileTypeDisk, "The selected handle no longer refers to a disk file.");
			var path = ReadHandlePath(duplicate, out var unavailable);
			Guard.Ensure(string.IsNullOrEmpty(unavailable) && string.Equals(path, expected.Path, StringComparison.OrdinalIgnoreCase),
				"The captured file path is unavailable or changed. Refresh before requesting closure.");
			var table = ReadHandleTable();
			var duplicateValue = unchecked((ulong)duplicate.DangerousGetHandle().ToInt64());
			var local = table.FirstOrDefault(entry => entry.ProcessId.ToUInt64() == (ulong)Environment.ProcessId && entry.HandleValue.ToUInt64() == duplicateValue);
			Guard.Ensure(unchecked((ulong)local.Object.ToInt64()) == expected.ObjectAddress, "The verification duplicate identifies a different file object; no close request was issued.");
			ValidateReleaseEntry(expected, owner.Identity, table.FirstOrDefault(entry => entry.ProcessId.ToUInt64() == (ulong)expected.ProcessId && entry.HandleValue.ToUInt64() == expected.HandleValue));
			// Microsoft documents that CLOSE_SOURCE may close the source even when DuplicateHandle reports failure.
			// Never retry this destructive native request automatically.
			var closed = WinAPI.KERNEL32.DuplicateHandle(process, sourceHandle, WinAPI.KERNEL32.GetCurrentProcess(), out var released,
				0, false, WinAPI.KERNEL32.DuplicateSameAccess | WinAPI.KERNEL32.DuplicateCloseSource);
			var closeError = Marshal.GetLastPInvokeError();
			using (released)
				if (!closed)
					throw new Win32Exception(closeError, "Windows reported failure after a source-close request. The source handle may already have been closed; refresh and review the application before any further action.");
		}
	}

	/// <summary>Uses the bounded existing diagnostic worker; cancellation cannot undo a native closure already issued.</summary>
	public async Task ForceReleaseAsync(FileHandleInfo expected, CancellationToken cancellationToken = default) {
		ValidateReleaseInput(expected);
		var result = await Task.Run(() => RunWorkerAsync("release", null, JsonSerializer.Serialize(expected, FileHandleJsonContext.Default.FileHandleInfo), cancellationToken), cancellationToken).ConfigureAwait(false);
		Guard.Ensure(!result.IsPartial, "The isolated file-handle release did not report a complete outcome. " + result.Availability);
	}

	public static bool TryRunWorker(string[] args, out int exitCode) {
		exitCode = 0;
		if (args == null || args.Length == 0 || args[0] != WorkerArgument)
			return false;
		Console.OutputEncoding = new UTF8Encoding(false);
		// The helper remains bounded even when the parent closes before its cancellation continuation runs.
		var watchdog = JobBuilder.For(() => Environment.Exit(3)).Called("File handle worker watchdog").RunOnce(DateTime.UtcNow.AddSeconds(22)).Build();
		Scheduler.Asynchronous.AddJob(watchdog);
		using var disarm = Tools.Scope.ExecuteOnDispose(() => Scheduler.Asynchronous.RemoveJob(watchdog));
		Tools.Exceptions.ExecuteCapturingException(() => {
			Guard.Argument(args.Length >= 2, nameof(args), "A file inspection worker requires a mode.");
			if (args[1] == "release") {
				Guard.Argument(args.Length == 3 && args[2].Length <= 128 * 1024, nameof(args), "File-handle release requires one captured native handle.");
				new FileHandleService().ForceRelease(JsonSerializer.Deserialize(args[2], FileHandleJsonContext.Default.FileHandleInfo));
			} else if (args[1] == "blockers") {
				Guard.Argument(args.Length == 3, nameof(args), "Restart Manager inspection requires a file path.");
				CaptureBlockers(args[2]);
			} else {
				var objects = args[1] == "objects";
				Guard.Argument((objects && (args.Length == 3 || args.Length == 5)) || (args[1] == "handles" && (args.Length == 2 || args.Length == 4)), nameof(args), "Invalid file handle worker arguments.");
				ProcessIdentity? identity = args.Length == (objects ? 5 : 4)
					? new ProcessIdentity(int.Parse(args[2], CultureInfo.InvariantCulture), new DateTime(long.Parse(args[3], CultureInfo.InvariantCulture), DateTimeKind.Utc))
					: null;
				var options = objects ? JsonSerializer.Deserialize(args[^1], FileHandleJsonContext.Default.FileHandleCaptureOptions) : null;
				CaptureHandles(identity, options);
			}
			WriteMessage(new WorkerMessage { Kind = "complete" });
		}, out var error);
		if (error != null) {
			WriteMessage(new WorkerMessage { Kind = "error", Message = $"{error.GetType().Name}: {error.Message}" });
			exitCode = 1;
		}
		return true;
	}

	internal static void ValidateReleaseInput(FileHandleInfo expected) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.Argument(expected.Identity.IsVerified && expected.ProcessId > 0 && expected.HandleValue.HasValue && expected.HandleValue > 0
			&& expected.IsDiskFile && string.Equals(expected.ObjectType, "File", StringComparison.Ordinal)
			&& expected.ObjectAddress != 0 && expected.ObjectTypeIndex != 0 && !string.IsNullOrEmpty(expected.Path) && string.IsNullOrEmpty(expected.Availability),
			nameof(expected), "Select a complete captured disk-file handle with verified process and native object identity.");
	}

	internal static void ValidateReleaseEntry(FileHandleInfo expected, ProcessIdentity actualProcess, WinAPI.NTDLL.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX actual) {
		ValidateReleaseInput(expected);
		Guard.Ensure(actualProcess == expected.Identity && actual.ProcessId.ToUInt64() == (ulong)expected.ProcessId && actual.HandleValue.ToUInt64() == expected.HandleValue
			&& unchecked((ulong)actual.Object.ToInt64()) == expected.ObjectAddress && actual.ObjectTypeIndex == expected.ObjectTypeIndex && actual.GrantedAccess == expected.GrantedAccess,
			"The owning process or source handle changed. Refresh and select its current capture before requesting closure.");
	}

	private static WinAPI.NTDLL.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX FindReleaseEntry(FileHandleInfo expected)
		=> ReadHandleTable().FirstOrDefault(entry => entry.ProcessId.ToUInt64() == (ulong)expected.ProcessId && entry.HandleValue.ToUInt64() == expected.HandleValue);

	private async Task<FileHandleCapture> RunWorkerAsync(string mode, ProcessIdentity? identity, string path, CancellationToken cancellationToken) {
		using var slot = await _workers.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		var startedAt = DateTime.UtcNow;
		if (_unfinishedWorker.HasValue && IsWorkerStillAllocated(_unfinishedWorker.Value))
			return new FileHandleCapture { CapturedAtUtc = startedAt, IsPartial = true,
				Availability = "The previous owned inspection worker is still terminating after a blocked driver query. Another worker will not be started until it exits." };
		_unfinishedWorker = null;
		var rows = new List<FileHandleInfo>();
		var partial = false;
		var complete = false;
		var notes = new List<string>();
		WindowsProcessWorkerSession worker = null;
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(20));
		await using (Tools.Scope.ExecuteOnDisposeAsync(async () => {
			if (worker == null)
				return;
			// Transport owns only the helper. The facade retains its original operation,
			// partial-result and unfinished-worker policies; release is still explicit.
			await worker.DisposeAsync().ConfigureAwait(false);
			if (!worker.TerminationConfirmed) {
				_unfinishedWorker = worker.Identity;
				partial = true;
			}
			foreach (var error in worker.CleanupErrors) {
				partial = true;
				notes.Add("Worker cleanup: " + error.Message);
			}
			if (!string.IsNullOrWhiteSpace(worker.StandardError))
				notes.Add(worker.StandardError.Trim());
			if (worker.ErrorTruncated) {
				partial = true;
				notes.Add("The owned worker's diagnostic error output exceeded the retained 1,048,576-character transport limit.");
			}
		})) {
			var inspectionError = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
				worker = await WindowsProcessWorkerSession.StartAsync(BuildWorkerStartInfo(mode, identity, path),
					new WindowsProcessWorkerSessionOptions { MaximumErrorCharacters = 1024 * 1024 }, timeout.Token).ConfigureAwait(false);
				while (true) {
					var line = await worker.ReadLineAsync(128 * 1024, timeout.Token).ConfigureAwait(false);
					if (line == null)
						break;
					var message = JsonSerializer.Deserialize(line, FileHandleJsonContext.Default.WorkerMessage);
					if (message == null)
						continue;
					if (message.Kind == "row" && message.Row != null) {
						Guard.Ensure(rows.Count < MaximumRows, "The file inspection row limit was reached.");
						rows.Add(message.Row);
						partial |= !string.IsNullOrEmpty(message.Row.Availability) && mode is "handles" or "objects";
					} else if (message.Kind == "complete") {
						complete = true;
					} else if (!string.IsNullOrEmpty(message.Message)) {
						partial = true;
						notes.Add(message.Message);
					}
				}
				var exitCode = await worker.Exit.WaitAsync(timeout.Token).ConfigureAwait(false);
				partial |= !complete || exitCode != 0;
				if (!complete || exitCode != 0)
					notes.Add($"The isolated worker exited with code {exitCode} before completing inspection.");
			}, error => error is not OperationCanceledException || !cancellationToken.IsCancellationRequested).ConfigureAwait(false);
			if (inspectionError is OperationCanceledException) {
				partial = true;
				notes.Add("The isolated worker reached its 20-second limit. Available rows were retained; native object name resolution may be blocked by a provider.");
			} else if (inspectionError != null) {
				partial = true;
				notes.Add($"File inspection: {inspectionError.GetType().Name}: {inspectionError.Message}");
			}
		}
		cancellationToken.ThrowIfCancellationRequested();
		if (mode == "handles")
			notes.Insert(0, "Open disk-file handles are a point-in-time inventory, not proof that a file blocks sharing. Handle values may be reused; access masks belong to the initial table snapshot. Pipes, mapped images, exited and protected processes may be omitted.");
		else if (mode == "objects")
			notes.Insert(0, "Read-only selected object handles. Names can be absent, stale or unavailable. Known type-specific rights and undecoded mask bits are retained. Only disk-file handles support the separate explicit release action.");
		else if (mode == "blockers")
			notes.Insert(0, "Restart Manager reports applications using the selected file; this is distinct from open-handle enumeration. No process is stopped or restarted.");
		else
			notes.Insert(0, "An explicit source-handle close request is irreversible. Failure or cancellation after dispatch can leave its outcome indeterminate; refresh before any further action.");
		return new FileHandleCapture { Rows = rows.ToArray(), CapturedAtUtc = startedAt, IsPartial = partial, Availability = string.Join(Environment.NewLine, notes) };
	}

	private ProcessStartInfo BuildWorkerStartInfo(string mode, ProcessIdentity? identity, string path) {
		var result = _workerHost.CreateStartInfo(WorkerArgument, mode);
		result.RedirectStandardOutput = true;
		result.RedirectStandardError = true;
		result.StandardOutputEncoding = Encoding.UTF8;
		result.StandardErrorEncoding = Encoding.UTF8;
		if (identity.HasValue) {
			result.ArgumentList.Add(identity.Value.ProcessId.ToString(CultureInfo.InvariantCulture));
			result.ArgumentList.Add(identity.Value.StartTimeUtc.Value.Ticks.ToString(CultureInfo.InvariantCulture));
		}
		if (path != null)
			result.ArgumentList.Add(path);
		return result;
	}

	private static void WriteMessage(WorkerMessage message) {
		Console.WriteLine(JsonSerializer.Serialize(message, FileHandleJsonContext.Default.WorkerMessage));
		Console.Out.Flush();
	}

	private static bool IsWorkerStillAllocated(ProcessIdentity identity) {
		using var handle = WinAPI.KERNEL32.OpenProcess(WinAPI.KERNEL32.ProcessQueryLimitedInformation, false, (uint)identity.ProcessId);
		return !handle.IsInvalid && WinAPI.KERNEL32.GetProcessTimes(handle, out var creation, out _, out _, out _)
			&& DateTime.FromFileTimeUtc(FileTimeTicks(creation)) == identity.StartTimeUtc;
	}

	internal sealed class WorkerMessage {
		public WorkerMessage() {
		}

		public string Kind { get; init; } = string.Empty;
		public FileHandleInfo Row { get; init; }
		public string Message { get; init; } = string.Empty;
	}

	private static void CaptureHandles(ProcessIdentity? requested, FileHandleCaptureOptions options = null) {
		// A real owned disk handle identifies this boot's File object-type index without hardcoding it.
		var ownImagePath = Environment.ProcessPath ?? typeof(FileHandleService).Assembly.Location;
		using var probe = File.OpenHandle(ownImagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		var enumeratedAtUtc = DateTime.UtcNow;
		var entries = ReadHandleTable();
		var probeValue = unchecked((ulong)probe.DangerousGetHandle().ToInt64());
		var fileType = entries.FirstOrDefault(entry => entry.ProcessId.ToUInt64() == (ulong)Environment.ProcessId && entry.HandleValue.ToUInt64() == probeValue).ObjectTypeIndex;
		Guard.Ensure(fileType != 0, "Windows did not provide a usable File object-type index in the native handle snapshot.");
		var owners = new Dictionary<int, HandleOwner>();
		var objectTypes = new Dictionary<ushort, string> { [fileType] = "File" };
		var selectedTypes = options == null ? null : new HashSet<string>(options.ObjectTypes, StringComparer.OrdinalIgnoreCase);
		using var resources = new Disposables();
		var emitted = 0;
		var deadline = Stopwatch.StartNew();
		foreach (var entry in entries.Where(entry => options != null || entry.ObjectTypeIndex == fileType).OrderBy(entry => entry.ProcessId.ToUInt64())) {
			if (deadline.Elapsed > TimeSpan.FromSeconds(17) || emitted >= MaximumRows) {
				WriteMessage(new WorkerMessage { Kind = "partial", Message = "Open-handle enumeration reached its time or row budget; available rows were retained." });
				return;
			}
			var rawProcessId = entry.ProcessId.ToUInt64();
			if (rawProcessId == 0 || rawProcessId > int.MaxValue || rawProcessId == (ulong)Environment.ProcessId)
				continue;
			var processId = (int)rawProcessId;
			if (requested.HasValue && requested.Value.ProcessId != processId)
				continue;
			if (!owners.TryGetValue(processId, out var owner)) {
				owner = ReadOwner(processId, requested, enumeratedAtUtc);
				owners[processId] = owner;
				resources.Add(owner.Handle);
				if (!string.IsNullOrEmpty(owner.Availability)) {
					WriteRow(new FileHandleInfo { Identity = owner.Identity, ProcessName = owner.Name, Availability = owner.Availability });
					emitted++;
				}
			}
			if (!string.IsNullOrEmpty(owner.Availability))
				continue;

			// Inventory only duplicates into this worker. Destructive source-close requests use the separate explicit release mode.
			if (!WinAPI.KERNEL32.DuplicateHandle(owner.Handle, new IntPtr(unchecked((long)entry.HandleValue.ToUInt64())), WinAPI.KERNEL32.GetCurrentProcess(),
				out var duplicate, 0, false, WinAPI.KERNEL32.DuplicateSameAccess)) {
				duplicate?.Dispose();
				continue;
			}
			using (duplicate) {
				if (!objectTypes.TryGetValue(entry.ObjectTypeIndex, out var objectType)) {
					Tools.Exceptions.ExecuteCapturingException(() => objectType = Tools.Windows.Objects.QueryHandleString(duplicate, true), out var typeError);
					if (typeError != null)
						objectType = "Type #" + entry.ObjectTypeIndex.ToString(CultureInfo.InvariantCulture);
					objectTypes[entry.ObjectTypeIndex] = objectType;
				}
				if (selectedTypes != null && !selectedTypes.Contains("*") && !selectedTypes.Contains(objectType))
					continue;
				var diskFile = objectType == "File" && WinAPI.KERNEL32.GetFileType(duplicate) == WinAPI.KERNEL32.FileTypeDisk;
				if (objectType == "File" && !diskFile && options?.IncludeNonDiskFiles != true)
					continue;
				var path = string.Empty;
				var unavailable = string.Empty;
				if (options?.ResolveNames != false) {
					if (diskFile)
						path = ReadHandlePath(duplicate, out unavailable);
					else {
						Tools.Exceptions.ExecuteCapturingException(() => path = Tools.Windows.Objects.QueryHandleString(duplicate, false), out var nameError);
						unavailable = nameError?.Message ?? string.Empty;
					}
				}
				WriteRow(new FileHandleInfo {
					Identity = owner.Identity, ProcessName = owner.Name, HandleValue = entry.HandleValue.ToUInt64(),
					ObjectAddress = unchecked((ulong)entry.Object.ToInt64()), ObjectTypeIndex = entry.ObjectTypeIndex,
					GrantedAccess = entry.GrantedAccess, Path = path, Availability = unavailable, ObjectType = objectType, IsDiskFile = diskFile,
					AccessRights = Tools.Windows.Objects.DecodeAccessMask(objectType, entry.GrantedAccess)
				});
				emitted++;
			}
		}
		if (requested.HasValue && !owners.ContainsKey(requested.Value.ProcessId)) {
			// Absence is an empty result only when the original process identity is still current.
			var owner = ReadOwner(requested.Value.ProcessId, requested, enumeratedAtUtc);
			using (owner.Handle)
				if (!string.IsNullOrEmpty(owner.Availability))
					WriteMessage(new WorkerMessage { Kind = "partial", Message = owner.Availability });
		}
	}

	private static WinAPI.NTDLL.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX[] ReadHandleTable() {
		var size = 1024 * 1024;
		for (var attempt = 0; attempt < 8; attempt++) {
			Guard.Ensure(size <= 64 * 1024 * 1024, "The Windows handle table exceeds the supported capture size.");
			using var allocation = new UnmanagedMemoryScope(size);
			var buffer = allocation.Item;
			var status = WinAPI.NTDLL.NtQuerySystemInformation(WinAPI.NTDLL.SystemExtendedHandleInformation, buffer, (uint)size, out var needed);
			if (status is WinAPI.NTDLL.StatusInfoLengthMismatch or WinAPI.NTDLL.StatusBufferTooSmall) {
				size = checked((int)Math.Max((long)size * 2, (long)needed + 65536));
				continue;
			}
			Guard.Ensure(status >= 0, $"Windows native handle enumeration is unavailable (NTSTATUS 0x{status:X8}). Elevation or a different Windows version may be required.");
			var headerSize = IntPtr.Size * 2;
			var rowSize = Marshal.SizeOf<WinAPI.NTDLL.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
			var count = Marshal.ReadIntPtr(buffer).ToInt64();
			Guard.Ensure(count >= 0 && count <= (size - headerSize) / rowSize, "Windows returned an invalid native handle-table layout.");
			var rows = new WinAPI.NTDLL.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX[checked((int)count)];
			for (var index = 0; index < rows.Length; index++)
				rows[index] = Marshal.PtrToStructure<WinAPI.NTDLL.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(IntPtr.Add(buffer, headerSize + index * rowSize));
			return rows;
		}
		throw new InvalidOperationException("The Windows handle table kept growing during capture; refresh to retry.");
	}

	private static HandleOwner ReadOwner(int processId, ProcessIdentity? requested, DateTime enumeratedAtUtc) {
		var handle = WinAPI.KERNEL32.OpenProcess(WinAPI.KERNEL32.ProcessQueryLimitedInformation | WinAPI.KERNEL32.ProcessDuplicateHandle | WinAPI.KERNEL32.Synchronize,
			false, (uint)processId);
		var transferred = false;
		using var rollback = Tools.Scope.ExecuteOnDispose(() => { if (!transferred) handle.Dispose(); });
		var identity = new ProcessIdentity(processId, null);
		var name = string.Empty;
		var availability = string.Empty;
		if (handle.IsInvalid) {
			availability = $"Cannot inspect this process's handles: {new Win32Exception(Marshal.GetLastWin32Error()).Message}";
		} else if (!WinAPI.KERNEL32.GetProcessTimes(handle, out var creation, out _, out _, out _)) {
			availability = "The owning process creation time could not be verified.";
		} else {
			identity = new ProcessIdentity(processId, DateTime.FromFileTimeUtc(FileTimeTicks(creation)));
			Tools.Exceptions.ExecuteCapturingException<Win32Exception>(() => {
				if (requested.HasValue && identity != requested.Value) {
					availability = "The selected process exited or its PID was reused; replacement-process handles are omitted.";
					identity = requested.Value;
				} else if (WindowsProcess.HasSignaledExit(handle)) {
					availability = "The owning process exited during handle inspection.";
				} else if (identity.StartTimeUtc > enumeratedAtUtc) {
					availability = "This process was created after handle enumeration started; stale PID associations are omitted.";
					identity = new ProcessIdentity(processId, null);
				} else {
					var path = new StringBuilder(32768);
					var size = (uint)path.Capacity;
					if (WinAPI.KERNEL32.QueryFullProcessImageName(handle, 0, path, ref size))
						name = Path.GetFileNameWithoutExtension(path.ToString());
				}
			}, out var error);
			if (error != null) {
				availability = "The owning process liveness could not be verified: " + error.Message;
			}
		}
		var owner = new HandleOwner { Handle = handle, Identity = identity, Name = name, Availability = availability };
		transferred = true;
		return owner;
	}

	private static string ReadHandlePath(WinAPI.KERNEL32.KernelHandle handle, out string unavailable) {
		// FILE_NAME_OPENED avoids requiring access to every component of a normalized network path.
		var buffer = new StringBuilder(1024);
		var length = WinAPI.KERNEL32.GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, 8);
		if (length >= buffer.Capacity && length < 32768) {
			buffer = new StringBuilder(checked((int)length + 1));
			length = WinAPI.KERNEL32.GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, 8);
		}
		if (length == 0 || length >= buffer.Capacity) {
			unavailable = "File path unavailable: " + new Win32Exception(Marshal.GetLastWin32Error()).Message;
			return string.Empty;
		}
		unavailable = string.Empty;
		var path = buffer.ToString();
		if (path.StartsWith("\\\\?\\UNC\\", StringComparison.OrdinalIgnoreCase))
			return "\\\\" + path[8..];
		return path.StartsWith("\\\\?\\", StringComparison.Ordinal) ? path[4..] : path;
	}

	private static void CaptureBlockers(string path) {
		var key = new StringBuilder(33);
		var error = WinAPI.RSTRTMGR.RmStartSession(out var session, 0, key);
		if (error != 0)
			throw new Win32Exception((int)error, "Unable to start a Restart Manager inspection session.");
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => WinAPI.RSTRTMGR.RmEndSession(session));
		error = WinAPI.RSTRTMGR.RmRegisterResources(session, 1, new[] { path }, 0, IntPtr.Zero, 0, IntPtr.Zero);
		if (error != 0)
			throw new Win32Exception((int)error, "Unable to register the selected file with Restart Manager.");
		var count = 0U;
		var rebootReasons = 0U;
		error = WinAPI.RSTRTMGR.RmGetList(session, out var needed, ref count, null, ref rebootReasons);
		for (var attempt = 0; attempt < 4; attempt++) {
			if (error == 0 && needed == 0)
				return;
			if (error != 0 && error != 234)
				throw new Win32Exception((int)error, "Restart Manager could not enumerate processes using the file.");
			Guard.Ensure(needed <= MaximumRows, "Restart Manager returned too many process records.");
			var applications = new WinAPI.RSTRTMGR.RM_PROCESS_INFO[needed];
			count = needed;
			error = WinAPI.RSTRTMGR.RmGetList(session, out needed, ref count, applications, ref rebootReasons);
			if (error == 234)
				continue;
			if (error != 0)
				throw new Win32Exception((int)error);
			foreach (var application in applications.Take(checked((int)count)))
				WriteRow(new FileHandleInfo {
					Identity = new ProcessIdentity((int)application.Process.ProcessId, DateTime.FromFileTimeUtc(FileTimeTicks(application.Process.StartTime))),
					ProcessName = application.ApplicationName ?? string.Empty, Path = path,
					Availability = "Reported by Restart Manager; no per-process handle or sharing-mode information is returned."
				});
			return;
		}
		throw new InvalidOperationException("Restart Manager results changed repeatedly during inspection; retry the selected file.");
	}

	private static long FileTimeTicks(WinAPI.KERNEL32.FILETIME time) => unchecked((long)(((ulong)time.DateTimeHigh << 32) | time.DateTimeLow));

	private static void WriteRow(FileHandleInfo row) => WriteMessage(new WorkerMessage { Kind = "row", Row = row });

	private sealed class HandleOwner {
		public WinAPI.KERNEL32.KernelHandle Handle { get; init; }
		public ProcessIdentity Identity { get; init; }
		public string Name { get; init; }
		public string Availability { get; init; }
	}
}
