# Extensions

Extension methods that start the Sphere10 Framework inside an ASP.NET Core host and add JSON session values, request helpers and per-request user messages.

`AddSphere10Framework` registers the modules that its `Sphere10FrameworkBuilder` callback adds, and `StartSphere10Framework` starts that pending configuration on the built host's service provider. Call the start method after `builder.Build()` and before `app.Run()`. `UseSphere10Framework` performs the same registration on an `IHostBuilder`. `AddSphere10FrameworkLogger` registers the provider from [Logging](../Logging/README.md), and the request parameter helpers delegate to `PageParameterProcessor` in [Processing](../Processing/README.md).

Most of these classes are declared in the Microsoft namespaces of the types they extend. `Microsoft.Extensions.DependencyInjection` holds `IServiceCollectionExtensions`, `Microsoft.Extensions.Hosting` holds the host and HTML content extensions, and `Microsoft.AspNetCore.Http` holds the session extensions. `HttpContextExtensions`, `HttpRequestExtensions` and `Sphere10FrameworkExtensions` are in `Sphere10.Framework.Web.AspNetCore`. `ILoggingBuilderExtensions.cs` is entirely commented out and declares no type.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`HttpContextExtensions`](HttpContextExtensions.cs) | static class | `GetUserMessages` returns a `Collection<UserMessage>` stored in `HttpContext.Items`, so it lives only for the current request. |
| [`HttpRequestExtensions`](HttpRequestExtensions.cs) | static class | Builds a `Uri` from a request, reads enum-named query parameters with `ContainsParameter` and `GetParameter<T>`, and formats the request line, headers and body with `ToStringAsync`. |
| [`IHostBuilderExtensions`](IHostBuilderExtensions.cs) | static class | `UseSphere10Framework` registers the Framework modules on an `IHostBuilder` through `AddSphere10Framework`. |
| [`IHostExtensions`](IHostExtensions.cs) | static class | `StartSphere10Framework` records the host and starts the configuration that `AddSphere10Framework` left pending. |
| [`IHtmlContentBuilderExtensions`](IHtmlContentBuilderExtensions.cs) | static class | `ToHtmlContent` renders an `IHtmlContentBuilder` with an `HtmlEncoder` into one `HtmlString`. |
| [`IServiceCollectionExtensions`](IServiceCollectionExtensions.cs) | static class | `AddSphere10Framework` runs the Framework builder callback and registers its modules, and `AddSphere10FrameworkLogger` registers a `Sphere10LoggerProvider` for a Sphere10 `ILogger`. |
| [`ISessionExtensions`](ISessionExtensions.cs) | static class | Stores and reads session values as JSON through `Tools.Json`, with set, try-get, get, get-or-create and get-or-default variants. |
| [`Sphere10FrameworkExtensions`](Sphere10FrameworkExtensions.cs) | static class | `SetAspNetCoreHost` and `GetAspNetCoreHost` keep the process-wide ASP.NET Core `IHost` beside `Sphere10Framework.Instance`. |
