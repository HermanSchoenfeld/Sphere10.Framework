<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Web.AspNetCore

This library is the shared ASP.NET Core integration for Sphere10 Framework. Use it to start and stop the Framework with an ASP.NET Core host and to connect logging, sessions, requests and middleware. The [MVC](../Sphere10.Framework.Web.AspNetCore.MVC/README.md) and [Blazor](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md) libraries build on this layer and add their controller and component APIs.

## Features

| Area | APIs |
| --- | --- |
| Framework startup and shutdown | `AddSphere10Framework`, `UseSphere10Framework` and `StartSphere10Framework`, the `ModuleConfiguration` that ends the Framework when the host stops, and `GetAspNetCoreHost`. |
| Logging | `AddSphere10FrameworkLogger`, `Sphere10LoggerProvider` and `MicrosoftExtensionsLoggerAdapter`. |
| Session values | `SetJsonObject`, `TryGetJsonObject`, `GetJsonObject`, `GetOrCreateJsonObject` and `GetJsonObjectOrDefault`. |
| Requests and context | `GetUserMessages`, `GetParameter<T>(Enum)`, `PageParameterProcessor`, `GetUri`, `ToStringAsync` and `HttpContextEx`. |
| HTML content | `IHtmlContentBuilder.ToHtmlContent(HtmlEncoder)`. |
| Networking | `Tools.Web.AspNetCore.ParseNetwork` and the opt-in `CloudflareConnectingIPMiddleware`. |

## Installation

Reference the package from an ASP.NET Core project that targets .NET 10 or later:

```powershell
dotnet add package Sphere10.Framework.Web.AspNetCore
```

