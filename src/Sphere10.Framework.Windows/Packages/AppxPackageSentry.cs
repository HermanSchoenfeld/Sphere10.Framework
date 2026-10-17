// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Completed commands against the in-box Windows PowerShell Appx module. No profiles or interactive shell are loaded.</summary>
/// <remarks>Inventory and removal target the current user, including when the caller is elevated. Other user registrations and image provisioning are not changed.</remarks>
public class AppxPackageSentry : ProcessSentry {
	private const string PackageTypes = "Main,Framework,Resource,Bundle,Xap,Optional";
	private const string ScriptPrefix = """
		$ErrorActionPreference = 'Stop'
		$ProgressPreference = 'SilentlyContinue'
		[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
		Import-Module Appx -ErrorAction Stop
		""";

	public AppxPackageSentry()
		: base(Path.Combine(Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
			? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative") : Environment.SystemDirectory,
			"WindowsPowerShell", "v1.0", "powershell.exe")) {
	}

	public virtual async Task<WindowsAppPackageInfo[]> GetPackagesAsync(CancellationToken cancellationToken = default) {
		// The WinRT projection is supplied by Windows PowerShell, avoiding a Windows SDK dependency in consumers.
		var script = ScriptPrefix + "\n" + $$"""
			try {
				$manager = $null; $managerError = ''
				try {
					[void][Windows.Management.Deployment.PackageManager, Windows.Management.Deployment, ContentType = WindowsRuntime]
					$manager = New-Object Windows.Management.Deployment.PackageManager
				} catch { $managerError = $_.Exception.Message }
				$rows = @()
				foreach ($package in @(Get-AppxPackage -PackageTypeFilter {{PackageTypes}} -ErrorAction Stop)) {
					$displayName = ''; $publisherName = ''; $logo = ''; $metadataError = ''
					try {
						if ($null -eq $manager) { throw $managerError }
						$native = $manager.FindPackageForUser('', [string]$package.PackageFullName)
						$displayName = [string]$native.DisplayName
						$publisherName = [string]$native.PublisherDisplayName
						$logo = [string]$native.Logo.AbsoluteUri
					} catch { $metadataError = $_.Exception.Message }
					$nonRemovable = $null
					if ($null -ne $package.PSObject.Properties['NonRemovable']) { $nonRemovable = [bool]$package.NonRemovable }
					$rows += [pscustomobject]@{
						Name = [string]$package.Name; FullName = [string]$package.PackageFullName; FamilyName = [string]$package.PackageFamilyName
						Version = [string]$package.Version; Architecture = [string]$package.Architecture; Publisher = [string]$package.Publisher
						PublisherDisplayName = $publisherName; DisplayName = $displayName; InstallLocation = [string]$package.InstallLocation
						LogoUri = $logo; SignatureKind = [string]$package.SignatureKind; Status = [string]$package.Status
						IsFramework = [bool]$package.IsFramework; IsResourcePackage = [bool]$package.IsResourcePackage
						IsBundle = [bool]$package.IsBundle; IsOptional = [bool]$package.IsOptional
						NonRemovable = $nonRemovable; MetadataError = $metadataError
					}
				}
				ConvertTo-Json -InputObject @($rows) -Depth 4 -Compress
			} catch { [Console]::Error.WriteLine($_.Exception.ToString()); exit 1 }
			""";
		var result = await ExecutePackageCommandAsync(script, TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
		Guard.Ensure(!string.IsNullOrWhiteSpace(result.StandardOutput), "Windows returned no structured package inventory. The package query did not produce a successful empty inventory.");
		var packages = JsonSerializer.Deserialize<WindowsAppPackageInfo[]>(result.StandardOutput);
		Guard.Ensure(packages != null && packages.All(package => package != null && !string.IsNullOrWhiteSpace(package.FullName)), "Windows returned an incomplete package inventory.");
		return packages;
	}

	/// <summary>Removes exactly one full package identity from the current user and verifies its registration disappeared.</summary>
	/// <remarks>Cancellation terminates the owned helper, but cannot roll back a Windows deployment already committed. Re-query after interrupted or failed removal.</remarks>
	public virtual async Task RemovePackageAsync(string fullName, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrWhitespace(fullName, nameof(fullName));
		Guard.Argument(fullName.Length <= 1024 && fullName.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_'), nameof(fullName), "Supply an exact package full name.");
		// Package data is encoded separately and never interpolated as executable PowerShell syntax or a wildcard query.
		var encodedName = Convert.ToBase64String(Encoding.UTF8.GetBytes(fullName));
		var script = ScriptPrefix + "\n" + $$"""
			try {
				$fullName = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{encodedName}}'))
				$current = @(Get-AppxPackage -PackageTypeFilter {{PackageTypes}} -ErrorAction Stop | Where-Object { $_.PackageFullName -ceq $fullName })
				if ($current.Count -ne 1) { throw 'The exact package registration is no longer present. Refresh before removing it.' }
				if ($null -eq $current[0].PSObject.Properties['NonRemovable'] -or $current[0].NonRemovable) { throw 'Windows does not identify this package as removable.' }
				Remove-AppxPackage -Package $fullName -Confirm:$false -ErrorAction Stop
				$remaining = @(Get-AppxPackage -PackageTypeFilter {{PackageTypes}} -ErrorAction Stop | Where-Object { $_.PackageFullName -ceq $fullName })
				if ($remaining.Count -ne 0) { throw 'Windows still reports the package registration after removal. Refresh its current state.' }
			} catch { [Console]::Error.WriteLine($_.Exception.ToString()); exit 1 }
			""";
		await ExecutePackageCommandAsync(script, TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
	}

	protected virtual async Task<ProcessExecutionResult> ExecutePackageCommandAsync(string script, TimeSpan timeout, CancellationToken cancellationToken) {
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		deadline.CancelAfter(timeout);
		try {
			// Pass the complete fixed script as one structured argument. Package identities remain separately encoded data.
			var result = await RunCapturedAsync(new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script }, deadline.Token).ConfigureAwait(false);
			if (result.ExitCode != 0)
				throw new ProcessSentryException(ExecutableFileName, result.ExitCode, result.StandardOutput + result.StandardError);
			return result;
		} catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
			throw new TimeoutException("The Windows package command exceeded its deadline; its owned helper was terminated and drained. Re-query package state before retrying removal.");
		}
	}
}
