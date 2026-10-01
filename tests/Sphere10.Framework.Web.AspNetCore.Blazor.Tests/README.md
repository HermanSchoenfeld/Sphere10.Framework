# Sphere10.Framework.Web.AspNetCore.Blazor.Tests

.NET 10 NUnit tests for the restored Blazor components and their runnable gallery.

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests -p:BuildRevision=0
```

`Components` restores the archived paged, virtual and rapid table view-model fixtures and original wizard tests. `Loader` restores plugin routing, menu merging, app navigation and plugin registration coverage. The previously commented-out BlockMenu component test is active through ASP.NET Core's `HtmlRenderer`. The old TopbarMenuTests file contained no tests and remains an empty fixture source; no backend-specific fixture was dropped.

`GalleryRenderingTests` renders each gallery route and layout, checks per-session modal service isolation, resolves generic registrations and exercises the modern grid's paging and local mutations. `StreamingAndPagingRegressionTests` checks dispatcher delivery, cancellation/disposal and zero-based page boundaries.

The tests reference the library and tester because the loader menu view models, demo pages and local grid data source belong to the utility. They require no browser driver, external service or WebAssembly runtime. Browser JavaScript behavior is outside HtmlRenderer coverage.

The two superseded archived test project files remain inactive migration references pending cleanup approval; only the root test project is included in solutions and CI.

ApplicationBlock coverage includes frozen builders/catalogs, per-type activation policies, circuit isolation, guard cancellation, block removal, scoped actions, rendered component retention/disposal, bookmark/history resolution and overlapping asynchronous route changes. Component lifecycle regressions cover parameter updates, stale page requests, stream replacement, modal completion/disposal and final wizard validation.
