<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Web.AspNetCore.MVC

Controller, Razor-view and form extensions for ASP.NET Core MVC on .NET 10. This library adds MVC-specific APIs to the shared [ASP.NET Core integration](../Sphere10.Framework.Web.AspNetCore/README.md); it does not register controllers, routes or client scripts automatically.

## Features

| Area | APIs |
| --- | --- |
| HTTP results | `XmlResult` for serialized objects or existing XML; Windows-only `ImageResult` for System.Drawing images. |
| Razor views | Bootstrap input/form helpers, enum select lists and `Controller.RenderViewAsync`. |
| Forms | `FormModelBase`, `BootstrapFormScope<TModel>`, `FormScopeOptions`, `FormResult`. |
| Validation and filtering | Model-state extensions, `CleanupAttribute`, `FilterFactoryBase<TFilter>`, `FormActionAttribute`. |
| Routing | `CustomRouteAttribute` and URL helpers. |

## Installation

Use the .NET 10 SDK and a `Microsoft.NET.Sdk.Web` host. Install an available version from your package feed:

```powershell
dotnet add package Sphere10.Framework.Web.AspNetCore.MVC --version <version> --source <feed>
```

Replace the placeholders with your exact version and feed. Publication of the new package is not assumed. [Package validation](../../scripts/package-consumers/README.md) generates a complete local feed and reports a unique validation version.

For the source-reference examples below, run from the repository root:

```powershell
dotnet new web --name MvcDemo --output ../MvcDemo --framework net10.0
dotnet add ../MvcDemo/MvcDemo.csproj reference src/Sphere10.Framework.Web.AspNetCore.MVC/Sphere10.Framework.Web.AspNetCore.MVC.csproj
```

## Quick start: serve a sitemap

Replace `MvcDemo/Program.cs` with:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.UseStaticFiles();
app.MapControllers();
app.Run();
```

Add `Controllers/SitemapController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Sphere10.Framework.Web;
using Sphere10.Framework.Web.AspNetCore.MVC;

namespace MvcDemo.Controllers;

public class SitemapController : Controller {
	[HttpGet("sitemap.xml")]
	public IActionResult Index() {
		var sitemap = new SitemapXml();
		sitemap.Add("https://example.com/", frequency: SitemapXml.Frequency.Weekly, priority: 1.0);
		return new XmlResult(sitemap);
	}
}
```

```powershell
dotnet run --project ../MvcDemo -p:BuildRevision=0
```

Open `/sitemap.xml` at the address printed by the host. `XmlResult` returns `application/xml`; its object overload serializes with the framework XML tool. `new XmlResult(xml, statusCode)` returns existing XML, and `new XmlResult(statusCode)` returns an empty body with that status. XML result helpers do not require starting the Application framework. Add the registration/start calls from the [shared hosting guide](../Sphere10.Framework.Web.AspNetCore/README.md#quick-start-framework-lifecycle-and-sessions) when your app needs framework modules and lifecycle services.

## Enum dropdowns

```csharp
using System;
using Sphere10.Framework;

var weekdays = Tools.Web.Mvc.ToSelectList<DayOfWeek>(DayOfWeek.Monday, SortDirection.Ascending);
```

Pass the returned `SelectList` to `Html.DropDownListFor` or `Html.BootstrapDropDownListFor`. Option values use enum names; labels use `Tools.Enums.GetDescription`; the optional sort orders labels while preserving the selected value. `BootstrapDropDownListFor` enables Choices.js by default: provide that client library or pass `useChoicesJS: false` for a normal select element.

## Bootstrap AJAX form example

This optional example extends the MVC host above. It needs application-provided jQuery and Bootstrap assets. Supply compatible files at `wwwroot/lib/jquery/jquery.min.js`, `wwwroot/lib/bootstrap/bootstrap.bundle.min.js` and `wwwroot/lib/bootstrap/bootstrap.min.css`. The forms script uses the Bootstrap jQuery tooltip/popover APIs; load jQuery before Bootstrap. The library does not download or serve these dependencies.

### Serve the embedded forms script

Add the following imports at the top of `Program.cs`, then the endpoint before `app.Run()`:

```csharp
using Microsoft.AspNetCore.Http;
using Sphere10.Framework;
using Sphere10.Framework.Web.AspNetCore.MVC;

app.MapGet("/sphere10/forms.js", () => {
	var script = typeof(FormModelBase).Assembly.GetManifestResourceStream(
		"Sphere10.Framework.Web.AspNetCore.MVC.Forms.hydrogen-bootstrap-forms-1.0.0.js"
	);
	Guard.Ensure(script != null, "The embedded forms script was not found.");
	return Results.Stream(script, "text/javascript");
});
```

The response owns the stream. Load this script in the page head before the generated form initialization runs, without `defer` or `async`.

### Define the model and controller

Save `Models/ContactForm.cs`:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Sphere10.Framework.Web.AspNetCore.MVC;

namespace MvcDemo.Models;

public sealed class ContactForm : FormModelBase {
	[Required]
	[DisplayName("Your name")]
	public string Name { get; set; } = string.Empty;
}
```

