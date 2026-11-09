# Logging

The `ILogger` abstraction with console, debug, file, rolling-file, text-writer and delegate sinks, prefixing and threading decorators, and the application-wide `SystemLog`.

`ILogger` has one method per level (`Debug`, `Info`, `Warning`, `Error`) plus `Exception`, and an `Options` property of `LogOptions` flags that selects the enabled levels and whether exception details are written. `LoggerBase` filters by those flags and leaves one `Log(level, message)` method to implement. In debug builds it defaults to `LogOptions.VerboseProfile` and otherwise to `LogOptions.StandardProfile`, which omits debug and information messages. `LogLevel.ToLogOptions()` converts a minimum level into flags.

Decorators derive from `LoggerDecorator`. `TimestampLogger`, `ThreadIdLogger` and `PrefixLogger` prepend text, `SynchronizedLogger` serializes calls under a lock, and `AsyncLogger` queues every call on a serial worker. `MulticastLogger` fans each message out to child loggers and ignores exceptions thrown by them. `SystemLog` is a static `MulticastLogger` that starts with no sinks, so nothing is written until a sink is added with `SystemLog.RegisterLogger`.

`RollingFileLogger` owns an open file writer and implements `IDisposable`, so dispose it when it is removed. `ILoggerExtensions.EnterLogDurationScope` returns a scope that logs the elapsed milliseconds at debug level on disposal. Text writers used by the sinks are in [TextWriters](../TextWriters/README.md), and the serial worker behind `AsyncLogger` is in [Threading](../Threading/README.md).

```csharp
var logger = new TimestampLogger(new ConsoleLogger()) { Options = LogLevel.Info.ToLogOptions() };
SystemLog.RegisterLogger(logger);
SystemLog.Info("Started");
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionLogger`](ActionLogger.cs) | class | Logger that passes each message to one delegate or to a delegate per level. |
| [`AsyncLogger`](AsyncLogger.cs) | class | Decorator that runs every logging call on a `SerialThreadPool`, with an optional error handler. |
| [`ConsoleLogger`](ConsoleLogger.cs) | class | `TextWriterLogger` over a `ConsoleTextWriter`. |
| [`DebugLogger`](DebugLogger.cs) | class | `TextWriterLogger` over a `DebugTextWriter` that writes to the debugger output. |
| [`FileAppendLogger`](FileAppendLogger.cs) | class | `TextWriterLogger` that appends to a file through a `FileAppendTextWriter`, optionally creating its directory. |
| [`ILogger`](ILogger.cs) | interface | Level methods, `Exception` and the `Options` flags that every logger implements. |
| [`ILoggerExtensions`](ILogger.cs) | static class | Extends `ILogger` with `Log(level, message)`, component and method prefixed overloads, `Result` error logging and `EnterLogDurationScope`. |
| [`LogLevel`](LogLevel.cs) | enum | Severity of a log entry, from none and debug up to info, warning and error. |
| [`LogLevelExtensions`](LogLevel.cs) | static class | Extends `LogLevel` with `ToLogOptions`, which enables that level, every higher level and exception detail. |
| [`LogOptions`](LogOptions.cs) | enum | Flags for each level and exception detail, with verbose, standard and user-display profiles. |
| [`LoggerBase`](LoggerBase.cs) | abstract class | Filters calls by `Options`, formats exceptions, and forwards the rest to an abstract `Log(level, message)`. |
| [`LoggerDecorator`](LoggerDecorator.cs) | class | Forwards every call and `Options` to an inner logger, with virtual level methods for decorators to override. |
| [`MulticastLogger`](MultiCastLogger.cs) | class | Sends each message to a synchronized list of child loggers, sequentially or in parallel, ignoring child failures. |
| [`NoOpLogger`](NoOpLogger.cs) | class | Logger that discards every message. |
| [`PrefixLogger`](PrefixLogger.cs) | class | Decorator that prepends a fixed prefix to every message. |
| [`PrefixLoggerBase`](PrefixLoggerBase.cs) | abstract class | Decorator that prepends the text returned by an abstract `GetPrefix()` to every message. |
| [`RollingFileLogger`](RollingFileLogger.cs) | class | Disposable file logger that archives the active file at a byte-size limit into timestamped files and keeps at most `MaxFiles`. |
| [`SynchronizedLogger`](SynchronizedLogger.cs) | class | Wraps a logger so that calls from several threads are serialized by a reader-writer lock. |
| [`SystemLog`](SystemLog.cs) | static class | Application-wide multicast logger with static level methods and `RegisterLogger`, `DeregisterLogger` and `DeregisterAllLoggers`. |
| [`TextWriterLogger`](TextWriterLogger.cs) | class | Logger that writes each message as a `[Level] message` line to a `TextWriter`, by default the debug output, and swallows writer errors. |
| [`ThreadIdLogger`](ThreadIdLogger.cs) | class | Decorator that prefixes messages with the managed thread ID in a configurable format. |
| [`TimestampLogger`](TimestampLogger.cs) | class | Decorator that prefixes messages with the current time in a configurable format. |
