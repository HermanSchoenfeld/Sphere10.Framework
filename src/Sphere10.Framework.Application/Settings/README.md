# Settings

`SettingsObject`, `UserSettings` and `GlobalSettings` provide persistent configuration through `ISettingsProvider`. These types are UI-independent. The [application module](../ModuleConfiguration.cs) registers cached directory providers and assigns the static settings facades during process initialization. Read settings after that initialization; individual screens should use the established providers.

## Declare settings

Use a public `SettingsObject` subclass with a parameterless constructor and serializable properties. Defaults can use property initializers or `DefaultValueAttribute`. `AppSettingAttribute` obtains a default from `ConfigurationManager.AppSettings` when the host supplies configuration in that format.

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

Call `Save()` on the provider-backed object returned by `Get<T>()`. A newly constructed settings object has no provider. The default provider creates defaults on first access but does not automatically save new settings. Its cache retains the same object, so provisional dialog edits should stay in a separate model until accepted. Use a fresh provider when verifying a subsequent-session disk read.

The default directories are `{UserDataDir}/{ProductName}` and `{SystemDataDir}/{ProductName}`. `DirectoryFileSettingsProvider` writes JSON `.setting` files and reads legacy XML files. Validate persisted values against the current UI's supported ranges and selections when restoring them.

For main-window placement, use the WinForms startup builder's `UseMainFormSettings()` integration. It captures accepted placement before application shutdown and saves during framework finalization while the window and providers remain alive. See the [WinForms guide](../../Sphere10.Framework.Windows.Forms/README.md) for other forms and navigation pane persistence. Save ordinary preferences on accepted Apply/Save actions instead of on every UI change.

Blazor interactive applications resolve their services per circuit, but the static `UserSettings` facade identifies the server process's operating-system user. For per-browser or per-account preferences, use an appropriately scoped application service and storage rather than assuming this static facade isolates browser users.

## Encryption

`EncryptedStringAttribute` marks string properties encrypted by providers with `EncryptSettings` enabled. The application module reads `AssemblyProductSecretAttribute` from the entry assembly during initialization and sets `EncryptedStringAttribute.ApplicationSharedSecret`:

```csharp
using Sphere10.Framework.Application;

[assembly: AssemblyProductSecret("your-product-secret")]
```

Configure the product's actual secret before encrypted settings are read or saved. The provider restores the in-memory string value after writing the encrypted representation.

## Provider composition

Implement `ISettingsProvider` directly or derive from `BaseSettingsProvider` for shared creation, encryption and provider attachment. `SettingsProviderDecorator` supports behavior extensions; `CachedSettingsProvider` adds identity caching by type and ID.

Override the default providers through `IServiceCollection` before the application module registers its defaults. The module uses `Local<ISettingsProvider>` and `Global<ISettingsProvider>` wrappers:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;

Sphere10Framework.Instance
	.Build()
	.ConfigureServices(services => {
		services.AddSingleton(new Local<ISettingsProvider>(new CachedSettingsProvider(new DirectoryFileSettingsProvider("data/user"))));
		services.AddSingleton(new Global<ISettingsProvider>(new CachedSettingsProvider(new DirectoryFileSettingsProvider("data/shared"))));
	})
	.UseModule<Sphere10.Framework.Application.ModuleConfiguration>()
	.Start();
```

The directories are examples; choose host-appropriate per-user and shared locations. Keep registration at process composition and let application initialization/finalization coordinate consuming tasks through the existing lifecycle. See the [application lifecycle guide](../README.md#application-initialization-and-shutdown).
