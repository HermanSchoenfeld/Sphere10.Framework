// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ProcessSentryTests {
	[Test]
	public async Task DefaultWorkingDirectoryRunsCommandFromTheApplicationDirectory() {
		var sentry = new ProcessSentry(Shell);
		var result = await sentry.RunCapturedAsync(Command("cd", "pwd"));
		Assert.That(result.ExitCode, Is.Zero);
		Assert.That(Path.GetFullPath(result.StandardOutput.Trim()).TrimEnd(Path.DirectorySeparatorChar),
			Is.EqualTo(Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar)).IgnoreCase);
	}

	[Test]
	public async Task CapturedResultIncludesBothCompletedStreamsAndExitCode() {
		var sentry = new ProcessSentry(Shell) { WorkingDirectory = Path.GetTempPath() };
		var result = await sentry.RunCapturedAsync(Command("echo last-output & echo last-error 1>&2 & exit /b 7", "printf 'last-output\\n'; printf 'last-error\\n' >&2; exit 7"));
		Assert.That(result.ExitCode, Is.EqualTo(7));
		Assert.That(result.StandardOutput, Does.Contain("last-output"));
		Assert.That(result.StandardError, Does.Contain("last-error"));
	}

	[Test]
	public async Task ErrorCodeExceptionIncludesActualTranscript() {
		var sentry = new ProcessSentry(Shell) { WorkingDirectory = Path.GetTempPath() };
		var arguments = OperatingSystem.IsWindows() ? "/d /c echo transcript & exit /b 9" : "-c \"printf transcript; exit 9\"";
		await Assert.ThatAsync(() => sentry.RunWithErrorCodeCheckAsync(arguments), Throws.TypeOf<ProcessSentryException>().With.Message.Contains("transcript"));
	}

	[Test]
	public async Task CancellationTerminatesAndDrainsOwnedProcessThenAllowsAnotherCommand() {
		var sentry = new InspectableSentry(Shell) { WorkingDirectory = Path.GetTempPath() };
		using var cancellation = new CancellationTokenSource();
		var running = sentry.RunCapturedAsync(Command("ping 127.0.0.1 -n 60 >nul", "sleep 60"), cancellation.Token);
		var pid = await WaitForProcessAsync(sentry);
		cancellation.Cancel();
		await Assert.ThatAsync(() => running, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(sentry.CurrentProcessId, Is.Null);
		Assert.That(() => Process.GetProcessById(pid), Throws.TypeOf<ArgumentException>());
		var next = await sentry.RunCapturedAsync(Command("echo next", "printf next"));
		Assert.That(next.StandardOutput, Does.Contain("next"));
	}

	[Test]
	public async Task QueuedCancellationDoesNotReplaceOrKillActiveCommand() {
		var sentry = new InspectableSentry(Shell) { WorkingDirectory = Path.GetTempPath() };
		using var firstCancellation = new CancellationTokenSource();
		var first = sentry.RunCapturedAsync(Command("ping 127.0.0.1 -n 60 >nul", "sleep 60"), firstCancellation.Token);
		var pid = await WaitForProcessAsync(sentry);
		{
			await using var drainCancelledProcess = Tools.Scope.ExecuteOnDisposeAsync<(CancellationTokenSource Cancellation, Task<ProcessExecutionResult> Pending)>(static async state => {
				state.Cancellation.Cancel();
				try { await state.Pending; } catch (OperationCanceledException) { }
			}, (firstCancellation, first));
			using var queuedCancellation = new CancellationTokenSource();
			var queued = sentry.RunCapturedAsync(Command("echo should-not-run", "printf should-not-run"), queuedCancellation.Token);
			queuedCancellation.Cancel();
			await Assert.ThatAsync(() => queued, Throws.InstanceOf<OperationCanceledException>());
			Assert.That(sentry.CurrentProcessId, Is.EqualTo(pid));
		}
	}

	[Test]
	public async Task OutputWriterRemainsBorrowedAfterCompletion() {
		using var writer = new StringWriter();
		var sentry = new ProcessSentry(Shell) { WorkingDirectory = Path.GetTempPath(), OutputWriter = writer };
		await sentry.RunCapturedAsync(Command("echo written", "printf written"));
		writer.WriteLine("still open");
		Assert.That(writer.ToString(), Does.Contain("written").And.Contains("still open"));
	}

	[Test]
	public async Task InvalidExecutableLeavesNoRunningProcess() {
		var sentry = new InspectableSentry(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".missing"));
		await Assert.ThatAsync(() => sentry.RunCapturedAsync(Array.Empty<string>()), Throws.InstanceOf<System.ComponentModel.Win32Exception>());
		Assert.That(sentry.CurrentProcessId, Is.Null);
	}

	private static string Shell => OperatingSystem.IsWindows() ? Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe" : "/bin/sh";

	private static string[] Command(string windows, string unix) => OperatingSystem.IsWindows() ? new[] { "/d", "/c", windows } : new[] { "-c", unix };

	private static async Task<int> WaitForProcessAsync(InspectableSentry sentry) {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		while (sentry.CurrentProcessId is not { } pid) {
			await Task.Delay(10, timeout.Token);
		}
		return sentry.CurrentProcessId.Value;
	}

	private sealed class InspectableSentry : ProcessSentry {
		public InspectableSentry(string executable) : base(executable) { }
		public int? CurrentProcessId => RunningProcess?.Id;
	}
}
