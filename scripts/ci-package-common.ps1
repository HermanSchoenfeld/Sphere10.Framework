# Shared build mechanics; each product owns its package and test policy in build/ci.ps1.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

function Invoke-CIDotNet([string[]]$Arguments) {
	& dotnet @Arguments
	if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments[0]) failed with exit code $LASTEXITCODE." }
}

function Initialize-PackageCI([string]$RepositoryRoot, [string]$ArtifactsRoot, [string]$RegistryUrl, [string]$Version) {
	New-Item -ItemType Directory -Path $ArtifactsRoot -Force | Out-Null
	$env:TEMP = Join-Path $ArtifactsRoot 'temp'
	$env:TMP = $env:TEMP
	New-Item -ItemType Directory -Path $env:TEMP -Force | Out-Null
	$config = Join-Path $ArtifactsRoot 'NuGet.Config'
	$url = [Security.SecurityElement]::Escape($RegistryUrl)
	[IO.File]::WriteAllText($config, "<configuration><packageSources><clear/><add key=`"nuget.org`" value=`"https://api.nuget.org/v3/index.json`"/><add key=`"ci-private`" value=`"$url`" allowInsecureConnections=`"true`"/></packageSources></configuration>")
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
	return [pscustomobject]@{ Root = $RepositoryRoot; Artifacts = $ArtifactsRoot; Packages = $packages; Config = $config; Commit = $commit; Version = $Version }
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

function Assert-CIPackageReferences([string]$ArtifactsRoot, [hashtable]$Expected, [string]$PackageRoot) {
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
			}
		}
	}
	foreach ($id in $Expected.Keys) { if (!$found.ContainsKey($id)) { throw "No consumer restored $id." } }
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
	Invoke-WebRequest "$env:CI_API_URL/repos/$Repository/actions/artifacts/$($artifact.id)/zip" -Headers $headers -OutFile $archive
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
