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
function Invoke-WebRequest { param($Uri, $Headers, $OutFile) Copy-Item $script:archive $OutFile }
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
Add-Content $package 'Different archive bytes'
$rejected = $false
try { Assert-CIPackageReferences $consumer @{ 'Example.Library' = $version } $batch } catch { $rejected = $_.Exception.Message -like '*different bytes*' }
if (!$rejected) { throw 'Stale cached package bytes must be rejected.' }
'Passed retained-tag recovery, expiry, source identity and consumer archive-hash checks.'
# The wrong-commit case deliberately invokes a failing native process. GitHub's
# PowerShell wrapper otherwise treats that expected exit code as this step's result.
exit 0
