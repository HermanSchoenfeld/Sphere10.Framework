<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Web

This library provides web models and formatting helpers that need no ASP.NET Core host. Use it for sitemaps, navigation trees, user messages and display formatting in any .NET application. It is the lowest of the Framework's web layers and references only `Sphere10.Framework`. Hosting, HTTP and session integration live in [Sphere10.Framework.Web.AspNetCore](../Sphere10.Framework.Web.AspNetCore/README.md).

## Features

| API | Purpose |
| --- | --- |
| `SitemapXml` | XML sitemap model whose entries carry an optional modification date, change frequency and priority. |
| `Menu` | Navigation tree of text, URLs, icons, glyphs and child menus. |
| `UserMessage`, `UserMessageType` | Information, warning or error message that any renderer can display. |
| `Tools.Web.Html` | Display formatting and Animate.css class strings. |
| `ParameterAttribute` | Request parameter name for an enum member, read by the ASP.NET Core [query parameter helpers](../Sphere10.Framework.Web.AspNetCore/README.md#typed-query-parameters). |
| `JDataTable`, `JDataTableParameters` | Obsolete jQuery DataTables transport models, kept for existing integrations. |

## Installation

Reference the package from a project that targets .NET 10 or later:

```powershell
dotnet add package Sphere10.Framework.Web
```

Pass `--version` to match the other Sphere10.Framework packages in the application, and `--source` to restore from a local folder feed. The root README explains how to [build and publish the packages](../../README.md#building-and-publishing-packages), including to a [private folder feed](../../README.md#private-feed).

To build against the source instead, reference the project. From the repository root, these commands create a console project beside the repository for the examples below:

```powershell
dotnet new console --name WebModelsDemo --output ../WebModelsDemo --framework net10.0
dotnet add ../WebModelsDemo/WebModelsDemo.csproj reference src/Sphere10.Framework.Web/Sphere10.Framework.Web.csproj
```

## Quick start: generate a sitemap

Replace the console project's `Program.cs` with:

```csharp
using System;
using System.IO;
using System.Xml.Serialization;
using Sphere10.Framework.Web;

var sitemap = new SitemapXml();
sitemap.Add("https://example.com/", frequency: SitemapXml.Frequency.Weekly, priority: 1.0);
sitemap.Add("https://example.com/help", new DateTime(2026, 10, 1), SitemapXml.Frequency.Monthly, 0.5);

// Declare the sitemap namespace as the default namespace, without the xsi and xsd prefixes.
var namespaces = new XmlSerializerNamespaces();
namespaces.Add(string.Empty, "http://www.sitemaps.org/schemas/sitemap/0.9");

var path = Path.Combine(AppContext.BaseDirectory, "sitemap.xml");
using (var file = File.Create(path))
	new XmlSerializer(typeof(SitemapXml)).Serialize(file, sitemap, namespaces);
Console.WriteLine(File.ReadAllText(path));
```

Run it from the repository root:

```powershell
dotnet run --project ../WebModelsDemo -p:BuildRevision=0
```

The program writes `sitemap.xml` beside its binaries and prints it. `SitemapXml` serializes as the standard `urlset` of `url` entries with `loc`, `lastmod`, `changefreq` and `priority` elements. It omits the optional elements that were not set and writes `lastmod` as a date without a time.

Each URL is a dictionary key, so `Add` throws `ArgumentException` for a URL that is already present. Call `HasNode(url)` first when URLs can repeat. The model does not crawl a site or serve an endpoint. The [MVC `XmlResult` example](../Sphere10.Framework.Web.AspNetCore.MVC/README.md#quick-start-serve-a-sitemap) serves it from a controller, and `XmlResult` serializes it with `Tools.Xml` from `Sphere10.Framework.Data`.

## Navigation and user messages

`Menu` and `UserMessage` are data models, and your application renders them:

```csharp
using Sphere10.Framework.Web;

var navigation = new Menu("Documentation", "/docs") {
	SubMenus = new[] {
		new Menu("Getting started", "/docs/start"),
		new Menu("API reference", "/docs/api")
	}
};
var message = new UserMessage("Your preferences were saved.", UserMessageType.Information);
```

The parameterless constructor gives a `Menu` the URL `#` and an empty `SubMenus` array. The optional third argument of the other constructor sets `Glyph`, and `Icon` is a separate property. The class serializes to XML as nested `<Menu text="..." url="..." icon="..." glyph="...">` elements, so a navigation tree can be stored in an XML file. The ASP.NET Core layer adds `HttpContext.GetUserMessages()` for [messages that belong to the current request](../Sphere10.Framework.Web.AspNetCore/README.md#messages-for-the-current-request).

`Menu` is only a lightweight web navigation model. Application menus, blocks and screens belong to the [shared UI framework](../Sphere10.Framework.Application/README.md#shared-ui-framework), which the WinForms and Blazor libraries implement.

## Formatting and animation classes

```csharp
using Sphere10.Framework.Web.AnimateCss;

var percentage = Tools.Web.Html.Percent(0.25m);
var displayValue = Tools.Web.Html.Beautify(123.4500m); // "123.45" in an English culture
var animation = Tools.Web.Html.AnimationClass(Animation.fadeIn, AnimationDelay.Seconds_0_0);
// animation: "animated fadeIn "
```

`Percent` and `Beautify` follow the current culture. `Beautify` returns an empty string for `null`, formats dates as `yyyy-MM-dd HH:mm:ss`, writes byte arrays as upper-case hexadecimal and drops trailing zeros from numbers. It does not HTML-encode its result, so let Razor or your renderer encode the text.

`AnimationClass` returns the unprefixed Animate.css classes `animated <animation>`, followed by an `animation-delay-<n>` class in which `n` counts tenths of a second. `Seconds_0_0` adds no delay class, and the `delay` parameter defaults to `Seconds_1_0`. `RandomEntryAnimationClass` and `RandomExitAnimationClass` pick an animation from the `EntryAnimationClasses` and `ExitAnimationClasses` arrays. The library ships no CSS or JavaScript, so the application supplies compatible Animate.css styles and defines the delay classes.

## Request parameter names

`ParameterAttribute` names the request parameter that an enum member stands for. `PageParameterProcessor` and `HttpRequest.GetParameter<T>(Enum)` in the [ASP.NET Core layer](../Sphere10.Framework.Web.AspNetCore/README.md#typed-query-parameters) read it, and a member without the attribute uses its own name:

```csharp
using Sphere10.Framework.Web.Processing;

public enum SearchParameter {
	[Parameter(Name = "page")]
	Page
}
```

The attribute lives in `Sphere10.Framework.Web.Processing` rather than `Sphere10.Framework.Web`, so that a Blazor component which imports `Sphere10.Framework.Web` still resolves `[Parameter]` to the component parameter attribute.

## Structure and dependencies

| Folder or file | Contents |
| --- | --- |
| `Sitemap/` | `SitemapXml`. |
| `Presentation/` | `Menu`, `UserMessage` and `UserMessageType`. |
| `Processing/` | `ParameterAttribute`. |
| `ExternalLibs/AnimateCss/` | `Animation` and `AnimationDelay`. |
| `ExternalLibs/JQuery/` | The obsolete DataTables models. |
| `HtmlTool.cs` | `Tools.Web.Html`. |

The project targets `net10.0`, and also `net11.0` when a .NET 11 SDK builds it. Its only project reference is [Sphere10.Framework](../Sphere10.Framework/README.md), and it has no ASP.NET Core framework reference. The [MVC](../Sphere10.Framework.Web.AspNetCore.MVC/README.md) and [Blazor](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md) libraries build on the shared ASP.NET Core layer above it.

## Upgrading from the combined library

Releases up to 3.1.2 shipped these types inside `Sphere10.Framework.Web.AspNetCore`, under its namespaces. They now come from this package, which `Sphere10.Framework.Web.AspNetCore` references, so only the imports change:

| Types | Namespace |
| --- | --- |
| `SitemapXml`, `Menu`, `UserMessage`, `UserMessageType`, `JDataTable`, `JDataTableParameters` | `Sphere10.Framework.Web` |
| `Animation`, `AnimationDelay` | `Sphere10.Framework.Web.AnimateCss` |
| `ParameterAttribute` | `Sphere10.Framework.Web.Processing` |
| `Tools.Web.Html` | `Tools.Web`, unchanged |

`JDataTable` and `JDataTableParameters` are marked `[Obsolete]` and remain only for existing jQuery DataTables integrations. New Blazor applications can use the [typed CRUD grid](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md#crud-grid-quick-start) instead.

## Build and verification

From the repository root:

```powershell
dotnet build src/Sphere10.Framework.Web/Sphere10.Framework.Web.csproj -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Tests -p:BuildRevision=0
```

This library has no test project of its own. The shared [web tests](../../tests/Sphere10.Framework.Web.AspNetCore.Tests) serialize a `SitemapXml` through the MVC `XmlResult`. The Core consumer in [package validation](../../scripts/package-consumers/README.md) builds against the packed library and fails when ASP.NET Core or desktop UI packages appear in its restore graph. `-p:BuildRevision=0` stops a local build from allocating the next build number.

## Related projects

- [Shared ASP.NET Core integration](../Sphere10.Framework.Web.AspNetCore/README.md)
- [MVC results, Razor helpers and forms](../Sphere10.Framework.Web.AspNetCore.MVC/README.md)
- [Blazor application framework and CRUD grid](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md)
- [Framework package and build guide](../../README.md#building-and-publishing-packages)
