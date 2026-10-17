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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Sphere10.Framework.Windows.Startup;

/// <summary>Provides Windows startup registration, folder and StartupApproved data without interpreting its UI state.</summary>
public class WindowsStartupUtil {
	private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\";
	private const string ApprovalPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";

	public RegistryAddress[] GetRunLocations() {
		var native = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
		var roots = new List<RegistryAddress>();
		foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine }) {
			var views = hive == RegistryHive.LocalMachine && Environment.Is64BitOperatingSystem ? new[] { native, RegistryView.Registry32 } : new[] { native };
			foreach (var view in views)
				foreach (var name in new[] { "Run", "RunOnce" })
					roots.Add(new RegistryAddress(hive, view, RunPath + name));
		}
		return roots.ToArray();
	}

	public RegistryValue[] ReadRunValues(RegistryAddress location) => new RegistryUtil().ReadValues(location, throwIfMissing: false);

	public Task<RegistryValue[]> ReadRunValuesAsync(RegistryAddress location, CancellationToken cancellationToken = default)
		=> new RegistryUtil().ReadValuesAsync(location, throwIfMissing: false, cancellationToken: cancellationToken);

	public string GetApprovalCategory(RegistryAddress location) {
		var name = location.SubKeyPath[(location.SubKeyPath.LastIndexOf('\\') + 1)..];
		if (!name.Equals("Run", StringComparison.OrdinalIgnoreCase))
			return null;
		return location.Hive == RegistryHive.LocalMachine && location.View == RegistryView.Registry32 && Environment.Is64BitOperatingSystem ? "Run32" : "Run";
	}

	public byte[] ReadApproval(RegistryHive hive, string category, string name) {
		Guard.ArgumentNotNullOrWhitespace(category, nameof(category));
		Guard.Argument(!category.Contains('\\') && !category.Contains('\0'), nameof(category), "Specify one startup approval category.");
		var view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
		return new RegistryUtil().ReadValue(new RegistryAddress(hive, view, ApprovalPath + category), name)?.Data as byte[];
	}

	public string GetStartupDirectory(bool allUsers)
		=> Environment.GetFolderPath(allUsers ? Environment.SpecialFolder.CommonStartup : Environment.SpecialFolder.Startup);

	public string[] GetStartupFiles(bool allUsers) => GetStartupFilesCore(allUsers, null);

	public Task<string[]> GetStartupFilesAsync(bool allUsers, CancellationToken cancellationToken = default)
		=> Task.Run(() => GetStartupFilesCore(allUsers, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private string[] GetStartupFilesCore(bool allUsers, Action checkpoint) {
		checkpoint?.Invoke();
		var folder = GetStartupDirectory(allUsers);
		if (string.IsNullOrEmpty(folder))
			return Array.Empty<string>();
		var files = new List<string>();
		foreach (var file in Directory.EnumerateFiles(folder)) {
			checkpoint?.Invoke();
			files.Add(file);
		}
		return files.ToArray();
	}
}
