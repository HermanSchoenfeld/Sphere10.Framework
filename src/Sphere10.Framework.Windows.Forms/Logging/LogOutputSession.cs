// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

// Each activation has a distinct logger. Its scope joins writes before disposing owned resources;
// a worker that captured this logger immediately before Stop subsequently sees a closed, harmless output.
internal sealed class LogOutputSession : LoggerBase, IDisposable {
	public event EventHandlerEx<Exception> LoggingFailed;

	private ILogger _sink;
	private readonly IDisposable _ownedResource;
	private readonly bool _timestampEnabled;
	private readonly SynchronizedObject _sinkScope = new();
	private readonly SynchronizedObject _queueScope = new();
	private readonly Channel<QueuedMessage> _messages = Channel.CreateBounded<QueuedMessage>(512);
	private Task _queueWorker;
	private Task _lastQueuedWrite = Task.CompletedTask;
	private Task _disposeTask;
	private Exception _queueFailure;
	private int _queuedCharacters;
	private volatile bool _accepting = true;

	private LogOutputSession(ILogger sink, IDisposable ownedResource, LogOutputConfiguration configuration) {
		_sink = sink;
		_ownedResource = ownedResource;
		_timestampEnabled = configuration.TimestampEnabled;
		Options = configuration.Options;
	}

	public bool IsLogging => _accepting && Volatile.Read(ref _sink) != null;

	public static LogOutputSession Create(LogOutputConfiguration configuration, ILogger logger) {
		IDisposable resource = null;
		try {
			ILogger sink;
			switch (configuration.Destination) {
				case LogOutputDestination.File:
					if (configuration.RollingEnabled) {
						var rolling = new RollingFileLogger(configuration.FilePath, configuration.MaxFiles, configuration.MaxFileSizeBytes, configuration.GetEncoding());
						resource = rolling;
						sink = rolling;
					} else {
						resource = new FileStream(configuration.FilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
						var writer = new StreamWriter((FileStream)resource, configuration.GetEncoding());
						resource = writer;
						sink = new CompletingTextWriterLogger(writer);
					}
					break;
				case LogOutputDestination.Application:
				case LogOutputDestination.Screen:
					Guard.ArgumentNotNull(logger, nameof(logger));
					sink = logger;
					break;
				case LogOutputDestination.StandardOutput:
					sink = new CompletingTextWriterLogger(Console.Out);
					break;
				case LogOutputDestination.StandardError:
					sink = new CompletingTextWriterLogger(Console.Error);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(configuration));
			}
			return new LogOutputSession(sink, resource, configuration);
		} catch {
			resource?.Dispose();
			throw;
		}
	}

	public override void Exception(Exception exception, string message = null) =>
		Write(LogLevel.Error, options => FormatExceptionMessage(exception, options, message));

	public bool EnqueueInfo(string message) => EnqueueInfo(message, false);

	public bool EnqueueInfo(string message, bool includesTimestamp) {
		if (message == null)
			return false;
		using (_queueScope.EnterWriteScope()) {
			if (!_accepting || message.Length > 1024 * 1024 - _queuedCharacters)
				return false;
			var queued = new QueuedMessage(message, includesTimestamp);
			if (!_messages.Writer.TryWrite(queued))
				return false;
			_queuedCharacters += message.Length;
			_lastQueuedWrite = queued.Completion.Task;
			_queueWorker ??= Task.Run(ProcessQueueAsync);
			return true;
		}
	}

	public Task FlushAsync() {
		using (_queueScope.EnterReadScope()) {
			return AwaitWritesAsync(_lastQueuedWrite);
		}
	}

	public void Dispose() {
		var completion = DisposeAsync();
		// Borrowed output may already have dispatched to the UI. Its owner controls that callback;
		// synchronous Stop must never join it from the same dispatcher.
		if (_ownedResource != null)
			completion.GetAwaiter().GetResult();
	}

