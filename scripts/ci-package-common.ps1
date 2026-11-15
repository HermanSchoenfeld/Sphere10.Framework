# Shared build mechanics; each product owns its package and test policy in build/ci.ps1.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

function Invoke-CIDotNet([string[]]$Arguments) {
	& dotnet @Arguments
	if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments[0]) failed with exit code $LASTEXITCODE." }
}

function Assert-CIPackageCachePath([string]$Path) {
	if (![IO.Path]::IsPathFullyQualified($Path)) { throw 'The CI package cache requires an absolute path.' }
	$resolved = [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
	if ($resolved -eq [IO.Path]::GetPathRoot($resolved).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)) { throw 'The CI package cache cannot be a filesystem root.' }
	# Never create or remove an owned cache through a junction in its ancestor chain.
	for ($current = $resolved; $current; $current = [IO.Path]::GetDirectoryName($current)) {
		if (Test-Path -LiteralPath $current) {
			$item = Get-Item -LiteralPath $current -Force
			if (!$item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "The CI package cache path contains a file or directory link: $current" }
		}
	}
	return $resolved
}

function Remove-CIPackageCache([string]$Path) {
	$cache = Assert-CIPackageCachePath $Path
	if (!(Test-Path -LiteralPath $cache)) { return }
	$markerPath = Join-Path $cache '.sphere10-ci-cache.json'
	if (!(Test-Path -LiteralPath $markerPath -PathType Leaf) -or ((Get-Item -LiteralPath $markerPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Refusing to remove an unowned CI package cache: $cache" }
	$marker = Get-Content -LiteralPath $markerPath -Raw | ConvertFrom-Json
	if ($marker.Schema -ne 1 -or $marker.Path -ne $cache) { throw "Invalid CI package cache ownership marker: $cache" }
	function Remove-OwnedCacheEntry([string]$Entry) {
		if (!$Entry.StartsWith($cache + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Package cache cleanup escaped its owned root.' }
		$item = Get-Item -LiteralPath $Entry -Force
		if ($item.PSIsContainer) {
			if (!($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
				foreach ($child in Get-ChildItem -LiteralPath $Entry -Force) { Remove-OwnedCacheEntry $child.FullName }
			}
			# Delete the link itself, never recurse through its shared-cache target.
			[IO.Directory]::Delete($Entry, $false)
		} else { [IO.File]::Delete($Entry) }
	}
	foreach ($entry in Get-ChildItem -LiteralPath $cache -Force | Where-Object { $_.FullName -ne $markerPath }) { Remove-OwnedCacheEntry $entry.FullName }
	# Retain ownership if cleanup fails so the same helper can safely resume it.
	[IO.File]::Delete($markerPath)
	[IO.Directory]::Delete($cache, $false)
}

function New-CIPackageCache([string]$Path, [string]$SharedCachePath, [string[]]$RegistryPackagePatterns) {
	if (!$RegistryPackagePatterns -or @($RegistryPackagePatterns | Where-Object { $_ -notmatch '^(?:[A-Za-z0-9_.-]+\*?|\*)$' }).Count) { throw 'CI requires valid registry package ID or prefix patterns.' }
	$cache = Assert-CIPackageCachePath $Path
	if (![IO.Path]::IsPathFullyQualified($SharedCachePath)) { throw 'The shared NuGet cache requires an absolute path.' }
	$shared = [IO.Path]::GetFullPath($SharedCachePath).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
	$separator = [IO.Path]::DirectorySeparatorChar
	if ($cache.Equals($shared, [StringComparison]::OrdinalIgnoreCase) -or $cache.StartsWith($shared + $separator, [StringComparison]::OrdinalIgnoreCase) -or $shared.StartsWith($cache + $separator, [StringComparison]::OrdinalIgnoreCase)) { throw 'The owned and shared package caches must not overlap.' }
	if ((Test-Path -LiteralPath $shared) -and !(Test-Path -LiteralPath $shared -PathType Container)) { throw 'The shared NuGet cache must be a directory.' }
	Remove-CIPackageCache $cache
	New-Item -ItemType Directory -Path $cache | Out-Null
	@{ Schema = 1; Path = $cache; SharedCache = $shared } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $cache '.sphere10-ci-cache.json') -Encoding utf8
	try {
		if (Test-Path -LiteralPath $shared) {
			foreach ($package in Get-ChildItem -LiteralPath $shared -Directory -Force) {
				if ($package.Name -notmatch '^[A-Za-z0-9_.-]+$' -or @($RegistryPackagePatterns | Where-Object { $package.Name -like $_ }).Count) { continue }
				$linkType = if ($IsWindows) { 'Junction' } else { 'SymbolicLink' }
				New-Item -ItemType $linkType -Path (Join-Path $cache $package.Name) -Target $package.FullName | Out-Null
			}
		}
	} catch {
		Remove-CIPackageCache $cache
		throw
	}
	return $cache
}

function Write-CIPackageRestoreConfig([string]$Path, [string]$RegistryUrl, [string[]]$RegistryPackagePatterns, [string]$StagingPath, [string[]]$StagedPackageIds) {
	if (!$RegistryPackagePatterns -or @($RegistryPackagePatterns | Where-Object { $_ -notmatch '^(?:[A-Za-z0-9_.-]+\*?|\*)$' }).Count) {
		throw 'CI requires valid registry package ID or prefix patterns.'
	}
	if ($StagingPath -and (!$StagedPackageIds -or @($StagedPackageIds | Where-Object { $_ -notmatch '^[A-Za-z0-9_.-]+$' }).Count)) {
		throw 'The staged source requires exact evaluated package IDs.'
	}
	$url = [Security.SecurityElement]::Escape($RegistryUrl)
	$patterns = @($RegistryPackagePatterns | Select-Object -Unique | Where-Object { $_ -notin $StagedPackageIds })
	$privateMapping = if ($patterns.Count) {
		$entries = ($patterns | ForEach-Object { "      <package pattern=`"$([Security.SecurityElement]::Escape($_))`" />" }) -join "`n"
		"    <packageSource key=`"ci-private`">`n$entries`n    </packageSource>"
	} else { '' }
	$stagedSource = ''
	$stagedMapping = ''
	if ($StagingPath) {
		$stagedSource = "    <add key=`"ci-staged`" value=`"$([Security.SecurityElement]::Escape($StagingPath))`" />"
		$entries = ($StagedPackageIds | Select-Object -Unique | ForEach-Object { "      <package pattern=`"$([Security.SecurityElement]::Escape($_))`" />" }) -join "`n"
		$stagedMapping = "    <packageSource key=`"ci-staged`">`n$entries`n    </packageSource>"
	}
	[IO.File]::WriteAllText($Path, @"
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="ci-private" value="$url" allowInsecureConnections="true" />
$stagedSource
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
$privateMapping
$stagedMapping
  </packageSourceMapping>
</configuration>
"@)
}

function Initialize-PackageCI([string]$RepositoryRoot, [string]$ArtifactsRoot, [string]$RegistryUrl, [string]$Version, [string[]]$RegistryPackagePatterns) {
	New-Item -ItemType Directory -Path $ArtifactsRoot -Force | Out-Null
	$env:TEMP = Join-Path $ArtifactsRoot 'temp'
	$env:TMP = $env:TEMP
	New-Item -ItemType Directory -Path $env:TEMP -Force | Out-Null
	$config = Join-Path $ArtifactsRoot 'NuGet.Config'
	Write-CIPackageRestoreConfig $config $RegistryUrl $RegistryPackagePatterns
	if ($env:NUGET_REGISTRY_READ_TOKEN) {
		[Environment]::SetEnvironmentVariable('NuGetPackageSourceCredentials_ci-private', "Username=$env:NUGET_REGISTRY_USER;Password=$env:NUGET_REGISTRY_READ_TOKEN;ValidAuthenticationTypes=Basic")
	}
	$commit = ((& git -C $RepositoryRoot rev-parse HEAD 2>$null) -join '').Trim()
	if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[a-f0-9]{40}$') {
		if ($Version -like '0.0.0-validation.*' -and !$env:GITHUB_SHA) {
			# A newly extracted product can be checked locally before its first commit.
			# This reserved version is rejected by every publication command.
			$commit = '0' * 40
		} else { throw 'CI requires a committed source checkout.' }
	}
	if ($env:GITHUB_SHA -and $env:GITHUB_SHA -ne $commit) { throw 'Checkout does not match the workflow commit.' }
	$packages = Join-Path $ArtifactsRoot 'packages'
	New-Item -ItemType Directory -Path $packages -Force | Out-Null
	$previousNuGetPackages = $env:NUGET_PACKAGES
	$sharedCache = $previousNuGetPackages
	if (!$sharedCache) {
		$cacheLocation = (& dotnet nuget locals global-packages --list --force-english-output) -join "`n"
		if ($LASTEXITCODE -ne 0 -or $cacheLocation -notmatch '^global-packages:\s*(.+)\s*$') { throw 'Could not resolve the existing global NuGet package cache.' }
		$sharedCache = $Matches[1].Trim()
	}
	$existingMarker = Join-Path $sharedCache '.sphere10-ci-cache.json'
	if (Test-Path -LiteralPath $existingMarker) {
		# Reinitialization in one process must reuse the original cache, not link another owned cache.
		$sharedCache = (Get-Content -LiteralPath $existingMarker -Raw | ConvertFrom-Json).SharedCache
	}
	$packageCache = New-CIPackageCache (Join-Path ([IO.Path]::GetFullPath($ArtifactsRoot)) 'nuget-cache') $sharedCache $RegistryPackagePatterns
	$env:NUGET_PACKAGES = $packageCache
	return [pscustomobject]@{ Root = $RepositoryRoot; Artifacts = $ArtifactsRoot; Packages = $packages; Config = $config; Commit = $commit; Version = $Version; RegistryUrl = $RegistryUrl; RegistryPackagePatterns = $RegistryPackagePatterns; PackageCache = $packageCache; PreviousNuGetPackages = $previousNuGetPackages }
}

function New-CIConsumerConfig($State, [string[]]$PackageIds) {
	$config = Join-Path $State.Artifacts 'NuGet.Consumer.Config'
	# Exact IDs take precedence over registry prefixes; only this producer's evaluated packages use staging.
	Write-CIPackageRestoreConfig $config $State.RegistryUrl $State.RegistryPackagePatterns $State.Packages $PackageIds
	return $config
}

function Get-CIPackageProjects([string]$RepositoryRoot, [string[]]$Properties) {
	$projects = foreach ($file in Get-ChildItem (Join-Path $RepositoryRoot 'src') -Filter *.csproj -Recurse) {
		$data = & dotnet msbuild $file.FullName -nologo -getProperty:IsPackable,PackageId @Properties
		if ($LASTEXITCODE -ne 0) { throw "Could not evaluate $($file.Name)." }
		$evaluated = ($data -join "`n" | ConvertFrom-Json).Properties
		if ($evaluated.IsPackable -eq 'true') { [pscustomobject]@{ Id = $evaluated.PackageId; Path = $file.FullName } }
	}
	if (!$projects) { throw 'No evaluated packable projects found.' }
	return @($projects)
}

function Assert-CIPackageReferences([string]$ArtifactsRoot, [hashtable]$Expected, [string]$PackageRoot, [string]$RegistryUrl, [string[]]$RegistryPackagePatterns) {
	if ([bool]$RegistryUrl -ne [bool]$RegistryPackagePatterns) { throw 'Registry provenance checks require both the URL and package patterns.' }
	$found = @{}
	$hashes = @{}
	foreach ($id in $Expected.Keys) {
		$archive = Join-Path $PackageRoot "$id.$($Expected[$id]).nupkg"
		$hashes[$id] = [Convert]::ToBase64String([Convert]::FromHexString((Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash))
	}
	foreach ($file in Get-ChildItem (Join-Path $ArtifactsRoot 'obj') -Filter project.assets.json -Recurse) {
		$assets = Get-Content $file.FullName -Raw | ConvertFrom-Json
		foreach ($item in $assets.libraries.PSObject.Properties) {
			$parts = $item.Name.Split('/')
			if ($Expected.ContainsKey($parts[0])) {
				if ($item.Value.type -ne 'package' -or $parts[1] -ne $Expected[$parts[0]]) { throw "Incorrect package reference: $($item.Name) in $($file.FullName)" }
				if ($item.Value.sha512 -notin @($hashes[$parts[0]], "sha512-$($hashes[$parts[0]])")) { throw "Consumer restored different bytes for $($item.Name). Use a new version or the original retained batch." }
				$found[$parts[0]] = $true
			} elseif ($RegistryUrl -and $item.Value.type -eq 'package' -and @($RegistryPackagePatterns | Where-Object { $parts[0] -like $_ }).Count) {
				# NuGet does not apply source mapping to an already cached package. Check the actual cache receipt too.
				$metadataPath = @($assets.packageFolders.PSObject.Properties.Name | ForEach-Object {
					Join-Path $_ "$($item.Value.path)/.nupkg.metadata"
				} | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1)
				$source = $null
				if ($metadataPath.Count) {
					$metadata = Get-Content -LiteralPath $metadataPath[0] -Raw | ConvertFrom-Json
					$sourceProperty = $metadata.PSObject.Properties['source']
					if ($sourceProperty) { $source = [string]$sourceProperty.Value }
				}
				if (!$source -or !$source.TrimEnd('/').Equals($RegistryUrl.TrimEnd('/'), [StringComparison]::OrdinalIgnoreCase)) {
					throw "Cannot verify Gitea provenance for cached dependency $($item.Name). Rerun with NUGET_PACKAGES set to a fresh isolated directory; the shared cache has not been modified."
				}
			}
		}
	}
	foreach ($id in $Expected.Keys) { if (!$found.ContainsKey($id)) { throw "No consumer restored $id." } }
}

function Save-CIArtifact([uri]$Uri, [string]$Token, [string]$OutFile) {
	$origin = $Uri.GetLeftPart([UriPartial]::Authority)
	$handler = [Net.Http.HttpClientHandler]::new()
	$handler.AllowAutoRedirect = $false
	$client = [Net.Http.HttpClient]::new($handler)
	try {
		for ($hop = 0; $hop -lt 5; $hop++) {
			$request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Get, $Uri)
			$null = $request.Headers.TryAddWithoutValidation('Authorization', "token $Token")
			$response = $null
			try {
				$response = $client.Send($request, [Net.Http.HttpCompletionOption]::ResponseHeadersRead)
				if ([int]$response.StatusCode -in @(301,302,303,307,308) -and $response.Headers.Location) {
					$Uri = [uri]::new($Uri, $response.Headers.Location)
					# Gitea redirects archive requests to an authenticated raw endpoint.
					# Retain the job credential only while the destination stays on Gitea.
					if ($Uri.GetLeftPart([UriPartial]::Authority) -ne $origin) { throw 'Artifact redirect changes origin; refusing to forward credentials.' }
					continue
				}
				$null = $response.EnsureSuccessStatusCode()
				$stream = [IO.File]::Create($OutFile)
				try { $null = $response.Content.CopyToAsync($stream).GetAwaiter().GetResult() } finally { $stream.Dispose() }
				return
			} finally {
				if ($response) { $response.Dispose() }
				$request.Dispose()
			}
		}
		throw 'Too many artifact redirects.'
	} finally { $client.Dispose(); $handler.Dispose() }
}

function Restore-CITagBatch($State, [string]$Repository, [string]$ToolsRoot) {
	if ($env:GITHUB_REF -notlike 'refs/tags/*') { return $false }
	if (!$env:CI_API_URL -or !$env:CI_JOB_TOKEN) { throw 'Tag recovery requires the repository Actions API.' }
	$headers = @{ Authorization = "token $env:CI_JOB_TOKEN" }
	$name = "nuget-$env:GITHUB_RUN_ID-tag"
	$page = 1
	do {
		$result = Invoke-RestMethod "$env:CI_API_URL/repos/$Repository/actions/artifacts?limit=100&page=$page" -Headers $headers
		$items = @($result.artifacts)
		if ($items | Where-Object { $_.name -eq $name -and $_.expired }) { throw 'The original tag batch expired. Recover its original archives; do not rebuild this version.' }
		$artifact = $items | Where-Object { $_.name -eq $name -and !$_.expired } | Select-Object -First 1
		$page++
	} while (!$artifact -and $items.Count -eq 100)
	if (!$artifact) { return $false }
	$archive = Join-Path $State.Artifacts 'retained-batch.zip'
	Save-CIArtifact "$env:CI_API_URL/repos/$Repository/actions/artifacts/$($artifact.id)/zip" $env:CI_JOB_TOKEN $archive
	# The artifact is produced by this workflow and contains a flat sealed package batch.
	Add-Type -AssemblyName System.IO.Compression.FileSystem
	$zip = [IO.Compression.ZipFile]::OpenRead($archive)
	try {
		foreach ($entry in $zip.Entries) {
			if (!$entry.Name) { continue }
			if ($entry.FullName -ne $entry.Name) { throw 'Unexpected path in retained package batch.' }
			[IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $State.Packages $entry.Name), $true)
		}
	} finally { $zip.Dispose() }
	& python (Join-Path $ToolsRoot 'nuget-registry.py') verify $State.Packages --repository $Repository --commit $State.Commit | Out-Host
	if ($LASTEXITCODE -ne 0) { throw 'Retained package batch does not match this source commit.' }
	$manifest = Get-Content (Join-Path $State.Packages 'release-manifest.json') -Raw | ConvertFrom-Json
	if ($manifest.version -ne $State.Version) { throw 'Retained package version differs from the tag.' }
	return $true
}
