# Build Transitive

An MSBuild targets file, shipped in the NuGet package, that copies this package's third-party licence notices into every consuming application's build and publish output.

The project packs `THIRD-PARTY-NOTICES.md` and the `ThirdParty` folder as NuGet content files under `contentFiles/any/any/Licenses/Sphere10.Framework.Windows`. NuGet content files do not flow through an indirect package reference, so an application that references this package only through another package would not receive them. `Sphere10.Framework.Windows.targets` is packed into the package's `buildTransitive` folder, which NuGet imports for direct and indirect consumers alike. It adds the notice files as `None` items linked under `Licenses/Sphere10.Framework.Windows` with `CopyToOutputDirectory` and `CopyToPublishDirectory` set to `PreserveNewest`, and keeps them out of single-file bundles.

Nothing here is compiled. Edit the targets file only together with the packing rules in `Sphere10.Framework.Windows.csproj`.

## Contents

| File | Kind | Purpose |
|---|---|---|
| [`Sphere10.Framework.Windows.targets`](Sphere10.Framework.Windows.targets) | MSBuild targets | Links the packaged licence notices into consumer output and publish folders. |
