param(
	[Parameter(Mandatory = $true)]
	[string]$InteractionDirectory,
	[string]$EvidenceDirectory,
	[switch]$ExecuteBenchmarks
)

$ErrorActionPreference = 'Stop'
$frameworkRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$profilerExecutable = Join-Path $frameworkRoot 'utils/Sphere10.Framework.Utils.Profiler/bin/Release/net10.0/Sphere10.Framework.Utils.Profiler.exe'
$dataRoot = Join-Path $frameworkRoot 'BenchmarkDotNet.Artifacts/study-data'
if ([string]::IsNullOrWhiteSpace($EvidenceDirectory)) {
	$EvidenceDirectory = Join-Path $frameworkRoot 'tests/Sphere10.Framework.Utils.Profiler.Tests/Results/20261013-objectspaces-bdn-confirmations'
}
$EvidenceDirectory = [System.IO.Path]::GetFullPath($EvidenceDirectory)
$InteractionDirectory = [System.IO.Path]::GetFullPath($InteractionDirectory)

function Get-CandidateStatistics($Result, [int]$ExpectedCount) {
	$samples = @($Result.Samples)
	if (!$Result.Verified -or $null -ne $Result.Error -or $samples.Count -ne $ExpectedCount -or $ExpectedCount -lt 3) {
		throw "Interaction case is not complete and verified: $($Result.Case.Id)"
	}
	$rounds = @($samples | ForEach-Object { [int]$_.Round } | Sort-Object)
	for ($index = 0; $index -lt $ExpectedCount; $index++) {
		if ($rounds[$index] -ne $index) { throw "Interaction rounds are incomplete: $($Result.Case.Id)" }
	}
	$values = @($samples | ForEach-Object { [double]$_.Nanoseconds })
	if (@($values | Where-Object { $_ -le 0 -or [double]::IsNaN($_) -or [double]::IsInfinity($_) }).Count -ne 0) {
		throw "Invalid interaction timing: $($Result.Case.Id)"
	}
	$mean = ($values | Measure-Object -Average).Average
	$sumSquares = 0.0
	foreach ($value in $values) { $sumSquares += [Math]::Pow($value - $mean, 2) }
	$deviation = [Math]::Sqrt($sumSquares / ($values.Count - 1))
	$degrees = $ExpectedCount - 1
	$critical = switch ($degrees) {
		2 { 4.303 } 3 { 3.182 } 4 { 2.776 } 5 { 2.571 } 6 { 2.447 } 7 { 2.365 } 8 { 2.306 } 9 { 2.262 }
		default { if ($degrees -le 19) { 2.262 } elseif ($degrees -le 29) { 2.093 } elseif ($degrees -le 59) { 2.045 } elseif ($degrees -le 99) { 2.001 } else { 1.984 } }
	}
	[PSCustomObject]@{
		Result = $Result
		MeanNanoseconds = $mean
		ConfidenceHalfWidth = $critical * $deviation / [Math]::Sqrt($ExpectedCount)
	}
}

function Quote-PowerShellArgument([string]$Value) {
	"'" + $Value.Replace("'", "''") + "'"
}

$interactionPlan = Get-Content -LiteralPath (Join-Path $InteractionDirectory 'run-plan.json') -Raw | ConvertFrom-Json -Depth 100
$interactionResults = @(Get-Content -LiteralPath (Join-Path $InteractionDirectory 'results.json') -Raw | ConvertFrom-Json -Depth 100)
if ($interactionPlan.Stage -ne 'Screening' -or $interactionResults.Count -ne 18 -or $interactionPlan.Cases.Count -ne 18) {
	throw 'Require the completed 18-case Memory/File interaction study before preparing confirmations.'
}
$statistics = @($interactionResults | ForEach-Object { Get-CandidateStatistics $_ $interactionPlan.SampleCount })
$plannedIds = @($interactionPlan.Cases | ForEach-Object { $_.Id } | Sort-Object)
$observedIds = @($interactionResults | ForEach-Object { $_.Case.Id } | Sort-Object)
if (($plannedIds -join [Environment]::NewLine) -ne ($observedIds -join [Environment]::NewLine)) { throw 'Interaction result IDs do not match the plan.' }

