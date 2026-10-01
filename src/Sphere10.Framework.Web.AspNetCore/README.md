# Sphere10.Framework.Web.AspNetCore

Shared ASP.NET Core integration for Sphere10 Framework on .NET 10. This library provides host lifecycle integration, dependency injection, framework logging adapters, HTTP/session extensions, HTML content builders, and middleware. It references `Microsoft.AspNetCore.App`, Sphere10.Framework.Application, and [Sphere10.Framework.Web](../Sphere10.Framework.Web/README.md).

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSphere10Framework(_ => { });
var app = builder.Build();
app.StartSphere10Framework();
app.MapGet("/", () => "Hello from Sphere10 Framework");
app.Run();
```

`AddSphere10Framework` accepts a configuration callback for `Sphere10FrameworkBuilder`. Call it before `StartSphere10Framework`. `AddSphere10FrameworkLogger` registers a Sphere10 logger with Microsoft.Extensions.Logging. `UseSphere10Framework` provides the corresponding `IHostBuilder` extension.

Other shared APIs include `HttpRequestExtensions`, `HttpContextExtensions`, `PageParameterProcessor`, JSON session helpers, and `Tools.Web.AspNetCore.ParseNetwork`. CIDR parsing normalizes IPv4 and IPv6 host bits into the network base address. `CloudflareConnectingIPMiddleware` reads the Cloudflare connecting-IP header; configure the hosting infrastructure to accept traffic only from trusted proxies before using it.

## Migration from the combined library

- HTML formatting, Animate.css enums, sitemap/navigation/message models, and jQuery DataTables models moved to `Sphere10.Framework.Web`.
- Forms, controller/view helpers, filters, MVC routes, `ImageResult`, and `XmlResult` moved to [Sphere10.Framework.Web.AspNetCore.MVC](../Sphere10.Framework.Web.AspNetCore.MVC/README.md). Add that project/package and import its namespace when using these APIs.
- Request parameter attributes now use `Sphere10.Framework.Web.Processing.ParameterAttribute` to avoid shadowing Blazor component attributes.
- Enum select-list conversion is now `Tools.Web.Mvc.ToSelectList` in the MVC library.
- Hosting, HTTP, logging, middleware, and DI APIs retain their existing namespaces. The shared library does not reference the MVC or Blazor libraries.
