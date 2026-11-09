<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Web.AspNetCore.MVC

This library extends ASP.NET Core MVC applications that use Sphere10 Framework with action results, Razor HTML helpers, AJAX Bootstrap forms and action filters. It builds on the shared [ASP.NET Core integration](../Sphere10.Framework.Web.AspNetCore/README.md) and does not register controllers, routes or client scripts by itself.

## Features

| Area | APIs |
| --- | --- |
| Action results | `XmlResult` for serialized objects or existing XML, and the Windows-only `ImageResult` for `System.Drawing` images. |
| Views | Bootstrap HTML helpers, enum select lists and `Controller.RenderViewAsync`. |
| AJAX forms | `FormModelBase`, `BootstrapFormScope<TModel>`, `FormScopeOptions`, `FormResult` and the embedded forms script. |
| Validation and filters | `ModelState` extensions, `CleanupAttribute`, `FilterFactoryBase<TFilter>` and `FormActionAttribute`. |
| Routing and URLs | `UrlExtensions.Content` and `CustomRouteAttribute`. |

## Installation

Reference the package from an ASP.NET Core project that targets .NET 10 or later:

```powershell
dotnet add package Sphere10.Framework.Web.AspNetCore.MVC
```

Pass `--version` to match the other Sphere10.Framework packages in the application, and `--source` to restore from a local folder feed. The root README explains how to [build and publish the packages](../../README.md#building-and-publishing-packages), including to a [private folder feed](../../README.md#private-feed).

To build against the source instead, reference the project. From the repository root, these commands create the host for the examples below, beside the repository:

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

Run the host from the repository root, then open `/sitemap.xml` at the address that it prints:

```powershell
dotnet run --project ../MvcDemo -p:BuildRevision=0
```

`XmlResult` is a `ContentResult` with the `application/xml` content type and status 200 unless you pass another status code:

| Constructor | Response body |
| --- | --- |
| `XmlResult(object)`, `XmlResult(object, statusCode)` | The object serialized by `Tools.Xml.WriteToString`, with the namespace of its `[XmlRoot]` as the default namespace. |
| `XmlResult(string)`, `XmlResult(string, statusCode)` | The string as given, so a string argument is never serialized. |
| `XmlResult()`, `XmlResult(statusCode)` | Empty. |

XML results do not need the Framework to be started. Add the registration and start calls from the [shared hosting quick start](../Sphere10.Framework.Web.AspNetCore/README.md#quick-start-framework-lifecycle-and-sessions) when the application needs Framework modules and lifecycle services.

## Select lists

```csharp
using System;
using Sphere10.Framework;

var weekdays = Tools.Web.Mvc.ToSelectList<DayOfWeek>(DayOfWeek.Monday, SortDirection.Ascending);
```

Pass the returned `SelectList` to `Html.DropDownListFor` or `Html.BootstrapDropDownListFor`. Each option's value is the enum member's name, and its label comes from `Tools.Enums.GetDescription`, which reads a `[Description]` attribute and falls back to the name. The optional sort orders the labels and keeps the selected value. `ToSelectList(Type, ...)` takes the enum type at run time instead.

For other collections, the `ToSelectList(text, value, defaultOption, selected)` extension on `IEnumerable<T>` builds `SelectListItem` objects from delegates. A `defaultOption` becomes the first item, with the value `-1`.

## Bootstrap AJAX forms

`BootstrapFormScope<TModel>` renders a form that the embedded `hydrogen-bootstrap-forms-1.0.0.js` script submits with jQuery AJAX. The action answers with a `FormResult`, and the script then shows a message, redirects, or replaces the page or the form. The steps below add a contact form to the sitemap host.

### Prerequisites

The application supplies the client libraries, because this package neither downloads nor serves them:

- jQuery, loaded before Bootstrap. The script calls Bootstrap's jQuery `tooltip` and `popover` plugins, which Bootstrap registers only when jQuery is present.
- Bootstrap 5, whose `data-bs-*` attributes and `form-control`, `form-select` and `btn-close` classes the helpers and the script use.
- Choices.js for any drop-down that `BootstrapDropDownListFor` renders with its default `useChoicesJS: true`. Without the library, the form's initialization stops at that element and the form is not converted to AJAX, so pass `useChoicesJS: false` for a plain select element.
- Font Awesome, optionally, for the `fa` icons in result alerts.

The example's view loads them from `wwwroot/lib/jquery/jquery.min.js`, `wwwroot/lib/bootstrap/bootstrap.bundle.min.js` and `wwwroot/lib/bootstrap/bootstrap.min.css`.

### Serve the embedded script

The script is an embedded resource of the MVC assembly, not a static web asset. Add these imports at the top of `Program.cs` and the endpoint before `app.Run()`:

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

`Results.Stream` disposes the resource stream once the response is written. Load the script in the page head, without `defer` or `async`, because each form calls the script's `F_Init` function from an inline script.

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

`FormModelBase` gives each model a random, DOM-safe `ID`, which becomes the form's element ID, and a `SubmitCount` that the script increments on every submission.

Save `Controllers/ContactController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using MvcDemo.Models;
using Newtonsoft.Json;
using Sphere10.Framework.Web.AspNetCore.MVC;

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
			Message = ModelState.IsValid ? "Demo submission accepted. No data was stored." : "Enter your name."
		};
		return Content(JsonConvert.SerializeObject(result), "application/json");
	}
}
```

`FormResult` carries Newtonsoft.Json attributes, so the action serializes it with `JsonConvert` to produce the `result`, `message`, `type`, `url` and `content` fields that the script reads. ASP.NET Core's default System.Text.Json output would name the type field `resultType` and write it as a number, so returning `Json(result)` does not work. No MVC Newtonsoft integration package is needed, because this library already references Newtonsoft.Json.

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

Stop the host, run it again and open `/contact`. Submitting the form posts it with AJAX and shows the action's message at the end of the form. The field keeps its value after submission, because the options omit `ClearOnSuccess`.

The scope writes the `<form>` tag with the model's `ID`, the hidden `ID` and `SubmitCount` fields, the antiforgery token and, with `UseLoadingOverlay`, a loading overlay. Disposing the scope writes a `{formId}_message_marker` element, the `F_Init` call and the closing tag.

The embedded script shows each result in an alert after that marker, and later results replace the alert. A page can instead supply an element whose ID is `{formId}_result`, which the script replaces with the alert. Call `MessageMarker()` to place the scope's marker earlier in the form, and the scope then omits it on disposal.

`BootstrapFormScope` implements both `Dispose` and `DisposeAsync`. `DisposeAsync` writes the closing markup with asynchronous writes only.

### Results and options

| `FormResultType` | `type` value | Script behavior |
| --- | --- | --- |
| `ShowMessage` | `message` | Shows `message` in a success alert when `result` is true and in a danger alert otherwise. |
| `Redirect` | `redirect` | Navigates to `url`. |
| `ReplacePage` | `replace_page` | Replaces the whole document with `content`. |
| `ReplaceForm` | `replace_form` | Replaces the form with `content`, then shows `message` when it is set. |

| `FormScopeOptions` flag | Effect |
| --- | --- |
| `UseLoadingOverlay` | Renders an overlay inside the form tag, which the script shows during each submission to block input. Without the flag, the scope renders no overlay and the script shows none. |
| `ClearOnSuccess` | Restores the form's original markup after a successful result. |
| `OmitFormTag` | Writes only the marker and the `F_Init` call, so the view supplies a `<form>` element whose ID is the model's `ID`. |
| `AddAntiForgeryToken` | Writes an antiforgery token inside the form. |
| `BotProtect` | Base64-encodes the action URL, which the script decodes before posting. It requires the URL overload of `BeginBootstrapForm`, because the action and controller overload throws with this flag. |
| `UseGet` | Declares `get` on the form tag, and the script sends the fields in the query string of a GET request instead of posting them. The action must accept GET. |
| `SmallerBlockerOverlay` | Sizes the overlay at 90% of the form instead of 100%. |
| `Default` | `UseLoadingOverlay`, `AddAntiForgeryToken`, `BotProtect` and `ClearOnSuccess`. |
| `DefaultRetainInput` | `Default` without `ClearOnSuccess`. |

`BotProtect` deters generic form bots only. It does not replace server-side validation or the antiforgery check.

## Bootstrap input helpers

The `Bootstrap*For` helpers wrap the MVC `*For` helpers with Bootstrap 5 markup. Each takes its label or placeholder text from the member's `[DisplayName]` attribute and appends ` *` for a `[Required]` member. When `ModelState` holds errors for the member, the input gets the `is-invalid` class and a Bootstrap tooltip that lists the errors.

| Helper | Renders |
| --- | --- |
| `BootstrapLabelFor` | A label. |
| `BootstrapTextBoxFor`, `BootstrapPasswordFor` | A `form-control` input. The text box can be read-only. |
| `BootstrapTextAreaFor` | A `form-control` text area whose height follows `approxRows`. |
| `BootstrapDropDownListFor` | A `form-select` element with the display name as its empty option, enhanced by Choices.js unless `useChoicesJS` is `false`. |
| `BootstrapCheckBoxAndLabelFor` | A `form-check` check box and its label. |
| `BootstrapFormButton` | The submit button of a form scope, with a spinner that the script shows during submission. |
| `ValidationMessageForEx` | A validation message styled as a Bootstrap warning badge, with the `badge bg-warning text-dark` classes, optionally floated to the end. |

## Render a view to a string

`Controller.RenderViewAsync(viewName, model, partial)` renders an MVC view into a string, for example as the `content` of a `ReplaceForm` result. An empty `viewName` selects the current action's view, and `partial: true` renders it as a partial view. When the view engine finds no view, the method returns the text `A view with the name <viewName> could not be found` instead of throwing, so check for a missing view before you send the result.

## Images

`ImageResult` writes a `System.Drawing.Image` to the response as JPEG, which is the default, or as PNG or GIF, and throws for any other `ImageFormat`. It is marked Windows-only and disposes the image after writing, so do not reuse the image. MVC executes it asynchronously: it encodes the image in memory, sets the content length and copies the bytes to the response body with asynchronous writes, so `AllowSynchronousIO` is not needed. Only a direct call to the synchronous `ExecuteResult` saves straight to the response body.

## Filters, routing and URLs

| API | Behavior |
| --- | --- |
| `CleanupAttribute` | Gives the action a `ViewBag.Cleanup` list of `Action` delegates and runs them after the result executes. A failing delegate is logged through `SystemLog`, and the next one still runs. |
| `FilterFactoryBase<TFilter>` | Base attribute that resolves `TFilter` from the request services on every request. Register `TFilter` in the service collection, because an unregistered filter throws `InvalidOperationException`. |
| `FormActionAttribute` | Sets `ViewData[FormActionAttribute.OmitFormTag]` to `true` for the view to read. Nothing in the library reads the flag. |
| `ModelStateDictionaryExtensions` | Adds the error messages of a `Result`, or a formatted message, as model errors, and removes several keys at once. |
| `UrlExtensions.Content(path, toAbsolute)` | Extends `IUrlHelper`, so views call it as `Url.Content(path, true)`. It resolves a content path and, with `toAbsolute`, makes it absolute for the helper's current request. When the host carries no port, the absolute URL omits it. |
| `CustomRouteAttribute` | Declares routes on a controller class. `CustomRouteAttribute.RegisterCustomRoutesFromAssembly(app, assembly)` maps each attribute of every controller in the assembly as a conventional controller route, ordered by `Priority`, with the controller and `DefaultAction` as defaults. A route may not contain `{controller}`. Attribute routing such as `[Route]` and `[HttpGet("...")]` remains the usual ASP.NET Core choice. |

## Structure and dependencies

| Folder or file | Contents |
| --- | --- |
| `Extensions/` | Controller, HTML helper, URL, select-list and model-state extensions. |
| `Forms/` | Form model, options, scope and result types, and the embedded forms script. |
| `Filters/` | `CleanupAttribute`, `FilterFactoryBase<TFilter>` and `FormActionAttribute`. |
| `Routing/` | `CustomRouteAttribute`. |
| `XML/` | `XmlResult`. |
| `Processing/` | `ImageResult`. |
| `MvcTool.cs` | `Tools.Web.Mvc.ToSelectList`. |

The project targets `net10.0`, and also `net11.0` when a .NET 11 SDK builds it. It has a `Microsoft.AspNetCore.App` framework reference, project references to the shared [Web.AspNetCore](../Sphere10.Framework.Web.AspNetCore/README.md) layer and to [Drawing](../Sphere10.Framework.Drawing/README.md), and a Newtonsoft.Json package reference for the form types. The Drawing reference serves only `ImageResult`. The host-independent [Web](../Sphere10.Framework.Web/README.md) library suffices when an application needs only the web models.

## Upgrading from the combined library

Releases up to 3.1.2 shipped these types inside `Sphere10.Framework.Web.AspNetCore`, in the `Sphere10.Framework.Web.AspNetCore` namespace. Reference this package and import `Sphere10.Framework.Web.AspNetCore.MVC` for the controller and view helpers, forms, filters, routing and results. Replace `Tools.Web.AspNetCore.ToSelectList` with `Tools.Web.Mvc.ToSelectList`. The hosting and HTTP types keep their namespaces in the shared package, and the sitemap, navigation and message models moved to `Sphere10.Framework.Web`.

The forms script is still an embedded resource rather than a static web asset. It moved from the `Sphere10.Framework.Web.AspNetCore` assembly, where its name was `Sphere10.Framework.Web.AspNetCore.Forms.hydrogen-bootstrap-forms-1.0.0.js`, to `typeof(FormModelBase).Assembly` under the name shown above.

## Build and verification

From the repository root:

```powershell
dotnet build src/Sphere10.Framework.Web.AspNetCore.MVC/Sphere10.Framework.Web.AspNetCore.MVC.csproj -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Tests -p:BuildRevision=0
```

The [shared web tests](../../tests/Sphere10.Framework.Web.AspNetCore.Tests) check `XmlResult` status codes and sitemap output, enum list selection and sorting, the embedded script's resource name, asynchronous `ImageResult` output, `FilterFactoryBase` resolution, `CustomRouteAttribute` registration, and `BootstrapFormScope` completion, form method and overlay options, including a retry after a failed write and asynchronous disposal. The [MVC tester](../../utils/Sphere10.Framework.Utils.MvcTester/README.md) is a minimal MVC host with one landing page for further controller and view demonstrations, and it does not include the contact form above. [Package validation](../../scripts/package-consumers/README.md) serves `XmlResult` output from an external consumer and checks it over HTTP. `-p:BuildRevision=0` stops a local build from allocating the next build number.

## Related projects

- [Shared ASP.NET Core integration](../Sphere10.Framework.Web.AspNetCore/README.md)
- [Web models and formatting](../Sphere10.Framework.Web/README.md)
- [Blazor application framework and CRUD grid](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md)
- [Framework package and build guide](../../README.md#building-and-publishing-packages)
