#requires -Version 7.2
param([Parameter(Mandatory)][string]$ArtifactsRoot)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../ci-package-common.ps1')
$root = Join-Path $ArtifactsRoot ([guid]::NewGuid().ToString('N'))
$batch = Join-Path $root 'original'
New-Item -ItemType Directory -Path $batch -Force | Out-Null
$repository = 'example/Library'
$version = '1.0.0-ci.1.2.1'
$commit = 'a' * 40
$package = Join-Path $batch "Example.Library.$version.nupkg"
Set-Content $package 'Package fixture bytes'
$manifest = @{ schema = 1; repository = $repository; version = $version; commit = $commit; files = @(@{ name = [IO.Path]::GetFileName($package); id = 'Example.Library'; sha256 = (Get-FileHash $package).Hash.ToLowerInvariant() }) }
$manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $batch 'release-manifest.json')
$script:archive = Join-Path $root 'retained.zip'
Compress-Archive -Path "$batch/*" -DestinationPath $script:archive
$state = [pscustomobject]@{ Artifacts = $root; Packages = (Join-Path $root 'restored'); Commit = $commit; Version = $version }
New-Item -ItemType Directory -Path $state.Packages -Force | Out-Null
$env:GITHUB_REF = 'refs/tags/v1.0.0-ci.1.2.1'
$env:GITHUB_RUN_ID = '77'
$env:CI_API_URL = 'http://unused.test/api/v1'
$env:CI_JOB_TOKEN = 'test-only'
$script:expired = $false
function Invoke-RestMethod { param($Uri, $Headers) @{ artifacts = @([pscustomobject]@{ id = 1; name = 'nuget-77-tag'; expired = $script:expired }) } }
function Save-CIArtifact { param($Uri, $Token, $OutFile) Copy-Item $script:archive $OutFile }
$restored = Restore-CITagBatch $state $repository (Split-Path $PSScriptRoot)
if ($restored -isnot [bool] -or !$restored) { throw 'Tag recovery must return one Boolean, not tool output.' }
if ((Get-FileHash (Join-Path $state.Packages ([IO.Path]::GetFileName($package)))).Hash -ne (Get-FileHash $package).Hash) { throw 'Retained bytes changed.' }
$script:expired = $true
$rejected = $false
try { Restore-CITagBatch $state $repository (Split-Path $PSScriptRoot) | Out-Null } catch { $rejected = $_.Exception.Message -like '*expired*' }
if (!$rejected) { throw 'Expired tag batches must require recovery.' }
$script:expired = $false
$state.Commit = 'b' * 40
$rejected = $false
try { Restore-CITagBatch $state $repository (Split-Path $PSScriptRoot) | Out-Null } catch { $rejected = $_.Exception.Message -like '*source commit*' }
if (!$rejected) { throw 'A batch from another commit must be rejected.' }
$consumer = Join-Path $root 'consumer'
New-Item -ItemType Directory -Path "$consumer/obj/Test" -Force | Out-Null
$hash = [Convert]::ToBase64String([Convert]::FromHexString((Get-FileHash $package -Algorithm SHA512).Hash))
$assets = @{ libraries = @{ "Example.Library/$version" = @{ type = 'package'; sha512 = $hash } } }
$assets | ConvertTo-Json -Depth 5 | Set-Content "$consumer/obj/Test/project.assets.json"
Assert-CIPackageReferences $consumer @{ 'Example.Library' = $version } $batch

