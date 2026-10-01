<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Web

Web models and formatting utilities for .NET 10 that can be used without an ASP.NET Core host. This is the lowest web layer: it references only `Sphere10.Framework` and has no ASP.NET Core framework reference.

## Features

| API | Purpose |
| --- | --- |
| `SitemapXml` | Build XML sitemap entries with optional modification dates, frequency and priority. |
| `Menu` | Describe a navigation tree using text, URLs, icons and child menus. |
| `UserMessage` / `UserMessageType` | Carry information, warning or error messages independently of the renderer. |
| `Tools.Web.Html` | Format values and generate legacy Animate.css class strings. |
| `Processing.ParameterAttribute` | Map enum members to request parameter names for the shared ASP.NET Core processor. |
| `JDataTable` / `JDataTableParameters` | Retained, obsolete jQuery DataTables transport models for existing integrations. |

## Installation

Use the .NET 10 SDK. Install a version available in your configured package feed:

```powershell
dotnet add package Sphere10.Framework.Web --version <version> --source <feed>
```

Replace the placeholders with a released version/feed or a local package directory and its exact version. This checkout does not establish whether the new package has been published. [Package validation](../../scripts/package-consumers/README.md) builds a complete local feed and reports its unique validation version.

For development directly against the checkout, run these commands from the repository root. They create a sibling console project and reference the source:

```powershell
dotnet new console --name WebModelsDemo --output ../WebModelsDemo --framework net10.0
dotnet add ../WebModelsDemo/WebModelsDemo.csproj reference src/Sphere10.Framework.Web/Sphere10.Framework.Web.csproj
```

## Quick start: generate a sitemap

Replace the console project's `Program.cs` with:

```csharp
using System;
using System.Xml.Serialization;
using Sphere10.Framework.Web;

var sitemap = new SitemapXml();
sitemap.Add("https://example.com/", frequency: SitemapXml.Frequency.Weekly, priority: 1.0);
sitemap.Add("https://example.com/help", new DateTime(2026, 10, 1), SitemapXml.Frequency.Monthly, 0.5);

var xml = Tools.Xml.WriteToString(sitemap, Encoding.UTF8);
Console.WriteLine(xml);
```

```powershell
dotnet run --project ../WebModelsDemo -p:BuildRevision=0
```

`SitemapXml` serializes as the sitemap `urlset`/`url` structure. Optional fields are omitted when unset. URLs are dictionary keys: check `HasNode(url)` before adding a duplicate. This model does not crawl a site or serve an HTTP endpoint; use the [MVC `XmlResult` example](../Sphere10.Framework.Web.AspNetCore.MVC/README.md#quick-start-serve-a-sitemap) to expose it from a controller.

## Navigation and user messages

These are data models; your application renders the links and messages:

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

`Menu` is the lightweight web navigation model. ApplicationBlock menus and screen activation belong to [Sphere10.Framework.Application.UI](../Sphere10.Framework.Application/README.md), with platform adapters in WinForms and Blazor.

## Formatting and animation classes

```csharp
using Sphere10.Framework.Web.AnimateCss;

var percentage = Tools.Web.Html.Percent(0.25m);
var displayValue = Tools.Web.Html.Beautify(123.4500m);
var animation = Tools.Web.Html.AnimationClass(Animation.fadeIn, AnimationDelay.Seconds_0_0);
// animation: "animated fadeIn "
```

Formatting follows the current culture. `Beautify` formats a value; it does not HTML-encode it. Let Razor or your renderer encode displayed text. Animation helpers return class names only; applications supply compatible Animate.css styles and any delay classes. No CSS or JavaScript is installed by this library.

Request parameter metadata uses a separate namespace so it cannot shadow Blazor's component `[Parameter]`:

```csharp
using Sphere10.Framework.Web.Processing;

public enum SearchParameter {
	[Parameter(Name = "page")]
	Page
}
```

## Structure and dependencies

| Folder/file | Contents |
| --- | --- |
| `Sitemap/` | XML sitemap model. |
| `Presentation/` | Navigation and message models. |
| `Processing/` | Request parameter attribute. |
| `ExternalLibs/` | Animate.css enums and legacy DataTables models. |
| `HtmlTool.cs` | `Tools.Web.Html` utilities. |

The project targets `net10.0`. Its only project dependency is [Sphere10.Framework](../Sphere10.Framework/README.md). HTTP/session/hosting integrations belong to [Web.AspNetCore](../Sphere10.Framework.Web.AspNetCore/README.md); MVC and Blazor each add their own host-specific layer.

## Migration from the combined web library

These models previously lived in `Sphere10.Framework.Web.AspNetCore`. Update imports to `Sphere10.Framework.Web`, `Sphere10.Framework.Web.AnimateCss` or `Sphere10.Framework.Web.Processing` as appropriate. `Tools.Web.Html` keeps its existing name. `JDataTable` and `JDataTableParameters` remain obsolete compatibility types; new Blazor applications can use the [typed CRUD grid](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md#crud-grid-quick-start).

## Build and verification

From the repository root:

```powershell
dotnet build src/Sphere10.Framework.Web/Sphere10.Framework.Web.csproj -p:BuildRevision=0
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Tests -p:BuildRevision=0
```

The shared [web tests](../../tests/Sphere10.Framework.Web.AspNetCore.Tests) cover sitemap serialization through MVC. The [external package consumers](../../scripts/package-consumers/README.md) also verify that the pure Web package restores without ASP.NET Core or desktop UI dependencies. `BuildRevision=0` avoids changing the local build counter.

## Related projects

- [Shared ASP.NET Core integration](../Sphere10.Framework.Web.AspNetCore/README.md)
- [MVC controllers, results and forms](../Sphere10.Framework.Web.AspNetCore.MVC/README.md)
- [Blazor application framework and CRUD grid](../Sphere10.Framework.Web.AspNetCore.Blazor/README.md)
- [Framework package/build guide](../../README.md#building-and-publishing-packages)
