# NuGet consumer validation

This folder holds the consumer fixtures for [validate-packages.ps1](../validate-packages.ps1). The script packs the Framework libraries under a throwaway version and then builds these fixtures against those packages alone, so that a package with a missing dependency, asset or API fails validation before release. It publishes nothing.

## Run the validation

Run the script from the Framework repository root with PowerShell and the .NET 10 and 11 SDKs:

```powershell
./scripts/validate-packages.ps1
./scripts/validate-packages.ps1 -solutionPath 'src/Sphere10.Framework (CrossPlatform).sln'
```

| Parameter | Default | Purpose |
|---|---|---|
| `-solutionPath` | `src/Sphere10.Framework (Win).sln` | The solution whose packable projects under `src/` are packed. The Windows solution covers every active library and needs the Windows desktop targeting pack. The cross-platform solution leaves out the Windows libraries, so the `Desktop` consumer is skipped. |
| `-configuration` | `Release` | The configuration, `Debug` or `Release`, used for packing and for building the consumers. |
| `-consumerFramework` | `net10.0` | Select `net10.0` or `net11.0`. Desktop consumers add the Windows suffix. CI checks both. |
| `-packagesDirectory` | Not set | Validate an existing package batch instead of rebuilding it. Requires `-packageVersion`. |
| `-packageVersion` | Generated validation version | Exact version of the supplied package batch. |
| `-workspaceDirectory` | New temporary folder | Optional new workspace for isolated consumers, cache and reports. Existing directories are rejected. |

To validate the exact artifacts that will be published, run once for each target with a distinct workspace:

```powershell
./scripts/validate-packages.ps1 -packagesDirectory <batch> -packageVersion <exact-version> -consumerFramework net10.0
./scripts/validate-packages.ps1 -packagesDirectory <batch> -packageVersion <exact-version> -consumerFramework net11.0
```

The [publishing workflow](../../docs/nuget-publishing.md) seals the original batch and uses these checks before uploading it.

Each run creates a new workspace folder named `s10nuget-<id>` in the system temporary directory and prints its path. The workspace and consumer names are short so that NuGet and Razor asset paths stay within Windows path-length limits. Third-party packages restore from nuget.org into an empty package cache inside that workspace, so every run needs network access. The script never deletes a workspace, so remove old `s10nuget-*` folders yourself.

