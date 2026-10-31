#requires -Version 7.2
<#
.SYNOPSIS
Builds the local Blazor tester and runs explicitly selected browser regressions.
.DESCRIPTION
Starts an isolated loopback tester, supplies its URL and an installed browser to
NUnit, and stops only its own server afterwards. Keeps logs, screenshots and TRX
results outside the repository by default. Does not install or download browsers.
.EXAMPLE
./scripts/test-blazor-browser.ps1
.EXAMPLE
./scripts/test-blazor-browser.ps1 -filter 'FullyQualifiedName~BlazorGridBrowserTests' -browserExecutablePath 'C:\Program Files\Google\Chrome\Application\chrome.exe'
#>
[CmdletBinding()]
param(
	[string]$filter = 'FullyQualifiedName~WizardBrowserTests',
	[ValidateSet('Debug', 'Release')]
	[string]$configuration = 'Release',
	[string]$browserExecutablePath,
	[string]$outputDirectory = (Join-Path ([IO.Path]::GetTempPath()) ('Sphere10-BlazorBrowser-' + [Guid]::NewGuid().ToString('N')))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repository = Split-Path -Parent $PSScriptRoot
$outputDirectory = [IO.Path]::GetFullPath($outputDirectory)
$artifactsDirectory = Join-Path $outputDirectory 'artifacts'
$resultsDirectory = Join-Path $outputDirectory 'results'
$browserWorkDirectory = Join-Path $outputDirectory 'browser'
$testProject = Join-Path $repository 'tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests/Sphere10.Framework.Web.AspNetCore.Blazor.Tests.csproj'
$testerDirectory = Join-Path $repository 'utils/Sphere10.Framework.Utils.BlazorTester'
$testerAssembly = Join-Path $artifactsDirectory "bin/Sphere10.Framework.Utils.BlazorTester/$($configuration.ToLowerInvariant())/Sphere10.Framework.Utils.BlazorTester.dll"
$resultName = 'browser-' + [Guid]::NewGuid().ToString('N') + '.trx'

# Runs one build/test command and retains its complete diagnostic output.
function Invoke-Dotnet {
	param([string[]]$arguments, [string]$logName)
	$logPath = Join-Path $outputDirectory $logName
	& dotnet @arguments *> $logPath
	Get-Content -LiteralPath $logPath -Tail 15 | Write-Host
	if ($LASTEXITCODE -ne 0) {
		throw "dotnet $($arguments[0]) failed. See $logPath"
	}
}

# Prefer an existing Windows browser; an omitted path elsewhere uses installed Playwright Chromium.
if ([string]::IsNullOrWhiteSpace($browserExecutablePath) -and $IsWindows) {
	$browserExecutablePath = @(
		[IO.Path]::Combine([Environment]::GetFolderPath('ProgramFilesX86'), 'Microsoft/Edge/Application/msedge.exe'),
		[IO.Path]::Combine([Environment]::GetFolderPath('ProgramFiles'), 'Microsoft/Edge/Application/msedge.exe'),
		[IO.Path]::Combine([Environment]::GetFolderPath('ProgramFiles'), 'Google/Chrome/Application/chrome.exe')
	) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if (-not [string]::IsNullOrWhiteSpace($browserExecutablePath)) {
	if (-not (Test-Path -LiteralPath $browserExecutablePath -PathType Leaf)) {
		throw "Browser executable does not exist: $browserExecutablePath"
	}
	$browserExecutablePath = (Get-Item -LiteralPath $browserExecutablePath).FullName
}
if ([string]::IsNullOrWhiteSpace($filter)) {
	throw 'A browser-test filter is required.'
}
$browserFilter = "($filter)&TestCategory=Browser"

# The test project builds its tester reference into the same isolated artifact directory.
[IO.Directory]::CreateDirectory($resultsDirectory) | Out-Null
[IO.Directory]::CreateDirectory($browserWorkDirectory) | Out-Null
Write-Host "Browser regression output: $outputDirectory"
Invoke-Dotnet -arguments @('build', $testProject, '-c', $configuration, '-p:BuildRevision=0', '--artifacts-path', $artifactsDirectory) -logName 'build.log'
if (-not (Test-Path -LiteralPath $testerAssembly -PathType Leaf)) {
	throw "The tester assembly was not built: $testerAssembly"
}

# Start only this tester on a loopback port assigned by Kestrel; no personal browser profile is used.
$stdout = Join-Path $outputDirectory 'server-stdout.log'
$stderr = Join-Path $outputDirectory 'server-stderr.log'
$processArguments = @{
	FilePath = (Get-Command dotnet).Source
	ArgumentList = @(('"' + $testerAssembly + '"'), '--urls', 'http://127.0.0.1:0', '--environment', 'Development', '--contentRoot', ('"' + $testerDirectory + '"'))
	WorkingDirectory = $testerDirectory
	RedirectStandardOutput = $stdout
	RedirectStandardError = $stderr
	PassThru = $true
}
if ($IsWindows) {
	$processArguments.WindowStyle = 'Hidden'
}
$webProcess = Start-Process @processArguments
try {
	# Wait for the assigned address and an actual rendered route before launching the browser fixture.
	$baseUrl = $null
	$deadline = [DateTime]::UtcNow.AddSeconds(60)
	while ([DateTime]::UtcNow -lt $deadline -and -not $webProcess.HasExited) {
		$log = Get-Content -LiteralPath $stdout -Raw -ErrorAction SilentlyContinue
		if (-not [string]::IsNullOrEmpty($log)) {
			$match = [regex]::Match($log, 'Now listening on:\s+(http://127\.0\.0\.1:\d+)')
			if ($match.Success) {
				try {
					$response = Invoke-WebRequest -Uri ($match.Groups[1].Value + '/components/grid') -TimeoutSec 2
					if ($response.StatusCode -eq 200) {
						$baseUrl = $match.Groups[1].Value
						break
					}
				} catch {
					# Startup can expose its address just before the first route is ready.
				}
			}
		}
		Start-Sleep -Milliseconds 200
	}
	if (-not $baseUrl) {
		Get-Content -LiteralPath $stdout, $stderr -Tail 40 | Write-Host
		throw "The tester did not become ready within 60 seconds. See $stdout and $stderr"
	}
	Write-Host "Running $browserFilter against $baseUrl"

	# Explicit selection runs the fixture while keeping ordinary browser-free test runs opt-in.
	$settings = [xml]'<RunSettings><NUnit><ExplicitMode>Strict</ExplicitMode><WorkDirectory /></NUnit><TestRunParameters /></RunSettings>'
	$settings.RunSettings.NUnit.WorkDirectory = $browserWorkDirectory
	$parameters = $settings.SelectSingleNode('/RunSettings/TestRunParameters')
	foreach ($entry in @{ BlazorTesterUrl = $baseUrl; BrowserExecutablePath = $browserExecutablePath }.GetEnumerator()) {
		$parameter = $settings.CreateElement('Parameter')
		$parameter.SetAttribute('name', $entry.Key)
		$parameter.SetAttribute('value', [string]$entry.Value)
		$parameters.AppendChild($parameter) | Out-Null
	}
	$settingsPath = Join-Path $outputDirectory 'browser.runsettings'
	$settings.Save($settingsPath)
	Invoke-Dotnet -arguments @('test', $testProject, '-c', $configuration, '-p:BuildRevision=0', '--artifacts-path', $artifactsDirectory, '--no-build', '--no-restore', '--filter', $browserFilter, '--settings', $settingsPath, '--logger', "trx;LogFileName=$resultName", '--results-directory', $resultsDirectory) -logName 'tests.log'

	# A successful CLI exit with every case skipped must not be reported as browser verification.
	$report = [xml](Get-Content -LiteralPath (Join-Path $resultsDirectory $resultName) -Raw)
	$counters = $report.SelectSingleNode('//*[local-name()="Counters"]')
	if (-not $counters -or [int]$counters.GetAttribute('executed') -eq 0) {
		throw "No browser tests executed. Check the filter and $settingsPath."
	}
	Write-Host "Browser regressions completed. Results: $resultsDirectory"
} finally {
	# Release only the server started by this invocation, including failed tests or interrupted setup.
	if (-not $webProcess.HasExited) {
		Stop-Process -Id $webProcess.Id
		$webProcess.WaitForExit()
	}
	$webProcess.Dispose()
}
