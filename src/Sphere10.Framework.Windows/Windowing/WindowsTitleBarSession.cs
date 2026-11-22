// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Reads other windows' title-bar content through one lazy, owned helper, so no provider or hung window can stall the caller.</summary>
/// <remarks>
/// Each read runs <see cref="WindowsTitleBarReader"/> in the helper within a bounded time; a read that fails or overruns returns
/// content of source <see cref="WindowsTitleBarContentSource.None"/> and the next read starts a new helper. The executable must
/// dispatch the helper before its ordinary startup with <see cref="WindowsUiAutomationUtil.TryRunTitleBarWorker"/>.
/// </remarks>
public sealed partial class WindowsTitleBarSession : AsyncDisposable {
	public const string WorkerArgument = "--sphere10-titlebar";
	private const int BudgetMilliseconds = 800;
	private WindowsProcessWorkerHost Host { get; }
	private AsyncSynchronizedGate Requests { get; } = new();
	private CancellationTokenSource Lifetime { get; } = new();
	private WindowsProcessWorkerSession Worker { get; set; }

	public WindowsTitleBarSession(WindowsProcessWorkerHost host = null) {
		Host = host ?? new WindowsProcessWorkerHost();
	}

	/// <summary>Reads what occupies a physical strip of a verified window's title bar.</summary>
	public async Task<WindowsTitleBarContent> ReadAsync(WindowsWindowInfo expected, Rectangle strip, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.Argument(expected.Handle != IntPtr.Zero && expected.ProcessId > 0 && expected.ThreadId > 0 && expected.ProcessStartTimeUtc.HasValue,
			nameof(expected), "A verified window identity is required.");
		ObjectDisposedException.ThrowIf(Lifetime.IsCancellationRequested, this);
		using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Lifetime.Token);
		using var admission = await Requests.EnterScopeAsync(lifetime.Token).ConfigureAwait(false);
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
		timeout.CancelAfter(TimeSpan.FromMilliseconds(BudgetMilliseconds + 2000));
		try {
			if (Worker != null && Worker.Exit.IsCompleted)
				await ReleaseWorkerAsync().ConfigureAwait(false);
			if (Worker == null) {
				var start = Host.CreateStartInfo(WorkerArgument);
				Worker = await WindowsProcessWorkerSession.StartAsync(start, new WindowsProcessWorkerSessionOptions { RedirectStandardInput = true }, timeout.Token).ConfigureAwait(false);
			}
			var request = new TitleBarRequest(expected.Handle.ToInt64(), expected.ProcessId, expected.ThreadId, expected.ClassName, expected.ProcessStartTimeUtc.Value.Ticks,
				strip.X, strip.Y, strip.Width, strip.Height);
			await Worker.WriteLineAsync(JsonSerializer.Serialize(request, TitleBarJsonContext.Default.TitleBarRequest), 4096, timeout.Token).ConfigureAwait(false);
			var line = await Worker.ReadLineAsync(256 * 1024, timeout.Token).ConfigureAwait(false);
			Guard.Ensure(line != null, "The title-bar helper ended before returning a result.");
			var result = JsonSerializer.Deserialize(line, TitleBarJsonContext.Default.WindowsTitleBarContent);
			Guard.Ensure(result?.Occupied != null && result.Occupied.Length <= 1024, "The title-bar helper returned an invalid result.");
			return result;
		} catch (Exception error) {
			await ReleaseWorkerAsync().ConfigureAwait(false);
			lifetime.Token.ThrowIfCancellationRequested();
			return new WindowsTitleBarContent { Availability = error is OperationCanceledException ? "The title bar could not be read in time." : error.Message };
		}
	}

	internal static bool TryRunWorker(string[] args, out int exitCode) {
		exitCode = 0;
		if (args == null || args.Length == 0 || args[0] != WorkerArgument)
			return false;
		// The executable's entry point may be STA; the reader runs on a dedicated MTA worker.
		Tools.Exceptions.ExecuteCapturingException(() => Task.Run(RunRequests).GetAwaiter().GetResult(), out var error);
		if (error != null) {
			Console.Error.WriteLine(error.Message);
			exitCode = 1;
		}
		return true;
	}

	protected override async ValueTask DisposeInternalAsync() {
		Lifetime.Cancel();
		using var admission = await Requests.EnterScopeAsync().ConfigureAwait(false);
		await ReleaseWorkerAsync().ConfigureAwait(false);
		Lifetime.Dispose();
	}

	private async Task ReleaseWorkerAsync() {
		if (Worker == null)
			return;
		await Worker.DisposeAsync().ConfigureAwait(false);
		Guard.Ensure(Worker.TerminationConfirmed, "The owned title-bar helper has not terminated; no replacement can be started.");
		Worker = null;
	}

	private static void RunRequests() {
		while (Console.ReadLine() is { } line) {
			Guard.Ensure(line.Length <= 4096, "The title-bar request exceeded its transport budget.");
			var request = JsonSerializer.Deserialize(line, TitleBarJsonContext.Default.TitleBarRequest);
			Guard.ArgumentNotNull(request, nameof(request));
			var expected = new WindowsWindowInfo(new IntPtr(request.Handle), string.Empty, request.ClassName, request.ProcessId, request.ThreadId,
				string.Empty, new DateTime(request.StartTicks, DateTimeKind.Utc), true, string.Empty);
			WindowsTitleBarContent result = null;
			Tools.Exceptions.ExecuteCapturingException(() => result = WindowsWindow.ExecuteVerified(expected,
				() => WindowsTitleBarReader.Read(expected, new Rectangle(request.X, request.Y, request.Width, request.Height), TimeSpan.FromMilliseconds(BudgetMilliseconds))), out var error);
			result ??= new WindowsTitleBarContent { Availability = error?.Message ?? "The title-bar read failed." };
			Console.WriteLine(JsonSerializer.Serialize(result, TitleBarJsonContext.Default.WindowsTitleBarContent));
			Console.Out.Flush();
		}
	}

	#region Inner Types
	private sealed record TitleBarRequest(long Handle, int ProcessId, uint ThreadId, string ClassName, long StartTicks, int X, int Y, int Width, int Height);

	[JsonSerializable(typeof(TitleBarRequest))]
	[JsonSerializable(typeof(WindowsTitleBarContent))]
	private partial class TitleBarJsonContext : JsonSerializerContext {
	}
	#endregion
}
