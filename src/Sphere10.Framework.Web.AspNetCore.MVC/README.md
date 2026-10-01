# Sphere10.Framework.Web.AspNetCore.MVC

ASP.NET Core MVC extensions for Sphere10 Framework on .NET 10. This library references the shared [AspNetCore](../Sphere10.Framework.Web.AspNetCore/README.md) integration and contains the MVC-specific APIs previously included there.

- Controller/view rendering, URL, HTML helper, select-list, and model-state extensions.
- `BootstrapFormScope<TModel>`, `FormModelBase`, `FormResult`, and form options.
- `CleanupAttribute`, `FilterFactoryBase<TFilter>`, `FormActionAttribute`, and `CustomRouteAttribute`.
- `XmlResult` and the Windows-only `ImageResult` (System.Drawing).

Import `Sphere10.Framework.Web.AspNetCore.MVC` for moved types and extension methods. Shared HTTP/hosting types remain in `Sphere10.Framework.Web.AspNetCore`, and framework-independent models now use `Sphere10.Framework.Web`.

```csharp
using Microsoft.AspNetCore.Mvc;
using Sphere10.Framework.Web;
using Sphere10.Framework.Web.AspNetCore.MVC;

public class SitemapController : Controller {
    [HttpGet("sitemap.xml")]
    public IActionResult Index() {
        var sitemap = new SitemapXml();
        sitemap.Add("https://example.com/");
        return new XmlResult(sitemap);
    }
}
```

Enum dropdown lists use `Tools.Web.Mvc.ToSelectList<TEnum>(selectedItem, sort)` (formerly `Tools.Web.AspNetCore.ToSelectList`). `FormResultType` retains its wire values: `ShowMessage`, `Redirect`, `ReplacePage`, and `ReplaceForm`.

The existing Bootstrap forms script is embedded as `Sphere10.Framework.Web.AspNetCore.MVC.Forms.hydrogen-bootstrap-forms-1.0.0.js` in this assembly. Applications using Bootstrap form helpers must serve/include this script and its existing jQuery/Bootstrap dependencies; the library does not add middleware automatically. Retrieve it from `typeof(FormModelBase).Assembly` using the new resource name.

The [MVC tester](../../utils/Sphere10.Framework.Utils.MvcTester/README.md) is a minimal executable host for future demonstrations.
