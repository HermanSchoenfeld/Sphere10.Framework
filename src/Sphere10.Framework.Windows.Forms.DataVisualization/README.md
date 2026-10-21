# Sphere10.Framework.Windows.Forms.DataVisualization

The legacy Microsoft Windows Forms **DataVisualization** chart control, absorbed into Sphere10 Framework. Its namespace, runtime assembly and package are `Sphere10.Framework.Windows.Forms.DataVisualization`. Internal support types use the corresponding `.Utilities` namespace.

The original project is [dotnet/winforms-datavisualization](https://github.com/dotnet/winforms-datavisualization). This import retains Kirsan's runtime, performance and per-monitor DPI improvements, via [HermanSchoenfeld/winforms-datavisualization](https://github.com/HermanSchoenfeld/winforms-datavisualization) at commit `2a1fcc400700401f655e6e7df01fc397afad9901` (which merged [kirsan31's continuation](https://github.com/kirsan31/winforms-datavisualization)). The upstream checkout is not needed to build or use this project.

Microsoft/.NET Foundation and contributor source notices remain intact. See [LICENSE.TXT](LICENSE.TXT), [THIRD-PARTY-NOTICES.TXT](THIRD-PARTY-NOTICES.TXT) and [PROVENANCE.md](PROVENANCE.md) for the original licenses, source revision and Sphere10 adaptations; all three are included in the package. Imported runtime code retains its original organization and style.

The project builds one modern .NET assembly containing line, bar, column, area, pie, financial, statistical and 3D chart types together with their enums, palettes, helpers and resources. It uses Framework's version and build metadata. The separate symbol package contains the chart assembly's PDB for each target.

The old .NET Framework 4.7.2 designer client and its custom editor/server integration were removed. Standard WinForms control design, property metadata, type converters and runtime chart serialization remain available.

Build and package this project with the usual Framework commands. SDK 10 builds `net10.0-windows`; SDK 11 also builds `net11.0-windows`. .NET 11 RC 2 support is a prerelease. `DataVisualizationTargetFrameworks` can restrict the targets explicitly. Build outputs and intermediates can be redirected with `--artifacts-path`.

```powershell
dotnet build src/Sphere10.Framework.Windows.Forms.DataVisualization/Sphere10.Framework.Windows.Forms.DataVisualization.csproj -c Release -p:BuildRevision=0
dotnet pack src/Sphere10.Framework.Windows.Forms.DataVisualization/Sphere10.Framework.Windows.Forms.DataVisualization.csproj -c Release -p:BuildRevision=0
```

Source builds and normal SDK packaging use the same single chart project. Tests adapted from the imported project are kept under `Tests/` and linked into Framework's WinForms NUnit suite; they are excluded from the runtime assembly.
