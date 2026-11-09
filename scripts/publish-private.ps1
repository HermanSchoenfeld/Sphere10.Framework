#requires -Version 5.1
<#
.SYNOPSIS
Packs Sphere10 Framework under a private prerelease version and adds it to a local folder feed.
.DESCRIPTION
The sibling of publish.ps1 for a private feed: a folder that NuGet restores from, not nuget.org.
Every packable library under src/ in the Windows solution is packed with one version. The packages
are written in NuGet's hierarchical folder layout (<id>/<version>/<id>.<version>.nupkg with its
.sha512, .nuspec and .snupkg, all lower case), which restore reads directly; dotnet nuget push would
write a flat layout instead.

The default version is <VersionPrefix>-<Label>.<yyyyMMdd>.<n>, using the next number the feed does
not already hold. An existing version is never replaced. Nothing is added unless every package
builds, every Sphere10 dependency resolves to this set or the feed, and the checkout did not change
while packing. Builds use their own artifacts folder, so the checkout's bin and obj folders are not
touched. Use -PackOnly to leave the packages in -OutputDirectory for a caller that adds its own
packages before publishing. Use -WhatIf to see the version and package list without packing.

The script writes an object with the Version, OutputDirectory and Packages to the pipeline; Packages
are the feed paths once added, or the staged paths with -PackOnly. Build output and progress go to
the information stream. Default staging under publish/.staging is removed after a successful
publish; folders passed with -OutputDirectory or -ArtifactsPath are left for the caller.
.EXAMPLE
.\scripts\publish-private.ps1
.EXAMPLE
.\scripts\publish-private.ps1 -Feed D:\Packages\sphere10 -Label preview
.EXAMPLE
.\scripts\publish-private.ps1 -Version 3.1.3-private.20261108.1 -PackOnly -OutputDirectory C:\Temp\stage
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
	# Folder feed. Defaults to SPHERE10_PRIVATE_FEED, then a nuget-private folder beside the repository.
	[string]$Feed,
	# Exact package version. Defaults to <VersionPrefix>-<Label>.<yyyyMMdd>.<next free number>.
	[string]$Version,
	[ValidatePattern('^[0-9A-Za-z-]+$')]
	[string]$Label = 'private',
	# Where the packages are built. Defaults to publish/.staging/private-<version>, removed once published.
	[string]$OutputDirectory,
	# MSBuild artifacts root. Defaults to a build folder beside the output directory.
	[string]$ArtifactsPath,
	[switch]$PackOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$stageRoot = Join-Path $repositoryRoot 'publish/.staging'
$pathComparison = if ([IO.Path]::DirectorySeparatorChar -eq '\') { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
	throw 'dotnet SDK not found on PATH. Install the .NET SDK and try again.'
}
if ([string]::IsNullOrWhiteSpace($Feed)) {
	$Feed = $env:SPHERE10_PRIVATE_FEED
}
if ([string]::IsNullOrWhiteSpace($Feed)) {
	$Feed = Join-Path (Split-Path -Parent $repositoryRoot) 'nuget-private'
}
$Feed = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Feed)
if (Test-Path -LiteralPath $Feed) {
	if (@(Get-ChildItem -LiteralPath $Feed -File -Filter '*.nupkg').Count) {
		throw "The feed has packages at its root, so NuGet reads it as a flat feed: $Feed. Move them into the hierarchical layout first."
	}
}

function Get-ProjectProperties([string]$project, [string[]]$names) {
	# Two or more names make MSBuild return JSON.
	$arguments = @('msbuild', $project, '-nologo', '-p:BuildRevision=0') + @($names | ForEach-Object { "-getProperty:$_" })
	$output = & dotnet @arguments
	if ($LASTEXITCODE -ne 0) {
		throw "Project evaluation failed: $project"
	}
	return (($output -join "`n") | ConvertFrom-Json).Properties
}

