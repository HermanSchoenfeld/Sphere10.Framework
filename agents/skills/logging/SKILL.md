---
name: logging
description: Add diagnostics through Sphere10 ILogger, IApplication.Logger or SystemLog using the existing logger decorators and sinks. Use when implementing logging, configuring sinks or integrating logging with application and screen lifetimes.
---

# Logging Skill

Read [logging guidance](../../../docs/guidelines/patterns/logging.md) for logger selection, composition and sink ownership.

## Emit log messages
- UI application code: use `IApplication.Logger`; hosted screens can reach it through `Block.Parent.Logger` while attached. Plugin lifecycle tasks can inject `IApplication` and use that logger; the initializer/finalizer interfaces do not supply a logger property. The root resolves `Sphere10.Framework.ILogger` from its provider, with `SystemLog.Logger` as fallback. Distinguish this type from `Microsoft.Extensions.Logging.ILogger` when both namespaces are imported.
- `StartWinFormsApplication` constructs its single application after module setup and routes every global initializer's diagnostics through that exact `IApplication.Logger` before installing blocks or creating screens. Native lifecycle tasks can inject `IApplication` and use its logger. Process task progress updates the splash even with a custom logger.
- Default and ASP.NET Core process startup run before a UI application or browser circuit exists. `Sphere10Framework` sends initializer diagnostics through the registered Sphere10 `ILogger`, with `SystemLog.Logger` as fallback. Register a singleton logger to share it with `ApplicationBase.Logger`; portable process tasks with their own messages should inject that logger rather than create a UI application or hardcode `SystemLog`.
- Process diagnostics outside an application can use `SystemLog.Info(...)`, `SystemLog.Error(...)`, etc. (backed by a `MulticastLogger`).
- Use `ConsoleLogger` for console diagnostics instead of ad hoc `Console.WriteLine`; ordinary command-line result output is separate from logging.
- Register sinks with `SystemLog.RegisterLogger(logger)` at the owning startup boundary; deregister the same instance with `SystemLog.DeregisterLogger(logger)` when its lifetime ends. Registration and deregistration do not dispose a sink. Avoid repeatedly registering a per-screen sink on each activation.
- Log exceptions with `logger.Exception(error, message)` or `SystemLog.Exception(error, message)`. In the current source, the `SystemLog.Exception(componentName, methodName, error)` overload reads the multicast logger's unsupported `Options` getter; use the exception/message overload for context.

## Implementing a logger
- Implement `ILogger` (methods `Debug`, `Info`, `Warning`, `Error`, `Exception`, plus `Options`) or derive `LoggerBase`, which filters by `LogOptions` and delegates to `Log(LogLevel, string)`.
- `LoggerBase` defaults to `VerboseProfile` in debug builds and `StandardProfile` otherwise (via `Tools.Runtime.IsDebugBuild`). Concrete sinks can override that default: `TextWriterLogger` and `RollingFileLogger` currently choose `VerboseProfile`. Configure the actual sink's `Options` explicitly when a particular profile is required.
- `MulticastLogger.Options` broadcasts assignments to currently registered sinks; its getter throws `NotSupportedException`. Configure newly added sinks independently rather than assuming they inherit an earlier multicast assignment.

## Composition
- Decorators wrap an inner `ILogger`: `TimestampLogger`, `ThreadIdLogger`, `PrefixLogger`, `SynchronizedLogger`, `AsyncLogger`.
- Sinks: `ConsoleLogger`, `DebugLogger`, `FileAppendLogger`, `RollingFileLogger`, `TextWriterLogger`, `EventLogLogger` (Windows), `TextBoxLogger` (WinForms).

`SynchronizedLogger` serializes calls; `AsyncLogger` queues them on a dedicated worker and returns before the write completes. The current `AsyncLogger` exposes no flush/drain or disposal contract, so do not treat a returned log call as completed persistence or assume application finalization drains it. `TextBoxLogger` uses that asynchronous composition; detach its registration before its owning control is disposed.

Keep ownership explicit. Dispose a `RollingFileLogger` or a supplied `TextWriter` through its actual owner; plain logger decorators and `TextWriterLogger` do not propagate disposal. `ILogger` does not imply `IDisposable`: add only application-owned resources that implement `IDisposable` to `Disposables`, and retain an owned writer separately when wrapping it with `TextWriterLogger`. An injected/borrowed logger retains its DI or external owner. Apply [disposable scopes](../disposable-scopes/SKILL.md) for registration cleanup.

Current-source references: [ILogger and extensions](../../../src/Sphere10.Framework/Logging/ILogger.cs), [LoggerBase](../../../src/Sphere10.Framework/Logging/LoggerBase.cs), [SystemLog](../../../src/Sphere10.Framework/Logging/SystemLog.cs), [MulticastLogger](../../../src/Sphere10.Framework/Logging/MultiCastLogger.cs), [AsyncLogger](../../../src/Sphere10.Framework/Logging/AsyncLogger.cs), and [application ownership](../../../src/Sphere10.Framework.Application/README.md#application-initialization-and-shutdown).
