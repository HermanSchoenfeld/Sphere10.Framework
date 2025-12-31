<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💻 Sphere10.Framework.Web.AspNetCore

**ASP.NET Core integration library** providing middleware, extensions, and utilities for building high-performance web applications and APIs with Sphere10 Framework framework.

Sphere10.Framework.Web.AspNetCore bridges Sphere10 Framework with **ASP.NET Core ecosystem**, enabling seamless integration of logging, configuration, dependency injection, routing, and custom middleware while following .NET best practices.

## ⚡ 10-Second Example

```csharp
using Sphere10.Framework.Web.AspNetCore;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Add Sphere10 Framework services
builder.Services.AddSphere10WebServices();

var app = builder.Build();

// Add Sphere10 Framework middleware
app.UseSphere10Logging();
app.UseSphere10ErrorHandling();

app.MapGet("/api/hello", () => Results.Ok(new { 
    message = "Hello from Sphere10 Framework!",
    timestamp = DateTime.UtcNow 
}));

app.Run();
```

## 🏗️ Core Concepts

**Middleware Pipeline**: Custom middleware components for logging, error handling, and cross-cutting concerns.

**Service Registration**: Extension methods for configuring Sphere10 Framework services in ASP.NET Core.

**Routing Extensions**: Enhanced routing helpers and conventions.

**HTML Processing**: HTML manipulation, parsing, and generation utilities.

**XML Processing**: XML document handling and transformation.

**Sitemap Support**: Automatic sitemap generation for SEO.

**Form Handling**: Server-side form processing and validation.

## 🔧 Core Examples

### Configure Sphere10 Framework Web Services

```csharp
using Sphere10.Framework.Web.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services
    .AddControllers()
    .AddSphere10SerializationSupport();

// Add Sphere10 Framework web services
builder.Services.AddSphere10WebServices(options => {
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.LogHttpRequests = true;
    options.CompressResponses = true;
});

// Add logging
builder.Services.AddSphere10Logging(builder.Configuration.GetSection("Logging"));

// Build app
var app = builder.Build();

// Configure middleware
app.UseSphere10ErrorHandling();
app.UseSphere10Logging();
app.UseSphere10ResponseCompression();

app.MapControllers();
app.Run();
```

### Custom Middleware & Filters

```csharp
using Sphere10.Framework.Web.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

// Create custom action filter
public class Sphere10ValidationFilter : IAsyncActionFilter {
    private readonly ILogger<Sphere10ValidationFilter> _logger;
    
    public Sphere10ValidationFilter(ILogger<Sphere10ValidationFilter> logger) {
        _logger = logger;
    }
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        // Pre-execution validation
        var modelState = context.ModelState;
        if (!modelState.IsValid) {
            var errors = modelState
                .Where(x => x.Value.Errors.Any())
                .Select(x => new { 
                    Field = x.Key,
                    Errors = x.Value.Errors.Select(e => e.ErrorMessage)
                })
                .ToList();
                
            _logger.LogWarning("Model validation failed: {@Errors}", errors);
            context.Result = new BadRequestObjectResult(new { 
                message = "Validation failed",
                errors = errors 
            });
            return;
        }
        
        // Continue execution
        var result = await next();
        
        // Post-execution processing
        _logger.LogInformation("Action executed successfully");
    }
}

// Register filter globally
public class Startup {
    public void ConfigureServices(IServiceCollection services) {
        services.AddControllers(options => {
            options.Filters.Add<Sphere10ValidationFilter>();
        });
    }
}
```

### HTML Processing & Generation

```csharp
using Sphere10.Framework.Web.AspNetCore;
using Tools;  // HtmlTool namespace

// Parse HTML
string html = "<html><body><p>Hello</p></body></html>";
var document = HtmlTool.ParseHtml(html);

// Find elements
var paragraphs = document.GetElementsByTagName("p");
foreach (var p in paragraphs) {
    Console.WriteLine(p.InnerText);  // "Hello"
}

// Generate HTML
var builder = new HtmlBuilder();
builder.StartTag("div").AddClass("container");
{
    builder.StartTag("h1").AddAttribute("id", "title");
    builder.Text("Welcome");
    builder.EndTag();
    
    builder.StartTag("p");
    builder.Text("This is a paragraph");
    builder.EndTag();
}
builder.EndTag();

string generatedHtml = builder.ToString();
// Output: <div class="container"><h1 id="title">Welcome</h1><p>This is a paragraph</p></div>
```

