<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💫 Sphere10.Framework.Application

<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

**Application framework and lifecycle management** providing dependency injection integration, settings management, command-line argument parsing, and foundation for building full-featured Sphere10 Framework-based applications.

Sphere10.Framework.Application enables **rapid application development** with built-in **service configuration, CLI argument parsing, settings persistence, and application lifecycle hooks** integrated with Microsoft.Extensions.DependencyInjection.

## ⚡ 10-Second Example

```csharp
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

// Build application with framework
var app = Sphere10App.CreateBuilder()
    .ConfigureServices(services => {
        services.AddSingleton<IRepository, DatabaseRepository>();
        services.AddSingleton<ILogger, ConsoleLogger>();
    })
    .Build();

// Access configured services
var repo = app.ServiceProvider.GetRequiredService<IRepository>();
var logger = app.ServiceProvider.GetRequiredService<ILogger>();

logger.Info("Application started");
repo.SaveData("important", data);

// Application runs until disposal
app.Run();
```

## 🏗️ Core Concepts

**Application Builder Pattern**: Fluent builder API for configuring services, settings, and lifecycle hooks.

**Dependency Injection**: Full integration with Microsoft.Extensions.DependencyInjection for service composition.

**Settings Management**: Type-safe application settings with persistence and validation.

**Lifecycle Hooks**: Startup, configuration, and shutdown extensions for custom initialization.

**Command-Line Parsing**: Attribute-based CLI argument parsing with validation and help generation.

**Product Management**: Product identification, versioning, and licensing support.

## :bulb: Core Examples

### Basic Application Setup

```csharp
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

// Create application with service configuration
var app = Sphere10App.CreateBuilder()
    .AddLogging()  // Add console logging
    .ConfigureServices(services => {
        // Register custom services
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IEmailService, SmtpEmailService>();
        services.AddSingleton<IDataProcessor, DataProcessor>();
    })
    .Build();

// Get configured service
var userRepo = app.ServiceProvider.GetRequiredService<IUserRepository>();
var users = userRepo.GetAllUsers();

Console.WriteLine($"Loaded {users.Count} users");
```

### Settings Management

```csharp
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

// Define typed settings
public class AppSettings {
    public string DatabaseConnection { get; set; }
    public int MaxConnections { get; set; }
    public bool EnableLogging { get; set; }
    public List<string> AllowedOrigins { get; set; }
}

var app = Sphere10App.CreateBuilder()
    .ConfigureSettings<AppSettings>(settings => {
        settings.DatabaseConnection = "Server=localhost;Database=mydb";
        settings.MaxConnections = 100;
        settings.EnableLogging = true;
        settings.AllowedOrigins = new[] { "http://localhost:3000", "https://myapp.com" }.ToList();
    })
    .ConfigureServices((services, settings) => {
        // Use settings in service configuration
        services.AddSingleton(new DatabaseConnection(settings.DatabaseConnection, settings.MaxConnections));
    })
    .Build();

// Access settings anywhere via DI
var appSettings = app.ServiceProvider.GetRequiredService<AppSettings>();
Console.WriteLine($"Database: {appSettings.DatabaseConnection}");
Console.WriteLine($"Max connections: {appSettings.MaxConnections}");
```

### Lifecycle Hooks

```csharp
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

var app = Sphere10App.CreateBuilder()
    .ConfigureServices(services => {
        services.AddSingleton<IInitializer, DatabaseInitializer>();
    })
    .OnStarting(async (app, cancellation) => {
        Console.WriteLine("Application starting...");
        
        // Initialize database
        var initializer = app.ServiceProvider.GetRequiredService<IInitializer>();
        await initializer.InitializeAsync(cancellation);
        
        Console.WriteLine("Application initialized");
    })
    .OnStopping(async (app, cancellation) => {
        Console.WriteLine("Application stopping...");
        
        // Cleanup resources
        var connection = app.ServiceProvider.GetRequiredService<IDbConnection>();
        connection?.Close();
        
        Console.WriteLine("Application stopped");
    })
    .Build();

// Run application until cancellation
await app.RunAsync();
```

### Command-Line Argument Parsing

```csharp
using Sphere10.Framework.Application;

// Define CLI arguments with attributes
[CommandLineOptions]
public class AppOptions {
    [CommandLineArgument("--database", "-d")]
    public string DatabasePath { get; set; } = "data.db";
    
    [CommandLineArgument("--verbose", "-v")]
    public bool Verbose { get; set; } = false;
    
    [CommandLineArgument("--threads", "-t")]
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    
    [CommandLineArgument("--config", "-c", Required = true)]
    public string ConfigFile { get; set; }
}

// Parse command-line arguments
var args = new[] { "--database", "custom.db", "--verbose", "--config", "app.json" };
var options = CommandLineParser.Parse<AppOptions>(args);

Console.WriteLine($"Database: {options.DatabasePath}");    // custom.db
Console.WriteLine($"Verbose: {options.Verbose}");          // true
Console.WriteLine($"Threads: {options.ThreadCount}");      // logical CPU count
Console.WriteLine($"Config: {options.ConfigFile}");        // app.json

// Get help message
string help = CommandLineParser.GetHelpText<AppOptions>();
Console.WriteLine(help);
```

