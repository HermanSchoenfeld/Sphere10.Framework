# Sphere10.Framework.Web

Web models and utilities for .NET 10 without an ASP.NET Core dependency. This library references only Sphere10.Framework and can be used in server, desktop, and browser applications.

- `SitemapXml` builds XML sitemaps.
- `Menu`, `UserMessage`, and `UserMessageType` describe navigation and messages.
- `Sphere10.Framework.Web.Processing.ParameterAttribute` names web request parameters independently of their host. The processing namespace avoids collisions with Blazor component parameter attributes.
- `JDataTable` and `JDataTableParameters` preserve the legacy jQuery DataTables transport format.
- `Tools.Web.Html` formats values and produces Animate.css class names using `Sphere10.Framework.Web.AnimateCss`.

```csharp
using Sphere10.Framework.Web;
using Sphere10.Framework.Web.AnimateCss;

var sitemap = new SitemapXml();
sitemap.Add("https://example.com/", frequency: SitemapXml.Frequency.Weekly, priority: 1.0);
var xml = Tools.Xml.WriteToString(sitemap);
var animation = Tools.Web.Html.AnimationClass(Animation.fadeIn);
```

These types previously lived in `Sphere10.Framework.Web.AspNetCore`; update their namespace imports to `Sphere10.Framework.Web` (with `.AnimateCss` for animations and `.Processing` for request parameter attributes). The `Tools.Web.Html` API is unchanged. HTTP integration belongs to [AspNetCore](../Sphere10.Framework.Web.AspNetCore/README.md), and controllers/forms belong to [MVC](../Sphere10.Framework.Web.AspNetCore.MVC/README.md).
