# Logging

The [application ownership guide](application-ownership.md) explains runtime and dependency ownership.

## Logging

Logging uses a simple `ILogger` interface with `Debug`, `Info`, `Warning`, `Error`, `Exception` methods controlled by `LogOptions` flags. Core types:

- **`ILogger`** — interface with level methods and `Options` property.
- **`LoggerBase`** — abstract base that filters by `LogOptions` and delegates to `Log(LogLevel, string)`.
- **`IApplication.Logger`** — use the runtime root logger in UI applications; screens reach it through `Block.Parent.Logger`, and application/plugin lifecycle tasks can inject `IApplication`.
- **Global WinForms initializers** — native startup selects the single root's exact `IApplication.Logger` after module setup, before process tasks, block registration or screen creation. Tasks can inject that application and use its logger. `Sphere10Framework.Progress` supplies splash task progress independently of the chosen logger.
- **Portable process initializers** — default and ASP.NET Core startup run their diagnostics through the registered Sphere10 `ILogger`, falling back to `SystemLog.Logger`. A singleton logger shares the same instance with `ApplicationBase.Logger`; inject it into process tasks with their own messages. Do not resolve a scoped UI application during process startup merely to obtain a logger.
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

`AsyncLogger` returns before writes finish and currently has no flush/drain or disposal contract. Application finalization does not imply that queued diagnostics have persisted. `TextBoxLogger` uses asynchronous composition, so detach its registration before the owning control is disposed. Inspect decorator disposal and `MulticastLogger.Options` limitations before composing sinks.

Use [resource scopes](resource-scopes.md) for registration cleanup and [scheduling](scheduling.md) for background job diagnostics. The [framework utility guidance](framework-utilities.md) keeps logging output separate from ordinary command-line results.

## Application operation outcomes

Log a workflow's actual final outcome through `IApplication.Logger`, including
successful, partial, cancelled and failed results. Use the existing `ILogger.Log`
level dispatch or `Exception(error, context)` rather than changing a borrowed
logger's Options. Keep result formatting and the choice of summary fields in
the application; reusable services return outcomes and accept the common logger
contract.

Record the outcome inside the worker's owned lifetime, before its admission or
shutdown-drain registration completes. A disposed progress window or abandoned
UI continuation must not prevent the final diagnostic. Preserve cancellation
and exception details, distinguish attempted actions from successful objects,
and keep credentials out of reports. Optional logging adds another sink; when
it selects the same application logger, avoid sending the outcome there twice.
See SystemExpert's [migration observer](../../../../SystemExpert/src/SystemExpert.FileServerMigrator/Dialogs/ActionObserverForm.cs)
and its [operation tests](../../../../SystemExpert/tests/SystemExpert.Tests/MigrationOperationTests.cs)
for an application example; these links require the sibling checkout.

## Optional visual output

For optional output in a native form, reuse the public
[LogOutputSelectorControl](../../../src/Sphere10.Framework.Windows.Forms/Controls/LogOutputSelectorControl.cs).
Its checkbox opens the shared wizard on first activation to select File,
Windows Application Log, Screen, Standard Output or Standard Error, followed by applicable options.
Destination names are bold with explanatory text beside them. Windows Application
Log forwards to the running application's supplied diagnostics logger.
Finish validates and opens the output before activating the checkbox. Later
activation reuses the accepted options. **...** (Configure Log Output) opens a designer-editable
details dialog with **Start Logging** / **Stop Logging** and a **Change** button that launches that same wizard. Cancellation
preserves the current output, including an output already active while editing.

`Logger` returns `NoOpLogger` while off and changes to the accepted session when
on. A long-lived worker/service must retrieve it per write through a callback;
caching the inactive logger would miss later activation. For example,
`FileSystemWatcherService` accepts `eventLoggerProvider: () => control.Logger`.
The selected session joins owned file writes before Stop/disposal; a previously
captured session ignores calls made after it closes. Borrowed callbacks run
outside the session scope, allowing synchronous UI dispatch without deadlocking
UI state reads/Stop. Their actual owner controls completion of dispatched writes.
The control owns file writers and rolling sinks, but borrows the application
logger and console writers. It must not dispose or globally reconfigure them.

