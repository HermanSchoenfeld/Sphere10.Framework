<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💫 Sphere10.Framework.Application

**Complete application framework** providing dependency injection, modular architecture, settings management, lifecycle hooks, and product information for building production-ready Sphere10 Framework-based applications.

Sphere10.Framework.Application enables **rapid application development** by providing a complete infrastructure for **service composition, settings persistence, initialization/finalization pipelines, and modular configuration**—all integrated with Microsoft.Extensions.DependencyInjection.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Application
```

## Shared UI framework

`Sphere10.Framework.Application.UI` contains the common ApplicationBlock model used by the WinForms and Blazor adapters. The Application package targets `net10.0` and has no ASP.NET Core, Windows desktop or other UI framework dependency. The `UI` folder also contains the existing help, website launcher and user-interface services, which retain their `Sphere10.Framework.Application` namespace.

| Shared responsibility | Types |
|---|---|
| Plugin definitions and lifecycle | `IApplicationPlugin`, `ApplicationPluginBase`, `ApplicationPlugin`, `ApplicationPluginDecorator`, `ApplicationPluginBuilder` |
| Running application and commands | `IApplication`, `ApplicationBase`, `ApplicationDecorator`, `IApplicationCommandProvider` |
| Menu and toolbar merging | `Tools.UI.MergeMenus`, `Tools.UI.MergeMenuItems`, `IApplicationMenuSeparator` |
| Wizard orchestration and configuration | `IWizard<TStep>`, `IWizard<TModel, TStep>`, `WizardBase`, `Wizard`, `WizardDecorator`, `WizardBuilderBase` |
| Block, menu and item contracts | `IApplicationBlock`, `IApplicationMenu`, `IApplicationMenuItem`, `IScreenMenuItem` |
| Mutable metadata and notification handlers | `ApplicationBlock`, `ApplicationMenu<TItem>`, `ApplicationMenuItem` |
| Builder state and validation | `ApplicationBlockBuilderBase<TMenu, TBlock>`, `ApplicationMenuBuilderBase<TItem, TMenu>`, `ApplicationMenuItemBuilderBase` |
| Snapshot traversal, stable IDs and ordering | `ApplicationBlockSnapshotBase<TBlock, TMenu, TItem>`, `ApplicationBlockCatalog<TBlock>` |
| Activation policy and lifecycle | `ScreenActivationPolicyRegistry`, `ScreenActivationMode`, `ScreenKind`, `ApplicationScreenDefinition`, `IApplicationScreen` |
| Awaitable menu actions | `ApplicationAction` |

Platform-specific extensions use explicit names: `IBlazorApplicationBlock : IApplicationBlock` and `IWinFormsApplicationBlock : IApplicationBlock`, implemented by `BlazorApplicationBlock` and `WinFormsApplicationBlock`. Their menus, builders and screen hosts follow the same convention. Import the shared UI namespace normally alongside the platform namespace; the APIs do not require namespace aliases. Both platforms use the shared `ScreenActivationMode` directly.

The shared `ApplicationBlock` stores `IApplicationMenu` directly, allowing different menu implementations in the same block. The shared models can be constructed without either UI adapter:

```csharp
using Sphere10.Framework.Application.UI;

