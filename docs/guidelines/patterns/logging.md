# Logging

Read the [logging skill](../../../agents/skills/logging/SKILL.md) for logger APIs, sink behavior and current source limitations. The [application ownership guide](application-ownership.md) explains runtime and dependency ownership.

## Logging

Logging uses a simple `ILogger` interface with `Debug`, `Info`, `Warning`, `Error`, `Exception` methods controlled by `LogOptions` flags. Core types:

- **`ILogger`** — interface with level methods and `Options` property.
- **`LoggerBase`** — abstract base that filters by `LogOptions` and delegates to `Log(LogLevel, string)`.
- **`IApplication.Logger`** — use the runtime root logger in UI applications; screens reach it through `Block.Parent.Logger`, and lifecycle tasks can inject `IApplication`.
- **`SystemLog`** — static process-wide logger backed by a `MulticastLogger`. Call `SystemLog.Info(...)`, `SystemLog.Error(...)`, etc. Register sinks via `SystemLog.RegisterLogger(logger)`.
- **Decorator loggers** — `TimestampLogger`, `ThreadIdLogger`, `PrefixLogger`, `SynchronizedLogger`, `AsyncLogger` wrap an inner `ILogger` to add behavior.
- **Sink loggers** — `ConsoleLogger`, `DebugLogger`, `FileAppendLogger`, `RollingFileLogger`, `TextWriterLogger`, `EventLogLogger` (Windows), `TextBoxLogger` (WinForms).
- `LoggerBase` default options: `VerboseProfile` in debug builds, `StandardProfile` otherwise (set via `Tools.Runtime.IsDebugBuild`). Concrete sinks can override this; `TextWriterLogger` and `RollingFileLogger` choose `VerboseProfile`. Configure the actual sink's options when a specific profile is required.

Register and deregister a sink at its owning lifetime boundary. `SystemLog.RegisterLogger` and `DeregisterLogger` do not dispose the sink. `ILogger` does not define disposal: `RollingFileLogger` implements `IDisposable`, but `TextWriterLogger` and plain logger decorators do not. Add only application-owned disposable resources to `Disposables`; retain and dispose an owned writer separately when wrapping it with `TextWriterLogger`. Injected or borrowed loggers retain their DI or external owner.

`MulticastLogger.Options` broadcasts assignments to current sinks; its getter
throws `NotSupportedException`, and later registrations do not inherit an earlier
assignment. Configure each new sink's options explicitly. The current
`SystemLog.Exception(componentName, methodName, exception)` overload reads that
unsupported getter; use `SystemLog.Exception(exception, message)` for context.

`AsyncLogger` returns before writes finish and currently has no flush/drain or disposal contract. Application finalization does not imply that queued diagnostics have persisted. `TextBoxLogger` uses asynchronous composition, so detach its registration before the owning control is disposed. Follow the skill for decorator disposal and `MulticastLogger.Options` limitations.

Use [resource scopes](resource-scopes.md) for registration cleanup and [scheduling](scheduling.md) for background job diagnostics. The [framework utility guidance](framework-utilities.md) keeps logging output separate from ordinary command-line results.

Implementation references: [ILogger](../../../src/Sphere10.Framework/Logging/ILogger.cs),
[filtering base](../../../src/Sphere10.Framework/Logging/LoggerBase.cs),
[process registration](../../../src/Sphere10.Framework/Logging/SystemLog.cs),
[multicast options](../../../src/Sphere10.Framework/Logging/MultiCastLogger.cs),
[writer sink](../../../src/Sphere10.Framework/Logging/TextWriterLogger.cs),
[disposable rolling sink](../../../src/Sphere10.Framework/Logging/RollingFileLogger.cs),
[asynchronous decorator](../../../src/Sphere10.Framework/Logging/AsyncLogger.cs)
and [WinForms sink](../../../src/Sphere10.Framework.Windows.Forms/TextBox/TextBoxLogger.cs).