After a successful run, create the release packages with `./scripts/pack.ps1`, as the [repository README](../../README.md#building-and-publishing-packages) describes.

## What the script checks

1. **Packing.** The script reads `IsPackable` and `PackageId` from every project under `src/` in the solution. It adds the packable projects to a temporary `Packages.sln` and packs them with the version `0.0.0-validation.<UTC timestamp>.<id>` and `BuildRevision=0`, which leaves the local build counter unchanged.
2. **Package contents.** Every package must declare authors, description, license, readme, icon and repository metadata, contain its readme, icon and `LICENSE` files, and have a matching `.snupkg`. Every dependency on `Sphere10.Framework*` or `Sphere10.HashLib4CSharp` must name a package from the same run at the same version. `Sphere10.Framework.Generators` must contain `analyzers/dotnet/cs/Sphere10.Framework.Generators.dll` and no `lib/` assets. The Blazor package must contain `app.css`, `themes.css`, `functions.js`, `BlazorGrid.js`, the grid and reference-picker modules and a `buildTransitive` props file.
3. **Isolated consumers.** The script copies each fixture folder into the workspace beside a generated `PackageConsumer.<name>.csproj`, whose only references are exact-version `PackageReference` items. Because the workspace is outside the repository, the consumers cannot see its project references, build properties or central package versions. The workspace's own `Directory.Build.props`, `Directory.Build.targets` and `Directory.Packages.props` also block customizations from parent folders. The generated `NuGet.Config` maps `Sphere10.Framework*` and `Sphere10.HashLib4CSharp` to the local feed and every other package to nuget.org. Together with the empty cache and the exact versions, this mapping stops a package already on nuget.org from hiding a missing local package.
4. **Published web host.** The script publishes the `Web` consumer, checks its static web assets and requests its pages over HTTP, as [The Web consumer](#the-web-consumer) describes.

| Fixture | Package references | What it must compile or pass |
|---|---|---|
| `Core` | `Sphere10.Framework.Web` | Creates a `SitemapXml`. Its restore graph and framework references must contain no ASP.NET Core, JS interop, MAUI, Windows desktop, WinForms, WPF or `System.Drawing.Common` assemblies, and no `Sphere10.Framework.Windows*`, `Sphere10.Framework.Web.AspNetCore*` or `Sphere10.Framework.Drawing` package. |
| `Application` | `Sphere10.Framework.Application` | Uses the UI-independent application model in `Sphere10.Framework.Application.UI`. It derives a block, a menu and a menu item from `ApplicationBlockBase`, `ApplicationMenuBase<T>` and `ApplicationMenuItemBase`, and it uses screen definitions, `ApplicationPluginBuilder`, `ApplicationPluginRegistration<TPlugin>`, `IApplication.LoadedPlugins` and `Blocks`, the screen-host contracts and `Wizard<TModel, TStep>`. Its restore graph passes the same UI-free check as `Core`. |
| `Libraries` | Every package except `Sphere10.Framework.Windows*` and `Sphere10.Framework.Web.AspNetCore*` | A partial property compiles through the packed `[AutoDirty]` generator, and `AssertEx` and `UnitTestDAC` resolve from the NUnit packages. |
| `Desktop` | Every `Sphere10.Framework.Windows*` package, for the selected Windows target | Derives the WinForms `ApplicationScreen` and builds a plugin and its block with the WinForms `ApplicationPluginBuilder`. |
| `Renderer` | `Sphere10.Framework.Web.VisualRenderer` | Executes HTML rendering, opens the embedded theme archive and verifies that license notices reach the consumer output. |
| `Web` | `Sphere10.Framework.Web.AspNetCore.MVC` and `Sphere10.Framework.Web.AspNetCore.Blazor` | Builds, publishes and serves the Blazor host that [The Web consumer](#the-web-consumer) describes. |

The workspace keeps the packages in `feed`, the generated consumer projects, the published host in `published-web`, a log for each restore, build, pack and publish step, and a `validation.json` summary of the run.

## The Web consumer

`Program.cs` registers the Blazor host through `BuildBlazorApplication()`, which returns the Blazor `ApplicationBuilder`. The builder sets the document title and favicon and adds one plugin through `BlazorPluginBuilder`, whose Blazor `ApplicationBlockBuilder` registers `WelcomeScreen` as a permanent singleton with `AddScreen<TScreen>(...)` and makes it the default screen. The block's `ApplicationMenuBuilder` shows that registration through `AddShowScreenItem<TScreen>(...)`. The host also maps `/sitemap.xml` to an MVC `XmlResult`.

`PackageWorkspace.razor` renders an interactive `ApplicationShell` inside a `ThemeProvider`, with a `ThemeSelector` in its toolbar. The block's screen, `WelcomeScreen.razor`, derives from the Blazor `ApplicationScreen` and shows a typed `BlazorGrid` over an in-memory `ListDataSource` that allows create, read, update, delete and paging. Its **Related record** column edits through a `BlazorGridReferencePicker`, and the grid's default `BlazorGridController` applies **Save** and **Cancel**.

After publishing, the script checks that:

- every static web asset in the Blazor package exists under `wwwroot/_content/Sphere10.Framework.Web.AspNetCore.Blazor/`
- the package's scoped CSS bundle is published and `PackageConsumer.Web.styles.css` imports it
- the published host starts on a loopback port in the Production environment within 30 seconds
- `/` renders the screen text `ApplicationBlock loaded from NuGet`, the **Package grid** menu item, the light theme, the theme selector and the grid with both sample records and its **Related record** column
- `/sitemap.xml` returns the MVC XML result, `themes.css` defines `--sphere10-surface`, and the host stylesheet and every packaged JavaScript module return successfully

The script then stops the host.

## Limits

The checks cover packaging, compilation, server rendering and static asset delivery. They do not drive a browser, run the NUnit suites or run plugin initializers and finalizers. The application and platform NUnit suites and both tester applications cover plugin lifecycles. The [application guide](../../src/Sphere10.Framework.Application/README.md) describes the application model that these fixtures compile against.

## Inspect the published host

After a run, start the published host from the workspace's `published-web` folder:

```powershell
dotnet ./PackageConsumer.Web.dll --urls http://127.0.0.1:5188 --environment Production
```

Open `http://127.0.0.1:5188`, choose a theme, select a row and click **Edit**. Open the **Related record** picker, then confirm that **Cancel** keeps the previous value and **Save** applies the selection. Edits stay in memory and belong to that screen instance.

## Recheck the web consumer

To retest edited `Web` fixture files against the packages of an earlier run, stop the published host if it is running and copy the files from `scripts/package-consumers/Web` into the workspace's `Web` folder. Then rebuild and republish without restoring, using the configuration of the original run:

```powershell
dotnet build <workspace>/Web/PackageConsumer.Web.csproj -c Release --no-restore
dotnet publish <workspace>/Web/PackageConsumer.Web.csproj -c Release --no-restore -o <workspace>/published-web
```

This reuses the packed libraries and repeats none of the script's checks, so inspect the host as described above. Run the validation script again when Framework source changes must be included.
