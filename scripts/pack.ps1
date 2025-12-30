#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Packs all Hydrogen framework projects into NuGet packages.

.DESCRIPTION
    Builds and packages all projects in the framework for release.
    - Uses local project references (UseLocalProjects=true) by default
    - Pass -UseNugetReferences to simulate production builds
    - Outputs packages to ./nuget-packages/

.EXAMPLE
    # Pack all projects (development build, local references)
    .\scripts\pack.ps1

.EXAMPLE
    # Pack with NuGet references (production simulation)
    .\scripts\pack.ps1 -UseNugetReferences

.EXAMPLE
    # Pack specific configuration
    .\scripts\pack.ps1 -Configuration Release -OutputPath ./artifacts
#>

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "./nuget-packages",
    [switch]$UseNugetReferences = $false,
    [switch]$Clean = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

# Resolve paths
$SolutionRoot = Split-Path -Parent $PSScriptRoot
$SrcPath = Join-Path $SolutionRoot "src"
$OutputFullPath = Join-Path $SolutionRoot $OutputPath

Write-Information "[PACK] Hydrogen Framework NuGet Pack Script"
Write-Information "=========================================="
Write-Information "Solution Root: $SolutionRoot"
Write-Information "Output Path: $OutputFullPath"
Write-Information "Configuration: $Configuration"
Write-Information "Use NuGet References: $UseNugetReferences"
Write-Information ""

# Create output directory
if (-not (Test-Path $OutputFullPath)) {
    New-Item -ItemType Directory -Path $OutputFullPath -Force | Out-Null
    Write-Information "[OK] Created output directory: $OutputFullPath"
}

# Clean if requested
if ($Clean) {
    Write-Information "[CLEAN] Cleaning previous builds..."
    Get-ChildItem -Path $OutputFullPath -Filter "*.nupkg" -Recurse | Remove-Item -Force
    Get-ChildItem -Path $SrcPath -Directory | ForEach-Object {
        $binPath = Join-Path $_.FullName "bin"
        $objPath = Join-Path $_.FullName "obj"
        if (Test-Path $binPath) { Remove-Item $binPath -Recurse -Force }
        if (Test-Path $objPath) { Remove-Item $objPath -Recurse -Force }
    }
    Write-Information "[OK] Cleaned build artifacts"
}

# Build arguments
$BuildArgs = @(
    "--configuration", $Configuration
    "--output", $OutputFullPath
)

if ($UseNugetReferences) {
    Write-Information "[INFO] Packing with NuGet references (production mode)"
    $BuildArgs += "/p:UseLocalProjects=false"
} else {
    Write-Information "[OK] Packing with local project references (development mode)"
}

if ($Verbose) {
    $BuildArgs += "--verbosity", "normal"
} else {
    $BuildArgs += "--verbosity", "minimal"
}

# Get all .csproj files to pack
$Projects = Get-ChildItem -Path $SrcPath -Filter "*.csproj" -Recurse | 
    Where-Object { $_.Name -notmatch "\.Tests\.csproj$" } |
    Where-Object { $_.BaseName -notmatch "\.DApp\." } |
    Where-Object { $_.BaseName -notmatch "^Hydrogen\.Generators$" } |
    Where-Object { $_.BaseName -notmatch "^Hydrogen\.NUnit" } |
    Sort-Object Name

Write-Information "[LIST] Found $($Projects.Count) projects to pack:"
$Projects | ForEach-Object { Write-Information "   - $($_.BaseName)" }
Write-Information ""

# Pack each project
$PackedCount = 0
$FailedProjects = @()

$Projects | ForEach-Object {
    $ProjectPath = $_.FullName
    $ProjectName = $_.BaseName
    
    Write-Information "[PACK] $ProjectName..."
    
    try {
        # Build project first to ensure DLLs exist
        & dotnet build $ProjectPath -c $Configuration -nologo | Out-Null
        
        # Then pack with appropriate reference mode
        $PackArgs = @(
            "pack"
            $ProjectPath
            "-c", $Configuration
            "-o", $OutputFullPath
            "-nologo"
        )
        
        if ($UseNugetReferences) {
            $PackArgs += "/p:UseLocalProjects=false"
        }
        
        $Output = & dotnet @PackArgs 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            $PackedCount++
            Write-Information "   [OK] $ProjectName packed successfully"
        } else {
            $FailedProjects += $ProjectName
            Write-Information "   [ERROR] $ProjectName failed (exit code: $LASTEXITCODE)"
            if ($Verbose) { Write-Information $Output }
        }
    } catch {
        $FailedProjects += $ProjectName
        Write-Information "   [ERROR] $ProjectName failed with exception: $_"
    }
}

Write-Information ""
Write-Information "[SUMMARY] Pack Summary"
Write-Information "====================="
Write-Information "[OK] Successfully packed: $PackedCount projects"
Write-Information "[ERROR] Failed: $($FailedProjects.Count) projects"

if ($FailedProjects.Count -gt 0) {
    Write-Information ""
    Write-Information "Failed projects:"
    $FailedProjects | ForEach-Object { Write-Information "  - $_" }
    exit 1
}

# Count generated packages
$Packages = Get-ChildItem -Path $OutputFullPath -Filter "*.nupkg" | Measure-Object
Write-Information ""
Write-Information "[COMPLETE] Generated $($Packages.Count) NuGet packages"
Write-Information "[PATH] Location: $OutputFullPath"
Write-Information ""
Write-Information "[SUCCESS] Pack complete!"
