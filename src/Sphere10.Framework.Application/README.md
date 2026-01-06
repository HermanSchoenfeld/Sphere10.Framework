<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💫 Sphere10.Framework.Application

**Complete application framework** providing dependency injection, modular architecture, settings management, lifecycle hooks, and product information for building production-ready Sphere10 Framework-based applications.

Sphere10.Framework.Application enables **rapid application development** by providing a complete infrastructure for **service composition, settings persistence, initialization/finalization pipelines, and modular configuration**—all integrated with Microsoft.Extensions.DependencyInjection.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Application
```

## ⚡ Quick Start

Here's a complete Windows Forms application using the framework (from [AutoMouse](https://github.com/HermanSchoenfeld/AutoMouse)):

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

static class Program {
    static void Main(params string[] args) {
        // Single instance enforcement
        using (new SingleApplicationInstanceScope()) {
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Start the framework and run the application
            Sphere10Framework.Instance.StartWinFormsApplication<MainForm>();
        }
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
- **.NET Target**: .NET 8.0+ (primary)
- **Thread Safety**: Application-wide; services should handle their own thread safety

## ⚖️ License

Distributed under the **MIT NON-AI License**.

See the LICENSE file for full details. More information: [Sphere10 NON-AI-MIT License](https://sphere10.com/legal/NON-AI-MIT)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

