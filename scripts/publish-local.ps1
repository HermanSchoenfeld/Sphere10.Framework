#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Publishes Sphere10 Framework NuGet packages to a local NuGet feed

.DESCRIPTION
    Copies pre-built NuGet packages from nuget-packages/ to a local directory
    that can be used as a local NuGet feed for testing before publishing to NuGet.org
    Framework version: 3.0.0

.EXAMPLE
    # Publish to default local feed location
    .\scripts\publish-local.ps1

.EXAMPLE
    # Publish to custom location
    .\scripts\publish-local.ps1 -LocalFeedPath "C:\LocalNuGetFeed"

.EXAMPLE
    # Dry run (show what would be published without copying)
    .\scripts\publish-local.ps1 -DryRun
#>

param(
    [string]$LocalFeedPath = "$env:USERPROFILE\.nuget\local-feed",
    [string]$PackagesPath = "./nuget-packages",
    [string]$PackagePattern = "*.nupkg",
    [switch]$DryRun = $false,
    [switch]$CreateSource = $true,
    [switch]$Clean = $false
)

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$SolutionRoot = Split-Path -Parent $PSScriptRoot
$PackagesFullPath = Join-Path $SolutionRoot $PackagesPath

Write-Information "[>] Sphere10 Framework Local NuGet Feed Publisher"
Write-Information "=================================================="
Write-Information "Local Feed Path: $LocalFeedPath"
Write-Information "Package Source: $PackagesFullPath"
Write-Information "Dry Run: $DryRun"
Write-Information ""

if (-not (Test-Path $PackagesFullPath)) {
    Write-Error "Packages directory not found: $PackagesFullPath"
    exit 1
}

# Create local feed directory if it doesn't exist
if (-not (Test-Path $LocalFeedPath)) {
    if ($DryRun) {
        Write-Information "[DRY RUN] Would create directory: $LocalFeedPath"
    } else {
        Write-Information "[*] Creating local feed directory..."
        New-Item -ItemType Directory -Path $LocalFeedPath -Force | Out-Null
        Write-Information "[OK] Directory created: $LocalFeedPath"
    }
}

# Clean existing packages if requested
if ($Clean -and -not $DryRun) {
    Write-Information "[*] Cleaning existing packages from local feed..."
    Get-ChildItem -Path $LocalFeedPath -Filter "*.nupkg" | Remove-Item -Force
    Get-ChildItem -Path $LocalFeedPath -Filter "*.snupkg" | Remove-Item -Force
    Write-Information "[OK] Cleaned old packages"
    Write-Information ""
}

# Get packages to publish
$Packages = Get-ChildItem -Path $PackagesFullPath -Filter $PackagePattern

if ($Packages.Count -eq 0) {
    Write-Error "No packages found matching pattern '$PackagePattern' in $PackagesFullPath"
    exit 1
}

Write-Information "[*] Found $($Packages.Count) packages to publish locally:"
$Packages | ForEach-Object { Write-Information "   - $($_.Name)" }
Write-Information ""

if ($DryRun) {
    Write-Information "[!] DRY RUN MODE - Packages will not be copied"
    Write-Information ""
}

$PublishedCount = 0
$FailedPackages = @()

$Packages | ForEach-Object {
    $PackagePath = $_.FullName
    $PackageName = $_.Name
    $DestPath = Join-Path $LocalFeedPath $PackageName
    
    Write-Information "[+] Publishing $PackageName..."
    
    if ($DryRun) {
        Write-Information "   [DRY RUN] Would copy to: $DestPath"
        $PublishedCount++
    } else {
        try {
            Copy-Item -Path $PackagePath -Destination $DestPath -Force
            
            if (Test-Path $DestPath) {
                $PublishedCount++
                Write-Information "   [OK] $PackageName published to local feed"
            } else {
                $FailedPackages += $PackageName
                Write-Information "   [FAIL] $PackageName copy failed"
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
Write-Information "Local Feed Location: $LocalFeedPath"

if ($FailedPackages.Count -gt 0) {
    Write-Information ""
    Write-Information "Failed packages:"
    $FailedPackages | ForEach-Object { Write-Information "  - $_" }
    exit 1
}

if ($DryRun) {
    Write-Information ""
    Write-Information "[OK] Dry run complete! No packages were actually copied."
} else {
    Write-Information ""
    Write-Information "[OK] Publishing to local feed complete!"
    Write-Information ""
    Write-Information "Next steps to use this local feed:"
    Write-Information "1. Add the local feed to your NuGet configuration:"
    Write-Information ""
    Write-Information "   dotnet nuget add source `"$LocalFeedPath`" -n sphere10-local"
    Write-Information ""
    Write-Information "2. Or in Visual Studio: Tools > NuGet Package Manager > Package Manager Settings"
    Write-Information "   > Package Sources > Add new source"
    Write-Information ""
    Write-Information "3. Use packages in your projects:"
    Write-Information "   dotnet add package Sphere10.Framework -s sphere10-local"
}
