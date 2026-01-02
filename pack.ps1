$ErrorActionPreference = 'Stop'

$solution = Join-Path $PSScriptRoot 'src\Sphere10.Framework (Win).sln'
$outDir = Join-Path $PSScriptRoot 'nuget-packages'

if (-not (Test-Path -LiteralPath $solution)) {
	throw "Solution not found: $solution"
}

Write-Host "Cleaning solution: $solution" -ForegroundColor Cyan
dotnet clean $solution -c Release

Write-Host "Cleaning output folder: $outDir" -ForegroundColor Cyan
if (Test-Path -LiteralPath $outDir) {
	Remove-Item -LiteralPath $outDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Write-Host "Packing to: $outDir" -ForegroundColor Cyan
dotnet pack $solution -c Release -o $outDir