# Exercise NuGet's own matching rules, including a private exact ID superseded by staging.
$sdk = (& dotnet --version).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot locate the SDK for NuGet configuration checks.' }
$sdkRoot = Join-Path (Split-Path (Get-Command dotnet).Source -Parent) "sdk/$sdk"
Add-Type -Path (Join-Path $sdkRoot 'NuGet.Common.dll')
Add-Type -Path (Join-Path $sdkRoot 'NuGet.Configuration.dll')
$registryUrl = 'https://packages.invalid/nuget/index.json'
$registryPatterns = @('Example.*', 'Example.Library')
$producerConfig = Join-Path $root 'NuGet.Producer.Config'
Write-CIPackageRestoreConfig $producerConfig $registryUrl $registryPatterns
$consumerState = [pscustomobject]@{ Artifacts = $root; Packages = $batch; RegistryUrl = $registryUrl; RegistryPackagePatterns = $registryPatterns }
$consumerConfig = New-CIConsumerConfig $consumerState @('Example.Library')
function Assert-MappedSource([string]$Config, [string]$PackageId, [string]$ExpectedSource) {
	$settings = [NuGet.Configuration.Settings]::LoadSpecificSettings((Split-Path $Config), (Split-Path $Config -Leaf))
	$mapping = [NuGet.Configuration.PackageSourceMapping]::GetPackageSourceMapping($settings)
	$actual = @($mapping.GetConfiguredPackageSources($PackageId))
	if ($actual.Count -ne 1 -or $actual[0] -cne $ExpectedSource) { throw "Unexpected source mapping for $PackageId in $Config." }
}
Assert-MappedSource $producerConfig 'Example.Library' 'ci-private'
Assert-MappedSource $consumerConfig 'Example.Library' 'ci-staged'
Assert-MappedSource $consumerConfig 'Example.Library.Extensions' 'ci-private'
Assert-MappedSource $consumerConfig 'Example.Framework' 'ci-private'
Assert-MappedSource $consumerConfig 'ThirdParty.Library' 'nuget.org'
$rejected = $false
try { New-CIConsumerConfig $consumerState @('Example.*') | Out-Null } catch { $rejected = $_.Exception.Message -like '*exact evaluated package IDs*' }
if (!$rejected) { throw 'Staging must not accept a package prefix in place of evaluated IDs.' }

# Cached packages bypass source mapping, so only a matching registry receipt is accepted.
$cache = Join-Path $root 'cache'
$dependencyPath = 'example.framework/2.0.0'
$metadataPath = Join-Path $cache "$dependencyPath/.nupkg.metadata"
New-Item -ItemType Directory -Path (Split-Path $metadataPath) -Force | Out-Null
$assets.packageFolders = @{ $cache = @{} }
$assets.libraries['Example.Framework/2.0.0'] = @{ type = 'package'; path = $dependencyPath; sha512 = 'fixture' }
$assets | ConvertTo-Json -Depth 5 | Set-Content "$consumer/obj/Test/project.assets.json"
@{ source = $registryUrl } | ConvertTo-Json | Set-Content $metadataPath
Assert-CIPackageReferences $consumer @{ 'Example.Library' = $version } $batch $registryUrl $registryPatterns
@{ source = 'https://api.nuget.org/v3/index.json' } | ConvertTo-Json | Set-Content $metadataPath
$rejected = $false
try { Assert-CIPackageReferences $consumer @{ 'Example.Library' = $version } $batch $registryUrl $registryPatterns } catch { $rejected = $_.Exception.Message -like '*fresh isolated directory*' }
if (!$rejected) { throw 'A dependency cached from a different registry must be rejected.' }
Remove-Item -LiteralPath $metadataPath
$rejected = $false
try { Assert-CIPackageReferences $consumer @{ 'Example.Library' = $version } $batch $registryUrl $registryPatterns } catch { $rejected = $_.Exception.Message -like '*provenance*' }
if (!$rejected) { throw 'A dependency without a cache source receipt must be rejected.' }

Add-Content $package 'Different archive bytes'
$rejected = $false
try { Assert-CIPackageReferences $consumer @{ 'Example.Library' = $version } $batch } catch { $rejected = $_.Exception.Message -like '*different bytes*' }
if (!$rejected) { throw 'Stale cached package bytes must be rejected.' }
'Passed retained-tag recovery, expiry, source identity, source mapping, cache provenance and consumer archive-hash checks.'
# The wrong-commit case deliberately invokes a failing native process. GitHub's
# PowerShell wrapper otherwise treats that expected exit code as this step's result.
exit 0
