# BAML regression fixtures

These are inert inputs for metadata/resource inspection and BAML-to-XAML tests.
The test projects do not reference WPF or the fixture assembly. Tests read the
PE/resources as data; they do not load the target assembly, instantiate its XAML,
or execute target code. The isolated build below uses WPF's normal build tools,
but does not launch the compiled fixture.

## Sources and attribution

- [SimpleDictionary.xaml](SimpleDictionary.xaml) is an unchanged upstream ILSpy
  fixture from commit `c86190ef4e24f3137c13a9275316042842d2fada`,
  path `ILSpy.BamlDecompiler.Tests.Windows/Cases/SimpleDictionary.xaml`.
  [Pinned source](https://github.com/icsharpcode/ILSpy/blob/c86190ef4e24f3137c13a9275316042842d2fada/ILSpy.BamlDecompiler.Tests.Windows/Cases/SimpleDictionary.xaml).
- [LICENSE](LICENSE) is the unchanged MIT license from that same commit:
  Copyright (c) 2011–2025 AlphaSierraPapa for the ILSpy team.
  [Pinned license](https://github.com/icsharpcode/ILSpy/blob/c86190ef4e24f3137c13a9275316042842d2fada/LICENSE).
- [AuthoredDictionary.xaml](AuthoredDictionary.xaml) was authored for Sphere10's
  2026 BAML regression tests. It contains a brush key/color and a style with
  literal text, so decoding tests verify meaningful values as well as the empty
  upstream dictionary. It is not an upstream ILSpy source file and follows this
  repository's licensing terms.

The fixture source, license and build recipe are preserved here so independent
checkouts do not depend on temporary build directories.

| Source file | Bytes | SHA-256 |
| --- | ---: | --- |
| SimpleDictionary.xaml | 143 | `bdadfe8691b261b13f0904632d4b12b0a3d2ecf2d71ffde37c3167ecbda6466a` |
| LICENSE | 1095 | `5527fe802347740da8ec2b510cffa4d22939c7f0773ae342a85a2e2b64e4a34b` |
| AuthoredDictionary.xaml | 377 | `b92f8827e0f7d6cd19ebfd6943b28fdec1bea302488c90c7581c23a077aa524e` |

## Committed generated inputs

Generated on Windows with .NET SDK **10.0.403**, targeting
`net10.0-windows` in Release configuration.

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| SimpleDictionary.baml | 630 | `c35ac0de3e1506fa3ec0f46680b2ccc6fd2333786c830e87df85d177e1dce0e7` |
| AuthoredDictionary.baml | 816 | `faf3d5177145e853d547662c3780ebd6381137e57b529c0643b326879a9a6f72` |
| OwnedBamlFixture.bin | 6656 | `5ac2dee835dfba7c3a63a68bfbd3bba1c6453e121a9317e246f75af6826fa1bb` |

`OwnedBamlFixture.bin` is the compiled `OwnedBamlFixture.dll` renamed to
`.bin` to keep it an explicit data input. Its `OwnedBamlFixture.g.resources`
contains both compiled dictionaries. It also embeds the authored BAML directly
as `Owned.DirectDictionary.baml`, exercising the direct and nested-resource
reader paths with the same bytes. The standalone BAML files preserve those
compiled inputs independently of the PE container.

## Rebuild in an isolated scratch directory

Use Windows with .NET SDK 10.0.403 and its Windows Desktop targeting pack.
Run the following PowerShell from this fixture directory. It creates a temporary
project outside the repository; do not add that project or a WPF reference to the
production/test solutions.

```powershell
$fixtureSource = (Get-Location).Path
$fixtureScratch = Join-Path ([System.IO.Path]::GetTempPath()) ('sphere10-baml-' + [guid]::NewGuid().ToString('N'))
$fixtureArtifacts = Join-Path $fixtureScratch 'artifacts'
New-Item -ItemType Directory -Path $fixtureScratch | Out-Null
Copy-Item -LiteralPath (Join-Path $fixtureSource 'SimpleDictionary.xaml'), (Join-Path $fixtureSource 'AuthoredDictionary.xaml') -Destination $fixtureScratch

@'
{"sdk":{"version":"10.0.403","rollForward":"disable"}}
'@ | Set-Content -LiteralPath (Join-Path $fixtureScratch 'global.json') -Encoding utf8

@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <EnableDefaultPageItems>false</EnableDefaultPageItems>
    <AssemblyName>OwnedBamlFixture</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Page Include="SimpleDictionary.xaml" />
    <Page Include="AuthoredDictionary.xaml" />
  </ItemGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $fixtureScratch 'OwnedBamlFixture.csproj') -Encoding utf8

Push-Location $fixtureScratch
try {
    dotnet build OwnedBamlFixture.csproj -c Release -p:UseArtifactsOutput=true "-p:ArtifactsPath=$fixtureArtifacts"
    if ($LASTEXITCODE -ne 0) { throw 'Initial WPF fixture compilation failed.' }

    $fixtureObj = Join-Path $fixtureArtifacts 'obj/OwnedBamlFixture/release'
    Copy-Item -LiteralPath (Join-Path $fixtureObj 'AuthoredDictionary.baml') -Destination $fixtureScratch

    [xml]$fixtureProject = Get-Content -LiteralPath 'OwnedBamlFixture.csproj' -Raw
    $fixtureEmbedded = $fixtureProject.CreateElement('EmbeddedResource')
    $fixtureEmbedded.SetAttribute('Include', 'AuthoredDictionary.baml')
    $fixtureEmbedded.SetAttribute('LogicalName', 'Owned.DirectDictionary.baml')
    $fixtureProject.Project.ItemGroup.AppendChild($fixtureEmbedded) | Out-Null
    $fixtureProject.Save((Join-Path $fixtureScratch 'OwnedBamlFixture.csproj'))

    dotnet build OwnedBamlFixture.csproj -t:Rebuild -c Release -p:UseArtifactsOutput=true "-p:ArtifactsPath=$fixtureArtifacts"
    if ($LASTEXITCODE -ne 0) { throw 'Final WPF fixture compilation failed.' }

    $fixtureOutput = Join-Path $fixtureScratch 'generated'
    New-Item -ItemType Directory -Path $fixtureOutput | Out-Null
    Copy-Item -LiteralPath (Join-Path $fixtureObj 'SimpleDictionary.baml'), (Join-Path $fixtureObj 'AuthoredDictionary.baml') -Destination $fixtureOutput
    Copy-Item -LiteralPath (Join-Path $fixtureArtifacts 'bin/OwnedBamlFixture/release/OwnedBamlFixture.dll') -Destination (Join-Path $fixtureOutput 'OwnedBamlFixture.bin')
    Get-ChildItem -LiteralPath $fixtureOutput -File | Get-FileHash -Algorithm SHA256
} finally {
    Pop-Location
}
```

The recorded hashes identify the committed artifacts. Compiler/targeting-pack
updates and PE build paths can change generated bytes; compare generated inputs
and review any deliberate fixture update before replacing them. This recipe
does not execute the fixture assembly or require the disposable directory used
for the original compilation.

