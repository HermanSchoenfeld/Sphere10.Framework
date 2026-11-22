// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Serializes bounded media queries through one lazy, owned helper. No provider reference enters the caller's process.</summary>
/// <remarks>Dispose the session when tracking stops. Cancellation joins helper cleanup; unavailable providers produce no trustworthy mask.</remarks>
public sealed partial class WindowsUiAutomationMediaSession : AsyncDisposable {
	public const string WorkerArgument = "--sphere10-uia-media";
	private WindowsUiAutomationMediaOptions Options { get; }
	private WindowsProcessWorkerHost Host { get; }
	private AsyncSynchronizedGate Requests { get; } = new();
	private CancellationTokenSource Lifetime { get; } = new();
	private WindowsProcessWorkerSession Worker { get; set; }

	public WindowsUiAutomationMediaSession(WindowsUiAutomationMediaOptions options = null, WindowsProcessWorkerHost host = null) {
		Options = options ?? new WindowsUiAutomationMediaOptions();
		Options.Validate();
		Host = host ?? new WindowsProcessWorkerHost();
	}

	/// <summary>Returns current media rectangles for a verified borrowed window, clipped to known viewport ancestors and its visible frame.</summary>
	/// <remarks>
	/// The helper walks the window's provider tree on its first read and then at <see cref="WindowsUiAutomationMediaOptions.RediscoveryInterval"/>;
	/// reads in between only refresh the geometry of the media it already knows. Pass <paramref name="rediscover"/> to walk the tree now,
	/// for example after the window scrolled or its content changed.
	/// </remarks>
	public async Task<WindowsUiAutomationMediaCapture> CaptureAsync(WindowsWindowInfo expected, bool rediscover = false, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(expected, nameof(expected));
		Guard.Argument(expected.Handle != IntPtr.Zero && expected.ProcessId > 0 && expected.ThreadId > 0 && expected.ProcessStartTimeUtc.HasValue,
			nameof(expected), "A verified window identity is required.");
		ObjectDisposedException.ThrowIf(Lifetime.IsCancellationRequested, this);
		using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Lifetime.Token);
		using var admission = await Requests.EnterScopeAsync(lifetime.Token).ConfigureAwait(false);
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
		timeout.CancelAfter(TimeSpan.FromMilliseconds(Options.TimeoutMilliseconds + 1000));
		try {
			if (Worker != null && Worker.Exit.IsCompleted)
				await ReleaseWorkerAsync().ConfigureAwait(false);
			if (Worker == null) {
				var start = Host.CreateStartInfo(WorkerArgument, JsonSerializer.Serialize(Options, MediaJsonContext.Default.WindowsUiAutomationMediaOptions));
				Worker = await WindowsProcessWorkerSession.StartAsync(start,
					new WindowsProcessWorkerSessionOptions { RedirectStandardInput = true }, timeout.Token).ConfigureAwait(false);
			}
			var request = new MediaRequest(expected.Handle.ToInt64(), expected.ProcessId, expected.ThreadId, expected.ClassName, expected.ProcessStartTimeUtc.Value.Ticks, rediscover);
			await Worker.WriteLineAsync(JsonSerializer.Serialize(request, MediaJsonContext.Default.MediaRequest), 16384, timeout.Token).ConfigureAwait(false);
			var line = await Worker.ReadLineAsync(1024 * 1024, timeout.Token).ConfigureAwait(false);
			Guard.Ensure(line != null, "The media helper ended before returning a result.");
			var result = JsonSerializer.Deserialize(line, MediaJsonContext.Default.WindowsUiAutomationMediaCapture);
			Guard.Ensure(result != null && result.Regions != null && result.Regions.Length <= Options.MaximumRegions
				&& result.Upcoming != null && result.Upcoming.Length <= Options.MaximumRegions, "The media helper returned an invalid result.");
			return result with { Window = expected };
		} catch (Exception error) {
			await ReleaseWorkerAsync().ConfigureAwait(false);
			lifetime.Token.ThrowIfCancellationRequested();
			return new WindowsUiAutomationMediaCapture {
				Window = expected, CapturedAtUtc = DateTime.UtcNow, IsPartial = true,
				Availability = error is OperationCanceledException ? "The media provider exceeded its query time budget." : error.Message
			};
		}
	}

	internal static bool TryRunWorker(string[] args, out int exitCode) {
		exitCode = 0;
		if (args == null || args.Length == 0 || args[0] != WorkerArgument)
			return false;
		Tools.Exceptions.ExecuteCapturingException(() => {
			Guard.Argument(args.Length == 2 && args[1].Length <= 16384, nameof(args), "Invalid media helper options.");
			var options = JsonSerializer.Deserialize(args[1], MediaJsonContext.Default.WindowsUiAutomationMediaOptions);
			Guard.ArgumentNotNull(options, nameof(options));
			options.Validate();
			// The executable's entry point may be STA. One dedicated MTA owns every retained provider reference.
			Task.Run(() => RunRequests(options)).GetAwaiter().GetResult();
		}, out var error);
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
		Guard.Ensure(Worker.TerminationConfirmed, "The owned media helper has not terminated; no replacement can be started.");
		Worker = null;
	}

	private static void RunRequests(WindowsUiAutomationMediaOptions options) {
		using var reader = new WindowsUiAutomationMediaReader(options);
		while (Console.ReadLine() is { } line) {
			Guard.Ensure(line.Length <= 16384, "The media request exceeded its transport budget.");
			var request = JsonSerializer.Deserialize(line, MediaJsonContext.Default.MediaRequest);
			Guard.ArgumentNotNull(request, nameof(request));
			var expected = new WindowsWindowInfo(new IntPtr(request.Handle), string.Empty, request.ClassName, request.ProcessId, request.ThreadId,
				string.Empty, new DateTime(request.StartTicks, DateTimeKind.Utc), true, string.Empty);
			WindowsUiAutomationMediaCapture result = null;
			Tools.Exceptions.ExecuteCapturingException(() => result = reader.Capture(expected, request.Rediscover), out var error);
			result ??= new WindowsUiAutomationMediaCapture { CapturedAtUtc = DateTime.UtcNow, IsPartial = true, Availability = error?.Message ?? "The media query failed." };
			Console.WriteLine(JsonSerializer.Serialize(result, MediaJsonContext.Default.WindowsUiAutomationMediaCapture));
			Console.Out.Flush();
		}
	}

	#region Inner Types
	private sealed record MediaRequest(long Handle, int ProcessId, uint ThreadId, string ClassName, long StartTicks, bool Rediscover);

	[JsonSerializable(typeof(MediaRequest))]
	[JsonSerializable(typeof(WindowsUiAutomationMediaOptions))]
	[JsonSerializable(typeof(WindowsUiAutomationMediaCapture))]
	private partial class MediaJsonContext : JsonSerializerContext {
	}
	#endregion
}