### XML Processing

```csharp
using Sphere10.Framework.Web.AspNetCore;
using Tools;  // XmlTool namespace

// Parse XML
string xml = @"
<root>
    <item id='1'>
        <name>Product 1</name>
        <price>29.99</price>
    </item>
    <item id='2'>
        <name>Product 2</name>
        <price>49.99</price>
    </item>
</root>";

var xmlDoc = XmlTool.ParseXml(xml);

// Query elements
var items = xmlDoc.GetElementsByTagName("item");
foreach (var item in items) {
    var id = item.GetAttribute("id");
    var name = item.GetElementsByTagName("name")[0].InnerText;
    var price = item.GetElementsByTagName("price")[0].InnerText;
    
    Console.WriteLine($"[{id}] {name}: ${price}");
}

// Generate XML
var xmlBuilder = new XmlBuilder("catalog");
xmlBuilder.StartElement("products");
{
    xmlBuilder.StartElement("product").AddAttribute("id", "1");
    {
        xmlBuilder.Element("name", "Widget");
        xmlBuilder.Element("price", "19.99");
    }
    xmlBuilder.EndElement();  // product
}
xmlBuilder.EndElement();  // products

string generatedXml = xmlBuilder.ToString();
```

### Form Handling & Validation

```csharp
using Sphere10.Framework.Web.AspNetCore;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase {
    [HttpPost]
    public IActionResult CreateProduct([FromBody] CreateProductRequest request) {
        // Model validation happens automatically
        if (!ModelState.IsValid) {
            return BadRequest(ModelState);
        }
        
        // Validate business rules
        if (request.Price < 0) {
            return BadRequest(new { 
                error = "Price must be positive",
                field = "Price"
            });
        }
        
        // Process form
        var product = new Product {
            Name = request.Name,
            Price = request.Price,
            Description = request.Description
        };
        
        // Save and return
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
    
    [HttpGet("{id}")]
    public IActionResult GetProduct(int id) {
        var product = // ... fetch from database
        if (product == null) {
            return NotFound();
        }
        
        return Ok(product);
    }
}

public class CreateProductRequest {
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }
    
    [Required]
    [Range(0.01, 99999.99)]
    public decimal Price { get; set; }
    
    [StringLength(500)]
    public string Description { get; set; }
}
```

### Sitemap Generation

```csharp
using Sphere10.Framework.Web.AspNetCore;
using Tools;  // SitemapTool namespace

[ApiController]
[Route("")]
public class SitemapController : ControllerBase {
    [HttpGet("sitemap.xml")]
    [Produces("application/xml")]
    public IActionResult GetSitemap() {
        var sitemap = new SitemapBuilder("https://example.com");
        
        // Add pages
        sitemap.AddUrl("/", ChangeFrequency.Weekly, 1.0);
        sitemap.AddUrl("/about", ChangeFrequency.Monthly, 0.8);
        sitemap.AddUrl("/products", ChangeFrequency.Daily, 0.9);
        
        // Add dynamic product URLs
        var products = // ... fetch from database
        foreach (var product in products) {
            sitemap.AddUrl(
                $"/products/{product.Id}",
                ChangeFrequency.Weekly,
                0.7,
                product.UpdatedDate);
        }
        
        var sitemapXml = sitemap.GenerateXml();
        return Content(sitemapXml, "application/xml");
    }
    
    [HttpGet("sitemap-index.xml")]
    [Produces("application/xml")]
    public IActionResult GetSitemapIndex() {
        // For large sites with multiple sitemaps
        var index = new SitemapIndexBuilder("https://example.com");
        
        index.AddSitemapUrl("/sitemap-products.xml", DateTime.UtcNow);
        index.AddSitemapUrl("/sitemap-posts.xml", DateTime.UtcNow);
        index.AddSitemapUrl("/sitemap-static.xml", DateTime.UtcNow);
        
        var indexXml = index.GenerateXml();
        return Content(indexXml, "application/xml");
    }
}
```

