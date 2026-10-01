<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Web.AspNetCore

Shared ASP.NET Core integration for Sphere10 Framework on .NET 10. Use this layer for hosting, dependency injection, logging, HTTP/session utilities and middleware. Add the separate MVC or Blazor library when you need its controller or component APIs.

## Features

| Area | APIs |
| --- | --- |
| Framework startup and shutdown | `AddSphere10Framework`, `StartSphere10Framework`, `UseSphere10Framework`, host lifecycle initializer. |
| Logging | `AddSphere10FrameworkLogger`, `Sphere10LoggerProvider`, `MicrosoftExtensionsLoggerAdapter`. |
| Session values | `SetJsonObject`, `TryGetJsonObject`, `GetOrCreateJsonObject`, `GetJsonObjectOrDefault`. |
| Request/context helpers | `HttpRequestExtensions`, `GetUserMessages`, `PageParameterProcessor`. |
| HTML content | `IHtmlContentBuilder.ToHtmlContent(HtmlEncoder)`. |
| Networking | `Tools.Web.AspNetCore.ParseNetwork` and optional `CloudflareConnectingIPMiddleware`. |

## Installation

Use the .NET 10 SDK and an ASP.NET Core host, normally a `Microsoft.NET.Sdk.Web` project. Install a version available from your package feed:

```powershell
dotnet add package Sphere10.Framework.Web.AspNetCore --version <version> --source <feed>
```

Replace the placeholders with your feed and exact version. New packages in this checkout may require a local feed; publication is not assumed. [Package validation](../../scripts/package-consumers/README.md) produces a complete local feed and reports a unique validation version.

For a runnable source-reference example, execute from the repository root:

```powershell
dotnet new web --name SharedWebDemo --output ../SharedWebDemo --framework net10.0
dotnet add ../SharedWebDemo/SharedWebDemo.csproj reference src/Sphere10.Framework.Web.AspNetCore/Sphere10.Framework.Web.AspNetCore.csproj
```

## Quick start: framework lifecycle and sessions

Replace the generated `Program.cs` with:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSphere10Framework(_ => { });
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();
app.StartSphere10Framework();
app.UseSession();
app.MapGet("/", () => "Sphere10 Framework is running.");
app.MapGet("/visits", (HttpContext context) => {
	var counter = context.Session.GetOrCreateJsonObject<VisitCounter>("visits");
	counter.Count++;
	context.Session.SetJsonObject("visits", counter);
	return Results.Ok(counter);
});
app.Run();

public sealed class VisitCounter {
	public int Count { get; set; }
}
```

```powershell
dotnet run --project ../SharedWebDemo -p:BuildRevision=0
```

Open the address printed by the host, then visit `/visits` repeatedly in the same browser. The example stores session data in process memory and loses it when the process restarts. A JSON session read returns a deserialized value; call `SetJsonObject` again after modifying it.

`AddSphere10Framework` invokes the `Sphere10FrameworkBuilder` callback and registers modules in the host's service collection. `StartSphere10Framework` must follow `builder.Build()` and consume that pending configuration once. The library's initializer links host shutdown to framework finalization. See the [Application guide](../Sphere10.Framework.Application/README.md) for modules and framework services. `UseSphere10Framework(configure)` is the registration alternative for `IHostBuilder`; it does not replace the subsequent start call.

## Send host logging to a Sphere10 logger

Add these imports and registration statements before `builder.Build()` in the example above:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sphere10.Framework;

builder.Logging.ClearProviders();
builder.Services.AddSphere10FrameworkLogger(new ConsoleLogger());
```

The provider adapts `Microsoft.Extensions.Logging.ILogger` calls to the supplied `Sphere10.Framework.ILogger`. Keep other providers when you want multiple destinations; omit `ClearProviders` in that case. The extension is on `IServiceCollection`, not `ILoggingBuilder`.

## Network parsing and request messages

`ParseNetwork` normalizes host bits for IPv4 and IPv6 rather than requiring an already-normalized network address:

