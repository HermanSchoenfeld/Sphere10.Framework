#requires -Version 5.1
<#
.SYNOPSIS
Publishes the Sphere10 Framework packages produced by pack.ps1.
.DESCRIPTION
Uses -ApiKey, NUGET_API_KEY, or a hidden prompt. Validates package identities, versions, and
matching symbols before publishing. Use -WhatIf to validate and preview without an API
key or network access. Use -Confirm:$false for an explicitly authorized CI run.
.EXAMPLE
.\publish.ps1 -IncludeSymbols -WhatIf
.EXAMPLE
.\publish.ps1 -IncludeSymbols
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
	[string]$apiKey,
	[ValidateNotNullOrEmpty()]
	[string]$source = 'https://api.nuget.org/v3/index.json',
	[switch]$includeSymbols,
	[ValidateNotNullOrEmpty()]
	[string]$symbolSource = 'https://symbols.nuget.org/api/v2/symbolpackage',
	[ValidateNotNullOrEmpty()]
	[string]$packagesDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

if (-not $PSBoundParameters.ContainsKey('packagesDirectory')) {
	$packagesDirectory = Join-Path $PSScriptRoot 'nuget-packages'
}
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Read-ApiKey {
	$secureKey = Read-Host -Prompt 'Enter NuGet API key (input hidden)' -AsSecureString
	try {
		$keyPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey)
		try {
			return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($keyPointer)
		} finally {
			[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($keyPointer)
		}
	} finally {
		$secureKey.Dispose()
	}
}

function Test-PackageVersion {
	param([string]$version)

	# NuGet accepts one to four numeric components, with optional SemVer release and build labels.
	$versionMatch = [regex]::Match($version, '\A(?<core>[0-9]+(?:\.[0-9]+){0,3})(?:-(?<release>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?\z')
	if (-not $versionMatch.Success) {
		return $false
	}
	foreach ($component in $versionMatch.Groups['core'].Value.Split('.')) {
		$number = 0
		if (-not [int]::TryParse($component, [ref]$number)) {
			return $false
		}
	}
	foreach ($label in $versionMatch.Groups['release'].Value.Split('.')) {
		if ($label -match '^0[0-9]+$') {
			return $false
		}
	}
	return $true
}

function Get-PackageMetadata {
	param([string]$packagePath)

	$archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
	try {
		$entries = @($archive.Entries | Where-Object { $_.FullName -notmatch '[/\\]' -and $_.Name -like '*.nuspec' })
		if ($entries.Count -ne 1 -or $entries[0].Length -gt 1048576) {
			throw "Package must contain one valid root nuspec: $packagePath"
		}
		$settings = New-Object Xml.XmlReaderSettings
		$settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
		$settings.XmlResolver = $null
		$stream = $entries[0].Open()
		try {
			$reader = [Xml.XmlReader]::Create($stream, $settings)
			try {
				$document = New-Object Xml.XmlDocument
				$document.XmlResolver = $null
				$document.Load($reader)
			} finally {
				$reader.Dispose()
			}
		} finally {
			$stream.Dispose()
		}
		$metadata = $document.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
		if ($null -eq $metadata) {
			throw "Package metadata is missing: $packagePath"
		}
		$idNode = $metadata.SelectSingleNode('*[local-name()="id"]')
		$versionNode = $metadata.SelectSingleNode('*[local-name()="version"]')
		if ($null -eq $idNode -or $idNode.InnerText -notmatch '^Sphere10\.(?:Framework(?:\.[A-Za-z][A-Za-z0-9_-]*)*|HashLib4CSharp)$' -or
			$null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
			throw "Package must have a Sphere10 Framework or Sphere10.HashLib4CSharp identity and version: $packagePath"
		}
		if (-not (Test-PackageVersion $versionNode.InnerText)) {
			throw "Invalid NuGet package version '$($versionNode.InnerText)': $packagePath"
		}
		return [pscustomobject]@{ Id = $idNode.InnerText; Version = $versionNode.InnerText }
	} finally {
		$archive.Dispose()
	}
}

