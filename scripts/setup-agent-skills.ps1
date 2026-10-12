#Requires -Version 7.2

<#
.SYNOPSIS
Creates or checks local aliases to the repository's shared skills and instructions.
.DESCRIPTION
Windows uses directory junctions and file hardlinks without elevation. Other
platforms use relative symbolic links. Aliases and Windows ownership metadata
are local, ignored files; tracked instructions and skills remain the only source.
Existing directories and independently maintained files are never overwritten.
.PARAMETER Check
Validates aliases without creating, deleting or updating filesystem entries.
.PARAMETER RepositoryRoot
Repository checkout to configure; defaults to the script's parent directory.
#>
[CmdletBinding()]
param(
	[switch]$Check,
	[string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryPath = [System.IO.Path]::TrimEndingDirectorySeparator([System.IO.Path]::GetFullPath($RepositoryRoot))
$pathComparison = if ($IsWindows) { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }
$repositoryPrefix = $repositoryPath + [System.IO.Path]::DirectorySeparatorChar
$stateRelativePath = 'agents/.local-alias-state.json'
$failures = [System.Collections.Generic.List[string]]::new()
$plans = [System.Collections.Generic.List[object]]::new()
$state = @{ Version = 2; Aliases = @{} }
$stateChanged = $false

function Get-PathItem([string]$Path) {
	try {
		$attributes = [System.IO.File]::GetAttributes($Path)
	} catch [System.IO.FileNotFoundException] {
		return $null
	} catch [System.IO.DirectoryNotFoundException] {
		return $null
	}
	if ($attributes -band [System.IO.FileAttributes]::Directory) {
		return [System.IO.DirectoryInfo]::new($Path)
	}
	return [System.IO.FileInfo]::new($Path)
}

function Assert-WorkspacePath([string]$Path) {
	$absolutePath = [System.IO.Path]::GetFullPath($Path)
	if (-not $absolutePath.Equals($repositoryPath, $pathComparison) -and -not $absolutePath.StartsWith($repositoryPrefix, $pathComparison)) {
		throw "Path escapes the named repository: $absolutePath"
	}
	return $absolutePath
}

function Assert-WorkspaceParents([string]$Path) {
	$parentPath = [System.IO.Path]::GetDirectoryName((Assert-WorkspacePath $Path))
	while ($parentPath -and -not $parentPath.Equals($repositoryPath, $pathComparison)) {
		$parentItem = Get-PathItem $parentPath
		if ($null -ne $parentItem) {
			if (-not ($parentItem.Attributes -band [System.IO.FileAttributes]::Directory)) {
				throw "An alias parent is not a directory: $parentPath"
			}
			if ($parentItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
				$resolvedParent = $parentItem.ResolveLinkTarget($true)
				if ($null -eq $resolvedParent -or -not $resolvedParent.Exists) {
					throw "An alias parent is a broken link: $parentPath"
				}
				Assert-WorkspacePath $resolvedParent.FullName | Out-Null
			}
		}
		$parentPath = [System.IO.Path]::GetDirectoryName($parentPath)
	}
}

function Get-RepositoryPath([string]$RelativePath) {
	$absolutePath = Assert-WorkspacePath (Join-Path $repositoryPath $RelativePath)
	Assert-WorkspaceParents $absolutePath
	return $absolutePath
}

function Test-LinkTarget([System.IO.FileSystemInfo]$Item, [string]$TargetPath) {
	try {
		$resolvedTarget = $Item.ResolveLinkTarget($true)
		return $null -ne $resolvedTarget -and $resolvedTarget.Exists -and $resolvedTarget.FullName.Equals($TargetPath, $pathComparison)
	} catch [System.IO.IOException] {
		return $false
	}
}

function Remove-AliasLink([System.IO.FileSystemInfo]$Item) {
	Assert-WorkspacePath $Item.FullName | Out-Null
	Assert-WorkspaceParents $Item.FullName
	# Never recurse or access the old target; moved-checkout links may point elsewhere.
	if ($Item.Attributes -band [System.IO.FileAttributes]::Directory) {
		[System.IO.Directory]::Delete($Item.FullName, $false)
	} else {
		[System.IO.File]::Delete($Item.FullName)
	}
}

# Reject a linked repository root so containment checks use one unambiguous workspace.
$repositoryItem = Get-PathItem $repositoryPath
if ($null -eq $repositoryItem -or -not ($repositoryItem.Attributes -band [System.IO.FileAttributes]::Directory)) {
	throw "Repository directory does not exist: $repositoryPath"
}
if ($repositoryItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
	throw "Use the physical repository directory, not a linked root: $repositoryPath"
}

# File identity distinguishes real hardlinks from equal-content files and detects atomic replacements.
$identityType = 'Sphere10.AgentSetup.WindowsFileIdentity' -as [type]
if ($IsWindows -and $null -ne $identityType -and $null -eq $identityType.GetMethod('GetLegacy')) {
	throw 'Restart PowerShell to load the updated Windows file identity implementation.'
}
if ($IsWindows -and -not ('Sphere10.AgentSetup.WindowsFileIdentity' -as [type])) {
	Add-Type -TypeDefinition @"
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Sphere10.AgentSetup;

public static class WindowsFileIdentity {
	[StructLayout(LayoutKind.Sequential)]
	private struct FileIdInformation {
		public ulong VolumeSerialNumber;
		public ulong FileIdLow;
		public ulong FileIdHigh;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct LegacyFileInformation {
		public uint Attributes;
		public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
		public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
		public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
		public uint VolumeSerialNumber;
		public uint FileSizeHigh;
		public uint FileSizeLow;
		public uint NumberOfLinks;
		public uint FileIndexHigh;
		public uint FileIndexLow;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetFileInformationByHandleEx(SafeFileHandle file, int informationClass, out FileIdInformation information, uint bufferSize);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetFileInformationByHandle(SafeFileHandle file, out LegacyFileInformation information);

	public static string Get(string path) {
		using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		// FileIdInfo (18) preserves the complete identity on NTFS and ReFS/Dev Drives.
		if (!GetFileInformationByHandleEx(stream.SafeFileHandle, 18, out var information, (uint)Marshal.SizeOf<FileIdInformation>()))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		return $"{information.VolumeSerialNumber:X16}:{information.FileIdHigh:X16}{information.FileIdLow:X16}";
	}

	public static string GetLegacy(string path) {
		using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		if (!GetFileInformationByHandle(stream.SafeFileHandle, out var information))
			throw new Win32Exception(Marshal.GetLastWin32Error());
		var fileIndex = ((ulong)information.FileIndexHigh << 32) | information.FileIndexLow;
		return $"{information.VolumeSerialNumber:X8}:{fileIndex:X16}";
	}
}
"@
}

# Fixed destinations prevent arbitrary link operations. Keep each skill's full resources together.
$aliases = @(
	@{ Alias = '.github/skills'; Target = 'agents/skills'; Directory = $true },
	@{ Alias = '.claude/skills'; Target = 'agents/skills'; Directory = $true },
	@{ Alias = '.agents/skills'; Target = 'agents/skills'; Directory = $true }
)
foreach ($directory in @('', 'src', 'tests', 'utils')) {
	$prefix = if ($directory) { "$directory/" } else { '' }
	foreach ($name in @('CLAUDE.md', 'GEMINI.md')) {
		$aliases += @{ Alias = "$prefix$name"; Target = "${prefix}AGENTS.md"; Directory = $false }
	}
}

# Remember only identities owned by this setup; an obsolete hardlink can become a one-link file.
$statePath = Get-RepositoryPath $stateRelativePath
$stateItem = Get-PathItem $statePath
if ($null -ne $stateItem) {
	if ($stateItem.Attributes -band ([System.IO.FileAttributes]::Directory -bor [System.IO.FileAttributes]::ReparsePoint)) {
		throw "Alias ownership state must be a regular file: $statePath"
	}
	$state = [System.IO.File]::ReadAllText($statePath) | ConvertFrom-Json -AsHashtable
	if ($state -isnot [System.Collections.IDictionary] -or $state.Version -notin @(1, 2) -or $state.Aliases -isnot [System.Collections.IDictionary]) {
		throw "Unrecognized alias ownership state: $statePath"
	}
	$identityPattern = if ($state.Version -eq 1) { '^[0-9A-F]{8}:[0-9A-F]{16}$' } else { '^[0-9A-F]{16}:[0-9A-F]{32}$' }
	$fileAliasNames = @($aliases | Where-Object { -not $_.Directory } | ForEach-Object { $_.Alias })
	foreach ($entry in $state.Aliases.GetEnumerator()) {
		if ($entry.Key -cnotin $fileAliasNames -or $entry.Value -isnot [string] -or $entry.Value -cnotmatch $identityPattern) {
			throw "Invalid alias ownership entry: $($entry.Key)"
		}
	}
}

# Inspect everything before mutating anything, including collisions and redirected parents.
foreach ($alias in $aliases) {
	try {
		$aliasPath = Get-RepositoryPath $alias.Alias
		$targetPath = Get-RepositoryPath $alias.Target
		$targetItem = Get-PathItem $targetPath
		if ($null -eq $targetItem -or ($targetItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
			throw "Canonical target is missing or is itself a link: $($alias.Target)"
		}
		$targetIsDirectory = [bool]($targetItem.Attributes -band [System.IO.FileAttributes]::Directory)
		if ($targetIsDirectory -ne $alias.Directory) {
			throw "Canonical target has the wrong file type: $($alias.Target)"
		}
		$item = Get-PathItem $aliasPath
		$isCorrect = $false
		$targetIdentity = $null
		if ($IsWindows -and -not $alias.Directory) {
			$targetIdentity = [Sphere10.AgentSetup.WindowsFileIdentity]::Get($targetPath)
		}
		if ($null -ne $item) {
			$isLink = [bool]($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)
			$isDirectory = [bool]($item.Attributes -band [System.IO.FileAttributes]::Directory)
			if ($isDirectory -and -not $isLink) {
				throw "Refusing to overwrite an actual directory: $($alias.Alias)"
			}
			if ($isLink) {
				$isCorrect = -not ($IsWindows -and -not $alias.Directory) -and $isDirectory -eq $alias.Directory -and (Test-LinkTarget $item $targetPath)
			} elseif ($IsWindows -and -not $alias.Directory) {
				$currentIdentity = [Sphere10.AgentSetup.WindowsFileIdentity]::Get($aliasPath)
				$isCorrect = $currentIdentity -ceq $targetIdentity
				if (-not $isCorrect) {
					$recordedIdentity = if ($state.Aliases.Contains($alias.Alias)) { $state.Aliases[$alias.Alias] } else { $null }
					$ownershipIdentity = if ($state.Version -eq 1) { [Sphere10.AgentSetup.WindowsFileIdentity]::GetLegacy($aliasPath) } else { $currentIdentity }
					if ($null -eq $recordedIdentity -or $ownershipIdentity -cne $recordedIdentity) {
						throw "Refusing to overwrite an independently maintained file: $($alias.Alias)"
					}
				}
			} else {
				throw "Refusing to overwrite an independently maintained file: $($alias.Alias)"
			}
		}
		if (-not $isCorrect -and $Check) {
			$failures.Add("Missing, broken or outdated alias: $($alias.Alias)")
		}
		$plans.Add([pscustomobject]@{ Definition = $alias; Path = $aliasPath; Target = $targetPath; Item = $item; Correct = $isCorrect; Identity = $targetIdentity })
	} catch {
		$failures.Add($_.Exception.Message)
	}
}
if ($failures.Count -gt 0) {
	throw ($failures -join [Environment]::NewLine)
}

if (-not $Check) {
	foreach ($plan in $plans) {
		if (-not $plan.Correct) {
			if ($null -ne $plan.Item) {
				Remove-AliasLink $plan.Item
			}
			Assert-WorkspaceParents $plan.Path
			[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($plan.Path)) | Out-Null
			if ($IsWindows) {
				$linkType = if ($plan.Definition.Directory) { 'Junction' } else { 'HardLink' }
				New-Item -ItemType $linkType -Path $plan.Path -Target $plan.Target | Out-Null
			} else {
				$relativeTarget = [System.IO.Path]::GetRelativePath([System.IO.Path]::GetDirectoryName($plan.Path), $plan.Target)
				if ($plan.Definition.Directory) {
					[System.IO.Directory]::CreateSymbolicLink($plan.Path, $relativeTarget) | Out-Null
				} else {
					[System.IO.File]::CreateSymbolicLink($plan.Path, $relativeTarget) | Out-Null
				}
			}
		}
		if ($IsWindows -and -not $plan.Definition.Directory) {
			$identity = [Sphere10.AgentSetup.WindowsFileIdentity]::Get($plan.Path)
			if ($identity -cne [Sphere10.AgentSetup.WindowsFileIdentity]::Get($plan.Target)) {
				throw "Instruction alias no longer identifies its canonical file: $($plan.Definition.Alias)"
			}
			if (-not $state.Aliases.Contains($plan.Definition.Alias) -or $state.Aliases[$plan.Definition.Alias] -cne $identity) {
				$state.Aliases[$plan.Definition.Alias] = $identity
				$stateChanged = $true
			}
		}
	}
	if ($IsWindows -and $state.Version -ne 2) {
		$state.Version = 2
		$stateChanged = $true
	}
	if ($stateChanged) {
		[System.IO.File]::WriteAllText($statePath, ($state | ConvertTo-Json -Depth 3) + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
	}
}

$operation = if ($Check) { 'Checked' } else { 'Configured' }
Write-Output "$operation 3 skill-directory aliases and 8 instruction aliases against the canonical repository files."
