<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💻 Sphere10.Framework.Web.AspNetCore

**ASP.NET Core integration library** providing framework bootstrapping, middleware, extensions, and utilities for building web applications and APIs with Sphere10 Framework.

Sphere10.Framework.Web.AspNetCore bridges Sphere10 Framework with the **ASP.NET Core ecosystem**, enabling seamless integration of framework services, logging, lifecycle management, and custom middleware.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Web.AspNetCore
```

## ⚡ 10-Second Example

```csharp
using Sphere10.Framework.Application;
using Sphere10.Framework.Web.AspNetCore;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Register Sphere10 Framework services
builder.Services.AddSphere10Framework();

var app = builder.Build();

// Start Sphere10 Framework with ASP.NET Core host
app.StartSphere10Framework();

app.MapGet("/api/hello", () => Results.Ok(new { 
    message = "Hello from Sphere10 Framework!",
    timestamp = DateTime.UtcNow 
}));

app.Run();
```

## 🏗️ Core Concepts

**Framework Integration**: `AddSphere10Framework()` and `StartSphere10Framework()` extension methods for bootstrapping.

**Middleware Pipeline**: Custom middleware components like `CloudflareConnectingIPMiddleware` for Cloudflare IP resolution.

**HTML Utilities**: Animation classes, value beautification, and formatting helpers via `Tools.Web.Html`.

**Sitemap Support**: `SitemapXml` class for generating SEO sitemaps.

**Form Processing**: Bootstrap form helpers and form result handling.

**SelectList Helpers**: `Tools.Web.AspNetCore.ToSelectList<TEnum>()` for building dropdown lists from enums.

## 🔧 Core Examples

### Bootstrap Sphere10 Framework in ASP.NET Core

```csharp
using Sphere10.Framework.Application;
using Sphere10.Framework.Web.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register all Sphere10 Framework module services
builder.Services.AddSphere10Framework();

// Optionally add Sphere10 Framework logger provider
builder.Services.AddSphere10FrameworkLogger(myLogger);

var app = builder.Build();

// Start the framework with the ASP.NET Core host
// This initializes all registered modules and lifecycle monitors
app.StartSphere10Framework(Sphere10FrameworkOptions.Default);

app.MapControllers();
app.Run();
```

### Cloudflare Connecting IP Middleware

Resolve the real client IP when behind Cloudflare:

```csharp
using Sphere10.Framework.Web.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Add middleware to extract real IP from Cloudflare header
app.UseMiddleware<CloudflareConnectingIPMiddleware>();

app.MapGet("/ip", (HttpContext context) => 
    Results.Ok(new { ip = context.Connection.RemoteIpAddress?.ToString() }));

app.Run();
```

### Enum to SelectList Conversion

Generate dropdown options from enum values:

```csharp
using Microsoft.AspNetCore.Mvc.Rendering;
using Sphere10.Framework;

public enum Priority { Low, Medium, High, Critical }

// In a controller or Razor page:
var selectList = Tools.Web.AspNetCore.ToSelectList<Priority>(
    selectedItem: Priority.Medium,
    sort: SortDirection.Ascending);

// Or from a Type:
var selectList2 = Tools.Web.AspNetCore.ToSelectList(
    typeof(Priority), 
    selectedItem: null, 
    sort: null);
```

### Sitemap Generation

Generate XML sitemaps for SEO:

```csharp
using Sphere10.Framework.Web.AspNetCore;

[ApiController]
[Route("")]
public class SitemapController : ControllerBase {
    [HttpGet("sitemap.xml")]
    [Produces("application/xml")]
    public IActionResult GetSitemap() {
        var sitemap = new SitemapXml();
        
        // Add pages with optional metadata
        sitemap.Add("/", DateTime.UtcNow, SitemapXml.Frequency.Weekly, 1.0);
        sitemap.Add("/about", DateTime.UtcNow, SitemapXml.Frequency.Monthly, 0.8);
        sitemap.Add("/products", DateTime.UtcNow, SitemapXml.Frequency.Daily, 0.9);
        
        // Add dynamic product URLs
        var products = GetProducts();
        foreach (var product in products) {
            if (!sitemap.HasNode($"/products/{product.Id}")) {
                sitemap.Add(
                    $"/products/{product.Id}",
                    product.UpdatedDate,
                    SitemapXml.Frequency.Weekly,
                    0.7);
            }
        }
        
        // Serialize to XML
        var xml = Tools.Xml.WriteToString(sitemap);
        return Content(xml, "application/xml");
    }
}
```

### HTML Animation Classes

Use Animate.css classes via the Html helper:

```csharp
using Sphere10.Framework.Web.AspNetCore;