Save `Controllers/ContactController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sphere10.Framework.Web.AspNetCore.MVC;
using MvcDemo.Models;

namespace MvcDemo.Controllers;

[Route("contact")]
public class ContactController : Controller {
	[HttpGet]
	public IActionResult Index() => View(new ContactForm());

	[HttpPost]
	[ValidateAntiForgeryToken]
	public IActionResult Submit(ContactForm model) {
		var result = new FormResult {
			Result = ModelState.IsValid,
			ResultType = FormResultType.ShowMessage,
			Message = ModelState.IsValid ? "Demo submission accepted; no data was stored." : "Enter your name."
		};
		return Content(JsonConvert.SerializeObject(result), "application/json");
	}
}
```

`FormResult` uses Newtonsoft JSON attributes and enum wire names. Explicit serialization above preserves the script's expected `result`, `message` and `type: "message"` fields without requiring a separate MVC Newtonsoft integration package. Returning it through a default System.Text.Json formatter is not equivalent. Other result types retain `redirect`, `replace_page` and `replace_form` wire values.

### Render the form

Save `Views/Contact/Index.cshtml`:

```razor
@using Microsoft.AspNetCore.Mvc.Rendering
@using Sphere10.Framework.Web.AspNetCore.MVC
@model MvcDemo.Models.ContactForm
@{
	Layout = null;
}
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="utf-8" />
	<title>Contact form demo</title>
	<link rel="stylesheet" href="/lib/bootstrap/bootstrap.min.css" />
	<script src="/lib/jquery/jquery.min.js"></script>
	<script src="/lib/bootstrap/bootstrap.bundle.min.js"></script>
	<script src="/sphere10/forms.js"></script>
</head>
<body>
	<main class="container py-4">
		@using (Html.BeginBootstrapForm("/contact", Model, options: FormScopeOptions.UseLoadingOverlay | FormScopeOptions.AddAntiForgeryToken)) {
			@Html.BootstrapLabelFor(model => model.Name)
			@Html.BootstrapTextBoxFor(model => model.Name)
			@Html.BootstrapFormButton(Model, "Submit")
		}
	</main>
</body>
</html>
```

Restart the host and open `/contact`. The scope emits the form ID, submit count, antiforgery input, result marker and initialization call. The example retains field values after submission. Default form options also enable `BotProtect`, which requires the explicit-URL overload; the action/controller overload cannot be used with that option. `BotProtect` encodes the form action and does not replace server-side validation or antiforgery checks.

For custom fragments, `Controller.RenderViewAsync(viewName, model, partial)` renders a configured MVC view into a string. It requires the MVC view services and an existing view. The current helper returns a diagnostic string when no view is found, so inspect that behavior before using it for generated content.

## Structure and dependencies

| Folder/file | Contents |
| --- | --- |
| `Extensions/` | Controller, HTML, URL, select-list and model-state extensions. |
| `Forms/` | Form models/options/scope/result and embedded legacy JavaScript. |
| `Filters/` and `Routing/` | Action filters and route attribute. |
| `XML/` | `XmlResult`. |
| `Processing/` | `ImageResult`. |
| `MvcTool.cs` | `Tools.Web.Mvc.ToSelectList`. |

The project targets `net10.0`, references `Microsoft.AspNetCore.App`, and depends on shared [Web.AspNetCore](../Sphere10.Framework.Web.AspNetCore/README.md), [Drawing](../Sphere10.Framework.Drawing/README.md) and Newtonsoft.Json. `ImageResult` is marked Windows-only and disposes the supplied `System.Drawing.Image` after writing it; do not reuse that image afterward. The pure [Web](../Sphere10.Framework.Web/README.md) layer remains available when these host/drawing dependencies are unnecessary.

## Migration from the combined library

Import `Sphere10.Framework.Web.AspNetCore.MVC` for moved controllers/view helpers, forms, filters, routes and result types. Use `Tools.Web.Mvc.ToSelectList` instead of `Tools.Web.AspNetCore.ToSelectList`. Shared hosting/HTTP types retain their previous namespaces; sitemap/navigation/message models now use `Sphere10.Framework.Web`.

The forms script now resides in `typeof(FormModelBase).Assembly` under the exact MVC resource name shown above. Update old assembly/resource lookups. It remains an embedded resource, not an automatically published static-web asset.

## Build and verification

From the repository root:

```powershell
dotnet build src/Sphere10.Framework.Web.AspNetCore.MVC/Sphere10.Framework.Web.AspNetCore.MVC.csproj -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Tests -p:BuildRevision=0
```

The [regression suite](../../tests/Sphere10.Framework.Web.AspNetCore.Tests) checks XML/status handling, sitemap output, enum selection/sorting and the embedded form resource. The [MVC tester](../../utils/Sphere10.Framework.Utils.MvcTester/README.md) is a minimal host for additional controller/view demonstrations; it does not already contain the optional contact form above. [Package validation](../../scripts/package-consumers/README.md) checks an external MVC consumer and HTTP XML output. `BuildRevision=0` preserves the local build counter.

## Related projects

- [Shared ASP.NET Core integration](../Sphere10.Framework.Web.AspNetCore/README.md)
- [Web models and formatting](../Sphere10.Framework.Web/README.md)
- [Blazor framework and CRUD grid](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md)
- [Framework package/build guide](../../README.md#building-and-publishing-packages)
