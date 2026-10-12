# 🛠️ Tools.* Namespace Reference

**Scope:** Source-audited utility catalog for the current Framework repository.
**Last source audit:** October 12, 2026.

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
`Tools.Threads.CreateAwaitTask` constructs an unstarted task whose body uses
the current `WaitSafe` helper; it is not an automatically started, awaited
replacement for an async operation. Follow the
[sync/async completion rules](guidelines/patterns/sync-async.md#adapter-completion-and-cancellation)
when the caller needs completion and failure propagation.

### Exception & Scope Tools

| Facade | Existing entrypoints | Implementation |
|---|---|---|
| `Tools.Exceptions` | `ExecuteCapturingException`, `ExecuteIgnoringException`, `TryOrDefault` | [ExceptionTool.cs](../src/Sphere10.Framework/Exceptions/ExceptionTool.cs) |
| `Tools.Scope` | `ExecuteOnDispose`, `ExecuteOnDisposeAsync`, file/directory cleanup scopes | [ScopeTool.cs](../src/Sphere10.Framework/Scopes/ScopeTool.cs) |

`Tools.Scope` supplies cleanup factories. Transaction begin/commit/rollback
belongs to the relevant [transactional scope](guidelines/patterns/resource-scopes.md),
not `Tools.Scope.CreateScope()`.

## Platform-Specific Tools

Reference the declaring project and meet its platform requirements.

| Facade | Project | Existing entrypoints and source |
|---|---|---|
| `Tools.Windows` | Sphere10.Framework.Windows | Thread-static `Registry`, `Services`, `Security`, `Processes`, `Win32` and `Shell` utility instances: [WindowsTool.cs](../src/Sphere10.Framework.Windows/WindowsTool.cs) |
| `Tools.WinForms` | Sphere10.Framework.Windows.Forms | Cursor helpers in [Tool.cs](../src/Sphere10.Framework.Windows.Forms/Tool.cs) and window capture/restore/automatic persistence in [WindowSettingsTool.cs](../src/Sphere10.Framework.Windows.Forms/WindowSettingsTool.cs) |
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
| `Tools.Application` | Sphere10.Framework.Application | `ExecuteInitializers`/`ExecuteFinalizers` and their async forms: [ApplicationTool.cs](../src/Sphere10.Framework.Application/Lifecycle/ApplicationTool.cs) |
| `Tools.UI` | Sphere10.Framework.Application | Definition validation, `GetCommandLayers`, `MergeApplicationMenus` and `MergeApplicationToolBarItems`: [UITool.cs](../src/Sphere10.Framework.Application/UI/UITool.cs) |
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

Read the [tools-namespace skill](../agents/skills/tools-namespace/SKILL.md) and
[Code Styling](guidelines/code-styling.md) before changing a utility.

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
