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
using System.Threading.Tasks;
using System.Threading.Channels;

namespace Sphere10.Framework.Windows.Forms;

// Each activation has a distinct logger. Its scope joins writes before disposing owned resources;
// a worker that captured this logger immediately before Stop subsequently sees a closed, harmless output.
internal sealed class LogOutputSession : SynchronizedObject, ILogger, IDisposable {
	public event EventHandlerEx<Exception> LoggingFailed;

	private ILogger _sink;
	private ILogger _untimestampedSink;
	private IDisposable _ownedSink;
	private volatile LogOptions _options;
	private bool _disposed;
	private readonly bool _ownsSink;
	private readonly SynchronizedObject _queueScope = new();
	private readonly Channel<QueuedMessage> _messages = Channel.CreateBounded<QueuedMessage>(512);
	private Task _queueWorker;
	private Task _lastQueuedWrite = Task.CompletedTask;
	private Task _disposeTask;
	private Exception _queueFailure;
	private int _queuedCharacters;
	private volatile bool _accepting = true;

	private LogOutputSession(ILogger sink, ILogger untimestampedSink, IDisposable ownedSink, LogOptions options) {
		_sink = sink;
		_untimestampedSink = untimestampedSink;
		_ownedSink = ownedSink;
		_ownsSink = ownedSink != null;
		_options = options;
	}

	public LogOptions Options {
		get => _options;
		set => _options = value;
	}

	public bool IsLogging {
		get => _accepting && System.Threading.Volatile.Read(ref _sink) != null;
	}

