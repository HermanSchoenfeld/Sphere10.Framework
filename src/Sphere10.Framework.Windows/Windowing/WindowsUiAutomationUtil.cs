// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Captures selected-window UIA metadata through an owned isolated worker. Provider hangs never occupy the application's native dispatcher.</summary>
public sealed partial class WindowsUiAutomationUtil {
	public const string WorkerArgument = "--sphere10-uia-inspection";
	private static readonly AsyncSynchronizedGate _workers = new();
	private static ProcessIdentity? _unfinishedWorker;

	public WindowsProcessWorkerHost WorkerHost { get; set; }

	public Task<WindowsUiAutomationCapture> CaptureAsync(WindowsWindowInfo expected, WindowsUiAutomationOptions options = null, CancellationToken cancellationToken = default) {
		var host = WorkerHost;
		return Task.Run(() => CaptureCoreAsync(expected, options ?? new WindowsUiAutomationOptions(), host, cancellationToken), cancellationToken);
	}

	/// <summary>Routes the existing UI Automation helper through the utility instance without changing its startup or result protocol.</summary>
	public bool TryRunAutomationWorker(string[] args, out int exitCode) => TryRunWorker(args, out exitCode);

	public static bool TryRunWorker(string[] args, out int exitCode) {
		exitCode = 0;
		if (args == null || args.Length == 0 || args[0] != WorkerArgument)
			return false;
		Tools.Exceptions.ExecuteCapturingException(() => {
			Guard.Argument(args.Length == 2 && args[1].Length <= 16 * 1024, nameof(args), "Invalid UI Automation worker request.");
			var request = JsonSerializer.Deserialize(args[1], WorkerJsonContext.Default.WorkerRequest);
			Guard.ArgumentNotNull(request, nameof(args));
			request.Options.Validate();
			var expected = new WindowsWindowInfo(new IntPtr(request.Handle), string.Empty, request.ClassName, request.ProcessId, request.ThreadId,
				string.Empty, new DateTime(request.StartTicks, DateTimeKind.Utc), false, string.Empty);
			var notes = new HashSet<string>(StringComparer.Ordinal);
			// Sync-over-async pattern: this early worker entrypoint has no application/UI lifetime. Native COM stays on an MTA.
			var partial = Task.Run(() => WindowsUiAutomationReader.Capture(expected, request.Options,
				row => Write(new WorkerMessage { Kind = "row", Row = row }), message => {
					if (notes.Count < 32 && notes.Add(message))
						Write(new WorkerMessage { Kind = "issue", Message = message });
				})).GetAwaiter().GetResult();
			Write(new WorkerMessage { Kind = "complete", Partial = partial });
		}, out var error);
		if (error != null) {
			Write(new WorkerMessage { Kind = "issue", Message = Clip(error.GetType().Name + ": " + error.Message) });
			exitCode = 1;
		}
		return true;
	}

