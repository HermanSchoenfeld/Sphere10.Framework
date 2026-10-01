# Sphere10.Framework.Utils.MvcTester

Minimal .NET 10 ASP.NET Core MVC application referencing `Sphere10.Framework.Web.AspNetCore.MVC`. It registers Sphere10 Framework with the host and serves an empty demonstration landing page through `HomeController` and a Razor view.

From the repository root:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.MvcTester -p:BuildRevision=0 --urls http://localhost:5091
```

Open http://localhost:5091. Add future MVC demonstrations as controllers and views here. This tester is not packaged for NuGet.
