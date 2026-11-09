# Logging

A Microsoft.Extensions.Logging provider and logger that forward ASP.NET Core log entries to a Sphere10 `ILogger`.

Register the provider with `AddSphere10FrameworkLogger` from [Extensions](../Extensions/README.md). The provider returns a new `MicrosoftExtensionsLoggerAdapter` over the same Sphere10 logger for every category. The adapter is a `LoggerDecorator`, so it reads the decorated logger's `LogOptions` to answer `IsEnabled`. `Trace` and `Debug` map to `Debug`, `Information` maps to `Info`, `Warning` maps to `Warning`, and `Error` and `Critical` map to `Error`, or to `Exception` when an exception is attached. The adapter drops the category name and returns an empty scope from `BeginScope`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`MicrosoftExtensionsLoggerAdapter`](MicrosoftExtensionsLoggerAdapter.cs) | class | Implements `Microsoft.Extensions.Logging.ILogger` over a Sphere10 `ILogger`, mapping host log levels to Sphere10 methods and appending exception text. |
| [`Sphere10LoggerProvider`](Sphere10LoggerProvider.cs) | class | `ILoggerProvider` that creates a `MicrosoftExtensionsLoggerAdapter` over its Sphere10 logger for each category. |