function Get-PackageManifest([string]$packagePath) {
	$archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
	try {
		$entries = @($archive.Entries | Where-Object { $_.FullName -notmatch '[/\\]' -and $_.Name -like '*.nuspec' })
		if ($entries.Count -ne 1) {
			throw "Package must contain one root nuspec: $packagePath"
		}
		$reader = New-Object IO.StreamReader($entries[0].Open())
		try {
			$document = New-Object Xml.XmlDocument
			$document.XmlResolver = $null
			$document.LoadXml($reader.ReadToEnd())
			return $document
		} finally {
			$reader.Dispose()
		}
	} finally {
		$archive.Dispose()
	}
}

function Get-FeedVersions([string]$packageId) {
	$id = $packageId.ToLowerInvariant()
	$folder = Join-Path $Feed $id
	if (-not (Test-Path -LiteralPath $folder)) {
		return @()
	}
	return @(Get-ChildItem -LiteralPath $folder -Directory |
		Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "$id.$($_.Name).nupkg") } | ForEach-Object { $_.Name })
}

# Adds one package and its symbols; the package file appears last, so restore never sees a partial version.
function Add-FeedPackage([string]$packagePath) {
	$metadata = (Get-PackageManifest $packagePath).package.metadata
	$id = $metadata.id.ToLowerInvariant()
	$packageVersion = $metadata.version.ToLowerInvariant()
	$folder = Join-Path $Feed "$id/$packageVersion"
	$target = Join-Path $folder "$id.$packageVersion.nupkg"
	if (Test-Path -LiteralPath $target) {
		throw "$($metadata.id) $($metadata.version) appeared in the feed during this run."
	}
	New-Item -ItemType Directory -Path $folder -Force | Out-Null
	$staging = Join-Path $folder ".adding-$([guid]::NewGuid().ToString('N'))"
	Copy-Item -LiteralPath $packagePath -Destination $staging
	$archive = [IO.Compression.ZipFile]::OpenRead($staging)
	try {
		$entry = $archive.Entries | Where-Object { $_.FullName -notmatch '[/\\]' -and $_.Name -like '*.nuspec' } | Select-Object -First 1
		[IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $folder "$id.nuspec"), $true)
	} finally {
		$archive.Dispose()
	}
	$stream = [IO.File]::OpenRead($staging)
	$hasher = [Security.Cryptography.SHA512]::Create()
	try {
		$hash = [Convert]::ToBase64String($hasher.ComputeHash($stream))
	} finally {
		$hasher.Dispose()
		$stream.Dispose()
	}
	[IO.File]::WriteAllText((Join-Path $folder "$id.$packageVersion.nupkg.sha512"), $hash)
	$symbols = [IO.Path]::ChangeExtension($packagePath, '.snupkg')
	if (Test-Path -LiteralPath $symbols) {
		Copy-Item -LiteralPath $symbols -Destination (Join-Path $folder "$id.$packageVersion.snupkg")
	}
	Move-Item -LiteralPath $staging -Destination $target
	return $target
}

