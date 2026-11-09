# Settings

Typed per-user and machine-wide settings objects with their storage providers, caching and encryption, plus helpers that read .NET configuration files.

The settings library stores typed preferences for the current user, or for every user of a machine, and restores them in later sessions. Use it for remembered choices and small configuration values in any Sphere10 application, with or without a user interface. Keep business data in the application's own storage.

Each set of preferences is a `SettingsObject` subclass. The static `UserSettings` and `GlobalSettings` facades load and create these objects through an `ISettingsProvider`, which the [Application module](../ModuleConfiguration.cs) installs when the framework starts. The settings types live in the `Sphere10.Framework.Application` package and namespace.

## Quick start

Declare a settings class and its defaults:

```csharp
using System.ComponentModel;
using Sphere10.Framework.Application;

public class WorkspacePreferences : SettingsObject {
	[DefaultValue(100)]
	public int PageSize { get; set; }

	[DefaultValue("")]
	public string SearchText { get; set; }
}
```

Once the framework has [started](#start-up-and-custom-providers), load the record, change it and save it:

```csharp
var customers = UserSettings.Get<WorkspacePreferences>("Customers");
customers.PageSize = 150;
customers.Save();
```

`Get<T>()` returns the stored record, or a new object with its defaults when nothing is stored yet. The optional ID keeps several records of one type apart, such as `"Customers"` and `"ArchivedCustomers"`.

## Main types

| Type | Role |
|---|---|
| `SettingsObject` | Base class of every settings record. It carries `ID` and `Provider` and offers `Save()`, `SaveAsync()`, `Load()`, `Delete()`, `Validate()` and the `OnChanged` event. |
| `UserSettings` | Static facade over the current user's provider, with `Get`, `Has` and `Clear` and their asynchronous forms. `UseProvider` binds another provider to the current asynchronous flow. |
| `GlobalSettings` | Static facade over the machine-wide provider, with synchronous `Get`, `Has` and `Clear`. |
| `ISettingsServices` | Injectable service that exposes the registered user and machine-wide providers as its `UserSettings` and `SystemSettings` properties. |
| `ISettingsProvider` | Storage contract that declares its create, exists, load, save, delete and clear operations in synchronous and asynchronous forms. |
| `DirectoryFileSettingsProvider` | Default storage, which writes one JSON file per record into a directory. |
| `CachedSettingsProvider` | Decorator that keeps one shared object for each settings type and ID. |
| `ProhibitedSettingsProvider` | Provider that rejects every operation with `NotSupportedException`, for hosts that must not store settings. |
| `SettingsObjectJsonSerializer` | JSON serializer for settings records. It skips runtime members and writes no CLR type names. |
| `Local<T>`, `Global<T>` | Service-collection wrappers that register the user and machine-wide providers. |

## Declare settings

```csharp
using System.ComponentModel;
using Sphere10.Framework;
using Sphere10.Framework.Application;

public class DatabaseSettings : SettingsObject {
	[DefaultValue("localhost")]
	public string Server { get; set; }

	[AppSetting("DatabaseName")]
	public string DatabaseName { get; set; }

	[EncryptedString]
	public string Password { get; set; }
}
```

- Derive a public class from `SettingsObject` with a public parameterless constructor, which the `new()` constraint of `Get<T>()` requires.
- Declare scalar defaults with `[DefaultValue]`. The `SettingsObject` constructor applies them, and `RestoreDefaultValues()`, which the Options dialog's **Reset** calls, applies them again. `RestoreDefaultValues()` does not reapply property initializers.
- Restore complex defaults, such as nested objects and collections, in an override of `RestoreDefaultValues()` that calls the base method first.
- `[AppSetting("key")]` is a `[DefaultValue]` that reads its value from `ConfigurationManager.AppSettings`, for hosts that still ship an `app.config` or `web.config` file.
- `[EncryptedString]` encrypts a string member in storage, as [Encryption](#encryption) describes.
- Override `Validate()` to reject values that the current version cannot use. The Options dialog checks it, together with data annotations, before it saves. The application adapters check screen, block and plugin settings when they restore them, and they keep the defaults when a stored record is invalid. Standalone code should call `Validate()` after `Get<T>()`, because a stored record can predate the current ranges and choices.
- Store plain values. The serializer skips members marked `[Transient]`, `[JsonIgnore]`, `[XmlIgnore]` or `[NonSerialized]`, and members that hold delegates, service providers, settings providers, applications, screens or screen hosts.
- Keep type names and IDs stable, because together they form the storage key.

## Load, save and observe

`UserSettings` holds per-user preferences, and `GlobalSettings` holds values meant for every user of the machine. Interactive code awaits the asynchronous forms:

```csharp
var archived = await UserSettings.GetAsync<WorkspacePreferences>("ArchivedCustomers", cancellationToken);
archived.SearchText = "2025";
await archived.SaveAsync(cancellationToken);

var database = GlobalSettings.Get<DatabaseSettings>();
```

- `Get<T>(id)` loads the record or creates one with defaults. The default provider does not store a new record until it is saved.
- Call `Save()` or await `SaveAsync()` on the object that `Get<T>()` returned, because that object carries its provider. An object created with `new` has no provider, so its `Save()` throws.
- `CachedSettingsProvider` returns the same object for a type and ID, so every consumer of a record shares one instance. For the same reason, `Has<T>(id)` also reports a record that this session created but has not saved.
- `OnChanged` fires after `Save()` or `SaveAsync()` succeeds, and a failed or cancelled save does not raise it. The provider's `Changed` event is not a save acknowledgement, because `SyncSettingsProviderBase` raises it before writing.
- `Delete()` removes the stored record through the object's own provider. A record that `CachedSettingsProvider` creates or loads carries the cache as its provider, so `Delete()` also evicts it and later `Has<T>()` and `Get<T>()` calls treat the record as missing.
- `UserSettings.Clear()` and `GlobalSettings.Clear()` remove every record of their provider.

Save ordinary preferences when the user accepts a change, such as on **Apply** or **OK**, rather than on every edit.

## Start-up and custom providers

During registration, the Application module adds a `CachedSettingsProvider` over a `DirectoryFileSettingsProvider` for each facade, unless the service collection already holds `Local<ISettingsProvider>` or `Global<ISettingsProvider>`. Its `OnInitialize` then assigns `UserSettings.Provider` and `GlobalSettings.Provider` and registers the token resolvers that the default directories use. The module's `Priority` is `int.MinValue`, so it initializes after the other modules. Read settings from process initializers and later code, because the facades throw `InvalidOperationException` until the module has initialized.

Register replacement providers through `AddServices`, whose callbacks run before any module registers its components:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;

Sphere10Framework.Instance
	.Build()
	.AddServices(services => {
		services.AddSingleton(new Local<ISettingsProvider>(new CachedSettingsProvider(new DirectoryFileSettingsProvider("data/user"))));
		services.AddSingleton(new Global<ISettingsProvider>(new CachedSettingsProvider(new DirectoryFileSettingsProvider("data/shared"))));
	})
	.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
	.Start();
```

The directories are examples, and a real host chooses its own per-user and shared locations. `DirectoryFileSettingsProvider` also accepts an `IFuture<string>`, so that the directory can be resolved on first access, as the module's defaults are. [Application initialization and shutdown](../README.md#application-initialization-and-shutdown) describes the rest of the start-up sequence.

## Storage location and format

| Facade | Registered as | Default directory | Token resolves to |
|---|---|---|---|
| `UserSettings` | `Local<ISettingsProvider>` | `{UserDataDir}/{ProductName}` | `Environment.SpecialFolder.LocalApplicationData` |
| `GlobalSettings` | `Global<ISettingsProvider>` | `{SystemDataDir}/{ProductName}` | `Environment.SpecialFolder.CommonApplicationData`, or `LocalApplicationData` on Unix |

On Unix the two default directories are therefore the same. A user record and a global record of one type then share a file, and `Clear()` on either facade removes the records of both.

`DirectoryFileSettingsProvider` writes each record as indented JSON through `SettingsObjectJsonSerializer`, and it still reads records stored as XML. It names the file `<type>.setting`, or `<type>_<id>.setting` when the record has an ID. `<type>` is the type name without its namespace, and a nested type keeps its declaring type, as in `UsersScreen+UsersPreferences`. Two settings types with the same name in different namespaces therefore share a file, unless the provider uses `DirectorySettingsProviderPolicy.UseFullyQualifiedTypeNameInFilename`. An empty file counts as missing and is deleted. A failed first save removes its partial file. A save creates a missing directory only when the policy includes `CreateDirectoryIfNotExists`, as `DirectorySettingsProviderPolicy.Default` does.

## Encryption

Mark sensitive strings with `[EncryptedString]`. A provider whose `EncryptSettings` is `true`, such as `DirectoryFileSettingsProvider`, encrypts these members with AES when it saves and decrypts them when it loads. `BlazorSettingsProvider` returns `false`, so browser storage holds them as plain text.

The secret comes from the static `EncryptedStringAttribute.ApplicationSharedSecret` field. The Application module sets it from the entry assembly's `AssemblyProductSecret` attribute when it initializes:

```csharp
using Sphere10.Framework.Application;

[assembly: AssemblyProductSecret("your-product-secret")]
```

- Without a secret, an encrypting provider throws `InvalidOperationException` when it loads or saves a type with encrypted members.
- The attribute's `Policy` and `Pepper` properties select the salt. By default the salt comes only from the pepper, so any machine with the same secret can decrypt the values.
- Only the settings object's own string members are encrypted, not the members of nested objects.
- A save encrypts a shallow copy, so the live object keeps clear text while the write is in progress. Nested objects are shared with that copy, so edits made to them during the write can reach storage.

## Application screens, blocks and plugins

Screens, blocks and plugins of an application own `SettingsObject` instances too. A screen declares its settings type with `[Settings(typeof(...))]`, and its base attribute `[UseSettings(typeof(...))]` still works. A registration can supply an instance through its builder's `UseSettings<T>()` or `UseSettings(instance)`, and blocks and plugins declare their settings through the same builder methods. Conflicting declarations throw when the screen definition is built.

The Options dialog edits the owner's actual object. It keeps a serialized backup, so that **Cancel** can restore unapplied edits, and its **Reset** calls `RestoreDefaultValues()`. [Screen, block and plugin preferences](../README.md#screen-block-and-plugin-preferences), [Screen Options](../../../docs/usage/screen-options.md) and [per-user form and control settings](../../../docs/guidelines/patterns/user-ui-settings.md) cover declarations, editors and screen lifecycles.

## WinForms main window

`UseMainFormSettings()` on the `BuildWinFormsApplication()` builder remembers the main window's size, monitor, position and maximized state. It saves once during framework shutdown, while the window and the settings providers are still available. [Remembering the main window](../../Sphere10.Framework.Windows.Forms/README.md#remembering-the-main-window) covers its options, the navigation pane width and other forms.

## Blazor

Blazor applications use the same `UserSettings` facade. Each circuit has its own `BlazorSettingsProvider`, which stores records as JSON in the browser's `localStorage`. Records belong to the signed-in user's subject, or to an anonymous browser profile when nobody is signed in. The adapter binds that provider with `UserSettings.UseProvider` while the circuit starts, handles browser events and closes, so one user's browser provider never replaces the process-wide default.

Browser storage becomes available once the circuit is interactive. From then on, await `UserSettings.HasAsync<T>()`, `UserSettings.GetAsync<T>()` and the record's `SaveAsync()`. When the signed-in user changes within a circuit, the provider throws until the application is reloaded.

## Implement a provider

Derive a storage provider from the base class for its natural direction, and the base supplies the other direction.

| Base | Members to implement | Other direction |
|---|---|---|
| `SyncSettingsProviderBase` | `LoadInternal`, `SaveInternal`, `ContainsSetting`, `DeleteSetting`, `ClearSettings` | Asynchronous members run the synchronous operation on a worker thread. |
| `AsyncSettingsProviderBase` | `LoadInternalAsync`, `SaveInternalAsync`, `ContainsSettingAsync`, `DeleteSettingAsync`, `ClearSettingsAsync` | Synchronous members start the asynchronous operation on a worker thread and wait for it. |

Both bases derive from `SettingsProviderBase`. Together they attach the provider to created and loaded objects, apply encryption and raise `Changed` on save, so the storage members only read and write. Override the `LoadInternal(SettingsObject defaults)` overload, or its asynchronous counterpart, when the format can fill an existing object so that values missing from storage keep their defaults. `DirectoryFileSettingsProvider` does this.

This provider keeps each record as a JSON string in memory, which suits tests and hosts that must not write files:

```csharp
using System;
using System.Collections.Concurrent;
using System.IO;
using Sphere10.Framework.Application;

public class MemorySettingsProvider : SyncSettingsProviderBase {
	private readonly ConcurrentDictionary<string, string> _records = new();

	public override bool ContainsSetting(Type settingsObjectType, object id = null)
		=> _records.ContainsKey(KeyOf(settingsObjectType, id));

	public override void DeleteSetting(SettingsObject settings) {
		_records.TryRemove(KeyOf(settings.GetType(), settings.ID), out _);
		FireChanged();
	}

	public override void ClearSettings() {
		_records.Clear();
		FireChanged();
	}

	protected override SettingsObject LoadInternal(Type settingsObjectType, object id = null) {
		using var reader = new StringReader(_records[KeyOf(settingsObjectType, id)]);
		return (SettingsObject)new SettingsObjectJsonSerializer().Deserialize(reader, settingsObjectType);
	}

	protected override void SaveInternal(SettingsObject settings) {
		using var writer = new StringWriter();
		new SettingsObjectJsonSerializer().Serialize(writer, settings);
		_records[KeyOf(settings.GetType(), settings.ID)] = writer.ToString();
	}

	private static string KeyOf(Type settingsObjectType, object id)
		=> id == null ? settingsObjectType.FullName : $"{settingsObjectType.FullName}_{id}";
}
```

`UserSettings.Get<T>()` calls `ContainsSetting` first, so `LoadInternal` only runs for a stored record. `SaveInternal` receives a copy whose `[EncryptedString]` members the base has already encrypted. Delete and clear raise `Changed` themselves, as `DirectoryFileSettingsProvider` does. Register the provider as [Start-up and custom providers](#start-up-and-custom-providers) shows, for example as `new Local<ISettingsProvider>(new CachedSettingsProvider(new MemorySettingsProvider()))`.

Wrap an existing provider by deriving from `SettingsProviderDecorator`, which forwards every member in both directions and exposes the wrapped provider as `Inner`. `CachedSettingsProvider` is the standard decorator, and it attaches itself as the provider of the records it creates or loads. When the inner save fails, it restores its previous cache entry and rethrows. Its `NewSettingAsync` caches a new record only after any automatic save has completed. [Settings providers](../../../docs/guidelines/patterns/sync-async.md#settings-providers) explains the execution rules behind both bases.

## Configuration files

`Tools.Config.GetAppSetting` reads an `appSettings` value with a fallback default. `GenericSectionHandler` is an `IConfigurationSectionHandler` that deserializes a custom configuration section with `XmlSerializer`. Both read .NET configuration files through `System.Configuration.ConfigurationManager`.

This `App.config` declares a custom section beside its `appSettings`:

```xml
<configuration>
  <configSections>
    <section name="reporting" type="Sphere10.Framework.Application.GenericSectionHandler, Sphere10.Framework.Application" />
  </configSections>
  <appSettings>
    <add key="ReportTitle" value="Monthly report" />
    <add key="PageSize" value="50" />
  </appSettings>
  <reporting type="MyApp.ReportingConfiguration, MyApp">
    <OutputFolder>reports</OutputFolder>
    <MaxRows>500</MaxRows>
  </reporting>
</configuration>
```

The application reads both parts:

```csharp
using System.Xml.Serialization;
using Sphere10.Framework.Application;

var title = Tools.Config.GetAppSetting("ReportTitle", "Untitled");
var pageSize = Tools.Config.GetAppSetting<int>("PageSize", 100);
var reporting = GenericSectionHandler.GetConfig<MyApp.ReportingConfiguration>("reporting");

namespace MyApp {
	[XmlRoot("reporting")]
	public class ReportingConfiguration {
		public string OutputFolder { get; set; }
		public int MaxRows { get; set; }
	}
}
```

`GetAppSetting<T>` parses the value with `Tools.Parser` and returns the default when the key is missing. The section's `type` attribute names the class to deserialize as `<type>, <assembly>`, where `MyApp` stands for the application's assembly. `[XmlRoot]` matches the class to the section's element name. `GetConfig<T>` throws `SoftwareException` when the configuration file has no such section.

## Tests

`SettingsProviderDirectionTests`, `SettingsProviderAsyncTests`, `SettingsProviderPublicationTests` and `SettingsObjectJsonSerializerTests` in the [Application test suite](../../../tests/Sphere10.Framework.Application.Tests/README.md) cover the providers and the serializer. Run them from the repository root:

```powershell
dotnet test tests/Sphere10.Framework.Application.Tests/Sphere10.Framework.Application.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~SettingsProvider|FullyQualifiedName~SettingsObjectJsonSerializerTests"
```

A persistence test should read the next session through a new provider instance, because a `CachedSettingsProvider` would return its cached object without reading the file.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AppSettingAttribute`](AppSettingAttribute.cs) | class | `DefaultValueAttribute` whose value is read from `ConfigurationManager.AppSettings` under a key |
| [`AsyncSettingsProviderBase`](AsyncSettingsProviderBase.cs) | abstract class | Provider base for asynchronous storage, whose synchronous members wait for the asynchronous operation on a worker |
| [`BaseSettingsServices`](BaseSettingsServices.cs) | abstract class | `ISettingsServices` base with abstract user and system providers and the `ConfigurationChanged` notification |
| [`CachedSettingsProvider`](CachedSettingsProvider.cs) | class | Decorator that keeps one shared settings object per type and ID and attaches itself to the records it creates or loads |
| [`DirectoryFileSettingsProvider`](DirectoryFileSettingsProvider.cs) | class | Synchronous provider that stores each record as an indented JSON `.setting` file in a directory and still reads XML records |
| [`DirectorySettingsProviderPolicy`](DirectorySettingsProviderPolicy.cs) | enum | Directory provider options: `CreateDirectoryIfNotExists`, `UseFullyQualifiedTypeNameInFilename` and `Default` |
| [`GenericSectionHandler`](GenericSectionHandler.cs) | class | `IConfigurationSectionHandler` that deserializes a configuration section with `XmlSerializer`, with static `GetConfig<T>` |
| [`Global<T>`](Global.cs) | class | Service-collection wrapper that marks its `Item` as the machine-wide instance, such as the global `ISettingsProvider` |
| [`GlobalSettings`](GlobalSettings.cs) | static class | Facade over the machine-wide provider with `Get`, `Has` and `Clear` |
| [`ISettingsProvider`](ISettingsProvider.cs) | interface | Storage contract for creating, checking, loading, saving, deleting and clearing settings in synchronous and asynchronous forms |
| [`ISettingsServices`](ISettingsServices.cs) | interface | Injectable access to the `UserSettings` and `SystemSettings` providers with a `ConfigurationChanged` event |
| [`Local<T>`](Local.cs) | class | Service-collection wrapper that marks its `Item` as the per-user instance, such as the user `ISettingsProvider` |
| [`ProhibitedSettingsProvider`](ProhibitedSettingsProvider.cs) | class | Provider that rejects every operation with `NotSupportedException`, for hosts that must not store settings |
| [`SettingsObject`](SettingsObject.cs) | abstract class | Base of every settings record, with `ID`, `Provider`, `Load`, `Save`, `SaveAsync`, `Delete`, `Validate` and `OnChanged` |
| [`SettingsObjectJsonSerializer`](SettingsObjectJsonSerializer.cs) | class | JSON serializer for settings records that skips transient and runtime members and writes no CLR type names |
| [`SettingsProviderBase`](SettingsProviderBase.cs) | abstract class | Shared provider identity, provider attachment, encryption and `Changed` notification for both storage directions |
| [`SettingsProviderDecorator`](SettingsProviderDecorator.cs) | class | Forwards every provider member and notification to an `Inner` provider in both directions |
| [`StandardSettingsServices`](StandardSettingsServices.cs) | class | `ISettingsServices` built from the registered `Global<ISettingsProvider>` and `Local<ISettingsProvider>` |
| [`SyncSettingsProviderBase`](SyncSettingsProviderBase.cs) | abstract class | Provider base for synchronous storage, whose asynchronous members run the synchronous operation on a worker |
| [`Tools.Config`](ConfigTool.cs) | tools | `GetAppSetting` reads an `appSettings` value as a string or a parsed struct with a fallback default |
| [`UserSettings`](UserSettings.cs) | static class | Facade over the current user's provider with synchronous and asynchronous `Get`, `Has` and `Clear`, and `UseProvider` for an asynchronous flow |