For UI capture, use `EnqueueInfo(message)` to send through the session's bounded
worker queue instead of doing file IO on the caller. It returns false when off,
full (512 entries or 1 MiB of pending message characters), or closed. Workers can
use the same API. `FlushLoggingAsync()` joins already accepted entries; native Off
and Change actions drain the session before closing its sink. Prefer
`StopLoggingAsync()` for user Off and begin `DisposeAsync()` on the UI thread for
owner cleanup. Async disposal preserves remembered enabled preferences, disposes
the native control immediately and returns a drain with no later UI continuation.
Retain or await that task in the actual owner. Do not add another capture queue
that retains logger sessions past the selector's stop boundary.

Use `EnqueueInfo(message, includesTimestamp: true)` for a record that already
includes its capture timestamp. It bypasses the optional processing-time prefix
for that record without changing ordinary messages or the user's timestamp choice.

Related telemetry measurements from one sample belong in one log entry: one
capture timestamp and source title, followed by every related metric's readable
name, value and units, with device identities when needed. Group multi-series
chart observations by captured time or their shared numeric/category identity.
Keep different samples separate. Rendering, retained history and polling of an
unchanged observation must not duplicate capture output.

Screen output reuses `LogScreen` and the actual `IWinFormsApplicationScreenHost`.
Assign the borrowed `ScreenHost` when toolbar composition removes the selector
from its screen's parent chain; the containing native frame is also resolved.
`ScreenTitle` names the source, producing `Log Output - <NAME>` in both detached
windows and docked tabs. Generic names derive from the owning screen or window;
an existing prefix is not duplicated. Chart-owned selectors use the resolved
chart title, while borrowed selectors keep their caller's source name.
It opens undocked. The application host owns its displayed view until it closes,
so those logs remain readable after Stop or origin disposal. A host restricted
to a single view uses an independent detached host rather than replacing the
originating screen. A standalone selector owns its fallback screen host, which
closes with the selector. Closing the active view disables its selector.
The screen's bounded writer posts asynchronously, survives native handle
recreation, and drops writes after actual screen destruction. Its
`TextWriterLogger` composition adds no forced timestamp or unowned worker.
The view retains its originating navigation block when hosted. Its text buffers
are capped at 1,048,576 displayed characters and 262,144 pending characters;
destruction or direct disposal clears both.

Configure/start/stop on the UI thread; `Logger` reads and log calls support workers.
Use `LoggingStarting(configuration)` for consumer validation before file IO.
For **Remember After Close**, follow the
[logging preference lifecycle](user-ui-settings.md#reusable-logging-preferences):
explicitly restore only after the application's logger and consumer hooks are
bound, use stable owner/control IDs, and keep live loggers out of settings.
Read the [control usage](../../../src/Sphere10.Framework.Windows.Forms/README.md#logoutputselectorcontrol)
for configuration and failure events. A filesystem-monitor consumer must reject
file output in its monitored tree to prevent feedback.

`RollingFileLogger` measures encoded bytes including line endings/preambles,
assigns unique UTC timestamp archive names for rapid writes, and retains only
recognized archives belonging to that output. Its configured file count includes
the active file. Oversized entries can be clipped to its byte limit; choose a
limit suitable for the expected entry size.

Implementation references: [ILogger](../../../src/Sphere10.Framework/Logging/ILogger.cs),
[filtering base](../../../src/Sphere10.Framework/Logging/LoggerBase.cs),
[process registration](../../../src/Sphere10.Framework/Logging/SystemLog.cs),
[multicast options](../../../src/Sphere10.Framework/Logging/MultiCastLogger.cs),
[writer sink](../../../src/Sphere10.Framework/Logging/TextWriterLogger.cs),
[disposable rolling sink](../../../src/Sphere10.Framework/Logging/RollingFileLogger.cs),
[asynchronous decorator](../../../src/Sphere10.Framework/Logging/AsyncLogger.cs)
and [WinForms sink](../../../src/Sphere10.Framework.Windows.Forms/TextBox/TextBoxLogger.cs).
