// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Installed driver-package information and native driver-file path conversions.</summary>
public sealed class DriverManager {
	public Task<WindowsDriverPackageInfo[]> GetDriverPackagesAsync(CancellationToken cancellationToken = default, TimeSpan? timeout = null)
		=> Task.Run(() => GetDriverPackagesCore(timeout, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public WindowsDriverPackageInfo[] GetDriverPackages(TimeSpan? timeout = null) => GetDriverPackagesCore(timeout, null);

	private WindowsDriverPackageInfo[] GetDriverPackagesCore(TimeSpan? timeout, Action checkpoint) {
		checkpoint?.Invoke();
		var queryTimeout = timeout ?? TimeSpan.FromSeconds(15);
		Guard.Argument(queryTimeout > TimeSpan.Zero, nameof(timeout), "The query timeout must be positive.");
		using var searcher = new ManagementObjectSearcher(new ManagementScope(@"\\.\root\cimv2", new ConnectionOptions { Timeout = queryTimeout }),
			new ObjectQuery("SELECT DeviceName,DeviceID,DeviceClass,DriverVersion,DriverProviderName,IsSigned,Signer,InfName,DriverDate FROM Win32_PnPSignedDriver"),
			new System.Management.EnumerationOptions { Timeout = queryTimeout, Rewindable = false, ReturnImmediately = true });
		using var objects = searcher.Get();
		var result = new List<WindowsDriverPackageInfo>();
		foreach (ManagementObject item in objects) {
			using (item) {
				checkpoint?.Invoke();
				var date = item["DriverDate"]?.ToString();
				result.Add(new WindowsDriverPackageInfo {
					DeviceName = item["DeviceName"]?.ToString(), DeviceId = item["DeviceID"]?.ToString(), DeviceClass = item["DeviceClass"]?.ToString(),
					DriverVersion = item["DriverVersion"]?.ToString(), Provider = item["DriverProviderName"]?.ToString(),
					IsSigned = item["IsSigned"] is bool signed ? signed : null, Signer = item["Signer"]?.ToString(), InfName = item["InfName"]?.ToString(),
					DriverDate = string.IsNullOrEmpty(date) ? null : ManagementDateTimeConverter.ToDateTime(date)
				});
			}
		}
		checkpoint?.Invoke();
		return result.ToArray();
	}

	public static string ResolveInfPath(string infName, string windowsDirectory = null) {
		Guard.ArgumentNotNullOrWhitespace(infName, nameof(infName));
		windowsDirectory ??= Environment.GetFolderPath(Environment.SpecialFolder.Windows);
		Guard.Argument(Path.GetFileName(infName) == infName && infName.EndsWith(".inf", StringComparison.OrdinalIgnoreCase), nameof(infName), "The INF filename is invalid.");
		return Path.GetFullPath(Path.Combine(windowsDirectory, "INF", infName));
	}

	public static string ResolveBinaryPath(string binaryPath, string windowsDirectory = null) {
		Guard.ArgumentNotNullOrWhitespace(binaryPath, nameof(binaryPath));
		windowsDirectory ??= Environment.GetFolderPath(Environment.SpecialFolder.Windows);
		var path = binaryPath.Trim();
		if (path.StartsWith('"')) {
			Guard.Argument(path.Length > 1 && path.EndsWith('"') && path.IndexOf('"', 1) == path.Length - 1, nameof(binaryPath), "The binary registration contains a command line rather than one file path.");
			path = path[1..^1];
		}
		path = Environment.ExpandEnvironmentVariables(path);
		if (path.StartsWith(@"\??\", StringComparison.Ordinal))
			path = path[4..];
		if (path.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase))
			path = Path.Combine(windowsDirectory, path[12..]);
		else if (path.StartsWith(@"System32\", StringComparison.OrdinalIgnoreCase))
			path = Path.Combine(windowsDirectory, path);
		Guard.Argument(Path.IsPathFullyQualified(path) && !path.StartsWith(@"\\", StringComparison.Ordinal)
			&& path.EndsWith(".sys", StringComparison.OrdinalIgnoreCase), nameof(binaryPath), "The registration does not identify an absolute local .sys file.");
		return Path.GetFullPath(path);
	}
}