	public Task DisposeAsync() {
		using (_queueScope.EnterWriteScope()) {
			if (_disposeTask != null)
				return _disposeTask;
			_accepting = false;
			_messages.Writer.Complete();
			var worker = _queueWorker ?? Task.CompletedTask;
			return _disposeTask = Task.Run(async () => {
				await worker.ConfigureAwait(false);
				using (_sinkScope.EnterWriteScope()) {
					CloseSink();
				}
				if (_queueFailure != null)
					ExceptionDispatchInfo.Capture(_queueFailure).Throw();
			});
		}
	}

	protected override void Log(LogLevel level, string message) => Write(level, _ => message);

	private async Task ProcessQueueAsync() {
		await foreach (var message in _messages.Reader.ReadAllAsync().ConfigureAwait(false)) {
			Tools.Exceptions.ExecuteCapturingException(() => Write(LogLevel.Info, _ => message.Text, queued: true, includesTimestamp: message.IncludesTimestamp), out var failure);
			using (_queueScope.EnterWriteScope()) {
				_queuedCharacters -= message.Text.Length;
				_queueFailure ??= failure;
			}
			message.Completion.SetResult();
		}
	}

	private async Task AwaitWritesAsync(Task completion) {
		await completion.ConfigureAwait(false);
		if (_queueFailure != null)
			ExceptionDispatchInfo.Capture(_queueFailure).Throw();
	}

	private void Write(LogLevel level, Func<LogOptions, string> formatMessage, bool queued = false, bool includesTimestamp = false) {
		Exception failure = null;
		ILogger borrowedSink = null;
		LogOptions options;
		using (_sinkScope.EnterWriteScope()) {
			// Options may have changed while a synchronous caller waited for admission.
			options = Options;
			var enabled = level switch {
				LogLevel.Debug => options.HasFlag(LogOptions.DebugEnabled),
				LogLevel.Info => options.HasFlag(LogOptions.InfoEnabled),
				LogLevel.Warning => options.HasFlag(LogOptions.WarningEnabled),
				LogLevel.Error => options.HasFlag(LogOptions.ErrorEnabled),
				_ => false
			};
			if (!queued && !_accepting || _sink == null || !enabled)
				return;
			if (_ownedResource == null) {
				borrowedSink = _sink;
			} else {
				Tools.Exceptions.ExecuteCapturingException(() => WriteMessage(_sink, level, formatMessage(options), includesTimestamp), out failure);
				if (failure != null)
					Tools.Exceptions.ExecuteIgnoringException(CloseSink);
			}
		}
		// External loggers may synchronously dispatch to this control's UI thread. Never hold our
		// lifetime scope across borrowed code; its actual owner controls completion and disposal.
		if (borrowedSink != null) {
			Tools.Exceptions.ExecuteCapturingException(() => WriteMessage(borrowedSink, level, formatMessage(options), includesTimestamp), out failure);
			if (failure != null) {
				using (_sinkScope.EnterWriteScope()) {
					CloseSink();
				}
			}
		}
		if (failure == null)
			return;
		LoggingFailed?.Invoke(failure);
		ExceptionDispatchInfo.Capture(failure).Throw();
	}

	private void WriteMessage(ILogger sink, LogLevel level, string message, bool includesTimestamp) {
		if (_timestampEnabled && !includesTimestamp)
			message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}: {message}";
		sink.Log(level, message);
	}

	private void CloseSink() {
		_accepting = false;
		if (_sink == null)
			return;
		_sink = null;
		_ownedResource?.Dispose();
	}

	#region Inner Types

	private sealed class CompletingTextWriterLogger : TextWriterLogger {
		private readonly TextWriter _writer;

		public CompletingTextWriterLogger(TextWriter writer)
			: base(writer) => _writer = writer;

		protected override void Log(LogLevel level, string message) {
			_writer.WriteLine($"[{level}] {message}");
			_writer.Flush();
		}
	}

	private sealed class QueuedMessage {
		public QueuedMessage(string text, bool includesTimestamp) {
			Text = text;
			IncludesTimestamp = includesTimestamp;
		}

		public string Text { get; }
		public bool IncludesTimestamp { get; }
		public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	#endregion
}