if (-not (Test-Path -LiteralPath $packagesDirectory -PathType Container)) {
	throw "Package folder not found: $packagesDirectory. Run pack.ps1 first."
}
$packagesRoot = (Resolve-Path -LiteralPath $packagesDirectory).ProviderPath
$packages = @(Get-ChildItem -LiteralPath $packagesRoot -File -Filter '*.nupkg' |
	Where-Object { $_.Name -match '^(?:Sphere10\.Framework(?:\.|$)|Sphere10\.HashLib4CSharp\.)' } | Sort-Object Name)
if ($packages.Count -eq 0) {
	throw "No Sphere10 Framework packages found in $packagesRoot. Run pack.ps1 first."
}

# Validate the entire batch before requesting credentials or publishing its first package.
$packageIds = @{}
$symbolPackages = @()
foreach ($package in $packages) {
	$metadata = Get-PackageMetadata $package.FullName
	if ($package.Name -ine "$($metadata.Id).$($metadata.Version).nupkg") {
		throw "Package filename does not match its identity and version: $($package.Name)"
	}
	if ($packageIds.ContainsKey($metadata.Id)) {
		throw "Multiple versions of $($metadata.Id) found in $packagesRoot. Run pack.ps1 to replace stale framework versions."
	}
	$packageIds[$metadata.Id] = $metadata.Version
	if ($includeSymbols) {
		$symbolPackagePath = [IO.Path]::ChangeExtension($package.FullName, '.snupkg')
		if (-not (Test-Path -LiteralPath $symbolPackagePath -PathType Leaf)) {
			throw "Matching symbol package not found: $symbolPackagePath. Run pack.ps1 first."
		}
		$symbolMetadata = Get-PackageMetadata $symbolPackagePath
		if ($symbolMetadata.Id -ine $metadata.Id -or $symbolMetadata.Version -ine $metadata.Version) {
			throw "Package and symbol identities or versions do not match: $($package.Name)"
		}
		$symbolPackages += Get-Item -LiteralPath $symbolPackagePath
	}
}

Write-Host "Packages to publish to ${source}:" -ForegroundColor Cyan
foreach ($package in $packages) {
	Write-Host " - $($package.Name)"
}
$publishTarget = "$($packages.Count) framework packages to $source"
if ($includeSymbols) {
	Write-Host "Symbol packages to publish to ${symbolSource}:" -ForegroundColor Cyan
	foreach ($symbolPackage in $symbolPackages) {
		Write-Host " - $($symbolPackage.Name)"
	}
	$publishTarget += "; matching symbols to $symbolSource"
}
if (-not $PSCmdlet.ShouldProcess($publishTarget, 'Publish NuGet packages')) {
	return
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
	throw 'dotnet SDK not found on PATH. Install the .NET SDK and try again.'
}
if ([string]::IsNullOrWhiteSpace($apiKey)) {
	$apiKey = $env:NUGET_API_KEY
}
if ([string]::IsNullOrWhiteSpace($apiKey)) {
	$apiKey = Read-ApiKey
}
if ([string]::IsNullOrWhiteSpace($apiKey)) {
	throw 'NuGet API key is required. Supply -ApiKey, set NUGET_API_KEY, or enter it when prompted.'
}

foreach ($package in $packages) {
	Write-Host "Pushing $($package.Name) ..." -ForegroundColor Green
	& dotnet nuget push $package.FullName --api-key $apiKey --source $source --skip-duplicate --no-symbols
	if ($LASTEXITCODE -ne 0) {
		throw "dotnet nuget push failed for $($package.Name) (exit code $LASTEXITCODE)."
	}
}
foreach ($symbolPackage in $symbolPackages) {
	Write-Host "Pushing symbols $($symbolPackage.Name) ..." -ForegroundColor Green
	& dotnet nuget push $symbolPackage.FullName --api-key $apiKey --source $symbolSource --skip-duplicate
	if ($LASTEXITCODE -ne 0) {
		throw "dotnet nuget push failed for $($symbolPackage.Name) (exit code $LASTEXITCODE)."
	}
}

Write-Host 'Framework publishing completed.' -ForegroundColor Green