Pass `--version` to match the other Sphere10.Framework packages in the application, and `--source` to restore from a local folder feed. The root README explains how to [build and publish the packages](../../README.md#building-and-publishing-packages), including to a [private folder feed](../../README.md#private-feed).

To build against the source instead, reference the project. From the repository root, these commands create the host for the examples below, beside the repository:

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
builder.Services.AddSphere10Framework(framework => framework
	.AddModule<Sphere10.Framework.Web.AspNetCore.ModuleConfiguration>());
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

Run the host from the repository root:

```powershell
dotnet run --project ../SharedWebDemo -p:BuildRevision=0
```

Open the address that the host prints, then visit `/visits` repeatedly in the same browser. The count lives in the session, which this example keeps in process memory, so it resets when the process restarts. A session read returns a deserialized copy, so call `SetJsonObject` again after you change the value. The session extensions serialize with `Tools.Json`, which uses Newtonsoft.Json.

### How startup and shutdown work

`AddSphere10Framework` runs the `Sphere10FrameworkBuilder` callback and registers the modules that the callback adds in the host's service collection. Modules are never discovered automatically. `Sphere10.Framework.Web.AspNetCore.ModuleConfiguration` registers the initializer that calls `Sphere10Framework.Instance.EndFramework()` when the host raises `ApplicationStopped`, so without that module nothing ends the Framework when the host stops. Add `Sphere10.Framework.Application.ModuleConfiguration` as well when the host needs the product information and settings services. The [Application guide](../Sphere10.Framework.Application/README.md) describes modules and process tasks.

`StartSphere10Framework` must follow `builder.Build()` and precede `app.Run()`. It starts the pending configuration once, on the host's service provider, and the host keeps ownership of that provider. `Sphere10Framework.Instance.GetAspNetCoreHost()` then returns the host. For an `IHostBuilder`, `UseSphere10Framework(configure)` performs the same registration, and the start call is still required. `Sphere10Framework.Instance` is a process-wide singleton, so only one host in a process can run the Framework at a time.

Set `Sphere10FrameworkOptions` with `WithOptions` inside the `AddSphere10Framework` callback, because modules read them during registration. For example, `Sphere10.Framework.Application.ModuleConfiguration` reads `EnsureSystemDataDirGloballyAccessible` then. `StartSphere10Framework` takes no options and keeps the ones the callback set.

These calls manage the process-wide Framework. A [Blazor application](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md) built with `BuildBlazorApplication` also has a scoped `IApplication` for each interactive circuit. That application initializes when its circuit opens and finalizes when the circuit closes. Both steps run the application's own tasks and the tasks registered under each loaded plugin's name. Prerendering and temporary disconnects run neither. Exiting the application ends only that circuit's application and does not stop the host. Use `IApplication.Logger` for application and screen diagnostics.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Web.AspNetCore` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [Extensions](Extensions/README.md) | Extension methods that start the Sphere10 Framework inside an ASP.NET Core host and add JSON session values, request helpers and per-request user messages. | [source](Extensions/) |
| [Lifecycle](Lifecycle/README.md) | The internal initializer that ends the Sphere10 Framework when the ASP.NET Core host stops. | [source](Lifecycle/) |
| [Logging](Logging/README.md) | A Microsoft.Extensions.Logging provider and logger that forward ASP.NET Core log entries to a Sphere10 `ILogger`. | [source](Logging/) |
| [Middleware](Middleware/README.md) | ASP.NET Core middleware that replaces the connection's remote address with the client address Cloudflare reports. | [source](Middleware/) |
| [Processing](Processing/README.md) | Typed query-string parameter reading and a static accessor for the current `HttpContext`. | [source](Processing/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`ModuleConfiguration`](ModuleConfiguration.cs) | class | Framework module that registers the initializer which calls `Sphere10Framework.Instance.EndFramework` when the host raises `ApplicationStopped`. |
| [`Tools.Web.AspNetCore`](AspNetCoreTool.cs) | tools | `ParseNetwork` parses IPv4 or IPv6 CIDR notation into a `System.Net.IPNetwork` and clears the host bits. |
<!-- domains:end -->

## Send host logging to a Sphere10 logger

Add these imports at the top of the quick start's `Program.cs` and the two statements before `builder.Build()`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sphere10.Framework;

builder.Logging.ClearProviders();
builder.Services.AddSphere10FrameworkLogger(new ConsoleLogger());
```

`AddSphere10FrameworkLogger` extends `IServiceCollection`, not `ILoggingBuilder`. It registers a `Sphere10LoggerProvider`, which gives each logging category a `MicrosoftExtensionsLoggerAdapter` over the supplied `Sphere10.Framework.ILogger`. Omit `ClearProviders` to keep the host's other providers as additional destinations.

| Host level | Sphere10 method | Enabled by |
| --- | --- | --- |
| `Trace`, `Debug` | `Debug` | `LogOptions.DebugEnabled` |
| `Information` | `Info` | `LogOptions.InfoEnabled` |
| `Warning` | `Warning` | `LogOptions.WarningEnabled` |
| `Error`, `Critical` | `Error` | `LogOptions.ErrorEnabled` |

The adapter writes the text that the host's formatter produces and drops the category name and scopes. An `Error` or `Critical` entry with an exception goes to `ILogger.Exception`, and at the other levels the exception's text is appended to the message, with its stack trace when `LogOptions.ExceptionDetailEnabled` is set.

## Request and network helpers

### Network parsing

`ParseNetwork` parses IPv4 or IPv6 CIDR notation into a `System.Net.IPNetwork` and clears the host bits, so the address does not have to be normalized first:

```csharp
using System.Net;

var network = Tools.Web.AspNetCore.ParseNetwork("192.0.2.123/24");
var baseAddress = network.BaseAddress; // 192.0.2.0
var includesClient = network.Contains(IPAddress.Parse("192.0.2.45")); // true
```

It throws `ArgumentException` for an invalid address and for a prefix length that is missing, not a number or outside the address's range.

### Messages for the current request

`GetUserMessages()` returns a `Collection<UserMessage>` that is stored in `HttpContext.Items`. The collection lives only for the current request, so it is neither session state nor a cross-request notification queue. To try it, add the imports at the top of the quick start's `Program.cs` and the endpoint before `app.Run()`:

```csharp
using Microsoft.AspNetCore.Http;
using Sphere10.Framework.Web;
using Sphere10.Framework.Web.AspNetCore;

app.MapGet("/messages", (HttpContext context) => {
	context.GetUserMessages().Add(new UserMessage("Welcome.", UserMessageType.Information));
	return Results.Ok(context.GetUserMessages());
});
```

### Typed query parameters

`HttpRequest.GetParameter<T>(Enum)` and `PageParameterProcessor` read a query-string value named by an enum member and parse it with `Tools.Parser`. The member's name is the parameter name unless a [`ParameterAttribute`](../Sphere10.Framework.Web/README.md#request-parameter-names) overrides it. `ContainsParameter` reports whether the query string carries the parameter, so call it before you read an optional parameter. `GetParameter<T>` throws `SoftwareException` when the query string lacks the value, and it throws `FormatException` when the value does not parse as `T`.

### Cloudflare client addresses

`CloudflareConnectingIPMiddleware` replaces `HttpContext.Connection.RemoteIpAddress` with the address in the `cf-connecting-ip` request header. The library does not register it, so add it early in the pipeline, before any middleware that reads the client address:

```csharp
using Sphere10.Framework.Web.AspNetCore;

app.UseMiddleware<CloudflareConnectingIPMiddleware>();
```

The middleware trusts the header without checking the sender. Enable it only when the host accepts requests from Cloudflare alone. A header value that is not an IP address is ignored, and the connection keeps its own address.

### Other helpers

| API | Behavior |
| --- | --- |
| `HttpRequest.GetUri()` | Builds a `Uri` from the request's scheme, host, port, path and query. When the host carries no port, the `Uri` uses the scheme's default port, such as 443 for HTTPS. |
| `HttpRequest.ToStringAsync()` | Formats the request line, headers and body as text. It reads the body to the end and leaves the stream open. It restores the position of a seekable body, so call `EnableBuffering()` first when another reader needs the body afterwards. |
| `HttpContextEx.Current` | Returns the current `HttpContext` from a static accessor. Register the accessor with `AddHttpContextAccessor()` and pass it to `HttpContextEx.Configure` at startup first. |
| `IHtmlContentBuilder.ToHtmlContent(HtmlEncoder)` | Renders the builder's content into one `HtmlString`. |

## Namespaces

The hosting, service and session extensions live in the Microsoft namespaces of the types they extend, so importing `Sphere10.Framework.Web.AspNetCore` alone does not bring them into scope.

| Namespace | APIs |
| --- | --- |
| `Microsoft.Extensions.DependencyInjection` | `AddSphere10Framework` and `AddSphere10FrameworkLogger` |
| `Microsoft.Extensions.Hosting` | `UseSphere10Framework`, `StartSphere10Framework` and `ToHtmlContent` |
| `Microsoft.AspNetCore.Http` | The session JSON extensions |
| `Sphere10.Framework.Web.AspNetCore` | `GetUserMessages`, `GetParameter<T>`, `GetUri`, `ToStringAsync`, `GetAspNetCoreHost`, `PageParameterProcessor`, `HttpContextEx`, `CloudflareConnectingIPMiddleware`, `Sphere10LoggerProvider`, `MicrosoftExtensionsLoggerAdapter` and `ModuleConfiguration` |
| `Tools.Web` | `Tools.Web.AspNetCore.ParseNetwork` |

## Structure and dependencies

| Folder or file | Contents |
| --- | --- |
| `Extensions/` | Service, host, session, request, context and HTML-content extensions. |
| `Lifecycle/` and `ModuleConfiguration.cs` | The initializer that ends the Framework when the host stops. |
| `Logging/` | The Microsoft logging adapter and provider. |
| `Processing/` | `PageParameterProcessor` and `HttpContextEx`. |
| `Middleware/` | `CloudflareConnectingIPMiddleware`. |
| `AspNetCoreTool.cs` | `Tools.Web.AspNetCore`. |

The project targets `net10.0`, and also `net11.0` when a .NET 11 SDK builds it. It has a `Microsoft.AspNetCore.App` framework reference and project references to [Sphere10.Framework.Application](../Sphere10.Framework.Application/README.md) and the host-independent [Sphere10.Framework.Web](../Sphere10.Framework.Web/README.md). It does not reference MVC, Blazor, Windows Forms or Drawing. The framework reference flows to referencing projects, so a plain `Microsoft.NET.Sdk` project such as the web test project can use these APIs.

## Upgrading from the combined library

Releases up to 3.1.2 shipped the web models and the MVC helpers inside this package. They now live in two other packages, and this package keeps the hosting, HTTP, logging and middleware APIs under their existing namespaces.

| Previous API | Current package and namespace |
| --- | --- |
| Sitemap, navigation, message, HTML formatting and DataTables models | `Sphere10.Framework.Web`, with the animation enums in `Sphere10.Framework.Web.AnimateCss` |
| `ParameterAttribute` | `Sphere10.Framework.Web`, in the `Sphere10.Framework.Web.Processing` namespace |
| Controller and Razor helpers, forms, filters, routing, `XmlResult` and `ImageResult` | `Sphere10.Framework.Web.AspNetCore.MVC` |
| `Tools.Web.AspNetCore.ToSelectList` | `Tools.Web.Mvc.ToSelectList` in `Sphere10.Framework.Web.AspNetCore.MVC` |

## Build and verification

From the repository root:

```powershell
dotnet build src/Sphere10.Framework.Web.AspNetCore/Sphere10.Framework.Web.AspNetCore.csproj -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Tests -p:BuildRevision=0
```

The [shared web tests](../../tests/Sphere10.Framework.Web.AspNetCore.Tests) cover `ParseNetwork` normalization and rejection for IPv4 and IPv6, `GetUri` default ports, `ToStringAsync`, query parameters, the Cloudflare middleware and the logging adapter's exception forwarding, together with the MVC results and form scope. They do not cover the hosting or session extensions. [Package validation](../../scripts/package-consumers/README.md) builds and publishes an external host against the packed libraries and checks its responses over HTTP. `-p:BuildRevision=0` stops a local build from allocating the next build number.

## Related projects

- [Web models without ASP.NET Core](../Sphere10.Framework.Web/README.md)
- [MVC results, Razor helpers and forms](../Sphere10.Framework.Web.AspNetCore.MVC/README.md)
- [Blazor application framework and CRUD grid](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md)
- [Minimal MVC tester](../../utils/Sphere10.Framework.Utils.MvcTester/README.md)