### Error Handling & Exception Filters

```csharp
using Sphere10.Framework.Web.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class Sphere10ExceptionFilter : IExceptionFilter {
    private readonly ILogger<Sphere10ExceptionFilter> _logger;
    
    public Sphere10ExceptionFilter(ILogger<Sphere10ExceptionFilter> logger) {
        _logger = logger;
    }
    
    public void OnException(ExceptionContext context) {
        var exception = context.Exception;
        
        _logger.LogError(exception, "Unhandled exception: {ExceptionType}", 
            exception.GetType().Name);
        
        // Map exceptions to HTTP responses
        var response = exception switch {
            ArgumentException => new { 
                status = StatusCodes.Status400BadRequest,
                message = "Invalid argument",
                error = exception.Message
            },
            KeyNotFoundException => new {
                status = StatusCodes.Status404NotFound,
                message = "Resource not found",
                error = exception.Message
            },
            _ => new {
                status = StatusCodes.Status500InternalServerError,
                message = "Internal server error",
                error = context.HttpContext.Request.Host.ToString()
            }
        };
        
        context.Result = new ObjectResult(response) {
            StatusCode = (int?)response.GetType().GetProperty("status")?.GetValue(response)
        };
        
        context.ExceptionHandled = true;
    }
}

// Register
builder.Services.AddControllers(options => {
    options.Filters.Add<Sphere10ExceptionFilter>();
});
```

## 🏗️ Architecture & Modules

**Middleware Pipeline**: Custom middleware components
- Request/response logging
- Error handling
- Response compression
- CORS support

**Controller Support**: Base controller classes
- Sphere10Controller: Enhanced controller base
- API controller conventions
- Result mapping utilities

**HTML Processing**: HTML utilities
- HTML parsing and generation
- DOM manipulation
- Tag builder API

**XML Processing**: XML utilities
- XML parsing and generation
- XPath queries
- Namespace support

**Routing**: Enhanced routing
- Attribute-based conventions
- Route templates
- Constraint validation

**Sitemap Generation**: SEO utilities
- Sitemap generation
- Sitemap indexes
- Change frequency tracking

## 📦 Dependencies

- **Sphere10 Framework**: Core framework
- **Sphere10.Framework.NETCore**: .NET Core utilities
- **Microsoft.AspNetCore.App**: ASP.NET Core runtime (.NET built-in)
- **Microsoft.AspNetCore.Mvc**: Controller and view support
- **System.Xml.XPath**: XPath support

## ⚠️ Best Practices

- **Async all the way**: Use async/await throughout request handlers
- **Error handling**: Implement proper exception filters and handlers
- **Validation**: Validate input at multiple levels
- **Caching**: Cache static content and expensive operations
- **Security**: Use HTTPS, validate input, implement CSRF protection
- **Logging**: Log request/response for debugging
- **Performance**: Monitor request times and optimize slow endpoints
- **API versioning**: Use URL-based or header-based versioning

## ✅ Status & Compatibility

- **Maturity**: Production-ready for web applications and APIs
- **.NET Target**: .NET 8.0+ (primary), .NET 6.0+ (compatible)
- **Performance**: Optimized for high-throughput request processing
- **Scalability**: Designed for distributed deployments with load balancing

## 📖 Related Projects

- [Sphere10 Framework](../Sphere10 Framework) - Core framework
- [Sphere10.Framework.NETCore](../Sphere10.Framework.NETCore) - .NET Core integration
- [Sphere10.Framework.Data](../Sphere10.Framework.Data) - Data access for web applications
- [Sphere10.Framework.DApp.Presentation](../../blackhole/Sphere10.Framework.DApp.Presentation) - UI components

## ⚖️ License

Distributed under the **MIT NON-AI License**.

See the LICENSE file for full details. More information: [Sphere10 NON-AI-MIT License](https://sphere10.com/legal/NON-AI-MIT)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

---

**Version**: 2.0+



