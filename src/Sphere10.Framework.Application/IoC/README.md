# Named service registrations

Registration and lookup of several implementations of one service type by name, on top of Microsoft.Extensions.DependencyInjection.

`AddNamedTransient`, `AddNamedSingleton` and `AddNamed` register an implementation under its concrete type, record its name and register `INamedLookup<TService>` the first time a service type is named. `GetNamedService<T>`, `GetRequiredNamedService<T>` and `HasNamedService<T>` then resolve an implementation by name. Each registration also maps the service type itself to that implementation as a singleton, so resolving the unnamed service returns the last named registration. Named registrations require an implementation type or instance, and a factory is rejected. The name maps live in the static `NamedLookupInfo`, so they are shared by every service collection in the process, and adding a name twice throws. Both extension classes are declared in the `Microsoft.Extensions.DependencyInjection` namespace. The other service-collection helpers are in [Extension methods](../Extensions/README.md).

```csharp
services.AddNamedSingleton<IExporter, CsvExporter>("csv");
services.AddNamedSingleton<IExporter, JsonExporter>("json");

var exporter = provider.GetRequiredNamedService<IExporter>("json");
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`INamedLookup<T>`](INamedLookup.cs) | interface | Indexer and `ContainsKey` over the named implementations of one service type |
| [`IServiceCollectionNamedRegistrationsExtensions`](NamedRegistrationsServiceCollectionExtensions.cs) | static class | `AddNamedTransient`, `AddNamedSingleton`, `AddNamed` and `HasNamedImplementationFor` on `IServiceCollection` |
| [`IServiceProviderNamedRegistrationsExtensions`](IServiceProviderNamedRegistrationsExtensions.cs) | static class | `GetNamedService`, `GetRequiredNamedService` and `HasNamedService` on `IServiceProvider` |
| [`NamedLookup<T>`](NamedLookup.cs) | class | `INamedLookup<T>` that resolves a name through the process-wide name map and the service provider |
| [`NamedLookupInfo`](NamedLookupInfo.cs) | static class | Process-wide maps from service type to its names and implementation types |
