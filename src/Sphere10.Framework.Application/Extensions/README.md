# Extension methods

Extension methods for service collections and service providers, JSON HTTP responses and configuration connection strings.

`ServiceCollectionExtensions` is declared in the `Microsoft.Extensions.DependencyInjection` namespace, so its members appear wherever service collections are used. Modules call `HasImplementationFor<T>()` to keep a host's registration and add a default only when none exists. `AddInitializer<T>()` and `AddFinalizer<T>()` register the tasks described in [Initialization and finalization tasks](../Lifecycle/README.md), and their keyed overloads register tasks that belong to a named owner instead of the process. Named registrations are in [Named service registrations](../IoC/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Extension`](ConnectionStringSettingsCollectionExtension.cs) | static class | `HasConnectionString` on a `ConnectionStringSettingsCollection`, which returns `false` for a missing or empty connection string |
| [`HttpResponseMessageExtensions`](HttpResponseMessageExtensions.cs) | static class | `ParseStreamAsync<T>` deserializes an `HttpResponseMessage` body stream with Newtonsoft.Json |
| [`ServiceCollectionExtensions`](ServiceCollectionExtensions.cs) | static class | `HasImplementationFor<T>`, `HasImplementation<T>` and the unkeyed and keyed `AddInitializer<T>` and `AddFinalizer<T>` on `IServiceCollection` |
| [`ServiceProviderExtensions`](ServiceProviderExtensions.cs) | static class | `TryGetService<T>`, which returns the last registration, and `GetServiceOrThrow<T>` on `IServiceProvider` |
