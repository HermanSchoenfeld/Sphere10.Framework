#requires -Version 7.2

# Artifact helper: dot-sourcing only defines functions; binary capture starts when explicitly called.
function Get-RetainedAssemblyIdentity {
	param([Parameter(Mandatory = $true)][string]$Path)
	$workerAssemblyFile = Get-Item -LiteralPath $Path
	$workerVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($workerAssemblyFile.FullName)
	$workerMetadataStream = $null
	$workerPeReader = $null
	try {
		$workerMetadataStream = [System.IO.File]::OpenRead($workerAssemblyFile.FullName)
		$workerPeReader = [System.Reflection.PortableExecutable.PEReader]::new($workerMetadataStream)
		$workerMetadataReader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($workerPeReader)
		$workerMvid = $workerMetadataReader.GetGuid($workerMetadataReader.GetModuleDefinition().Mvid).ToString()
	} finally {
		if ($workerPeReader) { $workerPeReader.Dispose() }
		if ($workerMetadataStream) { $workerMetadataStream.Dispose() }
	}
	[PSCustomObject]@{
		Path = $workerAssemblyFile.FullName
		Sha256 = (Get-FileHash -LiteralPath $workerAssemblyFile.FullName -Algorithm SHA256).Hash
		Length = $workerAssemblyFile.Length
		LastWriteTimeUtc = $workerAssemblyFile.LastWriteTimeUtc
		ModuleVersionId = $workerMvid
		AssemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($workerAssemblyFile.FullName).Version.ToString()
		FileVersion = $workerVersion.FileVersion
		InformationalVersion = $workerVersion.ProductVersion
		InformationalVersionSource = 'PE ProductVersion populated by SDK assembly informational version.'
	}
}

