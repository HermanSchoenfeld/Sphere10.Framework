# Third-Party Notices

Holds the upstream license and notice files of the managed-analysis packages this library references, and the manifest that maps them to package versions.

This folder contains no source types. The library references the ILSpy decompiler packages and ClrMD (`Microsoft.Diagnostics.Runtime`) as separate NuGet assemblies, and their transitive dependencies bring Azure SDK, MSAL, IdentityModel, AntiSSRF, .NET diagnostics and .NET runtime notices. The files here are the original upstream texts, kept unedited and stored once per identical file. [`managed-analysis-sources.json`](managed-analysis-sources.json) records each file's upstream source (a pinned repository commit or NuGet package), its SHA-256 and the package versions it applies to. The project's [third-party notices](../THIRD-PARTY-NOTICES.md) explain how builds and the NuGet package carry these files. Refresh the manifest and the files when a referenced package version changes.

## Contents

| File | Purpose |
|---|---|
| [`AntiSSRF-8366ff53e9c7.LICENSE.txt`](AntiSSRF-8366ff53e9c7.LICENSE.txt) | AntiSSRF license |
| [`AzureSDK-f7400e447111.LICENSE.txt`](AzureSDK-f7400e447111.LICENSE.txt) | Azure SDK for .NET license |
| [`AzureSDK-f7400e447111.NOTICES.txt`](AzureSDK-f7400e447111.NOTICES.txt) | Azure SDK for .NET third-party notices |
| [`ClrMD.LICENSE.txt`](ClrMD.LICENSE.txt) | ClrMD license, shared with the identical .NET diagnostics license |
| [`Diagnostics-d7b455b46332.NOTICES.txt`](Diagnostics-d7b455b46332.NOTICES.txt) | .NET diagnostics third-party notices |
| [`DotNet-10.0.3.NOTICES.txt`](DotNet-10.0.3.NOTICES.txt) | Third-party notices shipped in the 10.0.x `Microsoft.Extensions` and BCL packages |
| [`DotNet-11rc2-Package.NOTICES.txt`](DotNet-11rc2-Package.NOTICES.txt) | Third-party notices shipped in the .NET 11 release-candidate BCL and `Microsoft.Extensions` packages |
| [`DotNet-c2435c3e0f46.LICENSE.txt`](DotNet-c2435c3e0f46.LICENSE.txt) | License of the `dotnet/dotnet` repository, shared by the .NET packages |
| [`DotNet-c2435c3e0f46.NOTICES.txt`](DotNet-c2435c3e0f46.NOTICES.txt) | Third-party notices of the `dotnet/dotnet` repository |
| [`IdentityModel-c8f7d87bcda3.LICENSE.txt`](IdentityModel-c8f7d87bcda3.LICENSE.txt) | IdentityModel license |
| [`IdentityModel-c8f7d87bcda3.NOTICES.html`](IdentityModel-c8f7d87bcda3.NOTICES.html) | IdentityModel third-party notices in HTML |
| [`IdentityModel-c8f7d87bcda3.NOTICES.txt`](IdentityModel-c8f7d87bcda3.NOTICES.txt) | IdentityModel third-party notices |
| [`ILSpy.LICENSE.txt`](ILSpy.LICENSE.txt) | ILSpy license for `ICSharpCode.Decompiler` and `ICSharpCode.BamlDecompiler` |
| [`managed-analysis-sources.json`](managed-analysis-sources.json) | Provenance manifest of upstream URLs, SHA-256 values and package mappings for every file above |
| [`MSAL-d5d7de6b103f.LICENSE.txt`](MSAL-d5d7de6b103f.LICENSE.txt) | Microsoft Authentication Library license |
| [`MSAL-d5d7de6b103f.NOTICES.txt`](MSAL-d5d7de6b103f.NOTICES.txt) | Microsoft Authentication Library third-party notices |
| [`ProtectedData-4.5.0.LICENSE.txt`](ProtectedData-4.5.0.LICENSE.txt) | `System.Security.Cryptography.ProtectedData` 4.5.0 license |
| [`ProtectedData-4.5.0.NOTICES.txt`](ProtectedData-4.5.0.NOTICES.txt) | `System.Security.Cryptography.ProtectedData` 4.5.0 third-party notices |
