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

# Private IDs must be restored into an owned cache while third-party files are reused.
$sharedCache = Join-Path $root 'shared-cache'
$ownedCache = Join-Path $root 'owned-cache'
foreach ($id in @('thirdparty.library', 'example.framework', 'example.library', 'exact.private')) {
	New-Item -ItemType Directory -Path (Join-Path $sharedCache "$id/1.0.0") -Force | Out-Null
	Set-Content -LiteralPath (Join-Path $sharedCache "$id/1.0.0/keep.txt") -Value "Original $id"
}
$sourceFile = Join-Path $sharedCache 'thirdparty.library/1.0.0/keep.txt'
$sourceHash = (Get-FileHash -LiteralPath $sourceFile).Hash
$cachePatterns = @('Example.*', 'Exact.Private')
try {
	$created = New-CIPackageCache $ownedCache $sharedCache $cachePatterns
	if ($created -ne $ownedCache) { throw 'The isolated cache helper must return its owned path.' }
	$link = Get-Item -LiteralPath (Join-Path $ownedCache 'thirdparty.library') -Force
	if (!($link.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Third-party package folders must reuse the shared cache through directory links.' }
	if ((Get-FileHash -LiteralPath (Join-Path $ownedCache 'thirdparty.library/1.0.0/keep.txt')).Hash -ne $sourceHash) { throw 'Linked third-party content differs.' }
	foreach ($id in @('example.framework', 'example.library', 'exact.private')) {
		if (Test-Path -LiteralPath (Join-Path $ownedCache $id)) { throw "Private package was reused from the shared cache: $id" }
	}
	New-Item -ItemType Directory -Path (Join-Path $ownedCache 'example.framework/2.0.0') -Force | Out-Null
	Set-Content -LiteralPath (Join-Path $ownedCache 'example.framework/2.0.0/local.txt') 'Owned private package'
	New-CIPackageCache $ownedCache $sharedCache $cachePatterns | Out-Null
	if (Test-Path -LiteralPath (Join-Path $ownedCache 'example.framework')) { throw 'Repeated initialization retained a private package cache entry.' }
	# Also remove a nested directory link without following its target.
	New-Item -ItemType Directory -Path (Join-Path $ownedCache 'owned.private/1.0.0') -Force | Out-Null
	$linkType = if ($IsWindows) { 'Junction' } else { 'SymbolicLink' }
	New-Item -ItemType $linkType -Path (Join-Path $ownedCache 'owned.private/1.0.0/shared-link') -Target (Join-Path $sharedCache 'thirdparty.library') | Out-Null
} finally { Remove-CIPackageCache $ownedCache }
if ((Test-Path -LiteralPath $ownedCache) -or (Get-FileHash -LiteralPath $sourceFile).Hash -ne $sourceHash) { throw 'Cache cleanup removed shared package contents or retained the owned root.' }
foreach ($id in @('example.framework', 'example.library', 'exact.private')) {
	if (!(Test-Path -LiteralPath (Join-Path $sharedCache "$id/1.0.0/keep.txt"))) { throw 'Cache isolation removed an original private package.' }
}
Remove-CIPackageCache $ownedCache # Repeated cleanup is harmless.
foreach ($invalid in @($sharedCache, (Join-Path $sharedCache 'nested'), $root, 'relative-cache')) {
	$rejected = $false
	try { New-CIPackageCache $invalid $sharedCache $cachePatterns | Out-Null } catch { $rejected = $true }
	if (!$rejected) { throw "Unsafe cache destination was accepted: $invalid" }
}
$unowned = Join-Path $root 'unowned-cache'
New-Item -ItemType Directory -Path $unowned | Out-Null
Set-Content -LiteralPath (Join-Path $unowned 'keep.txt') 'Unowned data'
$rejected = $false
try { New-CIPackageCache $unowned $sharedCache $cachePatterns | Out-Null } catch { $rejected = $_.Exception.Message -like '*unowned*' }
if (!$rejected -or !(Test-Path -LiteralPath (Join-Path $unowned 'keep.txt'))) { throw 'An unowned destination was modified.' }
$linkedDestination = Join-Path $root 'linked-destination'
New-Item -ItemType $linkType -Path $linkedDestination -Target $sharedCache | Out-Null
try {
	$rejected = $false
	try { New-CIPackageCache (Join-Path $linkedDestination 'nested') $sharedCache $cachePatterns | Out-Null } catch { $rejected = $_.Exception.Message -like '*directory link*' }
	if (!$rejected) { throw 'A cache destination through a directory link was accepted.' }
} finally { [IO.Directory]::Delete($linkedDestination, $false) }
if ((Get-FileHash -LiteralPath $sourceFile).Hash -ne $sourceHash) { throw 'Invalid destination checks changed the shared cache.' }

$previousEnvironment = @{}
foreach ($name in @('NUGET_PACKAGES', 'GITHUB_SHA', 'NUGET_REGISTRY_READ_TOKEN', 'TEMP', 'TMP')) { $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name) }
$initializationRoot = Join-Path $root 'initialization'
function git { $global:LASTEXITCODE = 0; 'a' * 40 }
function dotnet {
	if (($args -join ' ') -ne 'nuget locals global-packages --list --force-english-output') { throw 'Unexpected SDK invocation during cache initialization.' }
	$global:LASTEXITCODE = 0
	"global-packages: $script:sharedCache"
}
try {
	$env:GITHUB_SHA = 'a' * 40
	$env:NUGET_REGISTRY_READ_TOKEN = $null
	$env:NUGET_PACKAGES = $sharedCache
	$initialized = Initialize-PackageCI $root $initializationRoot $registryUrl '1.0.0-ci.1.2.1' $cachePatterns
	if ($initialized.PreviousNuGetPackages -ne $sharedCache -or $env:NUGET_PACKAGES -ne $initialized.PackageCache) { throw 'Initialization did not preserve and replace the package-cache environment.' }
	$reinitialized = Initialize-PackageCI $root $initializationRoot $registryUrl '1.0.0-ci.1.2.1' $cachePatterns
	$marker = Get-Content -LiteralPath (Join-Path $reinitialized.PackageCache '.sphere10-ci-cache.json') -Raw | ConvertFrom-Json
	if ($marker.SharedCache -ne $sharedCache) { throw 'Repeated initialization linked another owned cache instead of the original shared cache.' }
	Remove-CIPackageCache $reinitialized.PackageCache
	$env:NUGET_PACKAGES = $null
	$initialized = Initialize-PackageCI $root $initializationRoot $registryUrl '1.0.0-ci.1.2.1' $cachePatterns
	if ($initialized.PreviousNuGetPackages -or $env:NUGET_PACKAGES -ne $initialized.PackageCache) { throw 'An initially unset cache environment was not preserved.' }
	if (!(Test-Path -LiteralPath (Join-Path $initialized.PackageCache 'thirdparty.library/1.0.0/keep.txt'))) { throw 'Default cache discovery did not reuse third-party packages.' }
} finally {
	Remove-CIPackageCache (Join-Path $initializationRoot 'nuget-cache')
	foreach ($name in $previousEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name]) }
	Remove-Item Function:git,Function:dotnet
}
'Passed retained-tag recovery, expiry, source identity, source mapping, cache provenance, isolated cache lifecycle and consumer archive-hash checks.'
# The wrong-commit case deliberately invokes a failing native process. GitHub's
# PowerShell wrapper otherwise treats that expected exit code as this step's result.
exit 0
