[CmdletBinding()]
param(
	[string]$ApiKey,
	[string]$Source = "https://api.nuget.org/v3/index.json",
	[switch]$IncludeSymbols,
	[string]$SymbolSource = "https://symbols.nuget.org/api/v2/symbolpackage",
	[switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

function Read-ApiKey {
	param([string]$Prompt)
	$secure = Read-Host -Prompt $Prompt -AsSecureString
	try {
		$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
		try {
			return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
		} finally {
			[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
		}
	} finally {
		$secure = $null
	}
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
	throw "dotnet SDK not found on PATH. Install .NET SDK and try again."
}

$packagesDir = Join-Path $PSScriptRoot 'nuget-packages'
if (-not (Test-Path -LiteralPath $packagesDir)) {
	throw "Package folder not found: $packagesDir. Run pack.ps1 first."
}

$nupkgs = Get-ChildItem -LiteralPath $packagesDir -File -Filter '*.nupkg' |
	Where-Object { $_.Name -notlike '*.snupkg' } |
	Sort-Object Name

if ($nupkgs.Count -eq 0) {
	Write-Host "No .nupkg files found in $packagesDir" -ForegroundColor Yellow
	exit 0
}

Write-Host "Packages to publish to ${Source}:" -ForegroundColor Cyan
$nupkgs | ForEach-Object { Write-Host (" - " + $_.Name) }

$snupkgs = @()
if ($IncludeSymbols) {
	$snupkgs = Get-ChildItem -LiteralPath $packagesDir -File -Filter '*.snupkg' | Sort-Object Name
	if ($snupkgs.Count -gt 0) {
		Write-Host "Symbol packages to publish to ${SymbolSource}:" -ForegroundColor Cyan
		$snupkgs | ForEach-Object { Write-Host (" - " + $_.Name) }
	}
}

if ($WhatIf) {
	Write-Host "WhatIf: not pushing anything." -ForegroundColor Yellow
	exit 0
}

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
	$ApiKey = Read-ApiKey "Enter NuGet API key (input hidden)"
}

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
	throw "NuGet API key is required. Provide -ApiKey or enter it when prompted."
}

$confirm = Read-Host -Prompt "Proceed to publish these packages? Type 'publish' to continue"
if ($confirm -ne 'publish') {
	Write-Host "Aborted." -ForegroundColor Yellow
	exit 1
}

foreach ($pkg in $nupkgs) {
	Write-Host ("Pushing " + $pkg.Name + " ...") -ForegroundColor Green
	& dotnet nuget push $pkg.FullName --api-key $ApiKey --source $Source --skip-duplicate
	if ($LASTEXITCODE -ne 0) {
		throw "dotnet nuget push failed for $($pkg.Name) (exit code $LASTEXITCODE)"
	}
}

if ($IncludeSymbols -and $snupkgs.Count -gt 0) {
	foreach ($spkg in $snupkgs) {
		Write-Host ("Pushing symbols " + $spkg.Name + " ...") -ForegroundColor Green
		& dotnet nuget push $spkg.FullName --api-key $ApiKey --source $SymbolSource --skip-duplicate
		if ($LASTEXITCODE -ne 0) {
			throw "dotnet nuget push failed for $($spkg.Name) (exit code $LASTEXITCODE)"
		}
	}
}

Write-Host "Done." -ForegroundColor Cyan
