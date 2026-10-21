# Source Provenance

This project derives from Microsoft's Windows Forms DataVisualization chart control, released through the [.NET Foundation repository](https://github.com/dotnet/winforms-datavisualization). It includes Kirsan's maintained continuation through [HermanSchoenfeld/winforms-datavisualization](https://github.com/HermanSchoenfeld/winforms-datavisualization), imported at commit `2a1fcc400700401f655e6e7df01fc397afad9901`.

Sphere10 adaptations:

- Consolidated runtime code, chart enums, helper types and resources into one `Sphere10.Framework.Windows.Forms.DataVisualization` assembly and package.
- Adopted the `Sphere10.Framework.Windows.Forms.DataVisualization` namespace and its subnamespaces, including corresponding embedded resource names.
- Retained modern .NET 10 and .NET 11 Windows targets, Framework version metadata and standard NuGet/symbol packaging.
- Removed the legacy .NET Framework designer client, its protocol/server support and associated custom editor declarations. Standard WinForms component metadata, runtime converters and chart serialization remain.
- Adapted upstream collection, keyword and chart rendering tests to Framework's NUnit suite.

Original Microsoft/.NET Foundation and contributor license notices remain in the source. See `LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT` for the upstream MIT license and third-party notices. The separate Framework license also applies to Sphere10's contributions.
