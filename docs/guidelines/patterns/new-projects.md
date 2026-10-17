# Independent Sphere10 applications

Read this practice when creating an internal/client application or separating
an existing Framework utility into its own solution. Keep application product
code in its own directory and reusable infrastructure in Framework. Follow
[Framework reuse](framework-reuse.md) and [3-tier architecture](../3-tier-architecture.md)
when selecting layers and references.

## Location and ownership

The Sphere10 workspace contains the `Framework` checkout and independent
applications:

| Application | Location from the workspace root | Default Framework root from the application |
|---|---|---|
| Internal product/tool | `<Name>/` | `../Framework` |
| Client project | `Clients/<Name>/` | `../../Framework` |

For example, `SystemExpert` is an internal sibling of `Framework`, while
`DewateringPipelineDesigner` lives under `Clients`. Resolve these paths from the
chosen workspace rather than hardcoding a developer's drive or home directory.
Honor an explicit user location and preserve established project names.

Each application has its own solution, startup project, tests, build
configuration and documentation. Common source/tests folders are useful; the
architecture determines the actual project split rather than a fixed project
count. Do not turn a new application into another Framework utility solely to
inherit build settings.

When absorbing an old tool into an existing application as a plugin, put the
plugin in a separate class-library project under that application's source
folder. Reference the plugin from the application; the plugin references
Framework libraries rather than the application executable. Keep one application
root, service provider, logger and lifecycle. Preserve useful workflows and
resource provenance while replacing the old shell and obsolete dependencies.
Leave the original repository intact when adapting rather than moving its files.

When extracting an application, move general collectors, models, services,
controls and native operations into the owning Framework assemblies and adopt
them from the app. Keep product startup, menus, screens, user confirmations and
application-specific policy in the application. Move its behavioral tests to
the application test project while keeping shared infrastructure coverage with
Framework. Preserve tracked file history and update callers, project/solution
references, resource paths and documentation together.

## Interchangeable source and NuGet references

Use these three properties in the application's `Directory.Build.props`:

| Property | Meaning |
|---|---|
| `UseSphere10FrameworkSource` | `true` uses the selected Framework checkout; `false` uses packages. |
| `Sphere10FrameworkRoot` | The checkout root, with a default relative to `MSBuildThisFileDirectory`. |
| `Sphere10FrameworkVersion` | Exact package version matching the implementation the application requires. |

Keep an application's assemblies in one item list so the two modes cannot
silently drift:

```xml
<ItemGroup>
  <Sphere10Library Include="Sphere10.Framework" />
  <Sphere10Library Include="Sphere10.Framework.Windows" />
  <Sphere10Library Include="Sphere10.Framework.Windows.Forms" />
</ItemGroup>
```

Import `build/Sphere10.Framework.References.targets` from
`Directory.Build.targets`. The helper projects each item to
`$(Sphere10FrameworkRoot)/src/<Library>/<Library>.csproj` in source mode, or to
`PackageReference` with `VersionOverride="[$(Sphere10FrameworkVersion)]"` in
package mode. The brackets require that exact version; an unbracketed NuGet
version permits a higher-version fallback. `VersionOverride` works with central package management; ordinary
third-party dependency versions belong in the application's
`Directory.Packages.props`. Validate the selected mode, source library paths
and required package version instead of falling back silently.

Do not import Framework's entire `Directory.Build.props` into the application's
own projects. Give the product its own metadata and supported target frameworks;
source project references already use Framework's own build configuration.
Keep package mode independent of a source checkout at build time. The configured
NuGet feeds must supply the exact selected version and its transitive packages.
For a local integration version, document the local feed and produce a matching
complete package set before testing package mode.

Build/test the real application in each mode with separate restored assets.
Changing the mode requires restore; a previous `project.assets.json` can hide
the wrong reference graph. Verify source mode resolves project references and
package mode resolves the selected package version without Framework project
references. A source build alone does not prove that the public packages contain
new APIs or that deployment works.

## Solution and verification

Put the real application and test projects in its independent solution. Include
its README, configuration and scripts as solution items. Link relevant Framework
API documentation without copying files or adding Framework's development
solution as a product project.

When source mode exposes Framework projects for navigation in Visual Studio,
provide a companion `<Name>.Packages.sln` containing only the application's
projects. Select package mode for that solution through `SolutionName` in the
application properties, while still permitting an explicit property override.
Direct project builds/tests must pass `UseSphere10FrameworkSource=false` to
select packages. This keeps package solution builds independent of the source
checkout; changing the reference mode does not remove projects explicitly
listed in a source solution.

Apply the same reference switch to every plugin library and include those
projects in both solutions. Check the resulting solution graph: adding a project
can automatically add its currently resolved Framework source dependencies to
the package solution. Remove those source entries from the package solution.
Package preparation must traverse application project references as well as
Framework project references, so plugin-only Framework libraries are included.

Record the actual checks run: source/package restores and builds, meaningful
NUnit tests, startup and resource loading. Update the relevant project documents
and engineering practices when their contracts change. See [testing](testing.md)
for the validation workflow.