# Removes a staging folder this script created, leaving anything outside publish/.staging or behind a link.
function Remove-StagingDirectory([string]$path) {
	if ([string]::IsNullOrWhiteSpace($path) -or -not (Test-Path -LiteralPath $path)) {
		return
	}
	$resolvedRoot = (Resolve-Path -LiteralPath $stageRoot).ProviderPath.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
	$resolvedTarget = (Resolve-Path -LiteralPath $path).ProviderPath
	if (-not $resolvedTarget.StartsWith($resolvedRoot, $pathComparison) -or
		((Get-Item -LiteralPath $resolvedTarget).Attributes -band [IO.FileAttributes]::ReparsePoint) -or
		@(Get-ChildItem -LiteralPath $resolvedTarget -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) {
		Write-Warning "Left an unexpected staging directory in place: $resolvedTarget"
		return
	}
	try {
		if ([IO.Path]::DirectorySeparatorChar -eq '\' -and -not $resolvedTarget.StartsWith('\\')) {
			# The extended path form lets Windows PowerShell 5.1 delete build outputs deeper than 260 characters.
			[IO.Directory]::Delete('\\?\' + $resolvedTarget, $true)
		} else {
			Remove-Item -LiteralPath $resolvedTarget -Recurse -Force
		}
	} catch {
		Write-Warning "The packages were published, but staging could not be removed: $resolvedTarget. $($_.Exception.Message)"
	}
}

# Fingerprints tracked edits and untracked files, so an edit made while packing is detected.
function Get-SourceFingerprint {
	if (-not (Get-Command git -ErrorAction SilentlyContinue) -or -not (Test-Path -LiteralPath (Join-Path $repositoryRoot '.git'))) {
		return $null
	}
	$builder = New-Object Text.StringBuilder
	[void]$builder.AppendLine((& git -C $repositoryRoot rev-parse --verify --quiet HEAD) -join '')
	[void]$builder.AppendLine((& git -C $repositoryRoot -c core.quotepath=off diff HEAD --binary) -join "`n")
	foreach ($path in @(& git -C $repositoryRoot -c core.quotepath=off ls-files --others --exclude-standard)) {
		$item = Get-Item -LiteralPath (Join-Path $repositoryRoot $path) -ErrorAction SilentlyContinue
		if ($item) {
			[void]$builder.AppendLine("$path|$($item.Length)|$($item.LastWriteTimeUtc.Ticks)")
		} else {
			[void]$builder.AppendLine("$path|missing")
		}
	}
	$hasher = [Security.Cryptography.SHA256]::Create()
	try {
		return [BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($builder.ToString()))).Replace('-', '')
	} finally {
		$hasher.Dispose()
	}
}

# The Windows solution is the package build boundary; legacy mobile projects sit outside it.
$solutionPath = Join-Path $repositoryRoot 'src/Sphere10.Framework (Win).sln'
if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
	throw "Solution not found: $solutionPath"
}
$sourceRoot = (Join-Path $repositoryRoot 'src') + [IO.Path]::DirectorySeparatorChar
$projects = @(& dotnet sln $solutionPath list | Where-Object { $_ -match '\.csproj$' } |
	ForEach-Object { [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $solutionPath) $_.Trim())) } |
	Where-Object { $_.StartsWith($sourceRoot, [StringComparison]::OrdinalIgnoreCase) } |
	ForEach-Object {
		$properties = Get-ProjectProperties $_ @('IsPackable', 'PackageId', 'VersionPrefix')
		if ($properties.IsPackable -eq 'true') {
			[pscustomobject]@{ Project = $_; PackageId = $properties.PackageId; VersionPrefix = $properties.VersionPrefix }
		}
	})
if ($projects.Count -eq 0) {
	throw "No packable projects found in $solutionPath."
}

if ([string]::IsNullOrWhiteSpace($Version)) {
	$date = Get-Date -Format 'yyyyMMdd'
	$pattern = "^[0-9.]+-$([regex]::Escape($Label.ToLowerInvariant()))\.$date\.(\d+)$"
	$used = @($projects | ForEach-Object { Get-FeedVersions $_.PackageId } | ForEach-Object { if ($_ -match $pattern) { [int]$Matches[1] } })
	$number = 1
	if ($used.Count) {
		$number = ($used | Measure-Object -Maximum).Maximum + 1
	}
	$Version = "$($projects[0].VersionPrefix)-$Label.$date.$number"
}
foreach ($project in $projects) {
	if ((Get-FeedVersions $project.PackageId) -contains $Version.ToLowerInvariant()) {
		throw "$($project.PackageId) $Version is already in $Feed. Choose a new version; published versions are never replaced."
	}
}

