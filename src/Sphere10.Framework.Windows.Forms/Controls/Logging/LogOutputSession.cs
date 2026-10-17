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

namespace Sphere10.Framework.Windows.Forms;

// Each activation has a distinct logger. Its scope joins writes before disposing owned resources;
// a worker that captured this logger immediately before Stop subsequently sees a closed, harmless output.
internal sealed class LogOutputSession : SynchronizedObject, ILogger, IDisposable {
	public event EventHandlerEx<Exception> LoggingFailed;

	private ILogger _sink;
	private IDisposable _ownedSink;
	private LogOptions _options;
	private bool _disposed;

	private LogOutputSession(ILogger sink, IDisposable ownedSink, LogOptions options) {
		_sink = sink;
		_ownedSink = ownedSink;
		_options = options;
	}

	public LogOptions Options {
		get { using var scope = EnterReadScope(); return _options; }
		set { using var scope = EnterWriteScope(); _options = value; }
	}

	public bool IsLogging {
		get { using var scope = EnterReadScope(); return _sink != null; }
	}

	public static LogOutputSession Create(LogOutputConfiguration configuration, ILogger applicationLogger) {
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
				default:
					throw new ArgumentOutOfRangeException(nameof(configuration));
			}
			if (configuration.TimestampEnabled)
				sink = new TimestampLogger(sink, "yyyy-MM-dd HH:mm:ss.fff zzz");
			return new LogOutputSession(sink, resource, configuration.Options);
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

	public void Dispose() {
		using var scope = EnterWriteScope();
		if (_disposed)
			return;
		_disposed = true;
		CloseSink();
	}

	private void Forward(LogOptions level, Action<ILogger, LogOptions> write) {
		Exception failure = null;
		ILogger borrowedSink = null;
		LogOptions options;
		using (EnterWriteScope()) {
			if (_sink == null || (_options & level) == 0)
				return;
			options = _options;
			if (_ownedSink == null) {
				borrowedSink = _sink;
			} else {
				try {
					write(_sink, options);
				} catch (Exception error) {
					failure = error;
					try { CloseSink(); } catch { /* Preserve the failed write as the primary failure. */ }
				}
			}
		}
		// External loggers may synchronously dispatch to this control's UI thread. Never hold our
		// lifetime scope across borrowed code; its actual owner controls completion and disposal.
		if (borrowedSink != null)
			try {
				write(borrowedSink, options);
			} catch (Exception error) {
				failure = error;
				using var scope = EnterWriteScope();
				CloseSink();
			}
		if (failure == null)
			return;
		LoggingFailed?.Invoke(failure);
		ExceptionDispatchInfo.Capture(failure).Throw();
	}

	private void CloseSink() {
		var resource = _ownedSink;
		_sink = null;
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
}
