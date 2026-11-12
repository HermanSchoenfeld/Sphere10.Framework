# 🛠️ Tools.* Namespace Reference

**Scope:** Source-audited utility catalog for the current Framework repository.
**Core catalog source audit:** October 12, 2026. **Windows and WinForms discovery update:** November 4, 2026.

Type `Tools.` in IntelliSense to discover the utilities exposed by the assemblies
your project references. Most facades are in the global `Tools` namespace; web
helpers use `Tools.Web`. A filename such as `StreamTool.cs` does not establish a
`Tools.Stream` type: use the declaring class and its actual signature.

The tables list the public utility facades and representative existing members.
Follow each source link for overloads, defaults and ownership behavior. Package
and platform references remain necessary; importing `Sphere10.Framework` does
not add another assembly or introduce a missing helper.

## Table of Contents

1. [Core Framework Tools](#core-framework-tools)
2. [Platform-Specific Tools](#platform-specific-tools)
3. [Database Tools](#database-tools)
4. [Application & Testing Tools](#application--testing-tools)
5. [Usage Patterns](#usage-patterns)
6. [Adding New Tools](#adding-new-tools-for-contributors)

## Core Framework Tools

These facades are declared in `Sphere10.Framework`, including
`Tools.Web.Downloader`.

### Collection & Data Structure Tools

| Facade | Existing entrypoints | Implementation |
|---|---|---|
| `Tools.Array` | `Clone`, `Concat`, `Fill`, `Gen`, `Transpose`, `GetReversedCopy` | [ArrayTool.cs](../src/Sphere10.Framework/Collections/Arrays/ArrayTool.cs) |
| `Tools.Collection` | `GenerateArray`, `Repeat`, `Partition`, `BinarySearch`, `RangeL`, `ResizeArray` | [CollectionTool.cs](../src/Sphere10.Framework/Collections/CollectionTool.cs) |
| `Tools.Streams` | `ReadByteArray`, `RouteStream`, `WriteStreamToFile`, GZip compression and encryption helpers | [StreamTool.cs](../src/Sphere10.Framework/Streams/StreamTool.cs) |
| `Tools.Spans` | `Empty<T>()` | [SpanTool.cs](../src/Sphere10.Framework/Spans/SpanTool.cs) |
| `Tools.DataSources` | `ReadAllAsync` loads a loadable source when required and returns every row; `CaptureAsync` owns, reads and drains a temporary source; `Query` searches, sorts and pages rows | [DataSourceTool.cs](../src/Sphere10.Framework/DataSource/DataSourceTool.cs) |

`Tools.Array.Gen(size, value)` repeats one value; indexed generation belongs to
`Tools.Collection.GenerateArray(count, generator)`. Array resizing belongs to
`Tools.Collection.ResizeArray(ref array, length)`. Filtering and projection can
use the existing sequence extensions or LINQ; `Tools.Collection` does not provide
`Where`, `Select`, `Flatten` or `GroupBy` methods.

`ReadByteArray` defaults `closeStream` to `true`, and `WriteStreamToFile` defaults
`closeReadStream` to `true`. Pass `false` when borrowing a caller-owned stream.
Stream decorators and bounded streams are separate framework types, rather
than factory methods on a `Tools.Stream` facade.

### Cryptography Tools

| Facade | Existing entrypoints | Implementation |
|---|---|---|
| `Tools.Crypto` | `GenerateCryptographicallyRandomBytes`, `NewCryptographicGuid`, `PasswordHash`, `ValidatePassword`, `EncryptStringAES`, `DecryptStringAES`, `SecureErase` | [CryptoTool.cs](../src/Sphere10.Framework/Crypto/CryptoTool.cs) |

Hash algorithms are accessed through
[Hashers](../src/Sphere10.Framework/Crypto/Hashing/Hashers.cs), for example
`Hashers.Hash(CHF.SHA2_256, payload)`. `Tools.Crypto` has no `SHA256`,
`BLAKE2B`, `Sign` or `VerifySignature` methods. Signature operations belong to
the selected [signature scheme](../src/Sphere10.Framework/Crypto/IDigitalSignatureScheme.cs).
Register the required algorithms before hashing; availability depends on the
referenced module and registry configuration. See
[cryptographic utility guidance](guidelines/patterns/framework-utilities.md#cryptography).

### Text, Values & Time

| Facade | Existing entrypoints | Implementation |
|---|---|---|
| `Tools.Text` | `ToCasing`, `FormatEx`, `GenerateRandomString`, text/base encoders and compression | [TextTool.cs](../src/Sphere10.Framework/Text/TextTool.cs) |
| `Tools.Parser` | `Parse<T>`, `TryParse<T>`, `SafeParse<T>` and runtime-type overloads | [ParseTool.cs](../src/Sphere10.Framework/Text/ParseTool.cs) |
| `Tools.Enums` | `GetValues<T>`, `GetDescription`, `GetHumanReadableName`, `ParseEnum<T>` | [EnumTool.cs](../src/Sphere10.Framework/Values/EnumTool.cs) |
| `Tools.Values` | `ClipValue`, `Swap`, numeric formatting and nested `Future` factories | [ValueTool.cs](../src/Sphere10.Framework/Values/ValueTool.cs) |
| `Tools.Time` | `FromUnixTime`, `CalculateNextDayOfWeek`, `CalculateNextDayOfMonth`, `Min`, `Max` | [DateTimeTool.cs](../src/Sphere10.Framework/Values/DateTimeTool.cs) |

The future factories are `Tools.Values.Future.Explicit`, `LazyLoad`,
`Reloadable`, `AlwaysLoad`, `BackgroundFetched` and `Projection`.
Read [future semantics](guidelines/patterns/futures.md) before choosing caching,
invalidation, projection or background loading; IFuture is not an async Task.
Object cloning belongs to `Tools.Object`, rather than a `Tools.Value` facade.

### Numeric & Memory Tools

| Facade | Existing entrypoints | Implementation |
|---|---|---|
| `Tools.Maths` | `Abs`, `EpsilonClip`, `Rotate2D`, `NextInt`, distances and index permutations | [MathTool.cs](../src/Sphere10.Framework/Maths/MathTool.cs) |
| `Tools.MathPlus` | `Integrate`, `SafeIntegrate`, `Derivative`, `ArcLength` | [MathPlusTool.cs](../src/Sphere10.Framework/Maths/MathPlusTool.cs) |
| `Tools.Memory` | `GetBytesReadable`, `ConvertMemoryMetric`, `SizeOfPrimitive` | [MemoryTool.cs](../src/Sphere10.Framework/Memory/MemoryTool.cs) |

`MathPlus` is a numerical calculus helper, not a prime/GCD utility.
`Memory` provides metrics and primitive sizing, not buffer allocation or clearing.

### Type, Reflection & Object Tools

| Facade | Existing entrypoints | Implementation |
|---|---|---|
| `Tools.Reflection` | `GetPropertyValue`, `SetPropertyValue`, `GetFieldValue`, `SetFieldValue`, `InvokeMethod` | [ReflectionTool.cs](../src/Sphere10.Framework/Introspection/ReflectionTool.cs) |
| `Tools.Mapping` | `GetMember`, `GetAccessor` from model expressions | [ReflectionHelper.cs](../src/Sphere10.Framework/Mapping/ReflectionHelper.cs) |
| `Tools.Object` | `Create`, `ResolveType`, `ChangeType`, `Clone`, `CopyMembers` | [ObjectTool.cs](../src/Sphere10.Framework/Objects/ObjectTool.cs) |

General type activation is also supplied by
[TypeActivator](../src/Sphere10.Framework/Types/TypeActivator.cs) and
the framework's `Type` extensions. `Tools.Reflection.CreateInstance` is not
an existing activation API.

### I/O & Network Tools

| Facade | Existing entrypoints | Implementation |
|---|---|---|
| `Tools.FileSystem` | `GenerateTempFilename`, `CopyFile`, `DeleteFile`, `GetFiles`, `WriteAllBytes` | [FileSystemTool.cs](../src/Sphere10.Framework/IO/FileSystemTool.cs) |
| `Tools.Network` | `GetMimeType`, `GetNetworkAddress`, `GetMacAddresses` | [NetworkTool.cs](../src/Sphere10.Framework/Network/NetworkTool.cs) |
| `Tools.Url` | `EncodeUrl`, `DecodeUrl`, `ToUrlSlug`, `ParseQueryString`, `ToQueryString` | [UrlTool.cs](../src/Sphere10.Framework/Network/UrlTool.cs) |
| `Tools.Mail` | `IsValidEmail`, `ParseEmailsFromCommaSeparatedList`, SMTP send helpers | [MailTool.cs](../src/Sphere10.Framework/Network/MailTool.cs) |
| `Tools.Web.Downloader` | `DownloadFile`, `DownloadFileAsync`, filename parsing | [Downloader.cs](../src/Sphere10.Framework/Network/Downloader.cs) |

`Tools.FileSystem` has no `ReadAllBytes`, `ReadAllText` or `WriteAllText`
method. Do not infer a wrapper exists from a BCL method's name. Network and mail
helpers also expose their specific transport and argument contracts; the old
connectivity/download shorthand is not a general `IsInternetAvailable` or
`Download(url)` API.

### Functional, Threading & Runtime Tools

| Facade | Existing entrypoints | Implementation |
|---|---|---|
| `Tools.Lambda` | `ActionWithRetry`, `ActionWithFailOver`, `CreateSetterExpression`, `NoOp` | [LambdaTool.cs](../src/Sphere10.Framework/Functional/LambdaTool.cs) |
| `Tools.Expressions` | Unary/binary `CreateExpression` factories | [ExpressionTool.cs](../src/Sphere10.Framework/Functional/ExpressionTool.cs) |
| `Tools.OperatorTool` | Generic `Add`, `Subtract`, `Multiply`, `Divide` and comparison operations | [OperatorTool.cs](../src/Sphere10.Framework/Functional/OperatorTool.cs) |
| `Tools.Threads` | `QueueAction`, `ExecuteConcurrently`, `EnterLockScope`, `MeasureDuration` | [ThreadTool.cs](../src/Sphere10.Framework/Threading/ThreadTool.cs) |
| `Tools.Runtime` | `IsDebugBuild`, `GetExecutablePath`, `GetEntryAssembly`, design-mode checks | [RuntimeTool.cs](../src/Sphere10.Framework/Environment/RuntimeTool.cs) |
| `Tools.Debugger` | Shared diagnostic counters/objects/messages and `AddMessage` | [Debugger.cs](../src/Sphere10.Framework/Environment/Debugger.cs) |
| `Tools.CodeAccessSecurity` | `HasUnrestrictedFeatureSet` compatibility property | [CodeAccessSecurityTool.cs](../src/Sphere10.Framework/Environment/CodeAccessSecurityTool.cs) |

`Tools.Expressions` has no `BuildPredicate` or `CombinePredicates`.
The current `Tools.Runtime.IsWebApp` returns `false`; these helpers do not
provide general framework-version or OS inspection. `Tools.Debugger` is not a
wrapper for `System.Diagnostics.Debugger.Break` or logging sinks.

Some legacy thread helpers return no completion task.
`Tools.Threads.CreateAwaitTask` constructs an unstarted task whose body starts
the async method and joins it with `WaitSafe`; the caller must start it, and it
is not an awaited replacement for an async operation. `WaitSafe` and
`ResultSafe` block until a started task completes and rethrow its own
exception; they cannot undo a synchronization context the task already
captured. Follow the
[sync/async completion rules](guidelines/patterns/sync-async.md#adapter-completion-and-cancellation)
when the caller needs completion and failure propagation.

### Exception & Scope Tools

| Facade | Existing entrypoints | Implementation |
|---|---|---|
| `Tools.Exceptions` | `ExecuteCapturingException` (typed/filter overloads), `ExecuteCapturingExceptionAsync`, `Execute`, `ExecuteIgnoringException`, `TryOrDefault`; see [execution/capture practice](guidelines/patterns/framework-utilities.md#exception-execution-and-capture) | [ExceptionTool.cs](../src/Sphere10.Framework/Exceptions/ExceptionTool.cs) |
| `Tools.Scope` | `ExecuteOnDispose[Async]` (plain/generic concrete returns), `EnterReadScope`, `EnterWriteScope`, `EnterAccessScope`, `EnterRestorePositionScope`, `EnterRestorePositionSeek`, `EnterBorrowHasherScope`, `EnterBorrowArrayScope` (byte/generic, shared/custom pool), `EnterOpenPageScope`, `EnterLockScope`, `EnterWaitScope`, directional scopes, `DeleteFileOnDispose[Async]` / `DeleteDirOnDispose[Async]`, single-instance and no-op values | [ScopeTool.cs](../src/Sphere10.Framework/Scopes/ScopeTool.cs) |

`Tools.Scope.ExecuteOnDispose` and `ExecuteOnDisposeAsync` return concrete `ActionScope` / `ActionScope<T>` and `TaskScope` / `TaskScope<T>` values for ad-hoc callbacks. Generic overloads take the callback first and item second, preserving static callbacks without boxing or an additional closure; see the [scope implementation guide](guidelines/patterns/resource-scopes.md#choosing-a-disposal-implementation). Transaction begin/commit/rollback
belongs to the relevant [transactional scope](guidelines/patterns/resource-scopes.md),
not `Tools.Scope.CreateScope()`.
Windows/WinForms contribute platform scope factories as C# 14 static extension
members on this same facade. Import `using Tools;` in addition to referencing
the platform assembly; see [scope factories](guidelines/patterns/resource-scopes.md#toolsscope-factories).

## Platform-Specific Tools

Reference the declaring project and meet its platform requirements.

| Facade | Project | Existing entrypoints and source |
|---|---|---|
| `Tools.Windows` | Sphere10.Framework.Windows | Thread-static native utility discovery, including `Windowing`, `Drivers`, `Hooks`, resource operations and fresh inventory factories: [WindowsTool.cs](../src/Sphere10.Framework.Windows/WindowsTool.cs) |
| `Tools.WinForms` | Sphere10.Framework.Windows.Forms | Fresh widgets and adapters grouped under `Windowing`, `Charts`, `Logging`, `Grids`, `Review` and `Controls`, plus existing cursor and window-settings operations. See the tables below. |
| `Tools.Web.Html` | Sphere10.Framework.Web | `Percent`, `Beautify` and animation classes: [HtmlTool.cs](../src/Sphere10.Framework.Web/HtmlTool.cs) |
| `Tools.Web.AspNetCore` | Sphere10.Framework.Web.AspNetCore | `ParseNetwork`: [AspNetCoreTool.cs](../src/Sphere10.Framework.Web.AspNetCore/AspNetCoreTool.cs) |
| `Tools.Web.Mvc` | Sphere10.Framework.Web.AspNetCore.MVC | `ToSelectList` enum helpers: [MvcTool.cs](../src/Sphere10.Framework.Web.AspNetCore.MVC/MvcTool.cs) |
| `Tools.BlazorGrid` | Sphere10.Framework.Web.AspNetCore.Blazor | Supported types, input selection, `FormatValue` and `ParseValue`: [GridTool.cs](../src/Sphere10.Framework.Web.AspNetCore.Blazor/Grids/GridTool.cs) |
| `Tools.iOSTool` | Sphere10.Framework.iOS | Native controller, keyboard and image helpers: [iOSTool.cs](../src/Sphere10.Framework.iOS/iOSTool.cs) |

`Tools.Windows` properties return the utility instances that implement Windows
operations; there is no `Tools.WinTool`, `Tools.WindowsTool` or `Tools.WinShell`
facade. `Tools.Drawing` is supplied by the separate Drawing project below.
The iOS project retains its legacy Xamarin target; consult its
[project guide](../src/Sphere10.Framework.iOS/README.md) before selecting that integration.
The Android project does not define a `Tools.AndroidTool` facade.

### Windows operations and resource factories

Use the existing utility object, or ask it for the existing concrete resource
class. These are thin entry points to the original implementations. Discovery
does not enumerate the machine, install a hook, open a process or start capture.
An explicit operation or resource constructor retains its existing behavior.

| Route | Operations or existing owner returned |
|---|---|
| `Tools.Windows.Windowing` | `Tools.Windows.Windowing`: window enumeration, pointer/physical-point queries, captured-owner bounds and verified Post/Send/main-window message dispatch. |
| `Tools.Windows.Drivers` | `Tools.Windows.Drivers`: existing installed-package queries and INF/registered driver-image path resolution. |
| `Tools.Windows.UiAutomation` | Existing isolated read-only capture and `TryRunAutomationWorker` startup dispatch. |
| `Tools.Windows.Objects` | Existing bounded native object-directory/symbolic-link reads and `GetRetainedCharacterCount` over detached captures. |
| `Tools.Windows.Hooks` | Existing input simulation and button-state operations; `CreateMouseHook` / `CreateKeyboardHook` return fresh native hook owners. They are installed and started explicitly through their existing APIs. |
| `Tools.Windows.Processes` | `Open` / `OpenAsync` return an owned `WindowsProcess`; identity, command, console signal and scheduling operations forward to their existing services. Fresh suspension, working-set-watch, worker-host and worker-session factories keep their original lifetime contracts. |
| `Tools.Windows.Processes.CreateDataSource()` | Fresh pageable `WindowsProcessDataSource`; its first read captures data. |
| `Tools.Windows.Services.CreateDataSource(drivers: false)` | Fresh pageable service or system-driver inventory. |
| `Tools.Windows.Network.CreateEndpointDataSource(...)` / `CreateInterfaceDataSource()` | Existing TCP/UDP owner-table and interface snapshot sources. Preserve family/PID/options arguments. |
| `Tools.Windows.Drives.CreateDataSource()` / `CreatePhysicalDiskDataSource()` / `CreatePartitionDataSource()` | Existing volume, physical-disk and partition sources; preserve probe and timeout options. |
| `Tools.Windows.Display.CreateDataSource()` | Existing monitor/display inventory. |
| `Tools.Windows.Startup.CreateRegistrationDataSource(...)` | Existing startup-registration source with selected provenance and limits. |
| `Tools.Windows.Computer` | Existing machine/power/system-counter reads; `CreatePowerPlanDataSource`, `CreatePowerSettingDataSource`, `CreatePowerDiagnosticDataSource` and a fresh `CreatePowerCfgSentry`. |
| `Tools.Windows.Files.CreateHandleService(...)` / `CreateInspectionService()` | Existing file/kernel-handle capture, blocking-process/explicit-close operations and file inspection. File helper startup dispatch is `TryRunHandleWorker`. |
| `Tools.Windows.Assemblies` | Existing C#/IL decompilation, references, resources, BAML/XAML and project export. |
| `Tools.Windows.Managed`, `.Dumps`, `.Symbols` | Existing runtime/heap/native-and-managed-dump analysis, confirmed dump capture and symbol operations. Capture-helper startup dispatch is `Dumps.TryRunCaptureWorker`. |
| `Tools.Windows.TraceSessions` | Existing accessible session/trace operations plus `TryReadCpuProfileSample` / `TryReadCpuProfileStack` over captured records. |
| `Tools.Windows.Pipes` | Existing passive names/path discovery and borrowed connected-pipe client/server process-ID queries. |

Inventory factories also expose their existing capture-delegate constructors
where the source supports them. Every factory call returns an independent source;
no Tools singleton retains its snapshot or chooses a refresh timer. Async native
operations retain the original worker dispatch and cancellation. Synchronous
native operations still belong on a worker.

Dispose returned process handles, sessions, watches and executable wrappers as
their types require. Suspension services remain application-owned and require
their existing finalization tasks. Working-set watch still needs explicit user
confirmation: Windows does not provide a disable operation for the target's
enabled watch. A tool proxy changes neither permission nor operation policy.

### WinForms widgets and adapters

Reference `Sphere10.Framework.Windows.Forms` and import `using Tools;` to discover
the C# 14 `Forms` extension property on `Tools.Windows`. It returns the thread's
lightweight `Tools.WinForms`; native Windows code has no dependency on the UI
assembly. Each factory returns the existing
class with the existing defaults. Create and use controls on their UI thread;
the returned control belongs to the caller or its containing native control.

| Group | Entry points |
|---|---|
| `Tools.WinForms.Windowing` | `CreatePicker(Control dispatcher)`; its dispatcher must already have a native handle. The picker owns its input hooks. Existing highlights remain `Tools.Scope.EnterHighlightScope`. |
| `Tools.WinForms.Charts` | `CreateGraphChart`, `CreateBarChart`, `CreateTelemetryChart`, `CreateChart`; `CreateLogOutputBehavior` attaches caller-owned behavior to a bare DataVisualization chart. `FormatLogSample` delegates the existing captured-sample formatter. |
| `Tools.WinForms.Logging` | `CreateOutputSelector`, `RegisterOutputLifetime`; configuration, files and host-owned log screens retain their existing lifecycle. |
| `Tools.WinForms.Grids` | `CreateVisualGrid`, `CreateDataSourceController`, `CreateArrayController`, `CreateArrayDetail`, `CreateControlDetail`. Controllers borrow existing sources/grids; detail adapters retain their documented control/resource ownership. |
| `Tools.WinForms.Review` | `CreateHexEditor`, `ShowBytesAsync`, `ShowTextAsync`; existing accepted-value, optional-save and cancellation contracts. |
| `Tools.WinForms.Controls` | `CreateChoiceSelector`, `CreateSearchBox`. |

GraphChart, BarChart and TelemetryChart already own a lazy logging behavior;
use their `LogOutputBehavior` property instead of attaching another behavior.
Tools stores no control, chart history, hook, logger session or screen host.
Designer-declared controls retain their normal constructors so native designer
serialization and layout remain supported.

```csharp
using Sphere10.Framework;

var processes = Tools.Windows.Processes.CreateDataSource();
var page = await processes.ReadRangeAsync(null, 50, 0, "ProcessId", SortDirection.Ascending);
if (page.Items.Length > 0) {
	using var process = await Tools.Windows.Processes.OpenAsync(page.Items[0].ProcessId);
	var metadata = await process.ReadMetadataAsync();
}
```

The example needs the Windows assembly. It starts no shared poller.

## Database Tools

| Facade | Project | Existing entrypoints and source |
|---|---|---|
| `Tools.Data` | Sphere10.Framework.Data | `ReadCsv`, `CreateDataTable`, `CreateDataTableForType`: [DataTool.cs](../src/Sphere10.Framework.Data/DataTool.cs) |
| `Tools.Json` | Sphere10.Framework.Data | `WriteToString`, `ReadFromString` and file/reader/writer overloads: [JsonTool.cs](../src/Sphere10.Framework.Data/JSON/JsonTool.cs) |
| `Tools.Xml` | Sphere10.Framework.Data | `WriteToString`, `ReadFromString` and XML serializer helpers: [XmlTool.cs](../src/Sphere10.Framework.Data/XML/XmlTool.cs) |
| `Tools.Sqlite` | Sphere10.Framework.Data.Sqlite | `Open`, `Create`, `Drop`, `ExistsByPath`, `ExistsByConnectionString` and connection strings: [SqliteTool.cs](../src/Sphere10.Framework.Data.Sqlite/SqliteTool.cs) |
| `Tools.MSSQL` | Sphere10.Framework.Data.MSSQL | `Open`, `Exists`, `TestConnectionString`, database creation/removal and connection strings: [MSSQLTool.cs](../src/Sphere10.Framework.Data.MSSQL/MSSQLTool.cs) |
| `Tools.MySQL` | Sphere10.Framework.Data.MySQL | `Open`, `Exists`, `TestConnectionString` and connection strings: [MySQLTool.cs](../src/Sphere10.Framework.Data.MySQL/MySQLTool.cs) |
| `Tools.Oracle` | Sphere10.Framework.Data.Oracle | `Open`, `TestConnectionString` and connection strings: [OracleTool.cs](../src/Sphere10.Framework.Data.Oracle/OracleTool.cs) |
| `Tools.PostgreSQL` | Sphere10.Framework.Data.PostgreSQL | `Open`, `Exists`, `TestConnectionString` and connection strings: [PostgreSQLTool.cs](../src/Sphere10.Framework.Data.PostgreSQL/PostgreSQLTool.cs) |
| `Tools.NHibernate` | Sphere10.Framework.Data.NHibernate | `NVarCharMaxValue` compatibility value: [NHibernate.cs](../src/Sphere10.Framework.Data.NHibernate/NHibernate.cs) |

JSON and XML are Data utilities, not Core facades, and use the names in this
table rather than `Serialize`/`Deserialize` shorthand. NHibernate sessions and
queries belong to that framework's session APIs; `Tools.NHibernate` does not
supply `CreateSession` or `CreateQuery`.

The Firebird provider exposes
[FirebirdDAC](../src/Sphere10.Framework.Data.Firebird/FirebirdDAC.cs), not a
`Tools.Firebird` facade.

## Application & Testing Tools

| Facade | Project | Existing entrypoints and source |
|---|---|---|
| `Tools.Config` | Sphere10.Framework.Application | `GetAppSetting` over `ConfigurationManager.AppSettings`: [ConfigTool.cs](../src/Sphere10.Framework.Application/Settings/ConfigTool.cs) |
| `Tools.Application` | Sphere10.Framework.Application | `ExecuteInitializers`/`ExecuteFinalizers` and async forms; `SerializeSettingsValues` / `PopulateSettingsValues` forward the existing values-only settings serializer: [ApplicationTool.cs](../src/Sphere10.Framework.Application/Lifecycle/ApplicationTool.cs) |
| `Tools.NUnit` | Sphere10.Framework.NUnit | `Convert2DArrayToString` and test diagnostics: [NUnitTool.cs](../src/Sphere10.Framework.NUnit/NUnitTool.cs) |
| `Tools.Drawing` | Sphere10.Framework.Drawing | Color conversion/gradients, image encoding and disabled images: [DrawingTool.cs](../src/Sphere10.Framework.Drawing/DrawingTool.cs) |

`Config` reads application configuration; the
[settings providers](../src/Sphere10.Framework.Application/Settings/README.md)
handle persisted settings. `Tools.Application` executes supplied, borrowed task
sequences. UI consumers use `IApplication.InitializeAsync`, `FinalizeAsync` and
`ExitAsync` to preserve application state, notifications, keyed task resolution
and owned cleanup. Initializers have sequential/parallelizable phases; finalizers
attempt every supplied task and aggregate failures. See the
[application lifecycle guide](../src/Sphere10.Framework.Application/README.md#application-initialization-and-shutdown).

## Usage Patterns

These complete fragments demonstrate existing Core signatures. They can be
compiled as a class library; they do not require network, database or UI setup.

```csharp
using System;
using System.Linq.Expressions;
using Sphere10.Framework;

namespace Examples;

public static class CoreToolExamples {
	public static int[] CreateSequence(int count) {
		Guard.ArgumentGTE(count, 0, nameof(count));
		return Tools.Collection.GenerateArray(count, index => index + 1);
	}

	public static DayOfWeek ParseDay(string text)
		=> Tools.Enums.ParseEnum<DayOfWeek>(text, ignoreValueCase: true);

	public static Func<int, int, int> CreateAdder()
		=> Tools.Expressions.CreateExpression<int, int, int>(Expression.Add);

	public static byte[] HashPayload(byte[] payload) {
		Guard.ArgumentNotNull(payload, nameof(payload));
		return Hashers.Hash(CHF.SHA2_256, payload);
	}
}
```

This stream helper leaves the caller's input open:

```csharp
using System.IO;
using Sphere10.Framework;

namespace Examples;

public static class BorrowedStreamExample {
	public static byte[] Read(Stream source) {
		Guard.ArgumentNotNull(source, nameof(source));
		return Tools.Streams.ReadByteArray(source, closeStream: false);
	}
}
```

## Using the Tools Namespace

1. Reference the project or package that declares the required facade.
2. Type `Tools.` and inspect its actual overloads. Types such as `CHF`, `Guard`
   and `IApplication` need their respective `Sphere10.Framework` namespace imports;
   those imports do not change the globally qualified `Tools` path.
3. Follow the [utility guidance](guidelines/patterns/framework-utilities.md) for
   framework reuse, exact facade names and ownership. Use domain contracts such as
   `Hashers` or `TypeActivator` when that is where the operation belongs.

## Adding New Tools (For Contributors)

Read [Code Styling](guidelines/code-styling.md) and the
[utility practice](guidelines/patterns/framework-utilities.md) before changing a utility.

- Inspect the existing facade and its consumers first; extend it when it already
  owns the responsibility.
- Give a new facade a discoverable domain name and put its static implementation
  in the global `Tools` namespace. Extend `Tools.Web` for the existing web family.
  A conventional source filename is `DomainTool.cs`; the public class name is the
  API consumers actually discover.
- Reuse framework validation, ownership and utility contracts. Preserve existing
  public names, including historical names such as `OperatorTool` and `iOSTool`.
- Keep examples aligned with current signatures, reference the actual owning
  project, and add or update this catalog when a facade changes.

## Related Documentation

- [Sphere10.Framework README](../src/Sphere10.Framework/README.md) — Core architecture and consumers.
- [Framework Utilities](guidelines/patterns/framework-utilities.md) — Selection, hashing and ownership rules.
- [Application ownership](guidelines/patterns/application-ownership.md) — Plugins, tasks, commands and runtime roots.
- [Code Styling](guidelines/code-styling.md) — Source conventions.