	private static async Task<WindowsUiAutomationCapture> CaptureCoreAsync(WindowsWindowInfo expected, WindowsUiAutomationOptions options, WindowsProcessWorkerHost host, CancellationToken cancellationToken) {
		Debug.AssertNotMainThread();
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.Argument(expected.ProcessStartTimeUtc.HasValue && expected.ProcessId > 0 && expected.ThreadId > 0 && expected.Handle != IntPtr.Zero,
			nameof(expected), "Select a window whose process, thread and native class identity were verified.");
		options.Validate();
		using var slot = await _workers.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		var captured = DateTime.UtcNow;
		if (_unfinishedWorker.HasValue && ProcessCommandService.IsCurrent(_unfinishedWorker.Value))
			return new WindowsUiAutomationCapture { CapturedAtUtc = captured, IsPartial = true,
				Availability = "The previous owned UI Automation helper is still terminating. Another helper will not be started until it exits." };
		_unfinishedWorker = null;
		host ??= new WindowsProcessWorkerHost();
		var request = new WorkerRequest(expected.Handle.ToInt64(), expected.ProcessId, expected.ProcessStartTimeUtc.Value.Ticks, expected.ThreadId, expected.ClassName, options);
		var startInfo = host.CreateStartInfo(WorkerArgument, JsonSerializer.Serialize(request, WorkerJsonContext.Default.WorkerRequest));
		var rows = new List<WindowsUiAutomationElement>();
		var notes = new List<string>();
		var complete = false;
		var partial = false;
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(TimeSpan.FromMilliseconds(options.TimeoutMilliseconds + 500));
		await using var worker = await WindowsProcessWorkerSession.StartAsync(startInfo, cancellationToken: cancellationToken).ConfigureAwait(false);
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			while (true) {
				var line = await worker.ReadLineAsync(cancellationToken: timeout.Token).ConfigureAwait(false);
				if (line == null)
					break;
				var message = JsonSerializer.Deserialize(line, WorkerJsonContext.Default.WorkerMessage);
				if (message == null)
					continue;
				if (message.Kind == "row" && message.Row != null) {
					Guard.Ensure(rows.Count < options.MaximumNodes, "The UI Automation helper exceeded the requested node budget.");
					Guard.Ensure(message.Row.NodeId == rows.Count + 1 && message.Row.Depth <= options.MaximumDepth,
						"The UI Automation helper returned an inconsistent node sequence.");
					rows.Add(message.Row);
				} else if (message.Kind == "complete") {
					complete = true;
					partial |= message.Partial;
				} else if (message.Kind == "issue" && !string.IsNullOrWhiteSpace(message.Message)) {
					partial = true;
					if (notes.Count < 32)
						notes.Add(Clip(message.Message));
				}
			}
			var code = await worker.Exit.WaitAsync(timeout.Token).ConfigureAwait(false);
			if (code != 0 || !complete) {
				partial = true;
				notes.Add($"UI Automation helper exited with code {code}; the capture did not complete.");
			}
		}).ConfigureAwait(false);
		// The operation owns cleanup even when its caller canceled. Preserve rows only for operational limits/errors, never pretend cancellation succeeded.
		await worker.DisposeAsync().ConfigureAwait(false);
		if (!worker.TerminationConfirmed)
			_unfinishedWorker = worker.Identity;
		cancellationToken.ThrowIfCancellationRequested();
		if (error != null) {
			partial = true;
			notes.Add(error is OperationCanceledException
				? "The isolated UI Automation helper reached its time limit. A provider may be unresponsive; available nodes were retained."
				: Clip(error.GetType().Name + ": " + error.Message));
		}
		foreach (var cleanup in worker.CleanupErrors) {
			partial = true;
			notes.Add("Helper Cleanup: " + Clip(cleanup.Message));
		}
		if (!string.IsNullOrWhiteSpace(worker.StandardError))
			notes.Add("Helper Diagnostics: " + Clip(worker.StandardError));
		if (worker.ErrorTruncated)
			notes.Add("Additional helper diagnostics were discarded after the bounded error-stream limit.");
		return new WindowsUiAutomationCapture { Rows = rows.ToArray(), CapturedAtUtc = captured, IsPartial = partial || !complete,
			Availability = string.Join(Environment.NewLine, notes.Distinct()) };
	}

	private static void Write(WorkerMessage message) {
		Console.WriteLine(JsonSerializer.Serialize(message, WorkerJsonContext.Default.WorkerMessage));
		Console.Out.Flush();
	}

	private static string Clip(string text) => text.Length > 1024 ? text[..1024] + "…" : text;

	#region Inner Types

	private sealed record WorkerRequest(long Handle, int ProcessId, long StartTicks, uint ThreadId, string ClassName, WindowsUiAutomationOptions Options);

	private sealed class WorkerMessage {
		public string Kind { get; set; }
		public WindowsUiAutomationElement Row { get; set; }
		public string Message { get; set; }
		public bool Partial { get; set; }
	}

	[JsonSerializable(typeof(WorkerRequest))]
	[JsonSerializable(typeof(WorkerMessage))]
	private partial class WorkerJsonContext : JsonSerializerContext {
	}

	#endregion
}
