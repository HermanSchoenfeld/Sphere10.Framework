#requires -Version 5.1
<#
.SYNOPSIS
Cleans and packs Sphere10 Framework for NuGet distribution.
.EXAMPLE
.\pack.ps1
#>
[CmdletBinding()]
param(
	[ValidateNotNullOrEmpty()]
	[string]$outputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

if (-not $PSBoundParameters.ContainsKey('outputDirectory')) {
	$outputDirectory = Join-Path $PSScriptRoot 'nuget-packages'
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
	throw 'dotnet SDK not found on PATH. Install the .NET SDK and try again.'
}

$solutionPath = Join-Path $PSScriptRoot 'src/Sphere10.Framework (Win).sln'
if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
	throw "Solution not found: $solutionPath"
}

$outputRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($outputDirectory)
$stageRoot = Join-Path $PSScriptRoot 'publish/.staging'
$stageDirectory = Join-Path $stageRoot "framework-$([guid]::NewGuid().ToString('N'))"
$packageNamePattern = '^(?:Sphere10\.Framework(?:\.[A-Za-z][A-Za-z0-9_-]*)*|Sphere10\.HashLib4CSharp|Sphere10\.VisualRenderer)\.[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?\.(?:nupkg|snupkg)$'
$pathComparison = if ([IO.Path]::DirectorySeparatorChar -eq '\') { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }

function Remove-StagingDirectory {
	if (-not (Test-Path -LiteralPath $stageDirectory)) {
		return
	}
	$resolvedRoot = (Resolve-Path -LiteralPath $stageRoot).ProviderPath.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
	$resolvedTarget = (Resolve-Path -LiteralPath $stageDirectory).ProviderPath
	if (-not $resolvedTarget.StartsWith($resolvedRoot, $pathComparison) -or
		((Get-Item -LiteralPath $resolvedTarget).Attributes -band [IO.FileAttributes]::ReparsePoint) -or
		@(Get-ChildItem -LiteralPath $resolvedTarget -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) {
		throw "Refusing to clean an unexpected staging directory: $resolvedTarget"
	}
	Remove-Item -LiteralPath $resolvedTarget -Recurse -Force
}

$buildArguments = @('-c', 'Release')

Write-Host "Cleaning solution: $solutionPath" -ForegroundColor Cyan
& dotnet clean $solutionPath @buildArguments
if ($LASTEXITCODE -ne 0) {
	throw "Solution clean failed (exit code $LASTEXITCODE). Packing was not started."
}

# Keep the last successful packages until the complete new package set is built.
New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
foreach ($directoryPath in @((Join-Path $PSScriptRoot 'publish'), $stageRoot)) {
	if ((Get-Item -LiteralPath $directoryPath).Attributes -band [IO.FileAttributes]::ReparsePoint) {
		throw "The package staging path must not be a symbolic link or junction: $directoryPath"
	}
}
New-Item -ItemType Directory -Path $stageDirectory | Out-Null
try {
	Write-Host "Packing framework to: $stageDirectory" -ForegroundColor Cyan
	& dotnet pack $solutionPath @buildArguments -o $stageDirectory
	if ($LASTEXITCODE -ne 0) {
		throw "Solution packing failed (exit code $LASTEXITCODE). Existing packages have been retained."
	}

	$packages = @(Get-ChildItem -LiteralPath $stageDirectory -File -Filter '*.nupkg')
	$symbols = @(Get-ChildItem -LiteralPath $stageDirectory -File -Filter '*.snupkg')
	if ($packages.Count -eq 0 -or $symbols.Count -ne $packages.Count) {
		throw 'Packing must produce framework packages and matching symbols. Existing packages have been retained.'
	}
	foreach ($package in @($packages + $symbols)) {
		if ($package.Name -notmatch $packageNamePattern) {
			throw "Unexpected package produced by the solution: $($package.Name). Existing packages have been retained."
		}
	}
	foreach ($package in $packages) {
		$symbolName = [IO.Path]::ChangeExtension($package.Name, '.snupkg')
		if (-not (Test-Path -LiteralPath (Join-Path $stageDirectory $symbolName) -PathType Leaf)) {
			throw "Matching symbols missing for $($package.Name). Existing packages have been retained."
		}
	}

	New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
	$outputRoot = (Resolve-Path -LiteralPath $outputRoot).ProviderPath
	if ((Get-Item -LiteralPath $outputRoot).Attributes -band [IO.FileAttributes]::ReparsePoint) {
		throw "The package output directory must not be a symbolic link or junction: $outputRoot"
	}
	# Replace only framework package artifacts; other packages and files are preserved.
	foreach ($package in @(Get-ChildItem -LiteralPath $outputRoot -File | Where-Object { $_.Name -match $packageNamePattern })) {
		$resolvedPackage = (Resolve-Path -LiteralPath $package.FullName).ProviderPath
		if (-not [IO.Path]::GetDirectoryName($resolvedPackage).Equals($outputRoot, $pathComparison) -or
			($package.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
			throw "Refusing to replace an unexpected package path: $resolvedPackage"
		}
		Remove-Item -LiteralPath $resolvedPackage -Force
	}
	foreach ($package in @($packages + $symbols)) {
		Copy-Item -LiteralPath $package.FullName -Destination (Join-Path $outputRoot $package.Name)
	}
} finally {
	Remove-StagingDirectory
}

Write-Host "Packing succeeded. Framework packages are ready in $outputRoot" -ForegroundColor Green