### Service Factories & Scoping

```csharp
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

// Create scoped services for request-like patterns
var app = Sphere10App.CreateBuilder()
    .ConfigureServices(services => {
        // Singleton: one instance for entire application
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // Transient: new instance every time requested
        services.AddTransient<IRequestHandler, RequestHandler>();
        
        // Scoped: one instance per scope (e.g., per request)
        services.AddScoped<IDbContext, DbContext>();
    })
    .Build();

// Use services with scoping
using (var scope = app.ServiceProvider.CreateScope()) {
    var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
    var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler>();
    
    handler.Process(dbContext);
}  // IDbContext disposed here
```

### REST API Integration

```csharp
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

public interface IUserService {
    Task<User> GetUserAsync(int id);
    Task CreateUserAsync(User user);
}

public class UserService : IUserService {
    private readonly IUserRepository _repo;
    
    public UserService(IUserRepository repo) => _repo = repo;
    
    public async Task<User> GetUserAsync(int id) => await _repo.GetAsync(id);
    public async Task CreateUserAsync(User user) => await _repo.SaveAsync(user);
}

// Build application with REST support
var app = Sphere10App.CreateBuilder()
    .ConfigureServices(services => {
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IUserService, UserService>();
        services.AddRestApi();  // Configure REST helpers
    })
    .Build();

// Use service through REST framework (in ASP.NET Core or similar)
var userService = app.ServiceProvider.GetRequiredService<IUserService>();
var user = await userService.GetUserAsync(42);
```

## 🎯 Module Organization

- **Core**: `Sphere10App`, `Sphere10AppBuilder` - Application creation and configuration
- **Lifecycle**: Startup/shutdown hooks with async cancellation support
- **IoC/DI**: Integration with `Microsoft.Extensions.DependencyInjection`
- **CommandLine**: `CommandLineParser`, `CommandLineArgument` attributes for CLI parsing
- **Settings**: Typed settings classes with validation and persistence
- **Product**: `ProductInfo`, versioning, and license management
- **Presentation**: Base classes for MVVM, view models, and UI bindings
- **Rest**: Helper utilities for REST API development

## 🔧 Application Builder Pattern

The builder pattern provides fluent configuration:

```csharp
Sphere10App.CreateBuilder()
    .AddLogging()
    .AddSettings<AppSettings>()
    .ConfigureServices(services => {/* ... */})
    .OnStarting(async (app, ct) => {/* ... */})
    .OnInitialized(async (app, ct) => {/* ... */})
    .OnStopping(async (app, ct) => {/* ... */})
    .Build()
    .Run();
```

## 🌐 Dependency Injection Container

Full Microsoft.Extensions.DependencyInjection support:

- **Singleton**: Application-wide single instance (configuration, loggers)
- **Transient**: New instance per request (handlers, processors)
- **Scoped**: Instance per logical scope (request, transaction)
- **Factory**: Custom factory methods for complex object creation

## ⚠️ Design Patterns

- **Composition Root**: All service configuration in one place at startup
- **Dependency Inversion**: Depend on abstractions, not implementations
- **Service Locator**: Get services from `ServiceProvider` when needed
- **Middleware Pattern**: Apply transformations/cross-cutting concerns
- **Settings by Convention**: Type-safe settings with strong validation

## 📖 Related Projects

- [Sphere10.Framework](../Sphere10.Framework) - Core framework
- [Sphere10.Framework.Web.AspNetCore](../Sphere10.Framework.Web.AspNetCore) - ASP.NET Core integration
- [Sphere10.Framework.Communications](../Sphere10.Framework.Communications) - RPC services with DI

## ✅ Status & Maturity

- **Core Framework**: Production-tested, stable
- **DI Integration**: Full support for Microsoft.Extensions.DependencyInjection
- **.NET Target**: .NET 8.0+ (primary)
- **Thread Safety**: Application-wide; services should handle their own thread safety
- **Async Support**: Full async/await support throughout lifecycle hooks

## 📦 Dependencies

- **Sphere10 Framework**: Core framework
- **Microsoft.Extensions.DependencyInjection**: Service composition
- **System.Configuration.ConfigurationManager**: Configuration support
- **System.Reflection**: Service discovery and attribute processing

## ⚖️ License

Distributed under the **MIT NON-AI License**.

See the LICENSE file for full details. More information: [Sphere10 NON-AI-MIT License](https://sphere10.com/legal/NON-AI-MIT)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

