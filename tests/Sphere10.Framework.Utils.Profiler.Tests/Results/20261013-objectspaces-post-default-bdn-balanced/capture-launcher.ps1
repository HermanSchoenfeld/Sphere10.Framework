#requires -Version 7.2
param(
	[string]$EvidenceDirectory,
	[switch]$ExecuteMeasurements
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$objectSpaceFrameworkRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$objectSpacePlanPath = Join-Path $PSScriptRoot 'plan.json'
$objectSpacePlan = Get-Content -LiteralPath $objectSpacePlanPath -Raw | ConvertFrom-Json -Depth 100
$objectSpaceProfilesPath = Join-Path $PSScriptRoot 'profiles.json'
$objectSpaceSelectionPath = Join-Path $PSScriptRoot 'selection.json'
$objectSpaceProfiles = @(Get-Content -LiteralPath $objectSpaceProfilesPath -Raw | ConvertFrom-Json -Depth 100)
if ($objectSpaceProfiles.Count -ne 4 -or $objectSpacePlan.ExpectedMeasuredCases -ne 4 -or $objectSpacePlan.ExpectedIndependentWorkers -ne 8 -or $objectSpacePlan.ExpectedSamplesPerCase -ne 20) {
	throw 'Require the exact four-profile post-default plan with eight workers and twenty samples per case.'
}
if (($objectSpaceProfiles | ConvertTo-Json -Depth 100 -Compress) -ne (@($objectSpacePlan.Settings.Profiles) | ConvertTo-Json -Depth 100 -Compress)) {
	throw 'The CLI profile input differs from the exact profiles recorded in plan.json.'
}
$objectSpaceExecutable = Join-Path $objectSpaceFrameworkRoot 'utils/Sphere10.Framework.Utils.Profiler/bin/Release/net10.0/Sphere10.Framework.Utils.Profiler.exe'
if ([string]::IsNullOrWhiteSpace($EvidenceDirectory)) { $EvidenceDirectory = Join-Path $objectSpaceFrameworkRoot $objectSpacePlan.EvidenceRelativeDirectory }
$objectSpaceDestination = [System.IO.Path]::GetFullPath($EvidenceDirectory)
$objectSpaceDataPath = [System.IO.Path]::GetFullPath((Join-Path $objectSpaceFrameworkRoot $objectSpacePlan.DataRelativeDirectory))
$objectSpaceRetainedProfiles = Join-Path $objectSpaceDestination 'profiles.json'
$objectSpaceSettings = $objectSpacePlan.Settings
$objectSpaceArguments = @(
	'object-spaces', '--config', $objectSpaceRetainedProfiles, '--records', [string]$objectSpaceSettings.RecordCount,
	'--operations', [string]$objectSpaceSettings.OperationCount, '--payload', [string]$objectSpaceSettings.PayloadBytes,
	'--categories', [string]$objectSpaceSettings.CategoryCount, '--hot-category-percent', [string]$objectSpaceSettings.HotCategoryPercent,
	'--lookup', [string]$objectSpaceSettings.LookupPattern, '--payload-mode', [string]$objectSpaceSettings.PayloadPattern,
	'--insertion', $(if ($objectSpaceSettings.ShuffledInsertion) { 'shuffled' } else { 'sequential' }), '--mix', [string]$objectSpaceSettings.WorkloadMix
) + @($objectSpacePlan.BenchmarkArguments | ForEach-Object { [string]$_ }) + @('--output', $objectSpaceDestination, '--data-root', $objectSpaceDataPath)
$objectSpaceRequiredArguments = @{
	'--filter' = '*MixedReadWriteAndPersist*'; '--launchCount' = '2'; '--iterationCount' = '10'; '--warmupCount' = '5';
	'--strategy' = 'Monitoring'; '--invocationCount' = '1'; '--unrollFactor' = '1'; '--outliers' = 'DontRemove'
}
foreach ($objectSpaceFlag in $objectSpaceRequiredArguments.Keys) {
	$objectSpaceFlagIndex = [Array]::IndexOf($objectSpaceArguments, $objectSpaceFlag)
	if ($objectSpaceFlagIndex -lt 0 -or $objectSpaceArguments[$objectSpaceFlagIndex + 1] -ne $objectSpaceRequiredArguments[$objectSpaceFlag]) {
		throw "Missing required bounded independent-process control: $objectSpaceFlag"
	}
}
if (!($objectSpaceArguments -contains '--noForcedGCs') -or !($objectSpaceArguments -contains '--keepFiles')) { throw 'Require --noForcedGCs and --keepFiles.' }
$objectSpaceRecipe = [PSCustomObject]@{ Stage = $objectSpacePlan.Stage; Plan = $objectSpacePlanPath; Executable = $objectSpaceExecutable
	Output = $objectSpaceDestination; DataRoot = $objectSpaceDataPath; Arguments = $objectSpaceArguments; Profiles = @($objectSpaceProfiles.Name)
	ExecuteOnlyAfter = $objectSpacePlan.ExecuteOnlyAfter }
if (!$ExecuteMeasurements) {
	$objectSpaceRecipe | ConvertTo-Json -Depth 100
	return
}
if (!(Test-Path -LiteralPath $objectSpaceExecutable)) { throw "Build the Release profiler before acquiring the serial execution slot: $objectSpaceExecutable" }
if ((Test-Path -LiteralPath $objectSpaceDestination) -and @(Get-ChildItem -LiteralPath $objectSpaceDestination -Force).Count -gt 0) {
	throw "Existing retained evidence must not be overwritten: $objectSpaceDestination"
}
$objectSpaceCompetingProcesses = @(Get-CimInstance Win32_Process | Where-Object {
	$_.Name -in @('Sphere10.Framework.Utils.Profiler.exe', 'testhost.exe') -or
		($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'Sphere10\.Framework\.Utils\.Profiler|testhost|\bdotnet\s+test\b')
})
if ($objectSpaceCompetingProcesses.Count -gt 0) { throw 'Another profiler/test process is active; acquire the exclusive execution slot before starting this run.' }
. (Join-Path $objectSpaceFrameworkRoot 'BenchmarkDotNet.Artifacts/retained-worker-evidence.ps1')
New-Item -ItemType Directory -Force -Path $objectSpaceDestination | Out-Null
Copy-Item -LiteralPath $objectSpaceProfilesPath -Destination $objectSpaceRetainedProfiles
Copy-Item -LiteralPath $objectSpacePlanPath -Destination (Join-Path $objectSpaceDestination 'plan.json')
Copy-Item -LiteralPath $objectSpaceSelectionPath -Destination (Join-Path $objectSpaceDestination 'selection.json')
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $objectSpaceDestination 'capture-launcher.ps1')
Copy-Item -LiteralPath (Join-Path $objectSpaceFrameworkRoot 'BenchmarkDotNet.Artifacts/retained-worker-evidence.ps1') -Destination (Join-Path $objectSpaceDestination 'capture-workers.ps1')
$objectSpaceRecipe | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $objectSpaceDestination 'execution-recipe.json') -Encoding utf8
$objectSpaceArguments | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $objectSpaceDestination 'command-arguments.json') -Encoding utf8
$objectSpaceCommand = '& ' + ("'" + $objectSpaceExecutable.Replace("'", "''") + "'") + ' ' + (($objectSpaceArguments | ForEach-Object { "'" + $_.Replace("'", "''") + "'" }) -join ' ')
$objectSpaceCommand | Set-Content -LiteralPath (Join-Path $objectSpaceDestination 'command.ps1') -Encoding utf8
$objectSpacePowerBefore = (& powercfg /getactivescheme | Out-String).Trim()
$objectSpaceAssemblyFingerprints = @('Sphere10.Framework.Utils.Profiler.dll', 'Sphere10.Framework.dll') | ForEach-Object {
	Get-RetainedAssemblyIdentity -Path (Join-Path (Split-Path $objectSpaceExecutable) $_)
}
$objectSpaceDrive = [System.IO.Path]::GetPathRoot($objectSpaceDataPath).TrimEnd('\')
$objectSpaceLogicalDisk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='$objectSpaceDrive'"
$objectSpacePartitions = @($objectSpaceLogicalDisk | Get-CimAssociatedInstance -Association Win32_LogicalDiskToPartition)
$objectSpaceMappedDisks = @($objectSpacePartitions | Get-CimAssociatedInstance -Association Win32_DiskDriveToDiskPartition | Select-Object Index, Model, InterfaceType, Size)
$objectSpaceStartedAtUtc = [DateTime]::UtcNow
[PSCustomObject]@{
	StartedAtUtc = $objectSpaceStartedAtUtc; Executable = $objectSpaceExecutable; Arguments = $objectSpaceArguments; Plan = $objectSpacePlanPath
	PowerPlan = $objectSpacePowerBefore; Assemblies = @($objectSpaceAssemblyFingerprints)
	CaptureScriptHashes = @('capture-launcher.ps1', 'capture-workers.ps1') | ForEach-Object {
		Get-FileHash -LiteralPath (Join-Path $objectSpaceDestination $_) -Algorithm SHA256 | Select-Object Path, Hash, Algorithm
	}
	Resources = [PSCustomObject]@{ DataRoot = $objectSpaceDataPath; LogicalDisk = $objectSpaceLogicalDisk | Select-Object DeviceID, DriveType, FileSystem, Size, FreeSpace, VolumeName
		DataDiskMapping = $objectSpaceMappedDisks; PhysicalDisks = @(Get-CimInstance Win32_DiskDrive | Select-Object Index, Model, InterfaceType, Size)
		MemoryKilobytes = Get-CimInstance Win32_OperatingSystem | Select-Object TotalVisibleMemorySize, FreePhysicalMemory }
	Controls = 'Two independent launches, ten actual iterations, five warmups, Monitoring, invocation/unroll1, no removed outliers, no forced GC, keep generated worker files, unchanged power plan.'
} | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $objectSpaceDestination 'execution-host.json') -Encoding utf8
& $objectSpaceExecutable @objectSpaceArguments 2>&1 | Tee-Object -FilePath (Join-Path $objectSpaceDestination 'console.log')
$objectSpaceDomainExitCode = $LASTEXITCODE
$objectSpaceFinishedAtUtc = [DateTime]::UtcNow
[PSCustomObject]@{
	DomainExitCode = $objectSpaceDomainExitCode; RecordedAtUtc = $objectSpaceFinishedAtUtc; CapturedAfterRun = $false
	Origin = 'Captured by launcher immediately after domain process exit, before any other native command or failure guard.'
	StartedAtUtc = $objectSpaceStartedAtUtc; ElapsedSeconds = ($objectSpaceFinishedAtUtc - $objectSpaceStartedAtUtc).TotalSeconds
} | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $objectSpaceDestination 'execution-result.json') -Encoding utf8
$objectSpaceArchiveRoot = Join-Path $objectSpaceFrameworkRoot ('BenchmarkDotNet.Artifacts/objectspace-post-default-confirmation/worker-archives/' + [Guid]::NewGuid().ToString('N'))
$objectSpaceCaptureError = $null
try {
	$objectSpaceWorkerEvidence = Save-RetainedWorkerEvidence -ConsolePath (Join-Path $objectSpaceDestination 'console.log') `
		-EvidencePath (Join-Path $objectSpaceDestination 'worker-binary-fingerprints.json') -IgnoredArchiveRoot $objectSpaceArchiveRoot
} catch {
	$objectSpaceCaptureError = $_.Exception.Message
	[PSCustomObject]@{ CapturedAtUtc = [DateTime]::UtcNow; Error = $objectSpaceCaptureError; DomainExitCode = $objectSpaceDomainExitCode; ArchiveRoot = $objectSpaceArchiveRoot } |
		ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $objectSpaceDestination 'worker-capture-failure.json') -Encoding utf8
}
$objectSpacePowerAfter = (& powercfg /getactivescheme | Out-String).Trim()
[PSCustomObject]@{ Before = $objectSpacePowerBefore; After = $objectSpacePowerAfter; Unchanged = $objectSpacePowerBefore -eq $objectSpacePowerAfter } |
	ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $objectSpaceDestination 'power-plan.json') -Encoding utf8
if ($objectSpaceDomainExitCode -ne 0) { throw "Domain process exited $objectSpaceDomainExitCode. Preserve all failure evidence at $objectSpaceDestination. Worker capture: $objectSpaceCaptureError" }
if ($null -ne $objectSpaceCaptureError) { throw "Actual worker capture failed: $objectSpaceCaptureError. Preserve evidence at $objectSpaceDestination." }
if ($objectSpacePowerBefore -ne $objectSpacePowerAfter) { throw 'Power plan changed during measurements; retain evidence and investigate before interpretation.' }
$objectSpaceWorkerExitLines = @(Select-String -LiteralPath (Join-Path $objectSpaceDestination 'console.log') -Pattern '^// Benchmark Process \d+ has exited with code (-?\d+)\.$')
if ($objectSpaceWorkerExitLines.Count -ne 8 -or @($objectSpaceWorkerExitLines | Where-Object { $_.Line -notmatch 'code 0\.$' }).Count -ne 0) {
	throw 'Require eight actual successful benchmark worker exits; preserve incomplete/failing evidence.'
}
if (($objectSpaceWorkerEvidence.Workers | Measure-Object -Property Executions -Sum).Sum -ne 8) { throw 'Recorded worker Execute paths do not cover all eight launches.' }
$objectSpaceReportPaths = @(Get-ChildItem -LiteralPath (Join-Path $objectSpaceDestination 'results') -Filter '*-report-full.json' -File)
$objectSpaceCases = @()
foreach ($objectSpaceReportPath in $objectSpaceReportPaths) {
	$objectSpaceReport = Get-Content -LiteralPath $objectSpaceReportPath.FullName -Raw | ConvertFrom-Json -Depth 100
	foreach ($objectSpaceBenchmark in $objectSpaceReport.Benchmarks) {
		$objectSpaceMatchingProfiles = @($objectSpaceProfiles | Where-Object {
			$objectSpaceBenchmark.FullName.EndsWith('(Profile: "' + $_.Name + '")', [StringComparison]::Ordinal)
		})
		if ($objectSpaceBenchmark.Method -ne 'MixedReadWriteAndPersist' -or $objectSpaceMatchingProfiles.Count -ne 1) { throw "Untraceable measured case: $($objectSpaceBenchmark.FullName)" }
		$objectSpaceStatistics = $objectSpaceBenchmark.Statistics
		$objectSpaceOriginalValues = @($objectSpaceStatistics.OriginalValues | ForEach-Object { [double]$_ })
		if ($null -eq $objectSpaceStatistics -or $objectSpaceStatistics.N -ne 20 -or $objectSpaceOriginalValues.Count -ne 20) { throw "Require twenty measured original samples: $($objectSpaceBenchmark.FullName)" }
		if (@($objectSpaceOriginalValues | Where-Object { $_ -le 0 -or ![double]::IsFinite($_) }).Count -ne 0) { throw 'Invalid measured duration.' }
		$objectSpaceMean = ($objectSpaceOriginalValues | Measure-Object -Average).Average
		if ([Math]::Abs($objectSpaceMean - $objectSpaceStatistics.Mean) -gt [Math]::Max(0.0001, [Math]::Abs($objectSpaceMean) * 1E-12)) { throw 'Statistics mean does not reconcile with original samples.' }
		$objectSpaceMeasurements = @($objectSpaceBenchmark.Measurements | Where-Object { $_.IterationMode -eq 'Workload' })
		$objectSpaceLaunches = @($objectSpaceMeasurements | Group-Object LaunchIndex)
		if ($objectSpaceLaunches.Count -ne 2) { throw 'Require two actual independent launch groups per case.' }
		foreach ($objectSpaceLaunch in $objectSpaceLaunches) {
			foreach ($objectSpaceStage in @('Warmup', 'Actual', 'Result')) {
				$objectSpaceStageRows = @($objectSpaceLaunch.Group | Where-Object { $_.IterationStage -eq $objectSpaceStage })
				$objectSpaceExpectedCount = if ($objectSpaceStage -eq 'Warmup') { 5 } else { 10 }
				if ($objectSpaceStageRows.Count -ne $objectSpaceExpectedCount -or @($objectSpaceStageRows | Where-Object { $_.Operations -ne 1 }).Count -gt 0) {
					throw "Unexpected $objectSpaceStage batch count/invocations: $($objectSpaceBenchmark.FullName)"
				}
			}
		}
		$objectSpaceSortedOriginal = @($objectSpaceOriginalValues | Sort-Object)
		foreach ($objectSpaceStage in @('Actual', 'Result')) {
			$objectSpaceStageSamples = @($objectSpaceMeasurements | Where-Object { $_.IterationStage -eq $objectSpaceStage } | ForEach-Object { [double]$_.Nanoseconds / [double]$_.Operations } | Sort-Object)
			for ($objectSpaceSampleIndex = 0; $objectSpaceSampleIndex -lt 20; $objectSpaceSampleIndex++) {
				if ([Math]::Abs($objectSpaceStageSamples[$objectSpaceSampleIndex] - $objectSpaceSortedOriginal[$objectSpaceSampleIndex]) -gt [Math]::Max(0.0001, [Math]::Abs($objectSpaceSortedOriginal[$objectSpaceSampleIndex]) * 1E-12)) {
					throw "$objectSpaceStage durations do not reconcile with original statistics samples."
				}
			}
		}
		$objectSpaceCases += [PSCustomObject]@{ Profile = $objectSpaceMatchingProfiles[0].Name; FullName = $objectSpaceBenchmark.FullName; Report = $objectSpaceReportPath.FullName
			Samples = 20; Launches = 2; MeanNanoseconds = $objectSpaceMean; OriginalNanoseconds = $objectSpaceOriginalValues }
	}
}
if ($objectSpaceCases.Count -ne 4 -or @($objectSpaceCases.Profile | Select-Object -Unique).Count -ne 4) { throw 'Require all four exact profiles, each measured once across two launches.' }
[PSCustomObject]@{ CapturedAtUtc = [DateTime]::UtcNow; DomainExitCode = $objectSpaceDomainExitCode; WorkerExitCount = 8; Cases = $objectSpaceCases
	Reconciled = 'All four exact FullName profiles, eight successful worker launches, 20 original samples per case and matching Workload Actual/Result measurements.' } |
	ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $objectSpaceDestination 'execution-validation.json') -Encoding utf8
Write-Output "Completed retained post-default ObjectSpace confirmation at $objectSpaceDestination in $(($objectSpaceFinishedAtUtc - $objectSpaceStartedAtUtc).TotalSeconds.ToString('F1')) seconds."