$ownedOutputDirectory = $null
$ownedArtifactsPath = $null
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
	$OutputDirectory = Join-Path $stageRoot "private-$Version"
	$ownedOutputDirectory = $OutputDirectory
}
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
if ([string]::IsNullOrWhiteSpace($ArtifactsPath)) {
	$ArtifactsPath = Join-Path (Split-Path -Parent $OutputDirectory) "$(Split-Path -Leaf $OutputDirectory)-build"
	if ($ownedOutputDirectory) {
		$ownedArtifactsPath = $ArtifactsPath
	}
}

Write-Host "Framework $Version, $($projects.Count) packages" -ForegroundColor Cyan
foreach ($project in $projects) {
	Write-Host " - $($project.PackageId)"
}
$action = 'Pack'
if (-not $PackOnly) {
	$action = "Pack and add to $Feed"
}
if (-not $PSCmdlet.ShouldProcess("Sphere10 Framework $Version", $action)) {
	return
}
if (Test-Path -LiteralPath $OutputDirectory) {
	if (@(Get-ChildItem -LiteralPath $OutputDirectory -File -Filter '*.nupkg' | Where-Object { $_.Name -like 'Sphere10.*' }).Count) {
		throw "The output directory already holds Sphere10 packages: $OutputDirectory"
	}
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$fingerprint = Get-SourceFingerprint
foreach ($project in $projects) {
	Write-Host "Packing $($project.PackageId)" -ForegroundColor Green
	# Build output goes to the information stream, keeping the pipeline for the result object.
	& dotnet pack $project.Project -c Release -p:BuildRevision=0 "-p:Version=$Version" "-p:PackageVersion=$Version" --artifacts-path $ArtifactsPath -o $OutputDirectory | Write-Host
	if ($LASTEXITCODE -ne 0) {
		throw "Packing $($project.PackageId) failed (exit code $LASTEXITCODE). Nothing was added to the feed."
	}
}
if ($fingerprint -and (Get-SourceFingerprint) -ne $fingerprint) {
	throw "$repositoryRoot changed while packing, so the packages match no single source state. Nothing was added; rerun when the checkout is quiet."
}

$packages = @($projects | ForEach-Object { Get-Item -LiteralPath (Join-Path $OutputDirectory "$($_.PackageId).$Version.nupkg") -ErrorAction SilentlyContinue })
if ($packages.Count -ne $projects.Count) {
	throw "Expected $($projects.Count) packages in $OutputDirectory but found $($packages.Count). Nothing was added to the feed."
}
$supplied = @{}
foreach ($project in $projects) {
	$supplied[$project.PackageId.ToLowerInvariant()] = $true
}
foreach ($package in $packages) {
	$manifest = Get-PackageManifest $package.FullName
	foreach ($dependency in @($manifest.SelectNodes('//*[local-name()="dependency"]') | Where-Object { $_.id -like 'Sphere10.*' })) {
		$dependencyVersion = $dependency.version.Trim('[', ']', '(', ')').Split(',')[0].ToLowerInvariant()
		$inSet = $supplied.ContainsKey($dependency.id.ToLowerInvariant()) -and $dependencyVersion -eq $Version.ToLowerInvariant()
		if (-not $inSet -and (Get-FeedVersions $dependency.id) -notcontains $dependencyVersion) {
			throw "$($package.Name) needs $($dependency.id) $($dependency.version), which neither this set nor the feed holds. Nothing was added."
		}
	}
}

$packagePaths = @($packages | ForEach-Object { $_.FullName })
if (-not $PackOnly) {
	New-Item -ItemType Directory -Path $Feed -Force | Out-Null
	$packagePaths = @(foreach ($package in $packages) {
		Add-FeedPackage $package.FullName
	})
	Write-Host "Added $($packages.Count) packages to $Feed" -ForegroundColor Green
	# Default staging belongs to this script and is removed once published; supplied folders are left alone.
	Remove-StagingDirectory $ownedOutputDirectory
	Remove-StagingDirectory $ownedArtifactsPath
}

[pscustomobject]@{
	Version = $Version
	OutputDirectory = $OutputDirectory
	Packages = $packagePaths
}