	public static LogOutputSession Create(LogOutputConfiguration configuration, ILogger applicationLogger, ILogger screenLogger = null) {
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
					Guard.ArgumentNotNull(applicationLogger, nameof(applicationLogger));
					sink = applicationLogger;
					break;
				case LogOutputDestination.StandardOutput:
					sink = new CompletingTextWriterLogger(Console.Out);
					break;
				case LogOutputDestination.StandardError:
					sink = new CompletingTextWriterLogger(Console.Error);
					break;
				case LogOutputDestination.Screen:
					Guard.ArgumentNotNull(screenLogger, nameof(screenLogger));
					sink = screenLogger;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(configuration));
			}
			var untimestampedSink = sink;
			if (configuration.TimestampEnabled)
				sink = new TimestampLogger(sink, "yyyy-MM-dd HH:mm:ss.fff zzz");
			return new LogOutputSession(sink, untimestampedSink, resource, configuration.Options);
		} catch {
			resource?.Dispose();
			throw;
		}
	}

	public void Debug(string message) { if (message != null) Forward(LogOptions.DebugEnabled, (logger, _) => logger.Debug(message)); }
	public void Info(string message) { if (message != null) Forward(LogOptions.InfoEnabled, (logger, _) => logger.Info(message)); }
	public void Warning(string message) { if (message != null) Forward(LogOptions.WarningEnabled, (logger, _) => logger.Warning(message)); }
	public void Error(string message) { if (message != null) Forward(LogOptions.ErrorEnabled, (logger, _) => logger.Error(message)); }

	public void Exception(Exception exception, string message = null) => Forward(LogOptions.ErrorEnabled, (logger, options) => {
		// Hydrate with our own detail flags, without changing a borrowed application logger's options.
		new ExceptionOutputLogger(logger) { Options = options }.Exception(exception, message);
	});

	public bool EnqueueInfo(string message) => EnqueueInfo(message, false);

	public bool EnqueueInfo(string message, bool includesTimestamp) {
		if (message == null)
			return false;
		using var scope = _queueScope.EnterWriteScope();
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

	public Task FlushAsync() {
		using var scope = _queueScope.EnterReadScope();
		return AwaitWritesAsync(_lastQueuedWrite);
	}

	public void Dispose() {
		var completion = DisposeAsync();
		// Borrowed output may already have dispatched to the UI. Its owner controls that callback;
		// synchronous Stop must never join it from the same dispatcher.
		// Sync-over-async pattern
		if (_ownsSink)
			completion.GetAwaiter().GetResult();
	}

	public Task DisposeAsync() {
		using var scope = _queueScope.EnterWriteScope();
		if (_disposeTask != null)
			return _disposeTask;
		_accepting = false;
		_messages.Writer.TryComplete();
		var worker = _queueWorker ?? Task.CompletedTask;
		return _disposeTask = Task.Run(async () => {
			await worker.ConfigureAwait(false);
			using (EnterWriteScope()) {
				if (!_disposed) {
					_disposed = true;
					CloseSink();
				}
			}
			if (_queueFailure != null)
				ExceptionDispatchInfo.Capture(_queueFailure).Throw();
		});
	}

	private async Task ProcessQueueAsync() {
		await foreach (var message in _messages.Reader.ReadAllAsync().ConfigureAwait(false)) {
			Tools.Exceptions.ExecuteCapturingException(() => Forward(LogOptions.InfoEnabled, (logger, _) => logger.Info(message.Text), queued: true, includesTimestamp: message.IncludesTimestamp), out var failure);
			using (_queueScope.EnterWriteScope()) {
				_queuedCharacters -= message.Text.Length;
				_queueFailure ??= failure;
			}
			message.Completion.TrySetResult();
		}
	}

	private async Task AwaitWritesAsync(Task completion) {
		await completion.ConfigureAwait(false);
		if (_queueFailure != null)
			ExceptionDispatchInfo.Capture(_queueFailure).Throw();
	}

	private void Forward(LogOptions level, Action<ILogger, LogOptions> write, bool queued = false, bool includesTimestamp = false) {
		Exception failure = null;
		ILogger borrowedSink = null;
		LogOptions options;
		using (EnterWriteScope()) {
			if (!queued && !_accepting || _sink == null || (_options & level) == 0)
				return;
			options = _options;
			var sink = includesTimestamp ? _untimestampedSink : _sink;
			if (_ownedSink == null) {
				borrowedSink = sink;
			} else {
				Tools.Exceptions.ExecuteCapturingException(() => write(sink, options), out failure);
				if (failure != null)
					Tools.Exceptions.ExecuteIgnoringException(CloseSink);
			}
		}
		// External loggers may synchronously dispatch to this control's UI thread. Never hold our
		// lifetime scope across borrowed code; its actual owner controls completion and disposal.
		if (borrowedSink != null) {
			Tools.Exceptions.ExecuteCapturingException(() => write(borrowedSink, options), out failure);
			if (failure != null) {
				using var scope = EnterWriteScope();
				CloseSink();
			}
		}
		if (failure == null)
			return;
		LoggingFailed?.Invoke(failure);
		ExceptionDispatchInfo.Capture(failure).Throw();
	}

	private void CloseSink() {
		_accepting = false;
		var resource = _ownedSink;
		_sink = null;
		_untimestampedSink = null;
		_ownedSink = null;
		resource?.Dispose();
	}

	private sealed class CompletingTextWriterLogger : TextWriterLogger {
		private readonly TextWriter _writer;

		public CompletingTextWriterLogger(TextWriter writer)
			: base(writer) {
			_writer = writer;
		}

		protected override void Log(LogLevel level, string message) {
			_writer.WriteLine($"[{level}] {message}");
			_writer.Flush();
		}
	}

	private sealed class ExceptionOutputLogger : LoggerBase {
		private readonly ILogger _logger;

		public ExceptionOutputLogger(ILogger logger) => _logger = logger;

		protected override void Log(LogLevel level, string message) => _logger.Error(message);
	}

	private sealed class QueuedMessage {
		public QueuedMessage(string text, bool includesTimestamp) { Text = text; IncludesTimestamp = includesTimestamp; }
		public string Text { get; }
		public bool IncludesTimestamp { get; }
		public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}
}
