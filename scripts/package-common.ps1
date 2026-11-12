# Shared evaluated package boundary for public packing and consumer validation.
function Get-FrameworkPackageProjects {
	param([string]$solutionPath)
	$solutionPath = (Resolve-Path -LiteralPath $solutionPath).ProviderPath
	$sourceRoot = (Split-Path -Parent $solutionPath) + [IO.Path]::DirectorySeparatorChar
	$seen = @{}
	foreach ($line in Get-Content -LiteralPath $solutionPath) {
		if ($line -notmatch '^Project\([^)]*\) = "[^"]+", "([^"]+\.csproj)",') { continue }
		$projectPath = [IO.Path]::GetFullPath((Join-Path $sourceRoot $Matches[1]))
		if (-not $projectPath.StartsWith($sourceRoot, [StringComparison]::OrdinalIgnoreCase)) { continue }
		if ($seen.ContainsKey($projectPath)) { continue }
		$seen[$projectPath] = $true
		$output = & dotnet msbuild $projectPath -nologo -p:BuildRevision=0 -getProperty:IsPackable,PackageId,TargetFramework,TargetFrameworks
		if ($LASTEXITCODE -ne 0) { throw "Cannot evaluate $projectPath" }
		$metadata = ($output -join [Environment]::NewLine | ConvertFrom-Json).Properties
		if ($metadata.IsPackable -ne 'true') { continue }
		[pscustomobject]@{ Path = $projectPath; Id = $metadata.PackageId; TargetFramework = $metadata.TargetFramework; TargetFrameworks = $metadata.TargetFrameworks }
	}
}
