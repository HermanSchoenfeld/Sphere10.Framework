# NuGet consumer validation

Run `./validate-packages.ps1` from the repository root with PowerShell and the .NET 10 SDK. The default Windows solution covers all active public libraries and requires the Windows desktop targeting pack. Pass `-solutionPath 'src/Sphere10.Framework (CrossPlatform).sln'` to validate that solution instead.

The script evaluates each source project's `IsPackable` and `PackageId`, builds a temporary solution containing the public libraries, and packs the complete dependency graph with a unique `0.0.0-validation.*` version. It checks IDs, versions, README/icon/license/repository metadata, matching symbol packages, internal dependencies and compiler-analyzer layout.

The fixtures in this directory are copied into an external temporary workspace. Their generated projects use only `PackageReference`; repository project references, build properties and central package versions are unavailable. Source mapping pins framework IDs to the generated local feed. An empty package cache and exact unique versions prevent an existing public package from satisfying a missing local dependency. Third-party packages restore from nuget.org, so the first restore requires network access.

Four consumers cover distinct compatibility requirements:

- **Core** references pure Web and verifies that ASP.NET Core is absent from its restore graph.
- **Libraries** references every non-desktop, non-ASP.NET package, including NUnit helpers and the generator. A partial property must compile from the packed `AutoDirty` analyzer.
- **Desktop** references the Windows packages and derives a WinForms application screen.
- **Web** references MVC and Blazor, registers an ApplicationBlock, and renders its screen through ApplicationShell using interactive server rendering. It builds and publishes, verifies static web assets and CSS isolation imports, then starts the published app on a temporary loopback port and checks rendered HTML, MVC XML, CSS and JavaScript over HTTP.

The checks validate packaging, compilation, server rendering and static asset delivery. They do not exercise browser interactions or replace the NUnit suites.

Packages, generated projects, logs, published files and `validation.json` remain in the reported temporary directory for diagnosis. Workspace names are deliberately short to keep NuGet/Razor asset paths within Windows tooling limits. No package publishing or automatic file deletion occurs. The source build uses `BuildRevision=0`, preserving the local build counter. Use `pack.ps1` separately to create release packages after validation.
