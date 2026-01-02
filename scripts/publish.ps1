#!/usr/bin/env pwsh
# Unified Sphere10 Framework packaging and publishing script
# Framework version: 3.0.0

param(
    [switch]$PackOnly,
    [switch]$Local,
    [switch]$NuGetOrg,
    [string]$ApiKey,
    [string]$ProjectPattern = "*",
    [string]$SolutionFile = "src/Sphere10.Framework (Win).sln",
    [string]$NuGetSource = "https://api.nuget.org/v3/index.json",
    [string]$LocalFeedPath = "$env:USERPROFILE\.nuget\local-feed",
    [string]$OutputPath = "./nuget-packages",
    [switch]$DryRun,
    [switch]$Clean,
    [switch]$SkipSymbols
)

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

# Default to local if no target specified
if (-not $PackOnly -and -not $Local -and -not $NuGetOrg) {
    $Local = $true
}

$SolutionRoot = Split-Path -Parent $PSScriptRoot
$SolutionFullPath = Join-Path $SolutionRoot $SolutionFile
$OutputFullPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path $SolutionRoot $OutputPath
}

$OutputFullPath = [System.IO.Path]::GetFullPath($OutputFullPath)

Write-Information ""
Write-Information "======================================================================"
Write-Information "Sphere10 Framework - Unified Publishing Script"
Write-Information "Version: 3.0.0"
Write-Information "======================================================================"
Write-Information ""
Write-Information "Configuration:"
Write-Information "  Solution: $SolutionFile"
Write-Information "  Output Path: $OutputFullPath"
Write-Information "  Project Pattern: $ProjectPattern"
Write-Information ""

if ($DryRun) {
    Write-Information "[!] DRY RUN MODE - No changes will be made"
    Write-Information ""
}

Write-Information "======================================================================"
Write-Information "PHASE 1: PACKING PROJECTS"
Write-Information "======================================================================"
Write-Information ""

if (-not (Test-Path $SolutionFullPath)) {
    Write-Error "Solution file not found: $SolutionFullPath"
    exit 1
}

if (-not (Test-Path $OutputFullPath)) {
    if ($DryRun) {
        Write-Information "[DRY RUN] Would create output directory: $OutputFullPath"
    } else {
        New-Item -ItemType Directory -Path $OutputFullPath -Force | Out-Null
        Write-Information "[+] Created output directory: $OutputFullPath"
    }
}

if ($Clean -and -not $DryRun) {
    Write-Information "[*] Cleaning old packages..."
    Get-ChildItem -Path $OutputFullPath -Filter "*.nupkg" -ErrorAction SilentlyContinue | Remove-Item -Force
    Get-ChildItem -Path $OutputFullPath -Filter "*.snupkg" -ErrorAction SilentlyContinue | Remove-Item -Force
    Write-Information "[OK] Cleaned old packages"
}

Write-Information "[*] Finding packable projects from src directory..."
[string[]]$ProjectFiles = @()

try {
    # Find all .csproj files that are not test projects
    $allProjectsPath = Join-Path $SolutionRoot "src"
    $csprojFiles = Get-ChildItem -Path $allProjectsPath -Filter "*.csproj" -Recurse -File
    
    foreach ($csprojFile in $csprojFiles) {
        $projectName = $csprojFile.BaseName
        
        # Skip test projects
        if ($projectName -like "*Test*" -or $projectName -like "*test*") {
            continue
        }
        
        # Skip excluded directories
        if ($csprojFile.DirectoryName -like "*Tests*" -or $csprojFile.DirectoryName -like "*tests*") {
            continue
        }

        # Skip DApp projects (moved to separate repo)
        if ($projectName -match "\.DApp\.") {
            continue
        }

        # Skip projects not intended for NuGet publishing
        if ($projectName -match "^Sphere10\.Framework\.Generators$") {
            continue
        }
        if ($projectName -match "^Sphere10\.Framework\.NUnit") {
            continue
        }
        if ($projectName -match "^Sphere10\.Framework\.Android$") {
            continue
        }
        if ($projectName -match "^Sphere10\.Framework\.iOS$") {
            continue
        }
        
        # Match pattern
        if ($projectName -like $ProjectPattern) {
            $ProjectFiles += $csprojFile.FullName
            Write-Information "  [+] Found: $projectName"
        }
    }
} catch {
    Write-Error "Failed to scan projects: $_"
    exit 1
}

if ($ProjectFiles.Count -eq 0) {
    Write-Error "No packable projects found matching pattern: $ProjectPattern"
    exit 1
}

Write-Information ""
Write-Information "[*] Packing $($ProjectFiles.Count) project(s)..."
Write-Information ""

$PackedCount = 0
$FailedPacks = @()

foreach ($projectFile in $ProjectFiles) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile)
    Write-Information "[+] Packing $projectName..."
    
    if ($DryRun) {
        Write-Information "   [DRY RUN] Would pack: $projectFile"
        $PackedCount++
    } else {
        try {
            $PackArgs = @("pack", $projectFile, "-c", "Release", "-o", $OutputFullPath, "--include-symbols")
            
            if ($SkipSymbols) {
                $PackArgs = $PackArgs | Where-Object { $_ -ne "--include-symbols" }
            }
            
            $Output = & dotnet @PackArgs 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                $PackedCount++
                Write-Information "   [OK] Packed: $projectName"
            } else {
                $FailedPacks += $projectName
                Write-Information "   [FAIL] Failed to pack: $projectName"
                Write-Information "   Error: $Output"
            }
        } catch {
            $FailedPacks += $projectName
            Write-Information "   [FAIL] Exception: $_"
        }
    }
}

