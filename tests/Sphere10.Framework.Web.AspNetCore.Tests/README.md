# Sphere10.Framework.Web.AspNetCore.Tests

NUnit tests for [Sphere10.Framework.Web.AspNetCore](../../src/Sphere10.Framework.Web.AspNetCore/README.md) and [Sphere10.Framework.Web.AspNetCore.MVC](../../src/Sphere10.Framework.Web.AspNetCore.MVC/README.md). The project references only the MVC library, which brings in the ASP.NET Core library. The tests use `DefaultHttpContext` and an in-process `WebApplication` builder, so they start no web server.

## Run the tests

Run these commands from the repository root. `dotnet test` restores and builds the referenced projects first. `BuildRevision=0` supplies an explicit local build revision.

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Tests/Sphere10.Framework.Web.AspNetCore.Tests.csproj -c Release -p:BuildRevision=0
```

Run the MVC fixtures only:

```powershell
dotnet test tests/Sphere10.Framework.Web.AspNetCore.Tests/Sphere10.Framework.Web.AspNetCore.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~MvcResultTests|FullyQualifiedName~BootstrapFormScopeTests"
```

## Requirements

`MvcResultTests.ImageResultWritesAsynchronously` draws a `System.Drawing` bitmap. It is marked as Windows-only and reports as inconclusive on other platforms. Every other case runs on any platform without a network, browser or database.

## Contents

### Project root

Request, middleware, form-scope and MVC result fixtures.

| Fixture | Purpose |
| --- | --- |
| [AspNetCoreRequestTests](AspNetCoreRequestTests.cs) | `GetUri` default ports, `ToStringAsync` request text with a restored body position, enum-named query parameters, `CloudflareConnectingIPMiddleware` client-address replacement and `MicrosoftExtensionsLoggerAdapter` exception forwarding. |
| [BootstrapFormScopeTests](BootstrapFormScopeTests.cs) | `BeginBootstrapForm` completion markers and client options, retry after a failed completion, awaited asynchronous output, form tag method and overlay options, the embedded result script and buffering of the synchronous MVC end-form output. |
| [MvcResultTests](MvcResultTests.cs) | `XmlResult` status codes and sitemap serialization, the embedded bootstrap-forms script, enum select lists, asynchronous `ImageResult` output, rejection of an unregistered filter by `FilterFactoryBase<T>` and `CustomRouteAttribute` route registration. |
| [CustomRouteSampleController](MvcResultTests.cs) | Sample controller with a `[CustomRoute]` attribute used by the route registration case. It contains no tests. |
| [NetworkParsingTests](NetworkParsingTests.cs) | `Tools.Web.AspNetCore.ParseNetwork` normalization of IPv4 and IPv6 CIDR host bits and rejection of invalid networks. |