$matrix = @()
foreach ($mix in @('Balanced', 'ReadHeavy', 'WriteHeavy')) {
	$mixDirectory = Join-Path $EvidenceDirectory $mix
	New-Item -ItemType Directory -Force -Path $mixDirectory | Out-Null
	if (Test-Path -LiteralPath (Join-Path $mixDirectory 'run.json')) {
		throw "Existing benchmark evidence must not be overwritten: $mixDirectory"
	}
	$profiles = @()
	$decisions = @()
	foreach ($storage in @('Memory', 'File')) {
		$candidates = @($statistics | Where-Object {
			$settings = $_.Result.Case.ObjectSpaces
			$settings.WorkloadMix -eq $mix -and $settings.Profiles[0].Configuration.Storage -eq $storage
		} | Sort-Object MeanNanoseconds)
		if ($candidates.Count -ne 3) { throw "Expected three verified interaction candidates for $storage/$mix." }
		$roles = @($candidates | ForEach-Object { $_.Result.Case.ObjectSpaces.Profiles[0].Name } | Sort-Object)
		if (($roles -join ',') -ne ((@('baseline-64', 'combined-candidate', 'order-only') | Sort-Object) -join ',')) {
			throw "Unexpected interaction candidate roles for $storage/$mix."
		}
		$best = $candidates[0]
		$overlapping = @($candidates | Where-Object { $_.MeanNanoseconds - $_.ConfidenceHalfWidth -le $best.MeanNanoseconds + $best.ConfidenceHalfWidth })
		foreach ($candidate in $candidates) {
			$settings = $candidate.Result.Case.ObjectSpaces
			if (
				$settings.RecordCount -ne 1024 -or $settings.OperationCount -ne 200 -or $settings.PayloadBytes -ne 128 -or $settings.CategoryCount -ne 1024 -or
				$settings.HotCategoryPercent -ne 0 -or !$settings.ShuffledInsertion -or $settings.LookupPattern -ne 'Shuffled' -or $settings.PayloadPattern -ne 'Fixed'
			) { throw "Unexpected confirmation dataset in $($candidate.Result.Case.Id)" }
			$sourceProfile = $settings.Profiles[0]
			$policyText = [string]$sourceProfile.Configuration.Policy
			$hasIntegrityChecks = $policyText -in @('Default', 'Debug') -or $policyText.Contains('IntegrityChecks')
			if (
				$sourceProfile.Configuration.IndexProfile -ne 'Full' -or $sourceProfile.Configuration.SchemaProfile -ne 'PayloadOnly' -or
				$sourceProfile.Configuration.Merkleized -or $sourceProfile.Configuration.Endianness -ne 'LittleEndian' -or !$hasIntegrityChecks
			) { throw "Unexpected confirmation guarantees in $($candidate.Result.Case.Id)" }
			$configuration = $sourceProfile.Configuration | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100
			$profiles += [PSCustomObject]@{ Name = "$($storage.ToLowerInvariant())-$($sourceProfile.Name)"; Configuration = $configuration }
		}
		$decisions += [PSCustomObject]@{
			Storage = $storage
			Mix = $mix
			LowestInteractionMean = $best.Result.Case.ObjectSpaces.Profiles[0].Name
			UnresolvedCandidates = @($overlapping | ForEach-Object { $_.Result.Case.ObjectSpaces.Profiles[0].Name })
			AllCandidatesConfirmed = @($candidates | ForEach-Object {
				[PSCustomObject]@{ CaseId = $_.Result.Case.Id; Profile = $_.Result.Case.ObjectSpaces.Profiles[0].Name; MeanNanoseconds = $_.MeanNanoseconds; ConfidenceHalfWidth = $_.ConfidenceHalfWidth }
			})
			Meaning = 'Screening candidates and overlapping intervals; BenchmarkDotNet independently confirms all three competing configurations.'
		}
	}
	$configPath = Join-Path $mixDirectory 'profiles.json'
	$profiles | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $configPath -Encoding utf8
	$decisions | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $mixDirectory 'interaction-selection.json') -Encoding utf8
	$arguments = @(
		'object-spaces', '--config', $configPath, '--records', '1024', '--operations', '200', '--payload', '128',
		'--categories', '1024', '--hot-category-percent', '0', '--lookup', 'Shuffled', '--payload-mode', 'Fixed',
		'--insertion', 'shuffled', '--mix', $mix, '--filter', '*MixedReadWriteAndPersist*',
		'--launchCount', '2', '--iterationCount', '10', '--warmupCount', '5', '--strategy', 'Monitoring',
		'--invocationCount', '1', '--unrollFactor', '1', '--outliers', 'DontRemove', '--noForcedGCs',
		'--output', $mixDirectory, '--data-root', $dataRoot
	)
	$command = '& ' + (Quote-PowerShellArgument $profilerExecutable) + ' ' + (($arguments | ForEach-Object { Quote-PowerShellArgument $_ }) -join ' ')
	$command | Set-Content -LiteralPath (Join-Path $mixDirectory 'command.ps1') -Encoding utf8
	$arguments | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $mixDirectory 'command-arguments.json') -Encoding utf8
	$matrix += [PSCustomObject]@{ Mix = $mix; Directory = $mixDirectory; Profiles = $profiles.Count; MeasuredSamplesPerProfile = 20; Arguments = $arguments }
	if ($ExecuteBenchmarks) {
		if (!(Test-Path -LiteralPath $profilerExecutable)) { throw "Build the Release profiler before execution: $profilerExecutable" }
		$powerPlanBefore = (& powercfg /getactivescheme | Out-String).Trim()
		$fingerprints = @('Sphere10.Framework.Utils.Profiler.dll', 'Sphere10.Framework.dll') | ForEach-Object {
			Get-FileHash -LiteralPath (Join-Path (Split-Path $profilerExecutable) $_) -Algorithm SHA256 | Select-Object Path, Hash, Algorithm
		}
		[PSCustomObject]@{
			StartedAtUtc = [DateTime]::UtcNow
			InteractionDirectory = $InteractionDirectory
			PowerPlan = $powerPlanBefore
			Assemblies = $fingerprints
			Controls = 'Two independent launches, ten measured iterations, five warmups, Monitoring, one invocation, no outlier removal, no forced GC, current power plan retained.'
		} | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $mixDirectory 'execution-host.json') -Encoding utf8
		& $profilerExecutable @arguments 2>&1 | Tee-Object -FilePath (Join-Path $mixDirectory 'console.log')
		$runExitCode = $LASTEXITCODE
		if ($runExitCode -ne 0) { throw "Benchmark exit code $runExitCode for $mix; retain its output." }
		$powerPlanAfter = (& powercfg /getactivescheme | Out-String).Trim()
		if ($powerPlanAfter -ne $powerPlanBefore) { throw 'Power plan changed during measurements; investigate before interpreting results.' }
		$reports = @(Get-ChildItem -LiteralPath (Join-Path $mixDirectory 'results') -Filter '*-report-full.json' -File)
		if ($reports.Count -eq 0) { throw "No Full JSON benchmark report for $mix." }
		foreach ($report in $reports) {
			$full = Get-Content -LiteralPath $report.FullName -Raw | ConvertFrom-Json -Depth 100
			foreach ($benchmark in $full.Benchmarks) {
				if ($null -eq $benchmark.Statistics -or $benchmark.Statistics.N -ne 20) {
					throw "Expected 20 measured samples: $($benchmark.DisplayInfo)"
				}
			}
		}
	}
}
$matrix | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $EvidenceDirectory 'confirmation-matrix.json') -Encoding utf8
Write-Output "Prepared 3 mix runs, 18 profiles, 20 measured samples each. ExecuteBenchmarks=$ExecuteBenchmarks"