```csharp
using System.Net;

var network = Tools.Web.AspNetCore.ParseNetwork("192.0.2.123/24");
var baseAddress = network.BaseAddress; // 192.0.2.0
var includesClient = network.Contains(IPAddress.Parse("192.0.2.45")); // true
```

To attach messages to the current request, add this endpoint before `app.Run()` in the quick-start host, with the listed imports at the top of `Program.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using Sphere10.Framework.Web;
using Sphere10.Framework.Web.AspNetCore;

app.MapGet("/messages", (HttpContext context) => {
	context.GetUserMessages().Add(new UserMessage("Welcome.", UserMessageType.Information));
	return Results.Ok(context.GetUserMessages());
});
```

The message collection lives in `HttpContext.Items` for that request. It is not session persistence or a cross-request notification queue.

`PageParameterProcessor` and the `HttpRequest.GetParameter<T>(Enum)` extension retain the enum-based request API. Its optional name attribute lives in `Sphere10.Framework.Web.Processing`. Applications should validate query presence and input values before relying on this legacy conversion API. `HttpRequest.ToStringAsync` includes headers and reads the current body stream; it leaves the stream open but does not rewind it.

`CloudflareConnectingIPMiddleware` replaces `Connection.RemoteIpAddress` from the `cf-connecting-ip` header. It does not verify the sender or configure proxy trust. Use it only where the host accepts the header from trusted infrastructure; registering the library alone does not enable it.

## Structure and dependencies

| Folder/file | Contents |
| --- | --- |
| `Extensions/` | Host/service registration, request/context/session and HTML-content helpers. |
| `Lifecycle/` and `ModuleConfiguration.cs` | Framework finalization when the host stops. |
| `Logging/` | Microsoft logging adapter and provider. |
| `Processing/` | Legacy request parameter processor and context accessor. |
| `Middleware/` | Connecting-IP middleware. |
| `AspNetCoreTool.cs` | `Tools.Web.AspNetCore`. |

This project targets `net10.0`, references `Microsoft.AspNetCore.App`, and depends on [Application](../Sphere10.Framework.Application/README.md) and the pure [Web](../Sphere10.Framework.Web/README.md) library. It has no project dependency on MVC, Blazor, WinForms or Drawing. A non-web SDK project consuming these APIs also needs an appropriate ASP.NET Core framework reference.

## Migration from the combined library

| Previous API | Current location |
| --- | --- |
| Sitemap, navigation, messages, HTML formatting and DataTables models | `Sphere10.Framework.Web` (`.AnimateCss` for animation enums). |
| Request `ParameterAttribute` | `Sphere10.Framework.Web.Processing`. |
| Controllers, Razor helpers, forms, filters, routes, `XmlResult`, `ImageResult` | `Sphere10.Framework.Web.AspNetCore.MVC`. |
| Enum select lists | `Tools.Web.Mvc.ToSelectList` in the MVC project. |
| Hosting, HTTP, logging, middleware and DI | This shared project, with their existing namespaces. |

Hosting extensions use the standard `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Hosting` namespaces; session extensions use `Microsoft.AspNetCore.Http`. Importing only `Sphere10.Framework.Web.AspNetCore` does not bring those extension methods into scope.

## Build and verification

From the repository root:

```powershell
dotnet build src/Sphere10.Framework.Web.AspNetCore/Sphere10.Framework.Web.AspNetCore.csproj -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Tests -p:BuildRevision=0
```

The [web regression suite](../../tests/Sphere10.Framework.Web.AspNetCore.Tests) covers IPv4/IPv6 network normalization and shared/MVC integration. [Package validation](../../scripts/package-consumers/README.md) builds external consumers against generated packages. `BuildRevision=0` preserves the local build counter.

## Related projects

- [Web models without ASP.NET Core](../Sphere10.Framework.Web/README.md)
- [MVC forms, controllers and results](../Sphere10.Framework.Web.AspNetCore.MVC/README.md)
- [Blazor application framework and CRUD grid](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md)
- [Minimal MVC tester](../../utils/Sphere10.Framework.Utils.MvcTester/README.md)