var block = new ApplicationBlock {
	Id = "administration",
	Name = "Administration",
	Position = 10
};
var menu = new ApplicationMenu<ApplicationMenuItem> { Id = "tools", Text = "Tools" };
menu.AddItem(new ApplicationMenuItem { Id = "refresh", Title = "Refresh" });
block.AddMenu(menu);
```

IDs default to their display names when omitted. The shared builder bases copy menu, item and parameter collections when building; Blazor uses this to isolate registration snapshots. WinForms builders retain and update the same mutable native product across builds to preserve desktop ownership semantics. Snapshot factories copy platform metadata while the shared engine validates names, screen types, duplicate IDs and the reserved `__default` item ID. The catalog snapshots registrations before ordering blocks by position and indexing their IDs. The `Menus`, `Items` and catalog `Blocks` properties expose arrays. Shared models and catalogs return a fresh membership snapshot on each read, so replacing or reordering slots in a returned array cannot alter stored membership or catalog lookup. Previously returned arrays do not track later add/remove calls; use the model methods to change its membership and read the property again. These array copies preserve element identity rather than deep-cloning arbitrary objects. Enumerable method parameters remain available for registration and traversal.

Activation-policy declaration batches are atomic: incompatible policies or invalid screens reject the entire batch. Explicit declarations are distinguished from policies inferred when a host creates an instance. `IApplicationScreen` supplies UI-independent help metadata, cancellation-aware activation/deactivation hooks and a navigation guard. `ApplicationAction.ExecuteAsync` receives the executing service provider and cancellation token; callbacks should resolve scoped services there. Registration-time callbacks must not capture scoped services or mutable per-user state.

The [WinForms adapter](../Sphere10.Framework.Windows.Forms/README.md) retains controls, images, UI-thread dispatch and desktop screen ownership. The [Blazor adapter](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md) retains component validation, rendering, circuit scopes and browser interaction. Each supplies factories and platform validation to the shared engines. An ApplicationBlock describes navigation metadata; `ModuleConfiguration` continues to handle service registration and framework startup.

The [Application test suite](../../tests/Sphere10.Framework.Application.Tests/README.md) exercises the shared behavior using ordinary .NET classes and checks the framework assembly dependency graph. [NuGet validation](../../scripts/package-consumers/README.md) also builds a consumer referencing only the Application package and rejects UI dependencies in its resolved packages and framework references.

## Application plugins

The shared hierarchy is `IApplication` → `Plugins` → `IApplicationPlugin` → `Blocks`. A plugin is a named startup definition, while the running application projects its platform host's current navigation and screen selection. `ActivePlugin` resolves ownership by `ActiveBlock.Id`, allowing platform catalogs to create block snapshots without replacing the original plugin object. Browsing another block changes this navigation selection without requiring a screen switch.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;

var plugin = new ApplicationPluginBuilder()
	.WithName("Administration")
	.AddBlock(new ApplicationBlock { Id = "users", Name = "Users" })
	.ConfigureServices(services => services.AddSingleton<AdministrationSettings>())
	.Build();

var services = new ServiceCollection();
plugin.Load(services); // Startup owner calls this before building its service provider.
// The same startup owner calls plugin.Unload() during its shutdown sequence.

public class AdministrationSettings {
	public string Title { get; set; } = "Administration";
}
```

`ApplicationPluginBase.Load(IServiceCollection)` applies service configuration, invokes `OnLoaded`, then raises `Loaded`. `Unload` invokes `OnUnloaded`, then raises `Unloaded`. Registration failures propagate without announcing a successful load. The parameterless `Load()` is a notification-only compatibility entry point. These methods do not construct a service provider or enforce a once-only load policy: the startup owner controls calls, and runtime application adapters never load or unload plugins merely because a form or circuit opens or closes. Resolve scoped services when commands execute, rather than capturing them in startup definitions.

`ApplicationPlugin` supports constructor configuration and init-only `Name`/`Blocks`; its arrays copy membership and retain block identity. Platform adapters can snapshot their own block metadata before forwarding it to shared storage. `ApplicationPluginBuilderBase<TBlock, TPlugin>` shares name validation, accumulated blocks, service callback composition and independent build inputs with typed platform builders. Empty applications and service-only plugins with no blocks are valid.

`Tools.UI.ValidatePlugins` validates plugin names and block ownership with ordinal comparison: names must be nonblank and unique, and each nonblank block ID belongs to exactly one plugin. It returns an owned membership array preserving the supplied plugin instances. `ValidatePluginBlocks` provides the corresponding single-plugin block validation. `GetImplicitPluginName` returns the first free name from `Application`, `Application 2`, and so on, allowing adapters to wrap legacy standalone block registrations without creating a second plugin pipeline. Platform interfaces extend `IApplicationPlugin` with typed block arrays; existing plugins remain the same startup definitions when accessed through shared or platform contracts.

## Running application and command scopes

`IApplication` represents the running UI rather than service-container startup. It exposes `Plugins`, `ActivePlugin`, `Blocks`, `ActiveBlock`, `ActiveScreen`, `HasUnsavedChanges` and a `Changed` event. `IBlazorApplication` and `IWinFormsApplication` specialize that contract and project their existing screen hosts. Resolving the shared interface returns the same platform application instance; disposing that adapter releases subscriptions without taking ownership of the host, screens or plugin registrations.

