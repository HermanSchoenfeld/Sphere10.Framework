#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Publishes Sphere10 Framework NuGet packages to NuGet.org

.DESCRIPTION
    Uploads pre-built NuGet packages from nuget-packages/ to NuGet.org
    Requires NUGET_API_KEY environment variable to be set.
    Framework version: 3.0.0

.EXAMPLE
    # Publish all packages
    .\scripts\publish.ps1 -ApiKey $env:NUGET_API_KEY

.EXAMPLE
    # Publish specific package
    .\scripts\publish.ps1 -ApiKey $apiKey -PackagePattern "Sphere10.Framework.Data*"

.EXAMPLE
    # Dry run (don't actually publish)
    .\scripts\publish.ps1 -ApiKey $apiKey -DryRun
#>

param(
    [string]$ApiKey,
    [string]$Source = "https://api.nuget.org/v3/index.json",
    [string]$PackagesPath = "./nuget-packages",
    [string]$PackagePattern = "*.nupkg",
    [switch]$DryRun = $false,
    [switch]$SkipSymbols = $false
)

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

# Use environment variable if ApiKey not provided
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    $ApiKey = $env:NUGET_API_KEY
}

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Error "API key is required. Pass -ApiKey or set NUGET_API_KEY environment variable."
    exit 1
}

$SolutionRoot = Split-Path -Parent $PSScriptRoot
$PackagesFullPath = Join-Path $SolutionRoot $PackagesPath

Write-Information "[>] Sphere10 Framework NuGet Publish"
Write-Information "====================================="
Write-Information "Package Path: $PackagesFullPath"
Write-Information "NuGet Source: $Source"
Write-Information "Dry Run: $DryRun"
Write-Information ""

if (-not (Test-Path $PackagesFullPath)) {
    Write-Error "Packages directory not found: $PackagesFullPath"
    exit 1
}

# Get packages to publish (exclude .snupkg symbols unless specified)
$PackageFilter = $PackagePattern
if ($SkipSymbols) {
    $Packages = Get-ChildItem -Path $PackagesFullPath -Filter $PackageFilter | 
        Where-Object { $_.Name -notmatch "\.snupkg$" }
} else {
    $Packages = Get-ChildItem -Path $PackagesFullPath -Filter $PackageFilter
}

if ($Packages.Count -eq 0) {
    Write-Error "No packages found matching pattern '$PackageFilter' in $PackagesFullPath"
    exit 1
}

Write-Information "[*] Found $($Packages.Count) packages to publish:"
$Packages | ForEach-Object { Write-Information "   - $($_.Name)" }
Write-Information ""

if ($DryRun) {
    Write-Information "[!] DRY RUN MODE - Packages will not be published"
    Write-Information ""
}

$PublishedCount = 0
$FailedPackages = @()

$Packages | ForEach-Object {
    $PackagePath = $_.FullName
    $PackageName = $_.Name
    
    Write-Information "[+] Publishing $PackageName..."
    
    $PushArgs = @(
        "nuget", "push", $PackagePath
        "--source", $Source
        "--api-key", $ApiKey
        "--skip-duplicate"
    )
    
    if ($DryRun) {
        Write-Information "   [DRY RUN] Would execute: dotnet $($PushArgs -join ' ')"
        $PublishedCount++
    } else {
        try {
            $Output = & dotnet @PushArgs 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                $PublishedCount++
                Write-Information "   [OK] $PackageName published"
            } else {
                $FailedPackages += $PackageName
                Write-Information "   [FAIL] $PackageName failed (exit code: $LASTEXITCODE)"
                Write-Information $Output
            }
        } catch {
            $FailedPackages += $PackageName
            Write-Information "   [FAIL] $PackageName failed: $_"
        }
    }
}

Write-Information ""
Write-Information "Publish Summary"
Write-Information "================"
Write-Information "Published: $PublishedCount packages"
Write-Information "Failed: $($FailedPackages.Count) packages"

if ($FailedPackages.Count -gt 0) {
    Write-Information ""
    Write-Information "Failed packages:"
    $FailedPackages | ForEach-Object { Write-Information "  - $_" }
    exit 1
}

if ($DryRun) {
    Write-Information ""
    Write-Information "[OK] Dry run complete! No packages were actually published."
} else {
    Write-Information ""
    Write-Information "[OK] Publishing complete!"
}

