# Sphere10.Framework.Utils.MvcTester

A minimal .NET 10 ASP.NET Core MVC host referencing [Sphere10.Framework.Web.AspNetCore.MVC](../../src/Sphere10.Framework.Web.AspNetCore.MVC/README.md). Use it to verify host startup and add controller/view demonstrations for the MVC library. The current landing page displays **Sphere10 Framework MVC Tester** and a message that the application is ready for demonstrations.

This utility is not a NuGet package. It has no configured database, external service or application credentials.

## Run the tester

Install the .NET 10 SDK. Run commands **from the repository root**; the first run restores the project references and NuGet dependencies.

For a terminal session with an explicit HTTP address:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.MvcTester/Sphere10.Framework.Utils.MvcTester.csproj -c Release -p:BuildRevision=0 --no-launch-profile -- --urls http://127.0.0.1:5091
```

Wait for `Now listening on`, then open [http://127.0.0.1:5091](http://127.0.0.1:5091). `dotnet run` does not promise to open a browser. Keep the terminal running while using the site, then press **Ctrl+C** to stop it. `BuildRevision=0` supplies an explicit local build revision.

In Visual Studio, set `Sphere10.Framework.Utils.MvcTester` as the startup project and choose its named profile. [launchSettings.json](Properties/launchSettings.json) configures browser launch, the Development environment, HTTPS at `https://localhost:49901` and HTTP at `http://localhost:49902`. F5/Ctrl+F5 uses that profile rather than the explicit port in the terminal example.

To use the same profile from a terminal:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.MvcTester/Sphere10.Framework.Utils.MvcTester.csproj -c Release -p:BuildRevision=0
```

The HTTPS profile needs a usable development certificate. The explicit HTTP command above provides a straightforward local startup check without that requirement.

## Routes and host setup

| Location | Responsibility |
| --- | --- |
| [Program.cs](Program.cs) | Registers controllers/views and Sphere10 Framework, starts the framework, and maps conventional MVC routing. |
| [Controllers/HomeController.cs](Controllers/HomeController.cs) | Returns the landing view from `Index`. |
| [Views/Home/Index.cshtml](Views/Home/Index.cshtml) | Complete HTML landing page with `Layout = null`. |
| [Sphere10.Framework.Utils.MvcTester.csproj](Sphere10.Framework.Utils.MvcTester.csproj) | Web SDK host targeting `net10.0`, with a reference to the MVC library and packaging disabled. |

The route pattern is `{controller=Home}/{action=Index}/{id?}`. Both `/` and `/Home/Index` reach the landing page. The current host does not define a shared layout or an interactive Blazor circuit. Use the [Blazor tester](../Sphere10.Framework.Utils.BlazorTester/README.md) for grids, component dialogs and ApplicationBlock demonstrations.

The framework registration and startup calls are already present:

```csharp
builder.Services.AddControllersWithViews();
builder.Services.AddSphere10Framework(_ => { });

var app = builder.Build();
app.StartSphere10Framework();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
```

## Add a demonstration

Add a controller under `Controllers` and its matching Razor view under `Views/<ControllerName>`. For example, save this as `Controllers/SampleController.cs` with the repository's normal license header:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Sphere10.Framework.Utils.MvcTester;

public class SampleController : Controller {
	public IActionResult Index() => View();
}
```

Create `Views/Sample/Index.cshtml`:

```razor
@{
	Layout = null;
}
<!DOCTYPE html>
<html lang="en">
<head><meta charset="utf-8" /><title>Sample MVC demonstration</title></head>
<body><main><h1>Sample MVC demonstration</h1></main></body>
</html>
```

Restart the host and open `/Sample`. Use the existing framework registration callback when your sample needs additional Sphere10 modules, and register sample services in `Program.cs`. Add view models when the demonstration needs typed input or output. If adding CSS or scripts, configure their serving middleware and reference them from the view; the current landing page is self-contained.

Consult [the MVC library guide](../../src/Sphere10.Framework.Web.AspNetCore.MVC/README.md) for library features and extension points.

## Verification and troubleshooting

A build verifies the host and Razor views:

```powershell
dotnet build utils/Sphere10.Framework.Utils.MvcTester/Sphere10.Framework.Utils.MvcTester.csproj -c Release -p:BuildRevision=0
```

After starting the HTTP host, check `/` and `/Home/Index` in a browser. A terminal smoke check can also verify a successful response:

```powershell
Invoke-WebRequest http://127.0.0.1:5091/Home/Index | Select-Object StatusCode
```

This is a startup/view smoke test, not broad MVC feature coverage. Put reusable library regression tests in the appropriate test project and keep tester-specific sample checks separate.

| Symptom | Check |
| --- | --- |
| No browser opened | Open the address printed by the running terminal, or check the IDE startup project and launch profile. |
| Address already in use | Stop the previous host, choose another port, or run with `--no-launch-profile -- --urls http://127.0.0.1:0`. Open the assigned port printed by the server. |
| HTTPS certificate error | Use the explicit HTTP command for local testing, or configure/trust your ASP.NET Core development certificate before using the HTTPS profile. |
| Controller or view returns 404/not found | Check conventional route names and that the view is under `Views/<ControllerName>/<ActionName>.cshtml`. |
| SDK or restore failure | Check `dotnet --info` for a .NET 10 SDK and read the first restore/build error. |
| Build cannot replace a running executable | Stop the host with Ctrl+C before rebuilding that configuration. |