// Get a random entry animation class
string entryAnimation = Tools.Web.Html.RandomEntryAnimationClass(AnimationDelay.Seconds_1_0);
// Returns: "animated bounceIn delay-1s" (or similar)

// Get a random exit animation class
string exitAnimation = Tools.Web.Html.RandomExitAnimationClass();

// Get a specific animation class
string slideIn = Tools.Web.Html.AnimationClass(
    Animation.slideInLeft, 
    AnimationDelay.Seconds_0_5);
// Returns: "animated slideInLeft delay-0.5s"

// Beautify values for display
string formatted = Tools.Web.Html.Beautify(DateTime.Now);
// Returns: "2024-01-15 14:30:00"

string hexBytes = Tools.Web.Html.Beautify(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
// Returns: "0xDEADBEEF"
```

### Form Result Handling

Handle form submissions with typed results:

```csharp
using Sphere10.Framework.Web.AspNetCore;

// Form result types
public enum FormResultType {
    Success,
    Error,
    Warning,
    Info
}

// Return structured form results
public class ProductController : Controller {
    [HttpPost]
    public IActionResult Create(ProductModel model) {
        if (!ModelState.IsValid) {
            return Json(new FormResult {
                ResultType = FormResultType.Error,
                Message = "Validation failed"
            });
        }
        
        // Process form...
        return Json(new FormResult {
            ResultType = FormResultType.Success,
            Message = "Product created successfully"
        });
    }
}
```

## 📋 API Reference

### Extension Methods

| Extension | Target | Description |
|-----------|--------|-------------|
| `AddSphere10Framework()` | `IServiceCollection` | Registers all Sphere10 Framework module services |
| `AddSphere10FrameworkLogger()` | `IServiceCollection` | Adds Sphere10 logger as a provider |
| `StartSphere10Framework()` | `IHost` | Initializes framework with ASP.NET Core host |

### Tools.Web.AspNetCore

| Method | Description |
|--------|-------------|
| `ParseNetwork(cidr)` | Parses CIDR notation into `IPNetwork` |
| `ToSelectList<TEnum>()` | Converts enum to `SelectList` for dropdowns |
| `ToSelectList(Type, ...)` | Converts enum type to `SelectList` |

### Tools.Web.Html

| Method | Description |
|--------|-------------|
| `Beautify(object)` | Formats values for HTML display |
| `Percent(decimal)` | Formats decimal as percentage |
| `AnimationClass(animation, delay)` | Gets CSS animation class string |
| `RandomEntryAnimationClass()` | Gets random entry animation |
| `RandomExitAnimationClass()` | Gets random exit animation |

### SitemapXml

| Member | Description |
|--------|-------------|
| `Add(url, lastModified?, frequency?, priority?)` | Adds URL to sitemap |
| `HasNode(url)` | Checks if URL exists in sitemap |
| `Nodes` | Gets array of sitemap nodes |
| `Frequency` | Enum: `Never`, `Yearly`, `Monthly`, `Weekly`, `Daily`, `Hourly`, `Always` |

### Middleware

| Class | Description |
|-------|-------------|
| `CloudflareConnectingIPMiddleware` | Extracts real IP from `cf-connecting-ip` header |

## 📦 Dependencies

- **Sphere10.Framework.Application**: Application framework and lifecycle management
- **Microsoft.AspNetCore.App**: ASP.NET Core runtime
- **Microsoft.AspNetCore.Mvc**: MVC and controller support

## ⚠️ Best Practices

- **Call `StartSphere10Framework()` early**: Before mapping routes to ensure modules are initialized
- **Use Cloudflare middleware first**: Add it early in the pipeline when behind Cloudflare
- **Async all the way**: Use async/await throughout request handlers
- **Validate input**: Use model validation attributes and check `ModelState`

## ✅ Status & Compatibility

- **Maturity**: Production-ready for web applications and APIs
- **.NET Target**: .NET 8.0+ (primary), .NET 6.0+ (compatible)
- **Platform Support**: Cross-platform (Windows, Linux, macOS)

## 📖 Related Projects

- [Sphere10.Framework](../Sphere10.Framework) - Core framework
- [Sphere10.Framework.Application](../Sphere10.Framework.Application) - Application lifecycle management

## 👤 Author

**Herman Schoenfeld** - Software Engineer