`ApplicationBase` shares command storage, notifications, default plugin-to-block flattening and active-plugin lookup by the selected block ID. Platform adapters may override `Blocks` with their live registration projection. `IApplicationCommandProvider` supplies array-valued `Menus` and `ToolBarItems`. `Tools.UI.MergeMenus` and `MergeMenuItems` combine contributions from broader to narrower scopes: application, the active screen's owning block, then active screen. With no active screen, the selected navigation block supplies block commands. Matching IDs retain their original position and take the narrower command; matching menus combine their items. Separators are normalized and the `help` menu remains last. Merging creates membership snapshots and preserves command identity, so callbacks continue to execute on the original definitions.

`ScreenMode.SingleView` and `ScreenMode.MultiView` live in this shared namespace alongside `ScreenActivationMode`. Platform hosts retain responsibility for rendering, instance ownership and navigation guards.

## Default, permanent and empty screens

Ordinary Blazor and WinForms screens default to `ScreenActivationMode.MultiInstance`: each activation creates a new screen. Choose `SingleInstance` explicitly for a retained screen, such as Settings; permanent and empty-screen test scenarios keep their declared lifetimes. Enum numeric values are unchanged.

Both UI adapters use the same screen metadata and selection rules. Mark a screen menu item with `IsDefault`, or use a block's `DefaultScreen` (the platform builders expose `AsDefault()` and `WithDefaultScreen<TScreen>(...)`). Startup chooses the first marked screen in plugin registration order, then each plugin's block order, then menu declaration order. `Position` remains a navigation display setting and does not override plugin ownership order. Explicit navigation to a screen takes precedence over startup selection.

| Configuration | Behavior |
|---|---|
| `ScreenActivationMode.MultiInstance` (default) | Each activation opens an independent screen instance. |
| `ScreenActivationMode.SingleInstance` | Reopening selects the retained instance. Ordinary tabs can be closed. |
| `ScreenActivationMode.PermanentSingleton` | Automatically opens one instance, remains open for the block's lifetime and cannot be closed through tab commands. Other screens can still be selected. |
| `ScreenKind.Empty` + `SingleInstance` | A tabless placeholder appears only when no normal screen is open; its instance is retained while hidden. |
| `ScreenKind.Empty` + `MultiInstance` | The placeholder is destroyed when a normal screen opens and recreated when the workspace becomes empty again. |

`Empty` describes the screen's role, separately from its activation mode. It never has a tab and cannot be combined with `PermanentSingleton`. A permanently open normal screen therefore keeps an empty placeholder hidden. Administrative block removal ends its screens' lifetime, including permanent screens, after applicable navigation guards permit removal; normal application shutdown also disposes them. A permanent screen does not make application shutdown impossible.

`Tools.UI.OrderApplicationBlocks` resolves plugin membership against live block registrations. `GetScreenDefinitions` produces immutable `ApplicationScreenDefinition` metadata from block defaults and menu entries, validating lifetime and role consistency for each screen type. `GetDefaultScreen` and `GetEmptyScreen` select from those definitions, so both platforms share the ordering algorithm. The first explicitly marked empty candidate is preferred; otherwise the first empty candidate is used. A default block screen inherits a matching menu entry's lifetime when no override is specified. Conflicting explicit policies fail before registration.

For example, a UI-independent placeholder declaration is:

```csharp
var block = new ApplicationBlock {
	Id = "welcome",
	Name = "Welcome",
	DefaultScreen = typeof(WelcomeScreen),
	DefaultScreenActivationMode = ScreenActivationMode.SingleInstance,
	DefaultScreenKind = ScreenKind.Empty
};
```

`WelcomeScreen` supplies the platform screen implementation. See the [Blazor guide](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md) and [WinForms guide](../Sphere10.Framework.Windows.Forms/README.md) for executable builder configuration and demo profiles. Rendering, retained component/control ownership and UI-thread dispatch stay in those adapters.

## Shared wizard workflow

`Wizard<TModel, TStep>` owns ordered steps, the current position, dynamic branch updates, cancellation policy and completion. A step can be a component `Type`, a native screen, or an ordinary domain object. The shared assembly has no knowledge of Razor components or Windows controls. Hosts validate and present the current step before asking the workflow to navigate or finish.

```csharp
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Application.UI;

var wizard = new Wizard<string, string>(
	"Review request", "draft", new[] { "Details", "Review" },
	finish: model => Task.FromResult<Result<bool>>(true));

await wizard.MoveNextAsync();
var result = await wizard.FinishAsync();
// wizard.State == WizardState.Finished; another FinishAsync does not repeat the callback.
```

