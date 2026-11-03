# Settings

`SettingsObject`, `UserSettings` and `GlobalSettings` provide standalone persistent configuration through `ISettingsProvider`. These types are UI-independent. The [application module](../ModuleConfiguration.cs) registers cached directory providers and assigns the static settings facades during process initialization. Read settings after that initialization. Screens declare their nested `SettingsObject` type with `[Settings(typeof(...))]`, equivalent to the builder's `UseSettings` declaration. Conflicting declarations throw; see [Screen Options](../../../docs/usage/screen-options.md).

## Declare settings

Use a public `SettingsObject` subclass with a parameterless constructor and serializable properties. Declare scalar defaults with `DefaultValueAttribute`, so activation and Reset use the same values. A property initializer alone is not a Reset template. Restore complex defaults in `RestoreDefaultValues()`, calling base first. `AppSettingAttribute` obtains a default from `ConfigurationManager.AppSettings` when the host supplies configuration in that format.

```csharp
using System.ComponentModel;
using Sphere10.Framework;
using Sphere10.Framework.Application;

public class DatabaseSettings : SettingsObject {
	public string Server { get; set; } = "localhost";

	[AppSetting("DatabaseName")]
	public string DatabaseName { get; set; }

	[EncryptedString]
	public string Password { get; set; }
}

public class WorkspacePreferences : SettingsObject {
	[DefaultValue(100)]
	public int PageSize { get; set; }

	public string SearchText { get; set; } = string.Empty;
}
```

Keep settings type names and optional IDs stable. Persist values and identifiers; controls, components, live screens and services remain runtime objects.

## Retrieve and persist settings

`UserSettings` stores per-user preferences; `GlobalSettings` stores values deliberately shared between system users. An optional ID distinguishes settings instances of one type:

```csharp
var workspace = UserSettings.Get<WorkspacePreferences>("Customers");
var archived = UserSettings.Get<WorkspacePreferences>("ArchivedCustomers");
var database = GlobalSettings.Get<DatabaseSettings>();

workspace.PageSize = 150;
workspace.Save();
```

Call `Save()` on the provider-backed object returned by `Get<T>()`. A newly constructed settings object has no provider. The default provider creates defaults on first access but does not automatically save new settings. Its cache retains the same object. Options edits that actual object and keeps a dialog-local backup for cancelling unapplied changes. Use a fresh provider when verifying a subsequent-session disk read.

The default directories are `{UserDataDir}/{ProductName}` and `{SystemDataDir}/{ProductName}`. `DirectoryFileSettingsProvider` writes JSON `.setting` files and reads legacy XML files. Validate persisted values against the current UI's supported ranges and selections when restoring them.

For main-window placement, use the WinForms startup builder's `UseMainFormSettings()` integration. It captures accepted placement before application shutdown and saves during framework finalization while the window and providers remain alive. See the [WinForms guide](../../Sphere10.Framework.Windows.Forms/README.md) for other forms and navigation pane persistence. Save ordinary preferences on accepted Apply/Save actions instead of on every UI change.

Blazor applications use the same `UserSettings` facade. Framework circuit initialization and inbound activities bind the circuit's `BlazorSettingsProvider` through the existing call context, keeping each browser or account separate without replacing the native process default. Await `UserSettings.HasAsync<T>()`, `GetAsync<T>()` and the returned object's `SaveAsync()` after interactive startup.

For registration-owned preferences, declare the screen's nested `SettingsObject`
class through `[Settings(typeof(...))]` and use `UseSettings` for optional supplied
registration values. Blocks
and plugins declare their own settings through the same builder method. Standard
dialogs edit the actual owner object, retain local backups for Cancel and save accepted edits through its provider;
see the [Application guide](../README.md#screen-block-and-plugin-preferences).
The application captures the circuit provider while its scope is alive, so initialization, finalization and disposal retain the same `UserSettings` binding.

`CachedSettingsProvider` retains the inner provider's existing `Changed` event
forwarding and timing. A failed save restores its previous cache entry, or removes
an unpersisted new entry, and rethrows. `Changed` is not a persistence acknowledgement;
accept values only after the save call succeeds.

## Encryption

`EncryptedStringAttribute` marks string properties encrypted by providers with `EncryptSettings` enabled. The application module reads `AssemblyProductSecretAttribute` from the entry assembly during initialization and sets `EncryptedStringAttribute.ApplicationSharedSecret`:

```csharp
using Sphere10.Framework.Application;

[assembly: AssemblyProductSecret("your-product-secret")]
```

Configure the product's actual secret before encrypted settings are read or saved.
Encrypted saves transform a temporary shallow storage copy of the root string
members, keeping the live settings instance in cleartext while IO is pending.
Its provider, ID, cache identity and subsequent edits remain attached to that
actual instance. This does not add a snapshot contract for nested mutable values.

## Provider composition

Use the existing directional provider hierarchy:

- `SettingsProviderBase` shares policies, provider-bound creation and notifications without choosing an execution direction.
- `SyncSettingsProviderBase` implements synchronous storage wrappers and schedules them for asynchronous calls. `DirectoryFileSettingsProvider` uses this base.
- `AsyncSettingsProviderBase` implements awaited storage wrappers and starts/joins those operations outside the calling synchronization context for synchronous calls. `BlazorSettingsProvider` uses this base.

`ISettingsProvider` declares both operation directions; it has no default async
execution adapters. Implement protected load/save primitives and public
contains/delete/clear operations in the chosen direction. Keep encryption and
provider identity in the inherited wrappers. `SettingsProviderDecorator`
forwards both APIs; `CachedSettingsProvider` adds identity caching by type and ID.
`NewSettingAsync(type, id, cancellationToken)` awaits automatic persistence when
enabled before returning or publishing a cached value. Browser UI uses awaited
operations after interactive startup. See the
[directional implementation guide](../../../docs/guidelines/patterns/sync-async.md#settings-providers).

Override the default providers through `IServiceCollection` before the application module registers its defaults. The module uses `Local<ISettingsProvider>` and `Global<ISettingsProvider>` wrappers:

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

The directories are examples; choose host-appropriate per-user and shared locations. Keep registration at process composition and let application initialization/finalization coordinate consuming tasks through the existing lifecycle. See the [application lifecycle guide](../README.md#application-initialization-and-shutdown).
