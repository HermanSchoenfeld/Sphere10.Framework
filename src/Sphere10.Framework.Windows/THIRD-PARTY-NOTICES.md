# Managed analysis engine provenance

The Windows library references the following upstream NuGet packages. Their
source has not been vendored or relicensed into Sphere10 Framework.

| Package | Pinned version | Upstream | License |
| --- | --- | --- | --- |
| ICSharpCode.Decompiler | 11.2.0.10050 | [ILSpy package source](https://github.com/icsharpcode/ILSpy/tree/c86190ef4e24f3137c13a9275316042842d2fada) | [Retained MIT license](ThirdParty/ILSpy.LICENSE.txt) |
| ICSharpCode.BamlDecompiler | 11.2.0.10050 | [ILSpy BAML package source](https://github.com/icsharpcode/ILSpy/tree/c86190ef4e24f3137c13a9275316042842d2fada/ICSharpCode.BamlDecompiler) | [Retained MIT license](ThirdParty/ILSpy.LICENSE.txt) |
| Microsoft.Diagnostics.Runtime | 4.2.751201 | [ClrMD package source](https://github.com/microsoft/clrmd/tree/2e7e5d74cddb2be0ac523aedead10b90a95e71c3) | [Retained MIT license](ThirdParty/ClrMD.LICENSE.txt) |

Redistribution must preserve each package's license/copyright notices and the
notices of its transitive dependencies. The referenced packages remain separate
assemblies, with their original names, authorship and license metadata. Do not
replace their license declarations with Sphere10's source header.

## Resolved dependency notices

[The provenance manifest](ThirdParty/managed-analysis-sources.json) records the
resolved package/version variants and declared BAML dependency minimums, the
selected runtime assets, upstream repository commits and retained legal-file
SHA-256 values. It includes
Azure SDK, MSAL, IdentityModel, AntiSSRF, Diagnostics Client and shared .NET
dependency notices. Identical upstream files are stored once and referenced by
each applicable package. Original notices are preserved without editing or
applying Sphere10 source headers. The manifest describes the recorded dependency
resolution; refresh it when a dependency/version or resolved runtime target changes.

Source builds preserve the notice and `ThirdParty` sidecars beside the assembly.
The NuGet package retains these under `licenses/` and also declares `None`
content files with copy-to-output enabled and a package-owned `buildTransitive`
rule for indirect consumers. Package consumers receive the same
tree under `Licenses/Sphere10.Framework.Windows/`, keeping its notice names
separate from other libraries. Publish keeps these legal files external to
single-file bundles.

Native interop declarations follow the Windows SDK ABI (`ProcessSnapshot.h`,
`DbgHelp.h`) and call operating-system libraries. They do not redistribute the
Windows SDK or DbgHelp binaries. Microsoft platform support requirements apply.

These three engine dependencies were approved for managed assembly/runtime
analysis and explicit BAML-to-XAML resource viewing. Remote symbol services,
debugger automation and assembly rewriting are not implicitly enabled by their
presence. BAML conversion produces text; it does not host WPF or execute target
assemblies or markup.
