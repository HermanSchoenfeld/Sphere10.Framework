# Shared build mechanics; each product owns its package and test policy in build/ci.ps1.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

function Invoke-CIDotNet([string[]]$Arguments) {
	& dotnet @Arguments
	if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments[0]) failed with exit code $LASTEXITCODE." }
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
	return [pscustomobject]@{ Root = $RepositoryRoot; Artifacts = $ArtifactsRoot; Packages = $packages; Config = $config; Commit = $commit; Version = $Version; RegistryUrl = $RegistryUrl; RegistryPackagePatterns = $RegistryPackagePatterns }
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
