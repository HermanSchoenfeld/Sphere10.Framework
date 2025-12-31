// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;


public class MicrosoftExtensionsLoggerAdapter : Sphere10.Framework.LoggerDecorator, Microsoft.Extensions.Logging.ILogger {
	public MicrosoftExtensionsLoggerAdapter(Sphere10.Framework.ILogger decoratedLogger)
		: base(decoratedLogger) {
	}

	public IDisposable BeginScope<TState>(TState state) => Sphere10.Framework.Disposables.None; // 

	public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
		=> logLevel switch {
			Microsoft.Extensions.Logging.LogLevel.Trace => Options.HasFlag(Sphere10.Framework.LogOptions.DebugEnabled),
			Microsoft.Extensions.Logging.LogLevel.Debug => Options.HasFlag(Sphere10.Framework.LogOptions.DebugEnabled),
			Microsoft.Extensions.Logging.LogLevel.Information => Options.HasFlag(Sphere10.Framework.LogOptions.InfoEnabled),
			Microsoft.Extensions.Logging.LogLevel.Warning => Options.HasFlag(Sphere10.Framework.LogOptions.WarningEnabled),
			Microsoft.Extensions.Logging.LogLevel.Error => Options.HasFlag(Sphere10.Framework.LogOptions.ErrorEnabled),
			Microsoft.Extensions.Logging.LogLevel.Critical => Options.HasFlag(Sphere10.Framework.LogOptions.ErrorEnabled),
			Microsoft.Extensions.Logging.LogLevel.None => false,
			_ => throw new NotSupportedException($"{logLevel}")
		};

	public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
		switch (logLevel) {
			case Microsoft.Extensions.Logging.LogLevel.None:
				break;
			case Microsoft.Extensions.Logging.LogLevel.Trace:
			case Microsoft.Extensions.Logging.LogLevel.Debug:
				base.Debug(formatter(state, exception));
				break;
			case Microsoft.Extensions.Logging.LogLevel.Information:
				base.Info(formatter(state, exception));
				break;
			case Microsoft.Extensions.Logging.LogLevel.Warning:
				base.Warning(formatter(state, exception));
				break;
			case Microsoft.Extensions.Logging.LogLevel.Error:
			case Microsoft.Extensions.Logging.LogLevel.Critical:
				base.Error(formatter(state, exception));
				break;
			default:
				throw new NotSupportedException($"{logLevel}");
		}
		;

	}


}

