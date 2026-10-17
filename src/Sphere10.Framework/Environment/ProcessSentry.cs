// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Runs owned external commands, completing only after process exit and output collection.</summary>
public class ProcessSentry {
	private readonly AsyncSynchronizedGate _execution = new();
	private readonly SynchronizedObject _state = new();
	private Process _runningProcess;

	public ProcessSentry(string executableFileName) {
		Guard.ArgumentNotNullOrWhitespace(executableFileName, nameof(executableFileName));
		ExecutableFileName = executableFileName;
	}

	public string WorkingDirectory { get; init; } = AppContext.BaseDirectory;

	public string ExecutableFileName { get; }

	/// <summary>Borrowed output destination. Commands serialize writes and never dispose it.</summary>
	public TextWriter OutputWriter { get; init; } = new NoOpTextWriter();

	protected Process RunningProcess {
		get {
			using var read = _state.EnterReadScope();
			return _runningProcess;
		}
	}

	/// <summary>Requests termination of this sentry's owned running process tree. Its run task still owns completion and cleanup.</summary>
	public virtual void BreakRunningProcess() {
		using var write = _state.EnterWriteScope();
		if (_runningProcess != null && !_runningProcess.HasExited)
			_runningProcess.Kill(true);
	}

	public virtual async Task RunWithErrorCodeCheckAsync(string arguments = null, int expectedErrorCode = 0, CancellationToken cancellationToken = default) {
		var result = await RunCoreAsync(arguments, null, cancellationToken).ConfigureAwait(false);
		if (result.ExitCode != expectedErrorCode)
			throw new ProcessSentryException(ExecutableFileName, result.ExitCode, result.StandardOutput + result.StandardError);
	}

	public virtual async Task<int> RunAsync(string arguments = null, CancellationToken cancellationToken = default)
		=> (await RunCoreAsync(arguments, null, cancellationToken).ConfigureAwait(false)).ExitCode;

	/// <summary>Uses native structured arguments and returns completed stdout/stderr captures.</summary>
	public virtual Task<ProcessExecutionResult> RunCapturedAsync(string[] arguments, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(arguments, nameof(arguments));
		Guard.Argument(arguments.All(argument => argument != null), nameof(arguments), "Command arguments cannot contain null values.");
		return RunCoreAsync(null, (string[])arguments.Clone(), cancellationToken);
	}

	public static async Task<bool> CanRunAsync(string executableFileName, CancellationToken cancellationToken = default) {
		try {
			var sentry = new ProcessSentry(executableFileName);
			return await sentry.RunAsync(cancellationToken: cancellationToken).ConfigureAwait(false) == 0;
		} catch (OperationCanceledException) {
			throw;
		} catch {
			return false;
		}
	}

	public static Task<int> RunAsync(string fileName, string workingDir, string arguments = null, TextWriter output = null, CancellationToken cancellationToken = default)
		=> new ProcessSentry(fileName) { WorkingDirectory = workingDir, OutputWriter = output ?? new NoOpTextWriter() }.RunAsync(arguments, cancellationToken);

	protected virtual async Task<ProcessExecutionResult> RunCoreAsync(string arguments, string[] argumentList, CancellationToken cancellationToken) {
		using var admission = await _execution.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();
		using var process = new Process {
			StartInfo = new ProcessStartInfo(ExecutableFileName) {
				WorkingDirectory = WorkingDirectory ?? string.Empty,
				UseShellExecute = false,
				CreateNoWindow = true,
				ErrorDialog = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			}
		};
		if (argumentList != null)
			foreach (var argument in argumentList)
				process.StartInfo.ArgumentList.Add(argument);
		else
			process.StartInfo.Arguments = arguments ?? string.Empty;
		var standardOutput = new StringBuilder();
		var standardError = new StringBuilder();
		var outputAccess = new SynchronizedObject();
		Task outputTask = null;
		Task errorTask = null;
		var started = false;
		using var runningState = Tools.Scope.ExecuteOnDispose(() => {
			using var write = _state.EnterWriteScope();
			if (ReferenceEquals(_runningProcess, process))
				_runningProcess = null;
		});
		try {
			started = await Task.Run(process.Start, cancellationToken).ConfigureAwait(false);
			Guard.Ensure(started, $"Unable to start process: {ExecutableFileName}");
			using (_state.EnterWriteScope())
				_runningProcess = process;
			process.StandardInput.Close();
			outputTask = ReadOutputAsync(process.StandardOutput, standardOutput, outputAccess);
			errorTask = ReadOutputAsync(process.StandardError, standardError, outputAccess);
			var pending = new List<Task> { process.WaitForExitAsync(), outputTask, errorTask };
			while (pending.Count > 0) {
				var completed = await Task.WhenAny(pending).WaitAsync(cancellationToken).ConfigureAwait(false);
				pending.Remove(completed);
				await completed.ConfigureAwait(false);
			}
			cancellationToken.ThrowIfCancellationRequested();
			return new ProcessExecutionResult(process.ExitCode, standardOutput.ToString(), standardError.ToString());
		} catch (Exception failure) {
			if (started) {
				try {
					if (!process.HasExited)
						process.Kill(true);
					using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
					await process.WaitForExitAsync(cleanup.Token).ConfigureAwait(false);
					var pumps = new[] { outputTask, errorTask }.Where(task => task != null).ToArray();
					try {
						await Task.WhenAll(pumps).WaitAsync(cleanup.Token).ConfigureAwait(false);
					} catch when (pumps.All(task => task.IsCompleted)) {
						// The original output error already controls the operation outcome.
					}
				} catch (Exception cleanupFailure) {
					throw new AggregateException("The command failed and its owned process cleanup did not complete.", failure, cleanupFailure);
				}
			}
			throw;
		}
	}

	private async Task ReadOutputAsync(StreamReader reader, StringBuilder capture, SynchronizedObject outputAccess) {
		while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line) {
			using var write = outputAccess.EnterWriteScope();
			capture.AppendLine(line);
			OutputWriter?.WriteLine(line);
		}
	}
}