`MoveNextAsync` and `MovePreviousAsync` provide awaited navigation. `UpdateStepsAsync` and `RemoveStepAsync` let platform adapters update their displayed controls before returning. `WizardStepUpdateType` supports injecting steps, replacing the remaining branch, replacing all steps and removing subsequent steps. The workflow returns defensive `Steps` arrays while retaining step and model identities. Finish and Cancel share a [Stateless](https://www.nuget.org/packages/Stateless/5.20.1) lifecycle: concurrent terminal operations are rejected while busy; a declined or failed callback leaves a retryable workflow; successful completion runs once. Callbacks must not recursively finish or cancel the same wizard.

`WizardBuilderBase<TModel, TStep, TWizard>` centralizes configuration, callbacks and build validation. Platform builders supply their step validation and factories. Use `WinFormsWizardBuilder<TModel>` for native dialogs, or `BlazorWizardBuilder<TModel>` from the chosen Blazor wizard generation. Their adapters keep rendering, screen initialization and component validation in the UI libraries while consuming the same workflow. `WizardDecorator` forwards the common contract for behavior extensions.

## Awaitable UI services

`IUserInterfaceServices.ShowNagScreen`, `ShowSendCommentDialog`, `ShowSubmitBugReportDialog`, `ShowRequestFeatureDialog`, and `ShowAboutBox` return `Task`. Await them to preserve sequencing and observe errors. `IProductLicenseEnforcer.EnforceLicense(bool)` also returns `Task`, and completes after any licensing dialog closes. Implementations with no UI work return `Task.CompletedTask`.

## ⚡ Quick Start

This minimal Windows Forms host uses the current startup builder. Reference `Sphere10.Framework.Windows.Forms` from a Windows desktop project with `<UseWindowsForms>true</UseWindowsForms>`; the shared Application package itself remains UI-independent. See [AutoMouse](https://github.com/HermanSchoenfeld/AutoMouse) for a complete application.

```csharp
using System;
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;
using FormsApplication = System.Windows.Forms.Application;

static class Program {
	[STAThread]
	static void Main(string[] args) {
		using var singleInstance = new SingleApplicationInstanceScope();
		FormsApplication.SetHighDpiMode(HighDpiMode.PerMonitorV2);
		FormsApplication.EnableVisualStyles();
		FormsApplication.SetCompatibleTextRenderingDefault(false);

		Sphere10Framework.Instance
			.BuildWinFormsApplication()
			.UseMainForm<BlockMainForm>()
			.UseModule<Sphere10.Framework.Application.ModuleConfiguration>()
			.UseModule<Sphere10.Framework.Windows.Forms.ModuleConfiguration>()
			.StartWinFormsApplication();
	}
}
```

## 🏗️ Core Architecture

### The Framework Singleton

`Sphere10Framework.Instance` is the central orchestrator that:
- Discovers and registers all `ModuleConfiguration` classes in your assemblies
- Builds the DI container with all registered services
- Executes initialization and finalization pipelines
- Provides access to the `IServiceProvider`

```csharp
// Access services anywhere in your application
var settings = Sphere10Framework.Instance.ServiceProvider.GetService<IProductInformationProvider>();
var controller = Sphere10Framework.Instance.ServiceProvider.GetService<IAutoMouseController>();
```

### Framework Lifecycle

```
Sphere10Framework.Instance.StartFramework()
    ↓
    1. Discover ModuleConfiguration classes (via reflection)
    2. Call RegisterComponents() on each module (priority order)
    3. Build ServiceProvider
    4. Call OnInitialize() on each module
    5. Execute IApplicationInitializer instances
    ↓
Application runs...
    ↓
Sphere10Framework.Instance.EndFramework()
    ↓
    1. Execute IApplicationFinalizer instances  
    2. Call OnFinalize() on each module
    3. Dispose ServiceProvider (if owned)
```

## 🧩 ModuleConfiguration Pattern

The `ModuleConfiguration` pattern is the **core architectural pattern** for organizing your application into cohesive, self-contained modules. Each module registers its services, initializers, and configuration.

### Creating a Module

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp;

public class ModuleConfiguration : ModuleConfigurationBase {
    
    public override void RegisterComponents(IServiceCollection services) {
        // Register application services
        services.AddTransient<ISoundMaker, DefaultSoundMaker>();
        services.AddTransient<IMouseHook, WindowsMouseHook>();
        services.AddTransient<IKeyboardHook, WindowsKeyboardHook>();
        services.AddTransient<IAutoMouseController, WindowsAutoMouseController>();
        
        // Register help and UI services
        services.AddTransient<IHelpServices, CHMHelpProvider>();
        services.AddTransient<IAutoRunServices, StartupFolderAutoRunServicesProvider>();
        
        // Register initializers (run at startup)
        services.AddInitializer<FirstTimeSetWindowsStartupTask>();
        services.AddInitializer<DatabaseMigrationInitializer>();
        
        // Register control state providers for UI binding
        services.AddControlStateEventProvider<ClickRadiusSelector, ClickRadiusSelector.StateEventProvider>();
    }
    
    public override void OnInitialize(IServiceProvider serviceProvider) {
        base.OnInitialize(serviceProvider);
        // Custom initialization logic
    }
    
    public override void OnFinalize(IServiceProvider serviceProvider) {
        base.OnFinalize(serviceProvider);
        // Custom cleanup logic
    }
}
```

### Module Priority

Modules are executed in priority order. Set `Priority` to control execution order:

```csharp
public class ModuleConfiguration : ModuleConfigurationBase {
    public override int Priority => int.MinValue;  // Execute last (lowest priority)
    // or
    public override int Priority => int.MaxValue;  // Execute first (highest priority)
}
```

## ⚙️ Settings Management

### Defining Settings Classes

Settings inherit from `SettingsObject` and use `[DefaultValue]` attributes:

```csharp
using System.ComponentModel;
using Sphere10.Framework.Application;

public class AutoMouseSettings : SettingsObject {
    
    [DefaultValue(true)]
    public bool AutoStartProgram { get; set; }
    
    [DefaultValue(Key.LControlKey)]
    public Key ScreenMouseActivationKey { get; set; }
    
    [DefaultValue(1000)]
    public int ScreenMouseTimeoutMS { get; set; }
    
    // Computed property wrapping the stored value
    public TimeSpan ScreenMouseTimeout {
        get => TimeSpan.FromMilliseconds(ScreenMouseTimeoutMS);
        set => ScreenMouseTimeoutMS = (int)value.TotalMilliseconds;
    }
    
    [DefaultValue(true)]
    public bool MakeClickSound { get; set; }
    
    [DefaultValue(50)]
    public int ClickFreeZoneRadius { get; set; }
}
```

### Using Settings

```csharp
// Get settings (creates with defaults if not exists)
var settings = UserSettings.Get<AutoMouseSettings>();

// Modify and save
settings.AutoStartProgram = false;
settings.ScreenMouseTimeoutMS = 2000;
settings.Save();

// Reset to defaults
settings.RestoreDefaultValues();
settings.Save();

// Check if settings exist
bool hasSettings = UserSettings.Has<AutoMouseSettings>();
```

### Settings Scopes

- **`UserSettings`**: Per-user settings stored in `{UserDataDir}/{ProductName}/`
- **`GlobalSettings`**: System-wide settings stored in `{SystemDataDir}/{ProductName}/`

```csharp
// User-specific settings
var userPrefs = UserSettings.Get<UserPreferences>();

// System-wide settings (shared by all users)
var globalConfig = GlobalSettings.Get<SystemConfiguration>();
```

### Settings in UI Controls

Use the `[UseSettings]` attribute on controls to automatically bind settings:

```csharp
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

[UseSettings(typeof(AutoMouseSettings))]
public partial class AutoMouseSettingsControl : ApplicationControl {
    
    public AutoMouseSettings Settings => UserSettings.Get<AutoMouseSettings>();
    
    protected override void CopyModelToUI() {
        // Copy settings to UI controls
        _autoStartCheckBox.Checked = Settings.AutoStartProgram;
        _timeoutNumeric.Value = Settings.ScreenMouseTimeoutMS;
        _soundCheckBox.Checked = Settings.MakeClickSound;
    }
    
    protected override void CopyUIToModel() {
        // Copy UI values back to settings
        Settings.AutoStartProgram = _autoStartCheckBox.Checked;
        Settings.ScreenMouseTimeoutMS = (int)_timeoutNumeric.Value;
        Settings.MakeClickSound = _soundCheckBox.Checked;
        Settings.Save();
    }
}
```

## 🚀 Application Initializers

Initializers run automatically at framework startup. They're perfect for one-time setup tasks.

### Creating an Initializer

```csharp
using Sphere10.Framework.Application;

public class FirstTimeSetWindowsStartupTask : ApplicationInitializerBase {
    
    public FirstTimeSetWindowsStartupTask(
        IProductInformationProvider productInformationProvider,
        IProductUsageServices productUsageServices,
        IAutoRunServices autoRunServices) {
        ProductInformationProvider = productInformationProvider;
        ProductUsageServices = productUsageServices;
        AutoRunServices = autoRunServices;
    }
    
    public IProductInformationProvider ProductInformationProvider { get; }
    public IProductUsageServices ProductUsageServices { get; }
    public IAutoRunServices AutoRunServices { get; }
    
    public override void Initialize() {
        // Only on first launch
        if (ProductUsageServices.ProductUsageInformation.NumberOfUsesByUser == 1) {
            // Set the app to autorun on Windows startup
            AutoRunServices.SetAutoRun(
                AutoRunType.CurrentUser,
                ProductInformationProvider.ProductInformation.ProductName,
                Application.ExecutablePath,
                null);
        }
    }
}
```

### Registering Initializers

```csharp
public override void RegisterComponents(IServiceCollection services) {
    services.AddInitializer<FirstTimeSetWindowsStartupTask>();
    services.AddInitializer<DatabaseMigrationInitializer>();
    services.AddInitializer<CacheWarmupInitializer>();
}
```

### Initializer Options

```csharp
public class ParallelInitializer : ApplicationInitializerBase {
    public override int Priority => 50;  // Lower = runs earlier
    public override bool Parallelizable => true;  // Can run in parallel with others
    
    public override void Initialize() {
        // Initialization logic
    }
}
```

## 📋 Product Information

Product information is extracted from assembly attributes:

### Assembly Attributes

```csharp
// Properties/AssemblyInfo.cs
using Sphere10.Framework.Application;

[assembly: AssemblyCopyright("Copyright © Herman Schoenfeld 2008 - {CurrentYear}")]
[assembly: AssemblyProductDistribution(ProductDistribution.ReleaseCandidate)]
[assembly: AssemblyCompanyNumber("herman@sphere10.com")]
[assembly: AssemblyCompanyLink("https://sphere10.com")]
[assembly: AssemblyProductCode("2fbd6040-dece-45df-9f7a-7d2b562141ad")]
[assembly: AssemblyProductLink("https://sphere10.com/products/automouse")]
[assembly: AssemblyProductPurchaseLink("https://sphere10.com/products/automouse")]
[assembly: AssemblyProductHelpCHM("{StartPath}/AutoMouse.CHM")]
```

### Using Product Information

```csharp
var productInfo = Sphere10Framework.Instance.ServiceProvider
    .GetService<IProductInformationProvider>();

Console.WriteLine($"Product: {productInfo.ProductInformation.ProductName}");
Console.WriteLine($"Version: {productInfo.ProductInformation.ProductVersion}");
Console.WriteLine($"Company: {productInfo.ProductInformation.CompanyName}");
```

### Product Usage Tracking

```csharp
var usageServices = Sphere10Framework.Instance.ServiceProvider
    .GetService<IProductUsageServices>();

var usage = usageServices.ProductUsageInformation;
Console.WriteLine($"Times launched: {usage.NumberOfUsesByUser}");
Console.WriteLine($"First used: {usage.DateOfFirstUse}");
Console.WriteLine($"Last used: {usage.DateOfLastUse}");
```

## 📝 Command-Line Parsing

Built-in command-line parser with attribute-based configuration:

### Defining Options

```csharp
using Sphere10.Framework.Application;

public class Options {
    [Option('v', "verbose", HelpText = "Enable verbose output")]
    public bool Verbose { get; set; }
    
    [Option('i', "input", Required = true, HelpText = "Input file path")]
    public string InputFile { get; set; }
    
    [Option('o', "output", Default = "output.txt", HelpText = "Output file path")]
    public string OutputFile { get; set; }
    
    [Option('n', "count", Default = 10, HelpText = "Number of items to process")]
    public int Count { get; set; }
}
```

### Parsing Arguments

```csharp
var result = Parser.Default.ParseArguments<Options>(args);

result.WithParsed(options => {
    Console.WriteLine($"Input: {options.InputFile}");
    Console.WriteLine($"Output: {options.OutputFile}");
    Console.WriteLine($"Verbose: {options.Verbose}");
});

result.WithNotParsed(errors => {
    foreach (var error in errors) {
        Console.WriteLine($"Error: {error}");
    }
});
```

### Verb Commands

```csharp
[Verb("add", HelpText = "Add items to the list")]
public class AddOptions {
    [Value(0, Required = true, HelpText = "Item to add")]
    public string Item { get; set; }
}

[Verb("remove", HelpText = "Remove items from the list")]
public class RemoveOptions {
    [Value(0, Required = true, HelpText = "Item to remove")]
    public string Item { get; set; }
}

// Parse with verbs
Parser.Default.ParseArguments<AddOptions, RemoveOptions>(args)
    .WithParsed<AddOptions>(opts => AddItem(opts.Item))
    .WithParsed<RemoveOptions>(opts => RemoveItem(opts.Item));
```

## 🌐 Token Resolution

String tokens like `{ProductName}` and `{UserDataDir}` are automatically resolved:

```csharp
// Tokens are resolved in paths and strings
string logPath = Tools.Text.FormatEx("{UserDataDir}/{ProductName}/logs");
// Result: "C:\Users\John\AppData\Local\AutoMouse\logs"

string configPath = Tools.Text.FormatEx("{SystemDataDir}/{ProductName}/config.json");
// Result: "C:\ProgramData\AutoMouse\config.json"
```

Available tokens include:
- `{ProductName}` - Application name
- `{ProductVersion}` - Application version
- `{UserDataDir}` - User's local app data folder
- `{SystemDataDir}` - System-wide program data folder
- `{StartPath}` - Application startup directory
- `{CurrentYear}` - Current year

## 🔧 Built-in Services

The framework registers these services by default:

| Service | Description |
|---------|-------------|
| `ISettingsServices` | Settings save/load operations |
| `IProductInformationProvider` | Product metadata from assembly |
| `IProductUsageServices` | Usage tracking and statistics |
| `IHelpServices` | Help file/URL launching |
| `IWebsiteLauncher` | Open URLs in default browser |
| `IDuplicateProcessDetector` | Detect multiple instances |
| `IProductInstancesCounter` | Count running instances |
| `IAutoRunServices` | Windows startup registration |

## 🎯 Framework Options

Configure framework behavior with options:

```csharp
Sphere10Framework.Instance.StartFramework(
    Sphere10FrameworkOptions.EnableDrm | 
    Sphere10FrameworkOptions.BackgroundLicenseVerify |
    Sphere10FrameworkOptions.EnsureSystemDataDirGloballyAccessible
);
```

| Option | Description |
|--------|-------------|
| `EnableDrm` | Enable DRM/licensing support |
| `BackgroundLicenseVerify` | Verify license in background |
| `EnsureSystemDataDirGloballyAccessible` | Make system data dir accessible to all users |

## 📖 Related Projects

- [Sphere10.Framework](../Sphere10.Framework) - Core framework
- [Sphere10.Framework.Windows.Forms](../Sphere10.Framework.Windows.Forms) - Windows Forms integration
- [Sphere10.Framework.Web.AspNetCore](../Sphere10.Framework.Web.AspNetCore) - ASP.NET Core integration
- [Sphere10.Framework.Communications](../Sphere10.Framework.Communications) - RPC services with DI

## 🌍 Real-World Example

See [AutoMouse](https://github.com/HermanSchoenfeld/AutoMouse) for a complete production application using this framework, demonstrating:
- ModuleConfiguration for service registration
- SettingsObject with complex settings
- ApplicationInitializer for first-run tasks
- Product attributes for app metadata
- Windows Forms integration with LiteMainForm

## ✅ Status & Maturity

- **Core Framework**: Production-tested, stable
- **DI Integration**: Full support for Microsoft.Extensions.DependencyInjection
- **.NET Target**: .NET 10 (`net10.0`)
- **Thread Safety**: Application-wide; services should handle their own thread safety

## ⚖️ License

Distributed under the **MIT License**.

See the LICENSE file for full details. More information: [MIT License](https://opensource.org/license/mit)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

Subclasses of `Sphere10Framework` can raise the existing `VersionChangeDetected` event through `OnVersionChangeDetected(previousVersion, currentVersion)`.