function Save-RetainedWorkerEvidence {
	param(
		[Parameter(Mandatory = $true)][string]$ConsolePath,
		[Parameter(Mandatory = $true)][string]$EvidencePath,
		[Parameter(Mandatory = $true)][string]$IgnoredArchiveRoot
	)
	$workerConsoleLines = @(Get-Content -LiteralPath $ConsolePath)
	$workerCommands = @(for ($workerLineIndex = 0; $workerLineIndex -lt $workerConsoleLines.Count; $workerLineIndex++) {
		$workerLine = $workerConsoleLines[$workerLineIndex]
		if ($workerLine -match '^// Execute: dotnet (?<App>[^ ]+) .* in (?<Directory>.+)$') {
			[PSCustomObject]@{
				Application = $Matches.App.Trim('"')
				Directory = [System.IO.Path]::GetFullPath($Matches.Directory.Trim()).TrimEnd('\', '/')
				OriginalCommand = $workerLine
				ConsoleLine = $workerLineIndex + 1
			}
		}
	})
	if ($workerCommands.Count -eq 0) { throw "No actual generated worker Execute paths found in $ConsolePath" }
	$workerBuildCommands = @($workerConsoleLines | Where-Object { $_ -match '^// start dotnet\s+build .* in .+$' })
	$workerArchiveRoot = [System.IO.Path]::GetFullPath($IgnoredArchiveRoot)
	if (Test-Path -LiteralPath $workerArchiveRoot) { throw "Use a fresh uniquely owned worker archive: $workerArchiveRoot" }
	New-Item -ItemType Directory -Path $workerArchiveRoot | Out-Null
	$workerFingerprints = @($workerCommands | Group-Object Directory | ForEach-Object {
		$workerGroup = $_
		$workerDirectory = $workerGroup.Name
		$workerMatches = @($workerBuildCommands | Where-Object {
			if ($_ -match '/p:OutDir="(?<Output>[^"]+)"') {
				[System.IO.Path]::GetFullPath($Matches.Output).TrimEnd('\', '/') -eq $workerDirectory
			} else { $false }
		})
		if ($workerMatches.Count -eq 0) { throw "Worker Execute directory has no matching generated build OutDir: $workerDirectory" }
		$workerArchive = Join-Path $workerArchiveRoot ('worker-' + [Guid]::NewGuid().ToString('N'))
		New-Item -ItemType Directory -Path $workerArchive | Out-Null
		$workerApps = @($workerGroup.Group | Select-Object -ExpandProperty Application -Unique)
		$workerAssemblyNames = @('Sphere10.Framework.dll', 'Sphere10.Framework.Utils.Profiler.dll') + $workerApps
		$workerAssemblies = @($workerAssemblyNames | Select-Object -Unique | ForEach-Object {
			$workerAssemblyPath = Join-Path $workerDirectory $_
			if (!(Test-Path -LiteralPath $workerAssemblyPath)) { throw "Executed worker assembly is absent: $workerAssemblyPath" }
			$workerIdentity = Get-RetainedAssemblyIdentity -Path $workerAssemblyPath
			$workerArchivedAssembly = Join-Path $workerArchive $_
			Copy-Item -LiteralPath $workerAssemblyPath -Destination $workerArchivedAssembly
			$workerArchiveHash = (Get-FileHash -LiteralPath $workerArchivedAssembly -Algorithm SHA256).Hash
			if ($workerArchiveHash -ne $workerIdentity.Sha256) { throw "Worker archive hash mismatch: $workerArchivedAssembly" }
			$workerIdentity | Add-Member -NotePropertyName ArchivedPath -NotePropertyValue $workerArchivedAssembly
			$workerIdentity | Add-Member -NotePropertyName ArchivedSha256 -NotePropertyValue $workerArchiveHash
			$workerIdentity
		})
		$workerProjectDirectory = Split-Path (Split-Path (Split-Path $workerDirectory))
		$workerProjectFiles = @(Get-ChildItem -LiteralPath $workerProjectDirectory -File | Where-Object { $_.Extension -in @('.csproj', '.notcs', '.cs') } | ForEach-Object {
			$workerProjectFile = $_
			$workerProjectArchive = Join-Path $workerArchive $workerProjectFile.Name
			Copy-Item -LiteralPath $workerProjectFile.FullName -Destination $workerProjectArchive
			$workerProjectHash = (Get-FileHash -LiteralPath $workerProjectFile.FullName -Algorithm SHA256).Hash
			$workerProjectArchiveHash = (Get-FileHash -LiteralPath $workerProjectArchive -Algorithm SHA256).Hash
			if ($workerProjectHash -ne $workerProjectArchiveHash) { throw "Generated project archive hash mismatch: $workerProjectArchive" }
			[PSCustomObject]@{ Path = $workerProjectFile.FullName; ArchivedPath = $workerProjectArchive; Length = $workerProjectFile.Length
				Sha256 = $workerProjectHash; ArchivedSha256 = $workerProjectArchiveHash; LastWriteTimeUtc = $workerProjectFile.LastWriteTimeUtc }
		})
		if (@($workerProjectFiles | Where-Object { [System.IO.Path]::GetExtension($_.Path) -eq '.csproj' }).Count -eq 0) {
			throw "Generated worker project is absent: $workerProjectDirectory"
		}
		[PSCustomObject]@{
			Directory = $workerDirectory
			OriginalConsoleCommands = @($workerGroup.Group | Select-Object -ExpandProperty OriginalCommand)
			Executions = $workerGroup.Count
			Assemblies = $workerAssemblies
			ArchiveDirectory = $workerArchive
			GeneratedProjectFiles = $workerProjectFiles
			MatchingOriginalBuildCommands = $workerMatches
			ConsoleExecutionLineNumbers = @($workerGroup.Group | Select-Object -ExpandProperty ConsoleLine)
		}
	})
	$workerEvidence = [PSCustomObject]@{
		FormatVersion = 1
		CapturedAtUtc = [DateTime]::UtcNow
		CapturedAfterRun = $true
		CapturedAfterWorkersExited = $true
		Origin = 'capturedafterrunfromretainedworkerbuild'
		CaptureDetail = 'Original console Execute paths cross-checked with generated build OutDir, captured after domain exit before subsequent folder reuse.'
		OriginalConsolePath = [System.IO.Path]::GetFullPath($ConsolePath)
		OriginalConsoleSha256 = (Get-FileHash -LiteralPath $ConsolePath -Algorithm SHA256).Hash
		ArchiveRoot = $workerArchiveRoot
		ArchiveScope = 'Local ignored BenchmarkDotNet.Artifacts only; DLL archives are never force-added or required on a fresh clone.'
		Workers = $workerFingerprints
	}
	$workerEvidence | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $EvidencePath -Encoding utf8
	$workerEvidence
}
