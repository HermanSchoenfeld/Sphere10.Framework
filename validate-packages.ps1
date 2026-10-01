#requires -Version 5.1
<#
.SYNOPSIS
Packs the active framework libraries and validates isolated NuGet consumers.
.DESCRIPTION
Creates a unique prerelease version and an external temporary workspace. Framework
packages restore exclusively from the generated feed. Third-party packages restore
from nuget.org. Keeps packages, consumer projects and logs for inspection; does not
publish packages or remove existing files. Requires the .NET 10 SDK. The default
Windows solution also requires the Windows desktop targeting pack.
.EXAMPLE
.\validate-packages.ps1
.EXAMPLE
.\validate-packages.ps1 -solutionPath 'src/Sphere10.Framework (CrossPlatform).sln'
#>
[CmdletBinding()]
param(
	[string]$solutionPath = (Join-Path $PSScriptRoot 'src/Sphere10.Framework (Win).sln'),
	[ValidateSet('Debug', 'Release')]
	[string]$configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Invoke-Dotnet {
	param([string[]]$arguments, [string]$logName)
	$logPath = Join-Path $workspace "$logName.log"
	& dotnet @arguments *> $logPath
	if ($LASTEXITCODE -ne 0) {
		Get-Content -LiteralPath $logPath -Tail 80 | Write-Host
		throw "dotnet $($arguments[0]) failed. See $logPath"
	}
}

function Write-TextFile {
	param([string]$path, [string]$content)
	[IO.File]::WriteAllText($path, $content + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
}

function New-Consumer {
	param([string]$name, [string[]]$packageIds, [string]$sdk = 'Microsoft.NET.Sdk', [string]$targetFramework = 'net10.0')
	$directory = Join-Path $workspace $name
	New-Item -ItemType Directory -Path $directory | Out-Null
	Copy-Item -Path (Join-Path $PSScriptRoot "scripts/package-consumers/$name/*") -Destination $directory
	$references = foreach ($id in $packageIds) {
		$privateAssets = if ($id -eq 'Sphere10.Framework.Generators') { ' PrivateAssets="all"' } else { '' }
		"`t`t<PackageReference Include=`"$id`" Version=`"[$version]`"$privateAssets />"
	}
	$desktopProperties = if ($name -eq 'Desktop') { '<UseWindowsForms>true</UseWindowsForms><EnableWindowsTargeting>true</EnableWindowsTargeting>' } else { '' }
	$projectPath = Join-Path $directory "PackageConsumer.$name.csproj"
	Write-TextFile $projectPath @"
<Project Sdk="$sdk">
	<PropertyGroup>
		<TargetFramework>$targetFramework</TargetFramework>
		<RootNamespace>PackageConsumer.$name</RootNamespace>
		$desktopProperties
	</PropertyGroup>
	<ItemGroup>
$($references -join [Environment]::NewLine)
	</ItemGroup>
</Project>
"@
	Write-Host "Building external $name package consumer..."
	Invoke-Dotnet @('restore', $projectPath, '--configfile', $nugetConfig, '--nologo') "restore-$name"
	Invoke-Dotnet @('build', $projectPath, '-c', $configuration, '--no-restore', '--nologo') "build-$name"
	return $projectPath
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
	throw 'Install the .NET 10 SDK before running package validation.'
}
$solutionPath = (Resolve-Path -LiteralPath $solutionPath).ProviderPath
$solutionDirectory = Split-Path -Parent $solutionPath
$runId = [guid]::NewGuid().ToString('N')
$version = '0.0.0-validation.' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss') + '.' + $runId.Substring(0, 8)
$workspace = Join-Path ([IO.Path]::GetTempPath()) "s10nuget-$($runId.Substring(0, 8))"
$feed = Join-Path $workspace 'feed'
New-Item -ItemType Directory -Path $workspace, $feed | Out-Null
Write-Host "Validating $version in $workspace" -ForegroundColor Cyan

# Stop repository or user-directory build customizations from leaking into consumers.
Write-TextFile (Join-Path $workspace 'Directory.Build.props') @"
<Project>
	<PropertyGroup>
		<LangVersion>latest</LangVersion>
		<Nullable>annotations</Nullable>
		<ImplicitUsings>disable</ImplicitUsings>
		<IsPackable>false</IsPackable>
	</PropertyGroup>
</Project>
"@
Write-TextFile (Join-Path $workspace 'Directory.Build.targets') '<Project />'
Write-TextFile (Join-Path $workspace 'Directory.Packages.props') '<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>'

# Use evaluated metadata, so centrally inherited packability remains authoritative.
$projects = @()
foreach ($line in Get-Content -LiteralPath $solutionPath) {
	if ($line -notmatch '^Project\([^)]*\) = "[^"]+", "([^"]+\.csproj)",') { continue }
	$projectPath = [IO.Path]::GetFullPath((Join-Path $solutionDirectory $Matches[1]))
	if (-not $projectPath.StartsWith($solutionDirectory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { continue }
	$output = & dotnet msbuild $projectPath -nologo -p:BuildRevision=0 -getProperty:IsPackable,PackageId,TargetFramework
	if ($LASTEXITCODE -ne 0) { throw "Cannot evaluate $projectPath" }
	$metadata = ($output -join [Environment]::NewLine | ConvertFrom-Json).Properties
	if ($metadata.IsPackable -ne 'true') { continue }
	$projects += [PSCustomObject]@{ Path = $projectPath; Id = $metadata.PackageId; TargetFramework = $metadata.TargetFramework }
}
if ($projects.Count -eq 0) { throw "No packable framework projects found in $solutionPath" }

# Pack only libraries, without cleaning or rebuilding tester applications.
Write-Host "Packing $($projects.Count) public libraries..."
Invoke-Dotnet @('new', 'sln', '--name', 'Packages', '--format', 'sln', '--output', $workspace) 'create-solution'
$packSolution = Join-Path $workspace 'Packages.sln'
Invoke-Dotnet (@('sln', $packSolution, 'add') + @($projects.Path)) 'populate-solution'
Invoke-Dotnet @('pack', $packSolution, '-c', $configuration, '-o', $feed, '-p:BuildRevision=0', "-p:Version=$version", '--nologo') 'pack'

# Validate package metadata and the complete internal dependency graph.
$packages = @(Get-ChildItem -LiteralPath $feed -Filter '*.nupkg' -File)
if ($packages.Count -ne $projects.Count) { throw "Expected $($projects.Count) packages, found $($packages.Count)." }
$packageIds = @($projects.Id)
$blazorEntries = @()
foreach ($package in $packages) {
	$archive = [IO.Compression.ZipFile]::OpenRead($package.FullName)
	try {
		$entries = @($archive.Entries.FullName)
		$nuspecEntry = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') })[0]
		$reader = New-Object IO.StreamReader($nuspecEntry.Open())
		try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
		$metadata = $nuspec.package.metadata
		if ($metadata.id -notin $packageIds -or $metadata.version -ne $version) { throw "Unexpected identity in $($package.Name)" }
		foreach ($element in @('authors', 'description', 'license', 'readme', 'icon', 'repository')) {
			if (-not $metadata.SelectSingleNode("*[local-name()='$element']")) { throw "$($metadata.id) is missing $element metadata." }
		}
		foreach ($entry in @([string]$metadata.readme, [string]$metadata.icon, 'LICENSE')) {
			if ($entry -notin $entries) { throw "$($metadata.id) does not contain $entry" }
		}
		if (-not (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($package.FullName, '.snupkg')))) { throw "Missing symbols for $($metadata.id)" }
		foreach ($dependency in $metadata.SelectNodes(".//*[local-name()='dependency']")) {
			if ($dependency.id -like 'Sphere10.Framework*' -or $dependency.id -eq 'Sphere10.HashLib4CSharp') {
				if ($dependency.id -notin $packageIds -or $dependency.version -notmatch ([regex]::Escape($version))) {
					throw "Internal dependency missing or version mismatch: $($metadata.id) -> $($dependency.id) $($dependency.version)"
				}
			}
		}
		if ($metadata.id -eq 'Sphere10.Framework.Generators') {
			if ('analyzers/dotnet/cs/Sphere10.Framework.Generators.dll' -notin $entries -or @($entries | Where-Object { $_ -like 'lib/*' }).Count) {
				throw 'Generator must be delivered as a compiler analyzer, without runtime lib assets.'
			}
		}
		if ($metadata.id -eq 'Sphere10.Framework.Web.AspNetCore.Blazor') { $blazorEntries = $entries }
	} finally {
		$archive.Dispose()
	}
}
foreach ($asset in @('staticwebassets/css/app.css', 'staticwebassets/js/functions.js', 'staticwebassets/js/BlazorGrid.js')) {
	if ($asset -notin $blazorEntries) { throw "Blazor package is missing $asset" }
}
if (-not @($blazorEntries | Where-Object { $_ -like 'buildTransitive/*.props' }).Count) { throw 'Blazor static web asset integration is missing.' }

# Exact versions and an empty cache prevent public packages from masking missing local packages.
$nugetConfig = Join-Path $workspace 'NuGet.Config'
$escapedFeed = [Security.SecurityElement]::Escape($feed)
$escapedCache = [Security.SecurityElement]::Escape((Join-Path $workspace 'packages'))
Write-TextFile $nugetConfig @"
<configuration>
	<packageSources><clear /><add key="local" value="$escapedFeed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
	<config><add key="globalPackagesFolder" value="$escapedCache" /></config>
	<packageSourceMapping>
		<packageSource key="local"><package pattern="Sphere10.Framework*" /><package pattern="Sphere10.HashLib4CSharp" /></packageSource>
		<packageSource key="nuget.org"><package pattern="*" /></packageSource>
	</packageSourceMapping>
</configuration>
"@
$coreProject = New-Consumer 'Core' @('Sphere10.Framework.Web')
$coreAssets = Get-Content -LiteralPath (Join-Path (Split-Path $coreProject) 'obj/project.assets.json') -Raw
if ($coreAssets -match 'Microsoft.AspNetCore') { throw 'Pure Web unexpectedly requires ASP.NET Core.' }
$libraryIds = @($packageIds | Where-Object { $_ -notlike 'Sphere10.Framework.Windows*' -and $_ -notlike 'Sphere10.Framework.Web.AspNetCore*' })
$null = New-Consumer 'Libraries' $libraryIds
$desktopIds = @($packageIds | Where-Object { $_ -like 'Sphere10.Framework.Windows*' })
if ($desktopIds.Count) { $null = New-Consumer 'Desktop' $desktopIds 'Microsoft.NET.Sdk' 'net10.0-windows' }
$webProject = New-Consumer 'Web' @('Sphere10.Framework.Web.AspNetCore.MVC', 'Sphere10.Framework.Web.AspNetCore.Blazor') 'Microsoft.NET.Sdk.Web'
$publishDirectory = Join-Path $workspace 'published-web'
Invoke-Dotnet @('publish', $webProject, '-c', $configuration, '--no-restore', '-o', $publishDirectory, '--nologo') 'publish-web'
foreach ($entry in @($blazorEntries | Where-Object { $_.StartsWith('staticwebassets/') -and -not $_.EndsWith('/') })) {
	$asset = $entry.Substring('staticwebassets/'.Length)
	$assetPath = Join-Path $publishDirectory "wwwroot/_content/Sphere10.Framework.Web.AspNetCore.Blazor/$asset"
	if (-not (Test-Path -LiteralPath $assetPath -PathType Leaf)) { throw "Published Razor asset missing: $assetPath" }
}

$scopedCssDirectory = Join-Path $publishDirectory 'wwwroot/_content/Sphere10.Framework.Web.AspNetCore.Blazor'
if (-not @(Get-ChildItem -LiteralPath $scopedCssDirectory -Filter '*.bundle.scp.css' -File).Count) {
	throw 'Published Blazor scoped CSS bundle is missing.'
}
$hostStyles = Join-Path $publishDirectory 'wwwroot/PackageConsumer.Web.styles.css'
if (-not (Test-Path -LiteralPath $hostStyles) -or (Get-Content -LiteralPath $hostStyles -Raw) -notmatch 'Sphere10.Framework.Web.AspNetCore.Blazor') {
	throw 'Host CSS does not import the packaged Blazor scoped styles.'
}

# Start only this generated application, on a loopback port chosen by Kestrel.
$stdout = Join-Path $workspace 'web-stdout.log'
$stderr = Join-Path $workspace 'web-stderr.log'
$processArguments = @{
	FilePath = (Get-Command dotnet).Source
	ArgumentList = @(('"' + (Join-Path $publishDirectory 'PackageConsumer.Web.dll') + '"'), '--urls', 'http://127.0.0.1:0', '--environment', 'Production')
	WorkingDirectory = $publishDirectory
	RedirectStandardOutput = $stdout
	RedirectStandardError = $stderr
	PassThru = $true
}
if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) { $processArguments.WindowStyle = 'Hidden' }
$webProcess = Start-Process @processArguments
try {
	$baseUrl = $null
	$deadline = [DateTime]::UtcNow.AddSeconds(30)
	while ([DateTime]::UtcNow -lt $deadline -and -not $webProcess.HasExited) {
		$log = Get-Content -LiteralPath $stdout -Raw -ErrorAction SilentlyContinue
		if ($log -match 'Now listening on: (http://127\.0\.0\.1:\d+)') {
			$baseUrl = $Matches[1]
			break
		}
		Start-Sleep -Milliseconds 250
	}
	if (-not $baseUrl) { throw "Published app did not start. Inspect $stdout and $stderr" }
	$routes = @('/', '/sitemap.xml', '/PackageConsumer.Web.styles.css')
	$routes += @($blazorEntries | Where-Object { $_ -match '^staticwebassets/.*\.(?:js|mjs)$' } | ForEach-Object {
		'/_content/Sphere10.Framework.Web.AspNetCore.Blazor/' + $_.Substring('staticwebassets/'.Length)
	})
	foreach ($route in $routes) {
		$response = Invoke-WebRequest -Uri ($baseUrl + $route) -UseBasicParsing -TimeoutSec 15
		if ($response.StatusCode -ne 200) { throw "Published route failed: $route" }
		if ($route -eq '/' -and $response.Content -notmatch 'ApplicationBlock loaded from NuGet') {
			throw 'The packaged ApplicationBlock screen did not render.'
		}
		if ($route -eq '/sitemap.xml' -and $response.Content -notmatch 'urlset') { throw 'Packaged MVC XML result did not render.' }
	}
} finally {
	if (-not $webProcess.HasExited) { Stop-Process -Id $webProcess.Id }
	$webProcess.Dispose()
}

$report = [PSCustomObject]@{
	Version = $version
	Solution = $solutionPath
	Packages = @($packageIds | Sort-Object)
	Consumers = @('Core', 'Libraries', 'Web') + $(if ($desktopIds.Count) { 'Desktop' })
	PublishedWeb = $publishDirectory
	HttpChecks = 'ApplicationBlock SSR, MVC XML, scoped CSS, JavaScript'
}
Write-TextFile (Join-Path $workspace 'validation.json') ($report | ConvertTo-Json -Depth 5)
Write-Host "Validated $($packages.Count) packages, external consumers and published Razor assets. Artifacts: $workspace" -ForegroundColor Green