Write-Information ""
Write-Information "Pack Summary:"
Write-Information "  Successful: $PackedCount packages"
Write-Information "  Failed: $($FailedPacks.Count) packages"

if ($FailedPacks.Count -gt 0) {
    Write-Information ""
    Write-Information "Failed packages:"
    $FailedPacks | ForEach-Object { Write-Information "  - $_" }
    
    if (-not $DryRun) {
        exit 1
    }
}

Write-Information ""

if ($PackOnly) {
    Write-Information "======================================================================"
    Write-Information "Pack-only mode - Skipping publish phase"
    Write-Information "======================================================================"
    Write-Information ""
    Write-Information "[OK] Packing complete! Packages ready in: $OutputFullPath"
    exit 0
}

Write-Information "======================================================================"
Write-Information "PHASE 2: PUBLISHING PACKAGES"
Write-Information "======================================================================"
Write-Information ""

$Packages = Get-ChildItem -Path $OutputFullPath -Filter "*.nupkg" | Where-Object { $_.Name -notmatch "\.snupkg$" }

if ($Packages.Count -eq 0) {
    Write-Error "No packages found to publish in: $OutputFullPath"
    exit 1
}

Write-Information "[*] Found $($Packages.Count) packages to publish:"
$Packages | ForEach-Object { Write-Information "   - $($_.Name)" }
Write-Information ""

if ($Local) {
    Write-Information "[+] Publishing to local feed: $LocalFeedPath"
    Write-Information ""
    
    if (-not (Test-Path $LocalFeedPath)) {
        if ($DryRun) {
            Write-Information "   [DRY RUN] Would create: $LocalFeedPath"
        } else {
            New-Item -ItemType Directory -Path $LocalFeedPath -Force | Out-Null
            Write-Information "   [+] Created local feed directory"
        }
    }
    
    $PublishedCount = 0
    $FailedPublish = @()
    
    $Packages | ForEach-Object {
        $PackageName = $_.Name
        $PackagePath = $_.FullName
        $DestPath = Join-Path $LocalFeedPath $PackageName
        
        Write-Information "   [+] Publishing $PackageName..."
        
        if ($DryRun) {
            Write-Information "       [DRY RUN] Would copy to: $DestPath"
            $PublishedCount++
        } else {
            try {
                Copy-Item -Path $PackagePath -Destination $DestPath -Force
                if (Test-Path $DestPath) {
                    $PublishedCount++
                    Write-Information "       [OK]"
                } else {
                    $FailedPublish += $PackageName
                    Write-Information "       [FAIL] Copy operation failed"
                }
            } catch {
                $FailedPublish += $PackageName
                Write-Information "       [FAIL] $_"
            }
        }
    }
    
    Write-Information ""
    Write-Information "Local Publish Summary:"
    Write-Information "  Published: $PublishedCount packages"
    Write-Information "  Failed: $($FailedPublish.Count) packages"
    Write-Information "  Location: $LocalFeedPath"
    
    if ($FailedPublish.Count -gt 0 -and -not $DryRun) {
        exit 1
    }
}

if ($NuGetOrg) {
    Write-Information "[+] Publishing to NuGet.org"
    Write-Information ""
    
    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        $ApiKey = $env:NUGET_API_KEY
    }
    
    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        Write-Error "API key required for NuGet.org publishing. Pass -ApiKey or set NUGET_API_KEY environment variable."
        exit 1
    }
    
    $PublishedCount = 0
    $FailedPublish = @()
    
    $Packages | ForEach-Object {
        $PackageName = $_.Name
        $PackagePath = $_.FullName
        
        Write-Information "   [+] Publishing $PackageName..."
        
        if ($DryRun) {
            Write-Information "       [DRY RUN] Would push to NuGet.org"
            $PublishedCount++
        } else {
            try {
                $PushArgs = @("nuget", "push", $PackagePath, "--source", $NuGetSource, "--api-key", $ApiKey, "--skip-duplicate")
                
                $Output = & dotnet @PushArgs 2>&1
                
                if ($LASTEXITCODE -eq 0) {
                    $PublishedCount++
                    Write-Information "       [OK]"
                } else {
                    $FailedPublish += $PackageName
                    Write-Information "       [FAIL] Exit code: $LASTEXITCODE"
                    if ($Output) {
                        Write-Information "       Error: $Output"
                    }
                }
            } catch {
                $FailedPublish += $PackageName
                Write-Information "       [FAIL] $_"
            }
        }
    }
    
    Write-Information ""
    Write-Information "NuGet.org Publish Summary:"
    Write-Information "  Published: $PublishedCount packages"
    Write-Information "  Failed: $($FailedPublish.Count) packages"
    
    if ($FailedPublish.Count -gt 0 -and -not $DryRun) {
        exit 1
    }
}

Write-Information ""
Write-Information "======================================================================"
Write-Information "PUBLISH COMPLETE"
Write-Information "======================================================================"
Write-Information ""

if ($DryRun) {
    Write-Information "[*] Dry run completed successfully!"
} else {
    if ($Local) {
        Write-Information "[OK] Published to local feed at: $LocalFeedPath"
        Write-Information ""
        Write-Information "To use local packages, add the feed source if not already added:"
        Write-Information "  dotnet nuget add source '$LocalFeedPath' -n sphere10-local"
    }
    if ($NuGetOrg) {
        Write-Information "[OK] Published to NuGet.org successfully!"
    }
}

exit 0

