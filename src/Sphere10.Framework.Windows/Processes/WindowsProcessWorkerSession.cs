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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Owns one isolated helper, bounded redirected transport and asynchronous termination. It does not interpret result protocols or operation outcomes.</summary>
public sealed class WindowsProcessWorkerSession : IAsyncDisposable {
	private readonly Process _process;
	private readonly WindowsProcessWorkerSessionOptions _options;
	private readonly char[] _outputBuffer = new char[4096];
	private readonly CancellationTokenSource _errorLifetime = new();
	private readonly SynchronizedObject _disposeSync = new();
	private int _outputOffset;
	private int _outputCount;
	private Task _errors;
	private Task _disposal;
	private bool _started;
	private string _standardError = string.Empty;
	private bool _errorTruncated;
	private Exception[] _cleanupErrors = Array.Empty<Exception>();

	private WindowsProcessWorkerSession(ProcessStartInfo startInfo, WindowsProcessWorkerSessionOptions options) {
		_options = options;
		startInfo.UseShellExecute = false;
		startInfo.CreateNoWindow = true;
		startInfo.RedirectStandardOutput = true;
		startInfo.RedirectStandardError = true;
		startInfo.StandardOutputEncoding = Encoding.UTF8;
		startInfo.StandardErrorEncoding = Encoding.UTF8;
		_process = new Process { StartInfo = startInfo };
	}

	public ProcessIdentity Identity { get; private set; }
	public Task<int> Exit { get; private set; }
	public string StandardError => Volatile.Read(ref _standardError);
	public bool ErrorTruncated => Volatile.Read(ref _errorTruncated);
	public bool TerminationConfirmed => Exit?.IsCompletedSuccessfully == true;
	public Exception[] CleanupErrors => (Exception[])Volatile.Read(ref _cleanupErrors).Clone();

	/// <summary>Transfers the fresh start descriptor to an owned worker. Starting and reading native process identity occur on a worker.</summary>
	public static async Task<WindowsProcessWorkerSession> StartAsync(ProcessStartInfo startInfo, WindowsProcessWorkerSessionOptions options = null, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(startInfo, nameof(startInfo));
		options ??= new WindowsProcessWorkerSessionOptions();
		options.Validate();
		var session = new WindowsProcessWorkerSession(startInfo, options);
		var accepted = false;
		await using var reject = Tools.Scope.ExecuteOnDisposeAsync(async () => {
			if (!accepted)
				await session.DisposeAsync().ConfigureAwait(false);
		});
		await Task.Run(() => session.StartCore(cancellationToken.ThrowIfCancellationRequested), cancellationToken).ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();
		accepted = true;
		return session;
	}

	/// <summary>Reads one UTF-8 protocol line within the caller's character and cancellation budgets. One reader at a time owns stdout.</summary>
	public async Task<string> ReadLineAsync(int maximumCharacters = 128 * 1024, CancellationToken cancellationToken = default) {
		Guard.ArgumentInRange(maximumCharacters, 1, 1024 * 1024, nameof(maximumCharacters));
		var text = new StringBuilder(Math.Min(maximumCharacters, 4096));
		while (true) {
			cancellationToken.ThrowIfCancellationRequested();
			if (_outputOffset == _outputCount) {
				_outputCount = await _process.StandardOutput.ReadAsync(_outputBuffer.AsMemory(), cancellationToken).ConfigureAwait(false);
				_outputOffset = 0;
				if (_outputCount == 0)
					return text.Length == 0 ? null : text.ToString();
			}
			var newline = Array.IndexOf(_outputBuffer, '\n', _outputOffset, _outputCount - _outputOffset);
			var end = newline < 0 ? _outputCount : newline;
			var length = end - _outputOffset;
			Guard.Ensure(length <= maximumCharacters - text.Length, "The isolated worker returned an oversized protocol line.");
			text.Append(_outputBuffer, _outputOffset, length);
			_outputOffset = end + (newline < 0 ? 0 : 1);
			if (newline < 0)
				continue;
			if (text.Length > 0 && text[^1] == '\r')
				text.Length--;
			return text.ToString();
		}
	}

	/// <summary>Runs cleanup once, with independent bounded budgets even when the operation was canceled.</summary>
	public ValueTask DisposeAsync() {
		using var scope = _disposeSync.EnterWriteScope();
		return new ValueTask(_disposal ??= Task.Run(DisposeCoreAsync));
	}

	private void StartCore(Action checkpoint) {
		Debug.AssertNotMainThread();
		checkpoint();
		Guard.Ensure(_process.Start(), "Unable to start the isolated Windows inspection worker.");
		_started = true;
		// Install exit observation before identity/error metadata can fail, so rejected startup still drains the held native process.
		Exit = ObserveExitAsync();
		_errors = DrainErrorsAsync();
		checkpoint();
		Identity = new ProcessIdentity(_process.Id, _process.StartTime.ToUniversalTime());
	}

	private async Task<int> ObserveExitAsync() {
		await _process.WaitForExitAsync().ConfigureAwait(false);
		return _process.ExitCode;
	}

	private async Task DrainErrorsAsync() {
		var buffer = new char[4096];
		var text = new StringBuilder(Math.Min(_options.MaximumErrorCharacters, 4096));
		while (true) {
			var count = await _process.StandardError.ReadAsync(buffer.AsMemory(), _errorLifetime.Token).ConfigureAwait(false);
			if (count == 0)
				return;
			var retain = Math.Min(count, _options.MaximumErrorCharacters - text.Length);
			text.Append(buffer, 0, retain);
			Volatile.Write(ref _standardError, text.ToString());
			if (retain < count)
				Volatile.Write(ref _errorTruncated, true);
		}
	}

	private async Task DisposeCoreAsync() {
		Debug.AssertNotMainThread();
		using var releaseProcess = _process;
		using var releaseCancellation = _errorLifetime;
		var errors = new List<Exception>();
		if (_started) {
			Tools.Exceptions.ExecuteCapturingException(() => {
				if (!TerminationConfirmed && !_process.HasExited)
					_process.Kill(_options.KillDescendants);
			}, out var stopError);
			var termination = Exit == null ? null : await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Exit.WaitAsync(_options.TerminationTimeout)).ConfigureAwait(false);
			if (stopError != null && !TerminationConfirmed)
				errors.Add(stopError);
			if (termination != null)
				errors.Add(termination);
		}
		if (_errors != null) {
			var drain = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => _errors.WaitAsync(_options.ErrorDrainTimeout)).ConfigureAwait(false);
			if (drain != null) {
				errors.Add(drain);
				_errorLifetime.Cancel();
				_ = Tools.Exceptions.ExecuteCapturingExceptionAsync(() => _errors);
			}
		}
		if (Exit != null && !Exit.IsCompleted)
			_ = Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Exit);
		Volatile.Write(ref _cleanupErrors, errors.ToArray());
	}
}
